using Godot;
using System;

/// <summary>短促的程序合成音效，无外部素材依赖。</summary>
public partial class ImpactAudio : Node
{
    private readonly AudioStreamPlayer[] _voices = new AudioStreamPlayer[4];
    private AudioStreamWav _hit = null!, _miss = null!, _coins = null!;
    private int _voice;
    public override void _Ready()
    {
        for (int i = 0; i < _voices.Length; i++) { _voices[i] = new AudioStreamPlayer { VolumeDb = -14 }; AddChild(_voices[i]); }
        _hit = Tone(105, .16f, true); _miss = Tone(65, .10f, true); _coins = Tone(920, .21f, false);
    }
    private static AudioStreamWav Tone(float frequency, float duration, bool noise)
    {
        const int rate = 22050; int count = (int)(duration * rate); byte[] bytes = new byte[count * 2]; var rng = new Random(7);
        for (int i = 0; i < count; i++)
        {
            double t = (double)i / rate, fade = Math.Pow(1.0 - (double)i / count, 2.5);
            double sample = Math.Sin(Math.Tau * frequency * t) * (noise ? .7 : .6);
            sample += noise ? (rng.NextDouble() * 2 - 1) * .3 : Math.Sin(Math.Tau * frequency * 1.5 * t) * .15;
            short value = (short)(sample * fade * short.MaxValue * .65);
            bytes[i * 2] = (byte)(value & 255); bytes[i * 2 + 1] = (byte)((value >> 8) & 255);
        }
        return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = rate, Data = bytes };
    }
    public void Play(int hits, int coins)
    {
        var p = _voices[_voice++ % _voices.Length]; p.Stream = hits > 0 ? _hit : _miss; p.Play();
        if (coins > 0) { p = _voices[_voice++ % _voices.Length]; p.Stream = _coins; p.PitchScale = coins >= 6 ? 1.2f : 1; p.Play(); }
    }
}
