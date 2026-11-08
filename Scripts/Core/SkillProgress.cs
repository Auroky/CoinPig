using System.Collections.Generic;
using System.Linq;

public enum SkillStatus { Available, Learned, Locked, InsufficientFunds, Closed, UnknownSkill }
public sealed record SkillOffer(SkillDefinition Skill, SkillStatus Status);
public sealed record SkillResult(bool Succeeded, SkillStatus Status, int CashSpent = 0);

public interface ISkillTree
{
    IReadOnlyList<SkillOffer> GetSkills();
    SkillResult TryLearn(string id);
}

/// <summary>本局技能状态。升级校验前置和金币，由 GameSession 统一扣款，新的 GameSession 会创建全新状态。</summary>
public sealed class SkillProgress
{
    private readonly HashSet<string> _learned = new();
    public SkillBonus TotalBonus { get; private set; } = new();

    private SkillStatus Status(SkillDefinition skill, bool open, int wallet)
    {
        if (_learned.Contains(skill.Id)) return SkillStatus.Learned;
        if (!open) return SkillStatus.Closed;
        if (skill.ParentId != null && !_learned.Contains(skill.ParentId)) return SkillStatus.Locked;
        if (wallet < skill.Cost) return SkillStatus.InsufficientFunds;
        return SkillStatus.Available;
    }

    public IReadOnlyList<SkillOffer> Quote(bool open, int wallet) =>
        SkillCatalog.Nodes.Select(skill => new SkillOffer(skill, Status(skill, open, wallet))).ToArray();

    internal SkillResult TryLearn(string id, bool open, int wallet)
    {
        var skill = SkillCatalog.Nodes.FirstOrDefault(node => node.Id == id);
        if (skill == null) return new(false, SkillStatus.UnknownSkill);
        var status = Status(skill, open, wallet);
        if (status != SkillStatus.Available)
            return new(false, status);
        _learned.Add(id);
        TotalBonus = TotalBonus.Add(skill.Bonus);
        return new(true, SkillStatus.Learned, skill.Cost);
    }
}
