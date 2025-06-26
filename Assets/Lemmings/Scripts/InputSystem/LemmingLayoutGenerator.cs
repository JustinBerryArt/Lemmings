using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem.Layouts;

namespace Lemmings.Input.Editor
{
    #if UNITY_EDITOR
    
    /// <summary>
    /// Generates a static JSON layout file representing all input-enabled LemmingRelationships.
    /// This layout is registered by the InputSystem at runtime to define virtual control bindings for the LemmingDevice.
    /// </summary>
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public static class LemmingLayoutGenerator
    {
        /// <summary>
        /// Suffixes to append to each relationship ID to define a full set of input controls.
        /// These represent different views or interpretations of the base value.
        /// </summary>
        private static readonly string[] suffixes = new[]
        {
            ".RawValue",
            ".Normalized",
            ".Curved",
            ".Over",
            ".Under",
            ".InRange",
            ".AsAxis"
        };

        /// <summary>
        /// Menu item to generate and write a Lemmings.layout.json file based on all enabled relationships in Resources.
        /// Produces a device layout for Unity's InputSystem that can be registered at startup.
        /// </summary>
        [MenuItem("Lemmings/Update Input System Layout")]
        public static void GenerateJsonLayout()
        {
            // Load all LemmingRelationships stored in the designated Resources path
            var relationships = Resources.LoadAll<LemmingRelationship>("LemmingRelationships");
            if (relationships == null || relationships.Length == 0)
            {
                Debug.LogWarning("LemmingShepherd with relationships not found in scene.");
                return;
            }

            var layout = new LemmingLayoutJson
            {
                name = "Lemmings", // This must match the virtual device name used elsewhere
                controls = new List<ControlField>()
            };

            var seen = new HashSet<string>(); // To avoid duplicate controls from suffix reuse

            foreach (var rel in relationships)
            {
                if (rel == null || !rel.useInputSystem || string.IsNullOrWhiteSpace(rel.ID))
                    continue;

                foreach (var suffix in suffixes)
                {
                    string controlName = rel.ID + suffix;

                    // Always add RawValue control directly using actual type layout
                    if (suffix == ".RawValue")
                    {
                        layout.controls.Add(new ControlField
                        {
                            name = controlName,
                            layout = GetLayoutForType(rel.Type)
                        });
                    }
                    // Add other controls only once
                    else if (seen.Add(controlName))
                    {
                        bool isBooleanLike = suffix is ".Over" or ".Under" or ".InRange";

                        layout.controls.Add(new ControlField
                        {
                            name = controlName,
                            layout = isBooleanLike ? "Button" : "Axis",
                            format = isBooleanLike ? "BYTE" : null
                        });
                    }

                    // TODO: Optional: Register a custom icon for the device in the Input Debugger
                    // LemmingDeviceIconRegistrar.RegisterIcon();
                }
            }

            // Serialize the control layout to JSON
            string json = JsonUtility.ToJson(layout, true);

            // Write to disk and refresh asset database
            string path = "Assets/Input/Lemmings.layout.json";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();

            Debug.Log($"LemmingDevice control list written to {path} with {layout.controls.Count} controls. Register the layout in code using RegisterLayout<LemmingDevice>().");
        }

        /// <summary>
        /// Converts a LemmingValueType into a Unity InputSystem layout name.
        /// These names must match built-in control layouts (e.g. "Axis", "Button").
        /// </summary>
        private static string GetLayoutForType(LemmingValueType type)
        {
            return type switch
            {
                LemmingValueType.Float      => "Axis",
                LemmingValueType.Int        => "Axis",
                LemmingValueType.Bool       => "Button",
                LemmingValueType.Vector3    => "Vector3",
                LemmingValueType.Quaternion => "Quaternion",
                LemmingValueType.String     => "String",
                _                           => "Axis"
            };
        }

        /// <summary>
        /// Serializable container for writing InputDevice layout data to JSON.
        /// </summary>
        [Serializable]
        private class LemmingLayoutJson
        {
            public string name;
            public List<ControlField> controls;
        }

        /// <summary>
        /// Serializable description of a single control entry in the layout.
        /// </summary>
        [Serializable]
        private class ControlField
        {
            public string name;
            public string layout;
            public string format; // Optional (e.g., "BYTE" for Button)
        }

    }
#endif
}
