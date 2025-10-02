using UnityEngine;
using UnityEngine.Serialization;

public class MidiDemo2 : MonoBehaviour
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
    [Range(0f,1f)] public float tremoloNoteDuration;    
    [Range(0f,1f)] public float tremoloNote;
    [Range(0f,1f)] public float tremoloNoteVelocity;
    public bool playTremolo;
    private float _tremoloTimer;

    
    [Header("Tremolo 2 (follows 1 by ratio)")]
    [Range(0f,1f)] public float tremoloDuration2;  // Float that determines ratio
    [Range(0f,1f)] public float tremoloNoteDuration2;       
    [Range(0f,1f)] public float tremoloNote2;
    [Range(0f,1f)] public float tremoloNoteVelocity2;
    public bool playTremolo2;
    private float _tremoloTimer2;

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

    void Awake() => _setting = sender.setting;

    // --- helpers ---
    
    float tremoloPeriod1(float d01) =>
        Mathf.Lerp(_setting.tremoloMin, _setting.tremoloMax, Mathf.Clamp01(d01));

    float SelectRatio()
    {
        var arr = _setting?.tremoloRatios;
        if (arr == null || arr.Length == 0) return 2f/3f; // fallback
        int idx = Mathf.RoundToInt(Mathf.Clamp01(tremoloDuration2) * (arr.Length - 1));
        return Mathf.Clamp01(arr[idx]);
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



        float periodA = tremoloPeriod1(tremoloDuration);
        float periodB = periodA * SelectRatio();
        periodA = Mathf.Max(1e-4f, periodA);
        periodB = Mathf.Max(1e-4f, periodB);

        // --- Trem 1 ---
        if (playTremolo)
        {
            if (_tremoloTimer <= 0f && tremoloNoteDuration >= 0f)
                sender.PlayFixedNote01(tremoloNote, tremoloNoteVelocity, tremoloNoteDuration);

            _tremoloTimer += Time.deltaTime;
            if (_tremoloTimer >= periodA) _tremoloTimer = 0f;
        }
        else _tremoloTimer = 0f;

        // --- Trem 2 (derived) ---
        if (playTremolo2)
        {
            _tremoloTimer2 += Time.deltaTime;
            if (_tremoloTimer2 >= periodB)
            {
                _tremoloTimer2 -= periodB;
                if (tremoloNoteDuration2 >= 0f)
                    sender.PlayFixedNote01(tremoloNote2, tremoloNoteVelocity2, tremoloNoteDuration2);
            }
        }
        else _tremoloTimer2 = 0f;
    }
    
}