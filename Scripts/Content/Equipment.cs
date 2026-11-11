using System;
using System.Collections.Generic;

/// <summary>
/// 装备目录：价格、解锁前置、各级效果和外观。武器行为与护腕效果通过配置组合。
/// </summary>
public static class EquipmentContent
{
    public static WeaponDefinition Hammer { get; } = new("hammer", "敲击锤", new CircularWeaponAttack());
    public static WeaponDefinition Chainsaw { get; } = new(
        "chainsaw",
        "电锯",
        new CircularWeaponAttack(
            extraDamage: 2, extraRadius: .30f, windup: .10f, recovery: .23f, aimLock: .02f),
        "chainsaw",
        "cut",
        "chainsaw"
    );
    public static WeaponDefinition Grinder { get; } = new(
        "grinder",
        "砂轮",
        new CircularWeaponAttack(
            extraDamage: 4, extraRadius: .60f, windup: .14f, recovery: .36f, aimLock: .03f),
        "grinder",
        "grind",
        "grinder"
    );

    // 第一级增量与后续每级增量分别配置，商品描述与实际属性使用同一份数据计算。
    private static IReadOnlyList<EquipmentTier> Tiers(string noun, WeaponDefinition weapon, Bonus first, Bonus later) => Array.AsReadOnly(new[]
    {
        new EquipmentTier(
            $"铜芯{noun}",
            new(
                Body: "dd783e",
                Trim: "ffd479",
                Metal: "e7cc94",
                Grip: "51372e",
                Glow: "ffac58"
            ),
            StatsText(weapon, first),
            upgradeBonus: first
        ),
        new EquipmentTier(
            $"钴钢{noun}",
            new(
                Body: "318dde",
                Trim: "7ce9ed",
                Metal: "c7ebf4",
                Grip: "173d68",
                Glow: "5cdff4"
            ),
            StatsText(weapon, first.Add(later)),
            upgradeBonus: later
        ),
        new EquipmentTier(
            $"星辉{noun}",
            new(
                Body: "ab52d9",
                Trim: "ffe0a0",
                Metal: "ead7ff",
                Grip: "4a245e",
                Glow: "ef95ff"
            ),
            StatsText(weapon, first.Add(later).Add(later)),
            upgradeBonus: later
        )
    });

    private static FormattableString StatsText(WeaponDefinition weapon, Bonus bonus)
    {
        var stats = weapon.Attack.Stats(BalanceContent.Tuning, bonus);
        return $"伤害 {stats.Damage} · 半径 {stats.Radius:0.00} · 体力 +{bonus.Stamina} · 每击耗能 {stats.StaminaCost}（不含护腕与技能）";
    }

    private static IReadOnlyList<EquipmentTier> Cuffs { get; } = Array.AsReadOnly(new[]
    {
        new EquipmentTier(
            "鎏金振纹护腕",
            new(
                Body: "edaa37",
                Trim: "fff0a1",
                Metal: "d69b38",
                Grip: "6d4925",
                Glow: "ffda62"
            ),
            "命中释放金波：范围 1 伤害，每局 5 次\n全武器体力 +1",
            Array.AsReadOnly(new[] {
                new CombatEffectDefinition("impact-gold", () => new ImpactResonance(1.85f, 1, 3.5f, 5)) })
        ),
        new EquipmentTier(
            "共振护腕",
            new(
                Body: "268fae",
                Trim: "8df0ec",
                Metal: "c4f4f0",
                Grip: "153b53",
                Glow: "75f9ec"
            ),
            "金波 + 每 3 秒自动震裂 1 只、掉币\n每局 8 次自动共振，体力累计 +2",
            Array.AsReadOnly(new[] {
                new CombatEffectDefinition("impact-gold", () => new ImpactResonance(2.1f, 1, 3.5f, 5)),
                new CombatEffectDefinition("idle-resonance", () => new TimedResonance(2.1f, 1, 3, 8, 1)) })
        ),
        new EquipmentTier(
            "金鸣护腕",
            new(
                Body: "913bd3",
                Trim: "ffe582",
                Metal: "f1cb68",
                Grip: "432359",
                Glow: "f0b7ff"
            ),
            "大范围金鸣：2 伤害，每局 6 次\n自动群体慢裂 8 次，体力累计 +3",
            Array.AsReadOnly(new[] {
                new CombatEffectDefinition("impact-gold", () => new ImpactResonance(3.2f, 2, 4, 6)),
                new CombatEffectDefinition("idle-resonance", () => new TimedResonance(2.4f, 1, 3.5f, 8, 3)) })
        )
    });
    public static IReadOnlyList<ShopProduct> Products { get; } = Array.AsReadOnly(new ShopProduct[]
    {
        new(
            Id: "Rim",
            Name: "加宽锤面",
            Description: "升级扩大范围、提高伤害与体力",
            BasePrice: 18,
            PriceStep: 6,
            MaxLevel: 3,
            Bonus: new(Radius: .11f, Stamina: 1),
            Weapon: Hammer,
            Tiers: Tiers("锤", Hammer, new(Radius: .11f, Stamina: 1), new(Radius: .11f, Stamina: 1, Damage: 1)),
            DisplayYaw: 45
        ),
        new(
            Id: "Stamina",
            Name: "共振护腕",
            Description: "金色震荡 / 自动慢裂 / 群体金鸣\n每级体力 +1，与所有武器共存",
            BasePrice: 16,
            PriceStep: 12,
            MaxLevel: 3,
            Bonus: new(Stamina: 1),
            Appearance: "cuff",
            Tiers: Cuffs
        ),
        new(
            Id: "Chainsaw",
            Name: "电锯",
            Description: "广域快切 · 升级扩大范围、提高体力",
            BasePrice: 36,
            PriceStep: 20,
            MaxLevel: 3,
            Bonus: new(Radius: .11f, Recovery: .02f, Stamina: 1),
            Appearance: "chainsaw",
            Stage: 2,
            Requirements: Array.AsReadOnly(new[] { new ProductRequirement("Rim", 2), new ProductRequirement("Stamina", 1) }),
            Weapon: Chainsaw,
            Tiers: Tiers("电锯", Chainsaw, new(Radius: .11f, Recovery: .02f, Stamina: 4),
                new(Radius: .11f, Recovery: .02f, Stamina: 1))
        ),
        new(
            Id: "Grinder",
            Name: "砂轮",
            Description: "重型研磨 · 升级扩大范围、提高体力",
            BasePrice: 92,
            PriceStep: 40,
            MaxLevel: 3,
            Bonus: new(Radius: .11f, Stamina: 1),
            Appearance: "grinder",
            Stage: 3,
            Requirements: Array.AsReadOnly(new[] { new ProductRequirement("Chainsaw", 2) }),
            Weapon: Grinder,
            Tiers: Tiers("砂轮", Grinder, new(Radius: .11f, Stamina: 7), new(Radius: .11f, Stamina: 1))
        )
    });
}
