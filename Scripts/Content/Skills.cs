using System;
using System.Collections.Generic;

public static class SkillContent
{
    public static SkillBranch Hammer { get; } = new("hammer", "锤法", "477681", 0, "hammer");
    public static SkillBranch Heavy { get; } = new("heavy", "重击", "9b754a", 1, "hammer");
    public static SkillBranch Stamina { get; } = new("stamina", "耐力", "647956", 2, "cuff");
    public static SkillBranch Income { get; } = new("income", "收益", "9b6550", 3, "coins");
    public static IReadOnlyList<SkillDefinition> Nodes { get; } = Array.AsReadOnly(new SkillDefinition[]
    {
        new("wide", "宽面练习", "落锤半径 +0.10", null, SkillContent.Hammer, 0, 8, new(Radius: 0.10f)),
        new("quick", "轻巧手法", "落锤后的恢复时间缩短 0.04 秒", "wide", SkillContent.Hammer, 1, 8, new(Recovery: 0.04f)),
        new("rebound", "借势回弹", "每局首次一锤多碎返还 1 点体力，可与随机能力叠加", "quick", SkillContent.Hammer, 2, 12, new(Refunds: 1)),
        new("power1", "重击练习 I", "伤害 +1，铁皮猪从三锤变成两锤", "wide", SkillContent.Heavy, 1, 22, new(Damage: 1)),
        new("power2", "重击练习 II", "伤害再 +1，满耐久铁皮猪也可一锤击碎", "power1", SkillContent.Heavy, 2, 80, new(Damage: 1)),
        new("endurance", "耐力练习", "每局体力上限 +1", null, SkillContent.Stamina, 0, 8, new(Stamina: 1)),
        new("breath", "呼吸节奏", "每局体力上限再 +1", "endurance", SkillContent.Stamina, 1, 24, new(Stamina: 1)),
        new("reserve", "充沛精力", "每局体力上限再 +1", "breath", SkillContent.Stamina, 2, 36, new(Stamina: 1)),
        new("pairs", "双响红利", "每次一锤至少击碎两只，额外赚 ¥1", null, SkillContent.Income, 0, 10, new(PairCoins: 1)),
        new("coins", "收集习惯", "每只击碎的猪额外掉落 ¥1", "pairs", SkillContent.Income, 1, 52, new(PigCoins: 1)),
        new("jackpot", "满堂彩", "每次一锤至少击碎三只，额外赚 ¥4", "coins", SkillContent.Income, 2, 36, new(TripleCoins: 4))
    });
}


