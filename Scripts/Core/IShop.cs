using System.Collections.Generic;

public enum PurchaseStatus { Available, Purchased, Closed, UnknownProduct, SoldOut, InsufficientFunds, StaleOffer }

public sealed record ShopOffer(ShopProduct Product, int Level, int Price, PurchaseStatus Status);
public sealed record PurchaseResult(PurchaseStatus Status, int CashSpent = 0)
{
    public bool Succeeded => Status == PurchaseStatus.Purchased;
}

/// <summary>
/// 商品显示只需要余额和报价，不获得购买命令。
/// </summary>
public interface IShopView
{
    int Wallet { get; }
    IReadOnlyList<ShopOffer> GetOffers();
}

/// <summary>
/// 购买成功扣除当前报价；失败保持余额与等级不变。
/// expectedLevel 为玩家看到的商品等级，过期报价不能重复扣款。
/// </summary>
public interface IShop : IShopView
{
    PurchaseResult TryBuy(string productId, int expectedLevel);
}
