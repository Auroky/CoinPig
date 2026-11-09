using System;
using System.Collections.Generic;

internal sealed class ReadOnlyStock : IShopView
{
    public int Wallet => 0;
    public IReadOnlyList<ShopOffer> GetOffers() => Array.AsReadOnly(new[]
    {
        new ShopOffer(new ShopProduct("spare", "Spare tool", "", 1, 0, 1, new()), 0, 1, PurchaseStatus.InsufficientFunds)
    });
}

internal sealed class ReadOnlyTraining : ISkillTreeView
{
    public IReadOnlyList<SkillOffer> GetSkills() => Array.AsReadOnly(new[]
    {
        new SkillOffer(new SkillDefinition("lesson", "Lesson", "", null, SkillContent.Hammer, 0, 1, new()), SkillStatus.Available)
    });
}
