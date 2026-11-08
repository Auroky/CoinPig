using System;
using System.Collections.Generic;
using System.Linq;

public sealed record PigDamage(PigState Pig, DamageResult Result);

public sealed class SwingResult
{
    public IReadOnlyList<PigDamage> Targets { get; }
    public int HitCount { get; }
    public int DestroyedCount { get; }
    public int BaseCoins { get; }
    public bool IsSettled { get; private set; }

    internal SwingResult(IReadOnlyList<PigDamage> targets)
    {
        Targets = targets;
        HitCount = targets.Count;
        var destroyed = targets.Where(target => target.Result.Destroyed).ToArray();
        DestroyedCount = destroyed.Length;
        BaseCoins = destroyed.Sum(target => target.Pig.Definition.Coins);
    }

    internal void MarkSettled() => IsSettled = true;
}

/// <summary>一次落锤对每只活猪伤害一次。结果保存本锤快照，奖励不依赖后续的重生状态。</summary>
public static class StrikeResolver
{
    public static SwingResult Resolve(IEnumerable<PigState> targets, int damage)
    {
        if (damage <= 0) throw new ArgumentOutOfRangeException(nameof(damage));
        var results = new List<PigDamage>();
        foreach (var pig in targets.Distinct())
        {
            var result = pig.ApplyDamage(damage);
            if (result.WasHit) results.Add(new PigDamage(pig, result));
        }
        return new SwingResult(results.AsReadOnly());
    }
}
