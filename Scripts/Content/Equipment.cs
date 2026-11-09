using System;
using System.Collections.Generic;

public static class EquipmentContent
{
    public static WeaponDefinition Hammer { get; } = new("hammer", "敲击锤", new CircularWeaponAttack());
    public static WeaponDefinition Chainsaw { get; } = new("chainsaw", "电锯", new CircularWeaponAttack(
        extraDamage: 1, extraRadius: -.35f, windup: .10f, recovery: .23f, aimLock: .02f), "chainsaw", "cut");
    public static WeaponDefinition Grinder { get; } = new("grinder", "砂轮", new CircularWeaponAttack(
        extraDamage: 2, extraRadius: .50f, staminaCost: 2, windup: .14f, recovery: .36f, aimLock: .03f), "grinder", "grind");
    private static IReadOnlyList<EquipmentTier> Tiers(string noun) => Array.AsReadOnly(new[]
    {
        new EquipmentTier($"铜芯{noun}", new("dd783e", "ffd479", "e7cc94", "51372e", "ffac58")),
        new EquipmentTier($"钴钢{noun}", new("318dde", "7ce9ed", "c7ebf4", "173d68", "5cdff4")),
        new EquipmentTier($"星辉{noun}", new("ab52d9", "ffe0a0", "ead7ff", "4a245e", "ef95ff"))
    });
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
        new("Rim", "加宽锤面", "均衡敲击 · 每级范围 +0.18 · 每次耗能 1", 18, 14, 3,
            new(Radius: .18f), Weapon: Hammer, Tiers: Tiers("锤"), DisplayYaw: 45),
        new("Stamina", "共振护腕", "金色震荡 / 自动慢裂 / 群体金鸣\n每级体力 +1，护腕效果与所有武器共存", 16, 12, 3,
            new(Stamina: 1), Appearance: "cuff", Tiers: Cuffs),
        new("Chainsaw", "电锯", "窄域快切 · 基础伤害 2 · 每次耗能 1\n每级范围 +0.06、恢复 -0.02 秒", 52, 40, 3,
            new(Radius: .06f, Recovery: .02f), Appearance: "chainsaw", Stage: 2,
            Requirements: Array.AsReadOnly(new[] { new ProductRequirement("Rim", 2), new ProductRequirement("Stamina", 1) }),
            Weapon: Chainsaw, Tiers: Tiers("电锯")),
        new("Grinder", "砂轮", "广域研磨 · 基础伤害 3 · 每次耗能 2\n每级范围 +0.12，适合一击命中多只", 120, 80, 3,
            new(Radius: .12f), Appearance: "grinder", Stage: 3,
            Requirements: Array.AsReadOnly(new[] { new ProductRequirement("Chainsaw", 2) }), Weapon: Grinder, Tiers: Tiers("砂轮"))
    });
}
