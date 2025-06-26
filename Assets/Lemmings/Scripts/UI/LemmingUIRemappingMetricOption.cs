using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lemmings;
using Lemmings.Input.Editor;

namespace Lemmings.UI
{
    /// <summary>
    /// Leaf‐level script for one row in the “Metric” scroll‐list.
    /// Binds a MetricOptionInfo and raises onMetricChanged when the dropdown moves.
    /// </summary>
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public class LemmingUIRemappingMetricOption : UIDataRow<MetricSettingInfo>
    
    //TODO: This is currently a stub, I will need to update and implement this once the setting structure is better defined.
    //TODO: 1: A method for limiting metric remapping options needs to be available ot ensure predictable outcomes
    //TODO: 2: Settings need to established and structured in a predictable way - sliders - enums - toggles - ect.
    
    {
        
        
        
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private TextMeshProUGUI description;
        [SerializeField] private Slider slider;
        [SerializeField] private Toggle toggle;
        [SerializeField] private TMP_Dropdown dropdown;

        public event System.Action<MetricSettingInfo> onSettingChanged;

        protected override void Subscribe()
        {
            slider.onValueChanged.AddListener(v => { _data.floatValue = v; onSettingChanged?.Invoke(_data); });
            toggle.onValueChanged.AddListener(v => { _data.boolValue = v; onSettingChanged?.Invoke(_data); });
            dropdown.onValueChanged.AddListener(i => { _data.selectedIndex = i; onSettingChanged?.Invoke(_data); });
        }

        
        

        
        protected override void Unsubscribe()
        {
            slider.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.RemoveAllListeners();
        }

        protected override void UpdateUI()
        {
            if (label == null)
                Debug.LogWarning($"[MetricOption] Label is missing on {gameObject.name}");

            if (_data == null)
                Debug.LogWarning($"[MetricOption] Data is null on {gameObject.name}");

            
            if (label != null)
                label.text = _data.displayName ?? "<no name>";
            else
                Debug.LogWarning($"[MetricOption] Label not assigned on {gameObject.name}");

            
            if (description != null)
                 description.text = _data.description;
            Debug.LogWarning($"[MetricOption] description not assigned on {gameObject.name}");
            
            // First hide all controls…
            slider.gameObject.SetActive(false);
            toggle.gameObject.SetActive(false);
            dropdown.gameObject.SetActive(false);

            // …then show/configure the one we need:
            switch (_data.controlType)
            {
                case SettingType.Slider:
                    slider.gameObject.SetActive(true);
                    slider.value = _data.floatValue;
                    break;
                case SettingType.Toggle:
                    toggle.gameObject.SetActive(true);
                    toggle.isOn = _data.boolValue;
                    break;
                case SettingType.Dropdown:
                    dropdown.gameObject.SetActive(true);
                    dropdown.ClearOptions();
                    dropdown.AddOptions(_data.options);
                    dropdown.value = _data.selectedIndex;
                    break;
            }
        }
    }
}