// Reason CC Property API — ergonomic properties that auto-send CCs on set
// Usage:
//   var Subtractor = new ReasonMidi.SubtractorDevice(outDev, 0);  // ch1
//   Subtractor.ModWheel = 5;     // sends CC1 value 5
//   Subtractor.ModWheel += 1;    // increments and re-sends
//   var Thor = new ReasonMidi.ThorDevice(outDev, 0);
//   Thor.LFO2Sync = true;        // sends CC109 value 127
//
// Notes:
// - Backing fields store the last value you set (devices don’t report CC state).
// - All setters clamp to 0..127 and send immediately via RtMidi.MidiOut.
// - Bool setters send 0 (off) or 127 (on) per MIDI switch convention.
// - "Enum(unknown)" style parameters are exposed as int 0..127.
//
// If your RtMidi wrapper exposes SendMessage(ReadOnlySpan<byte>), the byte[] overload below
// will still work because arrays implicitly convert to ReadOnlySpan<byte>.

using System;
using RtMidi;
using UnityEngine;
// Your wrapper namespace

namespace ReasonMidi
{
    public abstract class CcDevice
    {
        protected readonly MidiOut Dev;
        protected int Channel; // 0..15 (MIDI Ch1 = 0)

        protected CcDevice(MidiOut dev, int channel)
        {
            Dev = dev ?? throw new ArgumentNullException(nameof(dev));
            if (channel < 0 || channel > 15) throw new ArgumentOutOfRangeException(nameof(channel));
            Channel = channel;
        }

        protected static int Clamp7(int v) => v < 0 ? 0 : (v > 127 ? 127 : v);

        protected void SendCC(int cc, int value)
        {
            if (cc < 0 || cc > 127) throw new ArgumentOutOfRangeException(nameof(cc));
            var v = Clamp7(value);
            byte status = (byte)(0xB0 + Channel);
            byte[] msg = { status, (byte)cc, (byte)v };
            Dev.SendMessage(msg);
        }

        // Backing-field helpers ensure "+=" works naturally.
        protected int Set(ref int field, int cc, int value)
        {
            field = Clamp7(value);
            SendCC(cc, field);
            return field;
        }

        protected bool Set(ref bool field, int cc, bool on)
        {
            field = on;
            SendCC(cc, on ? 127 : 0);
            return field;
        }

        // Convenience in case you want floats (0..1) — optional.
        protected float Set01(ref int field, int cc, float value01)
        {
            int v = Mathf.RoundToInt(Mathf.Clamp01(value01) * 127f);
            field = v;
            SendCC(cc, v);
            return v / 127f;
        }

        public void SetChannel(int channel)
        {
            if (channel < 0 || channel > 15) throw new ArgumentOutOfRangeException(nameof(channel));
            Channel = channel;
        }

        // Raw passthrough if needed
        public void RawCC(int cc, int value) => SendCC(cc, value);
    }

    public enum Instrument
    {
        ID8, 
        Subtractor, 
        Thor, 
        Malstrom
    }
    
    
    // ========================= Subtractor =========================
    public sealed class SubtractorDevice : CcDevice
    {
        public SubtractorDevice(MidiOut dev, int channel) : base(dev, channel) {}

        int _modWheel, _osc12Level, _portamento, _masterLevel, _mix12, _ampEnvDecay,
            _ampPan, _ampEnvSustain, _chorusMix, _fEnvAtk, _fEnvDec, _fEnvSus, _fEnvRel,
            _fEnvAmt, _osc1Wave, _osc1Oct, _osc1Semi, _osc1Fine, _osc1Type, _osc1Kbd,
            _lfo1Rate, _lfo1Amt, _lfo1Wave, _lfo1Dest, _mwToFFreq, _mwToFRes, _mwToLfo1,
            _mwToFM, _mwToPhase, _bendRange, _extFFreq, _extLfo1, _extAmp, _extSelect,
            _extFM, _velAmp, _velFEnvAmt, _velFDec, _velAAttack, _velFM, _velMEnv,
            _velPhase, _velFilt2, _velMix, _osc2Wave, _osc2Oct, _osc2Semi, _osc2Fine,
            _osc2PhaseMode, _osc2PhaseDiff, _oscMix, _fmAmt, _ringAmt, _lfo2Rate,
            _lfo2Amt, _lfo2Delay, _lfo2Dest, _lfo2Kbd, _osc2Kbd, _mod2Breath, _mod2Expr;
        bool _fEnvInvert, _lfoSync, _lfo1TempoSync;

        /// <summary>CC1 — Performance mod source.</summary>
        public int ModWheel { get => _modWheel; set => Set(ref _modWheel, 1, value); }
        /// <summary>CC2 — Second mod if source=Breath.</summary>
        public int Mod2Breath { get => _mod2Breath; set => Set(ref _mod2Breath, 2, value); }
        /// <summary>CC11 — Second mod if source=Expression.</summary>
        public int Mod2Expression { get => _mod2Expr; set => Set(ref _mod2Expr, 11, value); }

        public int Osc12Level { get => _osc12Level; set => Set(ref _osc12Level, 4, value); }
        public int Portamento { get => _portamento; set => Set(ref _portamento, 5, value); }
        public int MasterLevel { get => _masterLevel; set => Set(ref _masterLevel, 7, value); }
        public int MixBalance12 { get => _mix12; set => Set(ref _mix12, 8, value); }
        public int AmpEnvDecay { get => _ampEnvDecay; set => Set(ref _ampEnvDecay, 9, value); }
        public int AmpPan { get => _ampPan; set => Set(ref _ampPan, 10, value); }
        public int AmpEnvSustain { get => _ampEnvSustain; set => Set(ref _ampEnvSustain, 12, value); }
        public int ChorusMix { get => _chorusMix; set => Set(ref _chorusMix, 13, value); }
        public int FilterEnvAttack { get => _fEnvAtk; set => Set(ref _fEnvAtk, 14, value); }
        public int FilterEnvDecay { get => _fEnvDec; set => Set(ref _fEnvDec, 15, value); }
        public int FilterEnvSustain { get => _fEnvSus; set => Set(ref _fEnvSus, 16, value); }
        public int FilterEnvRelease { get => _fEnvRel; set => Set(ref _fEnvRel, 17, value); }
        public int FilterEnvAmount { get => _fEnvAmt; set => Set(ref _fEnvAmt, 18, value); }
        public bool FilterEnvInvert { get => _fEnvInvert; set => Set(ref _fEnvInvert, 19, value); }

        public int Osc1Wave { get => _osc1Wave; set => Set(ref _osc1Wave, 20, value); }
        public int Osc1Octave { get => _osc1Oct; set => Set(ref _osc1Oct, 21, value); }
        public int Osc1Semitone { get => _osc1Semi; set => Set(ref _osc1Semi, 22, value); }
        public int Osc1Fine { get => _osc1Fine; set => Set(ref _osc1Fine, 23, value); }
        public int Osc1Type { get => _osc1Type; set => Set(ref _osc1Type, 24, value); }
        public int Osc1KeyboardTrack { get => _osc1Kbd; set => Set(ref _osc1Kbd, 25, value); }

        public int LFO1Rate { get => _lfo1Rate; set => Set(ref _lfo1Rate, 26, value); }
        public int LFO1Amount { get => _lfo1Amt; set => Set(ref _lfo1Amt, 27, value); }
        public int LFO1Wave { get => _lfo1Wave; set => Set(ref _lfo1Wave, 28, value); }
        public int LFO1Destination { get => _lfo1Dest; set => Set(ref _lfo1Dest, 29, value); }
        public bool LfoSync { get => _lfoSync; set => Set(ref _lfoSync, 30, value); }
        public bool LFO1TempoSync { get => _lfo1TempoSync; set => Set(ref _lfo1TempoSync, 31, value); }

        public int ModWheelToFilterFreq { get => _mwToFFreq; set => Set(ref _mwToFFreq, 33, value); }
        public int ModWheelToFilterRes { get => _mwToFRes; set => Set(ref _mwToFRes, 34, value); }
        public int ModWheelToLFO1 { get => _mwToLfo1; set => Set(ref _mwToLfo1, 35, value); }
        public int ModWheelToFM { get => _mwToFM; set => Set(ref _mwToFM, 36, value); }
        public int ModWheelToPhaseDiff { get => _mwToPhase; set => Set(ref _mwToPhase, 37, value); }

        public int PitchBendRange { get => _bendRange; set => Set(ref _bendRange, 39, value); }
        public int ExternalToFilterFreq { get => _extFFreq; set => Set(ref _extFFreq, 40, value); }
        public int ExternalToLFO1 { get => _extLfo1; set => Set(ref _extLfo1, 41, value); }
        public int ExternalToAmp { get => _extAmp; set => Set(ref _extAmp, 42, value); }
        public int ExternalModSelect { get => _extSelect; set => Set(ref _extSelect, 43, value); }
        public int ExternalToFM { get => _extFM; set => Set(ref _extFM, 44, value); }

        public int VelocityToAmp { get => _velAmp; set => Set(ref _velAmp, 45, value); }
        public int VelocityToFilterEnvAmt { get => _velFEnvAmt; set => Set(ref _velFEnvAmt, 46, value); }
        public int VelocityToFilterDecay { get => _velFDec; set => Set(ref _velFDec, 47, value); }
        public int VelocityToAmpAttack { get => _velAAttack; set => Set(ref _velAAttack, 48, value); }
        public int VelocityToFM { get => _velFM; set => Set(ref _velFM, 49, value); }
        public int VelocityToModEnv { get => _velMEnv; set => Set(ref _velMEnv, 50, value); }
        public int VelocityToPhase { get => _velPhase; set => Set(ref _velPhase, 51, value); }
        public int VelocityToFilter2Freq { get => _velFilt2; set => Set(ref _velFilt2, 52, value); }
        public int VelocityToMix { get => _velMix; set => Set(ref _velMix, 53, value); }

        public int Osc2Wave { get => _osc2Wave; set => Set(ref _osc2Wave, 95, value); }
        public int Osc2Octave { get => _osc2Oct; set => Set(ref _osc2Oct, 102, value); }
        public int Osc2Semitone { get => _osc2Semi; set => Set(ref _osc2Semi, 103, value); }
        public int Osc2Fine { get => _osc2Fine; set => Set(ref _osc2Fine, 104, value); }
        public int Osc2PhaseMode { get => _osc2PhaseMode; set => Set(ref _osc2PhaseMode, 105, value); }
        public int Osc2PhaseDiff { get => _osc2PhaseDiff; set => Set(ref _osc2PhaseDiff, 106, value); }
        public int OscMix { get => _oscMix; set => Set(ref _oscMix, 107, value); }
        public int FMAmount { get => _fmAmt; set => Set(ref _fmAmt, 108, value); }
        public int RingModAmount { get => _ringAmt; set => Set(ref _ringAmt, 109, value); }
        public int LFO2Rate { get => _lfo2Rate; set => Set(ref _lfo2Rate, 110, value); }
        public int LFO2Amount { get => _lfo2Amt; set => Set(ref _lfo2Amt, 111, value); }
        public int LFO2Delay { get => _lfo2Delay; set => Set(ref _lfo2Delay, 112, value); }
        public int LFO2Destination { get => _lfo2Dest; set => Set(ref _lfo2Dest, 113, value); }
        public int LFO2KeyboardTrack { get => _lfo2Kbd; set => Set(ref _lfo2Kbd, 114, value); }
        public int Osc2KeyboardTrack { get => _osc2Kbd; set => Set(ref _osc2Kbd, 115, value); }
    }

    // ========================= Thor =========================
    public sealed class ThorDevice : CcDevice
    {
        public ThorDevice(MidiOut dev, int channel) : base(dev, channel) {}

        int _mw,_port,_level,_ampDec,_delayMix,_fAtk,_fDec,_fSus,_fRel,_delayTime,_osc3Lvl,
            _osc1Mod,_osc1Oct,_osc1Semi,_osc1Fine,_oscEnvAmt,_delayFb,_lfo1Rate,_lfo1Delay,
            _lfo1Wave,_lfo1Kbd,_mb1,_mb2,_mb3,_mb4,_mb5,_mb6,_mb7,_mb8,_mb9,_mb10,_mb11,_mb12,_mb13,
            _bendRange,_osc3Mod,_osc3Oct,_osc3Semi,_osc3Fine,_osc3Type,_osc3SyncAmt,_filterAMode, _shaperMode,
            _shaperAmt,_osc1PhaseMode,_osc1PhaseDiff,_osc2Mod,_osc2Oct,_osc2Semi,_osc2Fine,
            _osc2SyncAmt,_seqRate,_amAmt,_lfo2Rate,_lfo2Wave,_lfo2Delay,_gDelay,_gAtk,_gHold,_gDec,_gSus,_gRel;
        bool _lfo1KeySync,_fAOn,_shaperOn,_routeBToB,_routeAToB,_routeAToShaper,_routeBToShaper,
             _btn1,_osc2On,_osc2SyncOn,_lfo2Sync,_lfo2KeySync;

        public int ModWheel { get => _mw; set => Set(ref _mw, 1, value); }
        public int Portamento { get => _port; set => Set(ref _port, 5, value); }
        public int MasterLevel { get => _level; set => Set(ref _level, 7, value); }
        public int AmpEnvDecay { get => _ampDec; set => Set(ref _ampDec, 9, value); }
        public int DelayMix { get => _delayMix; set => Set(ref _delayMix, 12, value); }
        public int FilterEnvAttack { get => _fAtk; set => Set(ref _fAtk, 14, value); }
        public int FilterEnvDecay { get => _fDec; set => Set(ref _fDec, 15, value); }
        public int FilterEnvSustain { get => _fSus; set => Set(ref _fSus, 16, value); }
        public int FilterEnvRelease { get => _fRel; set => Set(ref _fRel, 17, value); }
        public int DelayTime { get => _delayTime; set => Set(ref _delayTime, 18, value); }
        public int Osc3Level { get => _osc3Lvl; set => Set(ref _osc3Lvl, 19, value); }
        public int Osc1ModAmount { get => _osc1Mod; set => Set(ref _osc1Mod, 20, value); }
        public int Osc1Octave { get => _osc1Oct; set => Set(ref _osc1Oct, 21, value); }
        public int Osc1Semitone { get => _osc1Semi; set => Set(ref _osc1Semi, 22, value); }
        public int Osc1Fine { get => _osc1Fine; set => Set(ref _osc1Fine, 23, value); }
        public int OscEnvAmount { get => _oscEnvAmt; set => Set(ref _oscEnvAmt, 24, value); }
        public int DelayFeedback { get => _delayFb; set => Set(ref _delayFb, 25, value); }

        public int LFO1Rate { get => _lfo1Rate; set => Set(ref _lfo1Rate, 26, value); }
        public int LFO1Delay { get => _lfo1Delay; set => Set(ref _lfo1Delay, 27, value); }
        public int LFO1Wave { get => _lfo1Wave; set => Set(ref _lfo1Wave, 28, value); }
        public int LFO1KeyboardTrack { get => _lfo1Kbd; set => Set(ref _lfo1Kbd, 29, value); }
        public bool LFO1KeySync { get => _lfo1KeySync; set => Set(ref _lfo1KeySync, 30, value); }

        // Mod Matrix Buses 1..13 => CC33..47
        public int ModBus1Amount { get => _mb1; set => Set(ref _mb1, 33, value); }
        public int ModBus2Amount { get => _mb2; set => Set(ref _mb2, 34, value); }
        public int ModBus3Amount { get => _mb3; set => Set(ref _mb3, 35, value); }
        public int ModBus4Amount { get => _mb4; set => Set(ref _mb4, 36, value); }
        public int ModBus5Amount { get => _mb5; set => Set(ref _mb5, 37, value); }
        public int ModBus6Amount { get => _mb6; set => Set(ref _mb6, 38, value); }
        public int ModBus7Amount { get => _mb7; set => Set(ref _mb7, 39, value); }
        public int ModBus8Amount { get => _mb8; set => Set(ref _mb8, 40, value); }
        public int ModBus9Amount { get => _mb9; set => Set(ref _mb9, 41, value); }
        public int ModBus10Amount { get => _mb10; set => Set(ref _mb10, 42, value); }
        public int ModBus11Amount { get => _mb11; set => Set(ref _mb11, 43, value); }
        public int ModBus12Amount { get => _mb12; set => Set(ref _mb12, 44, value); }
        public int ModBus13Amount { get => _mb13; set => Set(ref _mb13, 45, value); }

        public int PitchBendRange { get => _bendRange; set => Set(ref _bendRange, 39, value); }

        public int Osc3ModAmount { get => _osc3Mod; set => Set(ref _osc3Mod, 48, value); }
        public int Osc3Octave { get => _osc3Oct; set => Set(ref _osc3Oct, 49, value); }
        public int Osc3Semitone { get => _osc3Semi; set => Set(ref _osc3Semi, 50, value); }
        public int Osc3Fine { get => _osc3Fine; set => Set(ref _osc3Fine, 51, value); }
        public int Osc3Type { get => _osc3Type; set => Set(ref _osc3Type, 52, value); }
        public int Osc3SyncAmount { get => _osc3SyncAmt; set => Set(ref _osc3SyncAmt, 53, value); }

        public bool FilterAOn { get => _fAOn; set => Set(ref _fAOn, 54, value); }
        public int FilterAMode { get => _filterAMode; set => Set(ref _filterAMode, 55, value); }
        public bool ShaperOn { get => _shaperOn; set => Set(ref _shaperOn, 56, value); }
        public int ShaperMode  { get => _shaperMode;  set => Set(ref _shaperMode, 57, value); }
        public int ShaperAmount { get => _shaperAmt; set => Set(ref _shaperAmt, 58, value); }

        public bool RouteOscBToFilterB { get => _routeBToB; set => Set(ref _routeBToB, 59, value); }
        public bool RouteOscAToFilterB { get => _routeAToB; set => Set(ref _routeAToB, 60, value); }
        public bool RouteOscAToShaper { get => _routeAToShaper; set => Set(ref _routeAToShaper, 61, value); }
        public bool RouteFilterBToShaper { get => _routeBToShaper; set => Set(ref _routeBToShaper, 62, value); }
        public bool Button1 { get => _btn1; set => Set(ref _btn1, 63, value); }

        public int Osc1PhaseMode { get => _osc1PhaseMode; set => Set(ref _osc1PhaseMode, 92, value); }
        public int Osc1PhaseDiff { get => _osc1PhaseDiff; set => Set(ref _osc1PhaseDiff, 93, value); }

        public bool Osc2On { get => _osc2On; set => Set(ref _osc2On, 94, value); }
        public int Osc2ModAmount { get => _osc2Mod; set => Set(ref _osc2Mod, 95, value); }
        public int Osc2Octave { get => _osc2Oct; set => Set(ref _osc2Oct, 102, value); }
        public int Osc2Semitone { get => _osc2Semi; set => Set(ref _osc2Semi, 103, value); }
        public int Osc2Fine { get => _osc2Fine; set => Set(ref _osc2Fine, 104, value); }
        public int Osc2SyncAmount { get => _osc2SyncAmt; set => Set(ref _osc2SyncAmt, 105, value); }
        public bool Osc2SyncOn { get => _osc2SyncOn; set => Set(ref _osc2SyncOn, 106, value); }
        public int SequencerRate { get => _seqRate; set => Set(ref _seqRate, 107, value); }
        public int AMAmount { get => _amAmt; set => Set(ref _amAmt, 108, value); }

        public bool LFO2Sync { get => _lfo2Sync; set => Set(ref _lfo2Sync, 109, value); }
        public int LFO2Rate { get => _lfo2Rate; set => Set(ref _lfo2Rate, 110, value); }
        public int LFO2Wave { get => _lfo2Wave; set => Set(ref _lfo2Wave, 111, value); }
        public int LFO2Delay { get => _lfo2Delay; set => Set(ref _lfo2Delay, 112, value); }
        public bool LFO2KeySync { get => _lfo2KeySync; set => Set(ref _lfo2KeySync, 113, value); }

        public int GlobalEnvDelay { get => _gDelay; set => Set(ref _gDelay, 114, value); }
        public int GlobalEnvAttack { get => _gAtk; set => Set(ref _gAtk, 115, value); }
        public int GlobalEnvHold { get => _gHold; set => Set(ref _gHold, 116, value); }
        public int GlobalEnvDecay { get => _gDec; set => Set(ref _gDec, 117, value); }
        public int GlobalEnvSustain { get => _gSus; set => Set(ref _gSus, 118, value); }
        public int GlobalEnvRelease { get => _gRel; set => Set(ref _gRel, 119, value); }
    }

    // ========================= Malström =========================
    public sealed class MalstromDevice : CcDevice
    {
        public MalstromDevice(MidiOut dev, int channel) : base(dev, channel) {}

        int _mw,_port,_level,_bDec,_bSus,_fAtk,_fDec,_fSus,_fRel,_fAmt,_bOct,_bSemi,_bCent,
            _modARate,_modAToPitch,_modACurve,_modATarget,_modAToIndex,
            _aGain,_aMotion,_aIndex,_aOct,_aSemi,_aCent,_spread,
            _modBRate,_modBToLevel,_modBToFilter,_modBToModA,_modBCurve,_modBTarget,_modBToMotion;
        bool _fInvert,_modAOn,_modAOnce,_aOn,_modBOn,_modBOnce;

        public int ModWheel { get => _mw; set => Set(ref _mw, 1, value); }
        public int Portamento { get => _port; set => Set(ref _port, 5, value); }
        public int MasterLevel { get => _level; set => Set(ref _level, 7, value); }

        public int OscBDecay { get => _bDec; set => Set(ref _bDec, 9, value); }
        public int OscBSustain { get => _bSus; set => Set(ref _bSus, 12, value); }
        public int FilterEnvAttack { get => _fAtk; set => Set(ref _fAtk, 14, value); }
        public int FilterEnvDecay { get => _fDec; set => Set(ref _fDec, 15, value); }
        public int FilterEnvSustain { get => _fSus; set => Set(ref _fSus, 16, value); }
        public int FilterEnvRelease { get => _fRel; set => Set(ref _fRel, 17, value); }
        public int FilterEnvAmount { get => _fAmt; set => Set(ref _fAmt, 18, value); }
        public bool FilterEnvInvert { get => _fInvert; set => Set(ref _fInvert, 19, value); }

        public int OscBOctave { get => _bOct; set => Set(ref _bOct, 21, value); }
        public int OscBSemi { get => _bSemi; set => Set(ref _bSemi, 22, value); }
        public int OscBCent { get => _bCent; set => Set(ref _bCent, 23, value); }

        public bool ModAOn { get => _modAOn; set => Set(ref _modAOn, 25, value); }
        public int ModARate { get => _modARate; set => Set(ref _modARate, 26, value); }
        public int ModAToPitch { get => _modAToPitch; set => Set(ref _modAToPitch, 27, value); }
        public int ModACurve { get => _modACurve; set => Set(ref _modACurve, 28, value); }
        public bool ModAOneShot { get => _modAOnce; set => Set(ref _modAOnce, 29, value); }
        public int ModATarget { get => _modATarget; set => Set(ref _modATarget, 30, value); }
        public int ModAToIndex { get => _modAToIndex; set => Set(ref _modAToIndex, 31, value); }

        public int OscAGain { get => _aGain; set => Set(ref _aGain, 91, value); }
        public int OscAMotion { get => _aMotion; set => Set(ref _aMotion, 92, value); }
        public int OscAIndex { get => _aIndex; set => Set(ref _aIndex, 93, value); }
        public bool OscAOn { get => _aOn; set => Set(ref _aOn, 95, value); }

        public int OscAOctave { get => _aOct; set => Set(ref _aOct, 102, value); }
        public int OscASemi { get => _aSemi; set => Set(ref _aSemi, 103, value); }
        public int OscACent { get => _aCent; set => Set(ref _aCent, 104, value); }
        public int SpreadAmount { get => _spread; set => Set(ref _spread, 105, value); }

        public int ModBRate { get => _modBRate; set => Set(ref _modBRate, 110, value); }
        public int ModBToLevel { get => _modBToLevel; set => Set(ref _modBToLevel, 111, value); }
        public int ModBToFilter { get => _modBToFilter; set => Set(ref _modBToFilter, 112, value); }
        public int ModBToModA { get => _modBToModA; set => Set(ref _modBToModA, 113, value); }
        public bool ModBOn { get => _modBOn; set => Set(ref _modBOn, 114, value); }
        public int ModBCurve { get => _modBCurve; set => Set(ref _modBCurve, 115, value); }
        public bool ModBOneShot { get => _modBOnce; set => Set(ref _modBOnce, 116, value); }
        public int ModBTarget { get => _modBTarget; set => Set(ref _modBTarget, 117, value); }
        public int ModBToMotion { get => _modBToMotion; set => Set(ref _modBToMotion, 118, value); }
    }

    // ========================= ID8 =========================
    public sealed class ID8Device : CcDevice
    {
        public ID8Device(MidiOut dev, int channel) : base(dev, channel) {}
        int _mw,_vol,_semi,_cent; bool _transposeOn;
        public int ModWheel { get => _mw; set => Set(ref _mw, 1, value); }
        public int Volume { get => _vol; set => Set(ref _vol, 7, value); }
        public bool TransposeOn { get => _transposeOn; set => Set(ref _transposeOn, 16, value); }
        public int Semitone { get => _semi; set => Set(ref _semi, 17, value); }
        public int Cent { get => _cent; set => Set(ref _cent, 18, value); }
    }

    // Optional: convenience holder if you prefer a single entry point
    public sealed class Reason
    {
        public SubtractorDevice Subtractor { get; }
        public ThorDevice Thor { get; }
        public MalstromDevice Malstrom { get; }
        public ID8Device ID8 { get; }
        public Reason(MidiOut dev, int channel)
        {
            Subtractor = new SubtractorDevice(dev, channel);
            Thor = new ThorDevice(dev, channel);
            Malstrom = new MalstromDevice(dev, channel);
            ID8 = new ID8Device(dev, channel);
        }
        public void SetChannel(int channel)
        {
            Subtractor.SetChannel(channel);
            Thor.SetChannel(channel);
            Malstrom.SetChannel(channel);
            ID8.SetChannel(channel);
        }
    }
}

/* ======================= Quick Usage =======================
using RtMidi;
using ReasonMidi;

// already opened to Reason
MidiOut outDev = null!;
var reason = new Reason(outDev, 0); // Channel 1

// Property-style control
reason.Subtractor.ModWheel = 5;
reason.Subtractor.ModWheel += 1; // re-sends CC1 with new value
reason.Thor.LFO2Sync = true;     // sends CC109 with 127

// Or construct individual devices
var Subtractor = new SubtractorDevice(outDev, 0);
Subtractor.FilterEnvAmount = 100;
Subtractor.FilterEnvInvert = true;
*/
