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
        Targets = Array.AsReadOnly(targets.ToArray());
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
        var strike = Prepare(targets, damage);
        Apply(strike, damage);
        return strike;
    }

    internal static SwingResult Prepare(IEnumerable<PigState> targets, int damage)
    {
        ArgumentNullException.ThrowIfNull(targets);
        if (damage <= 0) throw new ArgumentOutOfRangeException(nameof(damage));
        var uniqueTargets = targets.Distinct().ToArray();
        if (uniqueTargets.Any(pig => pig == null))
            throw new ArgumentException("Targets cannot contain a null pig.", nameof(targets));
        var results = uniqueTargets.Where(pig => pig.IsAlive).Select(pig =>
            new PigDamage(pig, new(true, pig.Health <= damage, Math.Max(0, pig.Health - damage)))).ToArray();
        return new SwingResult(results);
    }

    internal static void Apply(SwingResult strike, int damage)
    {
        foreach (var target in strike.Targets) target.Pig.ApplyDamage(damage);
    }
}
