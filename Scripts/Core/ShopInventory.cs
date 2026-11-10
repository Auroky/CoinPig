using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 管理商品等级、装备选择、报价及累计效果。钱包属于 GameSession，此类只返回应扣金额。
/// </summary>
public sealed class ShopInventory
{
    private readonly IReadOnlyList<ShopProduct> _catalog;
    private readonly Dictionary<string, int> _levels = new();

    public string? EquippedProductId { get; private set; }

    public Bonus TotalBonus { get; private set; } = new();

    public ShopInventory(IReadOnlyList<ShopProduct> catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var snapshot = catalog.ToArray();
        if (snapshot.Any(product => !IsValidProduct(product)) ||
            snapshot.Select(product => product.Id).Distinct().Count() != snapshot.Length)
        {
            throw new ArgumentException("Products need unique IDs, valid prices and nonnegative finite bonuses.", nameof(catalog));
        }
        try
        {
            foreach (var product in snapshot)
            {
                _ = product.PriceAt(product.MaxLevel);
            }
        }
        catch (OverflowException exception)
        {
            throw new ArgumentException("Product prices exceed the supported range.", nameof(catalog), exception);
        }
        var ids = snapshot.ToDictionary(p => p.Id);
        foreach (var product in snapshot)
        {
            foreach (var requirement in product.Requirements ?? Array.Empty<ProductRequirement>())
            {
                if (requirement == null || !ids.TryGetValue(requirement.ProductId, out var prerequisite) ||
                                requirement.Level <= 0 || requirement.Level > prerequisite.MaxLevel)
                {
                    throw new ArgumentException("Equipment prerequisites must refer to valid product levels.", nameof(catalog));
                }
            }
        }
        foreach (var product in snapshot)
        {
            ValidateAcyclic(product.Id, ids, new HashSet<string>());
        }

        // 复制目录及其嵌套列表，避免外部后来修改商品配置，导致已报价的交易条件发生变化。
        _catalog = Array.AsReadOnly(snapshot.Select(p => p with
        {
            Requirements = p.Requirements == null ? null : Array.AsReadOnly(p.Requirements.ToArray()),
            Tiers = p.Tiers == null ? null : Array.AsReadOnly(p.Tiers.Select(t => t with
            {
                Effects = t.Effects == null ? null : Array.AsReadOnly(t.Effects.ToArray())
            }).ToArray())
        }).ToArray());
    }

    private static bool IsValidProduct(ShopProduct? product)
    {
        if (product == null || string.IsNullOrWhiteSpace(product.Id) ||
            product.BasePrice < 0 || product.PriceStep < 0 || product.MaxLevel <= 0 ||
            product.Bonus == null || !product.Bonus.IsValid || product.Stage <= 0 || !float.IsFinite(product.DisplayYaw))
        {
            return false;
        }
        if (product.Tiers != null &&
            (product.Tiers.Count != product.MaxLevel || product.Tiers.Any(tier => !IsValidTier(tier))))
        {
            return false;
        }
        return product.Weapon == null ||
            (!string.IsNullOrWhiteSpace(product.Weapon.Id) && product.Weapon.Attack != null);
    }

    private static bool IsValidTier(EquipmentTier? tier)
    {
        if (tier == null || tier.Palette == null || !ValidPalette(tier.Palette) ||
            (tier.UpgradeBonus != null && !tier.UpgradeBonus.IsValid))
        {
            return false;
        }
        return tier.Effects == null || tier.Effects.All(effect =>
            effect != null && effect.Create != null && !string.IsNullOrWhiteSpace(effect.Id));
    }

    private static bool ValidPalette(EquipmentPalette palette) =>
        new[] { palette.Body, palette.Trim, palette.Metal, palette.Grip, palette.Glow }
            .All(c => c != null && c.Length == 6 && c.All(Uri.IsHexDigit));

    private static void ValidateAcyclic(string id, Dictionary<string, ShopProduct> products, HashSet<string> ancestors)
    {
        if (!ancestors.Add(id))
        {
            throw new ArgumentException("Equipment prerequisites must be acyclic.");
        }

        foreach (var requirement in products[id].Requirements ?? Array.Empty<ProductRequirement>())
        {
            ValidateAcyclic(requirement.ProductId, products, ancestors);
        }

        ancestors.Remove(id);
    }

    public ShopProduct? EquippedProduct => EquippedProductId == null ? null : Find(EquippedProductId);
    public IReadOnlyList<CombatEffectDefinition> ActiveEffects => _catalog.Where(p => (p.Weapon == null || p.Id == EquippedProductId) && Level(p.Id) > 0)
        .SelectMany(p => p.TierAt(Level(p.Id) - 1).Effects ?? Array.Empty<CombatEffectDefinition>()).ToArray();
    public EquipmentTier? AccessoryTier => _catalog.Where(p => p.Weapon == null && Level(p.Id) > 0)
        .OrderByDescending(p => p.Stage).ThenByDescending(p => Level(p.Id)).Select(p => p.TierAt(Level(p.Id) - 1)).FirstOrDefault();
    public Bonus EquippedBonus
    {
        get
        {
            var product = EquippedProduct;
            return product?.BonusThrough(Level(product.Id)) ?? new Bonus();
        }
    }

    private IEnumerable<ProductRequirement> MissingRequirements(ShopProduct product) =>
        (product.Requirements ?? Array.Empty<ProductRequirement>())
            .Where(requirement => Level(requirement.ProductId) < requirement.Level);

    public ShopProduct? Find(string id) => _catalog.FirstOrDefault(product => product.Id == id);

    public int Level(string id) => _levels.GetValueOrDefault(id);

    public PurchaseStatus Availability(ShopProduct product, int wallet, bool open)
    {
        if (!open)
        {
            return PurchaseStatus.Closed;
        }

        if (Level(product.Id) >= product.MaxLevel)
        {
            return PurchaseStatus.SoldOut;
        }

        if (MissingRequirements(product).Any())
        {
            return PurchaseStatus.Locked;
        }

        if (wallet < product.PriceAt(Level(product.Id)))
        {
            return PurchaseStatus.InsufficientFunds;
        }

        return PurchaseStatus.Available;
    }

    public IReadOnlyList<ShopOffer> Quote(int wallet, bool open) => _catalog.Select(product =>
        new ShopOffer(
            product,
            Level(product.Id),
            product.PriceAt(Level(product.Id)),
            Availability(product, wallet, open),
            Array.AsReadOnly(MissingRequirements(product).ToArray()),
            EquippedProductId == product.Id
        )).ToArray();

    /// <summary>校验报价、前置和余额后更新库存，返回本次价格；钱包扣款由会话完成。</summary>
    internal PurchaseResult TryPurchase(string id, int expectedLevel, int wallet, bool open)
    {
        if (!open)
        {
            return new(PurchaseStatus.Closed);
        }

        var product = Find(id);
        if (product == null)
        {
            return new(PurchaseStatus.UnknownProduct);
        }

        // 报价携带旧等级；重复点击旧按钮时拒绝交易，防止在界面刷新前连续扣款。
        if (Level(id) != expectedLevel)
        {
            return new(PurchaseStatus.StaleOffer);
        }

        var status = Availability(product, wallet, open);
        if (status != PurchaseStatus.Available)
        {
            return new(status);
        }

        int price = product.PriceAt(expectedLevel);
        var updated = product.Weapon == null ? TotalBonus.Add(product.BonusAt(expectedLevel)) : TotalBonus;
        _levels[product.Id] = Level(product.Id) + 1;
        TotalBonus = updated;
        if (product.Weapon != null && (EquippedProduct == null || product.Stage > EquippedProduct.Stage))
        {
            EquippedProductId = product.Id;
        }

        return new(PurchaseStatus.Purchased, price);
    }

    internal EquipResult TryEquip(string id)
    {
        var product = Find(id);
        if (product == null)
        {
            return new(EquipStatus.UnknownProduct);
        }

        if (product.Weapon == null)
        {
            return new(EquipStatus.NotAWeapon);
        }

        if (Level(id) == 0)
        {
            return new(EquipStatus.NotOwned);
        }

        if (EquippedProductId == id)
        {
            return new(EquipStatus.AlreadyEquipped);
        }

        EquippedProductId = id;
        return new(EquipStatus.Equipped);
    }
}
