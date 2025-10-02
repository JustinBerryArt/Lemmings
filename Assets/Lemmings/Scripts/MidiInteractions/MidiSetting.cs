using System.Collections.Generic;
using ReasonMidi;
using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Lemmings/MidiInstrument/New Midi Preset")]
public class MidiSetting : ScriptableObject
{
    public ReasonCc.Instrument instrument;
    
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
    
    [Header("Velocity Range (0-127)")]
    [Range(0, 127)]
    public int velocityMin = 0;
    [Range(0, 127)]
    public int velocityMax = 127;
    
    [Header("tremolo Timing Range (seconds)")]
    [Range(0f, 2f)]
    public float tremoloMin = .05f;
    [Range(0f, 4f)]
    public float tremoloMax = 2f;

    [Header("Tremolo Timing Ratios")]
    [Space(6)]
    [Header("Timing Caps")]
    [InspectorName("Include Zero")] public bool r_0 = true;
    [InspectorName("Include One")] public bool r_1 = true;
    
    [Space(6)]
    [Header("Common Timings")]
    [InspectorName("1:2 (0.500)")] public bool r_1_2 = true;
    [InspectorName("1:3 (0.333)")] public bool r_1_3 = false;
    [InspectorName("1:4 (0.250)")] public bool r_1_4 = false;
    [InspectorName("2:3 (0.666)")] public bool r_2_3 = true;
    [InspectorName("3:4 (0.750)")] public bool r_3_4 = true;
    [InspectorName("4:5 (0.800)")] public bool r_4_5 = false;
    [InspectorName("5:6 (0.833)")] public bool r_5_6 = false;

    [Space(6)]
    [Header("Timing By Eighths")]
    [InspectorName("1:8 (0.125)")] public bool r_1_8 = false;
    [InspectorName("2:8 (0.250)")] public bool r_2_8 = false;
    [InspectorName("3:8 (0.375)")] public bool r_3_8 = true;
    [InspectorName("4:8 (0.500)")] public bool r_4_8 = true;
    [InspectorName("5:8 (0.625)")] public bool r_5_8 = false;
    [InspectorName("6:8 (0.750)")] public bool r_6_8 = false;
    [InspectorName("7:8 (0.875)")] public bool r_7_8 = false;

    
    [Space(6)]
    [Header("Polyrhthmic Timings")]
    [InspectorName("2:5 (0.400)")] public bool r_2_5 = false;
    [InspectorName("3:5 (0.600)")] public bool r_3_5 = false;
    [InspectorName("4:7 (0.571)")] public bool r_4_7 = false;
    [InspectorName("5:7 (0.714)")] public bool r_5_7 = false;

    [Space(6)] 
    [Header("Near Unison / Slow Phase Timings")]
    [InspectorName("8:9 (0.888)")]   public bool r_8_9 = false;
    [InspectorName("9:10 (0.900)")]  public bool r_9_10 = false;
    [InspectorName("10:11 (0.909)")] public bool r_10_11 = false;
    [InspectorName("11:12 (0.916)")] public bool r_11_12 = false;

    [Space(6)]
    [Header("Golden Ratio Timing")]
    [InspectorName("1:φ (≈0.618)")] public bool r_golden = false; // 1 / 1.618...

    [Tooltip("Built from the toggles above. Values are decimals in (0,1].")]
    public float[] tremoloRatios = new float[] { 2f / 3f, 3f / 4f, 1f / 2f }; 
    
    
    
    [Header("Fixed Note Duration Range (seconds)")]
    [Range(0f, 2f)]
    public float fixedNoteMin;
    [Range(0f, 2f)]
    public float fixedNoteMax;
    
    [Header("Mod Wheel Settings")]
    [Range(0, 127)]
    public int modWheelMin;
    [Range(0, 127)]
    public int modWheelMax;
    [ReadOnly] public int modWheelccChannel = 1;
    
    [Header("Pitch Bend Settings (64 is no change)")]
    [Range(0, 127)]
    public int pitchBendMin = 0;
    [Range(0, 127)]
    public int pitchBendMax = 127;
    [ReadOnly] public int pitchBendccChannel = 39;
    
    [Header("Variable 1 (channel-defined) Settings")]
    public string variable1Name;
    [Range(0, 127)]
    public int variable1Min = 0;
    [Range(0, 127)]
    public int variable1Max = 127;
    [Range(0, 127)]
    public int variable1ccChannel;
    
    [Header("Variable 2 (channel-defined) Settings")]
    public string variable2Name;
    [Range(0, 127)]
    public int variable2Min = 0;
    [Range(0, 127)]
    public int variable2Max = 127;
    [Range(0, 127)]
    public int variable2ccChannel;

    [Header("Portamento Settings")] 
    public bool portamentoOn;
    [Range(0, 127)]
    public int portamentoLevelMin = 0;
    [Range(0, 127)]
    public int portamentoLevelMax = 127;
    [ReadOnly] public int portamentoccChannel = 5;

    void OnValidate()
    {
        RebuildTremoloRatios();
    }

    [ContextMenu("Rebuild tremolo Ratios Now")]
    public void RebuildTremoloRatios()
    {
        var list = new List<float>(18);

        void Add(bool enabled, float value)
        {
            if (!enabled) return;
            // clamp into [0,1], skip zeros
            float v = Mathf.Clamp01(value);
            list.Add(v);
        }

        // endcaps
        Add(r_0, 0f);
        Add(r_1, 1f);
        
        // bread-and-butter
        Add(r_1_2,   1f / 2f);
        Add(r_1_3,   1f / 3f);
        Add(r_1_4,   1f / 4f);
        Add(r_2_3,   2f / 3f);
        Add(r_3_4,   3f / 4f);
        Add(r_4_5,   4f / 5f);
        Add(r_5_6,   5f / 6f);

        // bread-and-butter
        Add(r_1_8,   1f / 8f);
        Add(r_2_8,   2f / 8f);
        Add(r_3_8,   3f / 8f);
        Add(r_4_8,   4f / 8f);
        Add(r_5_8,   5f / 8f);
        Add(r_6_8,   6f / 8f);
        Add(r_7_8,   7f / 8f);
        
        // quintuple/septuple spice
        Add(r_2_5,   2f / 5f);
        Add(r_3_5,   3f / 5f);
        Add(r_4_7,   4f / 7f);
        Add(r_5_7,   5f / 7f);

        // near-unison
        Add(r_8_9,   8f / 9f);
        Add(r_9_10,  9f / 10f);
        Add(r_10_11, 10f / 11f);
        Add(r_11_12, 11f / 12f);

        // golden-ish
        if (r_golden) Add(true, 1f / 1.61803398875f);

        // Fallback if nothing is selected
        if (list.Count == 0)
            list.Add(2f / 3f); // 2:3 default

        // Sort ascending and de-dup (within tiny epsilon)
        list.Sort();
        for (int i = list.Count - 1; i > 0; i--)
            if (Mathf.Abs(list[i] - list[i - 1]) < 1e-6f)
                list.RemoveAt(i);

        tremoloRatios = list.ToArray();
    }
    
    
}
