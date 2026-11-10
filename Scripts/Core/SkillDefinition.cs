/// <summary>
/// 技能分支的展示身份和排序信息，与具体学习状态无关。
/// </summary>
public sealed record SkillBranch(string Id, string Name, string Color = "477681", int Order = 0, string Icon = "hammer");

/// <summary>
/// 单个技能的成本、前置和加成配置；不保存玩家是否已学习。
/// </summary>
public sealed record SkillDefinition(string Id, string Name, string Description, string? ParentId,
    SkillBranch Branch, int Tier, int Cost, Bonus Bonus);
