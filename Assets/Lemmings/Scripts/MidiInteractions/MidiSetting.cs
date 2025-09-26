using UnityEngine;

[CreateAssetMenu(menuName = "Lemmings/MidiInstrument/New Midi Preset")]
public class MidiSetting : ScriptableObject
{
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
    public int octaveMin = 3;
    public int octaveMax = 6;
    
    [Header("Velocity Range (0-127)")]
    public int velocityMin = 0;
    public int velocityMax = 127;
    
    [Header("Tremelo Timing Range (seconds)")]
    public float tremeloMin = .05f;
    public float tremeloMax = 2f;
    
    [Header("Fixed Note Duration Range (seconds)")]
    public float fixedNoteMin;
    public float fixedNoteMax;
    
    
    
    
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
