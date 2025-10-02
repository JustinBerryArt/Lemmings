using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RtMidi; // your C# RtMidi wrapper

/// <summary>
/// Interface for taking normalized (0..1) values and sending MIDI messages
/// filtered by a <see cref="MidiSetting"/> asset.
/// </summary>
/// <remarks>
/// Attach this to a GameObject, assign a MidiSetting in the inspector,
/// and call the public methods with normalized values. It will:
///  - Open the configured RtMidi output port
///  - Map 0..1 values into the ranges defined in the MidiSetting
///  - Constrain notes to the selected scale + octave range
///  - Send Note On/Off, Mod Wheel, Pitch Bend (14-bit), two custom CCs, and Portamento Time
/// </remarks>
[AddComponentMenu("MIDI/Midi Setting Sender")] 
public sealed class MidiSettingSender : MonoBehaviour
{
    [Header("Routing")] 
    public MidiSetting setting;                 // ScriptableObject with ranges/scale
    [Range(1,16)] public int midiChannel = 1;                     // 1..16
    public int bus;
    
    private MidiOut _out;                                         // RtMidi device
    public bool IsOpen => _out != null;

    #region Lifecycle
    private void OnEnable()
    {
        TryOpen();
    }

    private void OnDisable()
    {
        Close();
    }

    public void TryOpen()
    {
        if (IsOpen) return;
        if (setting == null)
        {
            Debug.LogWarning("MidiSettingSender: No MidiSetting asset assigned.");
            return;
        }

        try
        {
            _out = MidiOut.Create();
            int port = Mathf.Clamp(bus, 0, Mathf.Max(0, _out.PortCount - 1));
            _out.OpenPort(port);
            Debug.Log($"MidiSettingSender: Opened MIDI OUT port #{port}.");
        }
        catch (Exception e)
        {
            Debug.LogError($"MidiSettingSender: Failed to open MIDI port — {e.Message}");
            Close();
        }
    }

    public void Close()
    {
        if (_out != null)
        {
            _out.Dispose();
            _out = null;
        }
    }
    #endregion

    #region Utility (packing, scaling)
    private int ChNibble => Mathf.Clamp(midiChannel - 1, 0, 15);

    private static byte To7(float v01)
    {
        return (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(v01) * 127f), 0, 127);
    }

    private static (byte msb, byte lsb) To14(float v01)
    {
        int v = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(v01) * 16383f), 0, 16383);
        return ((byte)((v >> 7) & 0x7F), (byte)(v & 0x7F));
    }

    private static int LerpInt(int min, int max, float t01)
    {
        return Mathf.RoundToInt(Mathf.Lerp(min, max, Mathf.Clamp01(t01)));
    }

    private unsafe void SendShort(byte status, byte data1, byte data2)
    {
        if (!IsOpen) { Debug.LogWarning("MidiSettingSender: output not open."); return; }
        var msg = stackalloc byte[3] { status, data1, data2 };
        _out.SendMessage(new ReadOnlySpan<byte>(msg, 3));
    }

    private void SendCC(int cc, int value7)
    {
        byte status = (byte)(0xB0 | ChNibble);
        byte c = (byte)Mathf.Clamp(cc, 0, 127);
        byte v = (byte)Mathf.Clamp(value7, 0, 127);
        SendShort(status, c, v);
    }

    #endregion

    #region Scale → MIDI note mapping
    // Order matches MidiSetting fields: Ab, A, Bb, B, C, Db, D, Eb, E, F, Gb, G
    private IEnumerable<int> EnumerateAllowedNotes()
    {
        if (setting == null) yield break;

        bool[] pc = new bool[12];
        pc[8]  = setting.Ab; // G#
        pc[9]  = setting.A;
        pc[10] = setting.Bb; // A#
        pc[11] = setting.B;
        pc[0]  = setting.C;
        pc[1]  = setting.Db; // C#
        pc[2]  = setting.D;
        pc[3]  = setting.Eb; // D#
        pc[4]  = setting.E;
        pc[5]  = setting.F;
        pc[6]  = setting.Gb; // F#
        pc[7]  = setting.G;

        int oMin = Mathf.Clamp(setting.octaveMin, 0, 10);
        int oMax = Mathf.Clamp(setting.octaveMax, 0, 10);
        if (oMax < oMin) (oMin, oMax) = (oMax, oMin);

        for (int o = oMin; o <= oMax; o++)
        {
            for (int semitone = 0; semitone < 12; semitone++)
            {
                if (!pc[semitone]) continue;
                int note = o * 12 + semitone;
                if (note >= 0 && note <= 127)
                    yield return note;
            }
        }
    }

    /// <summary>Maps a normalized pitch position to the closest allowed MIDI note from the asset's scale+octave range.</summary>
    public int NoteFrom01(float pitch01)
    {
        var allowed = new List<int>(EnumerateAllowedNotes());
        if (allowed.Count == 0) { return 60; } // default: middle C
        int idx = Mathf.RoundToInt(Mathf.Clamp01(pitch01) * (allowed.Count - 1));
        return allowed[idx];
    }
    #endregion

    #region Performance messages (public API)
    /// <summary>Send Note On using normalized pitch and velocity filtered through the asset.</summary>
    public void NoteOn01(float pitch01, float velocity01)
    {
        int note = NoteFrom01(pitch01);
        int vel  = LerpInt(setting.velocityMin, setting.velocityMax, velocity01);
        byte status = (byte)(0x90 | ChNibble);
        SendShort(status, (byte)note, (byte)vel);
    }

    public void NoteOff01(float pitch01)
    {
        int note = NoteFrom01(pitch01);
        byte status = (byte)(0x80 | ChNibble);
        SendShort(status, (byte)Mathf.Clamp(note,0,127), 64);;
    }
    
    /// <summary>Send Note Off for a MIDI note number (0..127).</summary>
    public void NoteOff(int note)
    {
        byte status = (byte)(0x80 | ChNibble);
        SendShort(status, (byte)Mathf.Clamp(note,0,127), 64);
    }

    /// <summary>Plays a fixed-duration note; duration is remapped from 0..1 into the asset's fixedNoteMin..Max seconds.</summary>
    public void PlayFixedNote01(float pitch01, float velocity01, float duration01, MonoBehaviour runner = null)
    {
        var host = runner ? runner : this;
        host.StartCoroutine(CoPlayFixedNote01(pitch01, velocity01, duration01));
    }

    private IEnumerator CoPlayFixedNote01(float pitch01, float velocity01, float duration01)
    {
        int note = NoteFrom01(pitch01);
        int vel  = LerpInt(setting.velocityMin, setting.velocityMax, velocity01);
        float dur = Mathf.Lerp(setting.fixedNoteMin, setting.fixedNoteMax, Mathf.Clamp01(duration01));
        byte on = (byte)(0x90 | ChNibble);
        byte off = (byte)(0x80 | ChNibble);
        SendShort(on,  (byte)note, (byte)vel);
        yield return new WaitForSeconds(dur);
        SendShort(off, (byte)note, 64);
    }

    /// <summary>Sets Mod Wheel (CC1) using the asset's min/max range.</summary>
    public void SendModWheel01(float v01)
    {
        int val = LerpInt(setting.modWheelMin, setting.modWheelMax, v01);
        SendCC(setting.modWheelccChannel, val);
    }

    /// <summary>Sends Pitch Bend (14-bit). Value is normalized; asset's min/max (0..127) gate the usable range.</summary>
    public void SendPitchBend01(float v01)
    {
        float min01 = Mathf.Clamp01(setting.pitchBendMin / 127f);
        float max01 = Mathf.Clamp01(setting.pitchBendMax / 127f);
        if (max01 < min01) (min01, max01) = (max01, min01);
        float gated = Mathf.Lerp(min01, max01, Mathf.Clamp01(v01));
        var (msb, lsb) = To14(gated);
        byte status = (byte)(0xE0 | ChNibble);
        SendShort(status, lsb, msb); // LSB first, then MSB
    }

    /// <summary>Sets the first custom variable CC (name and CC are defined in the asset).</summary>
    public void SendVariable101(float v01)
    {
        if (setting.variable1ccChannel >= 0)
        {
            int val = LerpInt(setting.variable1Min, setting.variable1Max, v01);
            SendCC(setting.variable1ccChannel, val);
        }
    }

    /// <summary>Sets the second custom variable CC (name and CC are defined in the asset).</summary>
    public void SendVariable201(float v01)
    {
        if (setting.variable2ccChannel >= 0)
        {
            int val = LerpInt(setting.variable2Min, setting.variable2Max, v01);
            SendCC(setting.variable2ccChannel, val);
        }
    }

    /// <summary>Applies Portamento Time (CC5) if enabled in the asset. If off, sends 0.</summary>
    public void SendPortamento(float v01)
    {
        int val = LerpInt(setting.portamentoLevelMin, setting.portamentoLevelMax, v01);
        SendCC(setting.portamentoccChannel, val);
    }


    #endregion
}
