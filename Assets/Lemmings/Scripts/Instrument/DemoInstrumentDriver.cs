using System;
using System.Runtime.CompilerServices;
using Lemmings;
using ReasonMidi;
using UnityEngine;
using UnityEngine.Serialization;

public class DemoInstrumentDriver : MonoBehaviour
{
    [Header("Primary tremolo Note")]
    public MidiSettingSender sender;
    private MidiSetting _setting;

    [Header("Tremolo 1")] 
    public LemmingRelationship TremoloPeriod1;
    private float _tremoloPeriod;
    public LemmingRelationship TremoloNoteDuration1;
    private float _tremoloNoteDuration;  
    public LemmingRelationship TremoloNote1;
    private float _tremoloNote;
    public LemmingRelationship TremoloVelocity1;
    private float _tremoloNoteVelocity;
    public bool playTremolo => _tremoloPeriod > 0 && _tremoloNoteDuration > 0 && _tremoloNoteVelocity > 0;
    private float _tremoloTimer;

    
    [Header("Tremolo 2 (follows 1 by ratio)")]
    public LemmingRelationship TremoloPeriod2;
    private float _tremoloPeriod2;
    public LemmingRelationship TremoloNoteDuration2;
    private float _tremoloNoteDuration2;  
    public LemmingRelationship TremoloNote2;
    private float _tremoloNote2;
    public LemmingRelationship TremoloVelocity2;
    private float _tremoloNoteVelocity2;
    public bool playTremolo2 => _tremoloPeriod2 > 0 && _tremoloNoteDuration2 > 0 && _tremoloNoteVelocity2 > 0;
    private float _tremoloTimer2;

    [Header("Pitch Bend")] 
    public LemmingRelationship PitchBend;
    public bool setPitchBend;
    private float _pitchBend;
    private bool _pitchReset;
    
    [Header("Mod Wheel")] 
    public LemmingRelationship ModWheel;
    public bool setModWheel;
    private float _modWheel; 
    
    /// <summary>
    /// Grab the settings from the sender
    /// </summary>
    void Awake()
    {
        if (!sender) { Debug.LogError("GestureDriver: sender not assigned."); return; }
        _setting = sender.setting;
        if (!_setting) Debug.LogError("GestureDriver: sender has no MidiSetting assigned.");
    }
    
    
    /// <summary>
    /// This is an alternate function to update the tremolo values
    /// </summary>
    /// <param name="r">The driving relationship</param>
    /// <returns>The curved output of a given relationship</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float ReadCachedCurve(LemmingRelationship r) => r ? r.CachedInfo.CurvedValue : 0f;
    
    /// <summary>
    /// This pulls the instrument settings from the Lemming Relationships cached data
    /// </summary>
    /// <remarks>This relies on the relationships to be managed by a Lemming Shepherd</remarks>
    void PullData() 
    {
        // Tremolo 1 update 
        if (TremoloPeriod1) _tremoloPeriod = TremoloPeriod1.CachedInfo.CurvedValue; 
        if (TremoloNoteDuration1) _tremoloNoteDuration = TremoloNoteDuration1.CachedInfo.CurvedValue;
        if (TremoloNote1) _tremoloNote = TremoloNote1.CachedInfo.CurvedValue;
        if (TremoloVelocity1) _tremoloNoteVelocity = TremoloVelocity1.CachedInfo.CurvedValue;

        // Tremolo 2 update 
        if (TremoloPeriod2) _tremoloPeriod2 = TremoloPeriod2.CachedInfo.CurvedValue; 
        if (TremoloNoteDuration2) _tremoloNoteDuration2 = TremoloNoteDuration2.CachedInfo.CurvedValue;
        if (TremoloNote2) _tremoloNote2 = TremoloNote2.CachedInfo.CurvedValue;
        if (TremoloVelocity2) _tremoloNoteVelocity2 = TremoloVelocity2.CachedInfo.CurvedValue;

        // Modifiers update 
        if (PitchBend) _pitchBend = PitchBend.CachedInfo.CurvedValue; 
        if (ModWheel) _modWheel = ModWheel.CachedInfo.CurvedValue;
    }

    /// <summary>
    /// This converts the normalized value to being within the range defined in the settings
    /// </summary>
    /// <returns>The value in seconds for tremolo 1's period</returns>
    public float Period1(float d01) =>
        Mathf.Lerp(_setting.tremoloMin, _setting.tremoloMax, Mathf.Clamp01(d01));

    /// <summary>
    /// This sets the ratio for the second tremolo based on the options selected in the settings
    /// </summary>
    /// <returns>The timing ratio for tremolo 2</returns>
    public float SelectRatio()
    {
        var arr = _setting?.tremoloRatios;
        if (arr == null || arr.Length == 0) return 2f/3f; // fallback
        int idx = Mathf.RoundToInt(Mathf.Clamp01(_tremoloPeriod2) * (arr.Length - 1));
        return Mathf.Clamp01(arr[idx]);
    }
    
    /// <summary>
    /// This plays the tremolo(s)
    /// - Set Period A
    /// - Set Period B relative to A
    /// - Run Timers to play Tremolos 1 & 2
    /// </summary>
    public void PlayTremolo()
    {
        float periodA = Period1(_tremoloPeriod);
        float periodB = periodA * SelectRatio();
        periodA = Mathf.Max(1e-4f, periodA);
        periodB = Mathf.Max(1e-4f, periodB);

        // --- Trem 1 ---
        if (playTremolo)
        {
            if (_tremoloTimer <= 0f && _tremoloNoteDuration >= 0f)
                sender.PlayFixedNote01(_tremoloNote, _tremoloNoteVelocity, _tremoloNoteDuration);

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
                if (_tremoloNoteDuration2 >= 0f)
                    sender.PlayFixedNote01(_tremoloNote2, _tremoloNoteVelocity2, _tremoloNoteDuration2);
            }
        }
        else _tremoloTimer2 = 0f;
    }
    
    /// <summary>
    /// Using Late Update so that it pulls after the Shepherd's update function
    /// </summary>
    public void LateUpdate()
    {
        PullData();
        PlayTremolo();
        if (setPitchBend) sender.SendPitchBend01(_pitchBend);
        //if (!setPitchBend && !_pitchReset) ReasonMidiStandard.SendPitchBendCenter();
        if (setModWheel)  sender.SendModWheel01(_modWheel);
    }
}