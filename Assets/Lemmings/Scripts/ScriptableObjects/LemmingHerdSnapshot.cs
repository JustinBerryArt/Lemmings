using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine.Serialization;

namespace Lemmings
{
    /// <summary>
    /// A fixed, read-only snapshot of a Lemming Herd configuration.
    /// Stores Lemming names, confidence values, and their associated GameObjects for runtime reference.
    /// Also generates a unique enum per snapshot for consistent name referencing.
    /// </summary>
    
    // Lemming Herd Creation is done through the Herd Manager Window, not via the Assets window
    //[CreateAssetMenu(menuName = "Lemmings/Utilities/Lemming Herd Snapshot")]
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public class LemmingHerdSnapshot : ScriptableObject
    {
        
        /// <summary>
        /// This is the enum path for accessing the available Lemming Options.
        /// </summary>
        [HideInInspector]
        public string generatedEnumPath;
        
        /// <summary>
        /// The list of all lemmings in this herd snapshot.
        /// </summary>
        [HideInInspector]
        public List<LemmingReference> members = new List<LemmingReference>();

        /// <summary>
        /// This dictionary is meant to hold reference data more securely
        /// </summary>
        [NonSerialized]
        private Dictionary<string, LemmingReference> memberLookup;
        
        /// <summary>
        /// Gets all GameObjects in this snapshot.
        /// </summary>
        public IEnumerable<GameObject> AllObjects => members.ConvertAll(m => m.Source);

        /// <summary>
        /// Gets all lemming names in this snapshot.
        /// </summary>
        public IEnumerable<string> AllNames => members.ConvertAll(m => m.name);
        
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Finds a Lemming Entry's GameObject in the herd by its assigned name.
        /// </summary>
        /// <param name="name">The lemming name to search for.</param>
        /// <returns>The corresponding GameObject, or null if not found.</returns>
        public GameObject FindLemming(string name) => FindReference(name)?.Source;
        
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// Finds a Lemming Entry in the herd by its assigned name.
        /// </summary>
        /// <param name="name">The lemming name to search for.</param>
        /// <returns>The corresponding Reference, or null if not found.</returns>
        public LemmingReference? FindReference(string name)
        {
            foreach (var entry in members)
            {
                if (entry.name == name)
                    return entry;
                    //return entry.Source;
            }
            return null;
        }
        
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// This is a check to confirm the reference exists within the dictionary
        /// </summary>
        /// <param name="name">The name of the Lemming being sought</param>
        /// <param name="reference">The reference entry associated with that name</param>
        /// <returns></returns>
        public bool TryGetReference(string name, out LemmingReference reference)
        {
            if (memberLookup == null)
                RefreshLookup();

            return memberLookup.TryGetValue(name, out reference);
        }
        
        
        
        //_______________  METHOD BREAK   ___________________//


        /// <summary>
        /// This refreshes the dictionary with the member data
        /// </summary>
        public void RefreshLookup()
        {
            memberLookup = members
                .Where(m => !string.IsNullOrWhiteSpace(m.name))
                .ToDictionary(m => m.name, m => m);
        }
        
    }
}
