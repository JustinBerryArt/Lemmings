using Lemmings;
using UnityEngine;


/// <summary>
/// This is the BeBodBopper, a simple instrument that works with LoopMidi and RTMidi to send Midi outputs.
/// It is designed to establish a bar length and fill it with locations at regular intervals.
/// The instrument lets you adjust the notes and the note locations to create melodies and rhythms.
/// It contains space for up to 4 beats per bar controlled by Lemming Relationships
/// 
/// See below for resources required:
/// LoopMidi -> https://www.tobias-erichsen.de/software/loopmidi.html
/// RTMidi -> https://github.com/keijiro/jp.keijiro.rtmidi
/// </summary>
[RequireComponent(typeof(BeBodBopSender))]
public class BeBodBopMachine : MonoBehaviour
{
    [Header("Configuration")]
    public BeBodBopSender sender;
    private BeBodBopSetting _setting;
    [SerializeField] public float Timing => _beatTimer;
    private float _beatTimer;

    [Header("Note 1")] 
    public bool useNote1 = true;
    public LemmingRelationship BeatTiming1;
    private float _beatTiming1;
    public LemmingRelationship BeatNote1;
    private float _beatNote1;

    private bool _playBeat1;
    private bool _beat1Ready;

    [Header("Note 2")] 
    public bool useNote2 = true;
    public LemmingRelationship BeatTiming2;
    private float _beatTiming2;
    public LemmingRelationship BeatNote2;
    private float _beatNote2;
    
    private bool _playBeat2;
    private bool _beat2Ready;
    
    [Header("Note 3")] 
    public bool useNote3 = true;
    public LemmingRelationship BeatTiming3;
    private float _beatTiming3;
    public LemmingRelationship BeatNote3;
    private float _beatNote3;

    private bool _playBeat3;
    private bool _beat3Ready;
    
    [Header("Note 4")] 
    public bool useNote4 = true;
    public LemmingRelationship BeatTiming4;
    private float _beatTiming4;
    public LemmingRelationship BeatNote4;
    private float _beatNote4;

    private bool _playBeat4;
    private bool _beat4Ready;
    
    
    
    /// <summary>
    /// Grab the settings from the sender
    /// </summary>
    void Awake() {
        if (!sender) { Debug.LogError("BeBodBopperr: sender not assigned."); enabled = false; return; }
        _setting = sender.setting;
        if (!_setting) { Debug.LogError("BeBodBopper: sender has no MidiSetting assigned."); enabled = false; return; }
    }

    void Start()
    {
        var s = Lemmings.LemmingShepherd.Instance;
        if (!s) { Debug.LogError("No LemmingShepherd in scene."); enabled = false; return; }

        void Reg(Lemmings.LemmingRelationship r){ if (r) s.RegisterRelationship(r); }

        Reg(BeatTiming1); Reg(BeatNote1);
        Reg(BeatTiming2); Reg(BeatNote2);
        Reg(BeatTiming3); Reg(BeatNote3);
        Reg(BeatTiming4); Reg(BeatNote4);
    }
    
    /// <summary>
    /// This pulls the instrument settings from the Lemming Relationships cached data
    /// </summary>
    /// <remarks>This relies on the relationships to be managed by a Lemming Shepherd</remarks>
    void PullData() 
    {
        // Beat 1 update 
        var timing1info = BeatTiming1.Info;
        var note1info = BeatNote1.Info;
        _playBeat1 = useNote1 && !timing1info.Under;
        if (BeatTiming1) _beatTiming1 = timing1info.CurvedValue; 
        if (BeatNote1) _beatNote1 = note1info.CurvedValue;
        
        // Beat 2 update 
        var timing2info = BeatTiming2.Info;
        var note2info = BeatNote2.Info;
        _playBeat2 = useNote2 && !timing2info.Under;
        if (BeatTiming2) _beatTiming2 = timing2info.CurvedValue; 
        if (BeatNote2) _beatNote2 = note2info.CurvedValue;

        // Beat 3 update 
        var timing3info = BeatTiming3.Info;
        var note3info = BeatNote3.Info;
        _playBeat3 = useNote3 && !timing3info.Under;
        if (BeatTiming3) _beatTiming3 = timing3info.CurvedValue; 
        if (BeatNote3) _beatNote3 = note3info.CurvedValue;
        
        // Beat 4 update 
        var timing4info = BeatTiming4.Info;
        var note4info = BeatNote4.Info;
        _playBeat4 = useNote4 && !timing4info.Under;
        if (BeatTiming4) _beatTiming4 = timing4info.CurvedValue; 
        if (BeatNote4) _beatNote4 = note4info.CurvedValue;
        
        /*
        _playBeat4 =useNote4 &&  BeatTiming4 && !BeatTiming4.CachedInfo.Under;
        if (BeatTiming4) _beatTiming4 = BeatTiming4.CachedInfo.CurvedValue; 
        if (BeatNote4) _beatNote4 = BeatNote4.CachedInfo.CurvedValue;
        */

    }

    /// <summary>
    /// This takes the total duration of the bar and multiplies it by a ratio to get a beats timing
    /// </summary>
    /// <returns>The duration for a beat</returns>
    public float SelectRatio(float timing)
    {
        var arr = _setting?.noteRatios;
        if (arr == null || arr.Length == 0) return 2f/3f; // fallback
        int idx = Mathf.RoundToInt(Mathf.Clamp01(timing) * (arr.Length - 1));
        return Mathf.Clamp01(arr[idx]);
    }
    
    /// <summary>
    /// This plays the beat(s)
    /// </summary>
    public void PlayBeat()
    {
        float periodBase = _setting.duration;
        float periodA = Mathf.Max(1e-4f, periodBase * SelectRatio(_beatTiming1));
        float periodB = Mathf.Max(1e-4f, periodBase * SelectRatio(_beatTiming2));
        float periodC = Mathf.Max(1e-4f, periodBase * SelectRatio(_beatTiming3));
        float periodD = Mathf.Max(1e-4f, periodBase * SelectRatio(_beatTiming4));

        _beatTimer += Time.deltaTime;
        
        if (_beatTimer >= periodBase)
        {
            // reset beats
            _beat1Ready = true;
            _beat2Ready = true;
            _beat3Ready = true;
            _beat4Ready = true;
                   
            
            // reset timer
            _beatTimer = 0f;
        }
 
        // --- Beat 1 ---
        if (_playBeat1 )
        {
            if (_beatTimer >= periodA && _beat1Ready)
            {
                _beat1Ready = false;
                //Debug.Log("Note1 Traw: " + _beatTiming1 + " Tper: " + periodA + "Note: " + _beatNote1 );
                sender.PlayFixedNote01(_beatNote1, _setting.velocity, _setting.sustain);
            }
        }


        // --- Beat 2 ---
        if (_playBeat2)
        {
            if (_beatTimer >= periodB && _beat2Ready)
            {
                _beat2Ready = false;
                //Debug.Log("Note2 Traw: " + _beatTiming2 + " Tper: " + periodB + "Note: " + _beatNote2 );
                sender.PlayFixedNote01(_beatNote2, _setting.velocity, _setting.sustain);
            }
        }
        
        // --- Beat 3 ---
        if (_playBeat3)
        {
            if (_beatTimer >= periodC && _beat3Ready)
            {
                _beat3Ready = false;
                Debug.Log("Note3 Traw: " + _beatTiming3 + " Tper: " + periodC  + "Note: " + _beatNote3 );
                sender.PlayFixedNote01(_beatNote3, _setting.velocity, _setting.sustain);
            }
        }
        
        // --- Beat 4 ---
        if (_playBeat4)
        {
            if (_beatTimer >= periodD && _beat4Ready)
            {
                _beat4Ready = false;
                Debug.Log("Note4 Traw: " + _beatTiming4 + " Tper: " + periodD  + "Note: " + _beatNote4 );
                sender.PlayFixedNote01(_beatNote4, _setting.velocity, _setting.sustain);
            }
        }

    }
    
    /// <summary>
    /// Using Late Update so that it pulls after the Shepherd's update function
    /// </summary>
    public void LateUpdate()
    {
        PullData();
        PlayBeat();
    }
}