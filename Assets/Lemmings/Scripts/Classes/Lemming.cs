using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Lemmings
{
    /// <summary>
    /// Component representing a tracked Lemming attached to a GameObject.
    /// </summary>
    [Icon("Assets/Inuts/Icons/LemmingPale.png")]
    public class Lemming : MonoBehaviour, ILemming
    {
        [SerializeField] private string referenceName;
        [SerializeField, Range(0f, 1f)] private float confidence = 1f;

        // === Velocity Tracking ===
        [SerializeField, Range(3, 60)]
        private int velocitySampleSize = 10;

        private readonly Queue<Vector3> velocitySamples = new();
        private Vector3 lastPosition;
        private float lastTime;
        private Rigidbody rb;
        public Vector3 SmoothedVelocity { get; private set; }

        public Vector3 Velocity =>
            rb != null && rb.isKinematic == false ? rb.linearVelocity : SmoothedVelocity;
        
        /// <summary>
        /// This is a field for a gameObject that you wish the Lemming to track.
        /// It is generally easier to simply let the Lemming be the main object that is used
        /// to define relationships but in cases where a hierarchy is particularly complex,
        /// or it makes the project cleaner, you can add an object to this field
        /// and it will be used to evaluate Lemming Relationships.
        /// </summary>
        [Tooltip("Leave this blank by default. If you want to mirror the position and rotation of another object, drag it here")]
        public GameObject objectToTrack;
        
        /// <summary>
        /// Read-Only accessor for the object this Lemming is matching
        /// </summary>
        public GameObject matchTarget => objectToTrack;
        
        /// <summary>
        /// True if the Lemming is tracking an external GameObject (via objectToTrack).
        /// False if it is using its own transform as the tracked reference.
        /// </summary>
        public bool followingObject => objectToTrack != null;
        
        /// <summary>
        /// The assigned name of the Lemming. This inherits from a serialized private field
        /// </summary>
        public string Name => referenceName;

        /// <summary>
        /// This is a backup in case name is called with lowercase letters
        /// </summary>
        public string name => Name;
        
        /// <summary>
        /// When the name is never entered manually and the lemming is accessed remotely,
        /// it leads to a null result, this is a backup that uses the name of the gameobject
        /// to resolve.
        /// </summary>
        public string DefaultName => gameObject.name;
        
        /// <summary>
        /// The GameObject this Lemming refers to. Pulls the gameobject this script is a component of
        /// </summary>
        public GameObject Source => gameObject;

        /// <summary>
        /// The confidence value (0 to 1) for this Lemming.
        /// Placeholder implementation until dynamic evaluation is implemented.
        /// TODO: The confidence should be updatable by users testing the tracking of potential features
        /// </summary>
        public float Confidence => GetConfidence();


        private void Awake()
        {
            // initialize velocity checking settings
            lastPosition = transform.position;
            lastTime = Time.time;
            if (GetComponent<Rigidbody>() != null)
            {
                rb = GetComponent<Rigidbody>();
            }
            
        }

        // ---------------- Method Break -------------------------------------------------------     <<    <<   <<   <<

        /// <summary>
        /// If this is matching a tracked object then update these values each frame
        /// </summary>
        private void Update()
        {
            if (followingObject)
            {
                transform.position = objectToTrack.transform.position;
                transform.rotation = objectToTrack.transform.rotation;
            }

            UpdateSmoothedVelocity();
        }

        // ---------------- Method Break -------------------------------------------------------     <<    <<   <<   <<

        
        public void UpdateSmoothedVelocity()
        {
            float deltaTime = Time.time - lastTime;
            Vector3 currentPosition = transform.position;

            if (deltaTime > 0f)
            {
                Vector3 instantaneousVelocity = (currentPosition - lastPosition) / deltaTime;

                velocitySamples.Enqueue(instantaneousVelocity);
                if (velocitySamples.Count > velocitySampleSize)
                    velocitySamples.Dequeue();

                SmoothedVelocity = Vector3.zero;
                foreach (var sample in velocitySamples)
                    SmoothedVelocity += sample;

                SmoothedVelocity /= velocitySamples.Count;
            }

            lastPosition = currentPosition;
            lastTime = Time.time;
            
        }
        
        
        // ---------------- Method Break -------------------------------------------------------     <<    <<   <<   <<

        /// <summary>
        /// Returns the confidence score for this Lemming.
        /// TODO: This function should check for dropped tracking over a period of time and reflect that.
        /// </summary>
        /// <returns>A float between 0 and 1 representing confidence.</returns>
        public float GetConfidence()
        {
            // TODO: Currently returning a manually set value - needs to be dynamic
            return confidence;
        }
        
        // ---------------- Method Break -------------------------------------------------------     <<    <<   <<   <<

        private void Start()
        {
            RegisterWithShepherd();
        }
                
        // ---------------- Method Break -------------------------------------------------------     <<    <<   <<   <<

        private void OnDestroy()
        {
            if (LemmingShepherd.Instance != null)
            {
                LemmingShepherd.Instance.Unregister(this);
            }
        }
        
        // ---------------- Method Break -------------------------------------------------------     <<    <<   <<   <<

        /// <summary>
        /// This is the registration event with the Lemming Shepherd
        /// TODO: There may be additional check required here, such as ensuring that HasValidShepherd is true
        /// </summary>
        private void RegisterWithShepherd()
        {
                        
            if (LemmingShepherd.Instance != null)
            {
                LemmingShepherd.Instance.Register(this);
                if (!HasValidShepherd) {Debug.LogWarning($"Lemming '{Name}' is not a child of LemmingShepherd.");}
                
            }
            else
            {
                Debug.LogWarning($"Lemming '{Name}' could not register with a LemmingShepherd. Ensure one exists in the scene.");
            }
        }
        
        // ---------------- Method Break -------------------------------------------------------     <<    <<   <<   <<

        /// <summary>
        /// Returns true if this Lemming is under a valid LemmingContainer in the hierarchy.
        /// </summary>
        public bool HasValidShepherd
        {
            get
            {
                Transform current = transform;
                while (current != null)
                {
                    if (current.GetComponent<LemmingShepherd>() != null)
                        return true;

                    current = current.parent;
                }
                return false;
            }
        }
        
    }
}