using System;
using System.Collections.Generic;

/// <summary>
/// 三选一能力的静态目录；已拥有能力和每次候选项由 PerkProgress 管理。
/// </summary>
public static class PerkContent
{
    public const string PairBonus = "PairBonus";
    public const string SecondWind = "SecondWind";
    public const string CoinValue = "CoinValue";
    public const string Lunch = "Lunch";
    public const string Precision = "Precision";
    public const string WideRim = "WideRim";
    public const int PairBonusCoins = 2;
    public const int PrecisionBonusCoins = 1;
    public const int CoinValueLimit = 10;
    public const int LunchStamina = 2;
    public const int SecondWindRefunds = 2;

    public const float PerkRimGain = 0.24f;

    public static IReadOnlyList<PerkCard> Cards { get; } = Array.AsReadOnly(new PerkCard[]
    {
        new(
            Id: PerkContent.PairBonus,
            Name: "双响赏金",
            Description: $"一锤击碎至少两只，额外赚 ¥{PairBonusCoins}。",
            Bonus: new(PairCoins: PairBonusCoins),
            Emblem: "pair"
        ),
        new(
            Id: PerkContent.SecondWind,
            Name: "借力回弹",
            Description: $"每局前 {SecondWindRefunds} 次一锤多碎，各返还 1 点体力。",
            Bonus: new(Refunds: SecondWindRefunds),
            Emblem: "return"
        ),
        new(
            Id: PerkContent.CoinValue,
            Name: "黄铜内胆",
            Description: $"每局前 {CoinValueLimit} 只击碎的猪各额外掉落 ¥1。",
            Bonus: new(BonusCoinLimit: CoinValueLimit),
            Emblem: "coins"
        ),
        new(
            Id: PerkContent.Lunch,
            Name: "工间加餐",
            Description: $"从下一局开始，每局多 {LunchStamina} 点体力。",
            Bonus: new(Stamina: LunchStamina),
            Emblem: "cup"
        ),
        new(
            Id: PerkContent.Precision,
            Name: "单响奖",
            Description: $"一锤恰好击碎一只，额外赚 ¥{PrecisionBonusCoins}。",
            Bonus: new(SingleCoins: PrecisionBonusCoins),
            Emblem: "target"
        ),
        new(
            Id: PerkContent.WideRim,
            Name: "宽边锤套",
            Description: $"落锤半径增加 {PerkRimGain:0.00}，圈边更容易够到猪。",
            Bonus: new(Radius: PerkRimGain),
            Emblem: "rim"
        )
    });
}
