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
    
    [Header("Tremolo 1")]
    [Range(0f,1f)] public float tremoloDuration;        // Period A (seconds)
    [Range(0f,1f)] private float tremoloNoteDuration = .1f;    
    [Range(0f,1f)] public float tremoloNote;
    [Range(0f,1f)] private float tremoloNoteVelocity = .75f;
    public bool playTremolo;
    private float _tremoloTimer;
    private bool _wasPlayTremolo;

    
    [Header("Tremolo 2 (follows 1 by ratio)")]
    [Range(0f,1f)] public float tremoloDuration2;  // Float that determines ratio
    [Range(0f,1f)] private float tremoloNoteDuration2 = .1f;       
    [Range(0f,1f)] public float tremoloNote2;
    [Range(0f,1f)] private float tremoloNoteVelocity2 = .75f;
    public bool playTremolo2;
    private float _tremoloTimer2;
    private bool _t2Ready;

    [Header("Tremolo 3 (follows 1 by ratio)")]
    [Range(0f,1f)] public float tremoloDuration3;  // Float that determines ratio
    [Range(0f,1f)] private float tremoloNoteDuration3 = .1f;       
    [Range(0f,1f)] public float tremoloNote3;
    [Range(0f,1f)] private float tremoloNoteVelocity3 = .75f;
    public bool playTremolo3;
    private float _tremoloTimer3;
    private bool _t3Ready;
    
    [Header("Tremolo 4 (follows 1 by ratio)")]
    [Range(0f,1f)] public float tremoloDuration4;  // Float that determines ratio
    [Range(0f,1f)] private float tremoloNoteDuration4 = .1f;       
    [Range(0f,1f)] public float tremoloNote4;
    [Range(0f,1f)] private float tremoloNoteVelocity4 = .75f;
    public bool playTremolo4;
    private float _tremoloTimer4;
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



        float periodA = Mathf.Max(1e-4f, TremoloPeriod1(tremoloDuration));
        float periodB = Mathf.Max(1e-4f, periodA * SelectRatio(tremoloDuration2));
        float periodC = Mathf.Max(1e-4f, periodA * SelectRatio(tremoloDuration3));
        float periodD = Mathf.Max(1e-4f, periodA * SelectRatio(tremoloDuration4));

        // reset/prime on toggle
        
        if (!playTremolo)
        {
            _tremoloTimer = 0f;
            _t2Ready = _t3Ready = _t4Ready = false;
        }
        else if (!_wasPlayTremolo)
        {
            _tremoloTimer = 0f;
            _t2Ready = _t3Ready = _t4Ready = true;
        }
        

        //_tremoloTimer += Time.deltaTime;
        
        // --- Trem 1 ---
        if (playTremolo)
        {
            //if (_tremoloTimer <= 0f)
                

            if (_tremoloTimer >= periodA)
            {
                sender.PlayFixedNote01(tremoloNote, tremoloNoteVelocity, tremoloNoteDuration);
                _tremoloTimer = 0f;
                _t2Ready = _t3Ready = _t4Ready = true;
            }

            _tremoloTimer += Time.deltaTime;
        }

        // --- Trem 2 (derived) ---
        if (playTremolo2 && _t2Ready && _tremoloTimer >= periodB)
        {
            _t2Ready = false;
            sender.PlayFixedNote01(tremoloNote2, tremoloNoteVelocity2, tremoloNoteDuration2);
        }

        // --- Trem 3 (derived) ---
        if (playTremolo3 && _t3Ready && _tremoloTimer >= periodC)
        {
            _t3Ready = false;
            sender.PlayFixedNote01(tremoloNote3, tremoloNoteVelocity3, tremoloNoteDuration3);
        }

        // --- Trem 4 (derived) ---
        if (playTremolo4 && _t4Ready && _tremoloTimer >= periodD)
        {
            _t4Ready = false;
            sender.PlayFixedNote01(tremoloNote4, tremoloNoteVelocity4, tremoloNoteDuration4);
        }

        _wasPlayTremolo = playTremolo;
        
        
    }
    
    
    
}