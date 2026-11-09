using System;
using System.Collections.Generic;

public static class PerkContent
{
    public const string PairBonus = "PairBonus", SecondWind = "SecondWind", CoinValue = "CoinValue",
        Lunch = "Lunch", Precision = "Precision", WideRim = "WideRim";
    public const int PairBonusCoins = 2;
    public const int PrecisionBonusCoins = 1;
    public const int CoinValueLimit = 10;
    public const int LunchStamina = 2;
    public const int SecondWindRefunds = 2;

    public const float PerkRimGain = 0.24f;
    public static IReadOnlyList<PerkCard> Cards { get; } = Array.AsReadOnly(new PerkCard[]
    {
        new(PerkContent.PairBonus, "双响赏金", $"一锤击碎至少两只，额外赚 ¥{PairBonusCoins}。", new(PairCoins: PairBonusCoins), "pair"),
        new(PerkContent.SecondWind, "借力回弹", $"每局前 {SecondWindRefunds} 次一锤多碎，各返还 1 点体力。", new(Refunds: SecondWindRefunds), "return"),
        new(PerkContent.CoinValue, "黄铜内胆", $"每局前 {CoinValueLimit} 只击碎的猪各额外掉落 ¥1。", new(BonusCoinLimit: CoinValueLimit), "coins"),
        new(PerkContent.Lunch, "工间加餐", $"从下一局开始，每局多 {LunchStamina} 点体力。", new(Stamina: LunchStamina), "cup"),
        new(PerkContent.Precision, "单响奖", $"一锤恰好击碎一只，额外赚 ¥{PrecisionBonusCoins}。", new(SingleCoins: PrecisionBonusCoins), "target"),
        new(PerkContent.WideRim, "宽边锤套", $"落锤半径增加 {PerkRimGain:0.00}，圈边更容易够到猪。", new(Radius: PerkRimGain), "rim")
    });
}

