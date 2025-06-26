using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Lemmings.UI;
using Lemmings.Utilities;
using Lemmings;
using UnityEngine.SceneManagement;

namespace Lemmings
{

    #region Console Window: Herd Manager (IMPORTANT)

    /// <summary>
    /// Custom Unity Editor Window (Herd Manager) for managing the current Lemming herd in the active scene.
    /// Allows inspection, renaming, and removal of Lemmings, and generation of stable herd snapshots.
    /// </summary>
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public class LemmingHerdWindow : EditorWindow
    {


        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Declartions and Intitialization

        // list for holding herd members
        private List<Lemming> herdMembers = new();

        // vector used to establish the scroll position
        private Vector2 scrollPosition;

        // holds the selected Lemmings when creating a snapshot
        private HashSet<Lemming> selectedForSnapshot = new();

        /// <summary>
        /// Opens the Lemming Herd Manager window via Unity's top menu.
        /// </summary>
        [MenuItem("Lemmings/Create Herd Snapshot with Herd Manager")]
        public static void ShowWindow()
        {
            GetWindow<global::Lemmings.LemmingHerdWindow>("Lemming Herd Manager");
        }

        /// <summary>
        /// When the window opens, it refreshes the scene to update available lemmings
        /// </summary>
        private void OnEnable()
        {
            RefreshSceneLemmings();
        }

        #endregion

        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region GUI

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Lemmings Active in Scene", EditorStyles.boldLabel);

            // Draw drag-and-drop area for adding GameObjects
            Rect dropArea = GUILayoutUtility.GetRect(0f, 50f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop GameObjects Here To Add/Activate them as Lemmings", EditorStyles.helpBox);

            // Call function to process the drag and drop
            HandleDragAndDrop(dropArea);

            // NOTE: Scroll view is an area to be sensitive to order of operations
            // Avoid 'Returns' unless certain that all actions are resolved or a delay is in effect
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Display each lemming in the current herd
            foreach (var lemming in herdMembers)
            {
                if (lemming == null) continue;

                EditorGUILayout.BeginVertical("box");

                string newName = EditorGUILayout.TextField("Name", lemming.Name);

                // if the name in the manager and on the item do not match, use the one from manager
                if (newName != lemming.Name)
                {
                    // Set this up to be undoable via edit > undo
                    Undo.RecordObject(lemming, "Set Lemming Name");
                    var so = new SerializedObject(lemming);
                    // This assigns a default name in the case that one does not exist or updates with a custom name if one exists
                    so.FindProperty("referenceName").stringValue =
                        string.IsNullOrEmpty(newName) ? lemming.DefaultName : newName;
                    so.ApplyModifiedProperties();
                }

                EditorGUILayout.LabelField("Confidence", lemming.GetConfidence().ToString("F2"));
                EditorGUILayout.ObjectField("GameObject", lemming.gameObject, typeof(GameObject), true);

                // true if the lemming is set to be included - part of hash set
                bool isSelected = selectedForSnapshot.Contains(lemming);
                // reflects the toggle visible in inspector to 'include in snapshot'
                bool newSelected = EditorGUILayout.Toggle("Include in Snapshot", isSelected);

                // Updates the hash set with the lemming if it has been set in inspector but is not currently in the set
                if (newSelected != isSelected)
                {
                    if (newSelected) selectedForSnapshot.Add(lemming);
                    else selectedForSnapshot.Remove(lemming);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // Create button to refresh the scene
            if (GUILayout.Button("Refresh Herd From Scene"))
            {
                RefreshSceneLemmings();
            }

            // Create button to build the herd snapshot used for building Lemming relationships
            if (GUILayout.Button("Create Snapshot From Selected Lemmings"))
            {
                CreateSnapshot();
            }

        }

        #endregion

        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Rebuilds the list of active Lemmings in the scene.
        /// </summary>
        private void RefreshSceneLemmings()
        {
            var allLemmings =
                GameObject.FindObjectsByType<Lemming>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var lemming in allLemmings)
            {
                if (!lemming.enabled) continue;

                // sets the default name if the name field has not been filled out
                if (string.IsNullOrWhiteSpace(lemming.Name))
                {
                    // Set up following actions to be undoable via edit > undo
                    Undo.RecordObject(lemming, "Auto-Assign Lemming Name");
                    var so = new SerializedObject(lemming);
                    so.FindProperty("referenceName").stringValue = lemming.DefaultName;
                    so.ApplyModifiedProperties();
                }
            }

            // Now build the herd list after names are guaranteed valid
            herdMembers = allLemmings
                // Where is filtering the list
                .Where(l => l.enabled && !string.IsNullOrWhiteSpace(l.Name))
                // ToList updates the modified list
                .ToList();
        }

        //_________________________________________________________________________
        //                          Method Break
        //_________________________________________________________________________


        /// <summary>
        /// Creates a new LemmingHerdSnapshot ScriptableObject from the current herd.
        /// </summary>
        private void CreateSnapshot()
        {
            // set a constant value for saving Lemming Herds to the project
            const string folderPath = "Assets/Resources/LemmingHerds";

            // If the directory does not exist, create it at the path location
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Setting for the 'Save As' window popup
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Herd Snapshot",
                "MyHerd",
                "asset",
                "Enter a name for this Lemming Herd",
                folderPath
            );

            // confirm the path is clean 
            if (string.IsNullOrEmpty(path)) return;

            // initialize the snapshot for set up and create the snapshot Scriptable Object data holder
            var snapshot = ScriptableObject.CreateInstance<LemmingHerdSnapshot>();

            // For each lemming the Herd Members list, add a lemming to the scriptable object 'members' list
            foreach (var lemming in herdMembers)
            {

                if (lemming == null || !selectedForSnapshot.Contains(lemming)) continue;
                snapshot.members.Add(new LemmingReference
                {
                    name = lemming.Name,
                    Source = lemming.Source,
                    confidence = lemming.GetConfidence()
                });
            }

            // This is part of a workflow designed to strongly associate herds with relevant scenes for more complex projects. 
            // It is not going to be implemented in current version but retained for future consideration.
/*
#if UNITY_EDITOR
        // Automatically associate the current scene
        string scenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogWarning("Scene must be saved before creating a snapshot.");
            return;
        }

        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        if (sceneAsset != null)
        {
            if (snapshot.associatedScenes == null)
                snapshot.associatedScenes = new List<SceneAsset>();
            if (!snapshot.associatedScenes.Contains(sceneAsset))
                snapshot.associatedScenes.Add(sceneAsset);
        }
        else
        {
            Debug.LogWarning("Could not resolve SceneAsset from path: " + scenePath);
        }
#endif
*/

            // Bake in the location of the custom Enum of names specific to this Herd
            // All Enums have the same prefix and this establishes that precedence for naming and retrieval
            snapshot.generatedEnumPath = "Lemmings.LemmingName";

            // create and save the Scriptable Object
            AssetDatabase.CreateAsset(snapshot, path);
            AssetDatabase.SaveAssets();

            // Run function to ensure clean names for enums and build the enum
            GenerateEnumSafe(snapshot);

            Debug.Log($"Lemming snapshot created at {path}");
        }

        //_________________________________________________________________________
        //                          Method Break
        //_________________________________________________________________________

        /// <summary>
        /// Generates a strongly typed enum representing all Lemming names in this snapshot.
        /// The goal of this is to ensure that if the user has not written out a valid enum, this will generate a valid option.
        /// </summary>
        /// <param name="snapshot"> This is the Herd Snashot being created and for which this enum will act as the Lemming Selection Reference</param>
        private void GenerateEnumSafe(LemmingHerdSnapshot snapshot)
        {
            // establish path and combine with the preferred filename
            string folderPath = "Assets/Resources/_Generated";
            string enumFilePath = Path.Combine(folderPath, $"LemmingName_{snapshot.name}.cs");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // create dictionary to ensure the names are all different
            Dictionary<string, int> nameCounts = new();
            List<string> uniqueNames = new();

            // check every reference
            foreach (var entry in snapshot.members)
            {
                // if the name has problems continue this fixer function
                if (string.IsNullOrWhiteSpace(entry.name)) continue;

                // create a safe name by replacing bad characters with _
                string safeName = Regex.Replace(entry.name, "[^a-zA-Z0-9_]+", "_");
                // add an underscore in front of numbers
                if (char.IsDigit(safeName, 0)) safeName = "_" + safeName;

                // add the name to the dictionary and add a counter if it already exists
                if (nameCounts.ContainsKey(safeName))
                {
                    nameCounts[safeName]++;
                    safeName += "_" + nameCounts[safeName];
                }
                else
                {
                    nameCounts[safeName] = 1;
                }

                // add the name to the list of unique and safe names
                uniqueNames.Add(safeName);
            }

            // Build the string that will become the enum script
            string enumContent = "namespace Lemmings\n{\n";
            enumContent += $"    public enum LemmingName_{snapshot.name}\n    {{\n";
            enumContent += "        None,\n";
            foreach (var n in uniqueNames)
            {
                enumContent += $"        {n},\n";
            }

            enumContent += "    }\n}";

            // Save the script with the enum
            File.WriteAllText(enumFilePath, enumContent);
            AssetDatabase.Refresh();
        }

        //_________________________________________________________________________
        //                          Method Break
        //_________________________________________________________________________



        /// <summary>
        /// Handles drag-and-drop input for assigning GameObjects to the Lemming herd.
        /// Adds Lemming component and sets a default name if needed.
        /// </summary>
        /// <param name="dropArea">The screen rect for drop detection.</param>
        private void HandleDragAndDrop(Rect dropArea)
        {
            // register the event
            Event newEvent = Event.current;

            // If the latest event is a drag related event and the mouse is in the drop box
            if ((newEvent.type == EventType.DragUpdated || newEvent.type == EventType.DragPerform) &&
                dropArea.Contains(newEvent.mousePosition))
            {
                // Copy references - as opposed to moving them, etc
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                // If it is a dragged object event
                if (newEvent.type == EventType.DragPerform)
                {
                    // Accept this event 
                    DragAndDrop.AcceptDrag();

                    // for every object contained in the drag and drop
                    foreach (var thisObject in DragAndDrop.objectReferences)
                    {
                        // if it is a game then reference it as 'go' for the following
                        if (thisObject is GameObject go)
                        {
                            // initialize lemming component of the object or add a lemming component if one does not exist
                            var lemming = go.GetComponent<Lemming>() ?? go.AddComponent<Lemming>();

                            // If the lemming is disabled, renable it and set it to update in the editor
                            if (!lemming.enabled)
                            {
                                lemming.enabled = true;
                                EditorUtility.SetDirty(lemming);
                            }

                            // Automatically assign GameObject name if blank
                            if (string.IsNullOrWhiteSpace(lemming.Name))
                            {
                                // make this action undoable through the Edit > Undo menu option
                                Undo.RecordObject(lemming, "Set Lemming Name");
                                var so = new SerializedObject(lemming);
                                so.FindProperty("referenceName").stringValue =
                                    lemming.DefaultName; // Use the field name, not the property
                                so.ApplyModifiedProperties();
                            }
                        }
                    }

                    // NOTE: Use() is important for basically telling unity that this event has been handled
                    newEvent.Use();
                    RefreshSceneLemmings();
                }
            }
        }

        #endregion
    }

    #endregion
    
//------------------------------------------------------
// XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
//------------------------------------------------------

    #region Editor: Lemming Herd Snapshot

    /// <summary>
    /// Custom editor for LemmingHerdSnapshot to display entries as strings
    /// rather than relying on Unity's default object drawer, which can show type mismatch.
    /// </summary>
    [CustomEditor(typeof(LemmingHerdSnapshot))]
    public class LemmingHerdSnapshotEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw default fields like the description and enum path
            DrawDefaultInspector();

// This is a feature intended to ensure system stability for complex projects with many scenes, currently not implemented
/*
        // This is meant to make sure that the Lemmings are all active and up to date
        // TODO: The button in the Lemming Relationship might be a better solution - evaluate this
        if (GUILayout.Button("Sync Lemmings to Scene"))
        {
            ((LemmingHerdSnapshot)target).SyncToScene();
        }
*/
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Herd Members", EditorStyles.boldLabel);

            var snapshot = (LemmingHerdSnapshot)target;

            // Warn if it is empty
            if (snapshot.members == null || snapshot.members.Count == 0)
            {
                EditorGUILayout.HelpBox("This snapshot contains no members.", MessageType.Info);
                return;
            }

            // For every Lemming Reference, provide information about it
            foreach (var entry in snapshot.members)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Name", entry.Name);
                EditorGUILayout.LabelField("Confidence", entry.confidence.ToString("F2"));
                string sourceName = entry.Source ? entry.Source.name : "<null>";
                EditorGUILayout.LabelField("Source Object", sourceName);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }
    }

    #endregion

//------------------------------------------------------
// XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
//------------------------------------------------------

    #region Editor: Lemming Relationship

    /// <summary>
    /// Custom inspector for <see cref="LemmingRelationship"/> ScriptableObject.
    /// Provides a full-featured editor interface for selecting members, resolving snapshot references,
    /// configuring relationship metrics, and previewing output values.
    /// </summary>
    [CustomEditor(typeof(LemmingRelationship))]
    public class LemmingRelationshipEditor : Editor
    {

        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Declarations and Initialization

        

        /// <summary>
        /// The active LemmingRelationship being edited.
        /// </summary>
        private LemmingRelationship relationship;

        /// <summary>
        /// Whether to show the Datum value preview section.
        /// </summary>
        private bool showDatumPreview = true;

        /// <summary>
        /// Whether to show the resolved members section.
        /// </summary>
        private bool showResolvedMembers = true;

        /// <summary>
        /// Cache to hold a change check comparison for metric seelction
        /// </summary>
        private Enum lastMetricEnum;
        
        /// <summary>
        /// Called when the editor is enabled.
        /// Caches a reference to the currently selected LemmingRelationship.
        /// </summary>
        private void OnEnable()
        {
            relationship = (LemmingRelationship)target;
        }

        #endregion

        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region GUI

        /// <summary>
        /// Draws the complete custom inspector interface.
        /// Includes default fields, role assignment, metric selection,
        /// preview output, and resolved member diagnostics.
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("➤ Member Assignment", EditorStyles.boldLabel);
            DrawFamilyMemberControls();

            if (GUILayout.Button("Finalize Member Assignment"))
            {
                relationship.InvalidateCache();
                DefineMembersFromSnapshot();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("➤ Method Selection", EditorStyles.boldLabel);
            DrawModeSelector();

            
            var methodEnum = relationship.ActiveMode as Enum;
            if (methodEnum != null)
            {
                // Provide contextual metadata for the selected metric
                EditorGUILayout.HelpBox(LemmingMetricMetadata.GetDescription(methodEnum), MessageType.Info);
                //TODO: Setting are currently viewable but require some testing and evaluation
                
                Enum currentMetricEnum = relationship.Metric;

                if (!Equals(currentMetricEnum, lastMetricEnum))
                {
                    relationship.OnMetricChanged();
                    lastMetricEnum = currentMetricEnum;
                }

                serializedObject.Update();
                
                DrawMetricSettingsDynamic();
                
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(new GUIContent("➤ Curve Settings", "Defines the curve used for remapping normalized values."), EditorStyles.boldLabel);
            relationship.curveType = (LemmingCurveType)EditorGUILayout.EnumPopup(new GUIContent("Curve Type", "Type of curve to use for remapping the output."), relationship.curveType);
            if (relationship.curveType == LemmingCurveType.Custom)
            {
                relationship.customCurve = EditorGUILayout.CurveField(new GUIContent("Custom Curve", "Custom animation curve applied to normalized values."), relationship.customCurve);
            }
            
            EditorGUILayout.Space(10);
            showDatumPreview = EditorGUILayout.Foldout(showDatumPreview, "➤ Value Output Preview", true);
            if (showDatumPreview)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawDatumPreview();
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);
            showResolvedMembers = EditorGUILayout.Foldout(showResolvedMembers, "➤ Resolved Members", true);
            if (showResolvedMembers)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawResolvedMembers();
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }

            EditorUtility.SetDirty(relationship);
        }

        #endregion

        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Drawing Methods
        
        
        private void DrawMetricSettingsDynamic()
        {
            //relationship.EnsureSettingsInitialized(); // Make sure _settings is ready
            //relationship.OnMetricChanged();           // Rehydrate if needed

            serializedObject.Update();

            SerializedProperty settingsProp = serializedObject.FindProperty("_settings");
            if (settingsProp == null)
            {
                EditorGUILayout.HelpBox("_settings property not found. Is it marked [SerializeField]?", MessageType.Error);
                return;
            }

            if (settingsProp.hasVisibleChildren)
            {
                Enum metric = relationship.Metric;
                FamilyType family = relationship.Family;

                var draw = LemmingMetricSettingUtility.GetMetricSettingDrawFunction(family, metric);

                if (draw != null)
                {
                    draw(settingsProp); // Executes the resolved drawing method
                }
                else
                {
                    EditorGUILayout.HelpBox($"No settings drawer found for {family}.{metric}.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Settings property is empty.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(relationship);
        }

        
        
        
        private void DrawMetricSettings()
        {

            serializedObject.Update(); // Begin change tracking

            SerializedProperty settingsProp = serializedObject.FindProperty("_settings");
            if (settingsProp == null || !settingsProp.hasVisibleChildren)
            {
                EditorGUILayout.HelpBox("_settings property not found or empty. Is it marked [SerializeField]?", MessageType.Error);
                return;
            }

            Enum metric = relationship.Metric;
            FamilyType family = relationship.Family;

            // Draw the correct settings UI
            switch (family)
            {
                case FamilyType.Single:
                    switch ((SingleMetric)metric)
                    {
                        case SingleMetric.Position:
                            
                            LemmingMetricSettingsDrawer.DrawSettingsSinglePosition(settingsProp);
                            break;
                        case SingleMetric.Rotation:
                            LemmingMetricSettingsDrawer.DrawSettingsSingleRotation(settingsProp);
                            break;
                        case SingleMetric.Movement:
                            LemmingMetricSettingsDrawer.DrawSettingsSingleMovement(settingsProp);
                            break;
                        case SingleMetric.Trigger:
                            LemmingMetricSettingsDrawer.DrawSettingsSingleTrigger(settingsProp);
                            break;
                    }
                    break;

                case FamilyType.Couple:
                    switch ((CoupleMetric)metric)
                    {
                        case CoupleMetric.Position:
                            LemmingMetricSettingsDrawer.DrawSettingsCouplePosition(settingsProp);
                            break;
                        case CoupleMetric.Rotation:
                            LemmingMetricSettingsDrawer.DrawSettingsCoupleRotation(settingsProp);
                            break;
                        case CoupleMetric.Movement:
                            LemmingMetricSettingsDrawer.DrawSettingsCoupleMovement(settingsProp);
                            break;
                        case CoupleMetric.Trigger:
                            LemmingMetricSettingsDrawer.DrawSettingsCoupleTrigger(settingsProp);
                            break;
                        case CoupleMetric.Distance:
                            LemmingMetricSettingsDrawer.DrawSettingsCoupleDistance(settingsProp);
                            break;
                        case CoupleMetric.Difference:
                            LemmingMetricSettingsDrawer.DrawSettingsCoupleDifference(settingsProp);
                            break;
                    }
                    break;

                case FamilyType.Throuple:
                    switch ((ThroupleMetric)metric)
                    {
                        case ThroupleMetric.Position:
                            LemmingMetricSettingsDrawer.DrawSettingsThrouplePosition(settingsProp);
                            break;
                        case ThroupleMetric.Rotation:
                            LemmingMetricSettingsDrawer.DrawSettingsThroupleRotation(settingsProp);
                            break;
                        case ThroupleMetric.Movement:
                            LemmingMetricSettingsDrawer.DrawSettingsThroupleMovement(settingsProp);
                            break;
                        case ThroupleMetric.Trigger:
                            LemmingMetricSettingsDrawer.DrawSettingsThroupleTrigger(settingsProp);
                            break;
                        case ThroupleMetric.Distance:
                            LemmingMetricSettingsDrawer.DrawSettingsThroupleDistance(settingsProp);
                            break;
                        case ThroupleMetric.RotationAroundAxis:
                            LemmingMetricSettingsDrawer.DrawSettingsThroupleRotationAroundAxis(settingsProp);
                            break;
                        case ThroupleMetric.Density:
                            LemmingMetricSettingsDrawer.DrawSettingsThroupleDensity(settingsProp);
                            break;
                        case ThroupleMetric.Angle:
                            LemmingMetricSettingsDrawer.DrawSettingsThroupleAngle(settingsProp);
                            break;
                        case ThroupleMetric.Size:
                            LemmingMetricSettingsDrawer.DrawSettingsThroupleSize(settingsProp);
                            break;
                    }
                    break;

                case FamilyType.Group:
                    switch ((GroupMetric)metric)
                    {
                        case GroupMetric.Position:
                            LemmingMetricSettingsDrawer.DrawSettingsGroupPosition(settingsProp);
                            break;
                        case GroupMetric.Rotation:
                            LemmingMetricSettingsDrawer.DrawSettingsGroupRotation(settingsProp);
                            break;
                        case GroupMetric.Movement:
                            LemmingMetricSettingsDrawer.DrawSettingsGroupMovement(settingsProp);
                            break;
                        case GroupMetric.Trigger:
                            LemmingMetricSettingsDrawer.DrawSettingsGroupTrigger(settingsProp);
                            break;
                        case GroupMetric.RotationAroundAxis:
                            LemmingMetricSettingsDrawer.DrawSettingsGroupRotationAroundAxis(settingsProp);
                            break;
                        case GroupMetric.Density:
                            LemmingMetricSettingsDrawer.DrawSettingsGroupDensity(settingsProp);
                            break;
                        case GroupMetric.Size:
                            LemmingMetricSettingsDrawer.DrawSettingsGroupSize(settingsProp);
                            break;
                    }
                    break;
            }

            serializedObject.ApplyModifiedProperties(); // Write changes back to object
            EditorUtility.SetDirty(relationship);       // Mark asset dirty (especially important for ScriptableObjects)
        }
        
        

        /// <summary>
        /// Draws dropdowns for selecting lemming names based on their role within the selected FamilyType.
        /// Automatically adjusts the number of inputs for single, couple, throuple, or group configurations.
        /// </summary>
        private void DrawFamilyMemberControls()
        {
            // Determine how many members are required based on family structure
            int requiredCount = relationship.Family switch
            {
                FamilyType.Single => 1,
                FamilyType.Couple => 2,
                FamilyType.Throuple => 3,
                FamilyType.Group => Mathf.Clamp(
                    EditorGUILayout.IntField("Group Size", relationship.selectedNames.Count),
                    1,
                    relationship.Herd?.members?.Count ?? 1),
                _ => 1
            };

            EnsureNameListCount(requiredCount);

            for (int i = 0; i < relationship.selectedNames.Count; i++)
            {
                string label = GetRoleLabel(i);
                relationship.selectedNames[i] =
                    DrawNameDropdown(label, relationship.selectedNames[i], relationship.Herd);
            }
        }

        //_________________________________________________________________________
        //                          Method Break
        //_________________________________________________________________________

        /// <summary>
        /// Draws an overview of the current value computed by the relationship.
        /// Displays type, raw value, min/max, normalized output, and curve-transformed value.
        /// </summary>
        private void DrawDatumPreview()
        {
            var datum = relationship.Datum;
            var type = datum?.Type.ToString() ?? "null";
            var raw = datum?.Value ?? "<null>";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Type", type);
            EditorGUILayout.LabelField("Value (raw)", raw.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min", relationship.Min.ToString("F2"));
            EditorGUILayout.LabelField("Max", relationship.Max.ToString("F2"));
            EditorGUILayout.EndHorizontal();

            if (relationship.Converter is ILemmingConverter converter)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Normalized", converter.Normalized.ToString("F3"));
                EditorGUILayout.LabelField("With Curve",
                    converter.ToCurve(relationship.GetActiveCurve()).ToString("F3"));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Normalized As Axis", relationship.Converter.AsAxis.ToString("F3"));
            }
            else
            {
                EditorGUILayout.HelpBox("This type has no previewable converter.", MessageType.Info);
            }
        }

        //_________________________________________________________________________
        //                          Method Break
        //_________________________________________________________________________

        /// <summary>
        /// Displays a breakdown of each resolved lemming reference.
        /// Shows names, source objects, and live confidence (if available).
        /// Includes a validation button to check for null or inactive references.
        /// </summary>
        private void DrawResolvedMembers()
        {
            if (relationship.selectedReferences == null || relationship.selectedReferences.Count == 0)
            {
                EditorGUILayout.HelpBox("No resolved members. Click 'Define Members' to set them.", MessageType.Info);
                return;
            }

            // Button to validate all references for activity and resolution
            if (GUILayout.Button("Validate All References"))
            {
                foreach (var reference in relationship.selectedReferences)
                {
                    var source = reference.Source;

                    if (source == null)
                    {
                        Debug.LogWarning($"Missing reference: '{reference.name}' could not be resolved.");
                    }
                    else if (!source.activeInHierarchy)
                    {
                        Debug.LogWarning($"Inactive reference: '{reference.name}' is in scene but not active.", source);
                    }
                    else
                    {
                        Debug.Log($"✅ '{reference.name}' is valid and active.");
                    }
                }

                EditorGUILayout.Space();
            }

            foreach (var reference in relationship.selectedReferences)
            {
                GameObject obj = reference.Source;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Name", reference.name);

                if (obj != null)
                {
                    EditorGUILayout.ObjectField("Source Object", obj, typeof(GameObject), true);

                    var lemming = obj.GetComponent<Lemming>();
                    if (lemming != null)
                    {
                        EditorGUILayout.LabelField("Confidence (Live)", lemming.GetConfidence().ToString("F2"));
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("GameObject could not be resolved.", MessageType.Warning);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        //_________________________________________________________________________
        //                          Method Break
        //_________________________________________________________________________

        /// <summary>
        /// Renders a dropdown of known lemming names from the assigned herd snapshot.
        /// </summary>
        /// <param name="label">Label for the dropdown field.</param>
        /// <param name="currentValue">Currently selected name.</param>
        /// <param name="herd">Snapshot to source name options from.</param>
        /// <returns>The name selected by the user.</returns>
        private string DrawNameDropdown(string label, string currentValue, LemmingHerdSnapshot herd)
        {
            if (herd == null || herd.members == null || herd.members.Count == 0)
            {
                EditorGUILayout.HelpBox("No herd snapshot assigned or empty herd.", MessageType.Info);
                return currentValue;
            }

            List<string> options = herd.members
                .Where(reference => !string.IsNullOrWhiteSpace(reference.name))
                .Select(reference => reference.name)
                .Distinct()
                .ToList();

            int currentIndex = Mathf.Max(0, options.IndexOf(currentValue));
            int newIndex = EditorGUILayout.Popup(label, currentIndex, options.ToArray());

            return options[newIndex];
        }

        //_________________________________________________________________________
        //                          Method Break
        //_________________________________________________________________________


        /// <summary>
        /// Renders a metric selection dropdown for the current family structure.
        /// Assigns the selected enum to the corresponding property.
        /// </summary>
        private void DrawModeSelector()
        {
            switch (relationship.Family)
            {
                case FamilyType.Single:
                    relationship.singleMetric =
                        (SingleMetric)EditorGUILayout.EnumPopup("Metric", relationship.singleMetric);
                    break;
                case FamilyType.Couple:
                    relationship.coupleMetric =
                        (CoupleMetric)EditorGUILayout.EnumPopup("Metric", relationship.coupleMetric);
                    break;
                case FamilyType.Throuple:
                    relationship.throupleMetric =
                        (ThroupleMetric)EditorGUILayout.EnumPopup("Metric", relationship.throupleMetric);
                    break;
                case FamilyType.Group:
                    relationship.groupMetric =
                        (GroupMetric)EditorGUILayout.EnumPopup("Metric", relationship.groupMetric);
                    break;
            }
        }

        #endregion

        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Safety and Export Methods

        /// <summary>
        /// Ensures the selectedNames list matches the expected number of members.
        /// Pads or trims as needed.
        /// </summary>
        /// <param name="requiredCount">Target number of entries to match the family type.</param>
        private void EnsureNameListCount(int requiredCount)
        {
            while (relationship.selectedNames.Count < requiredCount)
                relationship.selectedNames.Add("");

            while (relationship.selectedNames.Count > requiredCount)
                relationship.selectedNames.RemoveAt(relationship.selectedNames.Count - 1);
        }

        //_________________________________________________________________________
        //                          Method Break
        //_________________________________________________________________________

        /// <summary>
        /// Replaces selectedReferences by resolving each selectedName to a LemmingReference from the herd snapshot.
        /// </summary>
        private void DefineMembersFromSnapshot()
        {
            relationship.selectedReferences.Clear();
            foreach (var name in relationship.selectedNames)
            {
                var entry = relationship.Herd?.FindReference(name);
                if (entry.HasValue)
                    relationship.selectedReferences.Add(entry.Value);
            }

            EditorUtility.SetDirty(relationship);
            Debug.Log($"Defined {relationship.Members.Count} member(s) for '{relationship.name}'.");
        }

        //_________________________________________________________________________
        //                          Method Break
        //_________________________________________________________________________

        /// <summary>
        /// Returns a readable role label for a given index based on the selected family type.
        /// </summary>
        /// <param name="index">Index of the member in the relationship.</param>
        /// <returns>Formatted role label.</returns>
        private string GetRoleLabel(int index)
        {
            return relationship.Family switch
            {
                FamilyType.Single => "Lemming",
                FamilyType.Couple => index == 0 ? "Leader" : "Follower",
                FamilyType.Throuple => new[] { "Leader", "Second", "Third" }[index],
                FamilyType.Group => $"Lemming {index + 1}",
                _ => $"Lemming {index}"
            };
        }

        /// <summary>
        /// Creates a new Lemming Relationship in the correct Resources folder for loading into Shepherd and Input System
        ///   -  Resources/LemmingRelationships
        /// </summary>
        [MenuItem("Lemmings/Create New Relationship")]
        public static void CreateRelationship()
        {
            const string folderPath = "Assets/Resources/LemmingRelationships";

            // Ensure folder exists
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Prompt user for a name
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Lemming Relationship",
                "NewLemmingRelationship",
                "asset",
                "Enter a name for the new Lemming Relationship",
                folderPath
            );

            if (string.IsNullOrEmpty(path)) return;

            // Create the ScriptableObject
            var relationship = ScriptableObject.CreateInstance<Lemmings.LemmingRelationship>();

            // Assign default ID and description
            relationship.ID = Path.GetFileNameWithoutExtension(path);
            relationship.description = "New relationship created via menu";

            // Save the asset
            AssetDatabase.CreateAsset(relationship, path);
            AssetDatabase.SaveAssets();

            // Ping and select the new asset
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = relationship;
            EditorGUIUtility.PingObject(relationship);

            Debug.Log($"[Lemmings] Created new relationship at {path}");
        }




        #endregion

    }

    #endregion

//------------------------------------------------------
// XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
//------------------------------------------------------

    #region Editor: Lemming Shepherd

    /// <summary>
    /// Custom inspector for the <see cref="LemmingShepherd"/> component.
    /// Provides debugging and live editing tools for viewing and modifying
    /// the current list of registered Lemmings and their relationships.
    /// </summary>
    [CustomEditor(typeof(LemmingShepherd))]
    public class LemmingShepherdEditor : Editor
    {

        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Declarations and Initialization

        // reference to parent class
        private LemmingShepherd shepherd;

        // Is displaying Lemmings
        private bool showLemmings = true;

        // Is displaying Relationships
        private bool showRelationships = true;

        /// <summary>
        /// Dictionary to hold references to Lemming Relationship and organize them for layout
        /// </summary>
        private Dictionary<LemmingRelationship, bool> relationshipFoldouts = new();

        // Sets the toggle for whether or not to allow live editting of relationship data
        private bool liveEditMode = false;


        /// <summary>
        /// Called when the inspector is enabled. Caches the target LemmingShepherd reference.
        /// </summary>
        private void OnEnable()
        {
            shepherd = (LemmingShepherd)target;
            ValidateAndPopulate();
        }

        #endregion

        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region GUI

        /// <summary>
        /// Draws the custom inspector interface.
        /// Includes toggles, buttons, and foldouts for lemmings and relationships.
        /// </summary>
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Lemming Shepherd", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // This is the toggle to turn on or off Live Editting of data
            // When this is on, you can make active changes to the relationships
            liveEditMode = EditorGUILayout.Toggle("Live Edit Mode", liveEditMode);

            // This button is the manual method to re-sync the scene and populate the lemmings and relationships
            if (GUILayout.Button("Validate and Auto-Populate"))
                ValidateAndPopulate();

            EditorGUILayout.Space(10);

            // Draw Lemmings calls the function that populates and organizes any active lemmings
            DrawLemmings();

            EditorGUILayout.Space(10);

            // Draw Relationships display any relationships incorporated into the dictionary via validation
            DrawRelationships();
        }

        #endregion

        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Drawing Methods

        /// <summary>
        /// Renders the list of registered Lemmings in a foldout section.
        /// Displays their names and associated GameObjects.
        /// </summary>
        private void DrawLemmings()
        {

            // This is the foldout, which can be minimized, where Lemmings are shown with related objects and confidence settings
            showLemmings = EditorGUILayout.Foldout(showLemmings, $"Registered Lemmings ({shepherd.Lemmings.Count})");
            EditorGUILayout.Space();

            // If show Lemmings is false, this simply returns empty
            if (!showLemmings) return;

            // This button runs the GetConfidence() for each Lemming
            // and will later be used to update the confidence value based on tracking consistency
            if (GUILayout.Button("Update All Confidence Values"))
            {
                foreach (var lemming in shepherd.Lemmings)
                {
                    // TODO: Establish logic for confidence detection and evaluation

                    lemming.GetConfidence();
                }
            }

            EditorGUI.indentLevel++;

            // This loop runs for each lemming that the Shepherd recognizes as active and as it's child
            foreach (var lemming in shepherd.Lemmings)
            {
                if (lemming != null)
                {
                    // This Horizontal array hold the Lemming Confidence, Name, and Source per Lemming
                    EditorGUILayout.BeginHorizontal();

                    // This pulls the confidence value from the Lemming and provides a color visualization
                    // Green is full confidence, Red is Low confidence, with full red at .33f
                    float confidence = Mathf.Clamp01(lemming.GetConfidence());
                    float colorValue = Mathf.InverseLerp(0.33f, 1f, confidence);
                    Color confidenceColor = Color.Lerp(Color.red, Color.green, colorValue);

                    // This creates a tooltip to provide numeric data on confidence
                    GUIContent tooltip = new GUIContent(" ", $"Confidence: {confidence:F2}");
                    // This sets up style for a label that can produce the tooltip when hovered over
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    style.normal.background = Texture2D.whiteTexture;
                    Color originalColor = GUI.color;

                    // This creates the actual label to hold the color and handle the tooltip hovering feature
                    GUI.color = confidenceColor;
                    GUILayout.Label(tooltip, style, GUILayout.Width(14), GUILayout.Height(14));
                    GUI.color = originalColor;
                    //GUILayout.Space(4);

                    // Displays the Lemming Name
                    EditorGUILayout.LabelField(lemming.Name);

                    // Displays Lemming Source object in a disabled field to avoid changes
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(lemming.gameObject, typeof(GameObject), true);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUI.indentLevel--;
        }

        //_________________________________________________________________________
        //                          Method Break
        //_________________________________________________________________________

        /// <summary>
        /// Renders the list of active LemmingRelationships.
        /// Provides foldouts for individual relationship entries.
        /// </summary>
        private void DrawRelationships()
        {
            // Foldout to show Lemming Relationships
            showRelationships = EditorGUILayout.Foldout(showRelationships,
                $"Registered Relationships ({shepherd.RelationshipDetails.Count})");

            EditorGUILayout.Space(5);

            // Return empty if show relationships is set to off
            if (!showRelationships) return;

            EditorGUI.indentLevel++;

            // This processes each Relevant relationship and creates the foldout for that relationship
            foreach (var (relationship, info) in shepherd.RelationshipDetails.ToList())
            {
                // Show the relationship if it is registered in the dictionary
                if (!relationshipFoldouts.ContainsKey(relationship))
                    relationshipFoldouts[relationship] = true;

                // Create the foldout for the relationship and include in the header its information
                relationshipFoldouts[relationship] = EditorGUILayout.Foldout(
                    relationshipFoldouts[relationship],
                    $"{info.Id} [{info.FamilyType}] - {info.MetricName}",
                    true
                );

                if (!relationshipFoldouts[relationship]) continue;

                // Create the box for display
                EditorGUILayout.BeginVertical("box");
                // Fill the box with the Relationship entry data
                DrawRelationshipEntry(relationship, info);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUI.indentLevel--;
        }

        //_________________________________________________________________________
        //                          Method Break
        //_________________________________________________________________________

        /// <summary>
        /// Renders a single LemmingRelationship with its preview and editable fields.
        /// </summary>
        private void DrawRelationshipEntry(LemmingRelationship relationship, LemmingRelationshipInfo info)
        {
            EditorGUILayout.Space();

            // This button is a locator for finding the relationship being referenced
            if (GUILayout.Button("Select Relationship Asset", GUILayout.Width(200)))
            {
                EditorGUIUtility.PingObject(relationship);
                Selection.activeObject = relationship;
            }

            EditorGUILayout.Space();

            // The following section is enabled or disabled based on liveEditMode's value
            using (new EditorGUI.DisabledScope(!liveEditMode))
            {
                // --- Preview Value Section ---
                EditorGUILayout.LabelField("Output Preview:");

                if (relationship.Converter != null)
                {
                    // TODO: That has been adapted to calculate directly from the relationship but might be more efficient to display from info struct if it is updating correctly

                    // Calculates the output values
                    float normalized = relationship.Converter.Normalized;
                    float curved = relationship.Converter.ToCurve(relationship.Curve);
                    string raw = relationship.Datum.Value.ToString();

                    // Displays the output values
                    // NOTE: This vertical layout resolves further down, after the status update
                    EditorGUILayout.BeginVertical(style: "box");
                    EditorGUILayout.LabelField($"  Raw: {raw}");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"  Normalized: {normalized:F3}");
                    EditorGUILayout.LabelField($"  From Curve: {curved:F3}");
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.LabelField("Axis", relationship.Converter.AsAxis.ToString("F2"));
                }
                else
                {
                    EditorGUILayout.HelpBox("No converter available for this relationship.", MessageType.Info);
                }

                // TODO: That has been adapted to calculate directly from the relationship but might be more efficient to display from info struct if it is updating correctly
                string status = relationship.Status.ToString("F");


                if (relationship.Converter != null)
                {
                    EditorGUILayout.LabelField($"  Status: {status}");
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("No converter available for this relationship.", MessageType.Info);
                }


                // Start listening for user input
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.Space();

                // --- Editable Min/Max ---
                float newMin = EditorGUILayout.FloatField("Min", info.Min);
                float newMax = EditorGUILayout.FloatField("Max", info.Max);


                EditorGUILayout.BeginHorizontal();

                // This button prompts users to set the min or max based on the current value
                // PromptSampleValue opens dialog with options
                GUILayout.FlexibleSpace();
                if (liveEditMode && GUILayout.Button("Set Min/Max From Scene", GUILayout.Width(200)))
                    PromptSampleValue(relationship, info);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                // --- Editable Curve Type ---
                // Select from an existing suite of curve options to preprocess data
                LemmingCurveType newCurveType = (LemmingCurveType)EditorGUILayout.EnumPopup("Curve Type", info.CurveType);
                AnimationCurve newCustomCurve = info.CustomCurve;

                if (newCurveType == LemmingCurveType.Custom)
                {
                    newCustomCurve = EditorGUILayout.CurveField("Custom Curve", info.CustomCurve);
                }
                
                SerializedObject serializedRelationship = new SerializedObject(relationship);
                
                LemmingMetricSettingsDrawer.DrawMetricSettings(serializedRelationship, relationship);

                // After changes are made, propagate through project
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(relationship, "Modify Relationship Properties");

                    info.Min = newMin;
                    info.Max = Mathf.Max(newMin + 0.01f, newMax);
                    info.CurveType = newCurveType;
                    info.CustomCurve = newCustomCurve;

                    relationship.min = info.Min;
                    relationship.max = info.Max;
                    relationship.curveType = newCurveType;
                    relationship.customCurve = newCustomCurve;

                    shepherd.RelationshipDetails[relationship] = info;
                    EditorUtility.SetDirty(relationship);
                }

                EditorGUILayout.Space();

                // --- Members Dropdown ---
                EditorGUILayout.LabelField("Members:");
                if (relationship.Herd != null && relationship.selectedNames != null)
                {
                    for (int i = 0; i < relationship.selectedNames.Count; i++)
                    {
                        // Setting names from the selections made in the relationship
                        string currentName = relationship.selectedNames[i];
                        // Establish role names based on the family type
                        string role = relationship.Family switch
                        {
                            FamilyType.Single => "Lemming",
                            FamilyType.Couple => i == 0 ? "Leader" : "Follower",
                            FamilyType.Throuple => new[] { "Leader", "Follower", "Third" }[i],
                            FamilyType.Group => $"Member {i + 1}",
                            _ => $"Member {i}"
                        };

                        // Create a list of the names in the herd snapshot as being available for remapping
                        List<string> options = relationship.Herd.AllNames.ToList();
                        int currentIndex = Mathf.Max(0, options.IndexOf(currentName));
                        int newIndex = EditorGUILayout.Popup(role, currentIndex, options.ToArray());

                        // This updates the Lemming references in the relationship if changed
                        if (newIndex != currentIndex)
                        {
                            relationship.selectedNames[i] = options[newIndex];
                            var lemmingReference = relationship.Herd.FindReference(options[newIndex]);

                            if (lemmingReference.HasValue)
                            {
                                if (relationship.selectedReferences.Count > i)
                                    relationship.selectedReferences[i] = lemmingReference.Value;
                                else
                                    relationship.selectedReferences.Add(lemmingReference.Value);

                                info.Members[i] = (options[newIndex], role);
                                shepherd.RelationshipDetails[relationship] = info;
                                EditorUtility.SetDirty(relationship);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        //------------------------------------------------------
        // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
        //------------------------------------------------------

        #region Helper Methods and Validation

        /// <summary>
        /// Opens a delay prompt and then samples a value from the relationship after a timer.
        /// Sets it as either the min or max.
        /// </summary>
        private void PromptSampleValue(LemmingRelationship relationship, LemmingRelationshipInfo info)
        {
            // This is a utilization of an embedded Dialog option reskinned to offer timing delay options
            float delay =
                EditorUtility.DisplayDialogComplex("Apply Delay?", "Apply a delay before sampling?", "0 sec", "3 sec",
                        "5 sec") switch
                    {
                        0 => 0f,
                        1 => 3f,
                        2 => 5f,
                        _ => 0f
                    };

            // Sets the SetMin bool based on user selection
            bool setMin = EditorUtility.DisplayDialog("Set Min or Max", "Set as Minimum?", "Min", "Max");

            // This delays the update and then sets the min or max to match the current value
            EditorApplication.delayCall += () =>
            {
                var timer = new System.Diagnostics.Stopwatch();
                timer.Start();
                EditorApplication.update += SampleAfterDelay;

                void SampleAfterDelay()
                {
                    if (timer.Elapsed.TotalSeconds < delay) return;
                    EditorApplication.update -= SampleAfterDelay;
                    timer.Stop();

                    float sampledValue = 0f;
                    var datum = relationship.Datum;

                    // Switch for using the coreect method based on the value type
                    switch (datum.Type)
                    {
                        case LemmingValueType.Quaternion:
                            sampledValue = (relationship.Converter as LemmingRotater?)?.Angle() ?? 0f;
                            break;
                        case LemmingValueType.Vector3:
                            sampledValue = datum.AsVector3().magnitude;
                            break;
                        case LemmingValueType.Float:
                            sampledValue = datum.AsFloat();
                            break;
                        case LemmingValueType.Int:
                            sampledValue = datum.AsInt();
                            break;
                        case LemmingValueType.Bool:
                            sampledValue = datum.AsBool() ? 1f : 0f;
                            break;
                    }

                    // Set the min
                    if (setMin)
                    {
                        info.Min = sampledValue;
                        relationship.min = sampledValue;
                    }
                    else
                        // Set the max
                    {
                        info.Max = Mathf.Max(sampledValue, info.Min + 0.01f);
                        relationship.max = info.Max;
                    }

                    shepherd.RelationshipDetails[relationship] = info;
                    EditorUtility.SetDirty(relationship);

                    Debug.Log($"Sampled {(setMin ? "Min" : "Max")} set to {sampledValue:F3} after {delay} sec");
                }
            };
        }

        //_________________________________________________________________________
        //                          Method Break
        //_________________________________________________________________________

        /// <summary>
        /// Refreshes all registered Lemmings and Relationships in the Shepherd from the current scene and project.
        /// </summary>
        private void ValidateAndPopulate()
        {
            if (shepherd == null) return;

            // Clear all current values
            shepherd.Lemmings.Clear();
            shepherd.Relationships.Clear();
            shepherd.LemmingToRelationships.Clear();
            shepherd.RelationshipDetails.Clear();

            // Checks children of the Shepherd for Lemmings and adds them to the list
            foreach (var lemming in shepherd.GetComponentsInChildren<Lemming>())
                shepherd.Register(lemming);

            // Searches project for LemmingRelationship Assets
            string[] interfaceAssets = AssetDatabase.FindAssets("t:LemmingRelationship");

            // Process each relationship found and for each one that has a Lemming owned by this shepherd, register it to the shepherd
            foreach (string relationshipReference in interfaceAssets)
            {
                string path = AssetDatabase.GUIDToAssetPath(relationshipReference);
                var relationship = AssetDatabase.LoadAssetAtPath<LemmingRelationship>(path);

                if (relationship == null) continue;

                bool relevant = relationship.References.Any(r =>
                    r.Source != null &&
                    r.Source.GetComponent<Lemming>() is Lemming l &&
                    shepherd.Lemmings.Contains(l));

                if (relevant)
                    shepherd.RegisterRelationship(relationship);
            }
        }

        #endregion
    }

    #endregion

//------------------------------------------------------
// XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
//------------------------------------------------------

    #region Editor: Lemming Class

    /// <summary>
    /// Custom editor for the <see cref="Lemming"/> component.
    /// Provides additional inspector functionality to validate snapshot references.
    /// </summary>
    [CustomEditor(typeof(Lemming))]
    public class LemmingEditor : Editor
    {
        /// <summary>
        /// A reference to the currently inspected Lemming component.
        /// </summary>
        private Lemming lemming;

        /// <summary>
        /// Called when the editor is enabled.
        /// Caches a reference to the target <see cref="Lemming"/> component.
        /// Also registers the Lemming with the Shepherd
        /// </summary>
        private void OnEnable()
        {
            lemming = (Lemming)target;

            // Only register if we're not in play mode
            if (!Application.isPlaying && LemmingShepherd.Instance != null)
            {
                LemmingShepherd.Instance.Register(lemming);
            }
        }

        /// <summary>
        /// Used when the component is disabled to remove it from the Shepherd
        /// </summary>
        private void OnDisable()
        {
            if (!Application.isPlaying && LemmingShepherd.Instance != null)
            {
                LemmingShepherd.Instance.Unregister(lemming);
            }
        }


        /// <summary>
        /// Draws the custom inspector GUI, including the default inspector
        /// and a button to check if this Lemming is referenced by any herd snapshot.
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!lemming.HasValidShepherd)
            {
                EditorGUILayout.HelpBox(
                    "This Lemming is not parented under a GameObject with a LemmingShepherd script.",
                    MessageType.Warning
                );
            }

            if (GUILayout.Button("Check Snapshot References"))
            {
                CheckSnapshotReferences();
            }
        }



        /// <summary>
        /// Checks all <see cref="LemmingHerdSnapshot"/> assets in the project
        /// to see if any of them reference this Lemming's GameObject.
        /// Logs a warning for each match to inform the user that this Lemming
        /// must be active to function properly.
        /// </summary>
        private void CheckSnapshotReferences()
        {

            //Debug.Log($"Lemming '{lemming.Name}' {lemming.Source} {lemming.DefaultName}.");

            var allSnapshots = AssetDatabase.FindAssets("t:LemmingHerdSnapshot");

            foreach (var guid in allSnapshots)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var snapshot = AssetDatabase.LoadAssetAtPath<LemmingHerdSnapshot>(path);

                foreach (var reference in snapshot.members)
                {
                    if (reference.Source == lemming.gameObject)
                    {
                        Debug.LogWarning(
                            $"Lemming '{lemming.Name}' is referenced in snapshot '{snapshot.name}' and needs to be enabled to function.",
                            lemming
                        );
                    }
                }
            }
        }
    }

    #endregion

//------------------------------------------------------
// XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
//------------------------------------------------------

    #region Editor: Lemming Relationship Proxy

    
    /// <summary>
    /// This is the editor script for the Relationship Proxy.
    /// It is primarily designed to accomodate an up-to-date
    /// display for the ID, which is determined when the relationship changes
    /// </summary>
    [CustomEditor(typeof(LemmingRelationshipProxy))]
    public class LemmingRelationshipProxyEditor : Editor
    {
        private LemmingRelationshipProxy proxy;
        private SerializedProperty onTriggerDetectedProp;
        private SerializedProperty onTriggerExitedProp;
        private SerializedProperty onGazeDetectedProp;
        private bool showEvents = false;
        private void OnEnable()
        {
            proxy = (LemmingRelationshipProxy)target;
            onTriggerDetectedProp = serializedObject.FindProperty("OnTriggerDetected");
            onTriggerExitedProp = serializedObject.FindProperty("OnTriggerExited");
            onGazeDetectedProp = serializedObject.FindProperty("OnGazeDetected");
        }

        public override void OnInspectorGUI()
        {
            // Let Unity draw all fields normally
            DrawDefaultInspector();

            EditorGUILayout.Space();
            showEvents = EditorGUILayout.Foldout(showEvents, "➤ Unity Events", true);
            if (showEvents)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(onTriggerDetectedProp);
                EditorGUILayout.PropertyField(onTriggerExitedProp);
                EditorGUILayout.PropertyField(onGazeDetectedProp);
                EditorGUI.indentLevel--;
            }
            
            
            // Update the ID field based on the relationship (if set)
            if (proxy.relationship != null)
            {
                string expectedID = $"{proxy.relationship.ID}_proxy";
                if (proxy.ID != expectedID)
                {
                    Undo.RecordObject(proxy, "Update Proxy ID");
                    proxy.ID = expectedID;
                    EditorUtility.SetDirty(proxy);
                }
            }
            else
            {
                proxy.ID = "<null>";
            }

            

        }
    }
    

    #endregion
    
//------------------------------------------------------
// XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
//------------------------------------------------------

    #region Editor: Lemming Secondary Metric
    [CustomEditor(typeof(LemmingSecondaryMetric))]
    public class LemmingSecondaryMetricEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var previewer = (LemmingSecondaryMetric)target;

            var propRelationship = serializedObject.FindProperty("relationship");
            var propUseThresholds = serializedObject.FindProperty("useNewThresholds");
            var propMin = serializedObject.FindProperty("min");
            var propMax = serializedObject.FindProperty("max");

            var propChangeCurve = serializedObject.FindProperty("changeCurve");
            var propCurveType = serializedObject.FindProperty("curveType");
            var propCurve = serializedObject.FindProperty("curve");

            var propUseAdvanced = serializedObject.FindProperty("useAdvancedSettings");
            var propSettings = serializedObject.FindProperty("secondarySettings");

            EditorGUILayout.PropertyField(propRelationship);
            EditorGUILayout.PropertyField(propUseThresholds);

            if (propUseThresholds.boolValue)
            {
                EditorGUILayout.PropertyField(propMin);
                EditorGUILayout.PropertyField(propMax);
            }

            EditorGUILayout.PropertyField(propChangeCurve);
            if (propChangeCurve.boolValue)
            {
                EditorGUILayout.PropertyField(propCurveType);
                if ((LemmingCurveType)propCurveType.enumValueIndex == LemmingCurveType.Custom)
                {
                    EditorGUILayout.PropertyField(propCurve);
                }
            }


        
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Metric", EditorStyles.boldLabel);

            if (propRelationship.objectReferenceValue is LemmingRelationship rel)
            {
                switch (rel.Family)
                {
                    case FamilyType.Single:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("singleMetric"));
                        break;
                    case FamilyType.Couple:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("coupleMetric"));
                        break;
                    case FamilyType.Throuple:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("throupleMetric"));
                        break;
                    case FamilyType.Group:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("groupMetric"));
                        break;
                }
            
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a relationship to view the relevant metric.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(propUseAdvanced);
            serializedObject.ApplyModifiedProperties(); // Make sure toggle change is committed

            if (propUseAdvanced.boolValue)
            {
                LemmingMetricSettingsDrawer.DrawSecondaryMetricSettings(serializedObject, previewer, propSettings);
            }
        
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Preview Output", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Raw", previewer.Raw?.ToString() ?? "<null>");
            EditorGUILayout.LabelField("Normalized", previewer.Normalized.ToString("F3"));
            EditorGUILayout.LabelField("Curved", previewer.Curved.ToString("F3"));
            EditorGUILayout.LabelField("In Range", previewer.InRange.ToString());
            EditorGUILayout.LabelField("Under", previewer.Under.ToString());
            EditorGUILayout.LabelField("Over", previewer.Over.ToString());

            if (GUILayout.Button("Refresh Preview"))
            {
                previewer.Refresh();
            }

            if (GUILayout.Button("Revert to original"))
            {
                previewer.RevertToOriginal();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    #endregion
    
//------------------------------------------------------
// XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
//------------------------------------------------------

    #region Min Max Slider for Editor

    /// <summary>
    /// Attribute for displaying a slider with editable min and max values in the inspector.
    /// Should be used on a Vector2 field where x = min, y = max.
    /// </summary>
    public class MinMaxSliderAttribute : PropertyAttribute
    {
        /// <summary>The minimum allowed value on the slider.</summary>
        public readonly float min;

        /// <summary>The maximum allowed value on the slider.</summary>
        public readonly float max;

        /// <summary>
        /// Creates a new MinMaxSlider attribute with a defined range.
        /// </summary>
        /// <param name="min">The lowest allowed value.</param>
        /// <param name="max">The highest allowed value.</param>
        public MinMaxSliderAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }

    /// <summary>
    /// Custom drawer for MinMaxSliderAttribute. Supports Vector2 and Vector2Int.
    /// Renders two numeric fields and a slider to control min/max ranges.
    /// </summary>
    [CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
    public class MinMaxSliderDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            MinMaxSliderAttribute range = (MinMaxSliderAttribute)attribute;

            // Setup layout slices
            float labelWidth = EditorGUIUtility.labelWidth;
            float fieldWidth = 50f;
            float padding = 4f;

            Rect labelRect = new Rect(position.x, position.y, labelWidth, position.height);
            Rect minRect = new Rect(position.x + labelWidth, position.y, fieldWidth, position.height);
            Rect sliderRect = new Rect(position.x + labelWidth + fieldWidth + padding, position.y,
                position.width - labelWidth - 2 * fieldWidth - 2 * padding, position.height);
            Rect maxRect = new Rect(position.xMax - fieldWidth, position.y, fieldWidth, position.height);

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.LabelField(labelRect, label);

            if (property.propertyType == SerializedPropertyType.Vector2)
            {
                Vector2 current = property.vector2Value;

                current.x = EditorGUI.FloatField(minRect, current.x);
                current.y = EditorGUI.FloatField(maxRect, current.y);

                current.x = Mathf.Clamp(current.x, range.min, range.max);
                current.y = Mathf.Clamp(current.y, current.x, range.max);

                EditorGUI.MinMaxSlider(sliderRect, ref current.x, ref current.y, range.min, range.max);
                property.vector2Value = current;
            }
            else if (property.propertyType == SerializedPropertyType.Vector2Int)
            {
                Vector2Int current = property.vector2IntValue;

                // Draw editable int fields
                int min = EditorGUI.IntField(minRect, current.x);
                int max = EditorGUI.IntField(maxRect, current.y);

                // Clamp and sync
                min = Mathf.Clamp(min, Mathf.RoundToInt(range.min), Mathf.RoundToInt(range.max));
                max = Mathf.Clamp(max, min, Mathf.RoundToInt(range.max));

                // Convert to float for slider
                float fMin = min;
                float fMax = max;
                EditorGUI.MinMaxSlider(sliderRect, ref fMin, ref fMax, range.min, range.max);

                // Convert back
                current.x = Mathf.RoundToInt(fMin);
                current.y = Mathf.RoundToInt(fMax);
                property.vector2IntValue = current;
            }
            else
            {
                EditorGUI.HelpBox(position, "MinMaxSlider only supports Vector2 or Vector2Int", MessageType.Warning);
            }

            EditorGUI.EndProperty();
        }
    }

    #endregion
    
//------------------------------------------------------
// XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
//------------------------------------------------------

    #region Read Only Drawer

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }

    #endregion 
    
    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------

    #region Tag Selection Drawer

    [CustomPropertyDrawer(typeof(TagSelectorAttribute))]
    public class TagSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use [TagSelector] with strings.");
            }
        }
    }
    

    #endregion

//------------------------------------------------------
// XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
//------------------------------------------------------

    #region Menu Tool: Create Game Objects from Prefabs

    public static class LemmingPrefabInstantiater
    {
        private const string prefabShepherdPath = "Assets/Lemmings/Prefabs/LemmingShepherd.prefab";
        private const string prefabProxyPath = "Assets/Lemmings/Prefabs/LemmingRelationshipProxy.prefab";
        private const string prefabSecondaryMetricPath = "Assets/Lemmings/Prefabs/LemmingSecondaryMetric.prefab";
        private const string prefabUIPath = "Assets/Lemmings/Prefabs/LemmingUI.prefab";
        private const string prefabDefaultPath = "Assets/Lemmings/Prefabs/LemmingDefaultSetup.prefab";

        //-------------------------------------------------------------------------------
        //                           Create Shepherd
        //-------------------------------------------------------------------------------
        
        
        [MenuItem("Lemmings/Game Objects/Create Lemming Shepherd (Singleton)")]
        public static void InstantiateLemmingShepherdPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabShepherdPath);
            if (prefab == null)
            {
                Debug.LogError($"[LemmingPrefabCreator] Prefab not found at path: {prefabShepherdPath}");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            // Position it at the scene view center
            if (SceneView.lastActiveSceneView != null)
            {
                var scenePos = SceneView.lastActiveSceneView.pivot;
                instance.transform.position = scenePos;
            }

            // Register undo and mark scene dirty
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Lemming Shepherd");
            EditorGUIUtility.PingObject(instance);
            Selection.activeGameObject = instance;
        }

        //-------------------------------------------------------------------------------
        //                           Create Proxy
        //-------------------------------------------------------------------------------
        
        [MenuItem("Lemmings/Game Objects/Create Lemming Relationship Proxy")]
        public static void InstantiateLemmingRelationshipProxyPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabProxyPath);
            if (prefab == null)
            {
                Debug.LogError($"[LemmingPrefabCreator] Prefab not found at path: {prefabProxyPath}");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            // Position it at the scene view center
            if (SceneView.lastActiveSceneView != null)
            {
                var scenePos = SceneView.lastActiveSceneView.pivot;
                instance.transform.position = scenePos;
            }

            // Register undo and mark scene dirty
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Lemming Proxy");
            EditorGUIUtility.PingObject(instance);
            Selection.activeGameObject = instance;
        }

        //-------------------------------------------------------------------------------
        //                           Create Secondary Metric
        //-------------------------------------------------------------------------------
        
        [MenuItem("Lemmings/Game Objects/Create Secondary Metric")]
        public static void InstantiateLemmingSecondaryMetricPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabSecondaryMetricPath);
            if (prefab == null)
            {
                Debug.LogError($"[LemmingPrefabCreator] Prefab not found at path: {prefabSecondaryMetricPath}");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            // Position it at the scene view center
            if (SceneView.lastActiveSceneView != null)
            {
                var scenePos = SceneView.lastActiveSceneView.pivot;
                instance.transform.position = scenePos;
            }

            // Register undo and mark scene dirty
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Lemming Secondary Metric");
            EditorGUIUtility.PingObject(instance);
            Selection.activeGameObject = instance;
        }

        //-------------------------------------------------------------------------------
        //                           Create UI
        //-------------------------------------------------------------------------------
        
        [MenuItem("Lemmings/Game Objects/Create Lemming UI")]
        public static void InstantiateLemmingUIPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabUIPath);
            if (prefab == null)
            {
                Debug.LogError($"[LemmingPrefabCreator] Prefab not found at path: {prefabUIPath}");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            // Position it at the scene view center
            if (SceneView.lastActiveSceneView != null)
            {
                var scenePos = SceneView.lastActiveSceneView.pivot;
                instance.transform.position = scenePos;
            }

            // Register undo and mark scene dirty
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Lemming UI");
            EditorGUIUtility.PingObject(instance);
            Selection.activeGameObject = instance;
        }

        //-------------------------------------------------------------------------------
        //                           Create Default Setup
        //-------------------------------------------------------------------------------
        
        [MenuItem("Lemmings/Generate Default Setup")]
        public static void InstantiateLemmingDefaultPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabDefaultPath);
            if (prefab == null)
            {
                Debug.LogError($"[LemmingPrefabCreator] Prefab not found at path: {prefabDefaultPath}");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            // Position it at the scene center

            instance.transform.position = new Vector3(0, 0, 0);
            
            // Register undo and mark scene dirty
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Lemming Default Setup");
            EditorGUIUtility.PingObject(instance);
            Selection.activeGameObject = instance;
        }
        
    }

    #endregion
    
//------------------------------------------------------
// XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
//------------------------------------------------------
    
    #region Drawer for metric setting options
    

    /// <summary>
    /// Draws inspector UI for settings relevant to each Lemming metric.
    /// Settings shown depend on the metric and family type.
    /// </summary>
    public static class LemmingMetricSettingsDrawer
    {

        private static bool showAdvancedSettings = false;

        /// <summary>
        /// Draw Metric settings option for Secondary Metrics
        /// </summary>
        /// <param name="serializedObject">The Object being editted</param>
        /// <param name="secondary">Secondary Metric Class</param>
        /// <param name="settings">The Serialized Setting reference</param>
        public static void DrawSecondaryMetricSettings(SerializedObject serializedObject, LemmingSecondaryMetric secondary, SerializedProperty settings)
        {
            if (secondary == null || serializedObject == null)
            {
                EditorGUILayout.HelpBox("Missing reference to relationship or serialized object.", MessageType.Error);
                return;
            }

            serializedObject.Update();

            SerializedProperty settingsProp = settings;
            if (settingsProp == null)
            {
                EditorGUILayout.HelpBox("_settings property not found. Is it marked [SerializeField]?", MessageType.Error);
                return;
            }

            if (settingsProp.hasVisibleChildren)
            {
                Enum metric = secondary.GetSelectedMetric(secondary.relationship.Family);
                FamilyType family = secondary.relationship.Family;

                var draw = LemmingMetricSettingUtility.GetMetricSettingDrawFunction(family, metric);

                if (draw != null)
                {
                    draw(settingsProp);
                }
                else
                {
                    EditorGUILayout.HelpBox($"No settings drawer found for {family}.{metric}.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Settings property is empty.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(secondary);
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<
        
        /// <summary>
        /// Drawing Method for Settings in Shepherd and other areas that write back to the original relationship
        /// </summary>
        /// <param name="serializedObject"></param>
        /// <param name="relationship"></param>
        /// <param name="settings"></param>
        public static void DrawMetricSettings(SerializedObject serializedObject, LemmingRelationship relationship, SerializedProperty settings = null)
        {
            if (relationship == null || serializedObject == null)
            {
                EditorGUILayout.HelpBox("Missing reference to relationship or serialized object.", MessageType.Error);
                return;
            }

            serializedObject.Update();

            SerializedProperty settingsProp = settings ?? serializedObject.FindProperty("_settings");
            if (settingsProp == null)
            {
                EditorGUILayout.HelpBox("_settings property not found. Is it marked [SerializeField]?", MessageType.Error);
                return;
            }

            if (settingsProp.hasVisibleChildren)
            {
                Enum metric = relationship.Metric;
                FamilyType family = relationship.Family;

                var draw = LemmingMetricSettingUtility.GetMetricSettingDrawFunction(family, metric);

                if (draw != null)
                {
                    draw(settingsProp);
                }
                else
                {
                    EditorGUILayout.HelpBox($"No settings drawer found for {family}.{metric}.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Settings property is empty.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(relationship);
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<
        
        
        /// <summary>
        /// Draws a foldout labeled "Advanced Settings" and executes the drawAction if it's expanded.
        /// </summary>
        /// <param name="drawAction">Callback for drawing advanced UI content.</param>
        public static void DrawAdvancedFoldout(System.Action drawAction)
        {
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings", true);
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                drawAction?.Invoke();
                EditorGUI.indentLevel--;
            }
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        //TODO: This is unused and might be worth deleting
        /// <summary>
        /// Clears all known property types from a serialized setting container.
        /// This is used when resetting or switching active metric options.
        /// </summary>
        /// <param name="setting">SerializedProperty representing the settings object.</param>
        public static void ClearAllSettings(SerializedProperty setting)
        {
            if (setting == null || !setting.hasVisibleChildren) return;

            foreach (SerializedProperty child in setting)
            {
                switch (child.propertyType)
                {
                    case SerializedPropertyType.Boolean:
                        child.boolValue = false;
                        break;
                    case SerializedPropertyType.Float:
                        child.floatValue = 0f;
                        break;
                    case SerializedPropertyType.Integer:
                        child.intValue = 0;
                        break;
                    case SerializedPropertyType.Enum:
                        child.enumValueIndex = 0;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        child.objectReferenceValue = null;
                        break;
                    case SerializedPropertyType.Vector3:
                        child.vector3Value = Vector3.zero;
                        break;
                    case SerializedPropertyType.String:
                        child.stringValue = string.Empty;
                        break;
                    case SerializedPropertyType.Generic:
                        if (child.isArray)
                            child.ClearArray();
                        break;
                }
            }
        }
                
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        //TODO: This is unused and might be worth deleting
        /// <summary>
        /// Draws a user-facing field for a given metric setting depending on its defined control type.
        /// </summary>
        /// <param name="setting">The setting definition and value.</param>
        public static void DrawMetricSetting(MetricSettingInfo setting)
        {
            if (setting == null) return;

            var label = new GUIContent(setting.displayName ?? "Setting", setting.description);

            switch (setting.controlType)
            {
                case SettingType.Slider:
                    setting.floatValue = EditorGUILayout.FloatField(label, setting.floatValue);
                    break;

                case SettingType.Toggle:
                    setting.boolValue = EditorGUILayout.Toggle(label, setting.boolValue);
                    break;

                case SettingType.Dropdown:
                    if (setting.options != null && setting.options.Count > 0)
                    {
                        setting.selectedIndex = EditorGUILayout.Popup(
                            label,
                            setting.selectedIndex,
                            setting.options.ToArray()
                        );
                    }
                    break;

                case SettingType.Vector3Picker:
                    setting.vector3Value = EditorGUILayout.Vector3Field(label, setting.vector3Value);
                    break;

                case SettingType.ObjectSelector:
                    setting.objectReference = EditorGUILayout.ObjectField(label, setting.objectReference, typeof(UnityEngine.Object), true);
                    break;


            }
        }
        
        // ================== SINGLE METRICS ==================

                

        /// <summary>
        /// Draws settings for the SinglePosition metric.
        /// Optionally enables axis isolation if toggled.
        /// </summary>
        public static void DrawSettingsSinglePosition(SerializedProperty setting)
        {

            DrawBoolField(setting, "useSingleAxis");
            var useSingleAxisProp = setting.FindPropertyRelative("useSingleAxis");
            if (useSingleAxisProp?.boolValue == true)
                DrawEnumField(setting, "singleAxis");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for the SingleRotation metric.
        /// Uses proxy and threshold if 'useGazeFromProxy' is enabled.
        /// </summary>
        public static void DrawSettingsSingleRotation(SerializedProperty setting)
        {
            
            DrawBoolField(setting, "useGazeFromProxy");
            var useGaze = setting.FindPropertyRelative("useGazeFromProxy");
            if (useGaze?.boolValue == true)
            {
                DrawFloatField(setting, "threshold");
                DrawObjectField(setting, "proxy");
            }
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for SingleMovement metric.
        /// Supports relative targeting and axis filtering.
        /// </summary>
        public static void DrawSettingsSingleMovement(SerializedProperty setting)
        {

            
            DrawBoolField(setting, "relativeToObject");
            var relativeToObjectProp = setting.FindPropertyRelative("relativeToObject");
            if (relativeToObjectProp?.boolValue == true)
                DrawObjectField(setting, "objectToReference");

            DrawBoolField(setting, "directionOnly");
            DrawBoolField(setting, "magnitudeOnly");
            DrawBoolField(setting, "useSingleAxis");
            var useSingleAxisProp = setting.FindPropertyRelative("useSingleAxis");
            if (useSingleAxisProp?.boolValue == true)
                DrawEnumField(setting, "singleAxis");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for SingleTrigger metric.
        /// Uses a proxy object reference.
        /// </summary>
        public static void DrawSettingsSingleTrigger(SerializedProperty setting)
        {

            
            DrawObjectField(setting, "proxy");
        }

        // ================== COUPLE METRICS ==================

        /// <summary>
        /// Draws settings for CouplePosition metric.
        /// Adds axis selection if single-axis mode is active.
        /// </summary>
        public static void DrawSettingsCouplePosition(SerializedProperty setting)
        {
            DrawBoolField(setting, "useSingleAxis");
            var useSingleAxisProp = setting.FindPropertyRelative("useSingleAxis");
            if (useSingleAxisProp?.boolValue == true)
                DrawEnumField(setting, "singleAxis");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for CoupleRotation metric.
        /// Adds support for custom or object-centered axis and inversion toggle.
        /// </summary>
        public static void DrawSettingsCoupleRotation(SerializedProperty setting)
        {
            DrawEnumField(setting, "axisSelection");
            var axis = setting.FindPropertyRelative("axisSelection");
            if (axis?.enumValueIndex == (int)AxisSelection.Custom)
                DrawVector3Field(setting, "rotationAxis");
            if (axis?.enumValueIndex == (int)AxisSelection.ObjectToCenter)
                DrawObjectField(setting, "objectToReference");

            DrawBoolField(setting, "invert");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for CoupleMovement metric.
        /// Supports relative-to-object, directional filtering, and axis selection.
        /// </summary>
        public static void DrawSettingsCoupleMovement(SerializedProperty setting)
        {
            DrawBoolField(setting, "relativeToObject");
            var relativeToObjectProp = setting.FindPropertyRelative("relativeToObject");
            if (relativeToObjectProp?.boolValue == true)
                DrawObjectField(setting, "objectToReference");

            DrawBoolField(setting, "directionOnly");
            DrawBoolField(setting, "relativeToMembers");
            DrawBoolField(setting, "magnitudeOnly");
            DrawBoolField(setting, "useSingleAxis");
            var useSingleAxisProp = setting.FindPropertyRelative("useSingleAxis");
            if (useSingleAxisProp?.boolValue == true)
                DrawEnumField(setting, "singleAxis");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for CoupleTrigger metric.
        /// Uses proxy reference.
        /// </summary>
        public static void DrawSettingsCoupleTrigger(SerializedProperty setting)
        {
            DrawObjectField(setting, "proxy");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for CoupleDistance metric.
        /// Allows distance unit configuration.
        /// </summary>
        public static void DrawSettingsCoupleDistance(SerializedProperty setting)
        {
            DrawEnumField(setting, "distanceUnit");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for CoupleDifference metric.
        /// Supports axis filtering toggle and enum selection.
        /// </summary>
        public static void DrawSettingsCoupleDifference(SerializedProperty setting)
        {
            DrawBoolField(setting, "useSingleAxis");
            var useSingleAxisProp = setting.FindPropertyRelative("useSingleAxis");
            if (useSingleAxisProp?.boolValue == true)
                DrawEnumField(setting, "singleAxis");
        }

        // ================== THROUPLE METRICS ==================

        /// <summary>
        /// Draws settings for ThrouplePosition metric.
        /// Optionally enables axis selection if toggled.
        /// </summary>
        public static void DrawSettingsThrouplePosition(SerializedProperty setting)
        {
            DrawBoolField(setting, "useSingleAxis");
            var useSingleAxisProp = setting.FindPropertyRelative("useSingleAxis");
            if (useSingleAxisProp?.boolValue == true)
                DrawEnumField(setting, "singleAxis");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for ThroupleRotation metric.
        /// Includes selection of axis mode and optional inversion.
        /// </summary>
        public static void DrawSettingsThroupleRotation(SerializedProperty setting)
        {
            DrawEnumField(setting, "axisSelectionThrouple");
            DrawBoolField(setting, "invert");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for ThroupleMovement metric.
        /// Allows for relative reference, direction filtering, and axis control.
        /// </summary>
        public static void DrawSettingsThroupleMovement(SerializedProperty setting)
        {
            DrawBoolField(setting, "relativeToObject");
            var relativeToObjectProp = setting.FindPropertyRelative("relativeToObject");
            if (relativeToObjectProp?.boolValue == true)
                DrawObjectField(setting, "objectToReference");

            DrawBoolField(setting, "directionOnly");
            DrawBoolField(setting, "relativeToMembers");
            DrawBoolField(setting, "magnitudeOnly");
            DrawBoolField(setting, "useSingleAxis");
            var useSingleAxisProp = setting.FindPropertyRelative("useSingleAxis");
            if (useSingleAxisProp?.boolValue == true)
                DrawEnumField(setting, "singleAxis");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for ThroupleTrigger metric.
        /// Uses proxy object selection.
        /// </summary>
        public static void DrawSettingsThroupleTrigger(SerializedProperty setting)
        {
            DrawObjectField(setting, "proxy");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for ThroupleDistance metric.
        /// Includes distance mode and unit selectors.
        /// </summary>
        public static void DrawSettingsThroupleDistance(SerializedProperty setting)
        {
            DrawEnumField(setting, "distanceOptions");
            DrawEnumField(setting, "distanceUnit");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for ThroupleRotationAroundAxis metric.
        /// Combines axis definition and inversion control.
        /// </summary>
        public static void DrawSettingsThroupleRotationAroundAxis(SerializedProperty setting)
        {
            DrawEnumField(setting, "axisSelection");
            var axis = setting.FindPropertyRelative("axisSelection");
            if (axis?.enumValueIndex == (int)AxisSelection.Custom)
                DrawVector3Field(setting, "rotationAxis");
            if (axis?.enumValueIndex == (int)AxisSelection.ObjectToCenter)
                DrawObjectField(setting, "objectToReference");

            DrawEnumField(setting, "axisSelectionThrouple");
            DrawBoolField(setting, "invert");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for ThroupleDensity metric.
        /// Supports distance unit and density method selection.
        /// </summary>
        public static void DrawSettingsThroupleDensity(SerializedProperty setting)
        {
            DrawEnumField(setting, "densityMethod");
            DrawEnumField(setting, "distanceUnit");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for ThroupleAngle metric.
        /// Optionally includes axis filtering.
        /// </summary>
        public static void DrawSettingsThroupleAngle(SerializedProperty setting)
        {
            DrawBoolField(setting, "useSingleAxis");
            var useSingleAxisProp = setting.FindPropertyRelative("useSingleAxis");
            if (useSingleAxisProp?.boolValue == true)
                DrawEnumField(setting, "singleAxis");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for ThroupleSize metric.
        /// Includes measurement method and axis filtering.
        /// </summary>
        public static void DrawSettingsThroupleSize(SerializedProperty setting)
        {
            DrawEnumField(setting, "sizeMethod");
            DrawBoolField(setting, "useSingleAxis");
            var useSingleAxisProp = setting.FindPropertyRelative("useSingleAxis");
            if (useSingleAxisProp?.boolValue == true)
                DrawEnumField(setting, "singleAxis");
        }
        
        // ================== GROUP METRICS ==================

        /// <summary>
        /// Draws settings for GroupPosition metric.
        /// Allows filtering to a single axis if enabled.
        /// </summary>
        public static void DrawSettingsGroupPosition(SerializedProperty setting)
        {
            DrawBoolField(setting, "useSingleAxis");
            var useSingleAxisProp = setting.FindPropertyRelative("useSingleAxis");
            if (useSingleAxisProp?.boolValue == true)
                DrawEnumField(setting, "singleAxis");
        }
                
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for GroupRotation metric.
        /// Includes a single 'invert' toggle only.
        /// </summary>
        public static void DrawSettingsGroupRotation(SerializedProperty setting)
        {
            DrawBoolField(setting, "invert");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for GroupMovement metric.
        /// Supports relative reference object, direction/magnitude toggles, and axis filtering.
        /// </summary>
        public static void DrawSettingsGroupMovement(SerializedProperty setting)
        {
            DrawBoolField(setting, "relativeToObject");
            var relativeToObjectProp = setting.FindPropertyRelative("relativeToObject");
            if (relativeToObjectProp?.boolValue == true)
                DrawObjectField(setting, "objectToReference");

            DrawBoolField(setting, "directionOnly");
            DrawBoolField(setting, "relativeToMembers");
            DrawBoolField(setting, "magnitudeOnly");
            DrawBoolField(setting, "useSingleAxis");
            var useSingleAxisProp = setting.FindPropertyRelative("useSingleAxis");
            if (useSingleAxisProp?.boolValue == true)
                DrawEnumField(setting, "singleAxis");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for GroupTrigger metric.
        /// Assigns a proxy object for interaction checks.
        /// </summary>
        public static void DrawSettingsGroupTrigger(SerializedProperty setting)
        {
            DrawObjectField(setting, "proxy");
        }
            
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for GroupRotationAroundAxis metric.
        /// Supports custom axis vector, object-centered axis, and inversion toggle.
        /// </summary>
        public static void DrawSettingsGroupRotationAroundAxis(SerializedProperty setting)
        {
            DrawEnumField(setting, "axisSelection");
            var axis = setting.FindPropertyRelative("axisSelection");
            if (axis?.enumValueIndex == (int)AxisSelection.Custom)
                DrawVector3Field(setting, "rotationAxis");
            if (axis?.enumValueIndex == (int)AxisSelection.ObjectToCenter)
                DrawObjectField(setting, "objectToReference");

            DrawBoolField(setting, "invert");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for GroupDensity metric.
        /// Allows selection of measurement method and distance unit.
        /// </summary>
        public static void DrawSettingsGroupDensity(SerializedProperty setting)
        {
            DrawEnumField(setting, "densityMethod");
            DrawEnumField(setting, "distanceUnit");
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws settings for GroupSize metric.
        /// Supports sizing method choice and optional axis filtering.
        /// </summary>
        public static void DrawSettingsGroupSize(SerializedProperty setting)
        {
            DrawEnumField(setting, "sizeMethod");
            DrawBoolField(setting, "useSingleAxis");
            var useSingleAxisProp = setting.FindPropertyRelative("useSingleAxis");
            if (useSingleAxisProp?.boolValue == true)
                DrawEnumField(setting, "singleAxis");
        }

        // ================== FIELD HELPERS ==================
        /// <summary>
        /// Safe lookup for a child property by name.
        /// Returns null if the parent is null.
        /// </summary>
        /// <param name="parent">Parent property.</param>
        /// <param name="name">Relative property name.</param>
        public static SerializedProperty FindSafe(SerializedProperty parent, string name)
        {
            return parent != null ? parent.FindPropertyRelative(name) : null;
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws a toggle field with label and description.
        /// </summary>
        public static void DrawBoolField(SerializedProperty parent, string name)
        {
            
            var prop = FindSafe(parent, name);
            if (prop == null) 
            {
                Debug.LogWarning($"[DRAW] Couldn't find property: {name}");
                return;
            }
            var label = new GUIContent(ObjectNames.NicifyVariableName(name), LemmingSettingDescriptions.GetDescription(name));
            EditorGUILayout.PropertyField(prop, label);
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws an enum popup with hover text from description registry.
        /// </summary>
        public static void DrawEnumField(SerializedProperty parent, string name)
        {
            
            var prop = FindSafe(parent, name);
            if (prop == null) 
            {
                Debug.LogWarning($"[DRAW] Couldn't find property: {name}");
                return;
            }
            var label = new GUIContent(ObjectNames.NicifyVariableName(name), LemmingSettingDescriptions.GetDescription(name));
            EditorGUILayout.PropertyField(prop, label);
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws a float input field with label and tooltip.
        /// </summary>
        public static void DrawFloatField(SerializedProperty parent, string name)
        {
            
            var prop = FindSafe(parent, name);
            if (prop == null) 
            {
                Debug.LogWarning($"[DRAW] Couldn't find property: {name}");
                return;
            }
            var label = new GUIContent(ObjectNames.NicifyVariableName(name), LemmingSettingDescriptions.GetDescription(name));
            EditorGUILayout.PropertyField(prop, label);
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws a Vector3 input field with label and help text.
        /// </summary>
        public static void DrawVector3Field(SerializedProperty parent, string name)
        {
            
            var prop = FindSafe(parent, name);
            if (prop == null) 
            {
                Debug.LogWarning($"[DRAW] Couldn't find property: {name}");
                return;
            }
            var label = new GUIContent(ObjectNames.NicifyVariableName(name), LemmingSettingDescriptions.GetDescription(name));
            EditorGUILayout.PropertyField(prop, label);
        }
        
        
        //------------------------ Method Break ----------------------------  << << << << << <<


        /// <summary>
        /// Draws a Unity object reference selector.
        /// </summary>
        public static void DrawObjectField(SerializedProperty parent, string name)
        {
            
            var prop = FindSafe(parent, name);
            if (prop == null) 
            {
                Debug.LogWarning($"[DRAW] Couldn't find property: {name}");
                return;
            }
            var label = new GUIContent(ObjectNames.NicifyVariableName(name), LemmingSettingDescriptions.GetDescription(name));
            EditorGUILayout.PropertyField(prop, label);
        }
    }

    #endregion

    //------------------------------------------------------
    // XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX
    //------------------------------------------------------
    
    #region LemmingMetricSettingUtility
    
    public static class LemmingMetricSettingUtility
    {
        /// <summary>
        /// Returns the list of field names (keys) from LemmingRelationSetting that are relevant for the given metric.
        /// </summary>
        public static List<string> GetRelevantKeys(Enum metric)
        {
            return metric switch
            {
                // ---------------- SINGLE ----------------
                SingleMetric.Position => new() { "useSingleAxis", "singleAxis" },
                SingleMetric.Rotation => new() { "useGazeFromProxy", "threshold", "proxy" },
                SingleMetric.Movement => new()
                {
                    "relativeToObject", "objectToReference", "directionOnly", "magnitudeOnly", "useSingleAxis",
                    "singleAxis"
                },
                SingleMetric.Trigger => new() { "proxy" },

                // ---------------- COUPLE ----------------
                CoupleMetric.Position => new() { "useSingleAxis", "singleAxis" },
                CoupleMetric.Rotation => new() { "axisSelection", "rotationAxis", "objectToReference", "invert" },
                CoupleMetric.Movement => new()
                {
                    "relativeToObject", "objectToReference", "directionOnly", "relativeToMembers", "magnitudeOnly",
                    "useSingleAxis", "singleAxis"
                },
                CoupleMetric.Trigger => new() { "proxy" },
                CoupleMetric.Distance => new() { "distanceUnit" },
                CoupleMetric.Difference => new() { "useSingleAxis", "singleAxis" },

                // ---------------- THROUPLE ----------------
                ThroupleMetric.Position => new() { "useSingleAxis", "singleAxis" },
                ThroupleMetric.Rotation => new() { "axisSelectionThrouple", "invert" },
                ThroupleMetric.Movement => new()
                {
                    "relativeToObject", "objectToReference", "directionOnly", "relativeToMembers", "magnitudeOnly",
                    "useSingleAxis", "singleAxis"
                },
                ThroupleMetric.Trigger => new() { "proxy" },
                ThroupleMetric.Distance => new() { "distanceOptions", "distanceUnit" },
                ThroupleMetric.RotationAroundAxis => new()
                    { "axisSelection", "rotationAxis", "objectToReference", "axisSelectionThrouple", "invert" },
                ThroupleMetric.Density => new() { "densityMethod", "distanceUnit" },
                ThroupleMetric.Angle => new() { "useSingleAxis", "singleAxis" },
                ThroupleMetric.Size => new() { "sizeMethod", "useSingleAxis", "singleAxis" },

                // ---------------- GROUP ----------------
                GroupMetric.Position => new() { "useSingleAxis", "singleAxis" },
                GroupMetric.Rotation => new() { "invert" },
                GroupMetric.Movement => new()
                {
                    "relativeToObject", "objectToReference", "directionOnly", "relativeToMembers", "magnitudeOnly",
                    "useSingleAxis", "singleAxis"
                },
                GroupMetric.Trigger => new() { "proxy" },
                GroupMetric.RotationAroundAxis => new()
                    { "axisSelection", "rotationAxis", "objectToReference", "invert" },
                GroupMetric.Density => new() { "densityMethod", "distanceUnit" },
                GroupMetric.Size => new() { "sizeMethod", "useSingleAxis", "singleAxis" },

                _ => new List<string>()
            };
        }
        


        /// <summary>
        /// Help function to draw the Metric Settings 
        /// </summary>
        /// <param name="family">The FamilyType</param>
        /// <param name="metric">The Metric Enum</param>
        /// <returns> The Lemming Metric Settings Drawer with the property settings</returns>
        public static Action<SerializedProperty> GetMetricSettingDrawFunction(FamilyType family, Enum metric)
        {
            if (metric == null) return null;

            string methodName = $"DrawSettings{family}{metric}";
            Debug.Log($"{methodName}");
            var method = typeof(LemmingMetricSettingsDrawer).GetMethod(methodName);
            if (method == null) return null;
            Debug.Log(method.ToString());
            return (SerializedProperty settingProp) =>
            {
                LemmingMetricSettingsDrawer.DrawAdvancedFoldout(() =>
                {
                    method.Invoke(null, new object[] { settingProp });
                });
            };
        }
    }
    

    #endregion
    
    
    
    
}