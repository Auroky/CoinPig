using System;
using System.Collections.Generic;
using System.Linq;

public enum SkillStatus { Available, Learned, Locked, InsufficientFunds, Closed, UnknownSkill }
public sealed record SkillOffer(SkillDefinition Skill, SkillStatus Status);
public sealed record SkillResult(bool Succeeded, SkillStatus Status, int CashSpent = 0);

/// <summary>Read-only offers used by skill displays.</summary>
public interface ISkillTreeView
{
    IReadOnlyList<SkillOffer> GetSkills();
}

/// <summary>Learning succeeds once; a rejected command preserves all state and spends no cash.</summary>
public interface ISkillTree : ISkillTreeView
{
    SkillResult TryLearn(string id);
}

/// <summary>本局技能状态。升级校验前置和金币，由 GameSession 统一扣款，新的 GameSession 会创建全新状态。</summary>
public sealed class SkillProgress
{
    private readonly HashSet<string> _learned = new();
    private readonly IReadOnlyList<SkillDefinition> _catalog;
    public Bonus TotalBonus { get; private set; } = new();

    public SkillProgress(IReadOnlyList<SkillDefinition> catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var snapshot = catalog.ToArray();
        if (snapshot.Any(skill => skill == null || string.IsNullOrWhiteSpace(skill.Id) ||
            skill.Cost < 0 || skill.Bonus == null || !skill.Bonus.IsValid || skill.Branch == null || string.IsNullOrWhiteSpace(skill.Branch.Id) || skill.Tier < 0) ||
            snapshot.Select(skill => skill.Id).Distinct().Count() != snapshot.Length)
            throw new ArgumentException("Skills need unique IDs, nonnegative costs and finite bonuses.", nameof(catalog));
        var byId = snapshot.ToDictionary(skill => skill.Id);
        foreach (var skill in snapshot)
        {
            var visited = new HashSet<string> { skill.Id };
            var parent = skill.ParentId;
            while (parent != null)
            {
                if (!byId.TryGetValue(parent, out var prerequisite) || !visited.Add(parent))
                    throw new ArgumentException("Skill prerequisites must exist and form an acyclic graph.", nameof(catalog));
                parent = prerequisite.ParentId;
            }
        }
        _catalog = Array.AsReadOnly(snapshot);
    }

    private SkillStatus Status(SkillDefinition skill, bool open, int wallet)
    {
        if (_learned.Contains(skill.Id)) return SkillStatus.Learned;
        if (!open) return SkillStatus.Closed;
        if (skill.ParentId != null && !_learned.Contains(skill.ParentId)) return SkillStatus.Locked;
        if (wallet < skill.Cost) return SkillStatus.InsufficientFunds;
        return SkillStatus.Available;
    }

    public IReadOnlyList<SkillOffer> Quote(bool open, int wallet) =>
        _catalog.Select(skill => new SkillOffer(skill, Status(skill, open, wallet))).ToArray();

    internal SkillResult TryLearn(string id, bool open, int wallet)
    {
        var skill = _catalog.FirstOrDefault(node => node.Id == id);
        if (skill == null) return new(false, SkillStatus.UnknownSkill);
        var status = Status(skill, open, wallet);
        if (status != SkillStatus.Available)
            return new(false, status);
        var updated = TotalBonus.Add(skill.Bonus);
        _learned.Add(id);
        TotalBonus = updated;
        return new(true, SkillStatus.Learned, skill.Cost);
    }
}
