using System;
using System.Collections.Generic;

/// <summary>
/// 默认回合、体力、攻击节奏和租金配置。这里只声明数据，结算规则由核心层执行。
/// </summary>
public static class BalanceContent
{
    public static GameTuning Tuning { get; } = new(StartingStamina: 14, BaseDamage: 1, BaseRadius: 1.43f);
    public static SwingTiming Timing { get; } = new(
        Windup: .19f,
        Recovery: .39f,
        MinimumRecovery: .12f,
        SettlementDelay: .75f,
        AimLockSeconds: .06f,
        OpeningDelay: 1.0f,
        RetargetSettleSeconds: .10f,
        RetargetSpeed: 7
    );
    public static IReadOnlyList<Bill> Bills { get; } = Array.AsReadOnly(new Bill[]
    {
        new(Shift: 2, Amount: 28), new(Shift: 4, Amount: 56), new(Shift: 6, Amount: 84), new(Shift: 8, Amount: 128), new(Shift: 10, Amount: 156)
    });
}
