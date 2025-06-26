using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Lemmings.UI
{
    /// <summary>
    /// Row view for a single LemmingRelationship in the Control Pane.
    /// Handles its own data binding, updating, and event wiring independently.
    /// </summary>
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public class LemmingUIRelationshipDataRow : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI metricText;
        [SerializeField] private TextMeshProUGUI outputText;
        [SerializeField] private TextMeshProUGUI normalText;
        [SerializeField] private TextMeshProUGUI curvedText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button remapButton;

        private LemmingRelationshipInfo _info;

        /// <summary>
        /// Raised when the 'Remap' button is clicked on this row.
        /// </summary>
        public event Action<LemmingUIRelationshipDataRow> onRemapClicked;

        private void Awake()
        {
            // Validate and wire the remap button
            if (remapButton == null)
                Debug.LogError($"[LemmingUIRelationshipDataRow] remapButton is not assigned on '{gameObject.name}'");
            else
                remapButton.onClick.AddListener(() => onRemapClicked?.Invoke(this));

            // Validate text references
            if (nameText   == null) Debug.LogError($"[LemmingUIRelationshipDataRow] nameText is not assigned on '{gameObject.name}'");
            if (metricText == null) Debug.LogError($"[LemmingUIRelationshipDataRow] metricText is not assigned on '{gameObject.name}'");
            if (outputText == null) Debug.LogError($"[LemmingUIRelationshipDataRow] outputText is not assigned on '{gameObject.name}'");
            if (normalText == null) Debug.LogError($"[LemmingUIRelationshipDataRow] normalText is not assigned on '{gameObject.name}'");
            if (curvedText == null) Debug.LogError($"[LemmingUIRelationshipDataRow] curvedText is not assigned on '{gameObject.name}'");
            if (statusText == null) Debug.LogError($"[LemmingUIRelationshipDataRow] statusText is not assigned on '{gameObject.name}'");
        }

        /// <summary>
        /// Bind this row to the given LemmingRelationshipInfo and (re)draw the UI.
        /// </summary>
        public void Bind(LemmingRelationshipInfo info)
        {
            // Unsubscribe previous update events
            if (_info.Relationship != null)
                _info.Relationship.OnDatumUpdated -= HandleDatumUpdated;

            _info = info.Relationship.Info;

            // Subscribe to future updates
            if (_info.Relationship != null)
                _info.Relationship.OnDatumUpdated += HandleDatumUpdated;
            
            // Subscribe to Update and refresh UI accordingly
            info.Relationship.Updated += () => RefreshUI();
            
            // Initial draw
            RefreshUI();
        }

        private void HandleDatumUpdated(LemmingDatum _) => RefreshUI();

        /// <summary>
        /// Safely redraw all UI fields based on the current info.
        /// </summary>
        public void RefreshUI()
        {
            // If any UI reference is missing, skip without crashing
            if (nameText == null || metricText == null || outputText == null || normalText == null || curvedText == null || statusText == null)
                return;

            // If no relationship, clear fields
            if (_info.Relationship == null)
            {
                ClearUI();
                return;
            }

            // Static labels
            nameText.text   = _info.Id;
            metricText.text = _info.MetricName;
            outputText.text = _info.RawValue.ToString() ?? "<null>";
            normalText.text = _info.NormalizedValue.ToString("F2") ?? "<null>";
            curvedText.text = _info.CurvedValue.ToString("F2") ?? "<null>";
            statusText.text = _info.Status.ToString("F");
                                
        }

        private void ClearUI()
        {
            nameText.text   = string.Empty;
            metricText.text = string.Empty;
            outputText.text = string.Empty;
            normalText.text = string.Empty;
            curvedText.text = string.Empty;
            statusText.text = string.Empty;
        }

        /// <summary>
        /// Exposes the current bound relationship info for external access or rebinding.
        /// </summary>
        public LemmingRelationshipInfo RelationshipInfo
        {
            get => _info;
            set => Bind(value);
        }
    }
}
