using System;
using System.Collections.Generic;

public sealed record ShopProduct(string Id, string Name, string Description, int BasePrice, int PriceStep,
    int MaxLevel, Bonus Bonus, string Appearance = "hammer", int Stage = 1,
    IReadOnlyList<ProductRequirement>? Requirements = null, WeaponDefinition? Weapon = null,
    IReadOnlyList<EquipmentTier>? Tiers = null, float DisplayYaw = 0)
{
    public int PriceAt(int level) => checked(BasePrice + PriceStep * level);
    public Bonus BonusAt(int index) => TierAt(index).UpgradeBonus ?? Bonus;
    public Bonus BonusThrough(int level)
    {
        if (level < 0 || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
        var total = new Bonus();
        for (int i = 0; i < level; i++) total = total.Add(BonusAt(i));
        return total;
    }
    public EquipmentTier TierAt(int index) => Tiers != null && Tiers.Count > 0
        ? Tiers[Math.Clamp(index, 0, Tiers.Count - 1)] : new($"等级 {index + 1}", StandardPalette);
    public static EquipmentPalette StandardPalette { get; } = new("657d87", "d6aa62", "c6d3d5", "49372e", "8bd9df");
}
