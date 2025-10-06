using UnityEditor;
using UnityEngine;

public class MidiDemo : MonoBehaviour
{
    public MidiSettingSender sender;
    private MidiSetting _setting;
    
    [Range(0f,1f)]
    public float note;
    public float duration;
    public bool playDuration;
    public bool playOn;
    public bool playOff;

    [Header("tremolo 1")]
    [Range(0f,2f)]
    public float tremoloDuration;
    [Range(0f,1f)]
    public float tremoloNoteDuration;
    [Range(0f,1f)]
    public float tremoloNote;
    [Range(0f,1f)]
    public float tremoloNoteVelocity;
    public bool playtremolo;
    private float _tremoloTimer;
    
    [Header("tremolo 2")]
    [Range(0f,2f)]
    public float tremoloDuration2;
    [Range(0f,1f)]
    public float tremoloNoteDuration2;
    [Range(0f,1f)]
    public float tremoloNote2;
    [Range(0f,1f)]
    public float tremoloNoteVelocity2;
    public bool playtremolo2;
    private float _tremoloTimer2;
    
    [Header("Pitch Bend")]
    [Range(0f,1f)]
    public float pitchBend;
    public bool setPitchBend;

    [Header("Mod Wheel")]
    [Range(0f,1f)]
    public float modWheel;
    public bool setModWheel;

    [Header("Portamento")]
    [Range(0f,1f)]
    public float portamento;
    public bool setPortamento;

    [Header("Variable 1")]
    [Range(0f,1f)]
    public float var1;
    public bool setVar1;
 
    [Header("Variable 2")]
    [Range(0f,1f)]
    public float var2;
    public bool setVar2;

    void Awake()
    {
        _setting = sender.setting;
    }

    // Update is called once per frame
    void Update()
    {
        if (playDuration)
        {
            sender.PlayFixedNote01(note, .8f, duration);
            playDuration = false;
        }
        
        if (playOn)
        {
            sender.NoteOn01(note, .8f);
            playOn = false;
        }
        if (playOff)
        {
            sender.NoteOff01(note);
            playOff = false;
        }
        if (setPitchBend)
        {
            sender.SendPitchBend01(pitchBend);
        }
        if (setPitchBend)
        {
            sender.SendModWheel01(modWheel);
        }
        if (setPortamento)
        {
            sender.SendPortamento(portamento);
        }
        if (setVar1)
        {
            sender.SendVariable101(var1);
        }
        if (setVar2)
        {
            sender.SendVariable201(var2);
        }

        if (playtremolo)
        {
            if (tremoloNoteDuration > tremoloDuration)
            {
                tremoloNoteDuration = tremoloDuration - .01f;
            }

            if (_tremoloTimer == 0)
            {
                
                sender.PlayFixedNote01(tremoloNote, tremoloNoteVelocity, tremoloNoteDuration);
            }
            
            _tremoloTimer += Time.deltaTime;
            
            if (_tremoloTimer > tremoloDuration)
            {
                _tremoloTimer = 0;
            }
            
        }

        if (!playtremolo)
        {
            _tremoloTimer = 0f;
        }

        if (playtremolo2)
        {
            int index = Mathf.RoundToInt(Mathf.Lerp(0, _setting.tremoloRatios.Length - 1, Mathf.Clamp01(tremoloDuration2)));
                
            float duration2 = tremoloDuration2 * _setting.tremoloRatios[index];
            
            if (tremoloNoteDuration2 > tremoloDuration2)
            {
                tremoloNoteDuration2 = tremoloDuration2 - .01f;
            }

            if (_tremoloTimer2 == 0)
            {

                
                sender.PlayFixedNote01(tremoloNote2, tremoloNoteVelocity2, tremoloNoteDuration2);
            }
            
            _tremoloTimer2 += Time.deltaTime;
            
            if (_tremoloTimer2 > duration2)
            {
                _tremoloTimer2 = 0;
            }
            
        }

        if (!playtremolo2)
        {
            _tremoloTimer2 = 0f;
        }
        
    }
}
