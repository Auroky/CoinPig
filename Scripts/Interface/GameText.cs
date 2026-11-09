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

    public static string Deadline(RentDue due) => due.Paid ? "已付" : due.Failed ? "未付" : due.DaysAway == 0 ? "今日到期" : $"{due.DaysAway} 天后";
}
