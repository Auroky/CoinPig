public sealed record SkillBranch(string Id, string Name, string Color = "477681", int Order = 0, string Icon = "hammer");
public sealed record SkillDefinition(string Id, string Name, string Description, string? ParentId,
    SkillBranch Branch, int Tier, int Cost, Bonus Bonus);