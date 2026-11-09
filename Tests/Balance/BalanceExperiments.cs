using System;
using System.Collections.Generic;
using System.Linq;

internal sealed record BalanceVariant(string Name, IReadOnlyList<ShopProduct> Products, IReadOnlyList<Bill>? Bills = null);
internal static class BalanceExperiments
{
    private static IReadOnlyList<Bill> PreviousBills => new[] { new Bill(2, 28), new(4, 56), new(6, 84), new(8, 115), new(10, 136) };
    public static IEnumerable<BalanceVariant> Variants()
    {
        yield return new("previous", PreviousProducts(), PreviousBills);
        yield return Make("reach-only", reach: true);
        yield return Make("stamina-only", stamina: true);
        yield return Make("resonance-only", resonance: true);
        yield return Make("reach-stamina", reach: true, stamina: true);
        yield return Make("combined", reach: true, stamina: true, resonance: true);
        yield return Make("combined-late-mild", reach: true, stamina: true, resonance: true) with
            { Bills = new[] { new Bill(2, 28), new(4, 56), new(6, 84), new(8, 130), new(10, 160) } };
        yield return Make("combined-late", reach: true, stamina: true, resonance: true) with
            { Bills = new[] { new Bill(2, 28), new(4, 56), new(6, 84), new(8, 135), new(10, 172) } };
        yield return Make("reach-stamina-late", reach: true, stamina: true) with
            { Bills = new[] { new Bill(2, 28), new(4, 56), new(6, 84), new(8, 135), new(10, 172) } };
        yield return Make("reach-stamina-late-mild", reach: true, stamina: true) with
            { Bills = new[] { new Bill(2, 28), new(4, 56), new(6, 84), new(8, 130), new(10, 160) } };
        yield return Make("reach-stamina-late-gentle", reach: true, stamina: true) with
            { Bills = new[] { new Bill(2, 28), new(4, 56), new(6, 84), new(8, 122), new(10, 148) } };
        yield return Make("combined-late-gentle", reach: true, stamina: true, resonance: true) with
            { Bills = new[] { new Bill(2, 28), new(4, 56), new(6, 84), new(8, 122), new(10, 148) } };
        yield return Make("reach-stamina-late-balanced", reach: true, stamina: true) with
            { Bills = new[] { new Bill(2, 28), new(4, 56), new(6, 84), new(8, 128), new(10, 156) } };
    }
    // Reconstruct the prior release's changed values so comparisons remain reproducible after tuning content.
    private static IReadOnlyList<ShopProduct> PreviousProducts() => EquipmentContent.Products.Select(p =>
    {
        if (p.Weapon == null) return p with { Bonus = p.Bonus with { Stamina = 2 },
            Tiers = p.Tiers!.Select((t, i) => t with { Effects = PreviousEffects(i) }).ToArray() };
        float gain = p.Id == "Rim" ? .22f : .25f;
        var weapon = p.Id switch
        {
            "Chainsaw" => p.Weapon with { Attack = new CircularWeaponAttack(extraDamage: 2, extraRadius: .47f,
                windup: .10f, recovery: .23f, aimLock: .02f) },
            "Grinder" => p.Weapon with { Attack = new CircularWeaponAttack(extraDamage: 4, extraRadius: 1.17f,
                windup: .14f, recovery: .36f, aimLock: .03f) },
            _ => p.Weapon
        };
        return p with { Weapon = weapon, Bonus = p.Bonus with { Radius = gain },
            Tiers = p.Tiers!.Select(t => t with { UpgradeBonus = t.UpgradeBonus! with { Radius = gain } }).ToArray() };
    }).ToArray();
    private static IReadOnlyList<CombatEffectDefinition> PreviousEffects(int tier) => tier switch
    {
        0 => new[] { new CombatEffectDefinition("impact-gold", () => new ImpactResonance(1.85f, 1, 3.5f, 5)) },
        1 => new[] { new CombatEffectDefinition("impact-gold", () => new ImpactResonance(2.1f, 1, 3.5f, 5)),
            new CombatEffectDefinition("idle-resonance", () => new TimedResonance(2.1f, 1, 3, 8, 1)) },
        _ => new[] { new CombatEffectDefinition("impact-gold", () => new ImpactResonance(3.2f, 2, 4, 6)),
            new CombatEffectDefinition("idle-resonance", () => new TimedResonance(2.4f, 1, 3.5f, 8, 3)) }
    };
    private static BalanceVariant Make(string name, bool reach = false, bool stamina = false, bool resonance = false)
    {
        var products = PreviousProducts().Select(p =>
        {
            var result = p;
            if (reach && p.Weapon != null)
            {
                var weapon = p.Id switch
                {
                    "Rim" => p.Weapon,
                    "Chainsaw" => p.Weapon with { Attack = new CircularWeaponAttack(extraDamage: 2, extraRadius: .30f,
                        windup: .10f, recovery: .23f, aimLock: .02f) },
                    "Grinder" => p.Weapon with { Attack = new CircularWeaponAttack(extraDamage: 4, extraRadius: .60f,
                        windup: .14f, recovery: .36f, aimLock: .03f) },
                    _ => p.Weapon
                };
                result = result with { Weapon = weapon, Bonus = p.Bonus with { Radius = .12f },
                    Tiers = p.Tiers!.Select(t => t with { UpgradeBonus = t.UpgradeBonus! with { Radius = .12f } }).ToArray() };
            }
            if (stamina && p.Id == "Stamina") result = result with { Bonus = p.Bonus with { Stamina = 1 } };
            if (resonance && p.Id == "Stamina") result = result with { Tiers = p.Tiers!.Select((t, i) => t with { Effects = Effects(i) }).ToArray() };
            return result;
        }).ToArray();
        return new(name, products, PreviousBills);
    }
    private static IReadOnlyList<CombatEffectDefinition> Effects(int tier) => tier switch
    {
        0 => new[] { new CombatEffectDefinition("impact-gold", () => new ImpactResonance(1.85f, 1, 5, 3)) },
        1 => new[] { new CombatEffectDefinition("impact-gold", () => new ImpactResonance(1.95f, 1, 5, 3)),
            new CombatEffectDefinition("idle-resonance", () => new TimedResonance(1.95f, 1, 4.5f, 6, 1)) },
        _ => new[] { new CombatEffectDefinition("impact-gold", () => new ImpactResonance(2.65f, 2, 5.5f, 4)),
            new CombatEffectDefinition("idle-resonance", () => new TimedResonance(2.2f, 1, 5, 6, 2)) }
    };
}
