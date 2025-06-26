using System;
using System.Collections.Generic;
using System.Text;
using Lemmings.UI;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Lemmings
{
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public class LemmingUtilities {}
    
    #region Lemming Relationship Info Struct

    /// <summary>
    /// This is a struct that holds the data from a relationship.
    /// Intended to streamline visualization and manipulation from external menus
    /// </summary>
  
    [System.Serializable]
    /// <summary>
    /// Data container carrying all relevant information about a <see cref="LemmingRelationship"/>
    /// for UI binding, serialization, or ad-hoc inspection.
    /// </summary>
    public struct LemmingRelationshipInfo
    {
        /// <summary>The original ScriptableObject defining this relationship.</summary>
        public LemmingRelationship Relationship;

        /// <summary>Unique identifier for this relationship (e.g., "distance_leader_follower").</summary>
        public string Id;

        /// <summary>Human-readable description of the relationship's purpose or logic.</summary>
        public string Description;

        /// <summary>Family structure type (Single, Couple, Throuple, Group).</summary>
        public FamilyType FamilyType;

        /// <summary>Herd Snapshot referenced by this relationship </summary>
        public LemmingHerdSnapshot Herd;
        
        /// <summary>The enum value representing the selected metric.</summary>
        public Enum Metric;

        /// <summary>Friendly name of the metric (e.g., "Distance").</summary>
        public string MetricName;

        /// <summary>Description of Metric.</summary>
        public string MetricDescription;

        /// <summary>Metric settings.</summary>
        public LemmingRelationSetting Settings;
        
        /// <summary>List of member names paired with their roles.</summary>
        public List<(string LemmingName, string Role)> Members;

        /// <summary>
        /// List of members with full reference info for more secure access
        /// </summary>
        public List<LemmingReference> References;
        
        /// <summary>Raw minimum bound for normalization.</summary>
        public float Min;

        /// <summary>Raw maximum bound for normalization.</summary>
        public float Max;

        /// <summary>Curve type used to shape the output.</summary>
        public LemmingCurveType CurveType;

        /// <summary>Custom AnimationCurve when <see cref="CurveType"/> is Custom.</summary>
        public AnimationCurve CustomCurve;

        /// <summary>True if using the custom curve.</summary>
        public bool UseCustomCurve;

        /// <summary>Most recent raw value computed by <see cref="LemmingRelation"/>.</summary>
        public object RawValue;

        /// <summary>Type of the raw value (Float, Vector3, Quaternion, etc.).</summary>
        public ValueType ValueType;

        /// <summary>Normalized [0,1] value after applying Min/Max bounds.</summary>
        public float NormalizedValue;

        /// <summary>Value after passing through the selected curve.</summary>
        public float CurvedValue;

        /// <summary>Logical status indicating Over, Under, InRange, or None.</summary>
        public RelationshipStatus Status;

        /// <summary>True if the raw value exceeds the Max bound.</summary>
        public bool Over;

        /// <summary>True if the raw value is below the Min bound.</summary>
        public bool Under;

        /// <summary>True if the raw value lies within the Min-Max range.</summary>
        public bool InRange;

        /// <summary>Converts Normalized value to -1 to 1 for easier input mapping and referencing relative values.</summary>
        public float AsAxis;
        
        /// <summary>Optional per-metric setting definitions (e.g., threshold, axis choice).</summary>
        public MetricSettingInfo MetricSettings;

        /// <summary>Optional grouped metric options for advanced UI scenarios.</summary>
        public MetricOptionInfo MetricOptions;

        /// <summary>The underlying struct wrapper for computations.</summary>
        public LemmingRelation Relation;

        /// <summary>
        /// Converter instance used to translate raw data into normalized/typed values.
        /// </summary>
        public ILemmingConverter Converter;

        /// <summary>The LemmingDatum representing the last computed datum.</summary>
        public LemmingDatum Datum;
        
        
        /// <summary>
        /// Refreshes only the “dynamic” fields (Datum, Raw/Normalized/Curved, flags, status)
        /// based on the static data already stored in this struct.
        /// </summary>
        public void RefreshDynamic()
        {
            // 1) recompute the datum (fires any events on the Relationship if you rely on that)
            var datum     = Relation.ToDatum();

            // 2) get a fresh converter from the cached bounds
            var converter = datum.GetConverter(Min, Max) as ILemmingConverter;

            // 3) stomp only the fields that actually change
            Datum           = datum;
            RawValue        = converter.Raw;
            NormalizedValue = converter.Normalized;

            // pick the proper curve
            var curve = UseCustomCurve ? CustomCurve
                : LemmingCurveLibrary.GetCurve(CurveType);
            CurvedValue = converter.ToCurve(curve);

            Over    = converter.Over;
            Under   = converter.Under;
            InRange = converter.InRange;

            // recompute status
            Status = Over && !Under && !InRange   ? RelationshipStatus.Over
                : !Over && Under && !InRange   ? RelationshipStatus.Under
                : !Over && !Under && InRange   ? RelationshipStatus.InRange
                : RelationshipStatus.None;

            Converter = converter;
            ValueType = datum.Type;
        }
        
    }


    #endregion
        
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------
    
    #region Lemming Output Struct
    
    /// <summary>
    /// This is a simple struct that holds the value outputs from a metric calculation
    /// </summary>
    [System.Serializable]
    public struct Output
    {
        public object Raw;
        public float Normalized;
        public float Curved;
        public bool InRange;
        public bool Over;
        public bool Under;
        public float AsAxis;
    }
    
    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------
    
    #region LemmingUtils class for managing data structure and stability
    
    /// <summary>
    /// Utility class providing structural and scene-based helpers for working with Lemmings.
    /// Includes path resolution, transform tracking, and fallback object retrieval.
    /// </summary>
    public static class LemmingUtils
    {
        
        /// <summary>
        /// Checks if the given ILemming's GameObject is a descendant of a valid LemmingShepherd.
        /// This is called because without a Shepherd the Lemming does not stay present from Scene to Scene.
        /// In other words, this is required for the Lemmings to be accessible in menus and other areas.
        /// </summary>
        public static bool HasValidShepherd(this ILemming lemming)
        {
            var obj = lemming.Source;
            if (obj == null) return false;

            Transform current = obj.transform;
            while (current != null)
            {
                if (current.GetComponent<LemmingShepherd>() != null)
                    return true;
                current = current.parent;
            }
            return false;
        }
        
        
        
        
        /// <summary>
        /// Builds a stable, slash-separated path to the given transform from the scene root.
        /// Example: "Root/Body/Hand/Lemming_A"
        /// </summary>
        /// <param name="transform">The transform to generate the path for.</param>
        /// <returns>A string path from root to the transform.</returns>
        public static string GetTransformPath(Transform transform)
        {
            if (transform == null) return string.Empty;

            var sb = new StringBuilder(transform.name);
            while (transform.parent != null)
            {
                transform = transform.parent;
                sb.Insert(0, transform.name + "/");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Attempts to find a GameObject in the active scene using a slash-separated transform path.
        /// Falls back to searching by GameObject name if the path is not found.
        /// TODO: This needs more robustness to account for moved items and name changes
        /// </summary>
        /// <param name="path">The transform path string (e.g., "Root/Body/Hand/Lemming_A").</param>
        /// <param name="fallbackName">Optional fallback name to search by if path fails.</param>
        /// <returns>The GameObject found, or null if not found.</returns>
        public static GameObject ResolveSceneObject(string path, string fallbackName = null)
        {
            if (string.IsNullOrEmpty(path) && string.IsNullOrWhiteSpace(fallbackName)) return null;

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return null;

            var roots = scene.GetRootGameObjects();

            // Try resolving by full path first
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var root in roots)
                {
                    var child = root.transform.Find(path);
                    if (child != null)
                        return child.gameObject;
                }
            }

            // Fallback: try resolving by name if full path fails
            if (!string.IsNullOrWhiteSpace(fallbackName))
            {
                foreach (var obj in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                {
                    if (obj.name == fallbackName)
                        return obj;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves a GameObject reference from a stored field or resolves it from a path if null.
        /// If successfully resolved, the current path reference will be updated to reflect any new structure.
        /// </summary>
        /// <param name="cached">Reference to the current (possibly null) GameObject.</param>
        /// <param name="path">Reference to the path string, which will be updated if a new object is resolved.</param>
        /// <param name="fallbackName">Optional fallback name used if path fails.</param>
        /// <returns>The resolved GameObject, or null if not found.</returns>
        public static GameObject GetOrResolve(ref GameObject cached, ref string path, string fallbackName = null)
        {
            // Use cached if already assigned
            if (cached != null)
                return cached;

            // Attempt to resolve using the current path and fallback name
            cached = ResolveSceneObject(path, fallbackName);

            // If successful, refresh the path to match current transform hierarchy
            if (cached != null)
            {
                path = GetTransformPath(cached.transform);
            }

            return cached;
        }
    }
    
    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    
    #region Relationship Preview Static Class and Extension Methods

    /// <summary>
    /// Provides methods to perform a non-persistent, live preview of
    /// a spatial relationship defined by <see cref="LemmingRelationshipInfo"/>.
    /// Useful for UI remapping scenarios where users adjust settings and
    /// need immediate feedback without altering the original ScriptableObject.
    /// </summary>
    public static class RelationshipPreview
    {
        /// <summary>
        /// Holds the results of a live relationship computation,
        /// including raw, normalized, curved values and boundary status flags.
        /// </summary>
        public struct Result
        {
            /// <summary>
            /// The unprocessed output of <see cref="LemmingRelation.Evaluate"/>.
            /// Can be any type (float, Vector3, Quaternion, etc.).
            /// </summary>
            public object Raw;

            /// <summary>
            /// The result remapped into [0,1] based on Min/Max bounds.
            /// </summary>
            public float Normalized;

            /// <summary>
            /// The normalized value after applying the chosen animation curve.
            /// </summary>
            public float Curved;

            /// <summary>
            /// True if the raw value lies strictly above the Max bound.
            /// </summary>
            public bool Over;

            /// <summary>
            /// True if the raw value lies strictly below the Min bound.
            /// </summary>
            public bool Under;

            /// <summary>
            /// True if the raw value is within the [Min,Max] range (inclusive).
            /// </summary>
            public bool InRange;
            
            /// <summary>
            /// This provides the normalized value in a range of -1 to 1, useful for input mapping and relative values
            /// </summary>
            public float AsAxis;
        }

        /// <summary>
        /// Performs a one-off preview calculation using the supplied info
        /// without mutating any underlying ScriptableObject or runtime data.
        /// </summary>
        /// <param name="info">
        /// A snapshot of user-adjustable settings and references captured in
        /// a <see cref="LemmingRelationshipInfo"/> struct.
        /// </param>
        /// <param name="settings">
        /// Optional settings defined by a <see cref="LemmingRelationSetting"/> struct.
        /// if you do not want to use the settings defined by the original relationship
        /// </param>
        /// <returns>
        /// A <see cref="Result"/> containing raw, normalized, curved values
        /// and status flags (Over/Under/InRange).
        /// </returns>
        public static Result Compute(in LemmingRelationshipInfo info, LemmingRelationSetting settings = null)
        {
            // Build a temporary struct-based relation context
            var relation = new LemmingRelation(
                info.FamilyType,
                info.References,
                info.Metric,
                settings ?? info.Settings
            );

            // 1) Evaluate raw metric
            var rawValue = relation.Evaluate();

            // 2) Wrap into a datum and then a converter with edited bounds
            var datum = relation.ToDatum();
            var converter = datum.GetConverter(info.Min, info.Max) as ILemmingConverter;

            // 3) Select the proper curve (custom or preset)
            AnimationCurve curve = info.CurveType == LemmingCurveType.Custom
                ? info.CustomCurve
                : LemmingCurveLibrary.GetCurve(info.CurveType);

            // 4) Package and return
            return new Result
            {
                Raw         = rawValue,
                Normalized  = converter?.Normalized ?? 0f,
                Curved      = converter != null ? converter.ToCurve(curve) : 0f,
                Over        = converter?.Over ?? false,
                Under       = converter?.Under ?? false,
                InRange     = converter?.InRange ?? false,
                AsAxis     = converter?.AsAxis ?? 0f
            };
        }
        
        /// <summary>
        /// Compute a preview result using a specific metric override.
        /// </summary>
        /// <typeparam name="TEnum">The metric enum type (SingleMetric, CoupleMetric, etc.)</typeparam>
        /// <param name="info">Base relationship info.</param>
        /// <param name="metric">The specific metric you want to preview.</param>
        /// <returns>Preview result with raw/normalized/curved/etc.</returns>
        public static Result ComputeMetric<TEnum>(in LemmingRelationshipInfo info, TEnum metric, LemmingRelationSetting settings = null) where TEnum : Enum
        {
            // Rebuild preview struct with the overridden metric
            var relation = new LemmingRelation(
                info.FamilyType,
                info.References,
                metric,
                settings ?? info.Settings
            );

            var rawValue = relation.Evaluate();
            var datum = relation.ToDatum();
            var converter = datum.GetConverter(info.Min, info.Max) as ILemmingConverter;

            AnimationCurve curve = info.CurveType == LemmingCurveType.Custom
                ? info.CustomCurve
                : LemmingCurveLibrary.GetCurve(info.CurveType);

            return new Result
            {
                Raw        = rawValue,
                Normalized = converter?.Normalized ?? 0f,
                Curved     = converter?.ToCurve(curve) ?? 0f,
                Over       = converter?.Over ?? false,
                Under      = converter?.Under ?? false,
                InRange    = converter?.InRange ?? false,
                AsAxis     = converter?.AsAxis ?? 0f
            };
        }
        
        
        /// <summary>
        /// Performs a one-off preview computation using a specific metric and optional overrides,
        /// without modifying the underlying relationship or asset data.
        /// </summary>
        /// <typeparam name="TEnum">
        /// The type of metric enum to evaluate (e.g. <see cref="SingleMetric"/>, <see cref="CoupleMetric"/>).
        /// </typeparam>
        /// <param name="info">
        /// A read-only struct containing snapshot data from a <see cref="LemmingRelationship"/>.
        /// This includes references, family type, curve, and bounds.
        /// </param>
        /// <param name="metric">
        /// The metric to evaluate. This overrides the metric stored in <paramref name="info"/>.
        /// </param>
        /// <param name="customMin">
        /// Optional minimum bound for normalization. If null, uses <paramref name="info"/>.Min.
        /// </param>
        /// <param name="customMax">
        /// Optional maximum bound for normalization. If null, uses <paramref name="info"/>.Max.
        /// </param>
        /// <param name="setting">
        /// Optional parameter structure that provides evaluation settings such as axis, threshold, etc.
        /// See <see cref="LemmingRelationSetting"/> for more.
        /// </param>
        /// <returns>
        /// A <see cref="Result"/> containing the raw value, normalized and curved outputs,
        /// and status flags indicating whether the result is over, under, or in range.
        /// </returns>
        public static Result ComputeMetric<TEnum>(
            in LemmingRelationshipInfo info,
            TEnum metric,
            float? customMin = null,
            float? customMax = null,
            LemmingRelationSetting? settings = null
        ) where TEnum : Enum
        {
            // Create the preview relation struct
            var relation = new LemmingRelation(
                info.FamilyType,
                info.References,
                metric,
                settings ?? info.Settings
            );

            // Evaluate the raw value with custom settings
            var rawValue = relation.Evaluate();
            var datum = relation.ToDatum();

            // Use custom min/max if provided
            float min = customMin ?? info.Min;
            float max = customMax ?? info.Max;

            var converter = datum.GetConverter(min, max) as ILemmingConverter;

            AnimationCurve curve = info.CurveType == LemmingCurveType.Custom
                ? info.CustomCurve
                : LemmingCurveLibrary.GetCurve(info.CurveType);

            return new Result
            {
                Raw        = rawValue,
                Normalized = converter?.Normalized ?? 0f,
                Curved     = converter?.ToCurve(curve) ?? 0f,
                Over       = converter?.Over ?? false,
                Under      = converter?.Under ?? false,
                InRange    = converter?.InRange ?? false,
                AsAxis     = converter?.AsAxis ?? 0f
            };
        }
    }

    /// <summary>
    /// This is a utility wrapper for the RelationshipPreview class to make it easier to use.
    /// example = Vector3 previewPos = RelationshipPreviewUtility.PreviewPosition(relationship);
    /// </summary>
    public static class RelationshipPreviewUtility
    {
        /// <summary>
        /// Previews the position metric for a given relationship, regardless of family type.
        /// </summary>
        public static Vector3 PreviewPosition(LemmingRelationship relationship)
        {
            if (relationship == null)
                return Vector3.zero;

            // Check for herd or metric issues before calling Info
            if (relationship.Herd == null || relationship.Metric == null)
                return Vector3.zero;

            LemmingRelationshipInfo info;

            try
            {
                info = relationship.Info;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PreviewPosition] Failed to get Info for '{relationship.name}': {ex.Message}");
                return Vector3.zero;
            }

            var metric = GetPositionMetric(info.FamilyType);

            var result = RelationshipPreview.ComputeMetric(info, metric, info.Settings);

            return result.Raw is Vector3 pos ? pos : Vector3.zero;
        }

        /// <summary>
        /// Previews the rotation metric for a given relationship, regardless of family type.
        /// </summary>
        public static Quaternion PreviewRotation(LemmingRelationship relationship)
        {
            if (relationship == null) return Quaternion.identity;

            var info = relationship.Info;
            var metric = GetRotationMetric(info.FamilyType);

            var result = RelationshipPreview.ComputeMetric(
                info,
                metric, 
                info.Settings
            );

            return result.Raw is Quaternion rot ? rot : Quaternion.identity;
        }

        /// <summary>
        /// Previews the raw float value for a custom metric with optional settings and bounds.
        /// </summary>
        public static float PreviewFloat<TEnum>(
            LemmingRelationship relationship,
            TEnum metric,
            float? min = null,
            float? max = null,
            LemmingRelationSetting settings = null
        ) where TEnum : System.Enum
        {
            if (relationship == null) return 0f;

            var result = RelationshipPreview.ComputeMetric(
                relationship.Info,
                metric,
                min,
                max,
                settings ?? relationship.Info.Settings
            );

            return result.Normalized;
        }

        /// <summary>
        /// Returns the correct "Position" metric enum for the given family type.
        /// </summary>
        private static System.Enum GetPositionMetric(FamilyType type) => type switch
        {
            FamilyType.Single => SingleMetric.Position,
            FamilyType.Couple => CoupleMetric.Position,
            FamilyType.Throuple => ThroupleMetric.Position,
            FamilyType.Group => GroupMetric.Position,
            _ => SingleMetric.Position
        };

        /// <summary>
        /// Returns the correct "Rotation" metric enum for the given family type.
        /// </summary>
        private static System.Enum GetRotationMetric(FamilyType type) => type switch
        {
            FamilyType.Single => SingleMetric.Rotation,
            FamilyType.Couple => CoupleMetric.Rotation,
            FamilyType.Throuple => ThroupleMetric.Rotation,
            FamilyType.Group => GroupMetric.Rotation,
            _ => SingleMetric.Rotation
        };
    }
    
    
    
    
    /// <summary>
    /// Extension methods for <see cref="LemmingRelationshipInfo"/>,
    /// enabling concise, fluent preview calls in UI or other code.
    /// </summary>
    public static class LemmingRelationshipInfoExtensions
    {
        /// <summary>
        /// Computes a live relationship preview directly from this info
        /// using <see cref="RelationshipPreview.Compute(in LemmingRelationshipInfo)"/>.
        /// </summary>
        /// <param name="info">
        /// The <see cref="LemmingRelationshipInfo"/> instance to preview.
        /// </param>
        /// <returns>
        /// A <see cref="RelationshipPreview.Result"/> containing raw,
        /// normalized, curved values and boundary statuses.
        /// </returns>
        public static RelationshipPreview.Result Preview(this in LemmingRelationshipInfo info)
            => RelationshipPreview.Compute(info);
    }

    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------
    
    #region Lemming Metric Metadata
    
    
    /// <summary>
    /// Provides descriptions and optional editor UI for Lemming metric enum values.
    /// Helps clarify affordances and behaviors for user-facing tools.
    /// </summary>
    public static class LemmingMetricMetadata
    {
        //____________________________________________ SINGLE METRIC DESCRIPTIONS __________>>
        
        private static string SinglePosition =
            "Returns the world position of the Lemming. "
            + "You can filter the output to a single axis (X, Y, or Z) if needed. "
            + "Use this when you care about where a Lemming is in space.";

        private static string SingleRotation =
            "Returns the rotation of the Lemming as a Quaternion. "
            + "If used with gaze settings, this can act as a trigger when the Lemming looks at something. "
            + "Choose this if you're working with orientation, facing, or view direction.";

        private static string SingleMovement =
            "Measures how fast and in what direction the Lemming is moving. "
            + "You can return the full velocity, limit it to a single axis, or simplify it to just speed. "
            + "Great for detecting motion or feeding movement into other systems.";

        private static string SingleTrigger =
            "Returns true when the Lemming’s trigger proxy is active. "
            + "This is often used for detecting collisions, proximity, or interaction events. "
            + "Only works if a proxy is set up and properly configured.";
        
        //____________________________________________ COUPLE METRIC DESCRIPTIONS __________>>
        private static string CouplePosition =
            "Finds the spot exactly between the two Lemmings. "
            + "You can narrow it down to just X, Y, or Z if needed. "
            + "Good for tracking where a pair is centered in space.";

        private static string CoupleRotation =
            "Figures out which way the pair is facing, from the follower toward the leader. "
            + "You can flip the direction or change how 'up' is defined. "
            + "Useful when you want something to aim or turn based on the pair’s layout.";

        private static string CoupleMovement =
            "Shows how the two Lemmings are moving together. "
            + "You can use it as a direction, a speed, or just focus on one axis. "
            + "Helpful for driving effects based on how the pair moves.";

        private static string CoupleTrigger =
            "Turns on when the Lemmings’ trigger setup is active — like touching or interacting. "
            + "Only works if you’ve set up a trigger proxy for this pair. "
            + "Use it to respond to contact or activation between the two.";

        private static string CoupleDistance =
            "Measures how far apart the two Lemmings are. "
            + "You can switch between meters, feet, inches, or centimeters. "
            + "Great for things like closeness, spacing, or proximity rules.";

        private static string CoupleDifference =
            "Returns the direction from one Lemming to the other. "
            + "You can look at just one axis or get a total distance instead. "
            + "Use this when you care about where one member is in relation to the other.";

        
        //private static string CoupleRotationAroundAxis = 
        //"example";
        
        //private static string CoupleRelativeVelocity = 
        //"example";
        
        //private static string CouplePose = 
        //"example"; 
        
        //____________________________________________ THROUPLE METRIC DESCRIPTIONS __________>>
        
        private static string ThrouplePosition =
            "Finds the center point between all three Lemmings. "
            + "You can pull just one axis or use the full 3D position. "
            + "Good for tracking where the group is gathered.";

        private static string ThroupleRotation =
            "Figures out which way the group is facing, based on the layout of all three. "
            + "You can choose which member the rotation is based on, or flip the direction. "
            + "Use this to align things to the group’s overall direction.";

        private static string ThroupleMovement =
            "Shows how the group of three is moving as a whole. "
            + "You can get a direction, a speed, or a single axis. "
            + "Useful for driving animations or effects tied to motion.";

        private static string ThroupleTrigger =
            "Turns on when the group’s trigger setup becomes active. "
            + "This could represent things like shared interactions or proximity. "
            + "Only works if a trigger proxy is configured for the group.";

        private static string ThroupleDistance =
            "Measures the distances between each member of the group. "
            + "You can pick one pair or get the average of all three. "
            + "Helpful for gauging how tightly or loosely the group is spread out.";

        private static string ThroupleRotationAroundAxis =
            "Checks how much the group has twisted or rotated around a chosen direction. "
            + "You can set which axis and which member layout to use. "
            + "Useful for detecting leaning, turning, or circling behaviors.";

        private static string ThroupleDensity =
            "Tells you how tightly packed the group is. "
            + "This is based on how far each member is from the group’s center. "
            + "Use it to track group cohesion or spreading out.";

        private static string ThroupleAngle =
            "Measures the angle at each point in the triangle made by the group. "
            + "You get three values: the X value is the angle at the Leader. "
            + "the Y value is the angle at the Follower, and the Z value is the angle at the Third. "
            + "Good for understanding shape, tension, or how the group is arranged.";

        private static string ThroupleSize =
            "Returns how large the group is — either the full bounding box or the distance from center. "
            + "You can pull one axis or use the full 3D size. "
            + "Great for understanding spread or formation shape.";


        //private static string ThrouplePose = 
        //"example"; 
                    
        //private static string ThroupleSpread = 
        //"example";
        
        //private static string ThroupleWeighting = 
        //"example";

        
        
        //____________________________________________ GROUP METRIC DESCRIPTIONS __________>>
        
                
        private static string GroupPosition =
            "Finds the average position of all the Lemmings in the group. "
            + "You can pull out just one axis if you like. "
            + "Useful for tracking where the whole group is centered.";

        private static string GroupRotation =
            "Figures out the overall direction the group is moving toward. "
            + "This is based on the average velocity of all members. "
            + "Helpful for aligning other objects to the group’s flow or movement.";

        private static string GroupMovement =
            "Shows how the group is moving together. "
            + "You can get a direction, speed, or even how much they’re moving toward or away from their center. "
            + "Use this for effects that respond to overall group motion.";

        private static string GroupTrigger =
            "Turns on when the group’s trigger proxy becomes active. "
            + "Often used for detecting when the group reaches a zone or interacts with something. "
            + "Requires a trigger proxy to be assigned.";

        private static string GroupRotationAroundAxis =
            "Checks how much the group’s movement is turning or rotating around a chosen axis. "
            + "You can define a reference direction and control the measurement settings. "
            + "Useful for spotting drifts, spirals, or coordinated turns.";

        private static string GroupDensity =
            "Tells you how tightly or loosely packed the group is. "
            + "Based on how far members are from the group’s center. "
            + "Helpful for detecting clustering or spreading behavior.";

        private static string GroupSize =
            "Returns how large the group is — either the bounding box size or the radius from center. "
            + "You can focus on one axis or get the full 3D size. "
            + "Good for tracking formation width or spacing.";
        
        //private static string GroupPose = 
        //"example"; 
            
        //private static string GroupDistance = 
        //"example";
            
        //private static string GroupSpread = 
        //"example";
        
        //private static string GroupWeighting = 
        //"example";

        
        
        
        /// <summary>
        /// Returns a descriptive string for the given metric enum.
        /// </summary>
        public static string GetDescription(Enum metric)
        {
            
            return metric switch
            {
                //____________________________________________ SINGLE METRIC DESCRIPTIONS __________>>
                
                SingleMetric.Position     => SinglePosition,
                SingleMetric.Rotation     => SingleRotation,
                SingleMetric.Movement     => SingleMovement,
                SingleMetric.Trigger      => SingleTrigger,
                
                //____________________________________________ COUPLE METRIC DESCRIPTIONS __________>>
                
                CoupleMetric.Position             => CouplePosition,
                CoupleMetric.Rotation             => CoupleRotation,
                CoupleMetric.Movement             => CoupleMovement,
                CoupleMetric.Trigger              => CoupleTrigger,
                CoupleMetric.Distance             => CoupleDistance,
                CoupleMetric.Difference           => CoupleDifference,
                //CoupleMetric.RotationAroundAxis   => CoupleRotationAroundAxis,
                //CoupleMetric.RelativeVelocity     => CoupleRelativeVelocity,
                //CoupleMetric.Pose                 => CouplePose,
                
                //____________________________________________ THROUPLE METRIC DESCRIPTIONS __________>>
                
                ThroupleMetric.Position              => ThrouplePosition,
                ThroupleMetric.Rotation              => ThroupleRotation,
                ThroupleMetric.Movement              => ThroupleMovement,
                ThroupleMetric.Trigger               => ThroupleTrigger,
                ThroupleMetric.Distance              => ThroupleDistance,
                ThroupleMetric.RotationAroundAxis    => ThroupleRotationAroundAxis,
                ThroupleMetric.Density               => ThroupleDensity,
                ThroupleMetric.Angle                 => ThroupleAngle,
                ThroupleMetric.Size                  => ThroupleSize,
                //ThroupleMetric.Pose                  => ThrouplePose,
                //ThroupleMetric.Spread                => ThroupleSpread,
                //ThroupleMetric.Weighting             => ThroupleWeighting,
                
                //____________________________________________ GROUP METRIC DESCRIPTIONS __________>>
                
                GroupMetric.Position              => GroupPosition,
                GroupMetric.Rotation              => GroupRotation,
                GroupMetric.Movement              => GroupMovement,
                GroupMetric.Trigger               => GroupTrigger,
                GroupMetric.RotationAroundAxis    => GroupRotationAroundAxis,
                GroupMetric.Density               => GroupDensity,
                GroupMetric.Size                  => GroupSize,
                //GroupMetric.Distance              => GroupDistance,
                //GroupMetric.Pose                  => GroupPose,
                //GroupMetric.Spread                => GroupSpread,
                //GroupMetric.Weighting             => GroupWeighting,
                
                _ => "No description is available for this metric."
            };

        }

        public static string ExampleDescription()
        {
            string example = "Long description";

            return example;
        }
        
        /*
         * TODO: This is where the metric options would populate - Currently Editor only but relevant for playmode remapping


        /// <summary>
        /// Draws any custom editor controls required by the given metric.
        /// </summary>
        public static void DrawCustomUI(Enum metric, SerializedObject serializedRelationship)
        {
            if (metric is SingleMetric.Position)
            {
            
            }
        
            if (metric is SingleMetric.Rotation)
            {
            
            }
        
            if (metric is SingleMetric.Movement)
            {
            
            }
        
            if (metric is SingleMetric.Trigger)
            {
            
            }

            // Add more metric-specific UI affordances as needed
        }
        */
    }
    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region Lemming Reference Struct
    /// <summary>
    /// A single named Lemming entry in the herd.
    /// </summary>
    [System.Serializable]
    public struct LemmingReference: ILemming
    {
        [SerializeField] private GameObject source;
        public string scenePath;
        public string objectName;

        public string name;
        public float confidence;

        /// <summary>
        /// Accessor for the GameObject source. Automatically resolves from scene path or name if null.
        /// Updates the cached reference and scene path as needed.
        /// </summary>
        public GameObject Source
        {
            get => LemmingUtils.GetOrResolve(ref source, ref scenePath, objectName);
            set
            {
                source = value;
                scenePath = value != null ? LemmingUtils.GetTransformPath(value.transform) : null;
                objectName = value != null ? value.name : null;
            }
        }
        
        public string Name => name;
        
        // TODO: Figure out method for testing and reflecting the confidence of the lemming
        public float GetConfidence() => confidence;
    }
    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region Lemming Curve Library

    public static class LemmingCurveLibrary
    {
        public static AnimationCurve GetCurve(LemmingCurveType type)
        {
            return type switch
            {
                LemmingCurveType.Linear => AnimationCurve.Linear(0, 0, 1, 1),

                LemmingCurveType.EaseIn => new AnimationCurve(
                    new Keyframe(0, 0, 0, 2),
                    new Keyframe(1, 1, 0, 0)
                ),

                LemmingCurveType.EaseOut => new AnimationCurve(
                    new Keyframe(0, 0, 0, 0),
                    new Keyframe(1, 1, 2, 0)
                ),

                LemmingCurveType.EaseInOut => AnimationCurve.EaseInOut(0, 0, 1, 1),
        
                LemmingCurveType.ExponentialIn => new AnimationCurve(
                    new Keyframe(0, 0),
                    new Keyframe(0.05f, 0.7f),
                    new Keyframe(1, 1)
                ),
        
                LemmingCurveType.ExponentialOut => new AnimationCurve(
                    new Keyframe(0, 0),
                    new Keyframe(0.7f, 0.05f),
                    new Keyframe(1, 1)
                ),
        
                LemmingCurveType.ExponentialInOut => new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 20f),
                    new Keyframe(0.5f, 0.5f, 0f, 0f),
                    new Keyframe(1f, 1f, -20f, 0f)
                ),
        

                LemmingCurveType.QuadraticIn => new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(1f, 1f, 2f, 2f)
                ),

                LemmingCurveType.QuadraticOut => new AnimationCurve(
                    new Keyframe(0f, 0f, 2f, 2f),
                    new Keyframe(1f, 1f, 0f, 0f)
                ),

                LemmingCurveType.QuadraticInOut => new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 2f),
                    new Keyframe(0.5f, 0.5f, 0f, 0f),
                    new Keyframe(1f, 1f, 2f, 0f)
                ),

                LemmingCurveType.CubicIn => new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(1f, 1f, 3f, 3f)
                ),

                LemmingCurveType.CubicOut => new AnimationCurve(
                    new Keyframe(0f, 0f, 3f, 3f),
                    new Keyframe(1f, 1f, 0f, 0f)
                ),

                LemmingCurveType.CubicInOut => new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 3f),
                    new Keyframe(0.5f, 0.5f, 0f, 0f),
                    new Keyframe(1f, 1f, 3f, 0f)
                ),

                LemmingCurveType.SineIn => new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 1.6f),
                    new Keyframe(1f, 1f, 0f, 0f)
                ),

                LemmingCurveType.SineOut => new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(1f, 1f, 1.6f, 0f)
                ),

                LemmingCurveType.SineInOut => new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 1.6f),
                    new Keyframe(0.5f, 0.5f, 0f, 0f),
                    new Keyframe(1f, 1f, 1.6f, 0f)
                ),

                _ => AnimationCurve.Linear(0, 0, 1, 1)
            };
        }

    }
    
    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region Metric Setting Descriptions
    
       /// <summary>
    /// Provides plainspoken descriptions for LemmingRelationSetting fields.
    /// These are used to guide users in UI or tooltips when configuring metrics.
    /// </summary>
    public static class LemmingSettingDescriptions
    {
        //________________________________________ GENERAL SETTINGS ________________________>>

        private static string Invert =
            "Flip the direction or sign of the result.";

        private static string UseSingleAxis =
            "Only return the value from one axis (X, Y, or Z).";

        private static string SingleAxis =
            "Choose which axis to use when isolating a value.";

        private static string ReferenceVector =
            "Use this vector as a reference direction for comparison.";

        private static string RotationAxis =
            "The axis used when measuring angles or rotations.";

        private static string Threshold =
            "The minimum value required to trigger a true result.";

        //________________________________________ OBJECT-BASED SETTINGS ____________________>>

        private static string RelativeToObject =
            "Measure relative to another GameObject (like the camera).";

        private static string ObjectToReference =
            "The GameObject used as a reference for relative comparisons.";

        //________________________________________ GROUP/PAIR COMPARISON ____________________>>

        private static string RelativeToMembers =
            "Measure how members are moving toward or away from each other.";

        //________________________________________ MOTION/VELOCITY __________________________>>

        private static string MagnitudeOnly =
            "Only return the speed, without direction.";

        private static string DirectionOnly =
            "Only return the direction of movement, not the speed.";

        //________________________________________ DISTANCE + UNITS _________________________>>

        private static string DistanceOptions =
            "Choose which pair of members to measure distance between.";

        private static string DistanceUnit =
            "Select the unit to use (meters, feet, etc.).";

        //________________________________________ ROTATION + AXIS __________________________>>

        private static string AxisSelection =
            "Choose how to define the 'up' direction or comparison axis.";

        private static string AxisSelectionThrouple =
            "Select how to define the axis based on group layout.";

        //________________________________________ DENSITY + SIZE ___________________________>>

        private static string DensityMethod =
            "Choose how to calculate how tightly packed the group is.";

        private static string SizeMethod =
            "Select how to measure the group’s size (bounding box or radius).";

        //________________________________________ PROXY + TRIGGER __________________________>>

        private static string Proxy =
            "Used to detect triggers or gaze-based events.";

        private static string UseGazeFromProxy =
            "Turn on when the Lemming is looking at a valid target.";

        private static string AddTags =
            "Allow custom tags to define what the proxy can interact with.";

        private static string TagsToAdd =
            "List of tags this proxy will respond to.";


        /// <summary>
        /// Gets a description for a known Lemming setting by field name.
        /// </summary>
        /// <param name="fieldName">The field name in LemmingRelationSetting (e.g. "invert").</param>
        /// <returns>The matching description string, or null if not found.</returns>
        public static string GetDescription(string fieldName)
        {
            return fieldName switch
            {
                nameof(Invert) => Invert,
                nameof(UseSingleAxis) => UseSingleAxis,
                nameof(SingleAxis) => SingleAxis,
                nameof(ReferenceVector) => ReferenceVector,
                nameof(RotationAxis) => RotationAxis,
                nameof(Threshold) => Threshold,
                nameof(RelativeToObject) => RelativeToObject,
                nameof(ObjectToReference) => ObjectToReference,
                nameof(RelativeToMembers) => RelativeToMembers,
                nameof(MagnitudeOnly) => MagnitudeOnly,
                nameof(DirectionOnly) => DirectionOnly,
                nameof(DistanceOptions) => DistanceOptions,
                nameof(DistanceUnit) => DistanceUnit,
                nameof(AxisSelection) => AxisSelection,
                nameof(AxisSelectionThrouple) => AxisSelectionThrouple,
                nameof(DensityMethod) => DensityMethod,
                nameof(SizeMethod) => SizeMethod,
                nameof(Proxy) => Proxy,
                nameof(UseGazeFromProxy) => UseGazeFromProxy,
                nameof(AddTags) => AddTags,
                nameof(TagsToAdd) => TagsToAdd,
                _ => null
            };
        }
    }

    

    #endregion

    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------
    
    #region Metric Setting Configurer
    
    //TODO: This is a nice tool that did not get worked in but should be referenced in later versions to make Settings easier to customize
    
    /// <summary>
    /// Provides preconfigured MetricSettingInfo entries for each supported setting.
    /// Used by editor UI to populate advanced configuration panels.
    /// </summary>
    public static class MetricSettingRegistry
    {
        public static MetricSettingInfo GetSetting(string key)
        {
            return key switch
            {
                "invert" => new MetricSettingInfo
                {
                    displayName = "Invert",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Toggle,
                    boolValue = false
                },

                "useSingleAxis" => new MetricSettingInfo
                {
                    displayName = "Use Single Axis",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Toggle,
                    boolValue = false
                },

                "singleAxis" => new MetricSettingInfo
                {
                    displayName = "Axis",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Dropdown,
                    options = new List<string>(System.Enum.GetNames(typeof(SingleAxis))),
                    selectedIndex = 0
                },

                "referenceVector" => new MetricSettingInfo
                {
                    displayName = "Reference Vector",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Vector3Picker,
                    vector3Value = Vector3.forward
                },

                "rotationAxis" => new MetricSettingInfo
                {
                    displayName = "Rotation Axis",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Vector3Picker,
                    vector3Value = Vector3.up
                },

                "threshold" => new MetricSettingInfo
                {
                    displayName = "Threshold",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Slider,
                    floatValue = 0.7f
                },

                "relativeToObject" => new MetricSettingInfo
                {
                    displayName = "Relative to Object",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Toggle,
                    boolValue = false
                },

                "objectToReference" => new MetricSettingInfo
                {
                    displayName = "Object to Reference",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.ObjectSelector,
                    objectReference = null
                },

                "relativeToMembers" => new MetricSettingInfo
                {
                    displayName = "Relative to Members",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Toggle,
                    boolValue = false
                },

                "magnitudeOnly" => new MetricSettingInfo
                {
                    displayName = "Magnitude Only",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Toggle,
                    boolValue = false
                },

                "directionOnly" => new MetricSettingInfo
                {
                    displayName = "Direction Only",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Toggle,
                    boolValue = false
                },

                "distanceOptions" => new MetricSettingInfo
                {
                    displayName = "Distance Option",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Dropdown,
                    options = new List<string>(System.Enum.GetNames(typeof(DistanceOptions))),
                    selectedIndex = 0
                },

                "distanceUnit" => new MetricSettingInfo
                {
                    displayName = "Distance Unit",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Dropdown,
                    options = new List<string>(System.Enum.GetNames(typeof(DistanceUnit))),
                    selectedIndex = 0
                },

                "axisSelection" => new MetricSettingInfo
                {
                    displayName = "Axis Selection",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Dropdown,
                    options = new List<string>(System.Enum.GetNames(typeof(AxisSelection))),
                    selectedIndex = 0
                },

                "axisSelectionThrouple" => new MetricSettingInfo
                {
                    displayName = "Throuple Axis Mode",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Dropdown,
                    options = new List<string>(System.Enum.GetNames(typeof(AxisSelectionThrouple))),
                    selectedIndex = 0
                },

                "densityMethod" => new MetricSettingInfo
                {
                    displayName = "Density Method",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Dropdown,
                    options = new List<string>(System.Enum.GetNames(typeof(DensityMethod))),
                    selectedIndex = 1
                },

                "sizeMethod" => new MetricSettingInfo
                {
                    displayName = "Size Method",
                    description = LemmingSettingDescriptions.GetDescription(key),
                    controlType = SettingType.Dropdown,
                    options = new List<string>(System.Enum.GetNames(typeof(SizeMethod))),
                    selectedIndex = 0
                },

                _ => null
            };
        }
    } 
    #endregion
}