using System;
using System.Collections.Generic;

public sealed record UpgradeBonus(int Stamina = 0, float Radius = 0)
{
    public UpgradeBonus Add(UpgradeBonus other) => new(Stamina + other.Stamina, Radius + other.Radius);
}

public enum EquipmentAppearance { Hammer, Cuff }

public sealed record ShopProduct(
    string Id, string Name, string Description, int BasePrice, int PriceStep, int MaxLevel, UpgradeBonus Bonus,
    EquipmentAppearance Appearance = EquipmentAppearance.Hammer)
{
    public int PriceAt(int level) => BasePrice + PriceStep * level;
}

/// <summary>商品数据。现有属性类型的新商品可直接在此配置。</summary>
public static class ShopCatalog
{
    public static IReadOnlyList<ShopProduct> Products { get; } = Array.AsReadOnly(new ShopProduct[]
    {
        new("Rim", "加宽锤面", "落锤半径 +0.18，范围圈同步扩大。", 18, 14, 3, new(Radius: 0.18f)),
        new("Stamina", "护腕", "每局体力 +1，下一局恢复到新的上限。", 16, 12, 3, new(Stamina: 1), EquipmentAppearance.Cuff)
    });
}
