using System.Collections.Generic;

/// <summary>
/// 技能报价或学习命令的状态，供规则判断和界面提示共用。
/// </summary>
public enum SkillStatus
{
    Available, Learned, Locked, InsufficientFunds, Closed, UnknownSkill
}

/// <summary>
/// 技能及其当前可用状态的只读报价。
/// </summary>
public sealed record SkillOffer(SkillDefinition Skill, SkillStatus Status);

/// <summary>
/// 学习结果与扣款金额；失败时金额为零且不改变学习状态。
/// </summary>
public sealed record SkillResult(bool Succeeded, SkillStatus Status, int CashSpent = 0);

/// <summary>
/// 技能界面的只读能力；只展示技能时无需依赖学习命令。
/// </summary>
public interface ISkillTreeView
{
    IReadOnlyList<SkillOffer> GetSkills();
}

/// <summary>
/// 学习技能的命令接口；成功只能发生一次，拒绝时保持状态和金币不变。
/// </summary>
public interface ISkillTree : ISkillTreeView
{
    SkillResult TryLearn(string id);
}
