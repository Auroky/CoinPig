using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>挥动、落锤、碎裂和金币采用独立声部，连续命中时允许短尾音重叠。</summary>
public partial class ImpactAudio : AudioStreamPlayer
{
    public IPigAppearanceSource Appearances { private get; set; } = new PigRoster(Array.Empty<PigArchetype>());
    private readonly List<AudioStreamWav> _clips = new();
    private AudioStreamPlayer _air = null!, _crack = null!, _coins = null!;
    private AudioStreamWav _thud = null!, _empty = null!, _ceramic = null!, _metal = null!;

    public override void _Ready()
    {
        MaxPolyphony = 4;
        _air = Voice(-13, 1);
        _crack = Voice(-7, 4);
        _coins = Voice(-10, 4);
        _air.Stream = MakeClip(0.18, (t, noise) => noise * Math.Sin(Math.PI * t / 0.18) * 0.34);
        _thud = MakeClip(0.24, (t, noise) =>
            Math.Sin(2 * Math.PI * (82 * t + 3.5 * (1 - Math.Exp(-t * 42)))) * Math.Exp(-t * 23) * 0.72 +
            noise * Math.Exp(-t * 100) * 0.36);
        _empty = MakeClip(0.15, (t, noise) => Math.Sin(2 * Math.PI * 145 * t) * Math.Exp(-t * 32) * 0.28 + noise * Math.Exp(-t * 75) * 0.13);
        _ceramic = MakeClip(0.29, (t, noise) =>
            (noise * 0.5 + Math.Sin(2 * Math.PI * 870 * t) * 0.17 + Math.Sin(2 * Math.PI * 1770 * t) * 0.08) *
            Math.Exp(-t * 17) * (0.65 + 0.35 * Math.Cos(t * 190)));
        _metal = MakeClip(0.30, (t, noise) =>
            (Math.Sin(2 * Math.PI * 640 * t) * 0.30 + Math.Sin(2 * Math.PI * 1427 * t) * 0.17) * Math.Exp(-t * 17) +
            noise * Math.Exp(-t * 90) * 0.16);
        _coins.Stream = MakeClip(0.50, (t, noise) =>
        {
            double wave = 0;
            for (int i = 0; i < 3; i++)
            {
                double age = t - 0.12 - i * 0.065;
                if (age < 0) continue;
                double pitch = 980 * Math.Pow(1.25, i);
                wave += (Math.Sin(2 * Math.PI * pitch * age) + 0.3 * Math.Sin(2 * Math.PI * pitch * 2.73 * age)) *
                    Math.Min(1, age * 900) * Math.Exp(-age * 25) * 0.22;
            }
            return wave;
        });
    }

    private AudioStreamPlayer Voice(float volume, int polyphony)
    {
        var voice = new AudioStreamPlayer { VolumeDb = volume, MaxPolyphony = polyphony };
        AddChild(voice);
        return voice;
    }

    public void PlaySwing() => _air.Play();
    public void StopSwing() => _air.Stop();

    public void PlayImpact(SwingResult result)
    {
        int broken = result.DestroyedCount, hits = result.HitCount;
        bool metalHit = hits > 0 && result.Targets.All(hit => Appearances.GetAppearance(hit.Pig.Definition.Id).Metal);
        Stream = hits > 0 ? _thud : _empty;
        PitchScale = broken >= 2 ? 0.90f : 1.0f;
        Play();
        if (hits > 0)
        {
            _crack.Stream = metalHit ? _metal : _ceramic;
            _crack.PitchScale = broken >= 2 ? 0.94f : 1.08f;
            _crack.Play();
        }
        if (broken > 0)
        {
            _coins.PitchScale = 1 + Math.Min(3, broken - 1) * 0.07f;
            _coins.Play();
        }
    }

    public void Clear()
    {
        Stop();
        _air.Stop();
        _crack.Stop();
        _coins.Stop();
    }

    private AudioStreamWav MakeClip(double seconds, Func<double, double, double> sample)
    {
        const int rate = 22050;
        var bytes = new byte[(int)(seconds * rate) * 2];
        var random = new Random(21);
        for (int i = 0; i < bytes.Length / 2; i++)
        {
            double time = (double)i / rate;
            double envelope = Math.Min(1, time / 0.0015) * Math.Min(1, (seconds - time) / 0.012);
            short value = (short)Math.Clamp(sample(time, random.NextDouble() * 2 - 1) * envelope * 12500, -26000, 26000);
            bytes[i * 2] = (byte)value;
            bytes[i * 2 + 1] = (byte)(value >> 8);
        }
        var clip = new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = rate, Data = bytes };
        _clips.Add(clip);
        return clip;
    }
    public override void _ExitTree()
    {
        foreach (var clip in _clips) clip.Dispose();
        _clips.Clear();
    }
}
