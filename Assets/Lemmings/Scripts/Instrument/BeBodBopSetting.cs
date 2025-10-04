using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// This is the array of settings used to control a BeBodBopper.
/// Select the following:
/// - Bar Duration
/// - Notes included in scale
/// - Octave Range
/// - Standard Velocity
/// - Standard Sustain
/// - Timing Intervals for Notes
/// 
/// See below for resources required:
/// LoopMidi -> https://www.tobias-erichsen.de/software/loopmidi.html
/// RTMidi -> https://github.com/keijiro/jp.keijiro.rtmidi
/// </summary>
[CreateAssetMenu(menuName = "Lemmings/MidiInstrument/BeBodBop Preset")]
public class BeBodBopSetting : ScriptableObject
{
    [Header("Bar Duration (seconds)")]
    [Range(0f, 4f)]
    public float duration = 1f;
        
    [Header("Notes In Scale")]
    public bool Ab;
    public bool A;
    public bool Bb;
    public bool B;
    public bool C;
    public bool Db;
    public bool D;
    public bool Eb;
    public bool E;
    public bool F;
    public bool Gb;
    public bool G;
    
    [Header("Octave Range (0-8)")]
    [Range(0, 8)]
    public int octaveMin = 3;
    [Range(0, 8)]
    public int octaveMax = 6;

    [Header("Velocity (0-127)")] [Range(0, 127)]
    public int velocity = 127;

    [Header("Note Sustain")] [Range(0, 2f)]
    public float sustain = 127;

    [Header("Note Timing Intervals")]

    [Space(6)]
    [Header("Timing By Fifths")]
    [InspectorName("Use All Fifths")]  public bool fifths  = false;
    [InspectorName("1:5 (0.2)")]       public bool r_1_5   = false;
    [InspectorName("2:5 (0.4)")]       public bool r_2_5   = false;
    [InspectorName("3:5 (0.6)")]       public bool r_3_5   = false;
    [InspectorName("4:5 (0.8)")]       public bool r_4_5   = false;
    [InspectorName("5:5 (1.0)")]       public bool r_5_5   = false;

    
    [Space(6)]
    [Header("Timing By Sixths")]
    [InspectorName("Use All Sixths")]  public bool sixths = false;
    [InspectorName("1:6 (0.167)")]     public bool r_1_6 = false;
    [InspectorName("2:6 (0.333)")]     public bool r_2_6 = false;
    [InspectorName("3:6 (0.5)")]       public bool r_3_6 = false;
    [InspectorName("4:6 (0.667)")]     public bool r_4_6 = false;
    [InspectorName("5:6 (0.833)")]     public bool r_5_6 = false;
    [InspectorName("6:6 (1.0)")]       public bool r_6_6 = false;

    
    [Space(6)]
    [Header("Timing By Eighths")]
    [InspectorName("Use All Eighths")] public bool eighths = true;
    [InspectorName("1:8 (0.125)")]     public bool r_1_8 = false;
    [InspectorName("2:8 (0.250)")]     public bool r_2_8 = false;
    [InspectorName("3:8 (0.375)")]     public bool r_3_8 = false;
    [InspectorName("4:8 (0.500)")]     public bool r_4_8 = false;
    [InspectorName("5:8 (0.625)")]     public bool r_5_8 = false;
    [InspectorName("6:8 (0.750)")]     public bool r_6_8 = false;
    [InspectorName("7:8 (0.875)")]     public bool r_7_8 = false;
    [InspectorName("8:8 (1.0)")]       public bool r_8_8 = false;
    
    

    [Tooltip("Built from the toggles above. Values are decimals in (0,1].")]
    public float[] noteRatios = new float[] { 1f / 4f, 2f / 4f, 3f / 4f, 4f / 4f }; 
    

    void OnValidate()
    {
        DefineRatios();
    }

    [ContextMenu("Rebuild tremolo Ratios Now")]
    public void DefineRatios()
    {
        var list = new List<float>(19);

        if (fifths) r_1_5 = r_2_5 = r_3_5 = r_4_5 = r_5_5 = true;
        if (sixths) r_1_6 = r_2_6 = r_3_6 = r_4_6 = r_5_6 = r_6_6 = true;
        if (eighths) r_1_8 = r_2_8 = r_3_8 = r_4_8 = r_5_8 = r_6_8 = r_7_8 = r_8_8 = true;
        
        // Add function for adding ratios to list
        void Add(bool enabled, float value)
        {
            if (!enabled) return;
            // clamp into [0,1], skip zeros
            float v = Mathf.Clamp01(value);
            list.Add(v);
        }
        
        // fifths
        Add(r_1_5,   1f / 5f);
        Add(r_2_5,   2f / 5f);
        Add(r_3_5,   3f / 5f);
        Add(r_4_5,   4f / 5f);
        Add(r_5_5,   5f / 5f);

        // sixths
        Add(r_1_6,   1f / 6f);
        Add(r_2_6,   2f / 6f);
        Add(r_3_6,   3f / 6f);
        Add(r_4_6,   4f / 6f);
        Add(r_5_6,   5f / 6f);
        Add(r_6_6,   6f / 6f);
        
        // eighths
        Add(r_1_8,   1f / 8f);
        Add(r_2_8,   2f / 8f);
        Add(r_3_8,   3f / 8f);
        Add(r_4_8,   4f / 8f);
        Add(r_5_8,   5f / 8f);
        Add(r_6_8,   6f / 8f);
        Add(r_7_8,   7f / 8f);
        Add(r_8_8,   8f / 8f);
        

        // Fallback if nothing is selected
        if (list.Count == 0)
        {
            list.Add(1f / 4f); 
            list.Add(2f / 4f);
            list.Add(3f / 4f);
            list.Add(4f / 4f);
        }


        // Sort ascending and de-dup (within tiny epsilon)
        list.Sort();
        for (int i = list.Count - 1; i > 0; i--)
            if (Mathf.Abs(list[i] - list[i - 1]) < 1e-6f)
                list.RemoveAt(i);

        noteRatios = list.ToArray();
    }
    
    
}
