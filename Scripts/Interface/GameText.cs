using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 把规则结果转换为玩家可读的文字，核心规则无需依赖界面措辞。
/// </summary>
public static class GameText
{
    public static string Purchase(PurchaseResult result) => result.Status switch
    {
        PurchaseStatus.Purchased => $"购入成功  −¥{result.CashSpent}",
        PurchaseStatus.InsufficientFunds => "余额不足",
        PurchaseStatus.SoldOut => "已达到最高等级",
        PurchaseStatus.Closed => "收工后可以购买",
        PurchaseStatus.StaleOffer => "价格已更新，请重新选择",
        PurchaseStatus.Locked => "需要先完成前置装备升级",
        _ => "商品不存在"
    };

    public static string Skill(SkillResult result) => result.Succeeded
        ? $"学会了  −¥{result.CashSpent}"
        : result.Status switch
        {
            SkillStatus.Learned => "已经学会",
            SkillStatus.Locked => "需要先学习前置技能",
            SkillStatus.InsufficientFunds => "余额不足",
            SkillStatus.Closed => "收工后可以学习",
            _ => "技能不存在"
        };

    /// <summary>将规则提供的缺失前置转换为提示，不在界面重新决定商品是否解锁。</summary>
    public static string LockReason(ShopOffer offer, IReadOnlyList<ShopOffer> catalog) =>
        string.Join("、", (offer.MissingRequirements ?? Array.Empty<ProductRequirement>()).Select(requirement =>
        {
            string productName = catalog.FirstOrDefault(item => item.Product.Id == requirement.ProductId)?.Product.Name
                ?? requirement.ProductId;
            return $"{productName} Lv.{requirement.Level}";
        }));

    public static string Deadline(RentDue due) => due.Paid ? "已付" : due.Failed ? "未付" : due.DaysAway == 0 ? "今日到期" : $"{due.DaysAway} 天后";
}
