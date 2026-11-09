using System;
using System.Collections.Generic;

public sealed record SynthSound(double Seconds, Func<double, double, double> Sample);
public sealed record EquipmentSound(SynthSound Swing, SynthSound Impact);

/// <summary>Sound recipes are registered independently of weapon IDs, animation and combat rules.</summary>
public sealed class EquipmentSounds
{
    private readonly Dictionary<string, EquipmentSound> _sounds = new();
    public IEnumerable<KeyValuePair<string, EquipmentSound>> All => _sounds;
    public EquipmentSounds Register(string cue, EquipmentSound sound)
    {
        if (string.IsNullOrWhiteSpace(cue) || sound == null || !Valid(sound.Swing) || !Valid(sound.Impact))
            throw new ArgumentException("Sound cues require finite positive durations and sample functions.");
        _sounds[cue] = sound;
        return this;
    }
    private static bool Valid(SynthSound sound) => sound != null && double.IsFinite(sound.Seconds) &&
        sound.Seconds > 0 && sound.Seconds <= 4 && sound.Sample != null;
    private static double Sine(double hz, double t) => Math.Sin(2 * Math.PI * hz * t);
    public static EquipmentSounds CreateDefault() => new EquipmentSounds()
        .Register("hammer", new(
            new(.18, (t, n) => n * Math.Sin(Math.PI * t / .18) * .34),
            new(.24, (t, n) => Sine(82, t) * Math.Exp(-t * 23) * .78 + n * Math.Exp(-t * 90) * .32)))
        .Register("chainsaw", new(
            new(.24, (t, n) => (Sine(110 + 160 * t, t) * .28 + n * .20) * Math.Sin(Math.PI * t / .24)),
            new(.30, (t, n) => (Sine(145, t) * .30 + Sine(290, t) * .14 + n * .40) *
                (.65 + .35 * Sine(48, t)) * Math.Exp(-t * 9))))
        .Register("grinder", new(
            new(.27, (t, n) => (Sine(630 + 900 * t, t) * .18 + n * .24) * Math.Sin(Math.PI * t / .27)),
            new(.38, (t, n) => (Sine(920, t) * .18 + Sine(1837, t) * .13 + n * .38) * Math.Exp(-t * 7))))
        .Register("resonance", new(
            new(.25, (t, n) => Sine(440, t) * Math.Sin(Math.PI * t / .25) * .15),
            new(.75, (t, n) => (Sine(523.25, t) * .33 + Sine(784.88, t) * .21 + Sine(1046.5, t) * .13) *
                Math.Exp(-t * 5) * (.85 + .15 * Math.Cos(t * 35)))));
}
