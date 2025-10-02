using System;
using System.Runtime.CompilerServices;
using Lemmings;
using ReasonMidi;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(MidiSettingSender))]
public class QuadTremDriver : MonoBehaviour
{
    [Header("Primary tremolo Note")]
    public MidiSettingSender sender;
    private MidiSetting _setting;

    [Header("Tremolo 1")] 
    public LemmingRelationship TremoloPeriod1;
    private float _tremoloPeriod;
    public LemmingRelationship TremoloNote1;
    private float _tremoloNote;

    private bool _playTremolo1;
    private float _tremoloTimer;

    [Header("Tremolo 2")] 
    public LemmingRelationship TremoloPeriod2;
    private float _tremoloPeriod2;
    public LemmingRelationship TremoloNote2;
    private float _tremoloNote2;
    

    private bool _playTremolo2;
    private bool _tremolo2Ready;
    
    
    [Header("Tremolo 3")] 
    public LemmingRelationship TremoloPeriod3;
    private float _tremoloPeriod3;
    public LemmingRelationship TremoloNote3;
    private float _tremoloNote3;

    private bool _playTremolo3;
    private bool _tremolo3Ready;
    
    [Header("Tremolo 4")] 
    public LemmingRelationship TremoloPeriod4;
    private float _tremoloPeriod4;
    public LemmingRelationship TremoloNote4;
    private float _tremoloNote4;

    private bool _playTremolo4;
    private bool _tremolo4Ready;
    
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
    void Awake() {
        if (!sender) { Debug.LogError("QuadTremDriver: sender not assigned."); enabled = false; return; }
        _setting = sender.setting;
        if (!_setting) { Debug.LogError("QuadTremDriver: sender has no MidiSetting assigned."); enabled = false; return; }
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
        _playTremolo1 = TremoloPeriod1 && !TremoloPeriod1.CachedInfo.Under;
        if (TremoloPeriod1) _tremoloPeriod = TremoloPeriod1.CachedInfo.CurvedValue; 
        if (TremoloNote1) _tremoloNote = TremoloNote1.CachedInfo.CurvedValue;
        
        // Tremolo 2 update 
        _playTremolo2 = TremoloPeriod2 && !TremoloPeriod2.CachedInfo.Under;
        if (TremoloPeriod2) _tremoloPeriod2 = TremoloPeriod2.CachedInfo.CurvedValue; 
        if (TremoloNote2) _tremoloNote2 = TremoloNote2.CachedInfo.CurvedValue;

        // Tremolo 3 update 
        _playTremolo3 = TremoloPeriod3 && !TremoloPeriod3.CachedInfo.Under;
        if (TremoloPeriod3) _tremoloPeriod3 = TremoloPeriod3.CachedInfo.CurvedValue; 
        if (TremoloNote3) _tremoloNote3 = TremoloNote3.CachedInfo.CurvedValue;
        
        // Tremolo 4 update 
        _playTremolo4 = TremoloPeriod4 && !TremoloPeriod4.CachedInfo.Under;
        if (TremoloPeriod4) _tremoloPeriod4 = TremoloPeriod4.CachedInfo.CurvedValue; 
        if (TremoloNote4) _tremoloNote4 = TremoloNote4.CachedInfo.CurvedValue;
        
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
    public float SelectRatio(float timing)
    {
        var arr = _setting?.tremoloRatios;
        if (arr == null || arr.Length == 0) return 2f/3f; // fallback
        int idx = Mathf.RoundToInt(Mathf.Clamp01(timing) * (arr.Length - 1));
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
        float periodB = periodA * SelectRatio(_tremoloPeriod2);
        float periodC = periodA * SelectRatio(_tremoloPeriod3);
        float periodD = periodA * SelectRatio(_tremoloPeriod4);
        periodA = Mathf.Max(1e-4f, periodA);
        periodB = Mathf.Max(1e-4f, periodB);
        periodC = Mathf.Max(1e-4f, periodC);
        periodD = Mathf.Max(1e-4f, periodD);

        // --- Trem 1 ---
        if (_playTremolo1)
        {
            

            _tremoloTimer += Time.deltaTime;
            if (_tremoloTimer >= periodA)
            {
                sender.PlayFixedNote01(_tremoloNote, .75f, .15f);
                
                _tremolo2Ready = true;
                _tremolo3Ready = true;
                _tremolo4Ready = true;
                
                _tremoloTimer = 0f;
            }
        }
        else _tremoloTimer = 0f;

        // --- Trem 2 (derived) ---
        if (_playTremolo2)
        {
            if (_tremoloTimer >= periodB && _tremolo2Ready)
            {
                _tremolo2Ready = false;
                sender.PlayFixedNote01(_tremoloNote2, .75f, .15f);
            }
        }
        
        // --- Trem 2 (derived) ---
        if (_playTremolo3)
        {
            if (_tremoloTimer >= periodC && _tremolo3Ready)
            {
                _tremolo3Ready = false;
                sender.PlayFixedNote01(_tremoloNote3, .75f, .15f);
            }
        }
        
        // --- Trem 2 (derived) ---
        if (_playTremolo4)
        {
            if (_tremoloTimer >= periodD && _tremolo4Ready)
            {
                _tremolo4Ready = false;
                sender.PlayFixedNote01(_tremoloNote4, .75f, .15f);
            }
        }

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