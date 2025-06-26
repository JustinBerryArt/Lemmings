using System;
using System.Collections.Generic;
//using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace Lemmings
{
    
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public class LemmingInterfaces{}
    
    //______________________________________________________

    /// <summary>
    /// This is the interface for the relationship
    /// TODO: Determine if this is neccesary
    /// TODO: Right now it is not really utilized but might have value in building virtual relationships
    /// </summary>
    public interface ILemmingRelationship
    {
        List<LemmingReference> References { get; }
        FamilyType            Family     { get; }
        Enum                  Metric     { get; }
        int Count => References.Count;
        float Min { get; }
        float Max { get; }
        LemmingCurveType      CurveType  { get; }
        AnimationCurve        CustomCurve{ get; }
        
        /// <summary>Fire this whenever ToDatum() or any core parameter changes.</summary>
        event Action Updated;
        
        ILemmingConverter ToConverter();
        LemmingDatum SetValue();
    }
    

    /// <summary>
    /// Interface for any reference to a tracked Lemming.
    /// </summary>
    public interface ILemming
    {
        /// <summary>
        /// The assigned name or label of the Lemming.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The GameObject associated with the Lemming.
        /// </summary>
        GameObject Source { get; }

        /// <summary>
        /// Returns the confidence score for this Lemming, from 0 to 1.
        /// </summary>
        float GetConfidence();
    }
    
    public interface ILemmingConverter
    {
        object Raw { get; }
        float Normalized { get; }
        float ToCurve(AnimationCurve curve);
        
        /// <summary>True if the normalized value exceeded 1.0 before clamping.</summary>
        bool Over { get; }

        /// <summary>True if the normalized value fell below 0.0 before clamping.</summary>
        bool Under { get; }

        /// <summary>True if the value landed in [0,1] (inclusive) after normalization.</summary>
        bool InRange { get; }
        
        /// <summary>Converts Normalize value to -1 to 1 for easier input mapping and referencing relative values.</summary>
        float AsAxis { get; }
    }
    
    
}