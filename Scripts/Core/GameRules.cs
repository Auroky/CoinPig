using System;
using System.Collections.Generic;

public enum Perk { PairBonus, SecondWind, CoinValue, Lunch, Precision, WideRim }

public sealed record Bill(int Shift, int Amount);
public sealed record RentDue(int Day, int Amount, bool Paid, int DaysAway, bool Failed);
public sealed record PerkCard(Perk Kind, string Name, string Description);

public static class GameRules
{
    public const int StartingStamina = 12;
    public const int PairBonusCoins = 2;
    public const int PrecisionBonusCoins = 1;
    public const int CoinValueLimit = 10;
    public const int LunchStamina = 2;
    public const int SecondWindRefunds = 2;

    public const float PerkRimGain = 0.24f;
    public static IReadOnlyList<Bill> Bills { get; } = Array.AsReadOnly(new Bill[]
    {
        new(2, 28), new(4, 56), new(6, 84), new(8, 115), new(10, 136)
    });
    public static int ContractLength => Bills[^1].Shift;
    public static IReadOnlyList<PerkCard> Cards { get; } = Array.AsReadOnly(new PerkCard[]
    {
        new(Perk.PairBonus, "双响赏金", $"一锤击碎至少两只，额外赚 ¥{PairBonusCoins}。"),
        new(Perk.SecondWind, "借力回弹", $"每局前 {SecondWindRefunds} 次一锤多碎，各返还 1 点体力。"),
        new(Perk.CoinValue, "黄铜内胆", $"每局前 {CoinValueLimit} 只击碎的猪各额外掉落 ¥1。"),
        new(Perk.Lunch, "工间加餐", $"从下一局开始，每局多 {LunchStamina} 点体力。"),
        new(Perk.Precision, "单响奖", $"一锤恰好击碎一只，额外赚 ¥{PrecisionBonusCoins}。"),
        new(Perk.WideRim, "宽边锤套", $"落锤半径增加 {PerkRimGain:0.00}，圈边更容易够到猪。")
    });
}

