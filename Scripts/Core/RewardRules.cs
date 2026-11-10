using System;

/// <summary>
/// 按本次击碎数量计算每猪额外金币，不将仅受伤的目标计入收益。
/// </summary>
public sealed class PerPigReward : ISwingRewardRule
{
    public SwingPayout Calculate(StrikeSummary strike, Bonus bonus, IRewardBudget budget) =>
        new(checked(strike.DestroyedCount * bonus.PigCoins), 0);
}

/// <summary>
/// 按单次击碎数量区间触发一次额外收益，适用于单响、双响和多碎奖励。
/// </summary>
public sealed class ComboReward : ISwingRewardRule
{
    private readonly int _minimum;
    private readonly int _maximum;
    private readonly Func<Bonus, int> _coins;

    public ComboReward(int minimum, Func<Bonus, int> coins, int maximum = int.MaxValue)
    {
        if (minimum < 1 || maximum < minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(minimum));
        }

        ArgumentNullException.ThrowIfNull(coins);
        _minimum = minimum;
        _maximum = maximum;
        _coins = coins;
    }

    public SwingPayout Calculate(StrikeSummary strike, Bonus bonus, IRewardBudget budget) =>
        new(strike.DestroyedCount >= _minimum && strike.DestroyedCount <= _maximum ? _coins(bonus) : 0, 0);
}

/// <summary>
/// 通过每日预算限制额外掉币次数；预算使用量在整次攻击成功后才提交。
/// </summary>
public sealed class LimitedPigReward : ISwingRewardRule
{
    public SwingPayout Calculate(StrikeSummary strike, Bonus bonus, IRewardBudget budget) =>
        new(budget.Consume("bonus-kills", strike.DestroyedCount, bonus.BonusCoinLimit), 0);
}

/// <summary>
/// 仅对消耗了体力的一锤多碎返还体力，受每日次数限制，避免被动效果产生免费循环。
/// </summary>
public sealed class ComboRefund : ISwingRewardRule
{
    public SwingPayout Calculate(StrikeSummary strike, Bonus bonus, IRewardBudget budget) =>
        new(0, strike.StaminaSpent > 0 && strike.DestroyedCount >= 2 ? budget.Consume("combo-refunds", 1, bonus.Refunds) : 0);
}
