using System;
using System.Collections.Generic;

public static class EquipmentContent
{
    public static IReadOnlyList<ShopProduct> Products { get; } = Array.AsReadOnly(new ShopProduct[]
    {
        new("Rim", "加宽锤面", "落锤半径 +0.18，范围圈同步扩大。", 18, 14, 3, new(Radius: 0.18f)),
        new("Stamina", "护腕", "每局体力 +1，下一局恢复到新的上限。", 16, 12, 3, new(Stamina: 1), Appearance: "cuff")
    });
}
