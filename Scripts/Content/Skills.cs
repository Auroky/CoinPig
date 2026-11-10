using System;
using System.Collections.Generic;

/// <summary>
/// 技能分支、前置关系、价格和加成目录；学习状态由 SkillProgress 独立保存。
/// </summary>
public static class SkillContent
{
    public static SkillBranch Hammer { get; } = new(
        Id: "hammer",
        Name: "锤法",
        Color: "477681",
        Order: 0,
        Icon: "hammer"
    );
    public static SkillBranch Heavy { get; } = new(
        Id: "heavy",
        Name: "重击",
        Color: "9b754a",
        Order: 1,
        Icon: "hammer"
    );
    public static SkillBranch Stamina { get; } = new(
        Id: "stamina",
        Name: "耐力",
        Color: "647956",
        Order: 2,
        Icon: "cuff"
    );
    public static SkillBranch Income { get; } = new(
        Id: "income",
        Name: "收益",
        Color: "9b6550",
        Order: 3,
        Icon: "coins"
    );
    public static IReadOnlyList<SkillDefinition> Nodes { get; } = Array.AsReadOnly(new SkillDefinition[]
    {
        new(
            Id: "wide",
            Name: "宽面练习",
            Description: "落锤半径 +0.10",
            ParentId: null,
            Branch: SkillContent.Hammer,
            Tier: 0,
            Cost: 8,
            Bonus: new(Radius: 0.10f)
        ),
        new(
            Id: "quick",
            Name: "轻巧手法",
            Description: "落锤后的恢复时间缩短 0.04 秒",
            ParentId: "wide",
            Branch: SkillContent.Hammer,
            Tier: 1,
            Cost: 8,
            Bonus: new(Recovery: 0.04f)
        ),
        new(
            Id: "rebound",
            Name: "借势回弹",
            Description: "每局首次一锤多碎返还 1 点体力，可与随机能力叠加",
            ParentId: "quick",
            Branch: SkillContent.Hammer,
            Tier: 2,
            Cost: 12,
            Bonus: new(Refunds: 1)
        ),
        new(
            Id: "power1",
            Name: "重击练习 I",
            Description: "伤害 +1，减少击碎高耐久猪所需的攻击次数",
            ParentId: "wide",
            Branch: SkillContent.Heavy,
            Tier: 1,
            Cost: 22,
            Bonus: new(Damage: 1)
        ),
        new(
            Id: "power2",
            Name: "重击练习 II",
            Description: "伤害再 +1，可与当前武器的伤害叠加",
            ParentId: "power1",
            Branch: SkillContent.Heavy,
            Tier: 2,
            Cost: 80,
            Bonus: new(Damage: 1)
        ),
        new(
            Id: "endurance",
            Name: "耐力练习",
            Description: "每局体力上限 +1",
            ParentId: null,
            Branch: SkillContent.Stamina,
            Tier: 0,
            Cost: 8,
            Bonus: new(Stamina: 1)
        ),
        new(
            Id: "breath",
            Name: "呼吸节奏",
            Description: "每局体力上限再 +1",
            ParentId: "endurance",
            Branch: SkillContent.Stamina,
            Tier: 1,
            Cost: 24,
            Bonus: new(Stamina: 1)
        ),
        new(
            Id: "reserve",
            Name: "充沛精力",
            Description: "每局体力上限再 +1",
            ParentId: "breath",
            Branch: SkillContent.Stamina,
            Tier: 2,
            Cost: 36,
            Bonus: new(Stamina: 1)
        ),
        new(
            Id: "pairs",
            Name: "双响红利",
            Description: "每次一锤至少击碎两只，额外赚 ¥1",
            ParentId: null,
            Branch: SkillContent.Income,
            Tier: 0,
            Cost: 10,
            Bonus: new(PairCoins: 1)
        ),
        new(
            Id: "coins",
            Name: "收集习惯",
            Description: "每只击碎的猪额外掉落 ¥1",
            ParentId: "pairs",
            Branch: SkillContent.Income,
            Tier: 1,
            Cost: 52,
            Bonus: new(PigCoins: 1)
        ),
        new(
            Id: "jackpot",
            Name: "满堂彩",
            Description: "每次一锤至少击碎三只，额外赚 ¥4",
            ParentId: "coins",
            Branch: SkillContent.Income,
            Tier: 2,
            Cost: 36,
            Bonus: new(TripleCoins: 4)
        )
    });
}
