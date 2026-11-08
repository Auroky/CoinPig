using System.Collections.Generic;

public enum PurchaseStatus { Available, Purchased, Closed, UnknownProduct, SoldOut, InsufficientFunds, StaleOffer }

public sealed record ShopOffer(ShopProduct Product, int Level, int Price, PurchaseStatus Status);
public sealed record PurchaseResult(PurchaseStatus Status, int CashSpent = 0)
{
    public bool Succeeded => Status == PurchaseStatus.Purchased;
}

/// <summary>
/// 界面只读取报价和提交购买。正常购买失败返回结果，并保持钱包与等级原值。
/// expectedLevel 是玩家看到的商品等级，防止重复提交同一报价时重复扣款。
/// </summary>
public interface IShop
{
    int Wallet { get; }
    IReadOnlyList<ShopOffer> GetOffers();
    PurchaseResult TryBuy(string productId, int expectedLevel);
}
