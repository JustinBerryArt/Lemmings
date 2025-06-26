using System;
using UnityEngine;

namespace Lemmings
{

    /// <summary>
    /// Allows previewing a secondary or alternate metric from an existing LemmingRelationship.
    /// Evaluates on-demand using public method call, avoiding continuous updates.
    /// </summary>
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public class LemmingSecondaryMetric : MonoBehaviour
    {
        [Tooltip("The source relationship from which to derive the metric.")]
        public LemmingRelationship relationship;

        [Tooltip("Select to override relationship min and max values.")]
        public bool useNewThresholds = false;

        [Tooltip("Optional override for min normalization value.")]
        public float min = 0f;

        [Tooltip("Optional override for max normalization value.")]
        public float max = 1f;

        [Tooltip("Select to use specific curve.")]
        public bool changeCurve = false;

        [Tooltip("Optional override for settings.")]
        [SerializeField] private bool useAdvancedSettings;
        
        [Tooltip("Select specific curve to use.")]
        public LemmingCurveType curveType = LemmingCurveType.Linear;
        
        [Tooltip("Curve to apply to the normalized value if using Custom Curve.")]
        public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Metric")]
        [Tooltip("Metric method to evaluate when relationship uses Single.")]
        public SingleMetric singleMetric;

        [Tooltip("Metric method to evaluate when relationship uses Couple.")]
        public CoupleMetric coupleMetric;

        [Tooltip("Metric method to evaluate when relationship uses Throuple.")]
        public ThroupleMetric throupleMetric;

        [Tooltip("Metric method to evaluate when relationship uses Group.")]
        public GroupMetric groupMetric;

        [SerializeField] private LemmingRelationSetting secondarySettings = new LemmingRelationSetting();
        
        [SerializeField] public LemmingRelationshipInfo secondaryInfo = new LemmingRelationshipInfo();
        
        private bool _hasInitialized = false;
        
        [Header("Preview Output")]
        [ReadOnly] public object Raw;
        [ReadOnly] public float Normalized;
        [ReadOnly] public float Curved;
        [ReadOnly] public bool Over;
        [ReadOnly] public bool Under;
        [ReadOnly] public bool InRange;

        /// <summary>
        /// Returns the most recently cached output. Refresh() must be called manually to update it.
        /// </summary>
        public Output Output => new Output
        {
            Raw = Raw,
            Normalized = Normalized,
            Curved = Curved,
            InRange = InRange,
            Over = Over,
            Under = Under
        };

        /// <summary>
        /// Returns a fresh output by re-evaluating the relationship. This calls Refresh internally.
        /// </summary>
        public Output LiveOutput
        {
            get
            {
                Refresh();
                return Output;
            }
        }


        public void OnEnable()
        {
            if (_hasInitialized == false)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            secondaryInfo = relationship.Info;
            _hasInitialized = true;
        }
        
        public void RevertToOriginal()
        {
            if (relationship == null) return;

            // Pull fresh data from the relationship
            secondaryInfo = relationship.Info;

            // Also restore visible secondary settings to match the relationship's
            secondarySettings = relationship.Settings;

            // Immediately apply the reset values
            Refresh();
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        /// <summary>
        /// This is called to refresh the output and the values, to have a responsive experience you will want to call
        /// this or reference the Output in an update function
        /// </summary>
        public void Refresh()
        {
            if (relationship == null) return;
            
            secondaryInfo = relationship.Info;
            
            secondaryInfo.Metric = GetSelectedMetric(secondaryInfo.FamilyType);

            if (useNewThresholds)
            {
                secondaryInfo.Min = min;
                secondaryInfo.Max = max;
            }
            
            if (changeCurve)
            {
                secondaryInfo.CurveType = curveType;
                secondaryInfo.CustomCurve = curve;
            }

            if (useAdvancedSettings)
            {
                secondaryInfo.Settings =  secondarySettings;
            }
            
            var result = RelationshipPreview.ComputeMetric(secondaryInfo, secondaryInfo.Metric, null);

            Raw = result.Raw;
            Normalized = result.Normalized;
            Curved = result.Curved;
            Over = result.Over;
            Under = result.Under;
            InRange = result.InRange;
        }
        
        
        public Enum GetSelectedMetric(FamilyType family)
        {
            return family switch
            {
                FamilyType.Single   => singleMetric,
                FamilyType.Couple   => coupleMetric,
                FamilyType.Throuple => throupleMetric,
                FamilyType.Group    => groupMetric,
                _ => singleMetric
            };
        }
    }

    /// <summary>
    /// Optional attribute to lock fields to read-only in the Inspector.
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }
}
