using System.Collections.Generic;
using UnityEngine;

namespace Lemmings.UI
{
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------
    
    #region UIDataRow

    /// <summary>
    /// Base for any UI row that binds a model of type T.
    /// Handles subscription lifecycle; subclasses implement UpdateUI.
    /// </summary>
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public abstract class UIDataRow<T> : MonoBehaviour
    {
        protected T _data;

        /// <summary>
        /// Called by your controller to bind this row to new data.
        /// </summary>
        public virtual void Bind(T data)
        {
            Unsubscribe();
            _data = data;
            Subscribe();
            UpdateUI();
        }

        /// <summary>
        /// Hookup any model events here (e.g. OnDatumUpdated).
        /// </summary>
        protected virtual void Subscribe() {}

        /// <summary>
        /// Tear down any event handlers here.
        /// </summary>
        protected virtual void Unsubscribe() {}

        /// <summary>
        /// Populate your Text/Dropdown/etc. based on _data.
        /// </summary>
        protected abstract void UpdateUI();

        private void OnDestroy() => Unsubscribe();
    }

    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    
    #region MetricOptionData
    
    /// <summary>
    /// Temporary data‐carrier for one metric option row.
    /// Fill this out when you have real metric names, IDs, and tuning parameters.
    /// </summary>
    public class MetricOptionInfo
    {
        /// <summary>Name to display in the row (e.g. "Distance", "Angle").</summary>
        public string displayName;

        /// <summary>List of available metric names / enum strings.</summary>
        public List<string> availableMetrics = new List<string>();

        public string description;
        
        /// <summary>Index of the currently selected metric in availableMetrics.</summary>
        public int selectedIndex;

        public int currentIndex;
        // ▼ Later you might add:
        // public float minValue, maxValue;
        // public AnimationCurve customCurve;
        // public Vector3 axisOverride;
        // etc.
    }
    
    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    
    #region MetricSettingInfo
    
    /// <summary>
    /// A single metric‐setting slot.  
    /// Right now it’s a stub—you’ll add real fields once you know what each metric needs.
    /// </summary>
    [System.Serializable]
    public class MetricSettingInfo
    {
        /// <summary>
        /// The label to show in the UI (e.g. “Rotate Around”).</summary>
        public string displayName;

        /// <summary>
        /// The label to show in the UI (e.g. “Rotate Around”).</summary>
        public string description;
        
        /// <summary>
        /// What kind of control to show: a slider, toggle, dropdown, etc.</summary>
        public SettingType controlType;

        
        
        /// <summary>
        /// For sliders or numeric inputs.</summary>
        public float floatValue;

        /// <summary>
        /// For on/off toggles.</summary>
        public bool boolValue;

        /// <summary>
        /// For dropdowns: list of options.</summary>
        public List<string> options;

        public Vector3 vector3Value;
        public Object objectReference;
        public AnimationCurve curveValue;
        
        /// <summary>
        /// For dropdowns: index of the selected option.</summary>
        public int selectedIndex;
    }

    public enum SettingType
    {
        Slider,
        Toggle,
        Dropdown,
        Vector3Picker,
        ObjectSelector,
        
    }
    
    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    
    #region Role mapping info
    
    /// <summary>
    /// Data model for binding a role-to-lemming mapping row.
    /// </summary>
    public class RoleMappingInfo
    {
        /// <summary>The role label (e.g., "Leader").</summary>
        public string Role;

        /// <summary>The currently selected lemming name.</summary>
        public string SelectedName;

        /// <summary>List of valid lemming names to choose from.</summary>
        public LemmingHerdSnapshot Herd;
    }
    
    #endregion
    
}

