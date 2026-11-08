using System;

public readonly record struct SwingPayout(int Coins, int StaminaRefund);

/// <summary>Calculates strike rewards and maintains the daily bonus limits.</summary>
public sealed class SwingRewards
{
    private int _refunds;
    private int _bonusKills;

    public void BeginDay()
    {
        _refunds = 0;
        _bonusKills = 0;
    }

    public SwingPayout Calculate(SwingResult strike, SkillBonus skills, Func<Perk, bool> owns)
    {
        int broken = strike.DestroyedCount;
        int coins = strike.BaseCoins + broken * skills.PigCoins;
        int refund = 0;
        if (owns(Perk.CoinValue))
        {
            int rewarded = Math.Min(broken, GameRules.CoinValueLimit - _bonusKills);
            coins += rewarded;
            _bonusKills += rewarded;
        }
        if (broken >= 3) coins += skills.TripleCoins;
        if (broken == 1 && owns(Perk.Precision)) coins += GameRules.PrecisionBonusCoins;
        if (broken >= 2)
        {
            coins += skills.PairCoins;
            if (owns(Perk.PairBonus)) coins += GameRules.PairBonusCoins;
            int limit = skills.Refunds + (owns(Perk.SecondWind) ? GameRules.SecondWindRefunds : 0);
            if (_refunds < limit)
            {
                _refunds++;
                refund = 1;
            }
        }
        return new(coins, refund);
    }
}
