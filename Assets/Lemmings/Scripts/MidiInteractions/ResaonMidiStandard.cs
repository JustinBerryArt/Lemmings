
// A clear, fully XML‑documented Unity component for sending common, device‑agnostic
// MIDI performance messages (Pitch Bend, Channel/Poly Aftertouch, and standard
// Continuous Controllers such as Mod Wheel, Expression, Volume, Pan, Sustain, etc.).

using System;
using UnityEngine;

namespace ReasonMidi
{


    /// <summary>
    /// A clear, shareable Unity component for sending standard MIDI performance messages
    /// (Pitch Bend, Channel/Poly Aftertouch, and common CCs) on a chosen MIDI channel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This component focuses on the universally-supported subset of the MIDI spec so it works
    /// with most instruments without any device-specific mapping. Pair it with patches that
    /// respond to Mod Wheel (CC1), Expression (CC11), Sustain (CC64), Aftertouch, and Pitch Bend.
    /// </para>
    /// <para>
    /// To use: supply an <see cref="IMidiOutput"/> (via <see cref="Initialize"/> or by assigning
    /// <see cref="Output"/> in code), select the <see cref="MidiChannel"/>, and call the helper
    /// methods (e.g. <see cref="SendModWheel01"/>, <see cref="SendPitchBend01"/>, <see cref="SendSustain"/>).
    /// </para>
    /// </remarks>
    [AddComponentMenu("MIDI/Midi Standard Sender")]
    public sealed class ReasonMidiStandard : MonoBehaviour
    {
        [Header("MIDI Routing")]
        [Tooltip("MIDI channel to transmit on (1–16). Internally sent as 0–15 in the status nybble.")]
        [Range(1, 16)]
        public int MidiChannel = 1;

        /// <summary>
        /// The MIDI output implementation used to transmit bytes to your device/DAW.
        /// </summary>
        /// <remarks>
        /// Inject your RtMidi-backed output at runtime using <see cref="Initialize"/>.
        /// </remarks>
        public IMidiOutput Output { get; private set; }

        #region Initialization
        /// <summary>
        /// Initializes the sender with a concrete MIDI output.
        /// </summary>
        /// <param name="output">An object that implements <see cref="IMidiOutput"/>.</param>
        /// <param name="midiChannel">Optional MIDI channel (1–16). If omitted, the existing <see cref="MidiChannel"/> is used.</param>
        /// <example>
        /// <code>
        /// // Suppose you have an RtMidi wrapper with SendShort/SendSysex delegates
        /// var output = new RtMidiOutputAdapter((status,d1,d2) => Native.SendShort(status,d1,d2), sysex => Native.SendSysex(sysex));
        /// sender.Initialize(output, 1); // Channel 1
        /// sender.SendMod(0.5f);
        /// </code>
        /// </example>
        public void Initialize(IMidiOutput output, int? midiChannel = null)
        {
            Output = output ?? throw new ArgumentNullException(nameof(output));
            if (midiChannel.HasValue) MidiChannel = Mathf.Clamp(midiChannel.Value, 1, 16);
        }
        #endregion

        #region Utility (clamping and packing)
        /// <summary>
        /// Converts a normalized value in the range <c>0..1</c> to a 7-bit integer (0–127).
        /// </summary>
        /// <param name="v01">The normalized value. Values outside 0..1 are clamped.</param>
        /// <returns>An integer in the range 0..127 suitable for CC/aftertouch.</returns>
        public static int To7Bit(float v01)
        {
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(v01) * 127f), 0, 127);
        }

        /// <summary>
        /// Converts a normalized value in the range <c>0..1</c> to a 14-bit integer (0–16383).
        /// </summary>
        /// <param name="v01">The normalized value. Values outside 0..1 are clamped.</param>
        /// <returns>A 14-bit integer (0..16383) for Pitch Bend or fine CC pairs.</returns>
        public static int To14Bit(float v01)
        {
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(v01) * 16383f), 0, 16383);
        }

        /// <summary>
        /// Converts a pan value in the range <c>-1..+1</c> to a 7-bit value (0..127) where 0 = hard left, 64 = center, 127 = hard right.
        /// </summary>
        /// <param name="pan">The pan scalar (−1 = left, +1 = right).</param>
        public static int PanTo7Bit(float pan)
        {
            float v01 = Mathf.InverseLerp(-1f, 1f, Mathf.Clamp(pan, -1f, 1f));
            return To7Bit(v01);
        }

        /// <summary>
        /// Returns the channel nybble (0–15) from the current <see cref="MidiChannel"/> (1–16).
        /// </summary>
        private int ChannelNibble => Mathf.Clamp(MidiChannel - 1, 0, 15);

        /// <summary>
        /// Sends a raw 3-byte MIDI message via <see cref="Output"/>.
        /// </summary>
        /// <param name="status">Status byte (includes message type and channel nybble).</param>
        /// <param name="data1">Data byte 1.</param>
        /// <param name="data2">Data byte 2 (use 0 if unused).</param>
        protected void Send(byte status, byte data1, byte data2)
        {
            if (Output == null)
            {
                Debug.LogWarning("ReasonMidiStandard: No Output set. Call Initialize() with an IMidiOutput.");
                return;
            }
            Output.SendShort(status, data1, data2);
        }
        #endregion

        #region Pitch Bend (14-bit)
        /// <summary>
        /// Sends a 14-bit Pitch Bend message on <see cref="MidiChannel"/> using a normalized value.
        /// </summary>
        /// <param name="v01">Normalized bend amount 0..1, where 0.5 is center. 0 = full down, 1 = full up.</param>
        /// <remarks>
        /// MIDI encodes Pitch Bend as a 14-bit value split across two data bytes (LSB then MSB). Center is 8192.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Smooth upward bend from center
        /// sender.SendPitchBend(0.5f); // center
        /// sender.SendPitchBend(0.75f);
        /// sender.SendPitchBend(1.0f); // full up
        /// </code>
        /// </example>
        public void SendPitchBend01(float v01)
        {
            int bend = To14Bit(v01);
            byte lsb = (byte)(bend & 0x7F);
            byte msb = (byte)((bend >> 7) & 0x7F);
            byte status = (byte)(0xE0 | ChannelNibble);
            Send(status, lsb, msb);
        }

        /// <summary>
        /// Centers Pitch Bend (value = 8192) on <see cref="MidiChannel"/>.
        /// </summary>
        public void SendPitchBendCenter()
        {
            const int center = 8192;
            byte lsb = (byte)(center & 0x7F);
            byte msb = (byte)((center >> 7) & 0x7F);
            Send((byte)(0xE0 | ChannelNibble), lsb, msb);
        }
        #endregion

        #region Aftertouch
        /// <summary>
        /// Sends Channel Aftertouch (Channel Pressure) with a normalized value.
        /// </summary>
        /// <param name="pressure01">Normalized pressure 0..1.</param>
        /// <remarks>
        /// Channel Aftertouch is a single 7-bit value that applies to the whole channel. For per-note pressure,
        /// use <see cref="SendPolyAftertouch01(int, float)"/>.
        /// </remarks>
        public void SendChannelAftertouch01(float pressure01)
        {
            byte status = (byte)(0xD0 | ChannelNibble);
            byte val = (byte)To7Bit(pressure01);
            Send(status, val, 0);
        }

        /// <summary>
        /// Sends Polyphonic Aftertouch (per-note pressure) with a normalized value.
        /// </summary>
        /// <param name="note">The MIDI note number (0–127).</param>
        /// <param name="pressure01">Normalized pressure 0..1.</param>
        /// <remarks>
        /// Not all instruments respond to Poly Aftertouch. Many DAWs record it just like other channel voice messages.
        /// </remarks>
        public void SendPolyAftertouch01(int note, float pressure01)
        {
            byte status = (byte)(0xA0 | ChannelNibble);
            byte n = (byte)Mathf.Clamp(note, 0, 127);
            byte val = (byte)To7Bit(pressure01);
            Send(status, n, val);
        }
        #endregion

        #region Continuous Controllers (generic + convenience wrappers)
        /// <summary>
        /// Sends a 7-bit Continuous Controller (CC) value on <see cref="MidiChannel"/>.
        /// </summary>
        /// <param name="cc">The controller number (0–127).</param>
        /// <param name="value7">The controller value (0–127).</param>
        /// <example>
        /// <code>
        /// // Set Mod Wheel (CC1) to half
        /// sender.SendCC(1, 64);
        /// </code>
        /// </example>
        public void SendCC(int cc, int value7)
        {
            byte status = (byte)(0xB0 | ChannelNibble);
            byte c = (byte)Mathf.Clamp(cc, 0, 127);
            byte v = (byte)Mathf.Clamp(value7, 0, 127);
            Send(status, c, v);
        }

        /// <summary>
        /// Sends a normalized Continuous Controller value (0..1).
        /// </summary>
        /// <param name="cc">The controller number (0–127).</param>
        /// <param name="value01">A normalized value in 0..1.</param>
        public void SendCC01(int cc, float value01) => SendCC(cc, To7Bit(value01));

        /// <summary>
        /// Sets Modulation Wheel (CC1) using a normalized value.
        /// </summary>
        public void SendModWheel01(float value01) => SendCC01(1, value01);

        /// <summary>
        /// Sets Expression (CC11) using a normalized value.
        /// </summary>
        public void SendExpression01(float value01) => SendCC01(11, value01);

        /// <summary>
        /// Sets Breath (CC2) using a normalized value.
        /// </summary>
        public void SendBreath01(float value01) => SendCC01(2, value01);

        /// <summary>
        /// Sets Foot Controller (CC4) using a normalized value.
        /// </summary>
        public void SendFoot01(float value01) => SendCC01(4, value01);

        /// <summary>
        /// Sets Channel Volume (CC7) using a normalized value.
        /// </summary>
        public void SendVolume01(float value01) => SendCC01(7, value01);

        /// <summary>
        /// Sets Pan (CC10) using a bipolar value (−1..+1). 0 = center.
        /// </summary>
        /// <param name="pan">−1 = hard left, +1 = hard right.</param>
        public void SendPanNeg1ToPos1(float pan) => SendCC(10, PanTo7Bit(pan));

        /// <summary>
        /// Sends Sustain/Hold pedal (CC64) as a switch.
        /// </summary>
        /// <param name="down">True to engage (>=64), false to release (0).</param>
        public void SendSustain(bool down) => SendCC(64, down ? 127 : 0);

        /// <summary>
        /// Sends Sostenuto (CC66) as a switch.
        /// </summary>
        public void SendSostenuto(bool down) => SendCC(66, down ? 127 : 0);

        /// <summary>
        /// Sends Soft Pedal (CC67) as a switch.
        /// </summary>
        public void SendSoftPedal(bool down) => SendCC(67, down ? 127 : 0);

        /// <summary>
        /// Enables or disables Portamento (CC65) as a switch.
        /// </summary>
        public void SendPortamentoOn(bool on) => SendCC(65, on ? 127 : 0);

        /// <summary>
        /// Sets Portamento Time (CC5) using a normalized value.
        /// </summary>
        public void SendPortamentoTime01(float value01) => SendCC01(5, value01);
        #endregion

        #region Safety / Panic
        /// <summary>
        /// Sends "All Notes Off" (CC123) on the current channel.
        /// </summary>
        /// <remarks>
        /// Note: Some synths interpret this as "release all sustained notes" rather than an immediate hard mute.
        /// </remarks>
        public void SendAllNotesOff() => SendCC(123, 0);

        /// <summary>
        /// Sends "All Sound Off" (CC120) on the current channel (hard mute).
        /// </summary>
        public void SendAllSoundOff() => SendCC(120, 0);

        /// <summary>
        /// Sends "Reset All Controllers" (CC121) on the current channel.
        /// </summary>
        public void SendResetAllControllers() => SendCC(121, 0);
        #endregion

        #region Optional: Notes (handy for testing)
        /// <summary>
        /// Sends Note On with a 7-bit velocity.
        /// </summary>
        public void SendNoteOn(int note, int velocity)
        {
            byte status = (byte)(0x90 | ChannelNibble);
            byte n = (byte)Mathf.Clamp(note, 0, 127);
            byte v = (byte)Mathf.Clamp(velocity, 0, 127);
            Send(status, n, v);
        }

        /// <summary>
        /// Sends Note Off.
        /// </summary>
        public void SendNoteOff(int note)
        {
            byte status = (byte)(0x80 | ChannelNibble);
            byte n = (byte)Mathf.Clamp(note, 0, 127);
            Send(status, n, 0);
        }
        #endregion
    }
    
    /// <summary>
    /// Abstraction for a MIDI output that can send 3-byte "short" messages and optional SysEx.
    /// </summary>
    /// <remarks>
    /// Implement this interface by adapting your RtMidi C# wrapper. For example, you can wrap
    /// your native function <c>rtmidi_out_send_message</c> and expose it here.
    /// </remarks>
    public interface IMidiOutput
    {
        /// <summary>
        /// Sends a 3-byte MIDI message (a.k.a. a "short" message).
        /// </summary>
        /// <param name="status">The status byte (includes the message type and channel nybble).</param>
        /// <param name="data1">The first data byte (meaning depends on the status).</param>
        /// <param name="data2">The second data byte (meaning depends on the status). Use <c>0</c> if unused.</param>
        /// <returns>True if the message was accepted for sending; otherwise false.</returns>
        bool SendShort(byte status, byte data1, byte data2);

        /// <summary>
        /// Sends a System Exclusive (SysEx) message, if supported by your device chain.
        /// </summary>
        /// <param name="payload">A complete SysEx payload including the starting <c>0xF0</c> and ending <c>0xF7</c> bytes.</param>
        void SendSysex(byte[] payload);
    }
    
    /// <summary>
    /// Tiny adapter you can use to bridge into <see cref="ReasonMidiStandard"/> using delegates.
    /// </summary>
    /// <remarks>
    /// Wrap your native RtMidi calls inside the provided delegates.
    /// </remarks>
    public sealed class RtMidiOutputAdapter : IMidiOutput
    {
        private readonly Func<byte, byte, byte, bool> _sendShort;
        private readonly Action<byte[]> _sendSysex;

        /// <summary>
        /// Creates a new adapter.
        /// </summary>
        /// <param name="sendShort">Delegate that sends a 3-byte MIDI message.</param>
        /// <param name="sendSysex">Delegate that sends a SysEx payload (optional).</param>
        public RtMidiOutputAdapter(Func<byte, byte, byte, bool> sendShort, Action<byte[]> sendSysex = null)
        {
            _sendShort = sendShort ?? throw new ArgumentNullException(nameof(sendShort));
            _sendSysex = sendSysex ?? (_ => { });
        }

        /// <inheritdoc />
        public bool SendShort(byte status, byte data1, byte data2) => _sendShort(status, data1, data2);

        /// <inheritdoc />
        public void SendSysex(byte[] payload) => _sendSysex(payload);
    }
}
