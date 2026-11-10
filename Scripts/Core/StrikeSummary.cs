using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 提供给奖励策略的不可变命中事实，不包含可修改的 PigState 引用。
/// </summary>
public sealed record PigHitSnapshot(PigDefinition Definition, bool Destroyed, int RemainingHealth);

/// <summary>
/// 奖励策略可读取的攻击摘要；区分命中和击碎，并记录实际体力消耗。
/// </summary>
public sealed class StrikeSummary
{
    public int HitCount { get; }
    public int DestroyedCount { get; }
    public int BaseCoins { get; }
    public IReadOnlyList<PigHitSnapshot> Targets { get; }
    public int StaminaSpent { get; }

    internal StrikeSummary(SwingResult strike, int staminaSpent = 1)
    {
        StaminaSpent = staminaSpent;
        HitCount = strike.HitCount;
        DestroyedCount = strike.DestroyedCount;
        BaseCoins = strike.BaseCoins;
        Targets = Array.AsReadOnly(strike.Targets.Select(hit =>
            new PigHitSnapshot(hit.Pig.Definition, hit.Result.Destroyed, hit.Result.RemainingHealth)).ToArray());
    }
}
