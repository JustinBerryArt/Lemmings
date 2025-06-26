using System;
using System.Collections.Generic;
using System.Linq;
using Lemmings.Input;
using Lemmings.UI;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lemmings
{
    
    /// <summary>
    /// Singleton container responsible for managing active Lemmings and their associated relationships.
    /// Provides central tracking, registration, and runtime access to all active Lemming objects and their
    /// connections to LemmingRelationships, supporting UI, analysis, and dynamic manipulation across scenes.
    /// </summary>
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    [RequireComponent(typeof(LemmingDeviceManager))]
    [RequireComponent(typeof(LemmingUIVisibilityController))]
    public class LemmingShepherd : MonoBehaviour
    {
                
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Serialized Fields
        
        // This establishes the Singleton Instance so that the Shepherd can be an authoritative source
        public static LemmingShepherd Instance { get; private set; }

        /// <summary>
        /// Provides static access to all currently registered Lemmings.
        /// </summary>
        public static List<Lemming> All => Instance?.Lemmings;
        
        // This is the list of Lemmings that are children of the Shepherd
        public List<Lemming> Lemmings { get; private set; } = new();
        
        // This is the list of Lemming Relationships that contain shepherded Lemmings
        public List<LemmingRelationship> Relationships { get; private set; } = new();
        
        // This dictionary holds and compares the relevant Lemmings to those in Lemming Relationships
        public Dictionary<Lemming, List<LemmingRelationship>> LemmingToRelationships { get; private set; } = new();
        
        // This dictionary holds the Lemming Relationship and the associated Lemming Relationship Info
        public Dictionary<LemmingRelationship, LemmingRelationshipInfo> RelationshipDetails { get; private set; } = new();

        // This is a publicly accessible and serializable reference to Relationship Details for valid relationships
        public IEnumerable<(LemmingRelationship relationship, LemmingRelationshipInfo info)> ValidRelationships =>
            RelationshipDetails.Select(kvp => (kvp.Key, kvp.Value));
        
        // This is a publicly accessible and serializable reference to shepherded Lemmings
        public IEnumerable<Lemming> ValidLemmings => Lemmings.Where(l => l != null);
        
        #endregion
        
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Initialization (Awake) and Updating (Update)
        
        /// <summary>
        /// Establish the Instance as singular and set it to remain active through scene changes
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Find Relevant Lemmings and register them, as well as any relationships that use them
            InitializeHerd();
        }

        
        // ---------------- Method Break -------------------------------------------------------     <<    <<   <<   <<
        
        
        /// <summary>
        /// This updates the shepherd's dictionary and the Scriptable Object concurrently.
        /// The scriptable object can't update itself for each frame and so this calls it from the scene.
        /// This way you can access the data from either the SO or the Shepherd, depending on your project structure.
        /// </summary>
        private void Update()
        {
            foreach (var rel in Relationships)
            {
                // 1) Update the Shepherd’s own copy in the Dictionary:
                if (RelationshipDetails.TryGetValue(rel, out var shepherdDictionaryInfo))
                {
                    shepherdDictionaryInfo.RefreshDynamic();              // calls the struct’s method
                    RelationshipDetails[rel] = shepherdDictionaryInfo;    // store it back
                }

                // 2) Update the scriptable object’s internal cache:
                ref var scriptableObjectInfo = ref rel.CachedInfo;       // get ref to the SO’s _cachedInfo
                scriptableObjectInfo.RefreshDynamic();                   // refresh dynamic on it
                
                // 3) Compare the status to see if there is a change in order to fire off event
                rel.CompareStatus();
            }
        }
        
        
        #endregion
                
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Lemming Registration and Removal - Safety Checks and Maintence
        
        /// <summary>
        /// Registers a Lemming to the shepherd if it has not already been added.
        /// </summary>
        /// <param name="lemming">The Lemming component to be registered.</param>
        public void Register(Lemming lemming)
        {
            if (!Lemmings.Contains(lemming))
                Lemmings.Add(lemming);
        }
        
        // ---------------- Method Break -------------------------------------------------------     <<    <<   <<   <<

        /// <summary>
        /// Unregisters a Lemming and removes any relationship mappings associated with it.
        /// </summary>
        /// <param name="lemming">The Lemming component to be removed.</param>
        public void Unregister(Lemming lemming)
        {
            Lemmings.Remove(lemming);
            LemmingToRelationships.Remove(lemming);
        }
        
        /*
         
        // These are from earlier iterations and are temporarily retained for potential utility  
         
        // ---------------- Method Break -------------------------------------------------------     <<    <<   <<   <<

        /// <summary>
        /// This updates the Lemming References in the Lemming Relationship
        /// </summary>
        public void SyncAllReferences()
        {
            foreach (var rel in Relationships)
            {
                rel.SyncReferencesFromNames(); // You could implement this per relationship
            }
        }
        
                
        // ---------------- Method Break -------------------------------------------------------     <<    <<   <<   <<
        
        
        /// <summary>
        /// This is designed to purge any Lemmings or relationship that are returning null
        /// </summary>
        public void PurgeInvalidEntries()
        {
            Lemmings.RemoveAll(l => l == null);
            Relationships.RemoveAll(r => r == null);
        }
        */
        #endregion
        
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Relationship Registration and Removal - Safety Checks and Maintence

        
        /// <summary>
        /// Registers a LemmingRelationship, maps all involved Lemmings to it, and stores descriptive metadata.
        /// </summary>
        /// <param name="relationship">The LemmingRelationship instance to register.</param>
        public void RegisterRelationship(LemmingRelationship relationship)
        {
            // Add this relationship if not already present
            if (!Relationships.Contains(relationship))
                Relationships.Add(relationship);

            // intialize members
            var members = new List<(string, string)>();
            
            // This checks to see if the relationship has a shepherded Lemming and if so,
            // adds it to the dictionary LemmingToRelationships
            foreach (var reference in relationship.References)
            {
                var lemming = reference.Source?.GetComponent<Lemming>();
                if (lemming == null) continue;

                if (!LemmingToRelationships.ContainsKey(lemming))
                    LemmingToRelationships[lemming] = new List<LemmingRelationship>();

                if (!LemmingToRelationships[lemming].Contains(relationship))
                    LemmingToRelationships[lemming].Add(relationship);
                
                // lists the role and the name of the lemming
                string role = GetRoleForLemming(reference.name, relationship);
                members.Add((reference.name, role));
            }
            
            //TODO: Is this the best insurance or should I also use Sync Role from Reference function?
            relationship.SyncReferencesFromNames(); // <-- Force resync

            RelationshipDetails[relationship] = relationship.Info;
        }
        
        // ---------------- Method Break -------------------------------------------------------     <<    <<   <<   <<

        /// <summary>
        /// Attempts to resolve a role name for a Lemming based on its assigned name in the relationship mapping.
        /// </summary>
        /// <param name="name">The name of the Lemming.</param>
        /// <param name="relationship">The relationship context for resolution.</param>
        /// <returns>The assigned role if found, otherwise "Unknown".</returns>
        private string GetRoleForLemming(string name, LemmingRelationship relationship)
        {
            if (relationship.roleToName != null)
            {
                foreach (var keyValuePair in relationship.roleToName)
                {
                    if (keyValuePair.Value == name)
                        return keyValuePair.Key;
                }
            }
            return "Unknown";
        }
        
        // ---------------- Method Break -------------------------------------------------------     <<    <<   <<   <<

        /// <summary>
        /// Removes a LemmingRelationship from tracking, including cleaning up all lemming-to-relationship mappings.
        /// </summary>
        /// <param name="relationship">The LemmingRelationship instance to remove.</param>
        public void UnregisterRelationship(LemmingRelationship relationship)
        {
            if (!Relationships.Contains(relationship)) return;

            Relationships.Remove(relationship);
            RelationshipDetails.Remove(relationship);

            foreach (var keyValuePair in LemmingToRelationships.ToList())
            {
                keyValuePair.Value.Remove(relationship);
                if (keyValuePair.Value.Count == 0)
                {
                    LemmingToRelationships.Remove(keyValuePair.Key);
                }
            }
        }
        #endregion
        
        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Initialization and Validation Methods

        
        public void InitializeHerd()
        {
            Debug.Log("[LemmingShepherd] InitializeHerd() running");
            
            Lemmings.Clear();
            Relationships.Clear();
            LemmingToRelationships.Clear();
            RelationshipDetails.Clear();

            // Register all Lemmings that are children of this Shepherd
            foreach (var lemming in GetComponentsInChildren<Lemming>(includeInactive: true))
            {
                Register(lemming);
            }

            // Load all LemmingRelationships from Resources (requires proper setup)
            LemmingRelationship[] allRelationships = Resources.LoadAll<LemmingRelationship>("LemmingRelationships");

            foreach (var relationship in allRelationships)
            {
                if (relationship == null || relationship.Herd == null) continue;

                bool relevant = relationship.References.Any(r =>
                    r.Source != null &&
                    r.Source.GetComponent<Lemming>() is Lemming l &&
                    Lemmings.Contains(l));

                if (relevant)
                {
                    RegisterRelationship(relationship);
                }
            }
            
            var lemmings = GetComponentsInChildren<Lemming>(includeInactive: true);
            Debug.Log($"[LemmingShepherd] Found {lemmings.Length} lemmings");
        }
                
        // ---------------- Method Break -------------------------------------------------------     <<    <<   <<   <<

        /// <summary>
        /// Scans all registered relationships and removes any that no longer reference any registered Lemmings.
        /// Useful for cleaning up after scene changes or tracking loss.
        /// </summary>
        [ContextMenu("Validate Scene Relationships")]
        public void ValidateScene()
        {
            var orphaned = Relationships.Where(relationship =>
                !relationship.References.Any(lemmingReference =>
                    lemmingReference.Source != null &&
                    lemmingReference.Source.GetComponent<Lemming>() is Lemming l &&
                    Lemmings.Contains(l))
            ).ToList();

            foreach (var relationship in orphaned)
            {
                Debug.LogWarning($"Removing orphaned relationship: {relationship.name}");
                UnregisterRelationship(relationship);
            }
        }
        
        
        // ---------------- Method Break -------------------------------------------------------     <<    <<   <<   <<
        
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // If someone drags in a new list of Relationships or edits the Shepherd in the Inspector,
            // clear and re-seed every relationship’s cache so Info will rebuild correctly.
            foreach (var rel in Relationships)
            {
                if (rel == null) 
                    continue;

                // 1) Invalidate the SO’s internal cache
                rel.InvalidateCache();

                // 2) Update the Shepherd’s own copy in RelationshipDetails
                if (RelationshipDetails.ContainsKey(rel))
                    RelationshipDetails[rel] = rel.Info;    // full rebuild (static + dynamic)
                else
                    RelationshipDetails.Add(rel, rel.Info);
            }
        }
#endif
        
        
        #endregion
        
    }
}