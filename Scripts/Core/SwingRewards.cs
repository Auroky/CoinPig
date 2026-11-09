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

    public SwingPayout Calculate(SwingResult strike, SkillBonus skills, PerkBonus perks)
    {
        int broken = strike.DestroyedCount;
        int coins = strike.BaseCoins + broken * skills.PigCoins;
        int refund = 0;
        if (perks.BonusCoinLimit > 0)
        {
            int rewarded = Math.Min(broken, Math.Max(0, perks.BonusCoinLimit - _bonusKills));
            coins += rewarded;
            _bonusKills += rewarded;
        }
        if (broken >= 3) coins += skills.TripleCoins;
        if (broken == 1) coins += perks.SingleCoins;
        if (broken >= 2)
        {
            coins += skills.PairCoins;
            coins += perks.PairCoins;
            int limit = skills.Refunds + perks.Refunds;
            if (_refunds < limit)
            {
                _refunds++;
                refund = 1;
            }
        }
        return new(coins, refund);
    }
}
