using System;

public sealed class PerPigReward : ISwingRewardRule
{
    public SwingPayout Calculate(StrikeSummary strike, Bonus bonus, IRewardBudget budget) =>
        new(checked(strike.DestroyedCount * bonus.PigCoins), 0);
}

public sealed class ComboReward : ISwingRewardRule
{
    private readonly int _minimum, _maximum;
    private readonly Func<Bonus, int> _coins;
    public ComboReward(int minimum, Func<Bonus, int> coins, int maximum = int.MaxValue)
    {
        if (minimum < 1 || maximum < minimum) throw new ArgumentOutOfRangeException(nameof(minimum));
        ArgumentNullException.ThrowIfNull(coins);
        _minimum = minimum; _maximum = maximum; _coins = coins;
    }
    public SwingPayout Calculate(StrikeSummary strike, Bonus bonus, IRewardBudget budget) =>
        new(strike.DestroyedCount >= _minimum && strike.DestroyedCount <= _maximum ? _coins(bonus) : 0, 0);
}

public sealed class LimitedPigReward : ISwingRewardRule
{
    public SwingPayout Calculate(StrikeSummary strike, Bonus bonus, IRewardBudget budget) =>
        new(budget.Consume("bonus-kills", strike.DestroyedCount, bonus.BonusCoinLimit), 0);
}

public sealed class ComboRefund : ISwingRewardRule
{
    public SwingPayout Calculate(StrikeSummary strike, Bonus bonus, IRewardBudget budget) =>
        new(0, strike.DestroyedCount >= 2 ? budget.Consume("combo-refunds", 1, bonus.Refunds) : 0);
}
