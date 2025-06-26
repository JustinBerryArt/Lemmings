using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lemmings
{
    /// <summary>
    /// Defines a relationship among one or more Lemmings.
    /// Can be used to calculate positions, rotations, and derived values.
    /// </summary>

    // Asset creation moved from the Assets menu to the Lemmings Menu - Find 'CreateRelationship()' in Lemming Relationship Editor Script 
    //[CreateAssetMenu(fileName = "new LemmingRelationship", menuName = "Lemmings/Lemming Relationship")]
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public class LemmingRelationship : ScriptableObject, ILemmingRelationship
    {

        
        #region Identity Declarations

        [Header("➤ User Facing and Identity Settings")]
        /// <summary>
        /// This section is to create the unique identifier for the relationship.
        /// (example) "Make Selection"
        /// </summary>
        [Tooltip("Unique identifier for this relationship (e.g., 'distance_leader_follower')")]
        public string ID;

        /// <summary>
        /// The description will automatically map to the User Interface by default and so it can be used to provide
        /// valuable information about the functionality to users that might be interested in remapping it.
        /// In many ways this is about establishing a place to communicate the goal of a relationship internally but primarilly for users.
        /// (example)
        /// "This relationship is used to select items in the scene triggers when the value is smaller than the minimum threshold.
        /// By default it is set up as a finger pinch, so that if your index finger and thumb are very close to each other, you select an item.
        /// You can remap it any way that you like, but be mindful that it is something that provides an on or off result,
        /// so some metrics will be more reliable than others."
        /// </summary>
        [TextArea]
        [Tooltip("Optional description for this relationship's purpose or logic.")]
        public string description;

        [Header("➤ Input System Settings")]
        
        /// <summary>
        /// Lemmings includes a virtual input device. If you want to use unity's input system and access events through it, this needs to be true.
        /// </summary>
        [Tooltip("Click this if you want to access relationship values in the input manager.")]
        public bool useInputSystem;
        
        
        #endregion
        
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------


        #region Inspector Declarations

        [Header("➤ Core Configuration")]
        
        /// <summary>
        /// The Herd Snapshot is a scriptable object (asset) that acts as the ground truth for which lemmings are available
        /// to be a part of a specific relationship and to populate the enums that allow selecting options.
        /// The Snapshot also have functions to help locate and resolve game objects.
        /// </summary>
        [Tooltip("Snapshot used to resolve lemming names into GameObjects.")]
        public LemmingHerdSnapshot Herd;

        /// <summary>
        /// This enum sets the type of family this relationship embodies. Essentially determining the number of members included.
        /// </summary>
        [Tooltip("Specifies the family structure (single, couple, group, etc) which determines the number of Lemmings to include in the relationship.")]
        public FamilyType familyType = FamilyType.Single;
        
        /// <summary>
        /// This is the mode switcher that changes the enum of available options based on the family type.
        /// It is boxed as an object so that it can be safely called and implemented while retaining flexibility.
        /// </summary>
        public object ActiveMode =>
            familyType switch
            {
                FamilyType.Single => singleMetric,
                FamilyType.Couple => coupleMetric,
                FamilyType.Throuple => throupleMetric,
                FamilyType.Group => groupMetric,
                _ => null
            };
        
        // These are the enums that are alternated between for defining Metrics from various family types
        [HideInInspector] public SingleMetric singleMetric;
        [HideInInspector] public CoupleMetric coupleMetric;
        [HideInInspector] public ThroupleMetric throupleMetric;
        [HideInInspector] public GroupMetric groupMetric;
        
        /// <summary>
        /// Each relationship has a minimum and maximum threshold for establishing Normalization and its boolean status
        /// This is the minimum value
        /// </summary>
        [Tooltip("Minimum bound used for converters or normalization.")]
        public float min = 0f;

        
        /// <summary>
        /// Each relationship has a minimum and maximum threshold for establishing Normalization and its boolean status
        /// This is the maximum value
        /// </summary>
        [Tooltip("Maximum bound used for converters or normalization.")]
        public float max = 1f;
        

        /// <summary>
        /// Enum specifying the type of curve to apply to this relationship's output.
        /// If set to 'Custom', the user-defined AnimationCurve will be used instead.
        /// </summary>
        [HideInInspector]
        [Tooltip("Choose a predefined curve to shape the output, or use 'Custom' to specify your own.")]
        public LemmingCurveType curveType = LemmingCurveType.Linear;
        

        /// <summary>
        /// The custom curve used when curveType is set to 'Custom'.
        /// This curve will be applied to the normalized value before output.
        /// </summary>
        [HideInInspector]
        [Tooltip("Editable curve used only when curveType is set to 'Custom'.")]
        public AnimationCurve customCurve = AnimationCurve.Linear(0, 0, 1, 1);
        
        
        /// <summary>
        /// This hold the current metric being used and its associated family type
        /// </summary>
        [Tooltip("Select the primary metric used to evaluate this relationship.")]
        public Enum Metric => (Enum)ActiveMode;
        
        
        //TODO: Consider whether this should remain in its current form or be adapted to create or establish a proxy from the Scriptable Object
        //TODO: Ensure that this does not limit proxys to a single one per relationship, as use case might exist for multiple.
        
        public LemmingRelationshipProxy proxy;
                
                
        #endregion
        
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------


        #region Public Accessors for External Scripts
        
        [SerializeField] private LemmingRelationSetting _settings;

        public LemmingRelationSetting Settings => _settings;
        /// <summary>
        /// The derived datum based on the relationship logic.
        /// </summary>
        public LemmingDatum Datum => SetValue();
                
        
        /// <summary>
        /// Number of valid members in the relationship.
        /// </summary>
        public int Count => Members.Count;
        
        
        /// <summary>
        /// Public access to the Min. Set ensures that the value is less than Max.
        /// </summary>
        public float Min
        {
            get => min;
            set
            {
                min = value;
                if (min >= max)
                {
                    max = min + 0.01f;
                }    
                InvalidateCache();
            }
        }
        

        /// <summary>
        /// Public access to the Max. Set ensures that the value is greater than Min.
        /// </summary>
        public float Max
        {
            get => max;
            set
            {
                max = value;
                if (max <= min)
                {
                    min = max - 0.01f;
                }
                InvalidateCache();
            }
        }
                
        /// <summary>
        /// Read-only accessor for the selected Curve Type
        /// </summary>
        public LemmingCurveType CurveType => curveType;
                
        /// <summary>
        /// This is the read-only pull for the active Curve, the active curve is selected from the enum 'curveType'.
        /// Most of the curves are predefined, except for the custom curve, which is draw in the inspector.
        /// </summary>
        public AnimationCurve Curve => GetActiveCurve();

        /// <summary>
        /// This is the read only version of the custom curve that users draw in the inspector
        /// </summary>
        public AnimationCurve CustomCurve => customCurve;
                
        /// <summary>
        /// This is the Read-Only value for the boolean state of the relationship, showing if it is over, under, or within its min/max range
        /// </summary>
        public RelationshipStatus Status
        {
            get
            {
                var converter = Converter;
                if (converter == null) return RelationshipStatus.None;

                return converter.Over && !converter.Under && !converter.InRange ? RelationshipStatus.Over :
                    !converter.Over && converter.Under && !converter.InRange ? RelationshipStatus.Under :
                    !converter.Over && !converter.Under && converter.InRange ? RelationshipStatus.InRange :
                    RelationshipStatus.None;
            }
        }
        
        /// <summary>
        /// This is the Info struct for the relationship that holds its important data.
        /// Developers can call for any relevant relationship information from this and it should provide it at minimal compute cost.
        /// The idea is for the information to mostly live in a cached state, with only the dynamic values updating per frame.
        /// </summary>
        public LemmingRelationshipInfo Info
        {
            get
            {
                // If the cached has been invalidated by a change in settings, or does not exist, this rebuilds it
                EnsureInitialization();
                // refresh only the dynamic bits when a user calls for data from the struct
                _cachedInfo.RefreshDynamic();

                CompareStatus();
                
                // return a _copy_ of the cached struct so callers can’t mutate it directly
                return _cachedInfo;
            }
        }
                
        
        // public accessor for the full list of Lemmings
        public List<GameObject> Members
        {
            get
            {
                // build once if null
                if (_membersCache == null)
                {
                    _membersCache = new List<GameObject>(selectedReferences.Count);
                    foreach (var r in selectedReferences)
                        if (r.Source != null)
                            _membersCache.Add(r.Source);
                }
                return _membersCache;
            }
        }
                
        
        /// <summary>
        /// This is the authoritative list of Lemmings,
        /// includes all the fallback support and metadata through the reference struct
        /// </summary>
        public List<LemmingReference> References => selectedReferences;
                
                
        /// <summary>
        /// This is the public reference to a copy of the cached info.
        /// This can be used to pull the info struct out of a relationship and use it elsewhere to make modifications that do not immediately update the original
        /// </summary>
        public ref LemmingRelationshipInfo CachedInfo
        {
            get
            {
                // If the cached has been invalidated by a change in settings, or does not exist, this rebuilds it
                EnsureInitialization();
                return ref _cachedInfo;
            }
        }
                
        /// <summary>
        /// This is a publicly accessible list to access the Lemming Roles and Pairs
        /// </summary>
        /// <returns> List of the Lemming Name and its role in the relationship</returns>
        public List<(string LemmingName, string Role)> GetRoleLemmingPairs()
        {
            var list = new List<(string, string)>();
            for (int i = 0; i < selectedNames.Count; i++)
            {
                string role = familyType switch
                {
                    FamilyType.Single   => "Lemming",
                    FamilyType.Couple   => i == 0 ? "Leader" : "Follower",
                    FamilyType.Throuple => new[] { "Leader", "Follower", "Third" }[i],
                    FamilyType.Group    => $"Member {i + 1}",
                    _                   => $"Member {i + 1}"
                };
                list.Add((selectedNames[i], role));
            }
            return list;
        }

        // convenience property that calls the same thing:
        public ILemmingConverter Converter
            => ToConverter();

        // for checking the data type used by the datum
        public LemmingValueType Type => Datum.Type;

        public FamilyType Family
        {
            get => familyType;
            set
            {
                if (familyType == value) return;
                familyType = value;
                InvalidateCache();
            }
        }
                
        /// <summary>
        /// This initiates the Lemming struct for the appropriate family type and relevant enum.
        /// Depending on the number of lemmings in a relationship, the metrics are calculated differently.
        /// This ensures that the correct metric options are listed for the number of tracked items.
        /// </summary>
        public LemmingRelation MyRelation
        {
            get
            {
                return familyType switch
                {
                    FamilyType.Single => new LemmingRelation(familyType, References, singleMetric, Settings),
                    FamilyType.Couple => new LemmingRelation(familyType, References, coupleMetric, Settings),
                    FamilyType.Throuple => new LemmingRelation(familyType, References, throupleMetric, Settings),
                    FamilyType.Group => new LemmingRelation(familyType, References, groupMetric, Settings),
                    _ => new LemmingRelation(familyType, References, singleMetric, Settings)
                };
            }
        }
        
        
        #endregion
        
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------


        #region Private and internal references

        /// <summary>
        /// Cache for detecting changes in family type
        /// </summary>
        private FamilyType _lastFamilyType;
        
        /// <summary>
        /// Cache for detecting changes in metric type
        /// </summary>
        private Enum _lastMetric;
        
        /// <summary>
        /// This is the info struct for the relationship that is cached in order to be more lightweight.
        /// </summary>
        [NonSerialized] 
        private LemmingRelationshipInfo _cachedInfo;
        
        
        /// <summary>
        /// This is the boolean flag used to determine whether or not the cache needs to be rebuilt.
        /// Changes made to the relationship settings should set this to false using <see cref="InvalidateCache"/>
        /// </summary>
        [NonSerialized]
        private bool _hasInitializedCache = false;
        
        
        // cached backing field for defining the relationship members
        private List<GameObject> _membersCache;

        
        /// <summary>
        /// This dictionary is used to confirm and hold the role of a particular lemming in a relationship.
        /// Used for display purposes.
        /// </summary>
        public Dictionary<string, string> roleToName = new();
        
        
        /// <summary>
        /// This dictionary is used to associate the specific Lemming Reference to the role that it plays in the relationship
        /// </summary>
        public Dictionary<string, LemmingReference> roleToReference = new();
        
        
        /// <summary>
        /// This is used to define the Converter, which can be called to make data conversions for flexible output.
        /// example: [relationship].Converter.ToIntRange(0, 20)
        /// </summary>
        public ILemmingConverter ToConverter()
            => Datum.GetConverter(Min, Max) as ILemmingConverter;
        
        
        /// <summary>
        /// Used to confirm if the herd snapshot has been altered
        /// </summary>
        private LemmingHerdSnapshot lastHerd;
        
        
        /// <summary>
        /// Used to hold the translation from the string value of the lemming selected into a more stable Lemming Reference
        /// </summary>
        [HideInInspector] public List<LemmingReference> selectedReferences = new();
        
        
        /// <summary>
        /// The enum used to select which lemmings to use gets converted into a string so that it can be more broadly used.
        /// This holds those strings in a list so that they can be converted into Lemming References
        /// </summary>
        [HideInInspector] public List<string> selectedNames = new();

        #endregion
        
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------


        #region Event Handlers


        /// <summary>
        /// This is called when the Info Struct is updated.
        /// </summary>
        public event Action Updated = ()=>{};
        
        /// <summary>
        /// Fired every time SetValue is called and the Datum is refreshed.
        /// Subscribers can pull Raw/Normalized/Curved/Over/Under/InRange
        /// from the new LemmingDatum.
        /// </summary>
        public event Action<LemmingDatum> OnDatumUpdated;

        private RelationshipStatus _lastStatus;
        
        public event Action OnUnder;
        public event Action OnOver;
        public event Action OnInRange;
        
        #endregion        
        
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------


        #region Output Generation
        

        /// <summary>
        /// This sets the Datum, which holds the type-agnostic value for this relationship.
        /// You can override this to set the value through another means
        /// </summary>
        public LemmingDatum SetValue()
        {
            LemmingDatum datum;
            if (Members.Count > 0)
                // my relation is the struct, holding the selected options, used to evaluate the datum value
                datum = MyRelation.ToDatum();
            else
                datum = new LemmingDatum();

            // notify anyone listening
            OnDatumUpdated?.Invoke(datum);
            return datum;
        }

        
        #endregion
        
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------


        #region Info Struct Generation and Utility Methods Related to Info Struct

        private LemmingRelationshipInfo DefineInfo()
        {
            var datum      = SetValue();
            var converter  = Converter;

            return new LemmingRelationshipInfo
            {
                // Identity
                Relationship       = this,
                Id                 = ID,
                Description        = description,

                // Settings
                FamilyType         = familyType,
                Herd               = Herd,
                Metric             = Metric,
                MetricName         = Metric.ToString(),
                Settings           = Settings,   
                MetricDescription  = GetMetricDescription(),
                Members            = GetRoleLemmingPairs(),
                References         = References,
                Min                = Min,
                Max                = Max,
                CurveType          = CurveType,
                CustomCurve        = CustomCurve,
                UseCustomCurve     = CurveType == LemmingCurveType.Custom,

                // Values
                RawValue           = converter.Raw,
                
                NormalizedValue    = converter.Normalized,
                CurvedValue        = converter.ToCurve(Curve),
                Status             = Status,
                Over               = converter.Over,
                Under              = converter.Under,
                InRange            = converter.InRange,
                AsAxis             = converter.AsAxis,
                
                // Utility
                ValueType          = datum.Type,
                MetricSettings     = default,
                MetricOptions      = default,
                Relation           = MyRelation,
                Converter          = converter,
                Datum              = datum
            };
        }
        
        
        //_______________  METHOD BREAK   ___________________//
        
        
        /// <summary>
        /// This is called in the formation of the Info struct.
        /// It makes sure that the cached info has been built.
        /// </summary>
        private void EnsureInitialization()
        {
            if (!_hasInitializedCache)
            {
                _cachedInfo = DefineInfo();  // full build one time
                _hasInitializedCache = true;
                _lastStatus = RelationshipStatus.None;
            }
        }
        
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Whenever changes are made to the relationship, such as OnValidation (for the inspector), in the Lemming Shepherd, or the UI, this needs to be called.
        /// This sets it up so that the entire cached Info struct is rebuilt and not simply updated with dynamic values.
        /// </summary>
        public void InvalidateCache()
        {
            _hasInitializedCache = false;
            _lastStatus = RelationshipStatus.None;
        }

        
        //_______________  METHOD BREAK   ___________________//


        public void UpdateFromInfo(LemmingRelationshipInfo info)
        {
            InvalidateCache();
            
            // 1) Bounds & curve
            Min         = info.Min;
            Max         = info.Max;
            curveType   = info.CurveType;
            customCurve = info.CustomCurve;

            // 2) Metric enum: clear all then set the one for our FamilyType
            singleMetric   = default;
            coupleMetric   = default;
            throupleMetric = default;
            groupMetric    = default;

            switch (Family)
            {
                case FamilyType.Single when info.Metric is SingleMetric sm:
                    singleMetric = sm;
                    break;
                case FamilyType.Couple when info.Metric is CoupleMetric cm:
                    coupleMetric = cm;
                    break;
                case FamilyType.Throuple when info.Metric is ThroupleMetric tm:
                    throupleMetric = tm;
                    break;
                case FamilyType.Group when info.Metric is GroupMetric gm:
                    groupMetric = gm;
                    break;
                default:
                    Debug.LogWarning($"[{name}] Tried to apply Metric {info.Metric} to FamilyType {Family}");
                    break;
            }
            _settings = info.Settings;
            
            // 3) Member → names & role map → references
            selectedNames.Clear();
            roleToName.Clear();
            foreach (var (lemmingName, role) in info.Members)
            {
                selectedNames.Add(lemmingName);
                roleToName[role] = lemmingName;
            }
            // rebuild the LemmingReference list from those names
            SyncReferencesFromNames();

            // 4) notify any watchers/UI to redraw
            Updated?.Invoke();
            OnDatumUpdated?.Invoke( SetValue() );

            // 5) clear any member‐cache so Members will re-read the new refs
            _membersCache = null;

        }

        #endregion
        
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------


        #region Validation and Utility Methods

        
        
        public void OnMetricChanged()
        {
            SetDefaultRelationSettings(_settings);
            
            /*
            // Create fresh settings
            var newSettings = new LemmingRelationSetting();
      
            // Pull from old _settings only for fields relevant to the new metric
            RehydrateSettings(newSettings, _settings, Family, Metric);

            // Replace
            _settings = newSettings;
            */
        }

        private void SetDefaultRelationSettings(LemmingRelationSetting settings)
        {
            // References
            if (proxy != null)
            settings.proxy = proxy;
            else settings.proxy = proxy;
            settings.objectToReference = null;

            // Booleans
            settings.relativeToObject = false;
            settings.useGazeFromProxy = false;
            settings.magnitudeOnly = false;
            settings.directionOnly = false;
            settings.relativeToMembers = false;
            settings.invert = false;
            settings.useSingleAxis = false;

            // Enums
            settings.axisSelection = AxisSelection.Up;
            settings.axisSelectionThrouple = AxisSelectionThrouple.CenterThroughLeader;
            settings.distanceOptions = DistanceOptions.CombinedAsVector3;
            settings.distanceUnit = DistanceUnit.Centimeters;
            settings.densityMethod = DensityMethod.AverageFromCenter;
            settings.sizeMethod = SizeMethod.UseHeightWidthDepth;
            settings.singleAxis = SingleAxis.Y;

            // Value types
            settings.threshold = 0f;
            settings.referenceVector = Vector3.zero;
            settings.rotationAxis = Vector3.zero;
        }


        /// <summary>
/// Copies only the relevant fields from a previous <see cref="LemmingRelationSetting"/>
/// into a freshly created one, based on the currently selected <paramref name="family"/> and <paramref name="metric"/>.
/// This ensures that only appropriate values are preserved while eliminating stale data from earlier metrics.
/// </summary>
/// <param name="fresh">A newly created, empty settings instance to populate.</param>
/// <param name="prior">The existing settings object that may contain user-authored values.</param>
/// <param name="family">The family type of the relationship (e.g., Single, Couple, Throuple, Group).</param>
/// <param name="metric">The selected metric for which relevant settings should be restored.</param>
private void RehydrateSettings(LemmingRelationSetting fresh, LemmingRelationSetting prior, FamilyType family, Enum metric)
{
    //LemmingRelationSetting temp = new LemmingRelationSetting();
    
    switch (family)
    {
        case FamilyType.Single:
            switch ((SingleMetric)metric)
            {
                case SingleMetric.Position:
                    fresh.useSingleAxis = prior.useSingleAxis;
                    fresh.singleAxis = prior.singleAxis;
                    break;

                case SingleMetric.Movement:
                    fresh.useSingleAxis = prior.useSingleAxis;
                    fresh.singleAxis = prior.singleAxis;
                    fresh.magnitudeOnly = prior.magnitudeOnly;
                    fresh.relativeToObject = prior.relativeToObject;
                    fresh.objectToReference = prior.objectToReference;
                    fresh.directionOnly = prior.directionOnly;
                    break;

                case SingleMetric.Rotation:
                    fresh.useGazeFromProxy = prior.useGazeFromProxy;
                    fresh.threshold = prior.threshold;
                    fresh.proxy = prior.proxy;
                    break;

                case SingleMetric.Trigger:
                    fresh.proxy = prior.proxy;
                    break;
            }
            break;

        case FamilyType.Couple:
            switch ((CoupleMetric)metric)
            {
                case CoupleMetric.Position:
                case CoupleMetric.Difference:
                    fresh.useSingleAxis = prior.useSingleAxis;
                    fresh.singleAxis = prior.singleAxis;
                    break;

                case CoupleMetric.Rotation:
                    fresh.axisSelection = prior.axisSelection;
                    fresh.rotationAxis = prior.rotationAxis;
                    fresh.invert = prior.invert;
                    fresh.objectToReference = prior.objectToReference;
                    break;

                case CoupleMetric.Movement:
                    fresh.magnitudeOnly = prior.magnitudeOnly;
                    fresh.useSingleAxis = prior.useSingleAxis;
                    fresh.singleAxis = prior.singleAxis;
                    fresh.relativeToObject = prior.relativeToObject;
                    fresh.objectToReference = prior.objectToReference;
                    fresh.relativeToMembers = prior.relativeToMembers;
                    fresh.directionOnly = prior.directionOnly;
                    break;

                case CoupleMetric.Distance:
                    fresh.distanceUnit = prior.distanceUnit;
                    break;

                case CoupleMetric.Trigger:
                    fresh.proxy = prior.proxy;
                    break;
            }
            break;

        case FamilyType.Throuple:
            switch ((ThroupleMetric)metric)
            {
                case ThroupleMetric.Position:
                case ThroupleMetric.Angle:
                case ThroupleMetric.Size:
                    fresh.useSingleAxis = prior.useSingleAxis;
                    fresh.singleAxis = prior.singleAxis;
                    break;

                case ThroupleMetric.Rotation:
                    fresh.axisSelectionThrouple = prior.axisSelectionThrouple;
                    fresh.invert = prior.invert;
                    break;

                case ThroupleMetric.Movement:
                    fresh.relativeToMembers = prior.relativeToMembers;
                    fresh.magnitudeOnly = prior.magnitudeOnly;
                    fresh.useSingleAxis = prior.useSingleAxis;
                    fresh.singleAxis = prior.singleAxis;
                    break;

                case ThroupleMetric.Distance:
                    fresh.distanceOptions = prior.distanceOptions;
                    fresh.distanceUnit = prior.distanceUnit;
                    break;

                case ThroupleMetric.Density:
                    fresh.densityMethod = prior.densityMethod;
                    fresh.distanceUnit = prior.distanceUnit;
                    break;

                case ThroupleMetric.RotationAroundAxis:
                    fresh.axisSelection = prior.axisSelection;
                    fresh.rotationAxis = prior.rotationAxis;
                    fresh.axisSelectionThrouple = prior.axisSelectionThrouple;
                    fresh.objectToReference = prior.objectToReference;
                    fresh.invert = prior.invert;
                    break;

                case ThroupleMetric.Trigger:
                    fresh.proxy = prior.proxy;
                    break;
            }
            break;

        case FamilyType.Group:
            switch ((GroupMetric)metric)
            {
                case GroupMetric.Position:
                case GroupMetric.Size:
                    fresh.useSingleAxis = prior.useSingleAxis;
                    fresh.singleAxis = prior.singleAxis;
                    break;

                case GroupMetric.Rotation:
                    fresh.invert = prior.invert;
                    break;

                case GroupMetric.Movement:
                    fresh.relativeToMembers = prior.relativeToMembers;
                    fresh.magnitudeOnly = prior.magnitudeOnly;
                    fresh.useSingleAxis = prior.useSingleAxis;
                    fresh.singleAxis = prior.singleAxis;
                    fresh.relativeToObject = prior.relativeToObject;
                    fresh.objectToReference = prior.objectToReference;
                    fresh.directionOnly = prior.directionOnly;
                    break;

                case GroupMetric.Density:
                    fresh.densityMethod = prior.densityMethod;
                    fresh.distanceUnit = prior.distanceUnit;
                    break;

                case GroupMetric.RotationAroundAxis:
                    fresh.axisSelection = prior.axisSelection;
                    fresh.rotationAxis = prior.rotationAxis;
                    fresh.objectToReference = prior.objectToReference;
                    break;

                case GroupMetric.Trigger:
                    fresh.proxy = prior.proxy;
                    break;
            }
            break;
    }
}
        
        
        
        
        public void CompareStatus()
        {
            // Check for a status flip
            var newStatus = _cachedInfo.Status;
            if (newStatus != _lastStatus)
            {
                switch (newStatus)
                {
                    case RelationshipStatus.Under:    OnUnder?.Invoke();   break;
                    case RelationshipStatus.Over:     OnOver?.Invoke();    break;
                    case RelationshipStatus.InRange:  OnInRange?.Invoke(); break;
                }
                _lastStatus = newStatus;
            }
        }
        
        
        public string GetMetricDescription()
        {
            return LemmingMetricMetadata.GetDescription(Metric);
        }


        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Returns the appropriate AnimationCurve to use based on the selected curve type.
        /// If curveType is 'Custom', returns the user-defined curve.
        /// Otherwise, returns a predefined curve from the curve library.
        /// </summary>
        public AnimationCurve GetActiveCurve()
        {
            return curveType == LemmingCurveType.Custom
                ? customCurve
                : LemmingCurveLibrary.GetCurve(curveType);
        }
        
        
        //_______________  METHOD BREAK   ___________________//
        
        
        //TODO: Confirm that this is in active use and eliminate or adopt if not
        /// <summary>
        /// Syncs each role-to-name assignment with its resolved LemmingReference.
        /// </summary>
        public void SyncRolesFromHerd()
        {
            roleToReference.Clear();

            if (Herd == null)
                return;

            foreach (var keyValuePair in roleToName)
            {
                if (Herd.TryGetReference(keyValuePair.Value, out var reference))
                    roleToReference[keyValuePair.Key] = reference;
            }
        }
        
        
        //_______________  METHOD BREAK   ___________________//
        
        
        // This is a protection to remove null objects from the list of options
        public void SyncReferencesFromNames()
        {
            selectedReferences.Clear();
            foreach (var name in selectedNames)
            {
                var reference = Herd?.FindReference(name);
                if (reference.HasValue)
                    selectedReferences.Add(reference.Value);
            }
        
            _membersCache = null;
        }
                
                
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Ensures the <see cref="Metric"/> enum matches the specified <see cref="Family"/>. Throws if not.
        /// This is used as a fail safe at run time to fail quickly in case of a bad configuration.
        /// <see cref="ValidateMetricFamily"/> is used while in editor mode to prevent mismatches in the first place.
        /// </summary>
        protected void ValidateMetricFamilyMatch()
        {
            bool ok = Family switch
            {
                FamilyType.Single   => Metric is SingleMetric,
                FamilyType.Couple   => Metric is CoupleMetric,
                FamilyType.Throuple => Metric is ThroupleMetric,
                FamilyType.Group    => Metric is GroupMetric,
                _                   => false
            };
            if (!ok)
                throw new InvalidOperationException(
                    $"Metric '{Metric}' is not valid for FamilyType {Family}");
        }
        
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Ensures that only the enum matching the current FamilyType is non-default.
        /// Auto-clears the others and logs a warning if there’s a mismatch. Used in editor mode only.
        /// <see cref="ValidateMetricFamilyMatch"/> is the stricter version called for catching runtime mistakes.
        /// </summary>
        private void ValidateMetricFamily()
        {
            // For each family type, only its enum should be non-default.
            switch (familyType)
            {
                case FamilyType.Single:
                    if (coupleMetric != default || throupleMetric != default || groupMetric != default)
                        Debug.LogWarning($"[{name}] Only SingleMetric is valid when FamilyType=Single. Clearing others.");
                    coupleMetric = default;
                    throupleMetric = default;
                    groupMetric = default;
                    break;

                case FamilyType.Couple:
                    if (singleMetric != default || throupleMetric != default || groupMetric != default)
                        Debug.LogWarning($"[{name}] Only CoupleMetric is valid when FamilyType=Couple. Clearing others.");
                    singleMetric = default;
                    throupleMetric = default;
                    groupMetric = default;
                    break;

                case FamilyType.Throuple:
                    if (singleMetric != default || coupleMetric != default || groupMetric != default)
                        Debug.LogWarning($"[{name}] Only ThroupleMetric is valid when FamilyType=Throuple. Clearing others.");
                    singleMetric = default;
                    coupleMetric = default;
                    groupMetric = default;
                    break;

                case FamilyType.Group:
                    if (singleMetric != default || coupleMetric != default || throupleMetric != default)
                        Debug.LogWarning($"[{name}] Only GroupMetric is valid when FamilyType=Group. Clearing others.");
                    singleMetric = default;
                    coupleMetric = default;
                    throupleMetric = default;
                    break;
            }
        }
        
        
        //_______________  METHOD BREAK   ___________________//


        // This runs on validation to ensure that Minimum and Maximum are not incorrectly assigned
        // and to ensure that the herd and its members are up to date.
        private void OnValidate()
        {
            if (min >= max) max = min + 0.01f;
            if (max <= min) min = max - 0.01f;
            ValidateMetricFamilyMatch();
            InvalidateCache();
            
            Enum currentMetric = Metric;

            if (familyType != _lastFamilyType || !Equals(currentMetric, _lastMetric))
            {
                _lastFamilyType = familyType;
                _lastMetric     = currentMetric;

                OnMetricChanged();  // <- where you reset _settings with new data
            }
            
            
#if UNITY_EDITOR
            if (Herd != null && Herd != lastHerd)
            {
                lastHerd = Herd;
                SyncReferencesFromNames();
                _membersCache = null;
            }
            ValidateMetricFamily();
#endif
        }
        
        #endregion
        
    }
}