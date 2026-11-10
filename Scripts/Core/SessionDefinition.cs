using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 基础玩法参数：初始体力、伤害和世界空间攻击半径。
/// </summary>
public sealed record GameTuning(int StartingStamina = 12, int BaseDamage = 1, float BaseRadius = 1.43f);

/// <summary>
/// 攻击与瞄准节奏。时间字段均为秒，RetargetSpeed 使用每秒世界单位。
/// </summary>
public sealed record SwingTiming(float Windup = .19f, float Recovery = .39f, float MinimumRecovery = .12f,
    float SettlementDelay = .75f, float AimLockSeconds = .06f, float OpeningDelay = 0,
    float RetargetSettleSeconds = 0, float RetargetSpeed = 7);

/// <summary>
/// 会话的配置快照；构造时校验账单、商品、技能和能力，核心不依赖默认目录或引擎节点。
/// </summary>
public sealed class SessionDefinition
{
    public GameTuning Tuning { get; }
    public IReadOnlyList<Bill> Bills { get; }
    public IReadOnlyList<ShopProduct> Products { get; }
    public IReadOnlyList<SkillDefinition> Skills { get; }
    public IReadOnlyList<PerkCard> Perks { get; }
    public IReadOnlyList<ISwingRewardRule> RewardRules { get; }
    public WeaponDefinition StartingWeapon { get; }
    public IRentContinuation? RentContinuation { get; }
    public int ContractLength => Bills.Count == 0 ? 0 : Bills[^1].Shift;

    public SessionDefinition(GameTuning tuning, IEnumerable<Bill> bills, IEnumerable<ShopProduct> products,
        IEnumerable<SkillDefinition> skills, IEnumerable<PerkCard> perks, IEnumerable<ISwingRewardRule> rewardRules,
        WeaponDefinition startingWeapon, IRentContinuation? rentContinuation = null)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        if (tuning.StartingStamina <= 0 || tuning.BaseDamage <= 0 ||
            !float.IsFinite(tuning.BaseRadius) || tuning.BaseRadius <= 0)
        {
            throw new ArgumentException("Starting values must be finite and positive.", nameof(tuning));
        }

        Tuning = tuning;
        ArgumentNullException.ThrowIfNull(startingWeapon);
        if (startingWeapon.Attack == null || string.IsNullOrWhiteSpace(startingWeapon.Id))
        {
            throw new ArgumentException("A starting weapon needs an identity and behavior.", nameof(startingWeapon));
        }

        StartingWeapon = startingWeapon;
        RentContinuation = rentContinuation;
        Bills = Snapshot(bills);
        Products = Snapshot(products);
        Skills = Snapshot(skills);
        Perks = Snapshot(perks);
        RewardRules = Snapshot(rewardRules);
        _ = new RentContract(Bills, RentContinuation);
        _ = new ShopInventory(Products);
        _ = new SkillProgress(Skills);
        _ = new PerkProgress(Perks, new Random(0));
        _ = new SwingRewards(RewardRules);
    }

    private static IReadOnlyList<T> Snapshot<T>(IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Array.AsReadOnly(values.ToArray());
    }
}
