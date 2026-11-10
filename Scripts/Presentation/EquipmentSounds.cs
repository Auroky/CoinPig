using System;
using System.Collections.Generic;

/// <summary>
/// 声音合成配方：时长及采样函数；函数输入为时间（秒）和噪声，输出为波形值。
/// </summary>
public sealed record SynthSound(double Seconds, Func<double, double, double> Sample);

/// <summary>
/// 某种声音键对应的挥动与命中两份配方。
/// </summary>
public sealed record EquipmentSound(SynthSound Swing, SynthSound Impact);

/// <summary>
/// 按声音键登记配方；新增音色无需修改武器伤害、动作或会话逻辑。
/// </summary>
public sealed class EquipmentSounds
{
    private readonly Dictionary<string, EquipmentSound> _sounds = new();

    public IEnumerable<KeyValuePair<string, EquipmentSound>> All => _sounds;

    public EquipmentSounds Register(string cue, EquipmentSound sound)
    {
        if (string.IsNullOrWhiteSpace(cue) || sound == null || !Valid(sound.Swing) || !Valid(sound.Impact))
        {
            throw new ArgumentException("Sound cues require finite positive durations and sample functions.");
        }

        _sounds[cue] = sound;
        return this;
    }

    private static bool Valid(SynthSound sound) => sound != null && double.IsFinite(sound.Seconds) &&
        sound.Seconds > 0 && sound.Seconds <= 4 && sound.Sample != null;

    private static double Sine(double frequencyHz, double time) => Math.Sin(2 * Math.PI * frequencyHz * time);

    public static EquipmentSounds CreateDefault() => new EquipmentSounds()
        .Register("hammer", new(
            new(.18, (time, noise) => noise * Math.Sin(Math.PI * time / .18) * .34),
            new(.24, (time, noise) => Sine(82, time) * Math.Exp(-time * 23) * .78 + noise * Math.Exp(-time * 90) * .32)))
        .Register("chainsaw", new(
            new(.24, (time, noise) => (Sine(110 + 160 * time, time) * .28 + noise * .20) * Math.Sin(Math.PI * time / .24)),
            new(.30, (time, noise) => (Sine(145, time) * .30 + Sine(290, time) * .14 + noise * .40) *
                (.65 + .35 * Sine(48, time)) * Math.Exp(-time * 9))))
        .Register("grinder", new(
            new(.27, (time, noise) => (Sine(630 + 900 * time, time) * .18 + noise * .24) * Math.Sin(Math.PI * time / .27)),
            new(.38, (time, noise) => (Sine(920, time) * .18 + Sine(1837, time) * .13 + noise * .38) * Math.Exp(-time * 7))))
        .Register("resonance", new(
            new(.25, (time, noise) => Sine(440, time) * Math.Sin(Math.PI * time / .25) * .15),
            new(.75, (time, noise) => (Sine(523.25, time) * .33 + Sine(784.88, time) * .21 + Sine(1046.5, time) * .13) *
                Math.Exp(-time * 5) * (.85 + .15 * Math.Cos(time * 35)))));
}
