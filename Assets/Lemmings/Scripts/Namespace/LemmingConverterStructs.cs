using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Lemmings
{

    //TODO: Some of these conversions really need to be reviewed, especially those that handle Quaternions
    
    
    #region Lemming Booler Struct


    [Serializable]
    public struct LemmingBooler : ILemmingConverter
    {
        public bool rawBool;
        public float min;
        public float max;

        public object Raw => rawBool;

        public float Normalized => Mathf.Clamp01((Value - min) / (max - min));

        public float AsAxis => Normalized * 2f - 1f;

        public bool Over => Value >= max;
        public bool Under => Value <= min;
        public bool InRange => !Over && !Under;

        private float Value => rawBool ? 1f : 0f;

        public LemmingBooler(bool value, float min = 0f, float max = 1f)
        {
            rawBool = value;
            this.min = min;
            this.max = max;
        }

        public float ToCurve(AnimationCurve curve) => curve.Evaluate(Normalized);
    }
    #endregion

    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    
    #region LemmingFloaterCode

    /// <summary>
    /// Represents a normalized floating-point value and provides tools to remap it across data types.
    /// </summary>
    [Serializable]
    public struct LemmingFloater : ILemmingConverter
    {
        
        #region FloatInitialization

        /// <summary>The raw input value to normalize. Meant to update regularly.</summary>
        public float raw;

        public object Raw => raw;
        
        /// <summary>The minimum value of the input range. Can be remotely set.</summary>
        public float min;

        /// <summary>The maximum value of the input range. Can be remotely set.</summary>
        public float max;

        /// <summary>Returns the normalized value of <c>raw</c> within the range [<c>min</c>, <c>max</c>], clamped between 0 and 1.</summary>
        public float Normalized => Mathf.Clamp01((raw - min) / (max - min));

        /// <summary>Returns <c>true</c> if <c>raw</c> is greater than or equal to <c>threshold</c>.</summary>
        public bool Over => raw >= max;

        /// <summary>
        /// Returns true if the raw value is within the specified range.
        /// </summary>
        public bool InRange => !Over && !Under;
        
        /// <summary>Returns <c>true</c> if <c>raw</c> is less than or equal to <c>threshold</c>.</summary>
        public bool Under => raw <= min;

        /// <summary>
        /// This provides the normalized value in a range of -1 to 1, useful for input mapping and relative values
        /// </summary>
        public float AsAxis => Normalized * 2f - 1f;

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Creates a new <see cref="LemmingFloater"/> with the given raw value, range, and threshold.
        /// </summary>
        /// <param name="raw">The raw input value.</param>
        /// <param name="min">The minimum input value.</param>
        /// <param name="max">The maximum input value.</param>
        /// <param name="threshold">The threshold to compare against.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="min"/> and <paramref name="max"/> are equal.</exception>
        public LemmingFloater(float raw, float min, float max)
        {
            if (Mathf.Approximately(min, max))
                throw new ArgumentException("min and max cannot be equal.");

            this.raw = raw;
            this.min = min;
            this.max = max;
        }

        #endregion
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region FloatConversions

        
        
        /// <summary>Remaps the normalized value to an integer range.</summary>
        /// <param name="minimum">Minimum integer output value.</param>
        /// <param name="maximum">Maximum integer output value.</param>
        /// <returns>Integer within [<paramref name="minimum"/>, <paramref name="maximum"/>].</returns>
        public int ToIntRange(int minimum, int maximum)
        {
            return Mathf.FloorToInt(Mathf.Lerp(minimum, maximum + 1, Normalized));
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>Remaps the normalized value to a floating-point range.</summary>
        /// <param name="minimum">Minimum float output value.</param>
        /// <param name="maximum">Maximum float output value.</param>
        /// <returns>Float within [<paramref name="maximum"/>, <paramref name="maximum"/>].</returns>
        public float ToFloatRange(float minimum, float maximum)
        {
            return Mathf.Lerp(minimum, max, Normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>Maps the normalized value to an index in an array of a given size.</summary>
        /// <param name="arrayCount">The size of the array.</param>
        /// <returns>Index from 0 to arrayCount - 1.</returns>
        public int ToIndex(int arrayCount)
        {
            return Mathf.FloorToInt(Normalized * arrayCount);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>Evaluates the normalized value on a Unity <see cref="AnimationCurve"/>.</summary>
        /// <param name="curve">The curve to evaluate.</param>
        /// <returns>The float value of the curve at the normalized position.</returns>
        public float ToCurve(AnimationCurve curve)
        {
            return curve.Evaluate(Normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>Interpolates between two vectors using the normalized value.</summary>
        /// <param name="vectorA">Start vector.</param>
        /// <param name="vectorB">End vector.</param>
        /// <returns>Interpolated <see cref="Vector3"/>.</returns>
        public Vector3 ToVector(Vector3 vectorA, Vector3 vectorB)
        {
            return Vector3.Lerp(vectorA, vectorB, Normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>Returns a label from a list based on the normalized value.</summary>
        /// <param name="labels">Array of labels.</param>
        /// <returns>A string label corresponding to the current normalized value.</returns>
        public string ToLabel(string[] labels)
        {
            int index = Mathf.Clamp(ToIndex(labels.Length), 0, labels.Length - 1);
            return labels[index];
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>Maps the normalized value to a value in a given enum type.</summary>
        /// <typeparam name="T">Enum type to map into.</typeparam>
        /// <returns>The enum value corresponding to the normalized index.</returns>
        public T ToEnum<T>() where T : Enum
        {
            Array values = Enum.GetValues(typeof(T));
            int index = Mathf.FloorToInt(Normalized * values.Length);
            index = Mathf.Clamp(index, 0, values.Length - 1);
            return (T)values.GetValue(index);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Interpolates between two rotations using the normalized value.
        /// </summary>
        /// <param name="a">Start rotation.</param>
        /// <param name="b">End rotation.</param>
        /// <returns>Interpolated <see cref="Quaternion"/>.</returns>
        public Quaternion ToQuaternion(Quaternion a, Quaternion b)
        {
            return Quaternion.Slerp(a, b, Normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Creates a rotation around a given axis by remapping the normalized value to an angle.
        /// </summary>
        /// <param name="axis">Axis to rotate around (must be normalized).</param>
        /// <param name="minAngle">Minimum angle in degrees.</param>
        /// <param name="maxAngle">Maximum angle in degrees.</param>
        /// <returns>A <see cref="Quaternion"/> representing the rotation.</returns>
        public Quaternion ToRotationAroundAxis(Vector3 axis, float minAngle, float maxAngle)
        {
            float angle = ToFloatRange(minAngle, maxAngle);
            return Quaternion.AngleAxis(angle, axis.normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Generates a look rotation from a direction interpolated between two vectors.
        /// </summary>
        /// <param name="from">Start direction.</param>
        /// <param name="to">End direction.</param>
        /// <returns>A look rotation <see cref="Quaternion"/>.</returns>
        public Quaternion ToLookRotation(Vector3 from, Vector3 to)
        {
            Vector3 direction = Vector3.Lerp(from, to, Normalized);
            return Quaternion.LookRotation(direction.normalized);
        }

        #endregion
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region FloatFundamentals

        /// <summary>Evaluates a custom function against the normalized value and returns the result.</summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="function">A function that takes a float (normalized) and returns a value.</param>
        /// <returns>The result of the function.</returns>
        public T EvaluateNormalized<T>(Func<float, T> function)
        {
            return function(Normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>Returns the inverse of the Normalized value.</summary>
        /// <returns>A float from 1 to 0.</returns>
        public float Invert()
        {
            return 1f - Normalized;
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>Returns a string representation of the floater for debugging.</summary>
        public override string ToString()
        {
            return $"Raw: {raw}, Normalized: {Normalized}, Range: [{min}, {max}]";
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>Sets or updates the raw value.</summary>
        /// <param name="newRaw">New raw input.</param>
        public void SetRaw(float newRaw)
        {
            raw = newRaw;
        }
        
        
        //_______________  METHOD BREAK   ___________________//



        /// <summary>Sets the min and max values of the range.</summary>
        /// <param name="newMin">New minimum.</param>
        /// <param name="newMax">New maximum.</param>
        /// <exception cref="ArgumentException">Thrown if newMin equals newMax.</exception>
        public void SetRange(float newMin, float newMax)
        {
            if (Mathf.Approximately(newMin, newMax))
                throw new ArgumentException("min and max cannot be equal.");

            min = newMin;
            max = newMax;
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>Resets all internal values at once.</summary>
        /// <param name="newRaw">New raw value.</param>
        /// <param name="newMin">New minimum value.</param>
        /// <param name="newMax">New maximum value.</param>
        /// <param name="newThreshold">New threshold value.</param>
        /// <exception cref="ArgumentException">Thrown if newMin equals newMax.</exception>
        public void Reset(float newRaw, float newMin, float newMax)
        {
            if (Mathf.Approximately(newMin, newMax))
                throw new ArgumentException("min and max cannot be equal.");

            raw = newRaw;
            min = newMin;
            max = newMax;
        }

        #endregion

    }

    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------
    
    #region LemmingInterCode
    
    /// <summary>
    /// Represents an integer value with normalized remapping, directional interpolation,
    /// and support for flexible out-of-range behavior via <see cref="Lemmings.LemmingInter.RangeBindingMode"/>.
    /// </summary>
    [Serializable]
    public struct LemmingInter : ILemmingConverter
    {
        
        #region LemmingInterInitialization
        
        /// <summary>The current integer value.</summary>
        [FormerlySerializedAs("value")] public int raw;

        public object Raw => raw;
        
        /// <summary>The minimum boundary of the value range.</summary>
        public int min;

        /// <summary>The maximum boundary of the value range.</summary>
        public int max;

        /// <summary>Determines how the value should behave when it exceeds the defined range.</summary>
        public RangeBindingMode bindingMode;

        /// <summary>
        /// Specifies how a value behaves outside its [min, max] range.
        /// </summary>
        public enum RangeBindingMode
        {
            /// <summary>Clamps the value within the min and max bounds.</summary>
            Clamped,

            /// <summary>Cycles upward from max back to min.</summary>
            CycleUp,

            /// <summary>Cycles downward from min back to max.</summary>
            CycleDown,

            /// <summary>Bounces the value back and forth between min and max.</summary>
            Oscillate,

            /// <summary>No bounding is applied; the value is left unmodified.</summary>
            None
        }

        /// <summary>
        /// Returns the current value normalized to a float in [0, 1],
        /// based on the configured min and max values.
        /// </summary>
        public float Normalized => Mathf.Clamp01((float)(raw - min) / (max - min));

        /// <summary>Returns true if the value is greater than or equal to max.</summary>
        public bool Over => raw >= max;

        /// <summary>
        /// Returns true if the value is within the specified range.
        /// </summary>
        public bool InRange => !Over && !Under;
        
        /// <summary>Returns true if the value is less than or equal to min.</summary>
        public bool Under => raw <= min;

        /// <summary>
        /// This provides the normalized value in a range of -1 to 1, useful for input mapping and relative values
        /// </summary>
        public float AsAxis => Normalized * 2f - 1f;
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Initializes a new instance of the <see cref="Lemmings.LemmingInter"/> struct.
        /// </summary>
        /// <param name="raw">The initial value.</param>
        /// <param name="min">Minimum range value.</param>
        /// <param name="max">Maximum range value.</param>
        /// <param name="mode">How the value should behave when out of range.</param>
        /// <exception cref="ArgumentException">Thrown if min and max are equal.</exception>
        public LemmingInter(int raw, int min, int max, RangeBindingMode mode)
        {
            if (min == max)
                throw new ArgumentException("min and max cannot be equal.");

            this.raw = raw;
            this.min = min;
            this.max = max;
            this.bindingMode = mode;
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Applies a specified bounding mode to the current value.
        /// </summary>
        /// <param name="mode">The binding mode to apply. Note that Oscillate requires a state to fully function</param>
        public void ApplyBounds(RangeBindingMode mode)
        {
            switch (mode)
            {
                case RangeBindingMode.None:
                    break;

                case RangeBindingMode.Clamped:
                    raw = Mathf.Clamp(raw, min, max);
                    break;

                case RangeBindingMode.CycleUp:
                {
                    int range = max - min + 1;
                    if (raw > max)
                        raw = ((raw - min) % range) + min;
                    else if (raw < min)
                        raw = max - ((min - raw - 1) % range);
                }
                    break;

                case RangeBindingMode.CycleDown:
                {
                    int range = max - min + 1;
                    if (raw < min)
                        raw = ((raw - min) % range + range) % range + min;
                    else if (raw > max)
                        raw = min + ((raw - max - 1) % range);
                }
                    break;

                case RangeBindingMode.Oscillate:
                {
                    /*
                    int span = max - min;
                    int total = value - min;
                    int period = span * 2;
                    int offset = ((total % period) + period) % period;
                    value = offset <= span ? min + offset : max - (offset - span);
                    */
                    
                    int span = max - min;
                    if (span <= 0) return;

                    int distance = raw - min;
                    int period = span * 2;
                    int mod = ((distance % period) + period) % period;

                    raw = mod <= span ? min + mod : max - (mod - span);
                    
                }
                    break;
            }
        }
        
        /// <summary>Clamps the current value to the [min, max] range.</summary>
        /// <returns>The clamped value.</returns>
        public int Clamp() => Mathf.Clamp(raw, min, max);
        
        #endregion
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region LemmingInterConversions

        /// <summary>
        /// Remaps the normalized value to a float in a specified output range.
        /// </summary>
        /// <param name="outMin">Minimum float output.</param>
        /// <param name="outMax">Maximum float output.</param>
        /// <returns>The remapped float.</returns>
        public float ToFloatRange(float outMin, float outMax)
        {
            return Mathf.Lerp(outMin, outMax, Normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//
        
        
        /// <summary>
        /// Converts the normalized value to an index within an array.
        /// </summary>
        /// <param name="arraySize">The size of the array.</param>
        /// <returns>An index between 0 and arraySize - 1.</returns>
        public int ToIndex(int arraySize)
        {
            return Mathf.Clamp(Mathf.FloorToInt(Normalized * arraySize), 0, arraySize - 1);
        }

        
        //_______________  METHOD BREAK   ___________________//

        
        /// <summary>Remaps the normalized value to an integer range.</summary>
        /// <param name="minimum">Minimum integer output value.</param>
        /// <param name="maximum">Maximum integer output value.</param>
        /// <returns>Integer within [<paramref name="minimum"/>, <paramref name="maximum"/>].</returns>
        public int ToIntRange(int minimum, int maximum)
        {
            return Mathf.FloorToInt(Mathf.Lerp(minimum, maximum + 1, Normalized));
        }

        
        //_______________  METHOD BREAK   ___________________//

        
        /// <summary>
        /// Maps the current value to a label from a list using normalized indexing.
        /// </summary>
        /// <param name="labels">An array of string labels.</param>
        /// <returns>A string from the array corresponding to the current value.</returns>
        public string ToLabel(string[] labels)
        {
            return labels[ToIndex(labels.Length)];
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Maps the current value to a member of the specified enum using normalized indexing.
        /// </summary>
        /// <typeparam name="T">The enum type to map to.</typeparam>
        /// <returns>A member of the enum.</returns>
        public T ToEnum<T>() where T : Enum
        {
            Array values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(ToIndex(values.Length));
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Evaluates the normalized value on a Unity <see cref="AnimationCurve"/>.
        /// </summary>
        /// <param name="curve">The curve to evaluate.</param>
        /// <returns>The result of evaluating the curve.</returns>
        public float ToCurve(AnimationCurve curve)
        {
            return curve.Evaluate(Normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Interpolates between two vectors using the normalized value.
        /// </summary>
        /// <param name="vectorA">Start vector.</param>
        /// <param name="vectorB">End vector.</param>
        /// <returns>The interpolated <see cref="Vector3"/>.</returns>
        public Vector3 ToVector(Vector3 vectorA, Vector3 vectorB)
        {
            return Vector3.Lerp(vectorA, vectorB, Normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Interpolates between two rotations using the normalized value.
        /// </summary>
        /// <param name="a">Start rotation.</param>
        /// <param name="b">End rotation.</param>
        /// <returns>The interpolated <see cref="Quaternion"/>.</returns>
        public Quaternion ToQuaternion(Quaternion a, Quaternion b)
        {
            return Quaternion.Slerp(a, b, Normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Remaps the normalized value to a rotation around a specified axis.
        /// </summary>
        /// <param name="axis">Axis to rotate around.</param>
        /// <param name="minAngle">Minimum rotation in degrees.</param>
        /// <param name="maxAngle">Maximum rotation in degrees.</param>
        /// <returns>The resulting <see cref="Quaternion"/>.</returns>
        public Quaternion ToRotationAroundAxis(Vector3 axis, float minAngle, float maxAngle)
        {
            float angle = ToFloatRange(minAngle, maxAngle);
            return Quaternion.AngleAxis(angle, axis.normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Creates a look rotation between two vectors based on the normalized value.
        /// </summary>
        /// <param name="from">Starting direction.</param>
        /// <param name="to">Target direction.</param>
        /// <returns>The resulting <see cref="Quaternion"/> look rotation.</returns>
        public Quaternion ToLookRotation(Vector3 from, Vector3 to)
        {
            Vector3 direction = Vector3.Lerp(from, to, Normalized);
            return Quaternion.LookRotation(direction.normalized);
        }
        #endregion
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region LemmingInterFundamentals

        
        /// <summary>
        /// Returns a string representation of the current state for debugging purposes.
        /// </summary>
        public override string ToString()
        {
            return $"Value: {raw}, Range: [{min}, {max}], Normalized: {Normalized:F2}";
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Sets a new value and automatically applies the configured binding mode.
        /// </summary>
        /// <param name="newValue">The new value to assign.</param>
        public void SetValue(int newValue)
        {
            raw = newValue;
            ApplyCurrentBinding();
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Updates the min and max range of the struct.
        /// </summary>
        /// <param name="newMin">New minimum value.</param>
        /// <param name="newMax">New maximum value.</param>
        /// <exception cref="ArgumentException">Thrown if newMin equals newMax.</exception>
        public void SetRange(int newMin, int newMax)
        {
            if (newMin == newMax)
                throw new ArgumentException("min and max cannot be equal.");

            min = newMin;
            max = newMax;
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Sets a new <see cref="Lemmings.LemmingInter.RangeBindingMode"/> for how the value should behave when outside the range.
        /// </summary>
        /// <param name="mode">The new binding mode to apply.</param>
        public void SetBindingMode(RangeBindingMode mode)
        {
            bindingMode = mode;
        }
   
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Resets the value and range all at once, applying the current binding mode.
        /// </summary>
        /// <param name="newValue">New value.</param>
        /// <param name="newMin">New minimum range.</param>
        /// <param name="newMax">New maximum range.</param>
        /// <exception cref="ArgumentException">Thrown if newMin equals newMax.</exception>
        public void Reset(int newValue, int newMin, int newMax)
        {
            if (newMin == newMax)
                throw new ArgumentException("min and max cannot be equal.");

            raw = newValue;
            min = newMin;
            max = newMax;
            
            ApplyCurrentBinding();
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Applies the stored <see cref="bindingMode"/> to the current value.
        /// This is a convenience method for using the configured range behavior directly.
        /// </summary>
        public void ApplyCurrentBinding()
        {
            ApplyBounds(bindingMode);
        }
        #endregion
        
    }
    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region LemmingVectorizerCode 
    
    /// <summary>
    /// A struct for managing and interpreting a Vector3 value,
    /// including normalization, projection, direction checks, and remapping.
    /// </summary>
    [Serializable]
    public struct LemmingVectorizer : ILemmingConverter
    {

        #region LemmingVectorizerInitialization
        
        /// <summary>
        /// The raw vector input.
        /// </summary>
        public Vector3 raw;

        public object Raw => raw;
        
        /// <summary>
        /// The maximum expected magnitude used for normalization.
        /// </summary>
        public float maxMagnitude;

        /// <summary>
        /// The minimum expected magnitude used for normalization.
        /// </summary>
        public float minMagnitude;
        
        /// <summary>
        /// Returns the magnitude of the vector normalized to the range [0, 1].
        /// </summary>
        public float Normalized => Mathf.Clamp01((raw.magnitude - minMagnitude) / (maxMagnitude - minMagnitude));

        /// <summary>
        /// Returns true if the distance is greater than the maximum Magnitude set.
        /// </summary>
        public bool Over => raw.magnitude > maxMagnitude;

        /// <summary>
        /// Returns true if the magnitude is within the specified range.
        /// </summary>
        public bool InRange => !Over && !Under;
        
        /// <summary>
        /// Returns true if the distance is less than the minimum Magnitude set.
        /// </summary>
        public bool Under => raw.magnitude < minMagnitude;
        
        /// <summary>
        /// Returns the normalized direction of the vector.
        /// </summary>
        public Vector3 Direction => raw.normalized;

        /// <summary>
        /// Returns true if the vector is (0, 0, 0).
        /// </summary>
        public bool IsZero => raw == Vector3.zero;

        /// <summary>
        /// This provides the normalized value in a range of -1 to 1, useful for input mapping and relative values
        /// </summary>
        public float AsAxis => Normalized * 2f - 1f;
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Constructs a new LemmingVectorizer.
        /// </summary>
        /// <param name="raw">The input vector.</param>
        /// <param name="maxMagnitude">The maximum magnitude for normalization.</param>
        /// <param name="minMagnitude">The minimum magnitude for normalization</param>
        public LemmingVectorizer(Vector3 raw, float minMagnitude, float maxMagnitude)
        {
            if (Mathf.Approximately(minMagnitude, maxMagnitude))
                throw new ArgumentException("min and max cannot be equal.");
            
            this.raw = raw;
            this.maxMagnitude = maxMagnitude;
            this.minMagnitude = minMagnitude;
        }
        #endregion
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region LemmingVectorizerMapping
        
        /// <summary>
        /// Projects this vector onto another and returns the scalar projection (dot product).
        /// * Use this when you want to measure alignment between two directions such as checking if a hand is pointing toward a target or a body part is aligned with a reference axis.
        /// * Is an object above or below another one, in front or behind?
        /// ** example:
        /// 1 Vector3 directionToTarget = target.position - transform.position;
        /// 2 float alignment = vectorizer.ProjectOnto(directionToTarget);
        /// </summary>
        /// <param name="direction">The direction to project onto.</param>
        /// <returns>The projected vector.</returns>
        public Vector3 ProjectOnto(Vector3 direction)
        {
            return Vector3.Project(raw, direction);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Projects this vector onto the forward direction of a rotation and returns the scalar projection (dot product).
        /// * Use this when you want to measure alignment between two directions such as checking if a hand is pointing toward a target or a body part is aligned with a reference axis.
        /// * Is an object above or below another one, in front or behind?
        /// ** example:
        /// 1 Vector3 directionToTarget = target.position - transform.position;
        /// 2 float alignment = vectorizer.ProjectOnto(directionToTarget);
        /// </summary>
        /// <param name="rotation">The rotation whose forward direction is used for projection.</param>
        /// <returns>The projected vector.</returns>
        public Vector3 ProjectOnto(Quaternion rotation)
        {
            return Vector3.Project(raw, rotation * Vector3.forward);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Returns the angle in degrees between this vector and another vector.
        /// </summary>
        /// <param name="other">The vector to compare against.</param>
        /// <returns>The angle in degrees.</returns>
        public float AngleTo(Vector3 other)
        {
            return Vector3.Angle(raw, other);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Returns the angle in degrees between this vector and the forward direction of a rotation.
        /// </summary>
        /// <param name="rotation">The rotation whose forward vector is used for comparison.</param>
        /// <returns>The angle in degrees.</returns>
        public float AngleTo(Quaternion rotation)
        {
            return Vector3.Angle(raw, rotation * Vector3.forward);
        }


        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Returns the signed angle between this vector and another, relative to a given axis.
        /// * Use this to measure rotation in a specific plane, such as turning left/right around the Y axis—great for determining rotation direction relative to a fixed axis.
        /// * A simpler alternative to tracking rotations without neccesarily using quaternions.
        /// </summary>
        /// <param name="other">The other vector.</param>
        /// <param name="axis">The axis around which to measure the angle.</param>
        /// <returns>Signed angle in degrees.</returns>
        public float SignedAngle(Vector3 other, Vector3 axis)
        {
            return Vector3.SignedAngle(raw, other, axis);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Remaps the normalized magnitude to a float within a custom range.
        /// </summary>
        /// <param name="outMin">The minimum output value.</param>
        /// <param name="outMax">The maximum output value.</param>
        /// <returns>A float between outMin and outMax.</returns>
        public float ToFloatRange(float outMin, float outMax)
        {
            return Mathf.Lerp(outMin, outMax, Normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>Remaps the normalized value to an integer range.</summary>
        /// <param name="minimum">Minimum integer output value.</param>
        /// <param name="maximum">Maximum integer output value.</param>
        /// <returns>Integer within [<paramref name="minimum"/>, <paramref name="maximum"/>].</returns>
        public int ToIntRange(int minimum, int maximum)
        {
            return Mathf.FloorToInt(Mathf.Lerp(minimum, maximum + 1, Normalized));
        }
 
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Remaps the normalized magnitude to an integer index for an array.
        /// </summary>
        /// <param name="arraySize">The size of the array.</param>
        /// <returns>Clamped array index based on normalized value.</returns>
        public int ToIndex(int arraySize)
        {
            return Mathf.Clamp(Mathf.FloorToInt(Normalized * arraySize), 0, arraySize - 1);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Maps the current value to a label from a list using normalized indexing.
        /// </summary>
        /// <param name="labels">An array of string labels.</param>
        /// <returns>A string from the array corresponding to the current value.</returns>
        public string ToLabel(string[] labels)
        {
            return labels[ToIndex(labels.Length)];
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Maps the current value to a member of the specified enum using normalized indexing.
        /// </summary>
        /// <typeparam name="T">The enum type to map to.</typeparam>
        /// <returns>A member of the enum.</returns>
        public T ToEnum<T>() where T : Enum
        {
            Array values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(ToIndex(values.Length));
        }

 
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Interpolates between two vectors using the normalized magnitude.
        /// </summary>
        /// <param name="a">The start vector.</param>
        /// <param name="b">The end vector.</param>
        /// <returns>An interpolated Vector3 between a and b.</returns>
        public Vector3 ToVector(Vector3 a, Vector3 b)
        {
            return Vector3.Lerp(a, b, Normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Interpolates between two rotations using the normalized magnitude.
        /// </summary>
        /// <param name="a">Start rotation.</param>
        /// <param name="b">End rotation.</param>
        /// <returns>An interpolated <see cref="Quaternion"/>.</returns>
        public Quaternion ToQuaternion(Quaternion a, Quaternion b)
        {
            return Quaternion.Slerp(a, b, Normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Creates a rotation around the given axis based on the normalized magnitude.
        /// </summary>
        /// <param name="axis">Axis to rotate around (should be normalized).</param>
        /// <param name="minAngle">Minimum rotation in degrees.</param>
        /// <param name="maxAngle">Maximum rotation in degrees.</param>
        /// <returns>A <see cref="Quaternion"/> representing the rotation.</returns>
        public Quaternion ToRotationAroundAxis(Vector3 axis, float minAngle, float maxAngle)
        {
            float angle = ToFloatRange(minAngle, maxAngle);
            return Quaternion.AngleAxis(angle, axis.normalized);
        }
 
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Converts this vector into a look rotation.
        /// </summary>
        /// <param name="up">The up vector to use for rotation stability.</param>
        /// <returns>A Quaternion facing in the direction of the vector.</returns>
        public Quaternion ToLookRotation(Vector3 up)
        {
            return IsZero ? Quaternion.identity : Quaternion.LookRotation(Direction, up);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Evaluates the normalized magnitude on an AnimationCurve.
        /// </summary>
        /// <param name="curve">The curve to evaluate.</param>
        /// <returns>The curve output at the normalized magnitude.</returns>
        public float ToCurve(AnimationCurve curve)
        {
            return curve.Evaluate(Normalized);
        }
 
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Checks if this rotation is approximately facing toward a given world position.
        /// </summary>
        /// <param name="targetPosition">The position to face.</param>
        /// <param name="currentPosition">The current position.</param>
        /// <param name="threshold">The dot product threshold (default = 0.95).</param>
        /// <returns>True if the rotation is approximately facing the target.</returns>
        public bool IsFacingPosition(Vector3 targetPosition, Vector3 currentPosition, float threshold = 0.95f)
        {
            Vector3 toTarget = (targetPosition - currentPosition).normalized;
            float dot = Vector3.Dot(Direction, toTarget);
            return dot >= threshold;
        }
 
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Checks if the rotation is pointed towards a given direction using a threshold
        /// </summary>
        /// <param name="direction">Direction to check against</param>
        /// <param name="threshold">The dot product threshold for detection (default = 0.95)</param>
        /// <returns></returns>
        public bool IsFacingDirection(Vector3 direction, float threshold = 0.95f)
        {
            return Vector3.Dot(Direction, direction.normalized) >= threshold;
        }
  
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Checks if this vector is facing toward a given Transform.
        /// </summary>
        /// <param name="target">The target transform.</param>
        /// <param name="threshold">Dot product threshold (default = 0.95).</param>
        /// <returns>True if the vector is approximately facing the target.</returns>
        public bool IsFacingObject(Transform target, float threshold = 0.95f)
        {
            if (target == null) return false;
            Vector3 toTarget = (target.position - raw).normalized;
            return Vector3.Dot(raw.normalized, toTarget) >= threshold;
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Checks if this vector is facing toward a given GameObject.
        /// </summary>
        /// <param name="target">The target GameObject.</param>
        /// <param name="threshold">Dot product threshold (default = 0.95).</param>
        /// <returns>True if the vector is approximately facing the GameObject.</returns>
        public bool IsFacingObject(GameObject target, float threshold = 0.95f)
        {
            return target != null && IsFacingObject(target.transform, threshold);
        }
        
        #endregion
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region LemmingVectorizerFundamentals
        /// <summary>
        /// Returns a string representation of the vector, its magnitude, and normalized value.
        /// </summary>
        public override string ToString()
        {
            return $"Raw: {raw}, Magnitude: {raw.magnitude:F2}, Normalized: {Normalized:F2}";
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Sets a new raw vector value.
        /// </summary>
        /// <param name="newRaw">The new vector value.</param>
        public void SetRaw(Vector3 newRaw)
        {
            raw = newRaw;
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Sets a new maximum magnitude for normalization.
        /// </summary>
        /// <param name="newMax">The new max magnitude.</param>
        /// <param name="newMin">New min magnitude.</param>
        public void SetRange(float newMax, float newMin)
        {
            if (Mathf.Approximately(minMagnitude, maxMagnitude))
                throw new ArgumentException("min and max cannot be equal.");
            
            maxMagnitude = newMax;
            minMagnitude = newMin; 
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Resets the vector and maximum magnitude.
        /// </summary>
        /// <param name="newRaw">New vector value.</param>
        /// <param name="newMax">New max magnitude.</param>
        /// <param name="newMin">New min magnitude.</param>
        public void Reset(Vector3 newRaw, float newMax, float newMin)
        {
            if (Mathf.Approximately(minMagnitude, maxMagnitude))
                throw new ArgumentException("min and max cannot be equal.");
            
            raw = newRaw;
            maxMagnitude = newMax;
            minMagnitude = newMin;
        }
        #endregion
    }
    #endregion
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------
    
    #region LemmingRotaterCode
       
    /// <summary>
    /// A struct for managing and interpreting Quaternion rotations,
    /// including angular comparison, interpolation, remapping, and directional checks.
    ///
    /// Quaternions represent 3D rotation in a stable, interpolation-friendly form.
    /// Use them for smooth orientation tracking, angle comparisons, and gesture dynamics.
    /// They avoid gimbal lock and provide consistent behavior when interpolating or blending.
    /// </summary>
    [Serializable]
    public struct LemmingRotater : ILemmingConverter
    {
        #region Lemming Rotater Initialization
        
        /// <summary>
        /// The raw Quaternion value representing a 3D rotation.
        /// </summary>
        public Quaternion raw;

        /// <summary>
        /// The raw Quaternion value representing a 3D rotation.
        /// </summary>
        public object Raw => raw;
        
        /// <summary>
        /// The maximum angle used to normalize the rotation magnitude.
        /// </summary>
        public float maxAngle;

        /// <summary>
        /// The minimum angle used to normalize the rotation magnitude.
        /// </summary>
        public float minAngle;
        
        /// <summary>
        /// Returns the normalized value between minAngle and maxAngle.
        /// </summary>
        public float Normalized => Mathf.Clamp01((Angle() - minAngle) / (maxAngle - minAngle));
        /// <summary>
        /// Returns true if the angle is greater than the maximum angle set.
        /// </summary>
        public bool Over => Angle() > maxAngle;
        
        /// <summary>
        /// Returns true if the angle is within the specified range.
        /// </summary>
        public bool InRange => !Over && !Under;
        
        /// <summary>
        /// Returns true if the angle is less than the maximum angle set.
        /// </summary>
        public bool Under => Angle() < minAngle;

        /// <summary>
        /// Returns true if the quaternion represents the identity rotation (no rotation).
        /// </summary>
        public bool IsZero => raw == Quaternion.identity;
        
        /// <summary>
        /// Returns the direction the quaternion is facing.
        /// </summary>
        public Vector3 Direction => raw * Vector3.forward;
        
        /// <summary>
        /// This provides the normalized value in a range of -1 to 1, useful for input mapping and relative values
        /// </summary>
        public float AsAxis => Normalized * 2f - 1f;
        
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Constructs a new LemmingRotater with a rotation and max angle.
        /// </summary>
        /// <param name="raw">The rotation to store.</param>
        /// <param name="maxAngle">The angle to use for normalization reference.</param>
        public LemmingRotater(Quaternion raw, float maxAngle, float minAngle)
        {
            if (Mathf.Approximately(minAngle, maxAngle))
                throw new ArgumentException("min and max cannot be equal.");
            
            this.raw = raw;
            this.maxAngle = maxAngle;
            this.minAngle = minAngle;
        }

        #endregion
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Lemming Rotater Mappings
        
        /// <summary>
        /// Returns the angular difference in degrees between this rotation and another quaternion.
        /// </summary>
        /// <param name="target">The target rotation to compare to.</param>
        /// <returns>The angle in degrees between the two rotations.</returns>
        public float AngleTo(Quaternion target)
        {
            return Quaternion.Angle(raw, target);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Returns the angular difference in degrees between the direction this quaternion faces and a world-space direction vector.
        /// </summary>
        /// <param name="direction">The world-space direction to compare against.</param>
        /// <returns>The angle in degrees between this rotation's forward direction and the given direction.</returns>
        public float AngleTo(Vector3 direction)
        {
            return Vector3.Angle(Direction, direction.normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//



        /// <summary>
        /// Interpolates between two quaternions based on the normalized value.
        /// </summary>
        /// <param name="a">The start rotation.</param>
        /// <param name="b">The end rotation.</param>
        /// <returns>An interpolated Quaternion based on the normalized factor.</returns>
        public Quaternion ToQuaternion(Quaternion a, Quaternion b)
        {
            return Quaternion.Slerp(a, b, Normalized);
        }
        
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Interpolates between two vectors using the normalized factor.
        /// </summary>
        /// <param name="a">The start vector.</param>
        /// <param name="b">The end vector.</param>
        /// <returns>An interpolated Vector3 between a and b.</returns>
        public Vector3 ToVector(Vector3 a, Vector3 b)
        {
            return Vector3.Lerp(a, b, Normalized);
        }


        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Remaps the normalized value to a float between outMin and outMax.
        /// </summary>
        /// <param name="outMin">Minimum output value.</param>
        /// <param name="outMax">Maximum output value.</param>
        /// <returns>A float value interpolated from outMin to outMax.</returns>
        public float ToFloatRange(float outMin, float outMax)
        {
            return Mathf.Lerp(outMin, outMax, Normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>Remaps the normalized value to an integer range.</summary>
        /// <param name="minimum">Minimum integer output value.</param>
        /// <param name="maximum">Maximum integer output value.</param>
        /// <returns>Integer within [<paramref name="minimum"/>, <paramref name="maximum"/>].</returns>
        public int ToIntRange(int minimum, int maximum)
        {
            return Mathf.FloorToInt(Mathf.Lerp(minimum, maximum + 1, Normalized));
        }
        
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Maps the current value to a label from a list using normalized indexing.
        /// </summary>
        /// <param name="labels">An array of string labels.</param>
        /// <returns>A string from the array corresponding to the current value.</returns>
        public string ToLabel(string[] labels)
        {
            return labels[ToIndex(labels.Length)];
        }
        
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Evaluates the normalized value on a Unity AnimationCurve.
        /// </summary>
        /// <param name="curve">The curve to evaluate.</param>
        /// <returns>A float representing the curve output at the normalized position.</returns>
        public float ToCurve(AnimationCurve curve)
        {
            return curve.Evaluate(Normalized);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Returns the axis of rotation from the stored quaternion.
        /// </summary>
        /// <returns>A normalized Vector3 representing the axis of rotation.</returns>
        public Vector3 Axis()
        {
            raw.ToAngleAxis(out float _, out Vector3 axis);
            return axis.normalized;
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Returns the rotation angle (in degrees) represented by the quaternion.
        /// </summary>
        /// <returns>The angle in degrees around the axis of rotation.</returns>
        public float Angle()
        {
            raw.ToAngleAxis(out float angle, out _);
            return angle;
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Projects the forward direction of this rotation onto the given vector.
        /// </summary>
        /// <param name="direction">The direction to project onto.</param>
        /// <returns>The projected vector.</returns>
        public Vector3 ProjectOnto(Vector3 direction)
        {
            return Vector3.Project(Direction, direction);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Projects the forward direction of this rotation onto another rotation's forward direction.
        /// </summary>
        /// <param name="rotation">The rotation whose direction to project onto.</param>
        /// <returns>The projected vector.</returns>
        public Vector3 ProjectOnto(Quaternion rotation)
        {
            return Vector3.Project(Direction, rotation * Vector3.forward);
        }


        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Checks whether this rotation is facing approximately the same direction as another.
        /// </summary>
        /// <param name="target">The target rotation to compare to.</param>
        /// <param name="threshold">The dot product threshold to consider as aligned (default = 0.95).</param>
        /// <returns>True if the dot product is greater than or equal to the threshold.</returns>
        public bool IsMatchingRotation(Quaternion target, float threshold = 0.95f)
        {
            return Quaternion.Dot(raw, target) >= threshold;
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Converts the normalized rotation value to a rotation around the given axis using a remapped angle range.
        /// </summary>
        /// <param name="axis">The axis to rotate around (e.g., Vector3.up).</param>
        /// <param name="minAngle">The minimum angle in degrees.</param>
        /// <param name="maxAngle">The maximum angle in degrees.</param>
        /// <returns>A <see cref="Quaternion"/> representing the rotation around the specified axis.</returns>
        public Quaternion ToRotationAroundAxis(Vector3 axis, float minAngle, float maxAngle)
        {
            float angle = Mathf.Lerp(minAngle, maxAngle, Normalized); // remap the normalized value
            return Quaternion.AngleAxis(angle, axis.normalized);
        }
        
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Checks if this rotation is approximately facing toward a given world position.
        /// </summary>
        /// <param name="targetPosition">The position to face.</param>
        /// <param name="currentPosition">The current position.</param>
        /// <param name="threshold">The dot product threshold (default = 0.95).</param>
        /// <returns>True if the rotation is approximately facing the target.</returns>
        public bool IsFacingPosition(Vector3 targetPosition, Vector3 currentPosition, float threshold = 0.95f)
        {
            Vector3 toTarget = (targetPosition - currentPosition).normalized;
            float dot = Vector3.Dot(Direction, toTarget);
            return dot >= threshold;
        }
       
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Checks if the rotation is pointed towards a given direction using a threshold
        /// </summary>
        /// <param name="direction">Direction to check against</param>
        /// <param name="threshold">The dot product threshold for detection (default = 0.95)</param>
        /// <returns></returns>
        public bool IsFacingDirection(Vector3 direction, float threshold = 0.95f)
        {
            float dot = Vector3.Dot(Direction, direction);
            return dot >= threshold;
        }
        
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Maps the normalized value to an array index.
        /// </summary>
        /// <param name="arraySize">The total number of elements in the array.</param>
        /// <returns>An index in the range [0, arraySize - 1].</returns>
        public int ToIndex(int arraySize)
        {
            return Mathf.Clamp(Mathf.FloorToInt(Normalized * arraySize), 0, arraySize - 1);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Maps the normalized value to an enum value based on index.
        /// </summary>
        /// <typeparam name="T">The enum type to convert to.</typeparam>
        /// <returns>An enum value mapped from the normalized value.</returns>
        public T ToEnum<T>() where T : Enum
        {
            Array values = Enum.GetValues(typeof(T));
            int index = ToIndex(values.Length);
            return (T)values.GetValue(index);
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Returns a formatted string summarizing the raw rotation, angle, and normalized value.
        /// </summary>
        /// <returns>A human-readable string for debugging or display.</returns>
        public override string ToString()
        {
            return $"Rotation: {raw.eulerAngles}, Angle: {Angle():F2}, Normalized: {Normalized:F2}";
        }

        #endregion
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Lemming Rotater Fundamentals
        
        /// <summary>
        /// Sets the internal rotation to a new value.
        /// </summary>
        /// <param name="newRotation">The new Quaternion rotation.</param>
        public void SetRaw(Quaternion newRotation)
        {
            raw = newRotation;
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Sets the maxAngle used for normalization.
        /// </summary>
        /// <param name="newMax">The new maximum angle.</param>
        /// /// <param name="newMin">The new minimum angle.</param>
        public void SetRange(float newMax, float newMin)
        {
            if (Mathf.Approximately(minAngle, maxAngle))
                throw new ArgumentException("min and max cannot be equal.");
            
            maxAngle = newMax;
            minAngle = newMin;
        }

        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Resets the internal rotation and maxAngle to new values.
        /// </summary>
        /// <param name="newRotation">The new Quaternion value.</param>
        /// <param name="newMaxAngle">The new maximum angle.</param>
        /// /// <param name="newMinAngle">The new minimum angle.</param>
        public void Reset(Quaternion newRotation, float newMaxAngle, float newMinAngle)
        {
            if (Mathf.Approximately(minAngle, maxAngle))
                throw new ArgumentException("min and max cannot be equal.");
            
            raw = newRotation;
            maxAngle = newMaxAngle;
            minAngle = newMinAngle;
        }
        #endregion
    }
    
    #endregion

    
}
