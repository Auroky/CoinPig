using System.Collections.Generic;

public enum EquipmentAppearance { Hammer, Cuff }

/// <summary>Shared visual identity used by the flat illustration and the physical display.</summary>
public static class EquipmentVisuals
{
    private static readonly IReadOnlyDictionary<string, EquipmentAppearance> Appearances =
        new Dictionary<string, EquipmentAppearance>
        {
            ["Rim"] = EquipmentAppearance.Hammer,
            ["Stamina"] = EquipmentAppearance.Cuff
        };

    public static EquipmentAppearance For(string productId) =>
        Appearances.TryGetValue(productId, out var appearance) ? appearance : EquipmentAppearance.Hammer;
}
