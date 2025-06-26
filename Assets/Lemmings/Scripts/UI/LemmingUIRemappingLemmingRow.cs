using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;


namespace Lemmings.UI
{
    
    /// <summary>
    /// Leaf‐level UI row in the remapping pane that lets the user reassign
    /// one <see cref="LemmingRelationshipInfo.Members"/> entry (role ↔ lemming).
    /// </summary>
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public class LemmingUIRemappingLemmingRow : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI roleText;
        [SerializeField] private TMP_Dropdown    dropdown;

        private string                            _role;
        private LemmingHerdSnapshot               _herd;
        public Action<string, string>            onRoleChanged;

        /// <summary>
        /// Bind this row to one member entry from the editing info.
        /// </summary>
        /// <param name="info">
        ///   The full relationship info struct, so we can grab its Herd snapshot.
        /// </param>
        /// <param name="member">
        ///   One tuple from <see cref="LemmingRelationshipInfo.Members"/>:
        ///   (currentName, roleLabel).
        /// </param>
        /// <param name="onRoleChanged">
        ///   Callback: (role, newName) → controller will update its <c>_editingInfo</c>.
        /// </param>
        public void Bind(
            LemmingRelationshipInfo info,
            (string LemmingName, string Role) member,
            Action<string, string> onRoleChanged
        )
        {
            // store locals
            _role           = member.Role;
            _herd           = info.Relationship.Herd;
            this.onRoleChanged  = onRoleChanged;

            // UI labels
            roleText.text = _role;

            // populate dropdown
            PopulateDropdown(member.LemmingName);
        }

        /// <summary>
        /// Fill the dropdown with all herd names, select the current one,
        /// and hook our listener.
        /// </summary>
        private void PopulateDropdown(string currentName)
        {
            var names = _herd.AllNames.ToList();

            dropdown.ClearOptions();
            dropdown.AddOptions(names);

            // select index
            int idx = names.IndexOf(currentName);
            dropdown.value = idx >= 0 ? idx : 0;

            // remove any old listeners, then add ours
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(newIndex =>
            {
                var chosen = names[newIndex];
                onRoleChanged?.Invoke(_role, chosen);
            });
        }
    }
}