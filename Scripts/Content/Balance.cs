using System;
using System.Collections.Generic;

public static class BalanceContent
{
    public static GameTuning Tuning { get; } = new(StartingStamina: 14, BaseDamage: 1, BaseRadius: 1.43f);
    public static SwingTiming Timing { get; } = new(Windup: .19f, Recovery: .39f, MinimumRecovery: .12f,
        SettlementDelay: .75f, AimLockSeconds: .06f, OpeningDelay: 1.0f, RetargetSettleSeconds: .10f, RetargetSpeed: 7);
    public static IReadOnlyList<Bill> Bills { get; } = Array.AsReadOnly(new Bill[]
    {
        new(2, 28), new(4, 56), new(6, 84), new(8, 128), new(10, 156)
    });
}
