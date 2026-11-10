using System.Collections.Generic;

/// <summary>
/// 购买报价或命令的结果；失败状态不扣款，也不改变已购买等级。
/// </summary>
public enum PurchaseStatus
{
    Available, Purchased, Closed, UnknownProduct, SoldOut, InsufficientFunds, StaleOffer, Locked
}

/// <summary>
/// 界面可读的商品报价快照。Level 用于识别过期报价；MissingRequirements 只提供事实，提示文字由界面生成。
/// </summary>
public sealed record ShopOffer(ShopProduct Product, int Level, int Price, PurchaseStatus Status,
    IReadOnlyList<ProductRequirement>? MissingRequirements = null, bool Equipped = false);

/// <summary>
/// 购买结果与需要扣除的金币；只有成功结果才由会话钱包扣款。
/// </summary>
public sealed record PurchaseResult(PurchaseStatus Status, int CashSpent = 0)
{
    public bool Succeeded => Status == PurchaseStatus.Purchased;
}

/// <summary>
/// 商店展示需要的只读接口：钱包和报价，不包含修改状态的能力。
/// </summary>
public interface IShopView
{
    int Wallet { get; }
    IReadOnlyList<ShopOffer> GetOffers();
}

/// <summary>
/// 购买命令接口；报价等级必须仍然有效，失败时保留钱包与库存状态。
/// </summary>
public interface IShop : IShopView
{
    // expectedLevel 为界面显示的旧等级；拒绝重复旧报价时不得产生任何扣款。
    PurchaseResult TryBuy(string productId, int expectedLevel);
}
