using System;
using System.Collections.Generic;

/// <summary>
/// 商品定义：价格公式、等级上限、前置条件及每级效果；武器只应用当前装备的属性。
/// </summary>
public sealed record ShopProduct(string Id, string Name, string Description, int BasePrice, int PriceStep,
    int MaxLevel, Bonus Bonus, string Appearance = "hammer", int Stage = 1,
    IReadOnlyList<ProductRequirement>? Requirements = null, WeaponDefinition? Weapon = null,
    IReadOnlyList<EquipmentTier>? Tiers = null, float DisplayYaw = 0)
{
    /// <summary>按已购买等级计算下一次价格：基础价格加每级涨价。参数从零开始。</summary>
    public int PriceAt(int level) => checked(BasePrice + PriceStep * level);
    public Bonus BonusAt(int index) => TierAt(index).UpgradeBonus ?? Bonus;
    /// <summary>累计指定已购等级的属性增量；不包含其他武器、技能或护腕。</summary>
    public Bonus BonusThrough(int level)
    {
        if (level < 0 || level > MaxLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        var total = new Bonus();
        for (int i = 0; i < level; i++)
        {
            total = total.Add(BonusAt(i));
        }

        return total;
    }
    public EquipmentTier TierAt(int index) => Tiers != null && Tiers.Count > 0
        ? Tiers[Math.Clamp(index, 0, Tiers.Count - 1)] : new($"等级 {index + 1}", StandardPalette);
    public static EquipmentPalette StandardPalette { get; } = new("657d87", "d6aa62", "c6d3d5", "49372e", "8bd9df");
}
