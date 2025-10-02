# Reason SendCC Reference — Subtractor, Thor, Malström, ID8 (Extended)

**Assumptions**
- Example calls use a helper: `SendCC(outDev, channel /*0–15*/, cc /*0–127*/, value /*0–127*/)`.

- Channel shown below is `0` (MIDI Ch 1). Substitute your channel as needed.

- Min value = `0`, Max value = `127`, unless noted.

- Data type conventions per your request: `float(min..max)`, `int(min..max)`, `bool(off.on)`, `Enum(setting[value].setting[value]...)`.

- “Notes” adds quick guidance (polarity, center points, or usage tips). If an enum’s exact steps aren’t in the chart, it’s marked unknown.

  

------



## Generic MIDI Performance Controls (work across most Reason devices)

These are standard MIDI messages most instruments respond to without device-specific mapping. Route by **port** and **channel**; choose the parameter here by the **message/CC** you send.

| Control                    | MIDI message                    | Helper (from `ReasonMidiStandard`)              | Value range                  | Notes                                                        |
| -------------------------- | ------------------------------- | ----------------------------------------------- | ---------------------------- | ------------------------------------------------------------ |
| Pitch Bend                 | 0xE0 + channel (14‑bit LSB/MSB) | `SendPitchBend01(v01)`, `SendPitchBendCenter()` | 0..1 (center = 0.5)          | High‑resolution bend; range depends on patch/device setting. |
| Channel Aftertouch         | 0xD0 + channel                  | `SendChannelAftertouch01(pressure01)`           | 0..1                         | Also called Channel Pressure (applies to the whole channel). |
| Poly Aftertouch (per‑note) | 0xA0 + channel                  | `SendPolyAftertouch01(note, pressure01)`        | note: 0..127; pressure: 0..1 | Per‑note pressure if the device supports it.                 |
| Mod Wheel                  | CC1                             | `SendModWheel01(v01)`                           | 0..1                         | Typical vibrato/filter per patch.                            |
| Expression                 | CC11                            | `SendExpression01(v01)`                         | 0..1                         | Post‑volume musical expression.                              |
| Volume                     | CC7                             | `SendVolume01(v01)`                             | 0..1                         | Channel/device volume (post synth).                          |
| Pan                        | CC10                            | `SendPanNeg1ToPos1(pan)`                        | −1..+1                       | −1 = Left, +1 = Right; internal helper maps to 0..127.       |
| Breath                     | CC2                             | `SendBreath01(v01)`                             | 0..1                         | Often a second mod source on some patches.                   |
| Foot                       | CC4                             | `SendFoot01(v01)`                               | 0..1                         | Generic performance pedal control.                           |
| Portamento Time            | CC5                             | `SendPortamentoTime01(v01)`                     | 0..1                         | Glide time; higher = slower glide.                           |
| Portamento On/Off          | CC65                            | `SendPortamentoOn(on)`                          | bool                         | Toggles portamento where supported.                          |
| Sustain (Hold)             | CC64                            | `SendSustain(down)`                             | bool                         | 127=down, 0=up.                                              |
| Sostenuto                  | CC66                            | `SendSostenuto(down)`                           | bool                         | Holds only notes that were down when pressed.                |
| Soft Pedal                 | CC67                            | `SendSoftPedal(down)`                           | bool                         | Often reduces timbre/volume.                                 |
| All Notes Off              | CC123                           | `SendAllNotesOff()`                             | n/a                          | Sends CC123=0 on the channel.                                |
| All Sound Off              | CC120                           | `SendAllSoundOff()`                             | n/a                          | Hard mute; stops sound immediately.                          |
| Reset All Controllers      | CC121                           | `SendResetAllControllers()`                     | n/a                          | Resets controllers to defaults.                              |

**Routing reminder:** Destination = **Port** + **Channel**. Parameter = **Message type/CC**. Reason does not auto‑retarget to the “focused” device when you send raw MIDI.



------



## Subtractor — full CC map

| Instrument | Instrument setting | Setting description | SendCC (max) | SendCC (min) | Data type | Notes |
|---|---|---|---|---|---|---|
| Subtractor | Mod Wheel (CC1) | Performance/mod source routed by patch. | `SendCC(outDev, 0, 1, 127)` | `SendCC(outDev, 0, 1, 0)` | int(0..127) | Often vibrato/filter per patch. |
| Subtractor | Mod Wheel 2* (CC2) | Second mod source if set to **Breath**. | `SendCC(outDev, 0, 2, 127)` | `SendCC(outDev, 0, 2, 0)` | int(0..127) | Works only if source = Breath. |
| Subtractor | Osc 1+2 Level (CC4) | Changes combined osc output level. | `SendCC(outDev, 0, 4, 127)` | `SendCC(outDev, 0, 4, 0)` | int(0..127) | Use with Mix for balance. |
| Subtractor | Portamento (CC5) | Glide time between notes. | `SendCC(outDev, 0, 5, 127)` | `SendCC(outDev, 0, 5, 0)` | int(0..127) | Higher = slower glide. |
| Subtractor | Master Level (CC7) | Device output level. | `SendCC(outDev, 0, 7, 127)` | `SendCC(outDev, 0, 7, 0)` | int(0..127) | Post‑synth level. |
| Subtractor | Mixer 1–2 Balance (CC8) | Blend Osc1 ↔ Osc2. | `SendCC(outDev, 0, 8, 127)` | `SendCC(outDev, 0, 8, 0)` | int(0..127) | 0=Osc1, 127=Osc2. |
| Subtractor | Amp Env Decay (CC9) | Amp envelope decay time. | `SendCC(outDev, 0, 9, 127)` | `SendCC(outDev, 0, 9, 0)` | int(0..127) | Longer tails at higher values. |
| Subtractor | Amplifier Pan (CC10) | Stereo position. | `SendCC(outDev, 0, 10, 127)` | `SendCC(outDev, 0, 10, 0)` | int(0..127) | 0=L, 64=C, 127=R. |
| Subtractor | Mod Wheel 2** (CC11) | Second mod source if set to **Expression**. | `SendCC(outDev, 0, 11, 127)` | `SendCC(outDev, 0, 11, 0)` | int(0..127) | Works only if source = Expression. |
| Subtractor | Amp Env Sustain (CC12) | Amp envelope sustain level. | `SendCC(outDev, 0, 12, 127)` | `SendCC(outDev, 0, 12, 0)` | int(0..127) | |
| Subtractor | Chorus Dry/Wet (CC13) | Built‑in chorus mix. | `SendCC(outDev, 0, 13, 127)` | `SendCC(outDev, 0, 13, 0)` | int(0..127) | If chorus enabled. |
| Subtractor | Filter Env Attack (CC14) | Filter envelope attack time. | `SendCC(outDev, 0, 14, 127)` | `SendCC(outDev, 0, 14, 0)` | int(0..127) | |
| Subtractor | Filter Env Decay (CC15) | Filter envelope decay time. | `SendCC(outDev, 0, 15, 127)` | `SendCC(outDev, 0, 15, 0)` | int(0..127) | |
| Subtractor | Filter Env Sustain (CC16) | Filter envelope sustain level. | `SendCC(outDev, 0, 16, 127)` | `SendCC(outDev, 0, 16, 0)` | int(0..127) | |
| Subtractor | Filter Env Release (CC17) | Filter envelope release time. | `SendCC(outDev, 0, 17, 127)` | `SendCC(outDev, 0, 17, 0)` | int(0..127) | |
| Subtractor | Filter Env Amount (CC18) | Depth of filter env to cutoff. | `SendCC(outDev, 0, 18, 127)` | `SendCC(outDev, 0, 18, 0)` | int(0..127) | Positive polarity. |
| Subtractor | Filter Env Invert (CC19) | Invert filter envelope polarity. | `SendCC(outDev, 0, 19, 127)` | `SendCC(outDev, 0, 19, 0)` | bool(off.on) | 0–63 off; 64–127 on. |
| Subtractor | Osc1 Wave (CC20) | Select Osc1 waveform. | `SendCC(outDev, 0, 20, 127)` | `SendCC(outDev, 0, 20, 0)` | Enum(unknown) | Stepped. |
| Subtractor | Osc1 Octave (CC21) | Octave transpose for Osc1. | `SendCC(outDev, 0, 21, 127)` | `SendCC(outDev, 0, 21, 0)` | Enum(unknown) | Stepped. |
| Subtractor | Osc1 Semitone (CC22) | Semitone detune for Osc1. | `SendCC(outDev, 0, 22, 127)` | `SendCC(outDev, 0, 22, 0)` | int(0..127) | Mid ≈ default pitch (varies). |
| Subtractor | Osc1 Fine Tune (CC23) | Fine tune Osc1 in cents. | `SendCC(outDev, 0, 23, 127)` | `SendCC(outDev, 0, 23, 0)` | int(0..127) | |
| Subtractor | Osc1 Type (CC24) | Osc1 synthesis type/variant. | `SendCC(outDev, 0, 24, 127)` | `SendCC(outDev, 0, 24, 0)` | Enum(unknown) | Stepped. |
| Subtractor | Osc1 Kbd Track (CC25) | Keyboard tracking amount. | `SendCC(outDev, 0, 25, 127)` | `SendCC(outDev, 0, 25, 0)` | int(0..127) | |
| Subtractor | LFO1 Rate (CC26) | LFO1 speed. | `SendCC(outDev, 0, 26, 127)` | `SendCC(outDev, 0, 26, 0)` | int(0..127) | |
| Subtractor | LFO1 Amount (CC27) | LFO1 depth. | `SendCC(outDev, 0, 27, 127)` | `SendCC(outDev, 0, 27, 0)` | int(0..127) | |
| Subtractor | LFO1 Wave (CC28) | LFO1 shape. | `SendCC(outDev, 0, 28, 127)` | `SendCC(outDev, 0, 28, 0)` | Enum(unknown) | Stepped shapes. |
| Subtractor | LFO1 Destination (CC29) | LFO1 target parameter. | `SendCC(outDev, 0, 29, 127)` | `SendCC(outDev, 0, 29, 0)` | Enum(unknown) | Picks dest (cutoff, pitch, etc.). |
| Subtractor | LFO Sync Enable (CC30) | Sync LFO to tempo. | `SendCC(outDev, 0, 30, 127)` | `SendCC(outDev, 0, 30, 0)` | bool(off.on) | 64–127 on. |
| Subtractor | LFO1 Tempo Sync Enable (CC31) | Tempo‑sync behavior. | `SendCC(outDev, 0, 31, 127)` | `SendCC(outDev, 0, 31, 0)` | bool(off.on) | 64–127 on. |
| Subtractor | Filter Freq ModWheel Amt (CC33) | Amount of ModWheel → Filter Freq. | `SendCC(outDev, 0, 33, 127)` | `SendCC(outDev, 0, 33, 0)` | int(0..127) | CC 33 is LSB for CC1. |
| Subtractor | Filter Res ModWheel Amt (CC34) | Amount of ModWheel → Filter Res. | `SendCC(outDev, 0, 34, 127)` | `SendCC(outDev, 0, 34, 0)` | int(0..127) | |
| Subtractor | LFO1 ModWheel Amt (CC35) | Amount of ModWheel → LFO1. | `SendCC(outDev, 0, 35, 127)` | `SendCC(outDev, 0, 35, 0)` | int(0..127) | |
| Subtractor | FM ModWheel Amt (CC36) | Amount of ModWheel → FM. | `SendCC(outDev, 0, 36, 127)` | `SendCC(outDev, 0, 36, 0)` | int(0..127) | |
| Subtractor | PhaseDiff ModWheel Amt (CC37) | Amount of ModWheel → Phase Diff. | `SendCC(outDev, 0, 37, 127)` | `SendCC(outDev, 0, 37, 0)` | int(0..127) | |
| Subtractor | Pitch Bend Range (CC39) | Bend range (device units). | `SendCC(outDev, 0, 39, 127)` | `SendCC(outDev, 0, 39, 0)` | int(0..127) | Often ≈ semitones. |
| Subtractor | Filter Freq Ext Mod (CC40) | External mod depth → Filter Freq. | `SendCC(outDev, 0, 40, 127)` | `SendCC(outDev, 0, 40, 0)` | int(0..127) | Requires ext source. |
| Subtractor | LFO1 Ext Mod (CC41) | External mod depth → LFO1. | `SendCC(outDev, 0, 41, 127)` | `SendCC(outDev, 0, 41, 0)` | int(0..127) | |
| Subtractor | Amp Ext Mod (CC42) | External mod depth → Amp. | `SendCC(outDev, 0, 42, 127)` | `SendCC(outDev, 0, 42, 0)` | int(0..127) | |
| Subtractor | Ext Mod Select (CC43) | Select external mod source. | `SendCC(outDev, 0, 43, 127)` | `SendCC(outDev, 0, 43, 0)` | Enum(unknown) | Stepped source select. |
| Subtractor | FM Ext Mod (CC44) | External mod depth → FM. | `SendCC(outDev, 0, 44, 127)` | `SendCC(outDev, 0, 44, 0)` | int(0..127) | |
| Subtractor | Amp Vel Amount (CC45) | Velocity → Amp amount. | `SendCC(outDev, 0, 45, 127)` | `SendCC(outDev, 0, 45, 0)` | int(0..127) | |
| Subtractor | Filter Env Vel Amount (CC46) | Velocity → Filter Env amount. | `SendCC(outDev, 0, 46, 127)` | `SendCC(outDev, 0, 46, 0)` | int(0..127) | |
| Subtractor | Filter Decay Vel Amount (CC47) | Velocity → Filter Decay. | `SendCC(outDev, 0, 47, 127)` | `SendCC(outDev, 0, 47, 0)` | int(0..127) | |
| Subtractor | Amp Attack Vel Amount (CC48) | Velocity → Amp Attack. | `SendCC(outDev, 0, 48, 127)` | `SendCC(outDev, 0, 48, 0)` | int(0..127) | |
| Subtractor | FM Vel Amount (CC49) | Velocity → FM amount. | `SendCC(outDev, 0, 49, 127)` | `SendCC(outDev, 0, 49, 0)` | int(0..127) | |
| Subtractor | Mod Env Vel Amount (CC50) | Velocity → Mod Env. | `SendCC(outDev, 0, 50, 127)` | `SendCC(outDev, 0, 50, 0)` | int(0..127) | |
| Subtractor | Phase Vel Amount (CC51) | Velocity → Phase. | `SendCC(outDev, 0, 51, 127)` | `SendCC(outDev, 0, 51, 0)` | int(0..127) | |
| Subtractor | Filter2 Freq Vel Amount (CC52) | Velocity → 2nd Filter Freq. | `SendCC(outDev, 0, 52, 127)` | `SendCC(outDev, 0, 52, 0)` | int(0..127) | If patch uses Filter 2. |
| Subtractor | Mix Vel Amount (CC53) | Velocity → Osc mix. | `SendCC(outDev, 0, 53, 127)` | `SendCC(outDev, 0, 53, 0)` | int(0..127) | |
| Subtractor | Osc2 Wave (CC95) | Select Osc2 waveform. | `SendCC(outDev, 0, 95, 127)` | `SendCC(outDev, 0, 95, 0)` | Enum(unknown) | Stepped. |
| Subtractor | Osc2 Octave (CC102) | Octave transpose for Osc2. | `SendCC(outDev, 0, 102, 127)` | `SendCC(outDev, 0, 102, 0)` | Enum(unknown) | Stepped. |
| Subtractor | Osc2 Semitone (CC103) | Semitone detune for Osc2. | `SendCC(outDev, 0, 103, 127)` | `SendCC(outDev, 0, 103, 0)` | int(0..127) | |
| Subtractor | Osc2 Fine Tune (CC104) | Fine tune Osc2 in cents. | `SendCC(outDev, 0, 104, 127)` | `SendCC(outDev, 0, 104, 0)` | int(0..127) | |
| Subtractor | Osc2 Phase Mode (CC105) | Osc2 phase mode. | `SendCC(outDev, 0, 105, 127)` | `SendCC(outDev, 0, 105, 0)` | Enum(unknown) | Stepped. |
| Subtractor | Osc2 Phase Diff (CC106) | Osc2 phase difference. | `SendCC(outDev, 0, 106, 127)` | `SendCC(outDev, 0, 106, 0)` | int(0..127) | |
| Subtractor | Osc Mix (CC107) | Blend Osc1↔Osc2 (post levels). | `SendCC(outDev, 0, 107, 127)` | `SendCC(outDev, 0, 107, 0)` | int(0..127) | 0=Osc1, 127=Osc2. |
| Subtractor | FM Amount (CC108) | FM depth. | `SendCC(outDev, 0, 108, 127)` | `SendCC(outDev, 0, 108, 0)` | int(0..127) | |
| Subtractor | Ring Mod Amount (CC109) | Ring modulation depth. | `SendCC(outDev, 0, 109, 127)` | `SendCC(outDev, 0, 109, 0)` | int(0..127) | |
| Subtractor | LFO2 Rate (CC110) | LFO2 speed. | `SendCC(outDev, 0, 110, 127)` | `SendCC(outDev, 0, 110, 0)` | int(0..127) | |
| Subtractor | LFO2 Amount (CC111) | LFO2 depth. | `SendCC(outDev, 0, 111, 127)` | `SendCC(outDev, 0, 111, 0)` | int(0..127) | |
| Subtractor | LFO2 Delay (CC112) | LFO2 fade‑in. | `SendCC(outDev, 0, 112, 127)` | `SendCC(outDev, 0, 112, 0)` | int(0..127) | |
| Subtractor | LFO2 Destination (CC113) | LFO2 target parameter. | `SendCC(outDev, 0, 113, 127)` | `SendCC(outDev, 0, 113, 0)` | Enum(unknown) | |
| Subtractor | LFO2 Keyboard Track (CC114) | KBD tracking for LFO2. | `SendCC(outDev, 0, 114, 127)` | `SendCC(outDev, 0, 114, 0)` | int(0..127) | |
| Subtractor | Osc2 Keyboard Track (CC115) | KBD tracking for Osc2. | `SendCC(outDev, 0, 115, 127)` | `SendCC(outDev, 0, 115, 0)` | int(0..127) | |

*Footnotes: `*` works when the device’s Mod Wheel 2 source is set to Breath; `**` when set to Expression.

---

## Thor — full CC map

| Instrument | Instrument setting | Setting description | SendCC (max) | SendCC (min) | Data type | Notes |
|---|---|---|---|---|---|---|
| Thor | Mod Wheel (CC1) | Performance source routed via Mod Matrix. | `SendCC(outDev, 0, 1, 127)` | `SendCC(outDev, 0, 1, 0)` | int(0..127) | |
| Thor | Portamento (CC5) | Glide time. | `SendCC(outDev, 0, 5, 127)` | `SendCC(outDev, 0, 5, 0)` | int(0..127) | |
| Thor | Master Level (CC7) | Device output level. | `SendCC(outDev, 0, 7, 127)` | `SendCC(outDev, 0, 7, 0)` | int(0..127) | |
| Thor | Amp Env Decay (CC9) | Amplitude decay. | `SendCC(outDev, 0, 9, 127)` | `SendCC(outDev, 0, 9, 0)` | int(0..127) | |
| Thor | Delay Dry/Wet (CC12) | Built‑in delay mix. | `SendCC(outDev, 0, 12, 127)` | `SendCC(outDev, 0, 12, 0)` | int(0..127) | |
| Thor | Filter Env Attack (CC14) | Filter env attack. | `SendCC(outDev, 0, 14, 127)` | `SendCC(outDev, 0, 14, 0)` | int(0..127) | |
| Thor | Filter Env Decay (CC15) | Filter env decay. | `SendCC(outDev, 0, 15, 127)` | `SendCC(outDev, 0, 15, 0)` | int(0..127) | |
| Thor | Filter Env Sustain (CC16) | Filter env sustain level. | `SendCC(outDev, 0, 16, 127)` | `SendCC(outDev, 0, 16, 0)` | int(0..127) | |
| Thor | Filter Env Release (CC17) | Filter env release. | `SendCC(outDev, 0, 17, 127)` | `SendCC(outDev, 0, 17, 0)` | int(0..127) | |
| Thor | Delay Time (CC18) | Delay time. | `SendCC(outDev, 0, 18, 127)` | `SendCC(outDev, 0, 18, 0)` | int(0..127) | Tempo‑scaled. |
| Thor | Osc3 Level (CC19) | Mixer level of Osc3. | `SendCC(outDev, 0, 19, 127)` | `SendCC(outDev, 0, 19, 0)` | int(0..127) | |
| Thor | Osc1 Mod Amount (CC20) | Osc1 modulation depth. | `SendCC(outDev, 0, 20, 127)` | `SendCC(outDev, 0, 20, 0)` | int(0..127) | Depends on osc type. |
| Thor | Osc1 Octave (CC21) | Octave for Osc1. | `SendCC(outDev, 0, 21, 127)` | `SendCC(outDev, 0, 21, 0)` | Enum(unknown) | Stepped. |
| Thor | Osc1 Semitone (CC22) | Semitone for Osc1. | `SendCC(outDev, 0, 22, 127)` | `SendCC(outDev, 0, 22, 0)` | int(0..127) | |
| Thor | Osc1 Fine (CC23) | Fine tune Osc1. | `SendCC(outDev, 0, 23, 127)` | `SendCC(outDev, 0, 23, 0)` | int(0..127) | |
| Thor | Osc Env Amount (CC24) | Osc env depth to target. | `SendCC(outDev, 0, 24, 127)` | `SendCC(outDev, 0, 24, 0)` | int(0..127) | |
| Thor | Delay Feedback (CC25) | Delay feedback. | `SendCC(outDev, 0, 25, 127)` | `SendCC(outDev, 0, 25, 0)` | int(0..127) | |
| Thor | LFO1 Rate (CC26) | LFO1 speed. | `SendCC(outDev, 0, 26, 127)` | `SendCC(outDev, 0, 26, 0)` | int(0..127) | |
| Thor | LFO1 Delay (CC27) | LFO1 fade‑in. | `SendCC(outDev, 0, 27, 127)` | `SendCC(outDev, 0, 27, 0)` | int(0..127) | |
| Thor | LFO1 Wave (CC28) | LFO1 shape. | `SendCC(outDev, 0, 28, 127)` | `SendCC(outDev, 0, 28, 0)` | Enum(unknown) | Stepped shapes. |
| Thor | LFO1 Kbd Track (CC29) | Pitch‑follow amount. | `SendCC(outDev, 0, 29, 127)` | `SendCC(outDev, 0, 29, 0)` | int(0..127) | |
| Thor | LFO1 Key Sync (CC30) | Sync LFO1 on keypress. | `SendCC(outDev, 0, 30, 127)` | `SendCC(outDev, 0, 30, 0)` | bool(off.on) | 64–127 on. |
| Thor | Mod Bus 1–13 Amount (CC33–47) | Mod Matrix Bus depths. | `SendCC(outDev, 0, 47, 127)` | `SendCC(outDev, 0, 33, 0)` | int(0..127) | CC33=Bus1 … CC47=Bus13. |
| Thor | Pitch Bend Range (CC39) | Bend range (device units). | `SendCC(outDev, 0, 39, 127)` | `SendCC(outDev, 0, 39, 0)` | int(0..127) | Often ≈ semitones. |
| Thor | Osc3 Mod Amount (CC48) | Osc3 modulation depth. | `SendCC(outDev, 0, 48, 127)` | `SendCC(outDev, 0, 48, 0)` | int(0..127) | |
| Thor | Osc3 Octave (CC49) | Octave for Osc3. | `SendCC(outDev, 0, 49, 127)` | `SendCC(outDev, 0, 49, 0)` | Enum(unknown) | Stepped. |
| Thor | Osc3 Semitone (CC50) | Semitone for Osc3. | `SendCC(outDev, 0, 50, 127)` | `SendCC(outDev, 0, 50, 0)` | int(0..127) | |
| Thor | Osc3 Fine (CC51) | Fine tune Osc3. | `SendCC(outDev, 0, 51, 127)` | `SendCC(outDev, 0, 51, 0)` | int(0..127) | |
| Thor | Osc3 Type (CC52) | Osc3 algorithm/type. | `SendCC(outDev, 0, 52, 127)` | `SendCC(outDev, 0, 52, 0)` | Enum(unknown) | Stepped. |
| Thor | Osc3 Sync Amount (CC53) | Depth of Osc3 sync. | `SendCC(outDev, 0, 53, 127)` | `SendCC(outDev, 0, 53, 0)` | int(0..127) | |
| Thor | Filter A On/Off (CC54) | Toggle Filter A. | `SendCC(outDev, 0, 54, 127)` | `SendCC(outDev, 0, 54, 0)` | bool(off.on) | 64–127 on. |
| Thor | Filter A Mode (CC55) | Filter A type/mode. | `SendCC(outDev, 0, 55, 127)` | `SendCC(outDev, 0, 55, 0)` | Enum(unknown) | Stepped. |
| Thor | Shaper On/Off (CC56) | Toggle Shaper. | `SendCC(outDev, 0, 56, 127)` | `SendCC(outDev, 0, 56, 0)` | bool(off.on) | 64–127 on. |
| Thor | Shaper Mode (CC57) | Shaper algorithm. | `SendCC(outDev, 0, 57, 127)` | `SendCC(outDev, 0, 57, 0)` | Enum(unknown) | Stepped. |
| Thor | Shaper Amount (CC58) | Shaper drive/depth. | `SendCC(outDev, 0, 58, 127)` | `SendCC(outDev, 0, 58, 0)` | int(0..127) | |
| Thor | Route OscB→FilterB (CC59) | Route toggle. | `SendCC(outDev, 0, 59, 127)` | `SendCC(outDev, 0, 59, 0)` | bool(off.on) | 64–127 on. |
| Thor | Route OscA→FilterB (CC60) | Route toggle. | `SendCC(outDev, 0, 60, 127)` | `SendCC(outDev, 0, 60, 0)` | bool(off.on) | 64–127 on. |
| Thor | Route OscA→Shaper (CC61) | Route toggle. | `SendCC(outDev, 0, 61, 127)` | `SendCC(outDev, 0, 61, 0)` | bool(off.on) | 64–127 on. |
| Thor | Route FilterB→Shaper (CC62) | Route toggle. | `SendCC(outDev, 0, 62, 127)` | `SendCC(outDev, 0, 62, 0)` | bool(off.on) | 64–127 on. |
| Thor | Button 1 (CC63) | Remote button. | `SendCC(outDev, 0, 63, 127)` | `SendCC(outDev, 0, 63, 0)` | bool(off.on) | Mappable.
| Thor | Osc1 Phase Mode (CC92) | Phase mode for Osc1. | `SendCC(outDev, 0, 92, 127)` | `SendCC(outDev, 0, 92, 0)` | Enum(unknown) | Stepped. |
| Thor | Osc1 Phase Diff (CC93) | Phase offset for Osc1. | `SendCC(outDev, 0, 93, 127)` | `SendCC(outDev, 0, 93, 0)` | int(0..127) | |
| Thor | Osc2 On/Off (CC94) | Toggle Osc2 engine. | `SendCC(outDev, 0, 94, 127)` | `SendCC(outDev, 0, 94, 0)` | bool(off.on) | 64–127 on. |
| Thor | Osc2 Mod Amount (CC95) | Mod amount for Osc2. | `SendCC(outDev, 0, 95, 127)` | `SendCC(outDev, 0, 95, 0)` | int(0..127) | |
| Thor | Osc2 Octave (CC102) | Octave for Osc2. | `SendCC(outDev, 0, 102, 127)` | `SendCC(outDev, 0, 102, 0)` | Enum(unknown) | Stepped. |
| Thor | Osc2 Semitone (CC103) | Semitone for Osc2. | `SendCC(outDev, 0, 103, 127)` | `SendCC(outDev, 0, 103, 0)` | int(0..127) | |
| Thor | Osc2 Fine (CC104) | Fine tune Osc2. | `SendCC(outDev, 0, 104, 127)` | `SendCC(outDev, 0, 104, 0)` | int(0..127) | |
| Thor | Osc2 Sync Amount (CC105) | Depth of Osc2 sync. | `SendCC(outDev, 0, 105, 127)` | `SendCC(outDev, 0, 105, 0)` | int(0..127) | |
| Thor | Osc2 Sync On (CC106) | Toggle Osc2 sync. | `SendCC(outDev, 0, 106, 127)` | `SendCC(outDev, 0, 106, 0)` | bool(off.on) | 64–127 on. |
| Thor | Sequencer Rate (CC107) | Thor internal Seq rate. | `SendCC(outDev, 0, 107, 127)` | `SendCC(outDev, 0, 107, 0)` | int(0..127) | |
| Thor | AM Amount (CC108) | Amplitude modulation amount. | `SendCC(outDev, 0, 108, 127)` | `SendCC(outDev, 0, 108, 0)` | int(0..127) | |
| Thor | LFO2 Tempo Sync (CC109) | Sync LFO2 to tempo. | `SendCC(outDev, 0, 109, 127)` | `SendCC(outDev, 0, 109, 0)` | bool(off.on) | 64–127 on. |
| Thor | LFO2 Rate (CC110) | LFO2 speed. | `SendCC(outDev, 0, 110, 127)` | `SendCC(outDev, 0, 110, 0)` | int(0..127) | |
| Thor | LFO2 Wave (CC111) | LFO2 shape. | `SendCC(outDev, 0, 111, 127)` | `SendCC(outDev, 0, 111, 0)` | Enum(unknown) | Stepped shapes. |
| Thor | LFO2 Delay (CC112) | LFO2 fade‑in. | `SendCC(outDev, 0, 112, 127)` | `SendCC(outDev, 0, 112, 0)` | int(0..127) | |
| Thor | LFO2 Key Sync (CC113) | Sync LFO2 on keypress. | `SendCC(outDev, 0, 113, 127)` | `SendCC(outDev, 0, 113, 0)` | bool(off.on) | 64–127 on. |
| Thor | Global Env Delay (CC114) | Global env delay. | `SendCC(outDev, 0, 114, 127)` | `SendCC(outDev, 0, 114, 0)` | int(0..127) | |
| Thor | Global Env Attack (CC115) | Global env attack. | `SendCC(outDev, 0, 115, 127)` | `SendCC(outDev, 0, 115, 0)` | int(0..127) | |
| Thor | Global Env Hold (CC116) | Global env hold. | `SendCC(outDev, 0, 116, 127)` | `SendCC(outDev, 0, 116, 0)` | int(0..127) | |
| Thor | Global Env Decay (CC117) | Global env decay. | `SendCC(outDev, 0, 117, 127)` | `SendCC(outDev, 0, 117, 0)` | int(0..127) | |
| Thor | Global Env Sustain (CC118) | Global env sustain. | `SendCC(outDev, 0, 118, 127)` | `SendCC(outDev, 0, 118, 0)` | int(0..127) | |
| Thor | Global Env Release (CC119) | Global env release. | `SendCC(outDev, 0, 119, 127)` | `SendCC(outDev, 0, 119, 0)` | int(0..127) | |

---

## Malström — full CC map

| Instrument | Instrument setting | Setting description | SendCC (max) | SendCC (min) | Data type | Notes |
|---|---|---|---|---|---|---|
| Malström | Mod Wheel (CC1) | Performance/mod source (often to Index/Motion). | `SendCC(outDev, 0, 1, 127)` | `SendCC(outDev, 0, 1, 0)` | int(0..127) | |
| Malström | Portamento (CC5) | Glide time. | `SendCC(outDev, 0, 5, 127)` | `SendCC(outDev, 0, 5, 0)` | int(0..127) | |
| Malström | Master Level (CC7) | Output level. | `SendCC(outDev, 0, 7, 127)` | `SendCC(outDev, 0, 7, 0)` | int(0..127) | |
| Malström | Osc B Decay (CC9) | Envelope decay for Osc B. | `SendCC(outDev, 0, 9, 127)` | `SendCC(outDev, 0, 9, 0)` | int(0..127) | |
| Malström | Osc B Sustain (CC12) | Sustain level for Osc B. | `SendCC(outDev, 0, 12, 127)` | `SendCC(outDev, 0, 12, 0)` | int(0..127) | |
| Malström | Filter Env Attack (CC14) | Filter env attack. | `SendCC(outDev, 0, 14, 127)` | `SendCC(outDev, 0, 14, 0)` | int(0..127) | |
| Malström | Filter Env Decay (CC15) | Filter env decay. | `SendCC(outDev, 0, 15, 127)` | `SendCC(outDev, 0, 15, 0)` | int(0..127) | |
| Malström | Filter Env Sustain (CC16) | Filter env sustain. | `SendCC(outDev, 0, 16, 127)` | `SendCC(outDev, 0, 16, 0)` | int(0..127) | |
| Malström | Filter Env Release (CC17) | Filter env release. | `SendCC(outDev, 0, 17, 127)` | `SendCC(outDev, 0, 17, 0)` | int(0..127) | |
| Malström | Filter Env Amount (CC18) | Depth of filter env. | `SendCC(outDev, 0, 18, 127)` | `SendCC(outDev, 0, 18, 0)` | int(0..127) | |
| Malström | Filter Env Invert (CC19) | Invert filter env polarity. | `SendCC(outDev, 0, 19, 127)` | `SendCC(outDev, 0, 19, 0)` | bool(off.on) | 64–127 on. |
| Malström | Osc B Octave (CC21) | Octave for Osc B. | `SendCC(outDev, 0, 21, 127)` | `SendCC(outDev, 0, 21, 0)` | Enum(unknown) | Stepped. |
| Malström | Osc B Semi (CC22) | Semitone for Osc B. | `SendCC(outDev, 0, 22, 127)` | `SendCC(outDev, 0, 22, 0)` | int(0..127) | |
| Malström | Osc B Cent (CC23) | Fine tune Osc B. | `SendCC(outDev, 0, 23, 127)` | `SendCC(outDev, 0, 23, 0)` | int(0..127) | |
| Malström | Mod A On/Off (CC25) | Toggle Modulator A. | `SendCC(outDev, 0, 25, 127)` | `SendCC(outDev, 0, 25, 0)` | bool(off.on) | 64–127 on. |
| Malström | Mod A Rate (CC26) | Rate of Modulator A. | `SendCC(outDev, 0, 26, 127)` | `SendCC(outDev, 0, 26, 0)` | int(0..127) | |
| Malström | Mod A → Pitch (CC27) | Amount to Pitch. | `SendCC(outDev, 0, 27, 127)` | `SendCC(outDev, 0, 27, 0)` | int(0..127) | |
| Malström | Mod A Curve (CC28) | Mod A curve shape. | `SendCC(outDev, 0, 28, 127)` | `SendCC(outDev, 0, 28, 0)` | Enum(unknown) | Stepped. |
| Malström | Mod A One Shot (CC29) | One‑shot behavior. | `SendCC(outDev, 0, 29, 127)` | `SendCC(outDev, 0, 29, 0)` | bool(off.on) | 64–127 on. |
| Malström | Mod A Target (CC30) | Mod A destination. | `SendCC(outDev, 0, 30, 127)` | `SendCC(outDev, 0, 30, 0)` | Enum(unknown) | |
| Malström | Mod A → Index (CC31) | Amount to Index. | `SendCC(outDev, 0, 31, 127)` | `SendCC(outDev, 0, 31, 0)` | int(0..127) | |
| Malström | Osc A Gain (CC91) | Oscillator A gain. | `SendCC(outDev, 0, 91, 127)` | `SendCC(outDev, 0, 91, 0)` | int(0..127) | |
| Malström | Osc A Motion (CC92) | Motion parameter. | `SendCC(outDev, 0, 92, 127)` | `SendCC(outDev, 0, 92, 0)` | int(0..127) | |
| Malström | Osc A Index (CC93) | Index parameter. | `SendCC(outDev, 0, 93, 127)` | `SendCC(outDev, 0, 93, 0)` | int(0..127) | |
| Malström | Osc A On/Off (CC95) | Toggle Oscillator A. | `SendCC(outDev, 0, 95, 127)` | `SendCC(outDev, 0, 95, 0)` | bool(off.on) | 64–127 on. |
| Malström | Osc A Octave (CC102) | Octave for Osc A. | `SendCC(outDev, 0, 102, 127)` | `SendCC(outDev, 0, 102, 0)` | Enum(unknown) | Stepped. |
| Malström | Osc A Semi (CC103) | Semitone for Osc A. | `SendCC(outDev, 0, 103, 127)` | `SendCC(outDev, 0, 103, 0)` | int(0..127) | |
| Malström | Osc A Cent (CC104) | Fine tune Osc A. | `SendCC(outDev, 0, 104, 127)` | `SendCC(outDev, 0, 104, 0)` | int(0..127) | |
| Malström | Spread Amount (CC105) | Stereo spread depth. | `SendCC(outDev, 0, 105, 127)` | `SendCC(outDev, 0, 105, 0)` | int(0..127) | |
| Malström | Mod B Rate (CC110) | Rate of Modulator B. | `SendCC(outDev, 0, 110, 127)` | `SendCC(outDev, 0, 110, 0)` | int(0..127) | |
| Malström | Mod B → Level (CC111) | Amount to Level. | `SendCC(outDev, 0, 111, 127)` | `SendCC(outDev, 0, 111, 0)` | int(0..127) | |
| Malström | Mod B → Filter (CC112) | Amount to Filter. | `SendCC(outDev, 0, 112, 127)` | `SendCC(outDev, 0, 112, 0)` | int(0..127) | |
| Malström | Mod B → Mod A (CC113) | Amount to Mod A. | `SendCC(outDev, 0, 113, 127)` | `SendCC(outDev, 0, 113, 0)` | int(0..127) | |
| Malström | Mod B On/Off (CC114) | Toggle Modulator B. | `SendCC(outDev, 0, 114, 127)` | `SendCC(outDev, 0, 114, 0)` | bool(off.on) | 64–127 on. |
| Malström | Mod B Curve (CC115) | Curve shape for Mod B. | `SendCC(outDev, 0, 115, 127)` | `SendCC(outDev, 0, 115, 0)` | Enum(unknown) | Stepped. |
| Malström | Mod B One Shot (CC116) | One‑shot behavior. | `SendCC(outDev, 0, 116, 127)` | `SendCC(outDev, 0, 116, 0)` | bool(off.on) | 64–127 on. |
| Malström | Mod B Target (CC117) | Destination select. | `SendCC(outDev, 0, 117, 127)` | `SendCC(outDev, 0, 117, 0)` | Enum(unknown) | |
| Malström | Mod B → Motion (CC118) | Amount to Motion. | `SendCC(outDev, 0, 118, 127)` | `SendCC(outDev, 0, 118, 0)` | int(0..127) | |

---

## ID8 — full CC map (device)

| Instrument | Instrument setting | Setting description | SendCC (max) | SendCC (min) | Data type | Notes |
|---|---|---|---|---|---|---|
| ID8 | Mod Wheel (CC1) | Performance/mod source (preset‑dependent). | `SendCC(outDev, 0, 1, 127)` | `SendCC(outDev, 0, 1, 0)` | int(0..127) | |
| ID8 | Volume (CC7) | Overall output level. | `SendCC(outDev, 0, 7, 127)` | `SendCC(outDev, 0, 7, 0)` | int(0..127) | |
| ID8 | Transpose On (CC16) | Enable transposition. | `SendCC(outDev, 0, 16, 127)` | `SendCC(outDev, 0, 16, 0)` | bool(off.on) | 64–127 on. |
| ID8 | Semitone (CC17) | Coarse tune in semitones. | `SendCC(outDev, 0, 17, 127)` | `SendCC(outDev, 0, 17, 0)` | int(0..127) | Center ≈ default pitch. |
| ID8 | Cent (CC18) | Fine tune in cents. | `SendCC(outDev, 0, 18, 127)` | `SendCC(outDev, 0, 18, 0)` | int(0..127) | |

---

### Notes
- Booleans: conventionally treat `0–63` as off and `64–127` as on; the examples use extreme values for clarity.
- Enums: Reason steps through discrete choices. Without the published step map, sweep 0→127 to discover breakpoints.
- Pitch/Semitone/Fine controls are device‑scaled; center points (often ≈ 64) can be neutral, but it varies with patch/device.
- The Subtractor “Mod Wheel 2” entries work only if the device’s Mod Wheel 2 source is assigned to Breath/Expression as indicated.
- LSB (CC33–63) entries are used for additional modulation amounts/routes in some devices; unsupported entries are safely ignored by others.

