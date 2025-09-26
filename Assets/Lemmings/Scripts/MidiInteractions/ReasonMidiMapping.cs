// Preview scaffolding for Reason mappers in Unity
// ------------------------------------------------
// This shows the overall shape and ergonomics without locking us to exact CC/NRPN IDs yet.
// Wire these to your existing RtMidi C# wrapper by implementing IMidiSender.

using System;
using System.Runtime.CompilerServices;
using ReasonMidi;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReasonMidi
{
    // 1) Transport-agnostic, GC-light helpers
    public interface IMidiSender
    {
        void SendShort(byte status, byte data1, byte data2);
        void SendSysex(byte[] data);
    }

    public static class MidiHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Clamp7(float v01) => (byte)Mathf.Clamp(Mathf.RoundToInt(v01 * 127f), 0, 127);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (byte msb, byte lsb) To14bit(float v01)
        {
            int val = Mathf.Clamp(Mathf.RoundToInt(v01 * 16383f), 0, 16383);
            return ((byte)((val >> 7) & 0x7F), (byte)(val & 0x7F));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendCC(IMidiSender s, int channel, int cc, int value)
        {
            byte status = (byte)(0xB0 | (channel & 0x0F));
            s.SendShort(status, (byte)(cc & 0x7F), (byte)(value & 0x7F));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendCC14Pair(IMidiSender s, int channel, int ccMsb, int ccLsb, float v01)
        {
            var (msb, lsb) = To14bit(v01);
            SendCC(s, channel, ccMsb, msb);
            SendCC(s, channel, ccLsb, lsb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendNoteOn(IMidiSender s, int channel, int note, int vel)
        {
            byte status = (byte)(0x90 | (channel & 0x0F));
            s.SendShort(status, (byte)(note & 0x7F), (byte)(vel & 0x7F));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendNoteOff(IMidiSender s, int channel, int note)
        {
            byte status = (byte)(0x80 | (channel & 0x0F));
            s.SendShort(status, (byte)(note & 0x7F), 0);
        }

        // Basic NRPN helper (14-bit). Many Reason devices are CC-only, but we keep this ready.
        public static void SendNRPN14(IMidiSender s, int channel, int nrpnMsb, int nrpnLsb, float v01)
        {
            var (msb, lsb) = To14bit(v01);
            SendCC(s, channel, 99, nrpnMsb); // NRPN MSB
            SendCC(s, channel, 98, nrpnLsb); // NRPN LSB
            SendCC(s, channel, 6, msb);      // Data Entry MSB
            SendCC(s, channel, 38, lsb);     // Data Entry LSB
        }

        public static void AllNotesOff(IMidiSender s, int channel)
        {
            SendCC(s, channel, 123, 0);
        }
    }

    // 2) Data containers so we can bind to Reason's chart w/o recompiling code so we can bind to Reason's chart w/o recompiling code
    [Serializable]
    public sealed class ChannelStripMap
    {
        [Range(0,127)] public int levelCC = 7;     // Placeholder. Fill from Reason chart.
        [Range(0,127)] public int panCC   = 10;    // Placeholder. Fill from Reason chart.
        [Range(0,127)] public int muteCC  = 120;   // Often CC for All Sound Off/etc., use device-specific mute if available.
        [Range(0,127)] public int soloCC  = 121;   // Placeholder. Replace with true mapping.
    }

    [CreateAssetMenu(menuName = "Reason MIDI/ReasonMixerMap")]
    public sealed class ReasonMixerMap : ScriptableObject
    {
        [Tooltip("MIDI channel used for mixer commands")] public int channel = 0;
        [Header("Master")]
        [Range(0,127)] public int masterLevelCC = 7; // Placeholder. Use Reason chart value if different.
        [Header("Channels (1-16)")]
        public ChannelStripMap[] strips = new ChannelStripMap[16];

        private void OnValidate()
        {
            if (strips == null || strips.Length != 16)
            {
                strips = new ChannelStripMap[16];
                for (int i = 0; i < strips.Length; i++) strips[i] = new ChannelStripMap();
            }
            channel = Mathf.Clamp(channel, 0, 15);
        }
    }

    // 3) Mixer mapper with ergonomic methods (float 0..1, pan -1..1, booleans)
    public sealed class ReasonMixerMapper
    {
        private readonly IMidiSender _sender;
        private readonly ReasonMixerMap _map;

        public ReasonMixerMapper(IMidiSender sender, ReasonMixerMap map)
        {
            _sender = sender;
            _map = map;
        }

        public void SetMasterLevel01(float level01)
        {
            MidiHelpers.SendCC(_sender, _map.channel, _map.masterLevelCC, MidiHelpers.Clamp7(level01));
        }

        public void SetChannelLevel01(int channelIndex1Based, float level01)
        {
            int i = Mathf.Clamp(channelIndex1Based - 1, 0, _map.strips.Length - 1);
            MidiHelpers.SendCC(_sender, _map.channel, _map.strips[i].levelCC, MidiHelpers.Clamp7(level01));
        }

        // pan: -1 (L) .. +1 (R) -> 0..127
        public void SetChannelPan(int channelIndex1Based, float panNeg1to1)
        {
            int i = Mathf.Clamp(channelIndex1Based - 1, 0, _map.strips.Length - 1);
            float v01 = Mathf.InverseLerp(-1f, 1f, Mathf.Clamp(panNeg1to1, -1f, 1f));
            MidiHelpers.SendCC(_sender, _map.channel, _map.strips[i].panCC, MidiHelpers.Clamp7(v01));
        }

        public void SetChannelMute(int channelIndex1Based, bool mute)
        {
            int i = Mathf.Clamp(channelIndex1Based - 1, 0, _map.strips.Length - 1);
            MidiHelpers.SendCC(_sender, _map.channel, _map.strips[i].muteCC, mute ? 127 : 0);
        }

        public void SetChannelSolo(int channelIndex1Based, bool solo)
        {
            int i = Mathf.Clamp(channelIndex1Based - 1, 0, _map.strips.Length - 1);
            MidiHelpers.SendCC(_sender, _map.channel, _map.strips[i].soloCC, solo ? 127 : 0);
        }
    }

    // 4) Generic parameter address (CC or NRPN), used by device mappers (e.g., Europa)
    public enum ParamKind { CC7, NRPN14, CC14Pair }

    [Serializable]
    public struct ParamAddress
    {
        public ParamKind kind;
        [Range(0,127)] public int a; // CC number or NRPN MSB or CC MSB (coarse)
        [Range(0,127)] public int b; // unused (CC7) or NRPN LSB or CC LSB (fine)
    }

    [Serializable]
    public struct ParamSpec
    {
        public string name;
        public ParamAddress address;
        public float min;   // device domain min (e.g., -100)
        public float max;   // device domain max (e.g., +100)
        public bool bipolar; // convenience for UIs

        public int ToMidi(float value, out float clamped01)
        {
            float v = Mathf.Clamp(value, Mathf.Min(min,max), Mathf.Max(min,max));
            float v01 = Mathf.InverseLerp(min, max, v);
            clamped01 = v01;
            return MidiHelpers.Clamp7(v01);
        }
    }

    [CreateAssetMenu(menuName = "Reason MIDI/Europa Map")]
    public class ReasonEuropaMap : ScriptableObject
    {
        [Tooltip("MIDI channel used for Europa parameters")] public int channel = 0;

        [Header("Engine 1 (Oscillator)")]
        public ParamSpec eng1Type;            // Spectral/Classic/Wavetable selector (discrete)
        public ParamSpec eng1Position;        // Wavetable Position / Spectral Index
        public ParamSpec eng1Harmonics;       // Spectral Harmonics / Partial Count
        public ParamSpec eng1Shift;           // Spectral Shift
        public ParamSpec eng1Modify;          // Engine-specific Modify
        public ParamSpec eng1Motion;          // Motion/Scan amount
        public ParamSpec eng1Octave;          // -2..+2
        public ParamSpec eng1Semitone;        // -12..+12
        public ParamSpec eng1Fine;            // cents
        public ParamSpec eng1KeyTrack;        // 0..1
        public ParamSpec eng1Level;           // 0..1

        [Header("Engine 2 (Oscillator)")]
        public ParamSpec eng2Type;
        public ParamSpec eng2Position;
        public ParamSpec eng2Harmonics;
        public ParamSpec eng2Shift;
        public ParamSpec eng2Modify;
        public ParamSpec eng2Motion;
        public ParamSpec eng2Octave;
        public ParamSpec eng2Semitone;
        public ParamSpec eng2Fine;
        public ParamSpec eng2KeyTrack;
        public ParamSpec eng2Level;

        [Header("Engine 3 (Oscillator)")]
        public ParamSpec eng3Type;
        public ParamSpec eng3Position;
        public ParamSpec eng3Harmonics;
        public ParamSpec eng3Shift;
        public ParamSpec eng3Modify;
        public ParamSpec eng3Motion;
        public ParamSpec eng3Octave;
        public ParamSpec eng3Semitone;
        public ParamSpec eng3Fine;
        public ParamSpec eng3KeyTrack;
        public ParamSpec eng3Level;

        [Header("Mixer / Unison")]
        public ParamSpec mixEng12Balance;     // blend Eng1-2
        public ParamSpec mixEng3Level;
        public ParamSpec unisonVoices;        // discrete voices count
        public ParamSpec unisonDetune;
        public ParamSpec unisonSpread;

        [Header("Filter")]
        public ParamSpec filterType;          // discrete model
        public ParamSpec filterFreq;          // Hz or normalized
        public ParamSpec filterRes;           // Q
        public ParamSpec filterDrive;
        public ParamSpec filterKeyTrack;
        public ParamSpec filterEnvAmt;

        [Header("Amp & Mod Envelopes")]
        public ParamSpec ampAtk; public ParamSpec ampDec; public ParamSpec ampSus; public ParamSpec ampRel;
        public ParamSpec modAtk; public ParamSpec modDec; public ParamSpec modSus; public ParamSpec modRel;

        [Header("LFOs")]
        public ParamSpec lfo1Rate; public ParamSpec lfo1Amt; public ParamSpec lfo1Shape; public ParamSpec lfo1Sync;
        public ParamSpec lfo2Rate; public ParamSpec lfo2Amt; public ParamSpec lfo2Shape; public ParamSpec lfo2Sync;

        [Header("Modifier / FX")]
        public ParamSpec shaperDrive; public ParamSpec shaperType; // if exposed via CC
        public ParamSpec effect1; public ParamSpec effect2;        // generic slots

        private void OnValidate() => channel = Mathf.Clamp(channel, 0, 15);
    }

    public sealed class ReasonEuropaMapper
    {
        private readonly IMidiSender _sender;
        private readonly ReasonEuropaMap _map;

        public ReasonEuropaMapper(IMidiSender sender, ReasonEuropaMap map)
        {
            _sender = sender;
            _map = map;
        }
    
        private void Send(ParamSpec spec, float deviceValue)
        {
            switch (spec.address.kind)
            {
                case ParamKind.CC7:
                {
                    int val = spec.ToMidi(deviceValue, out float v01);
                    MidiHelpers.SendCC(_sender, _map.channel, spec.address.a, val);
                    break;
                }
                case ParamKind.CC14Pair:
                {
                    spec.ToMidi(deviceValue, out float v01);
                    MidiHelpers.SendCC14Pair(_sender, _map.channel, spec.address.a, spec.address.b, v01);
                    break;
                }
                case ParamKind.NRPN14:
                {
                    spec.ToMidi(deviceValue, out float v01);
                    MidiHelpers.SendNRPN14(_sender, _map.channel, spec.address.a, spec.address.b, v01);
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        // Sugars (a representative subset)
        public void SetEngine1Position(float v)   => Send(_map.eng1Position, v);
        public void SetEngine1Harmonics(float v)  => Send(_map.eng1Harmonics, v);
        public void SetEngine1Shift(float v)      => Send(_map.eng1Shift, v);
        public void SetEngine1Modify(float v)     => Send(_map.eng1Modify, v);
        public void SetEngine1Motion(float v)     => Send(_map.eng1Motion, v);
        public void SetEngine1Level(float v01)    => Send(_map.eng1Level, v01);
        public void SetFilterFreq(float hzOr01) => Send(_map.filterFreq, hzOr01);
        public void SetFilterRes(float qOr01)     => Send(_map.filterRes, qOr01);
        public void SetFilterEnvAmt(float v)      => Send(_map.filterEnvAmt, v);
        
        
        public void SetAmpADSR(float a, float d, float s, float r)
        { Send(_map.ampAtk,a); Send(_map.ampDec,d); Send(_map.ampSus,s); Send(_map.ampRel,r); }

        public void SetLFO1(float rate, float amt)
        { Send(_map.lfo1Rate, rate); Send(_map.lfo1Amt, amt); }
    }
   


    // 5) Minimal example of an RtMidi-backed sender (replace with your actual wrapper)
    public sealed class RtMidiSender : IMidiSender
    {
        private readonly System.Func<byte, byte, byte, bool> _sendShort; // status, d1, d2
        private readonly System.Action<byte[]> _sendSysex;

        public RtMidiSender(Func<byte, byte, byte, bool> sendShort, Action<byte[]> sendSysex)
        {
            _sendShort = sendShort;
            _sendSysex = sendSysex;
        }

        public void SendShort(byte status, byte data1, byte data2)
        {
            _sendShort(status, data1, data2);
        }

        public void SendSysex(byte[] data) => _sendSysex(data);
    }
}

// === Prefilled Europa parameter map (suggested CCs) =====================
// Notes:
// - Reason’s official chart does NOT define fixed per-device CCs for Europa;
//   you Remote-Override these. We prefill with *sensible, non-conflicting* CCs.
// - For 14-bit control, we use CC pairs (MSB=a, LSB=b=a+32). Avoid 120–127.
// - Global/"classic" conventions used where possible:
//     Cutoff=CC74, Resonance=CC71, Attack=CC73, Release=CC72, Decay=CC75.
// - Sustain has no GM standard; we use CC70.
// - Feel free to change numbers to match your template.
// - Ranges are device-domain suggestions; adjust to taste. 

public static class EuropaPrefill
{
    public static void Fill(ReasonEuropaMap map)
    {
        int ch = Mathf.Clamp(map.channel, 0, 15);

        // --- Engine 1 --- (MSB/LSB pairs in 80–87 / 112–119 block for heads-up room)
        map.eng1Type       = Discrete("E1 Type",    CC7(81));            // selector via coarse CC; adjust as needed
        map.eng1Position   = Pair01("E1 Position",  80);                 // 0..1 (wavetable/spectral index)
        map.eng1Harmonics  = Pair01("E1 Harmonics", 81);                 // 0..1
        map.eng1Shift      = Pair01("E1 Shift",     82);
        map.eng1Modify     = Pair01("E1 Modify",    83);
        map.eng1Motion     = Pair01("E1 Motion",    84);
        map.eng1Octave     = PairRange("E1 Octave", 85, -2, 2);
        map.eng1Semitone   = PairRange("E1 Semitone",86, -12, 12);
        map.eng1Fine       = PairRange("E1 Fine Cents",87, -100, 100, true);
        map.eng1KeyTrack   = Pair01("E1 KeyTrack",  80); // reuse or reassign if you prefer
        map.eng1Level      = Pair01("E1 Level",     81);

        // --- Engine 2 --- (shift to 70–79 / 102–111 to leverage GM-ish block)
        map.eng2Type       = Discrete("E2 Type",     CC7(71));
        map.eng2Position   = Pair01("E2 Position",   76);
        map.eng2Harmonics  = Pair01("E2 Harmonics",  77);
        map.eng2Shift      = Pair01("E2 Shift",      78);
        map.eng2Modify     = Pair01("E2 Modify",     79);
        map.eng2Motion     = Pair01("E2 Motion",     70);
        map.eng2Octave     = PairRange("E2 Octave",  71, -2, 2);
        map.eng2Semitone   = PairRange("E2 Semitone",72, -12, 12);
        map.eng2Fine       = PairRange("E2 Fine Cents",73, -100, 100, true);
        map.eng2KeyTrack   = Pair01("E2 KeyTrack",   74);
        map.eng2Level      = Pair01("E2 Level",      75);

        // --- Engine 3 --- (use 60–67 / 92–99)
        map.eng3Type       = Discrete("E3 Type",     CC7(61));
        map.eng3Position   = Pair01("E3 Position",   60);
        map.eng3Harmonics  = Pair01("E3 Harmonics",  61);
        map.eng3Shift      = Pair01("E3 Shift",      62);
        map.eng3Modify     = Pair01("E3 Modify",     63);
        map.eng3Motion     = Pair01("E3 Motion",     64);
        map.eng3Octave     = PairRange("E3 Octave",  65, -2, 2);
        map.eng3Semitone   = PairRange("E3 Semitone",66, -12, 12);
        map.eng3Fine       = PairRange("E3 Fine Cents",67, -100, 100, true);
        map.eng3KeyTrack   = Pair01("E3 KeyTrack",   60);
        map.eng3Level      = Pair01("E3 Level",      61);

        // --- Mixer / Unison ---
        map.mixEng12Balance = PairRange("Mix E1-E2 Balance", 82, -1, 1, true);
        map.mixEng3Level    = Pair01("Mix E3 Level",         83);
        map.unisonVoices    = Discrete("Unison Voices",       CC7(84)); // use integer rounding in UI
        map.unisonDetune    = Pair01("Unison Detune",         85);
        map.unisonSpread    = Pair01("Unison Spread",         86);

        // --- Filter --- (use GM-ish classics + 14-bit LSBs)
        map.filterType     = Discrete("Filter Type", CC7(68));
        map.filterFreq     = Pair01("Filter Cutoff", 74); // 74/106
        map.filterRes      = Pair01("Filter Resonance", 71); // 71/103
        map.filterDrive    = Pair01("Filter Drive",  69);
        map.filterKeyTrack = Pair01("Filter KeyTrack",70);
        map.filterEnvAmt   = PairRange("Filter Env Amt",72, -1, 1, true);

        // --- Amp & Mod Envelopes --- (ADSR first priority)
        map.ampAtk = PairSeconds("Amp Attack (s)",  73, 0.0f, 10.0f); // 73/105
        map.ampDec = PairSeconds("Amp Decay (s)",   75, 0.0f, 10.0f); // 75/107
        map.ampSus = Pair01("Amp Sustain",         70);               // 70/102 (chosen)
        map.ampRel = PairSeconds("Amp Release (s)", 72, 0.0f, 10.0f); // 72/104

        map.modAtk = PairSeconds("Mod Attack (s)",  76, 0.0f, 10.0f);
        map.modDec = PairSeconds("Mod Decay (s)",   77, 0.0f, 10.0f);
        map.modSus = Pair01("Mod Sustain",         78);
        map.modRel = PairSeconds("Mod Release (s)", 79, 0.0f, 10.0f);

        // --- LFOs ---
        map.lfo1Rate  = Pair01("LFO1 Rate",  76);
        map.lfo1Amt   = Pair01("LFO1 Amount",78);
        map.lfo1Shape = Discrete("LFO1 Shape", CC7(77));
        map.lfo1Sync  = Discrete("LFO1 Sync",  CC7(79));

        map.lfo2Rate  = Pair01("LFO2 Rate",  66);
        map.lfo2Amt   = Pair01("LFO2 Amount",67);
        map.lfo2Shape = Discrete("LFO2 Shape", CC7(65));
        map.lfo2Sync  = Discrete("LFO2 Sync",  CC7(64));

        // --- Modifier / FX (optional)
        map.shaperDrive = Pair01("Shaper Drive", 81);
        map.shaperType  = Discrete("Shaper Type", CC7(82));
        map.effect1     = Pair01("FX1", 83);
        map.effect2     = Pair01("FX2", 84);
    }

    // ---- helpers to construct ParamSpec ----
    static ParamSpec Pair01(string name, int ccMsb)
        => new ParamSpec { name = name, address = CC14(ccMsb), min = 0f, max = 1f, bipolar = false };

    static ParamSpec PairRange(string name, int ccMsb, float min, float max, bool bipolar=false)
        => new ParamSpec { name = name, address = CC14(ccMsb), min = min, max = max, bipolar = bipolar };

    static ParamSpec PairSeconds(string name, int ccMsb, float min, float max)
        => new ParamSpec { name = name, address = CC14(ccMsb), min = min, max = max, bipolar = false };

    static ParamSpec Discrete(string name, ParamAddress addr)
        => new ParamSpec { name = name, address = addr, min = 0, max = 127, bipolar = false };

    static ParamAddress CC14(int msb)
        => new ParamAddress { kind = ParamKind.CC14Pair, a = msb, b = msb + 32 };

    static ParamAddress CC7(int cc)
        => new ParamAddress { kind = ParamKind.CC7, a = cc, b = 0 };
}
// =====================================================================


// ================= Europa Prefilled ScriptableObject + Creator ================


namespace ReasonMidi
{
    [CreateAssetMenu(menuName = "Reason MIDI/Europa Preset (Prefilled)", fileName = "Europa_Prefilled")]
    public sealed class ReasonEuropaPreset_Default : ReasonEuropaMap
    {
        [Tooltip("If true, this preset auto-fills suggested CC/14-bit pairs on load.")]
        public bool autoFillOnEnable = true;

        [SerializeField] private bool _initialized;

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (autoFillOnEnable && !_initialized)
            {
                EuropaPrefill.Fill(this);
                _initialized = true;
                // Mark dirty so values persist when created in editor
                EditorUtility.SetDirty(this);
            }
#endif
        }

        public void Refill()
        {
            EuropaPrefill.Fill(this);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
            _initialized = true;
        }
    }

#if UNITY_EDITOR
    public static class EuropaPresetCreator
    {
        [MenuItem("Assets/Create/Reason MIDI/Europa Prefilled Map", priority = 11)]
        public static void CreatePrefilledEuropaMap()
        {
            var asset = ScriptableObject.CreateInstance<ReasonEuropaPreset_Default>();
            asset.channel = 0; // MIDI channel 1 by default
            asset.Refill();

            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Europa_Prefilled.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
    }
#endif
}
// ===========================================================================
