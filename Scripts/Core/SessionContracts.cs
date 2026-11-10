using System.Collections.Generic;

/// <summary>
/// 界面需要的会话只读信息；不暴露购买、攻击或推进回合的方法。
/// </summary>
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

/// <summary>
/// 创建独立会话与猪群的能力。相同内容可由不同表现复用，每次创建不共享可变状态。
/// </summary>
public interface IGameFactory
{
    GameSession CreateSession();
    PigHerd CreateHerd(int shift = 1);
    SwingTiming Timing { get; }
}
