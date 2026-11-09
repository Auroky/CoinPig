using System;
using System.Collections.Generic;

public static class EquipmentContent
{
    public static WeaponDefinition Hammer { get; } = new("hammer", "敲击锤", new CircularWeaponAttack());
    public static WeaponDefinition Chainsaw { get; } = new("chainsaw", "电锯", new CircularWeaponAttack(
        extraDamage: 2, extraRadius: .30f, windup: .10f, recovery: .23f, aimLock: .02f), "chainsaw", "cut", "chainsaw");
    public static WeaponDefinition Grinder { get; } = new("grinder", "砂轮", new CircularWeaponAttack(
        extraDamage: 4, extraRadius: .60f, windup: .14f, recovery: .36f, aimLock: .03f), "grinder", "grind", "grinder");
    private static IReadOnlyList<EquipmentTier> Tiers(string noun, WeaponDefinition weapon, Bonus first, Bonus later) => Array.AsReadOnly(new[]
    {
        new EquipmentTier($"铜芯{noun}", new("dd783e", "ffd479", "e7cc94", "51372e", "ffac58"),
            StatsText(weapon, first), UpgradeBonus: first),
        new EquipmentTier($"钴钢{noun}", new("318dde", "7ce9ed", "c7ebf4", "173d68", "5cdff4"),
            StatsText(weapon, first.Add(later)), UpgradeBonus: later),
        new EquipmentTier($"星辉{noun}", new("ab52d9", "ffe0a0", "ead7ff", "4a245e", "ef95ff"),
            StatsText(weapon, first.Add(later).Add(later)), UpgradeBonus: later)
    });
    private static string StatsText(WeaponDefinition weapon, Bonus bonus)
    {
        var stats = weapon.Attack.Stats(BalanceContent.Tuning, bonus);
        return FormattableString.Invariant($"伤害 {stats.Damage} · 半径 {stats.Radius:0.00} · 体力 +{bonus.Stamina} · 每击耗能 {stats.StaminaCost}（不含护腕与技能）");
    }
    private static IReadOnlyList<EquipmentTier> Cuffs { get; } = Array.AsReadOnly(new[]
    {
        new EquipmentTier("鎏金振纹护腕", new("edaa37", "fff0a1", "d69b38", "6d4925", "ffda62"),
            "命中释放金波：范围 1 伤害，每局 5 次\n全武器体力 +1", Array.AsReadOnly(new[] {
                new CombatEffectDefinition("impact-gold", () => new ImpactResonance(1.85f, 1, 3.5f, 5)) })),
        new EquipmentTier("共振护腕", new("268fae", "8df0ec", "c4f4f0", "153b53", "75f9ec"),
            "金波 + 每 3 秒自动震裂 1 只、掉币\n每局 8 次自动共振，体力累计 +2", Array.AsReadOnly(new[] {
                new CombatEffectDefinition("impact-gold", () => new ImpactResonance(2.1f, 1, 3.5f, 5)),
                new CombatEffectDefinition("idle-resonance", () => new TimedResonance(2.1f, 1, 3, 8, 1)) })),
        new EquipmentTier("金鸣护腕", new("913bd3", "ffe582", "f1cb68", "432359", "f0b7ff"),
            "大范围金鸣：2 伤害，每局 6 次\n自动群体慢裂 8 次，体力累计 +3", Array.AsReadOnly(new[] {
                new CombatEffectDefinition("impact-gold", () => new ImpactResonance(3.2f, 2, 4, 6)),
                new CombatEffectDefinition("idle-resonance", () => new TimedResonance(2.4f, 1, 3.5f, 8, 3)) }))
    });
    public static IReadOnlyList<ShopProduct> Products { get; } = Array.AsReadOnly(new ShopProduct[]
    {
        new("Rim", "加宽锤面", "升级扩大范围、提高伤害与体力", 18, 6, 3,
            new(Radius: .12f, Stamina: 1), Weapon: Hammer,
            Tiers: Tiers("锤", Hammer, new(Radius: .12f, Stamina: 1), new(Radius: .12f, Stamina: 1, Damage: 1)), DisplayYaw: 45),
        new("Stamina", "共振护腕", "金色震荡 / 自动慢裂 / 群体金鸣\n每级体力 +1，与所有武器共存", 16, 12, 3,
            new(Stamina: 1), Appearance: "cuff", Tiers: Cuffs),
        new("Chainsaw", "电锯", "广域快切 · 升级提高伤害、范围和体力", 36, 32, 3,
            new(Radius: .12f, Recovery: .02f, Stamina: 1, Damage: 1), Appearance: "chainsaw", Stage: 2,
            Requirements: Array.AsReadOnly(new[] { new ProductRequirement("Rim", 2), new ProductRequirement("Stamina", 1) }),
            Weapon: Chainsaw, Tiers: Tiers("电锯", Chainsaw, new(Radius: .12f, Recovery: .02f, Stamina: 4),
                new(Radius: .12f, Recovery: .02f, Stamina: 1, Damage: 1))),
        new("Grinder", "砂轮", "大范围研磨 · 每击耗能 1，升级继续成长", 92, 60, 3,
            new(Radius: .12f, Stamina: 1, Damage: 1), Appearance: "grinder", Stage: 3,
            Requirements: Array.AsReadOnly(new[] { new ProductRequirement("Chainsaw", 2) }), Weapon: Grinder,
            Tiers: Tiers("砂轮", Grinder, new(Radius: .12f, Stamina: 7), new(Radius: .12f, Stamina: 1, Damage: 1)))
    });
}

