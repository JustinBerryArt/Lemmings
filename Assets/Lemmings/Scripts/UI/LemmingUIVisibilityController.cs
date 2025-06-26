using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

namespace Lemmings.UI
{
    /// <summary>
    /// Controls the visibility of the remapping UI at runtime.
    /// Supports toggling via keyboard input or UnityEvents (e.g., gesture-based triggers).
    /// </summary>
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public class LemmingUIVisibilityController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("➤ Visibility Settings")]

        /// <summary>
        /// If true, the UI will be active at all times.
        /// Use this for scenes dedicated to remapping where you never want the UI hidden.
        /// </summary>
        [Tooltip("If true, the UI is always visible (disables toggle logic).")]
        public bool alwaysVisible = false;

        /// <summary>
        /// If true, the toggle key can be used to show/hide the UI.
        /// You can turn this off to use gesture-based or event-only toggling.
        /// </summary>
        [Tooltip("Enable key-based toggling for developer or debug access.")]
        public bool enableKeyInput = true;

        /// <summary>
        /// The keyboard key used to toggle the UI visibility.
        /// This will only be used if <see cref="enableKeyInput"/> is true.
        /// </summary>
        [Tooltip("Keyboard key used to toggle the UI.")]
        public Key toggleKey = Key.F1;

        [Header("➤ UI Root")]

        /// <summary>
        /// The root GameObject for the UI to show or hide.
        /// This GameObject will be enabled/disabled during toggling.
        /// </summary>
        [Tooltip("Root GameObject of the UI to control visibility.")]
        public GameObject uiRoot;

        [Header("➤ UnityEvent Hook")]

        /// <summary>
        /// Optional UnityEvent that can trigger a toggle externally (e.g., from a Lemming gesture).
        /// External scripts or the inspector can invoke this at runtime.
        /// </summary>
        [Tooltip("Externally callable event to toggle UI visibility.")]
        public UnityEvent OnUIToggle;

        #endregion

        // -------------------- Unity Lifecycle --------------------

        /// <summary>
        /// Unity Start method — initializes the UI and hook.
        /// </summary>
        private void Start()
        {
            if (uiRoot == null)
            {
                Debug.LogWarning("[LemmingUIVisibilityController] No UI root assigned.");
                return;
            }

            // Start in correct visibility state
            uiRoot.SetActive(alwaysVisible);

            // Ensure UnityEvent exists
            if (OnUIToggle == null)
                OnUIToggle = new UnityEvent();

            // Attach the toggle handler to the UnityEvent
            OnUIToggle.AddListener(ToggleVisibility);
        }

        /// <summary>
        /// Unity Update method — checks for toggle key input.
        /// Only runs if key toggling is enabled and alwaysVisible is false.
        /// </summary>
        private void Update()
        {
            if (alwaysVisible || uiRoot == null || !enableKeyInput) return;

            if (Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                ToggleVisibility();
            }
        }

        // -------------------- Public API --------------------

        /// <summary>
        /// Toggles the visibility of the UI root GameObject.
        /// Only has an effect if <see cref="alwaysVisible"/> is false.
        /// </summary>
        public void ToggleVisibility()
        {
            if (alwaysVisible || uiRoot == null) return;

            uiRoot.SetActive(!uiRoot.activeSelf);
        }

        /// <summary>
        /// Forces the UI to be shown regardless of current state.
        /// </summary>
        public void ShowUI()
        {
            if (uiRoot != null)
                uiRoot.SetActive(true);
        }

        /// <summary>
        /// Forces the UI to be hidden regardless of current state.
        /// </summary>
        public void HideUI()
        {
            if (uiRoot != null)
                uiRoot.SetActive(false);
        }
    }
}