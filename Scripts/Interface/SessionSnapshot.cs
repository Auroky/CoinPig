using System.Collections.Generic;

/// <summary>Read-only display data consumed by the HUD and day-end screens.</summary>
public sealed record SessionSnapshot(
    ShiftPhase Phase, int Day, int ContractLength, int Wallet, int Stamina, int Capacity, int Earnings,
    int Pigs, int Damage, float Radius, bool Endless, Bill? NextBill, ShiftReceipt? Receipt,
    IReadOnlyList<RentDue> Schedule, IReadOnlyList<PerkCard> Perks)
{
    public static SessionSnapshot From(ISessionView session) => new(
        session.Phase, session.Shift, session.ContractLength, session.Wallet, session.Stamina, session.Capacity,
        session.ShiftEarnings, session.ShiftPigs, session.Damage, session.Radius,
        session.IsEndless, session.NextBill, session.Receipt, session.GetRentSchedule(), session.Offers);
}
