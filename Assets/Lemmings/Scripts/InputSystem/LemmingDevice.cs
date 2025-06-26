using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using System.Collections.Generic;
using UnityEngine;

namespace Lemmings.Input
{
    /// <summary>
    /// Custom input device for Lemming virtual input system.
    /// Supports dynamic control resolution based on layout JSON.
    /// </summary>
    [InputControlLayout(displayName = "Lemmings")]
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public class LemmingDevice : InputDevice
    {
        /// <summary>
        /// Attempts to resolve an AxisControl by name.
        /// Returns null if not found.
        /// </summary>
        public AxisControl GetFloat(string controlName) => this[controlName] as AxisControl;

        /// <summary>
        /// Attempts to resolve a ButtonControl by name.
        /// Returns null if not found.
        /// </summary>
        public ButtonControl GetBool(string controlName) => this[controlName] as ButtonControl;

        /// <summary>
        /// Attempts to resolve a Vector3Control by name.
        /// </summary>
        public Vector3Control GetVector(string controlName) => this[controlName] as Vector3Control;

        /// <summary>
        /// Attempts to resolve a QuaternionControl by name.
        /// </summary>
        public QuaternionControl GetQuaternion(string controlName) => this[controlName] as QuaternionControl;

        protected override void FinishSetup()
        {
            base.FinishSetup();
            // No fixed control names — controls are resolved dynamically.
        }


    }
}
