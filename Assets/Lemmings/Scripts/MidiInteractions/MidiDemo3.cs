using UnityEngine;
using UnityEngine.Serialization;

public class MidiDemo3 : MonoBehaviour
{
    public MidiSettingSender sender;
    private MidiSetting _setting;

    [Header("Single Note")]
    [Range(0f,1f)] public float note;
    [Range(0f,1f)] public float duration;
    [Range(0f,1f)] public float velocity;
    public bool playDuration;
    public bool playOn;
    public bool playOff;
    
    
    
    [Header("Beat 1")]
    [Range(0f,1f)] public float beatTiming1;        // Period A (seconds)
    [Range(0f,1f)] public float beatNoteDuration = .1f;    
    [Range(0f,1f)] public float beatNote;
    [Range(0f,1f)] public float beatNoteVelocity = .75f;
    public bool playBeat1;
    private float _beatTimer;
    private bool _wasPlayTremolo;

    
    
    
    [Range(0f,1f)] public float beatTiming2;  // Float that determines ratio
    [Range(0f,1f)] public float beatNoteDuration2 = .1f;       
    [Range(0f,1f)] public float beatNote2;
    [Range(0f,1f)] public float beatNoteVelocity2 = .75f;
    public bool playBeat2;
    private float _beatTimer2;
    private bool _t2Ready;

    
    [Header("Beat 3 (follows 1 by ratio)")]
    [Range(0f,1f)] public float beatTiming3;  // Float that determines ratio
    [Range(0f,1f)] public float beatNoteDuration3 = .1f;       
    [Range(0f,1f)] public float beatNote3;
    [Range(0f,1f)] public float beatNoteVelocity3 = .75f;
    public bool playBeat3;
    private float _beatTimer3;
    private bool _t3Ready;
    
    
    [Header("Beat 4 (follows 1 by ratio)")]
    [Range(0f,1f)] public float beatTiming4;  // Float that determines ratio
    [Range(0f,1f)] public float beatNoteDuration4 = .1f;       
    [Range(0f,1f)] public float beatNote4;
    [Range(0f,1f)] public float beatNoteVelocity4 = .75f;
    public bool playBeat4;
    private float _beatTimer4;
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
    
    float TremoloPeriod1(float d01) =>
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
        // one-shots
        if (playDuration) sender.PlayFixedNote01(note, velocity, duration); playDuration = false;
        if (playOn) sender.NoteOn01(note, velocity); playOn = false;
        if (playOff) sender.NoteOff01(note); playOff = false;
        // setting updates
        if (setPitchBend) sender.SendPitchBend01(pitchBend);
        if (setModWheel)  sender.SendModWheel01(modWheel); // fixed bug: was gated by setPitchBend
        if (setPortamento) sender.SendPortamento(portamento);
        if (setVar1) sender.SendVariable101(var1);
        if (setVar2) sender.SendVariable201(var2);



        float periodA = Mathf.Max(1e-4f, TremoloPeriod1(beatTiming1));
        float periodB = Mathf.Max(1e-4f, periodA * SelectRatio(beatTiming2));
        float periodC = Mathf.Max(1e-4f, periodB * SelectRatio(beatTiming3));
        float periodD = Mathf.Max(1e-4f, periodC * SelectRatio(beatTiming4));

        // reset/prime on toggle
        
        if (!playBeat1)
        {
            _beatTimer = 0f;
            _t2Ready = _t3Ready = _t4Ready = false;
        }
        else if (!_wasPlayTremolo)
        {
            _beatTimer = 0f;
            _t2Ready = _t3Ready = _t4Ready = true;
        }
        

        //_beatTimer += Time.deltaTime;
        
        // --- Trem 1 ---
        if (playBeat1)
        {
            //if (_beatTimer <= 0f)
                

            if (_beatTimer >= periodA)
            {
                sender.PlayFixedNote01(beatNote, beatNoteVelocity, beatNoteDuration);
                _beatTimer = 0f;
                _t2Ready = _t3Ready = _t4Ready = true;
            }

            _beatTimer += Time.deltaTime;
        }

        // --- Trem 2 (derived) ---
        if (playBeat2 && _t2Ready && _beatTimer >= periodB)
        {
            _t2Ready = false;
            sender.PlayFixedNote01(beatNote2, beatNoteVelocity2, beatNoteDuration2);
        }

        // --- Trem 3 (derived) ---
        if (playBeat3 && _t3Ready && _beatTimer >= periodC)
        {
            _t3Ready = false;
            sender.PlayFixedNote01(beatNote3, beatNoteVelocity3, beatNoteDuration3);
        }

        // --- Trem 4 (derived) ---
        if (playBeat4 && _t4Ready && _beatTimer >= periodD)
        {
            _t4Ready = false;
            sender.PlayFixedNote01(beatNote4, beatNoteVelocity4, beatNoteDuration4);
        }

        _wasPlayTremolo = playBeat1;
        
        
    }
    
    
    
}