using System.Collections.Generic;

/// <summary>Snapshot readers do not receive purchase, combat or lifecycle commands.</summary>
public interface ISessionView
{
    ShiftPhase Phase { get; }
    bool IsEndless { get; }
    int Shift { get; }
    int ContractLength { get; }
    int Wallet { get; }
    int ShiftEarnings { get; }
    int Stamina { get; }
    int Capacity { get; }
    int Damage { get; }
    int ShiftPigs { get; }
    float Radius { get; }
    Bill? NextBill { get; }
    ShiftReceipt? Receipt { get; }
    IReadOnlyList<PerkCard> Offers { get; }
    IReadOnlyList<RentDue> GetRentSchedule();
}

/// <summary>Each call creates fresh mutable state from the same immutable content.</summary>
public interface IGameFactory
{
    GameSession CreateSession();
    PigHerd CreateHerd(int shift = 1);
    SwingTiming Timing { get; }
}
