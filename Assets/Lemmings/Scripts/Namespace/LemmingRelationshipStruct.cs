using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Lemmings.UI;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lemmings
{
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    
    #region Lemming Setting Class
    
    //TODO: This is a class but a conversion to a struct might improve some editing options
    
    /// <summary>
    /// This is a class with numerous optional variables.
    /// This can be used to pass along settings for specific methods when appropriate.
    /// This is meant to be extensible for holding any setting options.
    ///
    /// Any new variable added to this need to established as being allowed as null.
    /// You can do this with the ? after the type or with the [CanBeNull] [ItemCanBeNull] attributes
    /// 
    /// Example Usage:
    ///     var setting = new LemmingRelationSetting
    ///     { referenceVector = Vector3.right };
    ///     var result = relationship.MyRelation.Evaluate(context, setting);
    /// </summary>
    [System.Serializable]
    public class LemmingRelationSetting
    {
        /// <summary>
        /// Proxy used for trigger and gaze interactions
        /// </summary>
        public LemmingRelationshipProxy proxy;
        
        /// <summary>
        /// Used to select a distance calculation choice for throuples
        /// </summary>
        public DistanceOptions distanceOptions;
        
        /// <summary>
        /// Used to set the distance with unit conversions, such as inches or centimeters
        /// </summary>
        public DistanceUnit distanceUnit;
        
        /// <summary>
        /// This is used if a value is to be determined relative to some other game object, 
        /// such as the main camera or the player.
        /// </summary>
        public bool relativeToObject;

        /// <summary>
        /// This is an object field which can be used to select a game object to be referenced..
        /// </summary>
        public GameObject objectToReference;

        /// <summary>
        /// This is to set the relationship to trigger if the proxy's forward vector is landing on a tagged object.
        /// </summary>
        public bool useGazeFromProxy;
        
        /// <summary>
        /// This is set true to receive velocity information as a float
        /// </summary>
        public bool magnitudeOnly;

        /// <summary>
        /// This is set true to receive velocity information normalized to provide directional information only
        /// </summary>
        public bool directionOnly;

        /// <summary>
        /// This is set true to receive velocity indicating how fast members are moving towards each other
        /// or away from each other.
        /// </summary>
        public bool relativeToMembers;

        /// <summary>
        /// This is enum is used to select which axis to use as a reference for a signed angle calculation.
        /// used when you want rotation to act as a trigger but want to avoid quaternions.
        /// </summary>
        public AxisSelection axisSelection;

        /// <summary>
        /// This is a boolean to set if the value or direction should be inverted.
        /// </summary>
        public bool invert;
        
        /// <summary>
        /// This is enum is used to select which axis to use as a reference for a signed angle calculation.
        /// used when you want rotation to act as a trigger but want to avoid quaternions.
        /// Throuples have unique options relative to their members.
        /// </summary>
        public AxisSelectionThrouple axisSelectionThrouple;

        /// <summary>
        /// This is enum is used to select which method to use for calculating density
        /// </summary>
        public DensityMethod densityMethod;

        /// <summary>
        /// This is enum is used to select which method to use for calculating size
        /// </summary>
        public SizeMethod sizeMethod;

        /// <summary>
        /// This is set to true if you want data to come in as a single axis, such as the Y axis for position
        /// </summary>
        public bool useSingleAxis;
        
        /// <summary>
        /// This is for selecting a single axis to isolate from a vector 3
        /// </summary>
        public SingleAxis singleAxis;
        
        /// <summary>
        /// This is for holding a custom reference vector
        /// </summary>
        public Vector3 referenceVector;
        
        /// <summary>
        /// This is for holding a custom rotation reference vector
        /// </summary>
        public Vector3 rotationAxis;
        
        /// <summary>
        /// This is for setting a threshold for establishing a potential boolean setting
        /// </summary>
        public float threshold;
        
        public bool IsBool =>
            useGazeFromProxy == true;

        public bool IsFloat =>
            useSingleAxis == true || magnitudeOnly == true || distanceOptions == DistanceOptions.FollowerToThird || distanceOptions == DistanceOptions.LeaderToFollower || distanceOptions == DistanceOptions.ThirdToLeader || distanceOptions == DistanceOptions.TotalValue || sizeMethod == SizeMethod.RadiusFromCenter;

        public bool IsVector3 =>
            directionOnly == true;

        
        public List<MetricSettingInfo> ToMetricSettingsList(FamilyType family, Enum metric)
        {
            var list = new List<MetricSettingInfo>();

            void AddSlider(string label, string desc, float value)
            {
                list.Add(new MetricSettingInfo
                {
                    displayName = label,
                    description = desc,
                    controlType = SettingType.Slider,
                    floatValue = value
                });
            }

            void AddToggle(string label, string desc, bool value)
            {
                list.Add(new MetricSettingInfo
                {
                    displayName = label,
                    description = desc,
                    controlType = SettingType.Toggle,
                    boolValue = value
                });
            }

            void AddDropdown<TEnum>(string label, string desc, TEnum selected) where TEnum : Enum
            {
                list.Add(new MetricSettingInfo
                {
                    displayName = label,
                    description = desc,
                    controlType = SettingType.Dropdown,
                    options = Enum.GetNames(typeof(TEnum)).ToList(),
                    selectedIndex = Convert.ToInt32(selected)
                });
            }

            // Shared settings
            if (threshold > 0f)
                AddSlider("Threshold", "Minimum value required before activation", threshold);

            if (useSingleAxis)
                AddToggle("Use Single Axis", "Restrict output to a single axis", useSingleAxis);

            switch (family)
            {
                case FamilyType.Single:
                    switch ((SingleMetric)metric)
                    {
                        case SingleMetric.Position:
                            AddDropdown("Axis", "Axis to extract value from", singleAxis);
                            break;

                        case SingleMetric.Movement:
                            AddDropdown("Axis", "Axis to extract movement", singleAxis);
                            AddToggle("Magnitude Only", "Ignore direction, only use speed", magnitudeOnly);
                            AddToggle("Relative to Object", "Compare movement to another object", relativeToObject);
                            break;

                        case SingleMetric.Rotation:
                            AddToggle("Use Proxy Gaze", "Use proxy's gaze instead of object's forward", useGazeFromProxy);
                            AddSlider("Threshold", "Minimum rotation required", threshold);
                            break;
                    }
                    break;

                case FamilyType.Couple:
                    switch ((CoupleMetric)metric)
                    {
                        case CoupleMetric.Distance:
                            AddDropdown("Distance Unit", "Unit for measuring distance", distanceUnit);
                            break;

                        case CoupleMetric.Position:
                        case CoupleMetric.Difference:
                            AddDropdown("Axis", "Axis to evaluate difference", singleAxis);
                            break;

                        case CoupleMetric.Movement:
                            AddToggle("Relative to Object", "Movement compared to external object", relativeToObject);
                            AddToggle("Magnitude Only", "Only use movement magnitude", magnitudeOnly);
                            break;

                        case CoupleMetric.Rotation:
                            AddDropdown("Axis Selection", "Primary axis to extract from rotation", axisSelection);
                            AddToggle("Invert", "Reverse rotational direction", invert);
                            break;
                    }
                    break;

                case FamilyType.Throuple:
                    switch ((ThroupleMetric)metric)
                    {
                        case ThroupleMetric.Angle:
                        case ThroupleMetric.Size:
                        case ThroupleMetric.Position:
                            AddDropdown("Axis", "Axis to extract from shape", singleAxis);
                            break;

                        case ThroupleMetric.Density:
                            AddDropdown("Density Method", "How to calculate density", densityMethod);
                            AddDropdown("Distance Unit", "Distance unit", distanceUnit);
                            break;

                        case ThroupleMetric.RotationAroundAxis:
                            AddDropdown("Axis Selection", "Which axis to rotate around", axisSelection);
                            AddDropdown("Axis Logic", "Center-through direction used", axisSelectionThrouple);
                            AddToggle("Invert", "Invert direction of evaluation", invert);
                            break;
                    }
                    break;

                case FamilyType.Group:
                    switch ((GroupMetric)metric)
                    {
                        case GroupMetric.Size:
                        case GroupMetric.Position:
                            AddDropdown("Axis", "Single axis to extract", singleAxis);
                            break;

                        case GroupMetric.Movement:
                            AddToggle("Magnitude Only", "Ignore direction", magnitudeOnly);
                            AddToggle("Relative to Members", "Movement relative to group center", relativeToMembers);
                            break;

                        case GroupMetric.Density:
                            AddDropdown("Density Method", "How density is calculated", densityMethod);
                            AddDropdown("Distance Unit", "Unit for distance", distanceUnit);
                            break;

                        case GroupMetric.RotationAroundAxis:
                            AddDropdown("Axis Selection", "Axis to rotate around", axisSelection);
                            break;
                    }
                    break;
            }

            return list;
        }
        
        
        public override string ToString()
        {
            List<string> activeFields = new();

            void AddIfSet(string label, object value)
            {
                if (value is bool b && b) activeFields.Add($"{label}: true");
                else if (value is Enum e && !Equals(e, default(Enum))) activeFields.Add($"{label}: {e}");
                else if (value is float f && Math.Abs(f) > 0.0001f) activeFields.Add($"{label}: {f}");
                else if (value is Vector3 v && v != Vector3.zero) activeFields.Add($"{label}: {v}");
                else if (value != null) activeFields.Add($"{label}: {value}");
            }

            AddIfSet("useSingleAxis", useSingleAxis);
            AddIfSet("singleAxis", singleAxis);
            AddIfSet("relativeToObject", relativeToObject);
            AddIfSet("relativeToMembers", relativeToMembers);
            AddIfSet("directionOnly", directionOnly);
            AddIfSet("magnitudeOnly", magnitudeOnly);
            AddIfSet("useGazeFromProxy", useGazeFromProxy);
            AddIfSet("threshold", threshold);
            AddIfSet("proxy", proxy);
            AddIfSet("objectToReference", objectToReference);
            AddIfSet("axisSelection", axisSelection);
            AddIfSet("axisSelectionThrouple", axisSelectionThrouple);
            AddIfSet("rotationAxis", rotationAxis);
            AddIfSet("invert", invert);
            AddIfSet("distanceUnit", distanceUnit);
            AddIfSet("distanceOptions", distanceOptions);
            AddIfSet("densityMethod", densityMethod);
            AddIfSet("sizeMethod", sizeMethod);

            return activeFields.Count > 0
                ? $"Active Settings:\n- " + string.Join("\n- ", activeFields)
                : "No active settings.";
        }
    }    
    
    #endregion

    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region Lemming Relation Struct
    
    /// <summary>
    /// Represents a context for computing a spatial relationship metric
    /// across a given list of lemming GameObjects and family structure.
    /// Supports Single, Couple, Throuple, and Group types with metric-specific logic.
    /// </summary>
    public struct LemmingRelation
    {
        /// <summary>
        /// Defines the structural type of the relationship (e.g., Single, Couple, etc).
        /// </summary>
        public FamilyType Family;

        /// <summary>
        /// The GameObjects involved in the spatial relationship.
        /// Expected to be size-matched to the FamilyType.
        /// Pulls directly from References or is defined when the struct is initialized
        /// </summary>
        public List<GameObject> Members => References
            .Where(r => r.Source != null)
            .Select(r => r.Source)
            .ToList();

        /// <summary>
        /// The Full Lemming Reference entry for getting reliable access.
        /// Uses 'Get' so that a LemmingRelation can be used with basic game objects
        /// </summary>
        public List<LemmingReference> References { get; }
        
        /// <summary>
        /// This contains settings for how to process various metrics
        /// </summary>
        public LemmingRelationSetting Settings { get; }
        
        
        /// <summary>
        /// The metric enum specific to the FamilyType that describes the desired output.
        /// </summary>
        public Enum Metric;

        /// <summary>
        /// Constructs a new relation context.
        /// This preffered method uses the full LemmingReference data which is more robust
        /// </summary>
        /// <param name="family">The type of relationship (e.g. Couple, Group).</param>
        /// <param name="references">The Lemming References involved in the relationship.</param>
        /// <param name="metric">The metric enum that defines what value to compute.</param>
        public LemmingRelation(FamilyType family, List<LemmingReference> references, Enum metric, LemmingRelationSetting settings)
        {
            Family = family;
            References = references ?? new List<LemmingReference>();
            Metric = metric;
            Settings = settings ?? new LemmingRelationSetting();
        }

        //--------------------------------- Method Break -------------------------------  << << << <<
        
        
        /// <summary>
        /// This constructor is for quick and dirty implementation in isolated cases.
        /// Developers might want to establish a Lemming Relationship for utility with non Lemming Game Objects
        /// </summary>
        /// <param name="family">The type of relationship (e.g. Couple, Group).</param>
        /// <param name="gameObjects">The GameObjects being mapped as a spatial relationship.</param>
        /// <param name="metric">The metric enum that defines what value to compute.</param>
        public LemmingRelation(FamilyType family, List<GameObject> gameObjects, Enum metric,  LemmingRelationSetting settings)
        {
            Family = family;
            // A reference list is needed and this will create it using default settings with the gameObjects
            References = gameObjects?
                // run through the game objects and make them Lemming References
                .Select(thisObject => new LemmingReference
                {
                    name = thisObject?.name ?? "<unnamed>",
                    Source = thisObject,
                    confidence = 1f
                    // compile these Lemming References into a list
                }).ToList() ?? new List<LemmingReference>();
            Metric = metric;
            Settings = settings ?? new LemmingRelationSetting();
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<

        
        /// <summary>
        /// Evaluates the selected metric for the current family type and member set.
        /// Routes to an internal handler method based on FamilyType.
        /// Has an optional 'setting' parameter for passing optional settings.
        /// </summary>
        /// <returns>The computed result for the selected metric, boxed as an object.</returns>
        public object Evaluate(LemmingRelationSetting setting = null)
        {
            LemmingRelationSetting evalationSetting = new LemmingRelationSetting();
            // if no setting is used is passed, build an empty one to avoid null results
            if (setting != null)
            {
                evalationSetting = setting;
            } else evalationSetting = Settings;

            return Family switch
            {
                FamilyType.Single => EvaluateSingle(Metric as SingleMetric? ?? default, evalationSetting),
                FamilyType.Couple => EvaluateCouple(Metric as CoupleMetric? ?? default, evalationSetting),
                FamilyType.Throuple => EvaluateThrouple(Metric as ThroupleMetric? ?? default, evalationSetting),
                FamilyType.Group => EvaluateGroup(Metric as GroupMetric? ?? default, evalationSetting),
                _ => null

            };
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Wraps the result of the evaluation into a LemmingDatum.
        /// Useful for integration into other systems.
        /// </summary>
        /// <returns>A LemmingDatum representing the evaluated value.</returns>
        public LemmingDatum ToDatum()
        {
            
            object result = Evaluate();
            var datum = new LemmingDatum();
            datum.SetValue(result);
            return datum;
        }

        #region Lemming Single Options
        
        /// <summary>
        /// Computes a spatial metric using a single GameObject.
        /// </summary>
        /// <param name="metric">The single-object metric to compute.</param>
        /// <param name="setting"> This can later hold settings if required </param>
        /// <returns>The resulting value for the metric.</returns>
        private object EvaluateSingle(SingleMetric metric, LemmingRelationSetting setting = null)
        {
            //setting ??= new LemmingRelationSetting();
            
            return metric switch
            {
                SingleMetric.Position => setting.IsFloat ? SinglePositionFloat(setting) : SinglePosition(setting),
                SingleMetric.Rotation => setting.IsBool ? SingleRotationBool(setting) : SingleRotation(setting),
                SingleMetric.Movement => setting.IsFloat ? SingleMovementFloat(setting) : SingleMovement(setting), 
                SingleMetric.Trigger  => SingleTrigger(setting),
                _ => null
            };
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the Position datum for a single Lemming Relationship
        /// </summary>
        /// <param name="setting"> This can later hold settings if required </param>
        /// <returns>The position of the Lemming source</returns>
        private Vector3 SinglePosition(LemmingRelationSetting setting = null)
        {
            if (Members.Count == 0 || Members[0] == null)
                return Vector3.zero;

            Vector3 position = Members[0].transform.position;
            
            return position;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the Position datum for a single Lemming Relationship where the value is for a single axis
        /// </summary>
        /// <param name="setting"> This can later hold settings if required </param>
        /// <returns>The value of the Lemming source in a given axis</returns>
        private float SinglePositionFloat(LemmingRelationSetting setting = null)
        {
            if (Members.Count == 0 || Members[0] == null)
                return 0f;
            
            Vector3 position = Members[0].transform.position;
            
            if (setting.useSingleAxis)
            {
                return setting.singleAxis switch
                {
                    SingleAxis.X => position.x,
                    SingleAxis.Y => position.y,
                    SingleAxis.Z => position.z,
                    _ => position.magnitude
                };
            }

            // Default to magnitude if SingleAxis is null for some reason
            return position.magnitude;
        }
                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the rotation datum for a single Lemming Relationship
        /// </summary>
        /// <param name="setting"> This can later hold settings if required </param>
        /// <returns>The rotation of the Lemming source</returns>
        private Quaternion SingleRotation(LemmingRelationSetting setting = null)
        {
            if (Members.Count == 0 || Members[0] == null)
                return Quaternion.identity;
            
            var thisTransform = Members[0]?.transform;
            return thisTransform?.rotation ?? Quaternion.identity;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Returns a boolean based on whether the object is gazing at a tagged object, defined in the relationship proxy
        /// </summary>
        /// <param name="setting"> This hold the proxy and the threshold for establishing gaze </param>
        /// <returns>True if an object is being seen and false if not</returns>
        private bool SingleRotationBool(LemmingRelationSetting setting = null)
        {
            if (setting?.proxy == null || setting.useGazeFromProxy != true)
                return false;

            float threshold = setting.threshold;
            return setting.proxy.IsGazing && setting.proxy.GazeConfidence >= threshold;
        }
                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the movement vector of a single Lemming.
        /// Applies settings for relative reference, direction-only formatting.
        /// </summary>
        /// <param name="setting">Optional configuration of relative reference or formatting.</param>
        /// <returns>Velocity vector of the single Lemming.</returns>
        private Vector3 SingleMovement(LemmingRelationSetting setting = null)
        {
            if (Members.Count == 0 || Members[0] == null)
                return Vector3.zero;

            var lemming = Members[0].GetComponent<Lemming>();
            if (lemming == null) return Vector3.zero;

            Vector3 velocity = lemming.Velocity;

            if (setting?.relativeToObject == true && setting.objectToReference != null)
            {
                var refVel = setting.objectToReference.GetComponent<Lemming>()?.Velocity ?? Vector3.zero;
                velocity -= refVel;
            }

            if (setting?.directionOnly == true)
                return velocity.normalized;

            return velocity;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the float value of a single Lemming's movement.
        /// Supports magnitude, single axis, or relative-to-object comparison.
        /// </summary>
        /// <param name="setting">Optional configuration for formatting or comparison behavior.</param>
        /// <returns>Float value of velocity for a single Lemming.</returns>
        private float SingleMovementFloat(LemmingRelationSetting setting = null)
        {
            Vector3 velocity = SingleMovement(setting);

            if (setting?.magnitudeOnly == true)
                return velocity.magnitude;

            if (setting?.useSingleAxis == true)
            {
                return setting.singleAxis switch
                {
                    SingleAxis.X => velocity.x,
                    SingleAxis.Y => velocity.y,
                    SingleAxis.Z => velocity.z,
                    _ => velocity.magnitude
                };
            }

            return velocity.magnitude;
        }

                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        
        /// <summary>
        /// Evaluates whether the proxy for a Single Lemming relationship is currently triggered.
        /// </summary>
        /// <param name="setting">Must include a proxy reference.</param>
        /// <returns>True if proxy is triggered, false otherwise.</returns>
        private bool SingleTrigger(LemmingRelationSetting setting = null)
        {
            return setting?.proxy?.IsTriggered == true;
        }

        
        #endregion
        
        #region Lemming Couple Options
        
        /// <summary>
        /// Computes a spatial metric for a pair of GameObjects.
        /// </summary>
        /// <param name="metric">The couple-specific metric to evaluate.</param>
        /// <param name="setting"> This can later hold settings if required </param>
        /// <returns>The resulting value, either numeric, vector, or boolean.</returns>
        private object EvaluateCouple(CoupleMetric metric, LemmingRelationSetting setting = null)
        {
            return metric switch
            {
                CoupleMetric.Position           => setting.IsFloat ? CouplePositionFloat(setting) : CouplePosition(setting),
                CoupleMetric.Rotation           => setting.IsBool ? CoupleRotationBool(setting) : CoupleRotation(setting),
                CoupleMetric.Distance           => CoupleDistance(setting),
                CoupleMetric.Movement           => setting.IsFloat ? CoupleMovementFloat(setting) : CoupleMovement(setting), // Placeholder
                CoupleMetric.Difference         => setting.IsFloat ? CoupleDifferenceFloat(setting) : CoupleDifference(setting),
                CoupleMetric.Trigger            => CoupleTrigger(setting),    // Placeholder
                // depreciated methods below
                //CoupleMetric.RotationAroundAxis => CoupleRotationAroundAxis(setting),
                //CoupleMetric.RelativeVelocity => CoupleRelativeVelocity(setting), // Placeholder
                //CoupleMetric.Pose => CouplePose(setting), // Placeholder
                _ => null
            };
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the average world position between two Lemmings.
        /// </summary>
        /// <param name="setting">Optional settings (e.g. axis filtering)</param>
        /// <returns>The average Vector3 position</returns>
        private Vector3 CouplePosition(LemmingRelationSetting setting = null)
        {
            if (Members.Count < 2 || Members[0] == null || Members[1] == null)
                return Vector3.zero;

            Vector3 a = Members[0].transform.position;
            Vector3 b = Members[1].transform.position;
            Vector3 average = (a + b) * 0.5f;

            return average;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the position along a selected axis or magnitude between two Lemmings.
        /// </summary>
        /// <param name="setting">Optional settings (e.g. axis filtering)</param>
        /// <returns>A float representing either a selected axis or the magnitude of the average position</returns>
        private float CouplePositionFloat(LemmingRelationSetting setting = null)
        {
            if (Members.Count < 2 || Members[0] == null || Members[1] == null)
                return 0f;

            Vector3 a = Members[0].transform.position;
            Vector3 b = Members[1].transform.position;
            Vector3 average = (a + b) * 0.5f;

            if (setting?.useSingleAxis == true)
            {
                return setting.singleAxis switch
                {
                    SingleAxis.X => average.x,
                    SingleAxis.Y => average.y,
                    SingleAxis.Z => average.z,
                    _ => average.magnitude
                };
            }

            return average.magnitude;
        }
        
                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the Rotation of a Lemming Couple with multiple axis options.
        /// </summary>
        /// <param name="setting">Optional settings for customizing the rotation axis and behavior.</param>
        /// <returns>A Quaternion representing the orientation of the couple.</returns>
        private Quaternion CoupleRotation(LemmingRelationSetting setting = null)
        {
            if (Members.Count < 2 || Members[0] == null || Members[1] == null)
                return Quaternion.identity;

            Vector3 leader = Members[0].transform.position;
            Vector3 follower = Members[1].transform.position;

            // Compute the forward direction from follower to leader
            Vector3 forward = (leader - follower).normalized;
            if (forward == Vector3.zero) forward = Vector3.forward;

            // Determine up vector based on the AxisSelection
            Vector3 up = Vector3.up;
            switch (setting?.axisSelection ?? AxisSelection.Up)
            {
                case AxisSelection.Up:
                    up = Vector3.up;
                    break;
                case AxisSelection.Right:
                    up = Vector3.right;
                    break;
                case AxisSelection.Forward:
                    up = Vector3.forward;
                    break;
                case AxisSelection.ObjectToCenter:
                    if (setting?.objectToReference != null)
                    {
                        Vector3 center = (leader + follower) * 0.5f;
                        up = (center - setting.objectToReference.transform.position).normalized;
                    }
                    break;
                case AxisSelection.Custom:
                    
                    up = setting.rotationAxis;
                    break;
            }
  
            // invert direction
            if (setting?.invert == true)
            {
                forward *= -1f;
            }
        

            return Quaternion.LookRotation(forward, up);
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Optional boolean output when rotation acts as a condition (e.g. gaze).
        /// </summary>
        /// <param name="setting">Setting must include a proxy reference and optionally a confidence threshold.</param>
        /// <returns>True if the proxy is currently gazing at a valid target with sufficient confidence.</returns>
        private bool CoupleRotationBool(LemmingRelationSetting setting = null)
        {
            if (setting?.proxy == null || setting.useGazeFromProxy != true)
                return false;

            float threshold = setting.threshold;
            return setting.proxy.Data.GazeConfidence >= threshold;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// The distance between the two Lemmings
        /// </summary>
        /// <param name="setting">Can choose to select a relevant scale</param>
        /// <returns>The distance between the two Lemmings</returns>
        private float CoupleDistance(LemmingRelationSetting setting = null)
        {
            if (Members.Count < 2 || Members[0] == null || Members[1] == null)
                return 0f;
            
            DistanceUnit unit = setting?.distanceUnit ?? DistanceUnit.Meters;
            
            float raw = Vector3.Distance(Members[0].transform.position, Members[1].transform.position);

            float scale = unit switch
            {
                DistanceUnit.Meters => 1,
                DistanceUnit.Centimeters => 100f,
                DistanceUnit.Inches => 39.3701f,
                DistanceUnit.Feet => 3.28084f,
                _ => 1
            };
            
            return raw * scale;
        }
                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the average velocity of a Lemming Couple.
        /// Applies optional settings: directionOnly, relativeToObject.
        /// </summary>
        /// <param name="setting">Optional settings for axis filtering or reference adjustment.</param>
        /// <returns>Vector3 representing couple's motion.</returns>
        private Vector3 CoupleMovement(LemmingRelationSetting setting = null)
        {
            if (Members.Count < 2 || Members[0] == null || Members[1] == null)
                return Vector3.zero;

            var lemmingA = Members[0].GetComponent<Lemming>();
            var lemmingB = Members[1].GetComponent<Lemming>();

            Vector3 velocityA = lemmingA?.Velocity ?? Vector3.zero;
            Vector3 velocityB = lemmingB?.Velocity ?? Vector3.zero;

            if (setting?.relativeToObject == true && setting.objectToReference != null)
            {
                var refLemming = setting.objectToReference.GetComponent<Lemming>();
                var refVel = refLemming?.Velocity ?? Vector3.zero;
                velocityA -= refVel;
                velocityB -= refVel;
            }

            Vector3 average = (velocityA + velocityB) * 0.5f;

            if (setting?.directionOnly == true)
                return average.normalized;

            return average;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the float representation of a Couple's movement.
        /// Supports magnitude, axis extraction, and relative-to-each-other projection.
        /// </summary>
        /// <param name="setting">Optional settings for formatting or projection.</param>
        /// <returns>Float representing motion of couple.</returns>
        private float CoupleMovementFloat(LemmingRelationSetting setting = null)
        {
            if (setting?.relativeToMembers == true && Members.Count == 2)
            {
                var lemmingA = Members[0].GetComponent<Lemming>();
                var lemmingB = Members[1].GetComponent<Lemming>();
                if (lemmingA == null || lemmingB == null) return 0f;

                Vector3 direction = (Members[1].transform.position - Members[0].transform.position).normalized;
                Vector3 relativeVelocity = lemmingB.Velocity - lemmingA.Velocity;

                return Vector3.Dot(relativeVelocity, direction);
            }

            Vector3 velocity = CoupleMovement(setting);

            if (setting?.magnitudeOnly == true)
                return velocity.magnitude;

            if (setting?.useSingleAxis == true)
            {
                return setting.singleAxis switch
                {
                    SingleAxis.X => velocity.x,
                    SingleAxis.Y => velocity.y,
                    SingleAxis.Z => velocity.z,
                    _ => velocity.magnitude
                };
            }

            return velocity.magnitude;
        }
        
                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Computes the difference vector from the first to the second member of a Couple.
        /// </summary>
        /// <param name="setting">Optional axis filter settings.</param>
        /// <returns>Vector3 direction from Member[0] to Member[1].</returns>
        private Vector3 CoupleDifference(LemmingRelationSetting setting = null)
        {
            if (Members.Count < 2 || Members[0] == null || Members[1] == null)
                return Vector3.zero;

            return Members[1].transform.position - Members[0].transform.position;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Computes a scalar representing the magnitude or axis value of the difference vector between Couple members.
        /// </summary>
        /// <param name="setting">Optional single axis filtering.</param>
        /// <returns>Float representing direction difference along selected axis or full magnitude.</returns>
        private float CoupleDifferenceFloat(LemmingRelationSetting setting = null)
        {
            Vector3 diff = CoupleDifference(setting);

            if (setting?.useSingleAxis == true)
            {
                return setting.singleAxis switch
                {
                    SingleAxis.X => diff.x,
                    SingleAxis.Y => diff.y,
                    SingleAxis.Z => diff.z,
                    _ => diff.magnitude
                };
            }

            return diff.magnitude;
        }
                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Evaluates whether the proxy for a Couple relationship is currently triggered.
        /// </summary>
        /// <param name="setting">Must include a proxy reference.</param>
        /// <returns>True if proxy is triggered, false otherwise.</returns>
        private bool CoupleTrigger(LemmingRelationSetting setting = null)
        {
            if (setting.proxy != null)
                return setting.proxy.IsTriggered == true;
            else return false;
            
        }

        #endregion
        
        #region Lemming Throuple Options
        
        /// <summary>
        /// Computes a spatial metric across three GameObjects.
        /// </summary>
        /// <param name="metric">The throuple-specific metric to evaluate.</param>
        /// <param name="setting"> Optional settings for evaluation </param>
        /// <returns>The computed result specific to the chosen metric.</returns>
        private object EvaluateThrouple(ThroupleMetric metric, LemmingRelationSetting setting = null)
        {
            return metric switch
            {
                ThroupleMetric.Position           => setting.IsFloat ? ThrouplePositionFloat(setting) : ThrouplePosition(setting),
                ThroupleMetric.Rotation           => setting.IsBool ? ThroupleRotationBool(setting) : ThroupleRotation(setting), // Placeholder
                ThroupleMetric.Distance           => setting.IsFloat ? ThroupleDistanceFloat(setting) : ThroupleDistance(setting),
                ThroupleMetric.Angle              => setting.IsFloat ? ThroupleAngleFloat(setting) : ThroupleAngle(setting), // Placeholder
                ThroupleMetric.Density            => ThroupleDensity(setting),
                ThroupleMetric.Movement           => setting.IsFloat ? ThroupleMovementFloat(setting) : ThroupleMovement(setting), // Placeholder
                ThroupleMetric.Trigger            => ThroupleTrigger(setting),    // Placeholder
                ThroupleMetric.RotationAroundAxis => ThroupleRotationAroundAxis(setting), // Placeholder
                ThroupleMetric.Size               => setting.IsFloat ? ThroupleSizeFloat(setting) : ThroupleSize(setting),
                // depreciated methods below
                //ThroupleMetric.Pose => ThrouplePose(setting), // Placeholder
                //ThroupleMetric.Spread => ThroupleSpread(setting),
                //ThroupleMetric.Weighting => ThroupleWeighting(setting),
                _ => null
            };
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the average world position across all three Lemmings.
        /// </summary>
        /// <param name="setting">Optional settings (not currently used)</param>
        /// <returns>The average Vector3 position</returns>
        private Vector3 ThrouplePosition(LemmingRelationSetting setting = null)
        {
            if (Members.Count < 3 || Members[0] == null || Members[1] == null || Members[2] == null)
                return Vector3.zero;

            Vector3 a = Members[0].transform.position;
            Vector3 b = Members[1].transform.position;
            Vector3 c = Members[2].transform.position;

            return (a + b + c) / 3f;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the averaged position across all three Lemmings as a float, filtered by axis if set.
        /// </summary>
        /// <param name="setting">Optional settings for axis filtering</param>
        /// <returns>A float value representing a single axis, or magnitude, of the average position</returns>
        private float ThrouplePositionFloat(LemmingRelationSetting setting = null)
        {
            if (Members.Count < 3 || Members[0] == null || Members[1] == null || Members[2] == null)
                return 0f;

            Vector3 a = Members[0].transform.position;
            Vector3 b = Members[1].transform.position;
            Vector3 c = Members[2].transform.position;

            Vector3 average = (a + b + c) / 3f;

            if (setting?.useSingleAxis == true)
            {
                return setting.singleAxis switch
                {
                    SingleAxis.X => average.x,
                    SingleAxis.Y => average.y,
                    SingleAxis.Z => average.z,
                    _ => average.magnitude
                };
            }

            return average.magnitude;
        }
                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the Rotation of a Lemming Throuple with defined axis rules.
        /// </summary>
        /// <param name="setting">Settings for controlling axis direction and style.</param>
        /// <returns>A Quaternion describing the orientation based on throuple layout.</returns>
        private Quaternion ThroupleRotation(LemmingRelationSetting setting = null)
        {
            if (Members.Count < 3 || Members.Any(m => m == null))
                return Quaternion.identity;

            Vector3 a = Members[0].transform.position;
            Vector3 b = Members[1].transform.position;
            Vector3 c = Members[2].transform.position;

            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            switch (setting?.axisSelectionThrouple ?? AxisSelectionThrouple.CenterThroughLeader)
            {
                case AxisSelectionThrouple.CenterThroughLeader:
                    forward = (a - ((b + c) * 0.5f)).normalized;
                    right = (c - b).normalized;
                    break;
                case AxisSelectionThrouple.CenterThroughThird:
                    forward = (c - ((a + b) * 0.5f)).normalized;
                    right = (a - b).normalized;
                    break;
                case AxisSelectionThrouple.CenterThroughFollower:
                    forward = (b - ((a + c) * 0.5f)).normalized;
                    right = (a - c).normalized;
                    break;
            }

            if (setting?.invert == true)
                right *= -1f;

            Vector3 up = Vector3.Cross(forward, right).normalized;
            if (up == Vector3.zero) up = Vector3.up;

            return Quaternion.LookRotation(forward, up);
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Optional boolean output when throuple rotation is acting as a condition (e.g. gaze).
        /// </summary>
        /// <param name="setting">Setting must include a proxy reference and optionally a confidence threshold.</param>
        /// <returns>True if the proxy is currently gazing at a valid target with sufficient confidence.</returns>
        private bool ThroupleRotationBool(LemmingRelationSetting setting = null)
        {
            if (setting?.proxy == null || setting.useGazeFromProxy != true)
                return false;

            float threshold = setting.threshold;
            return setting.proxy.Data.GazeConfidence >= threshold;
        }
                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        private Vector3 ThroupleDistance(LemmingRelationSetting setting = null)
        {
            var a = Members[0].transform.position;
            var b = Members[1].transform.position;
            var c = Members[2].transform.position;
            
            DistanceUnit unit = setting?.distanceUnit ?? DistanceUnit.Meters;
            
            float scale = unit switch
            {
                DistanceUnit.Meters => 1,
                DistanceUnit.Centimeters => 100f,
                DistanceUnit.Inches => 39.3701f,
                DistanceUnit.Feet => 3.28084f,
                _ => 1
            };
            
            
            return new Vector3(
                Vector3.Distance(a, b) * scale,
                Vector3.Distance(b, c) * scale,
                Vector3.Distance(c, a) * scale
            );
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get a float representation of Throuple distance based on selected distance option.
        /// Supports unit scaling via DistanceUnit.
        /// </summary>
        /// <param name="setting">Optional setting to choose pair type and unit format.</param>
        /// <returns>Float distance scaled to unit and selected configuration.</returns>
        private float ThroupleDistanceFloat(LemmingRelationSetting setting = null)
        {
            if (Members.Count < 3 || Members.Any(m => m == null))
                return 0f;

            Vector3 a = Members[0].transform.position;
            Vector3 b = Members[1].transform.position;
            Vector3 c = Members[2].transform.position;

            float scale = (setting?.distanceUnit ?? DistanceUnit.Meters) switch
            {
                DistanceUnit.Meters => 1f,
                DistanceUnit.Centimeters => 100f,
                DistanceUnit.Inches => 39.3701f,
                DistanceUnit.Feet => 3.28084f,
                _ => 1f
            };

            float distance = setting?.distanceOptions switch
            {
                DistanceOptions.LeaderToFollower => Vector3.Distance(a, b),
                DistanceOptions.FollowerToThird => Vector3.Distance(b, c),
                DistanceOptions.ThirdToLeader => Vector3.Distance(c, a),
                DistanceOptions.TotalValue => 
                    (Vector3.Distance(a, b) + Vector3.Distance(b, c) + Vector3.Distance(c, a)) / 3f,
                _ => Vector3.Distance(a, b) // Default
            };

            return distance * scale;
        }
                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Computes the angles between the three members of the Throuple, giving the corner angles at each member.
        /// </summary>
        /// <param name="setting">Optional axis filtering or distance unit scaling.</param>
        /// <returns>A Vector3 where X = angle at the Leader position, Y = angle at the Follower position, Z = angle at the Third position.</returns>
        private Vector3 ThroupleAngle(LemmingRelationSetting setting = null)
        {
            if (Members.Count < 3 || Members.Any(m => m == null))
                return Vector3.zero;

            Vector3 a = Members[0].transform.position;
            Vector3 b = Members[1].transform.position;
            Vector3 c = Members[2].transform.position;

            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 bc = c - b;
            Vector3 ba = a - b;
            Vector3 ca = a - c;
            Vector3 cb = b - c;

            float angleA = Vector3.Angle(ab, ac);
            float angleB = Vector3.Angle(bc, ba);
            float angleC = Vector3.Angle(ca, cb);

            return new Vector3(angleA, angleB, angleC);
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Computes a float value representing one of the angles in the Throuple, based on axis preference.
        /// </summary>
        /// <param name="setting">Optional single axis selection. Defaults to average if none.</param>
        /// <returns>Float angle in degrees.</returns>
        private float ThroupleAngleFloat(LemmingRelationSetting setting = null)
        {
            Vector3 angles = ThroupleAngle(setting);

            if (setting?.useSingleAxis == true)
            {
                return setting.singleAxis switch
                {
                    SingleAxis.X => angles.x,
                    SingleAxis.Y => angles.y,
                    SingleAxis.Z => angles.z,
                    _ => (angles.x + angles.y + angles.z) / 3f
                };
            }

            return (angles.x + angles.y + angles.z) / 3f;
        }

                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Compute the density of the Throuple based on average or total distance to center.
        /// </summary>
        /// <param name="setting">Optional distance method and unit conversion.</param>
        /// <returns>Density as a float.</returns>
        private float ThroupleDensity(LemmingRelationSetting setting = null)
        {
            if (Members.Count < 3 || Members.Any(m => m == null))
                return 0f;

            Vector3 center = (Members[0].transform.position + Members[1].transform.position + Members[2].transform.position) / 3f;
            float total = Members.Sum(m => Vector3.Distance(center, m.transform.position));

            float result = (setting?.densityMethod ?? DensityMethod.AverageFromCenter) switch
            {
                DensityMethod.TotalFromCenter => total,
                DensityMethod.AverageFromCenter => total / 3f,
                _ => total
            };

            float scale = (setting?.distanceUnit ?? DistanceUnit.Meters) switch
            {
                DistanceUnit.Centimeters => 100f,
                DistanceUnit.Inches => 39.3701f,
                DistanceUnit.Feet => 3.28084f,
                _ => 1f
            };

            return result * scale;
        }
                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the average velocity of a Lemming Throuple.
        /// Applies optional settings: directionOnly, relativeToObject.
        /// </summary>
        /// <param name="setting">Optional settings for formatting or reference adjustment.</param>
        /// <returns>Vector3 representing motion of throuple.</returns>
        private Vector3 ThroupleMovement(LemmingRelationSetting setting = null)
        {
            if (Members.Count < 3 || Members.Any(m => m == null))
                return Vector3.zero;

            Vector3 total = Vector3.zero;
            int count = 0;

            foreach (var m in Members)
            {
                var lemming = m.GetComponent<Lemming>();
                if (lemming == null) continue;

                Vector3 velocity = lemming.Velocity;

                if (setting?.relativeToObject == true && setting.objectToReference != null)
                {
                    var refVel = setting.objectToReference.GetComponent<Lemming>()?.Velocity ?? Vector3.zero;
                    velocity -= refVel;
                }

                total += velocity;
                count++;
            }

            if (count == 0)
                return Vector3.zero;

            Vector3 average = total / count;

            if (setting?.directionOnly == true)
                return average.normalized;

            return average;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get a float representation of Throuple movement.
        /// Supports single axis filtering, magnitude, and center-relative projection.
        /// </summary>
        /// <param name="setting">Optional settings for float formatting or group-relative behavior.</param>
        /// <returns>Float representing movement of the throuple.</returns>
        private float ThroupleMovementFloat(LemmingRelationSetting setting = null)
        {
            if (setting?.relativeToMembers == true && Members.Count == 3)
            {
                Vector3 center = ThrouplePosition(setting);
                float sum = 0f;
                int count = 0;

                foreach (var m in Members)
                {
                    var lemming = m.GetComponent<Lemming>();
                    if (lemming == null) continue;

                    Vector3 toCenter = (center - m.transform.position).normalized;
                    float projection = Vector3.Dot(lemming.Velocity, toCenter);

                    sum += projection;
                    count++;
                }

                return count > 0 ? sum / count : 0f;
            }

            Vector3 velocity = ThroupleMovement(setting);

            if (setting?.magnitudeOnly == true)
                return velocity.magnitude;

            if (setting?.useSingleAxis == true)
            {
                return setting.singleAxis switch
                {
                    SingleAxis.X => velocity.x,
                    SingleAxis.Y => velocity.y,
                    SingleAxis.Z => velocity.z,
                    _ => velocity.magnitude
                };
            }

            return velocity.magnitude;
        }
                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Evaluates whether the proxy for a Throuple relationship is currently triggered.
        /// </summary>
        /// <param name="setting">Must include a proxy reference.</param>
        /// <returns>True if proxy is triggered, false otherwise.</returns>
        private bool ThroupleTrigger(LemmingRelationSetting setting = null)
        {
            return setting?.proxy?.IsTriggered == true;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        //TODO: This might require revision
        /// <summary>
        /// 
        /// Computes the signed angle (in degrees) between the Throuple's defined forward vector and a configurable reference direction,
        /// measured around an axis derived from the member layout.
        /// Useful for detecting twist, turn, or leaning in stable directional setups.
        /// </summary>
        /// <param name="setting">Settings define how the primary and reference axes are selected and whether inversion is applied.</param>
        /// <returns>Signed angle in degrees between forward and reference (0 = aligned, ±180 = fully opposed).</returns>
        private float ThroupleRotationAroundAxis(LemmingRelationSetting setting = null)
        {
            if (Members.Count < 3 || Members.Any(m => m == null))
                return 0f;

            Vector3 a = Members[0].transform.position;
            Vector3 b = Members[1].transform.position;
            Vector3 c = Members[2].transform.position;

            // Define forward and right vectors from axisSelectionThrouple
            Vector3 forward;
            Vector3 right;

            switch (setting?.axisSelectionThrouple ?? AxisSelectionThrouple.CenterThroughLeader)
            {
                case AxisSelectionThrouple.CenterThroughLeader:
                    forward = (a - (b + c) * 0.5f).normalized;
                    right = (c - b).normalized;
                    break;
                case AxisSelectionThrouple.CenterThroughThird:
                    forward = (c - (a + b) * 0.5f).normalized;
                    right = (a - b).normalized;
                    break;
                case AxisSelectionThrouple.CenterThroughFollower:
                    forward = (b - (a + c) * 0.5f).normalized;
                    right = (a - c).normalized;
                    break;
                default:
                    forward = Vector3.forward;
                    right = Vector3.right;
                    break;
            }

            if (forward == Vector3.zero || right == Vector3.zero)
                return 0f;

            if (setting?.invert == true)
                right *= -1f;

            // Use AxisSelection to determine the reference vector
            Vector3 reference = setting?.axisSelection switch
            {
                AxisSelection.Up => Vector3.up,
                AxisSelection.Right => Vector3.right,
                AxisSelection.Forward => Vector3.forward,
                AxisSelection.ObjectToCenter when setting?.objectToReference != null =>
                    ((a + b + c) / 3f - setting.objectToReference.transform.position).normalized,
                AxisSelection.Custom =>
                    setting.rotationAxis.normalized,
                _ => Vector3.right
            };

            return Vector3.SignedAngle(reference, right, forward);
            
        }

        
        
        //--------------------------------- Method Break -------------------------------  << << << <<



        /// <summary>
        /// Get the bounding box size or radius of the Throuple based on selected method.
        /// </summary>
        /// <param name="setting">Optional settings: size method and axis filtering.</param>
        /// <returns>A Vector3 representing the bounding box size or max radius.</returns>
        private Vector3 ThroupleSize(LemmingRelationSetting setting = null)
        {
            if (Members.Count < 3 || Members.Any(m => m == null))
                return Vector3.zero;

            if (setting?.sizeMethod == SizeMethod.RadiusFromCenter)
            {
                Vector3 center = (Members[0].transform.position + Members[1].transform.position + Members[2].transform.position) / 3f;
                float maxDist = Members.Max(m => Vector3.Distance(center, m.transform.position));
                return new Vector3(maxDist, maxDist, maxDist);
            }

            Bounds bounds = new Bounds(Members[0].transform.position, Vector3.zero);
            bounds.Encapsulate(Members[1].transform.position);
            bounds.Encapsulate(Members[2].transform.position);

            return bounds.size;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get a scalar representing the size of the Throuple’s bounding box or max radius, filtered by a single axis.
        /// </summary>
        /// <param name="setting">Optional settings to determine method and axis.</param>
        /// <returns>A float representing the axis-aligned size or radius.</returns>
        private float ThroupleSizeFloat(LemmingRelationSetting setting = null)
        {
            Vector3 size = ThroupleSize(setting);

            if (setting?.useSingleAxis == true)
            {
                return setting.singleAxis switch
                {
                    SingleAxis.X => size.x,
                    SingleAxis.Y => size.y,
                    SingleAxis.Z => size.z,
                    _ => size.magnitude
                };
            }

            return size.magnitude;
        }


        #endregion
        
        #region Lemming Group Options
        
        /// <summary>
        /// Computes a spatial metric across a group of four or more GameObjects.
        /// </summary>
        /// <param name="metric">The group metric to compute.</param>
        /// <param name="setting"> Optional Settings for evaluation </param>
        /// <returns>The computed result, typically a float, Vector3, or Quaternion.</returns>
        private object EvaluateGroup(GroupMetric metric, LemmingRelationSetting setting = null)
        {
            return metric switch
            {
                GroupMetric.Position           => setting.IsFloat ? GroupPositionFloat(setting) : GroupPosition(setting),
                GroupMetric.Rotation           => setting.IsBool ? GroupRotationBool(setting) : GroupRotation(setting), // Placeholder
                GroupMetric.Density            => GroupDensity(setting),
                GroupMetric.Size               => setting.IsFloat ? GroupSizeFloat(setting) : GroupSize(setting),
                GroupMetric.Movement           => setting.IsFloat ? GroupMovementFloat(setting) : GroupMovement(setting), // Placeholder
                GroupMetric.Trigger            => GroupTrigger(setting), // Placeholder
                GroupMetric.RotationAroundAxis => GroupRotationAroundAxis(setting), // Placeholder
                // depreciated methods below
                //GroupMetric.Distance => GroupDistance(setting),
                //GroupMetric.Spread => GroupSpread(setting),
                //GroupMetric.Pose => GroupPose(setting), // Placeholder
                //GroupMetric.Weighting => GroupWeighting(setting),
                _ => null
            };
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Computes the average world position of all valid members in the group.
        /// </summary>
        /// <param name="setting">Optional settings (not currently used)</param>
        /// <returns>The average Vector3 position of all members</returns>
        private Vector3 GroupPosition(LemmingRelationSetting setting = null)
        {
            if (Members == null || Members.Count == 0)
                return Vector3.zero;

            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (var member in Members)
            {
                if (member == null) continue;
                sum += member.transform.position;
                count++;
            }

            return count > 0 ? sum / count : Vector3.zero;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Computes a float representation of the group’s average position,
        /// optionally filtered by axis.
        /// </summary>
        /// <param name="setting">Optional settings for axis filtering</param>
        /// <returns>Float representing axis value or magnitude of average</returns>
        private float GroupPositionFloat(LemmingRelationSetting setting = null)
        {
            if (Members == null || Members.Count == 0)
                return 0f;

            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (var member in Members)
            {
                if (member == null) continue;
                sum += member.transform.position;
                count++;
            }

            if (count == 0)
                return 0f;

            Vector3 average = sum / count;

            if (setting?.useSingleAxis == true)
            {
                return setting.singleAxis switch
                {
                    SingleAxis.X => average.x,
                    SingleAxis.Y => average.y,
                    SingleAxis.Z => average.z,
                    _ => average.magnitude
                };
            }

            return average.magnitude;
        }
        
                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the Rotation for a Lemming Group based on average velocity direction.
        /// </summary>
        /// <param name="setting">Optional setting with invert toggle.</param>
        /// <returns>A Quaternion facing the direction of group motion or identity if no movement is detected.</returns>
        private Quaternion GroupRotation(LemmingRelationSetting setting = null)
        {
            if (Members == null || Members.Count == 0)
                return Quaternion.identity;

            Vector3 totalVelocity = Vector3.zero;
            int count = 0;

            foreach (var m in Members)
            {
                if (m == null) continue;

                var lemming = m.GetComponent<Lemming>();
                if (lemming != null)
                {
                    totalVelocity += lemming.Velocity;
                    count++;
                }
            }

            if (count == 0) return Quaternion.identity;

            Vector3 averageVelocity = totalVelocity / count;
            if (averageVelocity == Vector3.zero) return Quaternion.identity;

            if (setting?.invert == true)
                averageVelocity *= -1f;

            return Quaternion.LookRotation(averageVelocity.normalized, Vector3.up);
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Optional boolean output when group rotation is acting as a condition (e.g. gaze).
        /// </summary>
        /// <param name="setting">Setting must include a proxy reference and optionally a confidence threshold.</param>
        /// <returns>True if the proxy is currently gazing at a valid target with sufficient confidence.</returns>
        private bool GroupRotationBool(LemmingRelationSetting setting = null)
        {
            if (setting?.proxy == null || setting.useGazeFromProxy != true)
                return false;

            float threshold = setting.threshold;
            return setting.proxy.Data.GazeConfidence >= threshold;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<



        /// <summary>
        /// Compute the density of the group using average or total distance to center.
        /// </summary>
        /// <param name="setting">Optional settings including method and unit.</param>
        /// <returns>Float density value.</returns>
        private float GroupDensity(LemmingRelationSetting setting = null)
        {
            if (Members == null || Members.Count == 0)
                return 0f;

            Vector3 center = GroupPosition(setting);
            float total = Members.Sum(m => m != null ? Vector3.Distance(center, m.transform.position) : 0f);

            float result = (setting?.densityMethod ?? DensityMethod.AverageFromCenter) switch
            {
                DensityMethod.TotalFromCenter => total,
                DensityMethod.AverageFromCenter => total / Members.Count,
                _ => total
            };

            float scale = (setting?.distanceUnit ?? DistanceUnit.Meters) switch
            {
                DistanceUnit.Centimeters => 100f,
                DistanceUnit.Inches => 39.3701f,
                DistanceUnit.Feet => 3.28084f,
                _ => 1f
            };

            return result * scale;
        }
                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the Group's bounding box size or radius based on method.
        /// </summary>
        /// <param name="setting">Optional settings.</param>
        /// <returns>Size as a Vector3.</returns>
        private Vector3 GroupSize(LemmingRelationSetting setting = null)
        {
            if (Members == null || Members.Count == 0)
                return Vector3.zero;

            if (setting?.sizeMethod == SizeMethod.RadiusFromCenter)
            {
                Vector3 center = GroupPosition(setting);
                float maxDist = Members.Max(m => m != null ? Vector3.Distance(center, m.transform.position) : 0f);
                return new Vector3(maxDist, maxDist, maxDist);
            }

            Bounds bounds = new Bounds();
            bool initialized = false;

            foreach (var m in Members)
            {
                if (m == null) continue;

                if (!initialized)
                {
                    bounds = new Bounds(m.transform.position, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(m.transform.position);
                }
            }

            return bounds.size;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Get the Group’s size as a float using bounding box or radius, filtered by axis.
        /// </summary>
        /// <param name="setting">Axis filtering and size method options.</param>
        /// <returns>Size as float (magnitude or single axis).</returns>
        private float GroupSizeFloat(LemmingRelationSetting setting = null)
        {
            Vector3 size = GroupSize(setting);

            if (setting?.useSingleAxis == true)
            {
                return setting.singleAxis switch
                {
                    SingleAxis.X => size.x,
                    SingleAxis.Y => size.y,
                    SingleAxis.Z => size.z,
                    _ => size.magnitude
                };
            }

            return size.magnitude;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<



        /// <summary>
        /// Get the average movement velocity for a Lemming group, with support for direction-only or relative motion.
        /// </summary>
        /// <param name="setting">Optional settings controlling normalization and relative calculation.</param>
        /// <returns>Vector3 representing group movement.</returns>
        private Vector3 GroupMovement(LemmingRelationSetting setting = null)
        {
            if (Members == null || Members.Count == 0)
                return Vector3.zero;

            Vector3 totalVelocity = Vector3.zero;
            int count = 0;

            foreach (var m in Members)
            {
                if (m == null) continue;

                var lemming = m.GetComponent<Lemming>();
                if (lemming != null)
                {
                    Vector3 velocity = lemming.Velocity;

                    if (setting?.relativeToObject == true && setting.objectToReference != null)
                    {
                        var referenceVelocity = setting.objectToReference.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;
                        velocity -= referenceVelocity;
                    }

                    totalVelocity += velocity;
                    count++;
                }
            }

            if (count == 0)
                return Vector3.zero;

            Vector3 averageVelocity = totalVelocity / count;

            if (setting?.directionOnly == true)
                return averageVelocity.normalized;

            return averageVelocity;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Optional float override when group movement is reduced to a single axis or magnitude.
        /// Supports relative object comparisons.
        /// </summary>
        /// <param name="setting">Optional settings: single axis, magnitudeOnly, and relative reference.</param>
        /// <returns>Float value of the group movement adjusted to requested setting.</returns>
        private float GroupMovementFloat(LemmingRelationSetting setting = null)
        {
            // If enabled, measure how much members are moving toward/away from group center
            if (setting?.relativeToMembers == true && Members.Count >= 2)
            {
                Vector3 center = GroupPosition(setting);
                float sum = 0f;
                int count = 0;

                foreach (var m in Members)
                {
                    var lemming = m?.GetComponent<Lemming>();
                    if (lemming == null) continue;

                    Vector3 toCenter = (center - m.transform.position).normalized;
                    float projection = Vector3.Dot(lemming.Velocity, toCenter);

                    // Positive = moving toward center, Negative = moving away
                    sum += projection;
                    count++;
                }

                return count > 0 ? sum / count : 0f;
            }

            Vector3 velocity = GroupMovement(setting);

            if (setting?.magnitudeOnly == true)
                return velocity.magnitude;

            if (setting?.useSingleAxis == true)
            {
                return setting.singleAxis switch
                {
                    SingleAxis.X => velocity.x,
                    SingleAxis.Y => velocity.y,
                    SingleAxis.Z => velocity.z,
                    _ => velocity.magnitude
                };
            }

            return velocity.magnitude;
        }
                
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Evaluates whether the proxy for a Group relationship is currently triggered.
        /// </summary>
        /// <param name="setting">Must include a proxy reference.</param>
        /// <returns>True if proxy is triggered, false otherwise.</returns>
        private bool GroupTrigger(LemmingRelationSetting setting = null)
        {
            return setting?.proxy?.IsTriggered == true;
        }
        
        
        //--------------------------------- Method Break -------------------------------  << << << <<


        /// <summary>
        /// Computes the signed angle (in degrees) between the Group's average velocity direction and a reference vector,
        /// measured around a specified axis.
        /// This is helpful for detecting group drift, heading deviation, or coordinated rotation in motion.
        /// </summary>
        /// <param name="setting">Settings control the rotation axis, reference vector selection, and optional inversion.</param>
        /// <returns>Signed angle in degrees (float) based on group motion and selected axis context.</returns>
        private float GroupRotationAroundAxis(LemmingRelationSetting setting = null)
        {
            if (Members == null || Members.Count == 0)
                return 0f;

            Vector3 totalVelocity = Vector3.zero;
            int count = 0;

            foreach (var m in Members)
            {
                var lemming = m?.GetComponent<Lemming>();
                if (lemming == null) continue;

                totalVelocity += lemming.Velocity;
                count++;
            }

            if (count == 0)
                return 0f;

            Vector3 forward = totalVelocity / count;
            if (forward == Vector3.zero)
                return 0f;

            if (setting?.invert == true)
                forward *= -1f;

            Vector3 axis = setting?.rotationAxis ?? Vector3.up;

            // Use AxisSelection to determine reference vector
            Vector3 reference = setting?.axisSelection switch
            {
                AxisSelection.Up => Vector3.up,
                AxisSelection.Right => Vector3.right,
                AxisSelection.Forward => Vector3.forward,
                AxisSelection.ObjectToCenter when setting?.objectToReference != null =>
                    (GroupPosition(setting) - setting.objectToReference.transform.position).normalized,
                AxisSelection.Custom =>
                    setting.rotationAxis.normalized,
                _ => Vector3.right
            };

            return Vector3.SignedAngle(reference, forward.normalized, axis);
        }
        
        
        #endregion
    }
    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------


 
}