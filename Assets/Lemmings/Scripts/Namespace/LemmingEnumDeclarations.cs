using UnityEngine;

[Icon("Assets/Input/Icons/LemmingPale.png")]
public class LemmingEnumDeclarations : MonoBehaviour
{}

namespace Lemmings
{
    /// <summary>
    /// Setting of Lemming Value type for developers that want to gatekeep outputs
    /// </summary>
    public enum LemmingValueType
    {
        Float,
        Int,
        Bool,
        Vector3,
        Quaternion,
        String
    }
    
    //_________________ New Enum ________________________
    
    /// <summary>
    /// For selecting the type of grouping used for evaluation
    /// </summary>
    public enum FamilyType
    {
        Single,
        Couple, 
        Throuple,
        Group
    }
    
    //_________________ New Enum ________________________
    
    /// <summary>
    /// Safe Methods for Single Evaluations
    /// </summary>
    public enum SingleMetric
    {
        Position,            // just the source position > Vector3
        Rotation,            // source quaternion > Quaternion
        Movement,            // Velocity > Vector3
        Trigger,             // Proxy trigger event > Bool
    }

    //_________________ New Enum ________________________
    
    /// <summary>
    /// Safe Methods for Couple Evaluations
    /// </summary>
    public enum CoupleMetric
    {
        Position,            // midpoint > Vector3
        Rotation,            // average or relative > Quaternion
        Distance,            // float between a and b > Float
        Movement,            // velocity of center > Vector3
        Difference,          // delta between positions as packed vector3 > Vector3
        Trigger,             // proxy trigger event > Bool
    }

    //_________________ New Enum ________________________
    
    /// <summary>
    /// Safe Methods for Throuple Evaluations
    /// </summary>
    public enum ThroupleMetric
    {
        Position,            // centroid > Vector3
        Rotation,            // average or dominant rotation > Quaternion
        Distance,            // 3 sides packed in Vector3 > Vector3
        Angle,               // internal angles of triangle packed in Vector3 > Vector3
        Density,             // distance sum / count > Float
        Movement,            // centroid velocity > Vector3
        Trigger,             // Based on proxy collider > Bool
        RotationAroundAxis,  // Axis would need defining > Float (if possible) or Vector3 containing forward, up and right relatively
        Size,                // The height, width, and depth as Vector3 > Vector3
    }

    //_________________ New Enum ________________________
    
    /// <summary>
    /// Safe Methods for Group Evaluations
    /// </summary>
    public enum GroupMetric
    {
        Position,            // group centroid > Vector3
        Rotation,            // average or representative > Quaternion
        Density,             // distance sum / count > Float
        Size,                // bounds size or volume, The height, width, and depth > Vector3
        Movement,            // centroid movement and velocity or average movement and velocity of members > Vector3
        Trigger,             // Proxy trigger event > Bool
        RotationAroundAxis,  // Axis would need defining > Float (if possible) or Vector3 containing forward, up and right relatively
    }

    //_________________ New Enum ________________________
    
    /// <summary>
    /// Holds the Boolean Status as an enum
    /// </summary>
    public enum RelationshipStatus
    {
        None,
        Over,
        Under,
        InRange,
    }
    
    //_________________ New Enum ________________________
    
    /// <summary>
    /// Options for evaluation curve that modify the normalized value
    /// </summary>
    public enum LemmingCurveType
    {
        Linear, // straight interpolation
        EaseIn, 
        EaseOut, 
        EaseInOut, // snappier variation of Sine curve
        SineIn,
        SineOut,
        SineInOut, // Smoothest transition
        QuadraticIn, 
        QuadraticOut,
        QuadraticInOut, // Sharper transition
        CubicIn,
        CubicOut,
        CubicInOut, // Even Sharper transition
        ExponentialIn, 
        ExponentialOut, 
        ExponentialInOut, // Sharpest transition
        Custom,  // fallback to user-assigned AnimationCurve
    }
    
    #region Metric Setting Enums
    
    /// <summary>
    /// This only applies to Throuples
    /// </summary>
    public enum DistanceOptions
    {
        CombinedAsVector3,       
        LeaderToFollower,
        FollowerToThird,
        ThirdToLeader,
        TotalValue
    }
    
    //_________________ New Enum ________________________

    public enum SingleAxis
    {
        X,
        Y,
        Z
    }
        
    //_________________ New Enum ________________________

    public enum DistanceUnit
    {
        Meters,
        Centimeters,
        Inches,
        Feet
    }
    
    //_________________ New Enum ________________________

    public enum AxisSelection
    {

        Up,
        Forward,
        Right,
        ObjectToCenter,
        Custom
    }
    
    //_________________ New Enum ________________________

    public enum AxisSelectionThrouple
    {
        CenterThroughLeader,
        CenterThroughThird,
        CenterThroughFollower,
    }
    
    //_________________ New Enum ________________________

    public enum DensityMethod
    {
        AverageFromCenter,
        TotalFromCenter
    }
    
    //_________________ New Enum ________________________

    public enum SizeMethod
    {
        UseHeightWidthDepth,
        RadiusFromCenter
    }
    #endregion
    
}
