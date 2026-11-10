using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 一次攻击中某只猪的预计算伤害结果，不依赖之后的重生或再次受击。
/// </summary>
public sealed record PigDamage(PigState Pig, DamageResult Result);

/// <summary>
/// 整次攻击的结果快照，包含去重目标、击碎数和基础收益，并标记是否已完成经济结算。
/// </summary>
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

/// <summary>
/// 计算并应用一次攻击的耐久变化。先完整验证所有目标，避免枚举失败留下部分扣血。
/// </summary>
public static class StrikeResolver
{
    public static SwingResult Resolve(IEnumerable<PigState> targets, int damage)
    {
        var strike = Prepare(targets, damage);
        Apply(strike, damage);
        return strike;
    }

    /// <summary>只生成伤害快照，先完整枚举并验证目标；此阶段不扣血。</summary>
    internal static SwingResult Prepare(IEnumerable<PigState> targets, int damage)
    {
        ArgumentNullException.ThrowIfNull(targets);
        if (damage <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage));
        }

        var uniqueTargets = targets.Distinct().ToArray();
        if (uniqueTargets.Any(pig => pig == null))
        {
            throw new ArgumentException("Targets cannot contain a null pig.", nameof(targets));
        }

        var results = uniqueTargets.Where(pig => pig.IsAlive).Select(pig =>
                    new PigDamage(pig, new(true, pig.Health <= damage, Math.Max(0, pig.Health - damage)))).ToArray();
        return new SwingResult(results);
    }

    /// <summary>应用已经验证的伤害结果，调用者负责确保同一次攻击只提交一次。</summary>
    internal static void Apply(SwingResult strike, int damage)
    {
        foreach (var target in strike.Targets)
        {
            target.Pig.ApplyDamage(damage);
        }
    }
}
