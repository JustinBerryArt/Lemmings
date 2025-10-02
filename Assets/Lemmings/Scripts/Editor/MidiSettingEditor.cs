// Place this file at: Assets/Editor/MidiSettingEditor.cs
// It adds dropdowns for Variable 1 / Variable 2 that are driven by the
// selected ReasonCc.Instrument at the top of your MidiSetting asset.
//
// Works with your existing ReasonCc static class (from reason_cc_via_rt_midi_c_unity_sample.cs)
// without modifying it. Uses reflection to read its private `Tables` dictionary
// and exposes the parameter names as a popup. Robust to assembly separation
// (Editor vs Runtime) and older C# versions (no ValueTuple dependency).

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MidiSetting))]
public class MidiSettingEditor : Editor
{
    // --- Serialized fields we care about ---
    SerializedProperty _instrument;
    SerializedProperty _var1Cc;
    SerializedProperty _var1Min;
    SerializedProperty _var1Max;
    SerializedProperty _var2Cc;
    SerializedProperty _var2Min;
    SerializedProperty _var2Max;

    // Cache lookups while the inspector is open
    static readonly Dictionary<object, List<KeyValuePair<string,int>>> _cache = new();

    void OnEnable()
    {
        _instrument = serializedObject.FindProperty("instrument");
        _var1Cc     = serializedObject.FindProperty("variable1ccChannel");
        _var1Min    = serializedObject.FindProperty("variable1Min");
        _var1Max    = serializedObject.FindProperty("variable1Max");
        _var2Cc     = serializedObject.FindProperty("variable2ccChannel");
        _var2Min    = serializedObject.FindProperty("variable2Min");
        _var2Max    = serializedObject.FindProperty("variable2Max");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw everything Unity would normally draw except our two Variable blocks.
        DrawPropertiesExcluding(serializedObject,
            nameof(MidiSetting.variable1Min),
            nameof(MidiSetting.variable1Max),
            nameof(MidiSetting.variable1ccChannel),
            nameof(MidiSetting.variable2Min),
            nameof(MidiSetting.variable2Max),
            nameof(MidiSetting.variable2ccChannel)
        );

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Variable Controls", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            DrawVariableBlock("Variable 1", _var1Min, _var1Max, _var1Cc);
            EditorGUILayout.Space(6);
            DrawVariableBlock("Variable 2", _var2Min, _var2Max, _var2Cc);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawVariableBlock(string label, SerializedProperty minProp, SerializedProperty maxProp, SerializedProperty ccProp)
    {
        // Resolve instrument directly from the target ScriptableObject
        object instBoxed = ((MidiSetting)target).instrument; // ReasonCc.Instrument enum value (boxed)

        // Resolve parameter list for this instrument
        var options = GetParamsForInstrument(instBoxed);

        using (new EditorGUILayout.VerticalScope("helpbox"))
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                // Min/Max range as usual
                EditorGUILayout.PropertyField(minProp, new GUIContent("Min (0..127)"));
                EditorGUILayout.PropertyField(maxProp, new GUIContent("Max (0..127)"));

                // Dropdown + manual field
                int currentCc = Mathf.Clamp(ccProp.intValue, 0, 127);

                // Find current index by cc match
                int currentIndex = -1;
                for (int i = 0; i < options.Count; i++)
                    if (options[i].Value == currentCc) { currentIndex = i; break; }

                // Build GUI list: named params + a manual fallback
                var names = options.Select(p => $"{p.Key}  (CC {p.Value})").ToList();
                names.Add("— Manual CC …");

                int chosen = EditorGUILayout.Popup(new GUIContent("Mapped Parameter"),
                                                   currentIndex >= 0 ? currentIndex : names.Count - 1,
                                                   names.ToArray());

                if (chosen >= 0 && chosen < options.Count)
                {
                    // User picked a named parameter — write its CC back
                    ccProp.intValue = options[chosen].Value;
                }

                // Manual CC entry (enabled when the last item is shown or chosen)
                using (new EditorGUI.DisabledScope(chosen >= 0 && chosen < options.Count))
                {
                    int manual = EditorGUILayout.IntSlider(new GUIContent("CC Number (manual)"), currentCc, 0, 127);
                    if (manual != currentCc) ccProp.intValue = manual;
                }

                // Read-only echo so it’s obvious what will be sent
                EditorGUILayout.LabelField("Resulting CC:", ccProp.intValue.ToString());
            }
        }
    }

    // ===== Reflection helpers =====
    static List<KeyValuePair<string,int>> GetParamsForInstrument(object instrument)
    {
        if (instrument == null) return Empty();
        if (_cache.TryGetValue(instrument, out var cached)) return cached;

        var rcType = FindReasonCcType();
        if (rcType == null) return Empty();

        // private static readonly Dictionary<Instrument, Dictionary<string,int>> Tables
        var tablesField = rcType.GetField("Tables", BindingFlags.NonPublic | BindingFlags.Static);
        if (tablesField == null) return Empty();

        var tables = tablesField.GetValue(null);
        if (tables == null) return Empty();

        // TryGetValue(instrument, out map)
        var dictType = tables.GetType();
        var tryGetValue = dictType.GetMethod("TryGetValue");
        if (tryGetValue == null) return Empty();

        object[] args = new object[] { instrument, null };
        bool ok = (bool)tryGetValue.Invoke(tables, args);
        if (!ok || args[1] == null) return Empty();

        var map = args[1]; // Dictionary<string,int>

        var list = new List<KeyValuePair<string,int>>();
        foreach (var entry in (IEnumerable)map)
        {
            var t = entry.GetType();
            string key = (string)t.GetProperty("Key")!.GetValue(entry, null);
            int val = Convert.ToInt32(t.GetProperty("Value")!.GetValue(entry, null));
            list.Add(new KeyValuePair<string,int>(key, val));
        }

        list = list.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase).ToList();
        _cache[instrument] = list;
        return list;
    }

    static Type FindReasonCcType()
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
            catch { continue; }
            foreach (var t in types)
            {
                if (t == null) continue;
                if (t.Name == "ReasonCc") return t; // class is in global namespace
            }
        }
        return null;
    }

    static List<KeyValuePair<string,int>> Empty() => new List<KeyValuePair<string,int>>();
}
