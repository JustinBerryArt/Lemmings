using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using System.Collections.Generic;
using Lemmings.Input;
using UnityEngine.InputSystem.Utilities;

#if UNITY_EDITOR
using UnityEditor;


/// <summary>
/// Initializes the custom Lemming input device layout at editor and runtime startup.
/// Uses a builder pattern to register a layout defined in a JSON file, and ensures that
/// a corresponding virtual input device is available in the system.
/// </summary>
[InitializeOnLoad]
public static class LemmingDeviceInitializer
{
    /// <summary>
    /// The expected name of the device layout.
    /// This must match what other components (like LemmingDeviceManager) use to query the device.
    /// </summary>
    private const string LayoutName = "Lemmings";

    /// <summary>
    /// Path to the generated layout JSON file defining device controls.
    /// </summary>
    private const string LayoutPath = "Assets/Input/Lemmings.layout.json";

    /// <summary>
    /// Serializable container for reading JSON layout configuration.
    /// Represents the full device layout definition.
    /// </summary>
    [System.Serializable]
    private class LayoutDefinition
    {
        public string name;
        public List<ControlField> controls;
    }

    /// <summary>
    /// Serializable control descriptor for each input on the virtual device.
    /// </summary>
    [System.Serializable]
    private class ControlField
    {
        public string name;
        public string layout;
        public string format;
    }

    /// <summary>
    /// Static constructor ensures that this logic runs automatically when Unity starts or recompiles.
    /// It loads and registers the Lemming device layout and guarantees a device instance exists.
    /// </summary>
    static LemmingDeviceInitializer()
    {
        RegisterBuilderLayout();
        EnsureDevice();
    }

    /// <summary>
    /// Reads the layout JSON file and dynamically builds a control layout using InputSystem APIs.
    /// If a previous layout with the same name exists, it is first removed to ensure a clean state.
    /// </summary>
    private static void RegisterBuilderLayout()
    {
        if (!File.Exists(LayoutPath))
        {
            Debug.LogWarning($"[LemmingDeviceInitializer] Layout file not found at: {LayoutPath}");
            return;
        }

        try
        {
            // Load and parse layout definition from JSON
            string json = File.ReadAllText(LayoutPath);
            var layoutData = new LayoutDefinition();
            JsonUtility.FromJsonOverwrite(json, layoutData);

            if (layoutData == null || string.IsNullOrWhiteSpace(layoutData.name) || layoutData.controls == null)
            {
                Debug.LogError("[LemmingDeviceInitializer] Layout file format is invalid.");
                return;
            }

            // Remove any existing layout with the same name to prevent conflicts
            InputSystem.RemoveLayout(LayoutName);

            // Dynamically register the layout using a builder
            InputSystem.RegisterLayoutBuilder(() =>
            {
                var builder = new InputControlLayout.Builder()
                    .WithName(LayoutName)
                    .WithType<LemmingDevice>(); // Link to custom InputDevice subclass

                foreach (var control in layoutData.controls)
                {
                    // Define each control from the layout file
                    var controlBuilder = builder.AddControl(control.name).WithLayout(control.layout);
                    if (!string.IsNullOrEmpty(control.format))
                        controlBuilder.WithFormat(new FourCC(control.format));
                }

                return builder.Build();
            }, layoutData.name);

            Debug.Log("[LemmingDeviceInitializer] Builder-based layout registered.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LemmingDeviceInitializer] Failed to register builder layout: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that a LemmingDevice instance exists in the InputSystem.
    /// If not present, it adds one based on the registered layout.
    /// </summary>
    private static void EnsureDevice()
    {
        if (InputSystem.GetDevice(LayoutName) != null)
        {
            Debug.Log("[LemmingDeviceInitializer] LemmingDevice already exists.");
            return;
        }

        try
        {
            // Add a new virtual device instance using the layout name
            InputSystem.AddDevice(LayoutName);
            Debug.Log("[LemmingDeviceInitializer] LemmingDevice created.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LemmingDeviceInitializer] Failed to create device: {ex.Message}");
        }
    }
}
#endif
