using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>维护本局商品等级、报价与累计效果。钱包及班次由 GameSession 管理。</summary>
public sealed class ShopInventory
{
    private readonly IReadOnlyList<ShopProduct> _catalog;
    private readonly Dictionary<string, int> _levels = new();

    public Bonus TotalBonus { get; private set; } = new();

    public ShopInventory(IReadOnlyList<ShopProduct> catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var snapshot = catalog.ToArray();
        if (snapshot.Any(product => product == null || string.IsNullOrWhiteSpace(product.Id) ||
            product.BasePrice < 0 || product.PriceStep < 0 || product.MaxLevel <= 0 ||
            product.Bonus == null || !product.Bonus.IsValid) ||
            snapshot.Select(product => product.Id).Distinct().Count() != snapshot.Length)
            throw new ArgumentException("Products need unique IDs, valid prices and nonnegative finite bonuses.", nameof(catalog));
        try
        {
            foreach (var product in snapshot) _ = product.PriceAt(product.MaxLevel);
        }
        catch (OverflowException exception)
        {
            throw new ArgumentException("Product prices exceed the supported range.", nameof(catalog), exception);
        }
        _catalog = Array.AsReadOnly(snapshot);
    }

    public ShopProduct? Find(string id) => _catalog.FirstOrDefault(product => product.Id == id);
    public int Level(string id) => _levels.GetValueOrDefault(id);

    public PurchaseStatus Availability(ShopProduct product, int wallet, bool open)
    {
        if (!open) return PurchaseStatus.Closed;
        if (Level(product.Id) >= product.MaxLevel) return PurchaseStatus.SoldOut;
        if (wallet < product.PriceAt(Level(product.Id))) return PurchaseStatus.InsufficientFunds;
        return PurchaseStatus.Available;
    }

    public IReadOnlyList<ShopOffer> Quote(int wallet, bool open) => _catalog.Select(product =>
        new ShopOffer(product, Level(product.Id), product.PriceAt(Level(product.Id)), Availability(product, wallet, open))).ToArray();

    internal PurchaseResult TryPurchase(string id, int expectedLevel, int wallet, bool open)
    {
        if (!open) return new(PurchaseStatus.Closed);
        var product = Find(id);
        if (product == null) return new(PurchaseStatus.UnknownProduct);
        if (Level(id) != expectedLevel) return new(PurchaseStatus.StaleOffer);
        var status = Availability(product, wallet, open);
        if (status != PurchaseStatus.Available) return new(status);
        int price = product.PriceAt(expectedLevel);
        var updated = TotalBonus.Add(product.Bonus);
        _levels[product.Id] = Level(product.Id) + 1;
        TotalBonus = updated;
        return new(PurchaseStatus.Purchased, price);
    }
}
