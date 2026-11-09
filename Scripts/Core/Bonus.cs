using System;

/// <summary>Shared value object for equipment, training and perks.</summary>
public sealed record Bonus(int Stamina = 0, float Radius = 0, float Recovery = 0,
    int Refunds = 0, int PairCoins = 0, int PigCoins = 0, int TripleCoins = 0,
    int Damage = 0, int SingleCoins = 0, int BonusCoinLimit = 0)
{
    public bool IsValid => Stamina >= 0 && Refunds >= 0 && PairCoins >= 0 && PigCoins >= 0 &&
        TripleCoins >= 0 && Damage >= 0 && SingleCoins >= 0 && BonusCoinLimit >= 0 &&
        float.IsFinite(Radius) && Radius >= 0 && float.IsFinite(Recovery) && Recovery >= 0;

    public Bonus Add(Bonus other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var sum = new Bonus(checked(Stamina + other.Stamina), Radius + other.Radius, Recovery + other.Recovery,
            checked(Refunds + other.Refunds), checked(PairCoins + other.PairCoins), checked(PigCoins + other.PigCoins),
            checked(TripleCoins + other.TripleCoins), checked(Damage + other.Damage),
            checked(SingleCoins + other.SingleCoins), checked(BonusCoinLimit + other.BonusCoinLimit));
        if (!sum.IsValid) throw new OverflowException("Combined bonuses exceed the supported range.");
        return sum;
    }
}
