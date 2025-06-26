using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lemmings;
using Lemmings.UI;
using TMPro;

/// <summary>
/// Manages the remapping UI, binding data from the <see cref="LemmingShepherd"/>
/// and allowing runtime editing of relationship parameters such as min/max bounds,
/// curves, and role-to-lemming assignments. Users can experiment with settings
/// before committing changes via an Apply button.
/// </summary>
[Icon("Assets/Input/Icons/LemmingPale.png")]
public class LemmingUIRemappingController : MonoBehaviour
{
        
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region Control Pane Prefabs & Containers

    /// <summary>
    /// Container Transform under which <see cref="LemmingUIRelationshipDataRow"/> instances are instantiated.
    /// </summary>
    [SerializeField]
    private RectTransform relationshipContent;

    /// <summary>
    /// Prefab for a single relationship row in the control pane.
    /// </summary>
    [SerializeField]
    private LemmingUIRelationshipDataRow relationshipDataPrefab;

    /// <summary>
    /// Container Transform under which <see cref="LemmingUIDataRow"/> instances are instantiated.
    /// </summary>
    [SerializeField]
    private RectTransform lemmingContent;

    /// <summary>
    /// Prefab for a single lemming row in the control pane.
    /// </summary>
    [SerializeField]
    private LemmingUIDataRow lemmingDataPrefab;

    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region Mapping Pane Prefabs & Containers

    /// <summary>
    /// Container Transform under which metric setting rows are instantiated.
    /// </summary>
    [SerializeField]
    private RectTransform metricOptionsContent;

    /// <summary>
    /// Prefab for a single metric option row (e.g. slider, toggle, dropdown).
    /// </summary>
    [SerializeField]
    private LemmingUIRemappingMetricOption metricPrefab;

    /// <summary>
    /// Container Transform under which role-to-lemming mapping rows are instantiated.
    /// </summary>
    [SerializeField]
    private RectTransform remappingLemmingContent;

    /// <summary>
    /// Prefab for a single role mapping row.
    /// </summary>
    [SerializeField]
    private LemmingUIRemappingLemmingRow lemmingRemappingPrefab;

    /// <summary>
    /// Container for remapping update for raw value
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI remappingOutput;
    
    /// <summary>
    /// Container for remapping update for normalized value
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI remappingNormal;
    
    /// <summary>
    /// Container for remapping update for curved value
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI remappingCurve;
    
    /// <summary>
    /// Container for remapping update for status
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI remappingStatus;
    
    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region Mapping Pane Controls

    /// <summary>Text UI for displaying the selected relationship ID.</summary>
    [SerializeField]
    private TextMeshProUGUI mappingHeaderName;

    /// <summary>Text UI for displaying the selected relationship description.</summary>
    [SerializeField]
    private TextMeshProUGUI mappingHeaderDescription;

    /// <summary>InputField for specifying the delay before min/max sampling.</summary>
    [SerializeField]
    private TMP_InputField delayField;

    /// <summary>Button to initiate sampling the current value as the 'Min' bound.</summary>
    [SerializeField]
    private Button setMinButton;

    /// <summary>Button to initiate sampling the current value as the 'Max' bound.</summary>
    [SerializeField]
    private Button setMaxButton;

    /// <summary>InputField to manually enter a minimum bound value.</summary>
    [SerializeField]
    private TMP_InputField manualMinField;

    /// <summary>InputField to manually enter a maximum bound value.</summary>
    [SerializeField]
    private TMP_InputField manualMaxField;

    /// <summary>Dropdown for selecting a predefined curve type.</summary>
    [SerializeField]
    private TMP_Dropdown curveDropdown;

    /// <summary>Button to apply all edits to the relationship.</summary>
    [SerializeField]
    private Button applyMappingButton;

    /// <summary>Button to cancel edits and revert to original values.</summary>
    [SerializeField]
    private Button cancelMappingButton;

    /// <summary>Text UI for displaying the selected Metric Type.</summary>
    [SerializeField]
    private TextMeshProUGUI metricType;
    
    /// <summary>Text UI for displaying the selected Metric Description.</summary>
    [SerializeField]
    private TextMeshProUGUI metricDescription;
    
    
    
    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region Private State

    private LemmingUIRelationshipDataRow    _selectedRow;
    private LemmingRelationshipInfo         _originalInfo;
    private LemmingRelationshipInfo         _editingInfo;

    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region Unity Lifecycle



    /// <summary>
    /// Initializes UI lists and hooks up control pane button callbacks.
    /// </summary>
    private void Start()
    {
        
        // Populate control pane lists from Shepherd
        PopulateRelationships();
        Debug.Log($"[UI] Loaded {LemmingShepherd.Instance.RelationshipDetails.Count} relationships and " +
                  $"{LemmingShepherd.Instance.ValidLemmings.Count()} lemmings.");
        PopulateLemmings();

        // populate the dropdown once up front
        PopulateCurveDropdown();
        
        MappingPaneSubscriptions();
        
    }
        
        
    //_______________  METHOD BREAK   ___________________//


    /// <summary>
    /// Subscribes UI events and initializes the first refresh.
    /// </summary>
    public void MappingPaneSubscriptions()
    {
        manualMinField.onEndEdit.AddListener(_ => RefreshMappingPane());
        manualMaxField.onEndEdit.AddListener(_ => RefreshMappingPane());
        curveDropdown.onValueChanged.AddListener(_ => RefreshMappingPane());
        
        // Hook mapping pane buttons
        setMinButton.onClick.AddListener(() => StartCoroutine(DelayedSample(true)));
        setMaxButton.onClick.AddListener(() => StartCoroutine(DelayedSample(false)));
        applyMappingButton.onClick.AddListener(OnApplyMappingClicked);
        cancelMappingButton.onClick.AddListener(OnCancelMappingClicked);

        // Disable until a relationship is selected
        applyMappingButton.interactable = false;
        cancelMappingButton.interactable = false;
    }
    
    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region Control Pane Population

    /// <summary>
    /// Instantiates and binds each relationship row based on <see cref="LemmingShepherd.ValidRelationships"/>.
    /// </summary>
    private void PopulateRelationships()
    {
        if (relationshipDataPrefab == null) {
            Debug.LogError("[UI] relationshipDataPrefab is _not_ assigned!", this);
            return;
        }
        if (relationshipContent == null) {
            Debug.LogError("[UI] relationshipContent is _not_ assigned!", this);
            return;
        }
        // clear…
        foreach (Transform t in relationshipContent)
            Destroy(t.gameObject);

        foreach (var (relSO, info) in LemmingShepherd.Instance.ValidRelationships)
        {
            if (relSO == null) continue;
            Debug.Log($"[PopulateRelationships] Instantiating row for `{relSO.name}` (ID: {info.Id})");

            // instantiate _directly_ from the component prefab:
            var row = Instantiate(relationshipDataPrefab, relationshipContent);
            if (row == null) {
                Debug.LogError("[UI] Failed to instantiate relationshipDataPrefab!", this);
                continue;
            }
            //Debug.Log($"Creating info for `{info.id}`: over={info.over}, under={info.under}, inRange={info.inRange}");

            
            // wrap Bind in try/catch so we get a stack-trace instead of a hard crash
            try {
                row.Bind(info);
                row.onRemapClicked += OnRelationshipSelected;
            }
            
            catch (Exception e) {
                Debug.LogException(e, row.gameObject);
            }
            
        }
    }

        
        
    //_______________  METHOD BREAK   ___________________//


    /// <summary>
    /// Instantiates and binds each lemming row based on <see cref="LemmingShepherd.ValidLemmings"/>.
    /// </summary>
    private void PopulateLemmings()
    {
        foreach (Transform t in lemmingContent)
            Destroy(t.gameObject);

        foreach (var lemming in Lemmings.LemmingShepherd.Instance.ValidLemmings)
        {
            //Debug.Log(lemming);
            var reference = new LemmingReference
            {
                name       = lemming.Name,
                Source     = lemming.gameObject,
                confidence = lemming.Confidence
            };
            var row = Instantiate(lemmingDataPrefab, lemmingContent);
            row.Bind(reference);
        }
    }

    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region Mapping Pane Binding

    /// <summary>
    /// Called when a relationship row's 'Remap' button is clicked.
    /// Loads the selected relationship into the mapping pane for editing.
    /// </summary>
    /// <param name="row">The selected relationship row.</param>
    private void OnRelationshipSelected(LemmingUIRelationshipDataRow row)
    {
        _selectedRow  = row;
        _originalInfo = row.RelationshipInfo;
        _editingInfo  = _originalInfo;

        // Header fields
        mappingHeaderName.text        = _editingInfo.Id;
        mappingHeaderDescription.text = _editingInfo.Description;
        manualMinField.text           = _editingInfo.Min .ToString("F2");
        manualMaxField.text           = _editingInfo.Max .ToString("F2");
        curveDropdown.value           = (int)_editingInfo.CurveType;

        // Pull in starting values
        RefreshMappingPane();
        
        // Metric Data
        metricType.text = _editingInfo.MetricName;
        metricDescription.text = _editingInfo.MetricDescription;
        // Dynamic lists
        PopulateMetricOptions();
        PopulateLemmingRemappingOptions();

        applyMappingButton.interactable = true;
        cancelMappingButton.interactable = true;
    }
            
        
    //_______________  METHOD BREAK   ___________________//


    /// <summary>
    /// Reads all remapping inputs, performs a live <see cref="RelationshipPreview"/>,
    /// and writes every output label in one cohesive pass.
    /// </summary>
    private void RefreshMappingPane()
    {
        // Parse and apply manual bounds
        if (float.TryParse(manualMinField.text, out var min))
            _editingInfo.Min = min;
        if (float.TryParse(manualMaxField.text, out var max))
            _editingInfo.Max = max;

        // Apply chosen curve
        _editingInfo.CurveType = (LemmingCurveType)curveDropdown.value;

        // Update header labels
        mappingHeaderName.text        = _editingInfo.Id;
        mappingHeaderDescription.text = _editingInfo.Description;
        metricType.text               = _editingInfo.MetricName;
        metricDescription.text        = _editingInfo.MetricDescription;

        // Compute lightweight preview
        var preview = _editingInfo.Preview();

        // Populate result fields
        remappingOutput.text = preview.Raw?.ToString() ?? "<null>";
        remappingNormal.text = preview.Normalized.ToString("F2");
        remappingCurve.text  = preview.Curved.ToString("F2");
        remappingStatus.text = preview.InRange ? "In Range"
            : preview.Over   ? "Over"
            : preview.Under  ? "Under"
            : "None";
        remappingNormal.text = preview.AsAxis.ToString("F2");
    }
    
    

    
    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region Metric Options (Stub)

    /// <summary>
    /// Populates stub metric-setting rows; replace with real data when ready.
    /// </summary>
    private void PopulateMetricOptions()
    {
        //TODO: This needs to be updated once the settings options have a clearer shape.
        //TODO: Right now I am not remapping the metric itself to protect the output and ensure it responds predicatably.
        //TODO: Improved UI control options will allow remapping the metric based on developer assigned constraints
        Debug.Log(metricOptionsContent.ToString());
        
        foreach (Transform t in metricOptionsContent)
            Destroy(t.gameObject);

        var metricSettings = _editingInfo.Settings.ToMetricSettingsList(_editingInfo.FamilyType, _editingInfo.Metric);
        
        Debug.Log($"[ToMetricSettingsList] Generated {metricSettings.Count} settings for ");
        
        foreach (var setting in metricSettings)
        {
            Debug.Log(setting.ToString());
            var opt = Instantiate(metricPrefab, metricOptionsContent);
            opt.Bind(setting);
            opt.onSettingChanged += updated =>
            {
                ApplyMetricSetting(_editingInfo.Settings, updated);
                RefreshMappingPane(); // Update preview after each change
            };
        }
        
    }

    private void ApplyMetricSetting(LemmingRelationSetting settings, MetricSettingInfo updated)
{
    switch (updated.displayName)
    {
        // ───── Floats / Sliders ─────
        case "Threshold":
            settings.threshold = updated.floatValue;
            break;

        // ───── Booleans / Toggles ─────
        case "Use Single Axis":
            settings.useSingleAxis = updated.boolValue;
            break;
        case "Relative to Object":
            settings.relativeToObject = updated.boolValue;
            break;
        case "Use Gaze From Proxy":
            settings.useGazeFromProxy = updated.boolValue;
            break;
        case "Magnitude Only":
            settings.magnitudeOnly = updated.boolValue;
            break;
        case "Direction Only":
            settings.directionOnly = updated.boolValue;
            break;
        case "Relative to Members":
            settings.relativeToMembers = updated.boolValue;
            break;
        case "Invert":
            settings.invert = updated.boolValue;
            break;

        // ───── Enums / Dropdowns ─────
        case "Axis":
            settings.singleAxis = (SingleAxis)updated.selectedIndex;
            break;
        case "Axis Selection":
            settings.axisSelection = (AxisSelection)updated.selectedIndex;
            break;
        case "Axis Logic":
            settings.axisSelectionThrouple = (AxisSelectionThrouple)updated.selectedIndex;
            break;
        case "Distance Unit":
            settings.distanceUnit = (DistanceUnit)updated.selectedIndex;
            break;
        case "Distance Options":
            settings.distanceOptions = (DistanceOptions)updated.selectedIndex;
            break;
        case "Density Method":
            settings.densityMethod = (DensityMethod)updated.selectedIndex;
            break;
        case "Size Method":
            settings.sizeMethod = (SizeMethod)updated.selectedIndex;
            break;

        // ───── Vector3 Pickers ─────
        case "Rotation Axis":
            settings.rotationAxis = updated.vector3Value;
            break;
        case "Reference Vector":
            settings.referenceVector = updated.vector3Value;
            break;

        // ───── Object Pickers ─────
        case "Reference Object":
            settings.objectToReference = updated.objectReference as GameObject;
            break;
        case "Proxy":
            settings.proxy = updated.objectReference as LemmingRelationshipProxy;
            break;

        default:
            Debug.LogWarning($"[ApplyMetricSetting] Unhandled setting: {updated.displayName}");
            break;
    }
    
    
}
    
    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region Role Remapping Options

    /// <summary>
    /// Populate the curve‐type dropdown with all the names in LemmingCurveType.
    /// </summary>
    private void PopulateCurveDropdown()
    {
        // 1) Clear any old entries
        curveDropdown.ClearOptions();

        // 2) Grab all enum names (e.g. "Linear", "EaseIn", etc)
        var names = Enum.GetNames(typeof(LemmingCurveType)).ToList();

        // 3) Add them as dropdown options
        curveDropdown.AddOptions(names);

        // 4) Optionally force the visual to update
        curveDropdown.RefreshShownValue();
    }
            
        
    //_______________  METHOD BREAK   ___________________//


    /// <summary>
    /// Populates role-mapping rows, limiting choices to the current herd snapshot.
    /// </summary>
    private void PopulateLemmingRemappingOptions()
    {
        foreach (Transform t in remappingLemmingContent)
            Destroy(t.gameObject);
        
        foreach (var (currentName, role) in _editingInfo.Members)
        {
            var row = Instantiate(lemmingRemappingPrefab, remappingLemmingContent);
            row.Bind(
                _editingInfo,
                (currentName, role),
                (r, newName) => {
                    // update the struct, not the SO
                    for (int i = 0; i < _editingInfo.Members.Count; i++)
                    {
                        if (_editingInfo.Members[i].Role == r)
                        {
                            _editingInfo.Members[i] = (newName, r);
                            break;
                        }
                    }
                }
            );
            row.onRoleChanged += HandleRoleChanged;
        }
    }
        
        
    //_______________  METHOD BREAK   ___________________//


    /// <summary>
    /// Handles when a user changes the assigned lemming for a given role.
    /// Updates the local editing info accordingly.
    /// </summary>
    /// <param name="role">Role label (e.g. "Leader").</param>
    /// <param name="newName">New lemming name selected by the user.</param>
    private void HandleRoleChanged(string role, string newName)
    {
        // find the index of the role we just changed
        int idx = _editingInfo.Members.FindIndex(m => m.Role == role);
        if (idx < 0) return;

        // update the (name, role) tuple
        _editingInfo.Members[idx] = (newName, role);

        // now also update the References list
        var maybeRef = _editingInfo
            .Relationship   // your SO
            .Herd           // LemmingHerdSnapshot
            .FindReference(newName);
        if (maybeRef.HasValue)
            _editingInfo.References[idx] = maybeRef.Value;

        // finally, re-run your preview
        RefreshMappingPane();
 
    }

    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region Apply/Cancel & Sampling

    /// <summary>
    /// Commits the current <c>_editingInfo</c> back to the underlying relationship
    /// and disables the Apply/Cancel buttons.
    /// </summary>
    private void OnApplyMappingClicked()
    {
        // 1) Push your edits into the SO and clear all caches
        _editingInfo.Relationship.UpdateFromInfo(_editingInfo);
        
        //TODO: Ensure that this is impacting the correct cached structs I think it only needs to happen once since they both reference the same relationship
        _editingInfo.Relationship.InvalidateCache();
        _originalInfo.Relationship.InvalidateCache();

        // 2) Fetch the freshly-computed info from your Shepherd’s cache
        var newInfo = LemmingShepherd.Instance.RelationshipDetails[_editingInfo.Relationship];

        // 3) Tell the row to rebind itself to the updated info
        _selectedRow.Bind(newInfo);
        
        applyMappingButton.interactable  = false;
        cancelMappingButton.interactable = false;
    }
        
        
    //_______________  METHOD BREAK   ___________________//


    /// <summary>
    /// Cancels edits and reloads the original settings into the UI.
    /// </summary>
    private void OnCancelMappingClicked()
    {
        OnRelationshipSelected(_selectedRow);
        RefreshMappingPane();
    }
        
        
    //_______________  METHOD BREAK   ___________________//


    /// <summary>
    /// Waits <c>delayField</c> seconds then samples the raw value for min/max binding.
    /// </summary>
    /// <param name="isMin">If true, binds to min; otherwise binds to max.</param>
    private IEnumerator DelayedSample(bool isMin)
    {
        if (!float.TryParse(delayField.text, out float delay))
            delay = 0f;
        yield return new WaitForSeconds(delay);

        //TODO: Ensure that the raw value has a configured conversion to float for every Metric
        
        
        var rawObj = _editingInfo.Relationship.MyRelation.Evaluate();
        float raw  = rawObj is float f ? f : 0f;

        if (isMin)
        {
            _editingInfo.Min            = raw;
            manualMinField.text        = raw.ToString("F2");
        }
        else
        {
            _editingInfo.Max            = raw;
            manualMaxField.text        = raw.ToString("F2");
        }
        
        RefreshMappingPane();
    }

    #endregion
}
