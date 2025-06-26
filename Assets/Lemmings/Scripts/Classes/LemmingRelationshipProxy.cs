using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

#if UNITY_EDITOR
using Lemmings;
using UnityEditor.SceneManagement;
#endif

namespace Lemmings
{

    /// <summary>
    /// A MonoBehaviour that represents the spatial location and rotation of a LemmingRelationship.
    /// It provides support for spatial interaction detection such as gaze and triggers,
    /// and exposes that data via structured fields and UnityEvents.
    /// </summary>
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public class LemmingRelationshipProxy : MonoBehaviour
    {
        #region Declared Variables
        
        [Header("➤ Relationship and Reference")]
        
        [Tooltip("The relationship this proxy represents.")]
        public LemmingRelationship relationship;
        
        /// <summary>
        /// Unique identifier for this proxy. Auto-generated using the relationship ID.
        /// </summary>
        [ReadOnly]
        public string ID;

        /// <summary>
        /// Optional description for debugging or tagging purposes.
        /// </summary>
        [TextArea]
        public string description;
        
        //--------------------------------------------------------------------- Header Break
        
        [Header("➤ Gaze and Trigger Settings")]
        
        [Tooltip("Whether to check for 3D trigger collisions.")]
        public bool detectTriggers = false;

        [Tooltip("Whether to perform gaze detection using this object's forward vector.")]
        public bool detectGaze = false;

        [Tooltip("Maximum range for gaze detection raycast.")]
        public float gazeRange = 10f;

        //--------------------------------------------------------------------- Header Break

        [Header("➤ Tag Settings")]

        [Tooltip("List of tags this proxy will respond to in trigger or gaze checks.")]
        public List<ValidTagEntry> validTags = new List<ValidTagEntry> { new ValidTagEntry() };
    
        /// <summary>
        /// Wrapper class to allow tag string dropdowns in the inspector.
        /// </summary>
        [System.Serializable]
        public class ValidTagEntry
        {
            [Tooltip("Tag name to match for interactions.")]
            [TagSelector] public string tag;
        }
        
        //--------------------------------------------------------------------- Header Break
        
        [HideInInspector]
        [Tooltip("Event invoked when a trigger interaction is detected.")]
        public TriggerEvent OnTriggerDetected;
        
        [HideInInspector]
        [Tooltip("Event invoked when a trigger interaction ends.")]
        public TriggerEvent OnTriggerExited;
        
        [HideInInspector]
        [Tooltip("Event invoked when a gaze interaction is detected.")]
        public GazeEvent OnGazeDetected;

        #endregion
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Public Accessors
        
        /// <summary>
        /// Whether a valid gaze target is currently detected.
        /// </summary>
        public bool IsGazing => _gazeTarget != null;
        
        /// <summary>
        /// Target of gaze.
        /// </summary>
        public GameObject GazeTarget => _gazeTarget;

        /// <summary>
        /// Whether a valid trigger event has occured.
        /// </summary>
        public bool IsTriggered => _isTriggered;
        
        /// <summary>
        /// Confidence of gaze.
        /// </summary>
        public float GazeConfidence => _gazeConfidence;
        
        /// <summary>
        /// Cached snapshot of the current proxy state.
        /// </summary>
        public LemmingProxyData Data => GetProxyData();

        /// <summary>
        /// Cached access to the underlying relationship's data struct.
        /// </summary>
        public LemmingRelationshipInfo RelationshipInfo => relationship ? relationship.Info : new LemmingRelationshipInfo();

        #endregion
            
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Private Variables
        
        /// <summary>
        /// Used to define and access a collider if one does not already exist
        /// </summary>
        private SphereCollider sphereCollider;

        /// <summary>
        /// If a collider already exists, this is a way to access it,
        /// if one does not, then the sphere collider is used for this
        /// </summary>
        private Collider myCollider;
        
        /// <summary>
        /// Is the proxy currently triggered
        /// </summary>
        private bool _isTriggered;
        
        /// <summary>
        /// What is the source of the triggering event
        /// </summary>
        private GameObject _triggerSource;
        
        /// <summary>
        /// What object was just gazed upon
        /// </summary>
        private GameObject _gazeTarget;
        
        /// <summary>
        /// How confident in that gaze is the system
        /// </summary>
        private float _gazeConfidence;
        
        #endregion
    
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Data Structure and formation
        
        /// <summary>
        /// Serializable data container for proxy state. Useful for diagnostics and UI representation.
        /// </summary>
        [System.Serializable]
        public struct LemmingProxyData
        {
            public string RelationshipID;
            public Vector3 Position;
            public Quaternion Rotation;
            public float Radius;
            public bool IsTriggered;
            public GameObject TriggerSource;
            public GameObject GazeTarget;
            public float GazeConfidence;
            public string[] ValidTags;
        }
        
        
        // ------------------   Method Break   ---------------- <  <  <  <  <  <  


        /// <summary>
        /// Constructs and returns the latest proxy data snapshot.
        /// </summary>
        public LemmingProxyData GetProxyData()
        {
            return new LemmingProxyData
            {
                RelationshipID = relationship?.ID,
                Position = transform.position,
                Rotation = transform.rotation,
                Radius = sphereCollider ? sphereCollider.radius : 0.1f,
                IsTriggered = _isTriggered,
                TriggerSource = _triggerSource,
                GazeTarget = _gazeTarget,
                GazeConfidence = _gazeConfidence,
                ValidTags = validTags?.Select(entry => entry.tag).ToArray()
            };
        }

        
        #endregion
            
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Methods
        
        private void Awake()
        {
            // Check for existing collider
            var existingCollider = GetComponent<Collider>();

            // If no collider at all, add a SphereCollider
            if (existingCollider == null)
            {
                sphereCollider = gameObject.AddComponent<SphereCollider>();
                myCollider = sphereCollider;
                if (relationship != null)
                {
                    ConfigureCollider(ComputeRadius(), true);
                }
                
            }
            else if (existingCollider is SphereCollider sc)
            {
                sphereCollider = sc;
                myCollider = sphereCollider;
                if (relationship != null)
                {
                    ConfigureCollider(ComputeRadius(), true);
                }
                
            }
            else
            {
                myCollider = existingCollider;
                // Another type of collider exists — don't overwrite or modify
                Debug.LogWarning($"[LemmingRelationshipProxy] Found existing collider of type {existingCollider.GetType().Name}. " +
                                 $"Proxy will not auto-configure radius.");
            }
            
            if (relationship != null)
            {
                // always assign this proxy to the relationship
                relationship.proxy = this;

                // Auto-generate ID
                ID = $"{relationship.ID}_proxy";
            }
        }
        
        
        // ------------------   Method Break   ---------------- <  <  <  <  <  <  


        private void Update()
        {
            
            if (relationship == null) return;
            if (detectGaze) UpdateGazeTarget();
            
            // Update this proxy's transform based on the relationship center
            transform.position = RelationshipPreviewUtility.PreviewPosition(relationship);
            transform.rotation = RelationshipPreviewUtility.PreviewRotation(relationship);
            sphereCollider.radius = ComputeRadius();
        }
        
        
        // ------------------   Method Break   ---------------- <  <  <  <  <  <  


        /// <summary>
        /// When a trigger is encountered, and trigger tracking is active,
        /// check to see if the tag of the trigger is part of the list of valid tags
        /// </summary>
        /// <param name="other">The Colliding object</param>
        private void OnTriggerEnter(Collider other)
        {
            if (!detectTriggers || !Matches(other.gameObject)) return;
            _isTriggered = true;
            _triggerSource = other.gameObject;
            OnTriggerDetected?.Invoke(_triggerSource);
        }
        
        
        // ------------------   Method Break   ---------------- <  <  <  <  <  <  
         
        
        /// <summary>
        /// When a trigger is exited, and trigger tracking is active,
        /// check to see if the tag of the trigger is part of the list of valid tags
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (!detectTriggers || _triggerSource != other.gameObject) return;
            OnTriggerExited?.Invoke(_triggerSource);
            _isTriggered = false;
            _triggerSource = null;
        }
        
        
        // ------------------   Method Break   ---------------- <  <  <  <  <  <  


        /// <summary>
        /// Performs a forward raycast from this proxy's transform to detect gaze targets.
        /// Calculates directional confidence and invokes an event if matched.
        /// </summary>
        private void UpdateGazeTarget()
        {
            _gazeTarget = null;
            _gazeConfidence = 0f;

            Vector3 origin = transform.position;
            Vector3 direction = transform.forward;

            Ray ray = new Ray(origin, direction);
            if (Physics.Raycast(ray, out RaycastHit hit, gazeRange))
            {
                if (Matches(hit.collider.gameObject))
                {
                    _gazeTarget = hit.collider.gameObject;
                    _gazeConfidence = Vector3.Dot(direction.normalized, (hit.point - origin).normalized);
                    OnGazeDetected?.Invoke(_gazeTarget, _gazeConfidence);
                }
            }
        }
        
        
        // ------------------   Method Break   ---------------- <  <  <  <  <  <  


        /// <summary>
        /// Computes a radius based on the average distance of relationship members from this proxy.
        /// </summary>
        public virtual float ComputeRadius()
        {
            if (relationship.Members == null || relationship.Members.Count <= 1)
                return 0.1f;

            Vector3 center = transform.position;
            float total = 0f;

            foreach (var m in relationship.Members)
            {
                if (m != null)
                    total += Vector3.Distance(center, m.transform.position);
            }

            return total / relationship.Members.Count;
        }
        
        
        // ------------------   Method Break   ---------------- <  <  <  <  <  <  


        /// <summary>
        /// Sets this proxy's collider radius and whether it acts as a trigger.
        /// </summary>
        public void ConfigureCollider(float radius, bool isTrigger)
        {
            if (sphereCollider == null)
                sphereCollider = GetComponent<SphereCollider>();

            if (relationship != null)
            {
                sphereCollider.radius = radius;
                sphereCollider.isTrigger = isTrigger;
            }
        }
        
        
        // ------------------   Method Break   ---------------- <  <  <  <  <  <  


        /// <summary>
        /// Checks whether a GameObject matches a tag in the proxy's filter or the relationship ID.
        /// </summary>
        public bool Matches(GameObject other)
        {
            return validTags.Any(entry => other.CompareTag(entry.tag));
        }
        
        #endregion
        
        
        private void OnValidate()
        {
#if UNITY_EDITOR
            if (relationship != null)
            {
                relationship.proxy = this;
                ID = $"{relationship.ID}_proxy";
                EditorUtility.SetDirty(relationship);
            }
#endif
        }

    }

           
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------
        
    #region Event Declarations

    /// <summary>
    /// UnityEvent that is triggered when a GameObject enters or exits this proxy's trigger collider.
    /// </summary>
    [System.Serializable]
    public class TriggerEvent : UnityEvent<GameObject> { }

    /// <summary>
    /// UnityEvent triggered when this proxy's forward gaze detects a target GameObject.
    /// Includes the detected object and a confidence value (0-1).
    /// </summary>
    [System.Serializable]
    public class GazeEvent : UnityEvent<GameObject, float> { }
    #endregion


               
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    
    
    /// <summary>
    /// Attribute that enables tag dropdown selection for string fields in the inspector.
    /// </summary>
    public class TagSelectorAttribute : PropertyAttribute { }
}
