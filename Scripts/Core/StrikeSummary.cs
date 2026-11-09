using System;
using System.Collections.Generic;
using System.Linq;

public sealed record PigHitSnapshot(PigDefinition Definition, bool Destroyed, int RemainingHealth);

/// <summary>Reward policies see immutable facts and never receive live PigState references.</summary>
public sealed class StrikeSummary
{
    public int HitCount { get; }
    public int DestroyedCount { get; }
    public int BaseCoins { get; }
    public IReadOnlyList<PigHitSnapshot> Targets { get; }
    internal StrikeSummary(SwingResult strike)
    {
        HitCount = strike.HitCount; DestroyedCount = strike.DestroyedCount; BaseCoins = strike.BaseCoins;
        Targets = Array.AsReadOnly(strike.Targets.Select(hit =>
            new PigHitSnapshot(hit.Pig.Definition, hit.Result.Destroyed, hit.Result.RemainingHealth)).ToArray());
    }
}
