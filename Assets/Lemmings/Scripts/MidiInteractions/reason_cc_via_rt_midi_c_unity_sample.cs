// Reason CC via RtMidi — C# helper for Unity
// Assumes your project includes the provided RtMidi C# wrapper (MidiOut, etc.).
// Focus: how to SEND Control Change (CC) messages to alter Reason instrument parameters.
// Channel numbering here is 0–15 (i.e., MIDI Ch 1 == 0).

using System;
using System.Collections.Generic;
using RtMidi; // from your uploaded wrapper

public static class ReasonCc
{
    // ---------- Low-level send helpers ----------
    public static void SetCC(MidiOut dev, int channel, int cc, int value)
    {
        if (channel < 0 || channel > 15) throw new ArgumentOutOfRangeException(nameof(channel));
        if (cc < 0 || cc > 127) throw new ArgumentOutOfRangeException(nameof(cc));
        if (value < 0 || value > 127) throw new ArgumentOutOfRangeException(nameof(value));

        Span<byte> msg = stackalloc byte[3];
        msg[0] = (byte)(0xB0 + channel); // status: Control Change on channel
        msg[1] = (byte)cc;               // controller number
        msg[2] = (byte)value;            // value 0–127
        dev.SendMessage(msg);
    }

    public static void SetSwitch(MidiOut dev, int channel, int cc, bool on)
        => SetCC(dev, channel, cc, on ? 127 : 0); // 0–63 off, 64–127 on — use extremes

    public static void SetPan(MidiOut dev, int channel, int cc, float pan)
    {
        // pan in [-1..+1] => 0..127 (0=L, 64=Center, 127=R)
        var v = (int)Math.Clamp(Math.Round((pan + 1f) * 63.5f), 0, 127);
        SetCC(dev, channel, cc, v);
    }

    // ---------- Friendly API by instrument & parameter name ----------
    public enum Instrument { Subtractor, Thor, Malstrom, ID8 }

    // Mark which entries behave like on/off switches
    static readonly HashSet<string> SwitchParams = new()
    {
        // Subtractor
        "FilterEnv.Invert",
        // Thor
        "LFO2.Sync", "LFO2.KeySync", "Osc2.OnOff",
        // Malström
        "OscA.OnOff", "ModB.OneShot",
        // ID8
        "Transpose.On"
    };

    // Parameter maps: name => CC number
    static readonly Dictionary<string,int> Subtractor = new()
    {
        ["ModWheel"] = 1,
        ["BreathAsMod"] = 2,
        ["Osc12.Level"] = 4,
        ["Portamento"] = 5,
        ["Master.Level"] = 7,
        ["Mix.Balance12"] = 8,
        ["AmpEnv.Decay"] = 9,
        ["Amp.Pan"] = 10,
        ["ExpressionAsMod"] = 11,
        ["AmpEnv.Sustain"] = 12,
        ["Chorus.Mix"] = 13,
        ["FilterEnv.Attack"] = 14,
        ["FilterEnv.Decay"] = 15,
        ["FilterEnv.Sustain"] = 16,
        ["FilterEnv.Release"] = 17,
        ["FilterEnv.Amount"] = 18,
        ["FilterEnv.Invert"] = 19, // switch-like
        ["Osc1.Wave"] = 20,
        ["Osc1.Oct"] = 21,
        ["Osc1.Semi"] = 22,
        ["Osc1.Fine"] = 23,
        ["Osc1.Type"] = 24,
        ["Osc1.KbdTrack"] = 25,
        ["Osc2.Oct"] = 102,
        ["Osc2.Semi"] = 103,
        ["Osc2.Fine"] = 104,
        ["Osc2.PhaseMode"] = 105,
        ["Osc2.PhaseDiff"] = 106,
        ["Osc.Mix"] = 107,
        ["FM.Amount"] = 108,
        ["RingMod.Amount"] = 109,
        ["LFO2.Rate"] = 110,
        ["LFO2.Amount"] = 111,
        ["LFO2.Delay"] = 112,
        ["LFO2.Destination"] = 113,
        ["LFO2.KbdTrack"] = 114,
        ["Osc2.KbdTrack"] = 115,
    };

    static readonly Dictionary<string,int> Thor = new()
    {
        ["ModWheel"] = 1,
        ["Portamento"] = 5,
        ["Master.Level"] = 7,
        ["AmpEnv.Decay"] = 9,
        ["Delay.Mix"] = 12,
        ["FilterEnv.Attack"] = 14,
        ["FilterEnv.Decay"] = 15,
        ["FilterEnv.Sustain"] = 16,
        ["FilterEnv.Release"] = 17,
        ["Delay.Time"] = 18,
        ["Osc3.Level"] = 19,
        ["Osc1.ModAmt"] = 20,
        ["Osc1.Oct"] = 21,
        ["Osc1.Semi"] = 22,
        ["Osc1.Fine"] = 23,
        ["Osc.EnvAmt"] = 24,
        ["Delay.Feedback"] = 25,
        ["Osc1.PhaseMode"] = 92,
        ["Osc1.PhaseDiff"] = 93,
        ["Osc2.OnOff"] = 94, // switch-like
        ["Osc2.Wave"] = 95,
        ["Osc2.Oct"] = 102,
        ["Osc2.Semi"] = 103,
        ["Osc2.Fine"] = 104,
        ["Osc2.SyncAmt"] = 105,
        ["Osc2.SyncOn"] = 106, // switch-like
        ["Seq.Rate"] = 107,
        ["AM.Amount"] = 108,
        ["LFO2.Sync"] = 109,   // switch-like
        ["LFO2.Rate"] = 110,
        ["LFO2.Wave"] = 111,
        ["LFO2.Delay"] = 112,
        ["LFO2.KeySync"] = 113, // switch-like
        ["GlobEnv.Delay"] = 114,
        ["GlobEnv.Attack"] = 115,
        ["GlobEnv.Hold"] = 116,
        ["GlobEnv.Decay"] = 117,
        ["GlobEnv.Sustain"] = 118,
        ["GlobEnv.Release"] = 119,
    };

    static readonly Dictionary<string,int> Malstrom = new()
    {
        ["ModWheel"] = 1,
        ["Portamento"] = 5,
        ["Master.Level"] = 7,
        ["OscB.Decay"] = 9,
        ["OscB.Sustain"] = 12,
        ["FilterEnv.Attack"] = 14,
        ["FilterEnv.Decay"] = 15,
        ["FilterEnv.Sustain"] = 16,
        ["FilterEnv.Release"] = 17,
        ["FilterEnv.Amount"] = 18,
        ["FilterEnv.Invert"] = 19, // switch-like
        ["OscB.Oct"] = 21,
        ["OscB.Semi"] = 22,
        ["OscB.Cent"] = 23,
        ["OscA.Gain"] = 91,
        ["OscA.Motion"] = 92,
        ["OscA.Index"] = 93,
        ["Osc2.Type"] = 94,
        ["OscA.OnOff"] = 95,  // switch-like
        ["OscA.Oct"] = 102,
        ["OscA.Semi"] = 103,
        ["OscA.Cent"] = 104,
        ["Spread.Amount"] = 105,
        ["ModB.Rate"] = 110,
        ["ModB.ToLevel"] = 111,
        ["ModB.ToFilter"] = 112,
        ["ModB.ToModA"] = 113,
        ["ModB.OnOff"] = 114, // switch-like
        ["ModB.Curve"] = 115,
        ["ModB.OneShot"] = 116, // switch-like
        ["ModB.Target"] = 117,
        ["ModB.ToMotion"] = 118,
    };

    static readonly Dictionary<string,int> ID8 = new()
    {
        ["ModWheel"] = 1,
        ["Volume"] = 7,
        ["Transpose.On"] = 16, // switch-like
        ["Transpose.Semitone"] = 17,
        ["Transpose.Cent"] = 18,
    };

    static readonly Dictionary<Instrument, Dictionary<string,int>> Tables = new()
    {
        [Instrument.Subtractor] = Subtractor,
        [Instrument.Thor] = Thor,
        [Instrument.Malstrom] = Malstrom,
        [Instrument.ID8] = ID8,
    };

    public static void Set(Instrument inst, MidiOut dev, int channel, string param, int value)
    {
        var table = Tables[inst];
        if (!table.TryGetValue(param, out var cc))
            throw new ArgumentException($"Unknown {inst} parameter '{param}'.");
        SetCC(dev, channel, cc, value);
    }

    public static void Set(Instrument inst, MidiOut dev, int channel, string param, bool on)
    {
        var table = Tables[inst];
        if (!table.TryGetValue(param, out var cc))
            throw new ArgumentException($"Unknown {inst} parameter '{param}'.");
        if (!SwitchParams.Contains(param))
            throw new ArgumentException($"Parameter '{param}' on {inst} is not flagged as a switch.");
        SetSwitch(dev, channel, cc, on);
    }

    // OPTIONAL: touch all mapped params once — handy for testing
    public static void TouchAll(Instrument inst, MidiOut dev, int channel,
                                int valueForKnobs = 100,
                                bool onForSwitches = true)
    {
        foreach (var kv in Tables[inst])
        {
            var cc = kv.Value;
            if (SwitchParams.Contains(kv.Key)) SetSwitch(dev, channel, cc, onForSwitches);
            else SetCC(dev, channel, cc, valueForKnobs);
        }
    }
}

/* ------------------ Usage example ------------------
// Somewhere in your code after opening an RtMidi output port:
// MidiOut outDev = ... (already opened to Reason’s MIDI input)
int ch = 0; // channel 1

// Tweak a few Subtractor params:
ReasonCc.Set(ReasonCc.Instrument.Subtractor, outDev, ch, "FilterEnv.Attack", 90);
ReasonCc.Set(ReasonCc.Instrument.Subtractor, outDev, ch, "FilterEnv.Invert", true);
ReasonCc.Set(ReasonCc.Instrument.Subtractor, outDev, ch, "Amp.Pan", 64); // center

// Tweak Thor:
ReasonCc.Set(ReasonCc.Instrument.Thor, outDev, ch, "LFO2.Sync", true);
ReasonCc.Set(ReasonCc.Instrument.Thor, outDev, ch, "LFO2.Rate", 80);

// Tweak Malström:
ReasonCc.Set(ReasonCc.Instrument.Malstrom, outDev, ch, "ModB.OnOff", true);
ReasonCc.Set(ReasonCc.Instrument.Malstrom, outDev, ch, "ModB.Rate", 100);

// Tweak ID8:
ReasonCc.Set(ReasonCc.Instrument.ID8, outDev, ch, "Transpose.On", true);
ReasonCc.Set(ReasonCc.Instrument.ID8, outDev, ch, "Transpose.Semitone", 76);

// Blast everything once (test):
ReasonCc.TouchAll(ReasonCc.Instrument.Subtractor, outDev, ch);
*/
