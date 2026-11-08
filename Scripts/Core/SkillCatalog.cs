using System;
using System.Collections.Generic;

public sealed record SkillBonus(int Stamina = 0, float Radius = 0, float Recovery = 0,
    int Refunds = 0, int PairCoins = 0, int PigCoins = 0, int TripleCoins = 0, int Damage = 0)
{
    public SkillBonus Add(SkillBonus other) => new(
        Stamina + other.Stamina, Radius + other.Radius, Recovery + other.Recovery,
        Refunds + other.Refunds, PairCoins + other.PairCoins, PigCoins + other.PigCoins, TripleCoins + other.TripleCoins, Damage + other.Damage);
}

public enum SkillBranch { Hammer, Stamina, Income, Heavy }

public sealed record SkillDefinition(string Id, string Name, string Description, string? ParentId,
    SkillBranch Branch, int Tier, int Cost, SkillBonus Bonus);

public static class SkillCatalog
{
    public static IReadOnlyList<SkillDefinition> Nodes { get; } = Array.AsReadOnly(new SkillDefinition[]
    {
        new("wide", "宽面练习", "落锤半径 +0.10", null, SkillBranch.Hammer, 0, 8, new(Radius: 0.10f)),
        new("quick", "轻巧手法", "落锤后的恢复时间缩短 0.04 秒", "wide", SkillBranch.Hammer, 1, 8, new(Recovery: 0.04f)),
        new("rebound", "借势回弹", "每局首次一锤多碎返还 1 点体力，可与随机能力叠加", "quick", SkillBranch.Hammer, 2, 12, new(Refunds: 1)),
        new("power1", "重击练习 I", "伤害 +1，铁皮猪从三锤变成两锤", "wide", SkillBranch.Heavy, 1, 22, new(Damage: 1)),
        new("power2", "重击练习 II", "伤害再 +1，满耐久铁皮猪也可一锤击碎", "power1", SkillBranch.Heavy, 2, 80, new(Damage: 1)),
        new("endurance", "耐力练习", "每局体力上限 +1", null, SkillBranch.Stamina, 0, 8, new(Stamina: 1)),
        new("breath", "呼吸节奏", "每局体力上限再 +2", "endurance", SkillBranch.Stamina, 1, 24, new(Stamina: 2)),
        new("reserve", "充沛精力", "每局体力上限再 +2", "breath", SkillBranch.Stamina, 2, 36, new(Stamina: 2)),
        new("pairs", "双响红利", "每次一锤至少击碎两只，额外赚 ¥1", null, SkillBranch.Income, 0, 10, new(PairCoins: 1)),
        new("coins", "收集习惯", "每只击碎的猪额外掉落 ¥1", "pairs", SkillBranch.Income, 1, 52, new(PigCoins: 1)),
        new("jackpot", "满堂彩", "每次一锤至少击碎三只，额外赚 ¥4", "coins", SkillBranch.Income, 2, 36, new(TripleCoins: 4))
    });
}

