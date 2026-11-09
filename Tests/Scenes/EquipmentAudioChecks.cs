using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

internal static class EquipmentAudioChecks
{
    public static void Run(Workshop game, Action<bool, string> check)
    {
        var audio = game.GetChildren().OfType<ImpactAudio>().Single();
        var hit = StrikeResolver.Resolve(new[] { new PigState(PigContent.Get("ceramic")) }, 1);
        var hashes = new HashSet<string>();
        string[] args = OS.GetCmdlineUserArgs(); int captureAt = Array.IndexOf(args, "--capture-dir");
        foreach (string cue in new[] { "hammer", "chainsaw", "grinder", "resonance" })
        {
            var clip = audio.ImpactClip(cue);
            hashes.Add(Convert.ToHexString(SHA256.HashData(clip.Data)));
            var samples = Enumerable.Range(0, clip.Data.Length / 2).Select(i => (double)BitConverter.ToInt16(clip.Data, i * 2)).ToArray();
            check(Math.Sqrt(samples.Average(s => s * s)) > 100 && samples.Max(Math.Abs) < 26000,
                cue + " has a non-silent, unclipped generated sound");
            if (cue == "resonance")
            {
                audio.PlayEffect(new(System.Numerics.Vector2.Zero, 2, "gold-wave", hit, 0, AudioCue: cue));
                check(audio.LastImpactCue == cue && ReferenceEquals(audio.EffectStream, clip),
                    "cuff resonance uses its own bell voice instead of the equipped weapon impact");
            }
            else
            {
                audio.SetWeapon(new(new("sound-test", "Sound test", new CircularWeaponAttack(), AudioCue: cue), 1, ShopProduct.StandardPalette, null));
                audio.PlayImpact(hit);
                check(audio.WeaponCue == cue && ReferenceEquals(audio.Stream, clip), cue + " selects its own actual hit stream");
            }
            if (captureAt >= 0 && captureAt + 1 < args.Length)
            {
                Directory.CreateDirectory(args[captureAt + 1]);
                check(clip.SaveToWav(Path.Combine(args[captureAt + 1], cue + ".wav")) == Error.Ok, "exported " + cue + " audio preview");
            }
        }
        check(hashes.Count == 4, "all four equipment impact waveforms are distinct");
        var custom = new ImpactAudio { Sounds = EquipmentSounds.CreateDefault().Register("custom-tone",
            new(new(.1, (t, n) => Math.Sin(t * 2400) * .2), new(.2, (t, n) => Math.Sin(t * 1800) * Math.Exp(-t * 15)))) };
        game.AddChild(custom);
        custom.SetWeapon(new(new("new-tool", "New tool", new CircularWeaponAttack(), AudioCue: "custom-tone"), 1, ShopProduct.StandardPalette, null));
        custom.PlayImpact(hit);
        check(custom.WeaponCue == "custom-tone" && ReferenceEquals(custom.Stream, custom.ImpactClip("custom-tone")),
            "a registered new sound cue works without changing playback control flow");
        custom.Clear(); custom.Free();
        audio.Clear(); audio.SetWeapon(game.Session.EquippedWeapon);
    }
}
