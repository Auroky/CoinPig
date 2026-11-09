using System;
using System.Collections.Generic;
using System.Linq;

public sealed record GameTuning(int StartingStamina = 12, int BaseDamage = 1, float BaseRadius = 1.43f);
public sealed record SwingTiming(float Windup = .19f, float Recovery = .39f, float MinimumRecovery = .12f,
    float SettlementDelay = .75f, float AimLockSeconds = .06f, float OpeningDelay = 0,
    float RetargetSettleSeconds = 0, float RetargetSpeed = 7);

/// <summary>Immutable session inputs. The domain never looks up built-in game content.</summary>
public sealed class SessionDefinition
{
    public GameTuning Tuning { get; }
    public IReadOnlyList<Bill> Bills { get; }
    public IReadOnlyList<ShopProduct> Products { get; }
    public IReadOnlyList<SkillDefinition> Skills { get; }
    public IReadOnlyList<PerkCard> Perks { get; }
    public IReadOnlyList<ISwingRewardRule> RewardRules { get; }
    public WeaponDefinition StartingWeapon { get; }
    public int ContractLength => Bills.Count == 0 ? 0 : Bills[^1].Shift;

    public SessionDefinition(GameTuning tuning, IEnumerable<Bill> bills, IEnumerable<ShopProduct> products,
        IEnumerable<SkillDefinition> skills, IEnumerable<PerkCard> perks, IEnumerable<ISwingRewardRule> rewardRules,
        WeaponDefinition startingWeapon)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        if (tuning.StartingStamina <= 0 || tuning.BaseDamage <= 0 ||
            !float.IsFinite(tuning.BaseRadius) || tuning.BaseRadius <= 0)
            throw new ArgumentException("Starting values must be finite and positive.", nameof(tuning));
        Tuning = tuning;
        ArgumentNullException.ThrowIfNull(startingWeapon);
        if (startingWeapon.Attack == null || string.IsNullOrWhiteSpace(startingWeapon.Id))
            throw new ArgumentException("A starting weapon needs an identity and behavior.", nameof(startingWeapon));
        StartingWeapon = startingWeapon;
        Bills = Snapshot(bills); Products = Snapshot(products); Skills = Snapshot(skills);
        Perks = Snapshot(perks); RewardRules = Snapshot(rewardRules);
        _ = new RentContract(Bills); _ = new ShopInventory(Products);
        _ = new SkillProgress(Skills); _ = new PerkProgress(Perks, new Random(0));
        _ = new SwingRewards(RewardRules);
    }

    private static IReadOnlyList<T> Snapshot<T>(IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Array.AsReadOnly(values.ToArray());
    }
}
