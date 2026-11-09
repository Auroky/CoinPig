using System;
using System.Collections.Generic;
using System.Linq;

internal static class LeaseExperiments
{
    private static IReadOnlyList<SkillDefinition> ReferenceSkills => SkillContent.Nodes.Select(s =>
        s.Id is "breath" or "reserve" ? s with { Bonus = s.Bonus with { Stamina = 2 } } : s).ToArray();
    private static IReadOnlyList<ShopProduct> ReferenceProducts => EquipmentContent.Products.Select(p => p.Weapon == null ? p : p with
    {
        Bonus = p.Bonus with { Radius = .12f },
        Tiers = p.Tiers!.Select(t => t with { UpgradeBonus = t.UpgradeBonus! with { Radius = .12f } }).ToArray()
    }).ToArray();
    private static IReadOnlyList<Bill> ReferenceBills => BalanceContent.Bills.Take(5).Concat(
        new[] { 200, 260, 330, 420, 530, 670, 840, 1030, 1250, 1450 }.Select((amount, i) => new Bill(12 + i * 2, amount))).ToArray();
    public static IEnumerable<BalanceVariant> Variants()
    {
        var lessStack = ReferenceSkills.Select(s => s.Id is "breath" or "reserve" ? s with { Bonus = s.Bonus with { Stamina = 1 } } : s).ToArray();
        var tighter = ReferenceProducts.Select(p => p.Weapon == null ? p : p with
        {
            Bonus = p.Bonus with { Radius = .11f },
            Tiers = p.Tiers!.Select(t => t with { UpgradeBonus = t.UpgradeBonus! with { Radius = .11f } }).ToArray()
        }).ToArray();
        yield return new("lease-reference", ReferenceProducts, ReferenceBills, ReferenceSkills);
        yield return new("less-stack", ReferenceProducts, ReferenceBills, lessStack);
        yield return new("tighter", tighter, ReferenceBills, ReferenceSkills);
        yield return new("less-stack-tighter", tighter, ReferenceBills, lessStack);
        yield return new("less-stack-tighter-rent", tighter,
            Plan("", new[] { 200, 260, 330, 420, 500, 620, 760, 920, 1100, 1300 }).Bills, lessStack);
        yield return Plan("lease-soft", new[] { 200, 260, 330, 420, 520, 650, 800, 980, 1180, 1400 });
        yield return Plan("lease-balanced", new[] { 200, 260, 330, 420, 540, 690, 870, 1080, 1320, 1590 });
        yield return Plan("lease-firm", new[] { 200, 260, 340, 440, 580, 750, 960, 1210, 1500, 1830 });
        yield return Plan("lease-steady", new[] { 220, 320, 450, 580, 700, 800, 880, 950, 1020, 1100 });
        yield return Plan("lease-steady-pressure", new[] { 230, 340, 480, 620, 750, 850, 930, 1010, 1090, 1170 });
        yield return new("without-stamina", EquipmentContent.Products.Select(p => p with
        {
            Bonus = p.Bonus with { Stamina = 0 },
            Tiers = p.Tiers?.Select(t => t with { UpgradeBonus = t.UpgradeBonus == null ? null : t.UpgradeBonus with { Stamina = 0 } }).ToArray()
        }).ToArray(), BalanceContent.Bills);
    }
    private static BalanceVariant Plan(string name, int[] amounts) => new(name, ReferenceProducts,
        BalanceContent.Bills.Take(5).Concat(amounts.Select((amount, i) => new Bill(12 + i * 2, amount))).ToArray(), ReferenceSkills);
}
