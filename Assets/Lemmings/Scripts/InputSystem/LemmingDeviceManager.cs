using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace Lemmings.Input
{
    /// <summary>
    /// Manages runtime updates for the virtual LemmingDevice input device.
    /// It collects data from LemmingRelationships and feeds the results into InputSystem-compatible controls,
    /// enabling real-time binding of spatial relationships to Unity Input Actions.
    /// </summary>
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public class LemmingDeviceManager : MonoBehaviour
    {
        /// <summary>
        /// Array of all loaded relationships configured to push values into the Input System.
        /// Populated on Start via Resources.LoadAll.
        /// </summary>
        private LemmingRelationship[] loadedRelationships;

        /// <summary>
        /// Internal flag to prevent repeated warnings about device load failures.
        /// </summary>
        private static bool hasWarnedAboutMissingDevice = false;

        /// <summary>
        /// Provides a static reference to the registered Lemming virtual input device.
        /// </summary>
        public static InputDevice Instance => InputSystem.GetDevice("Lemmings");

        // Runtime control buffers for each supported value type.
        private readonly Dictionary<string, float> floatValues = new();
        private readonly Dictionary<string, Vector3> vector3Values = new();
        private readonly Dictionary<string, Quaternion> quaternionValues = new();

        /// <summary>
        /// Unity Start callback. Loads all eligible LemmingRelationships from Resources,
        /// filters them by useInputSystem flag, and logs the loaded controls.
        /// </summary>
        private void Start()
        {
            loadedRelationships = Resources
                .LoadAll<LemmingRelationship>("LemmingRelationships")
                .Where(r => r != null && r.useInputSystem)
                .ToArray();

            Debug.Log($"[LemmingDeviceManager] Loaded {loadedRelationships.Length} input-enabled relationships.");

            // Optional: enumerate controls on the device to validate layout and binding names.
            foreach (var control in InputSystem.GetDevice("Lemmings.layout").allControls)
            {
                Debug.Log($"[{control.name}] => {control.layout}, actual type: {control.GetType().Name}");
            }
        }

        /// <summary>
        /// Unity Update loop. Each frame, we gather values from relationships and feed them into the virtual device.
        /// </summary>
        private void Update()
        {
            // Skip update if no relationships are active.
            if (loadedRelationships == null || loadedRelationships.Length == 0)
                return;

            // Skip update if the virtual device is not found.
            if (Instance == null)
            {
                if (!hasWarnedAboutMissingDevice)
                {
                    Debug.LogWarning("[LemmingDeviceManager] LemmingDevice not found. Did the initializer run?");
                    hasWarnedAboutMissingDevice = true;
                }
                return;
            }

            // Propagate updated values from relationship data into virtual device control cache.
            UpdateFromRelationships(loadedRelationships);
        }

        /// <summary>
        /// Collects the output values from all active LemmingRelationships and organizes them
        /// into dictionaries grouped by data type (float, Vector3, Quaternion).
        /// </summary>
        /// <param name="relationships">The relationships to evaluate and extract values from.</param>
        public void UpdateFromRelationships(IEnumerable<LemmingRelationship> relationships)
        {
            floatValues.Clear();
            vector3Values.Clear();
            quaternionValues.Clear();

            foreach (var rel in relationships)
            {
                if (rel == null || string.IsNullOrWhiteSpace(rel.ID))
                    continue;

                string id = rel.ID;
                var conv = rel.ToConverter();   // Provides normalized, curved, and threshold flags
                var datum = rel.SetValue();     // Forces evaluation and caching of new value

                // Primary control: raw value, differentiated by value type
                switch (datum.Type)
                {
                    case LemmingValueType.Vector3:
                        vector3Values[id + ".RawValue"] = datum.AsVector3();
                        break;
                    case LemmingValueType.Quaternion:
                        quaternionValues[id + ".RawValue"] = datum.AsQuaternion();
                        break;
                    default:
                        floatValues[id + ".RawValue"] = datum.AsFloat();
                        break;
                }

                // Supplemental controls: normalized and threshold flags
                floatValues[id + ".Normalized"] = conv.Normalized;
                floatValues[id + ".Curved"]     = conv.ToCurve(rel.Curve);
                floatValues[id + ".Over"]       = conv.Over     ? 1f : 0f;
                floatValues[id + ".Under"]      = conv.Under    ? 1f : 0f;
                floatValues[id + ".InRange"]    = conv.InRange  ? 1f : 0f;
                floatValues[id + ".AsAxis"]     = conv.AsAxis;
            }

            // Push all accumulated values into the input system
            SendToInputSystem();
        }

        /// <summary>
        /// Converts the most recent float, vector, and quaternion values into
        /// InputSystem control events and queues them for processing.
        /// </summary>
        private void SendToInputSystem()
        {
            var device = Instance;
            if (device == null)
                return;

            // Allocate a single StateEvent to push all values at once (faster and less garbage)
            using var buffer = StateEvent.From(device, out InputEventPtr eventPtr);

            // Write float values into device controls
            foreach (var kvp in floatValues)
            {
                var control = device[kvp.Key];
                if (control == null)
                    continue;

                if (control is ButtonControl button)
                {
                    // Boolean-style flag written as 1 or 0
                    bool pressed = kvp.Value >= 0.5f;
                    button.WriteValueIntoEvent(pressed ? 1f : 0f, eventPtr);
                }
                else if (control is AxisControl axis)
                {
                    // Scale normalized float from [0,1] to [-1,1] for typical Input axis mapping
                    float axisValue = kvp.Value * 2f - 1f;
                    axis.WriteValueIntoEvent(axisValue, eventPtr);
                }
            }

            // Write vector values into vector controls
            foreach (var kvp in vector3Values)
            {
                if (device[kvp.Key] is Vector3Control vec)
                {
                    vec.WriteValueIntoEvent(kvp.Value, eventPtr);
                }
            }

            // Write quaternion values into quaternion controls
            foreach (var kvp in quaternionValues)
            {
                if (device[kvp.Key] is QuaternionControl quat)
                {
                    quat.WriteValueIntoEvent(kvp.Value, eventPtr);
                }
            }

            // Final step: queue the composed input event to be read by Unity’s InputSystem
            InputSystem.QueueEvent(eventPtr);
        }
    }
}
