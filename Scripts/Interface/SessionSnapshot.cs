using System.Collections.Generic;

/// <summary>Immutable information consumed by the HUD and day-end screens.</summary>
public sealed record SessionSnapshot(
    ShiftPhase Phase, int Day, int Wallet, int Stamina, int Capacity, int Earnings,
    int Pigs, int Damage, float Radius, bool Endless, Bill? NextBill, ShiftReceipt? Receipt,
    IReadOnlyList<RentDue> Schedule, IReadOnlyList<PerkCard> Perks)
{
    public static SessionSnapshot From(GameSession session) => new(
        session.Phase, session.Shift, session.Wallet, session.Stamina, session.Capacity,
        session.ShiftEarnings, session.ShiftPigs, session.Damage, 1.43f + session.ExtraRadius,
        session.IsEndless, session.NextBill, session.Receipt, session.GetRentSchedule(), session.Offers);
}
