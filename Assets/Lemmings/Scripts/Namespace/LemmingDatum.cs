using UnityEngine;

namespace Lemmings
{
    /// <summary>
    /// Represents a flexible container for storing a single value of varying data types.
    /// Supports types such as float, int, bool, Vector3, Quaternion, and string.
    /// </summary>
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    [System.Serializable]
    public class LemmingDatum
    {
        #region Serialized Fields and Properties
        
        [SerializeField]
        private LemmingValueType type;

        [SerializeField] private float floatValue;
        [SerializeField] private int intValue;
        [SerializeField] private bool boolValue;
        [SerializeField] private Vector3 vector3Value;
        [SerializeField] private Quaternion quaternionValue;
        [SerializeField] private string stringValue;

        /// <summary>
        /// Gets the type of value currently stored.
        /// </summary>
        public LemmingValueType Type => type;
        
        public override string ToString()
        {
            return $"LemmingDatum(Type: {Type}, Value: {Value}, ConverterType: {ConverterType} Converter: {GetConverter(0,1)})";
        }
        
        #endregion
                    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Value Sending and Setting Methods

        /// <summary>
        /// Gets the stored value as a boxed object, according to the stored type.
        /// </summary>
        public object Value
        {
            get
            {
                return type switch
                {
                    LemmingValueType.Float => floatValue,
                    LemmingValueType.Int => intValue,
                    LemmingValueType.Bool => boolValue,
                    LemmingValueType.Vector3 => vector3Value,
                    LemmingValueType.Quaternion => quaternionValue,
                    LemmingValueType.String => stringValue,
                    _ => null,
                };
            }
        }

        /// <summary>
        /// Sets the value of the datum using a boxed object. Supported types are:
        /// float, int, bool, Vector3, Quaternion, and string.
        /// Automatically updates the internal type.
        /// </summary>
        /// <param name="value">The value to store.</param>
        public void SetValue(object value)
        {
            switch (value)
            {
                case float f:
                    type = LemmingValueType.Float;
                    floatValue = f;
                    break;
                case int i:
                    type = LemmingValueType.Int;
                    intValue = i;
                    break;
                case bool b:
                    type = LemmingValueType.Bool;
                    boolValue = b;
                    break;
                case Vector3 v3:
                    type = LemmingValueType.Vector3;
                    vector3Value = v3;
                    break;
                case Quaternion q:
                    type = LemmingValueType.Quaternion;
                    quaternionValue = q;
                    break;
                case string s:
                    type = LemmingValueType.String;
                    stringValue = s;
                    break;
                default:
                    Debug.LogError($"Unsupported LemmingDatum type: {value.GetType()}");
                    break;
            }
        }

        #endregion
                    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Boxing as Type Methods
        
        /// <summary>
        /// Gets the stored float value. Only valid if Type is Float.
        /// </summary>
        public float AsFloat() => floatValue;

        /// <summary>
        /// Gets the stored int value. Only valid if Type is Int.
        /// </summary>
        public int AsInt() => intValue;

        /// <summary>
        /// Gets the stored bool value. Only valid if Type is Bool.
        /// </summary>
        public bool AsBool() => boolValue;

        /// <summary>
        /// Gets the stored Vector3 value. Only valid if Type is Vector3.
        /// </summary>
        public Vector3 AsVector3() => vector3Value;

        /// <summary>
        /// Gets the stored Quaternion value. Only valid if Type is Quaternion.
        /// </summary>
        public Quaternion AsQuaternion() => quaternionValue;

        /// <summary>
        /// Gets the stored string value. Only valid if Type is String.
        /// </summary>
        public string AsString() => stringValue;

        #endregion
                    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Conversion Construction Methods
        
        /// <summary>
        /// Returns a converter object appropriate for the current Datum type.
        /// </summary>
        /// <param name="min">Minimum value for normalization.</param>
        /// <param name="max">Maximum value for normalization.</param>
        /// <returns>An object that can perform normalization and curve evaluation.</returns>
        public object GetConverter(float min, float max)
        {
            return Type switch
            {
                LemmingValueType.Float => new LemmingFloater(AsFloat(), min, max),
                LemmingValueType.Int => new LemmingInter(AsInt(), Mathf.FloorToInt(min), Mathf.CeilToInt(max), LemmingInter.RangeBindingMode.Clamped),
                LemmingValueType.Vector3 => new LemmingVectorizer(AsVector3(), min, max),
                LemmingValueType.Quaternion => new LemmingRotater(AsQuaternion(), min, max),
                LemmingValueType.Bool => new LemmingBooler(AsBool(), min, max),
                LemmingValueType.String => AsString(),
                _ => null,
            };
        }

        /// <summary>
        /// Returns the name of the converter struct or method for the current type.
        /// </summary>
        public string ConverterType
        {
            get
            {
                return Type switch
                {
                    LemmingValueType.Float => "LemmingFloater",
                    LemmingValueType.Int => "LemmingInter",
                    LemmingValueType.Vector3 => "LemmingVectorizer",
                    LemmingValueType.Quaternion => "LemmingRotater",
                    LemmingValueType.Bool => "LemmingBooler",
                    LemmingValueType.String => "Value Only",
                    _ => "<unknown>"
                };
            }
        }
        #endregion
    }
}
