using UnityEngine;
using UnityEngine.Serialization;

public class MidiDemo4 : MonoBehaviour
{
    public MidiSettingSender sender;
    private MidiSetting _setting;

    [Header("Master Timer")]

    [Range(0f,1f)] public float Period;
    [Range(0f, 1f)] public float NoteSustain = .1f;
    [Range(0f, 1f)] public float NoteVelocity = .75f;
    
    private float _beatTimer;
    
    
    [Header("Beat 1")]
    [Range(0f,1f)] public float beatTiming1;        // Period A (seconds)
    [Range(0f,1f)] public float beatNote1;
    public bool playBeat1;
    private bool _t1Ready;


    

    [Header("Beat 2")]
    [Range(0f,1f)] public float beatTiming2;  // Float that determines ratio
    [Range(0f,1f)] public float beatNote2;
    public bool playBeat2;
    private bool _t2Ready;


    [Header("Beat 3")]
    [Range(0f,1f)] public float beatTiming3;  // Float that determines ratio
    [Range(0f,1f)] public float beatNote3;
    public bool playBeat3;
    private bool _t3Ready;
    

    [Header("Beat 4")]
    [Range(0f,1f)] public float beatTiming4;  // Float that determines ratio
    [Range(0f,1f)] public float beatNote4;
    public bool playBeat4;
    private bool _t4Ready;
    
    [Header("Pitch Bend")]
    [Range(0f,1f)] public float pitchBend;
    public bool setPitchBend;

    [Header("Mod Wheel")]
    [Range(0f,1f)] public float modWheel;
    public bool setModWheel; // <-- make sure to use this in Update()

    [Header("Portamento")]
    [Range(0f,1f)] public float portamento;
    public bool setPortamento;

    [Header("Variable 1")]
    [Range(0f,1f)] public float var1;
    public bool setVar1;

    [Header("Variable 2")]
    [Range(0f,1f)] public float var2;
    public bool setVar2;

    void Awake()
    {
        if (!sender) { Debug.LogError("MidiDemo2: sender is not assigned."); enabled = false; return; }
        _setting = sender.setting;
        if (!_setting) { Debug.LogError("MidiDemo2: sender.setting is not assigned."); enabled = false; return; }
    }

    // --- helpers ---
    
    float BarPeriod1(float d01) =>
        Mathf.Lerp(_setting.tremoloMin, _setting.tremoloMax, Mathf.Clamp01(d01));

    float SelectRatio(float timing)
    {
        var arr = _setting?.tremoloRatios;
        if (arr == null || arr.Length == 0) return 2f/3f; // fallback
        int idx = Mathf.RoundToInt(Mathf.Clamp01(timing) * (arr.Length - 1));
        float r = Mathf.Clamp01(arr[idx]);
        return Mathf.Max(1e-4f, r);
    }

    void Update()
    {   
        // setting updates
        if (setPitchBend) sender.SendPitchBend01(pitchBend);
        if (setModWheel)  sender.SendModWheel01(modWheel); // fixed bug: was gated by setPitchBend
        if (setPortamento) sender.SendPortamento(portamento);
        if (setVar1) sender.SendVariable101(var1);
        if (setVar2) sender.SendVariable201(var2);


        float periodBase = BarPeriod1(Period);
        float periodA = Mathf.Max(1e-4f, periodBase * SelectRatio(beatTiming1));
        float periodB = Mathf.Max(1e-4f, periodBase * SelectRatio(beatTiming2));
        float periodC = Mathf.Max(1e-4f, periodBase * SelectRatio(beatTiming3));
        float periodD = Mathf.Max(1e-4f, periodBase * SelectRatio(beatTiming4));
        
        // --- Timer ---
        if (_beatTimer >= periodBase)
        {
            _beatTimer = 0f;
            _t1Ready = _t2Ready = _t3Ready = _t4Ready = true;
        }

        _beatTimer += Time.deltaTime;
        
        // --- Beat 1 (derived) ---
        if (playBeat1 && _t1Ready && _beatTimer >= periodA)
        {
            _t1Ready = false;
            sender.PlayFixedNote01(beatNote1, NoteVelocity, NoteSustain);
        }
        
        // --- Beat 2 (derived) ---
        if (playBeat2 && _t2Ready && _beatTimer >= periodB)
        {
            _t2Ready = false;
            sender.PlayFixedNote01(beatNote2, NoteVelocity, NoteSustain);
        }

        // --- Beat 3 (derived) ---
        if (playBeat3 && _t3Ready && _beatTimer >= periodC)
        {
            _t3Ready = false;
            sender.PlayFixedNote01(beatNote3, NoteVelocity, NoteSustain);
        }

        // --- Beat 4 (derived) ---
        if (playBeat4 && _t4Ready && _beatTimer >= periodD)
        {
            _t4Ready = false;
            sender.PlayFixedNote01(beatNote4, NoteVelocity, NoteSustain);
        }

        
    }
    
    
    
}