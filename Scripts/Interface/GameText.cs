using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public enum GameLanguage { Chinese, English }

/// <summary>
/// 把规则结果转换为玩家可读的文字，核心规则无需依赖界面措辞。
/// </summary>
public static class GameText
{
    private const string SettingsPath = "user://settings.cfg";
    private const string LanguageKey = "language";

    public static GameLanguage Language { get; private set; }

    public static void LoadLanguage()
    {
        using var settings = new ConfigFile();
        settings.Load(SettingsPath);
        ApplyLanguage(settings.GetValue("interface", LanguageKey, "zh_CN").AsString() == "en"
            ? GameLanguage.English : GameLanguage.Chinese);
    }

    public static void SetLanguage(GameLanguage language)
    {
        if (language == Language)
        {
            return;
        }

        using var settings = new ConfigFile();
        settings.Load(SettingsPath);
        settings.SetValue("interface", LanguageKey, ApplyLanguage(language));
        var error = settings.Save(SettingsPath);
        if (error != Error.Ok)
        {
            GD.PushWarning($"Unable to save the language preference: {error}");
        }
    }

    private static string ApplyLanguage(GameLanguage language)
    {
        Language = language;
        string locale = language == GameLanguage.English ? "en" : "zh_CN";
        TranslationServer.SetLocale(locale);
        return locale;
    }

    public static string T(string source) => source.Length == 0 ? source : TranslationServer.Translate(source).ToString();

    public static string T(string source, params object?[] values) =>
        string.Format(CultureInfo.InvariantCulture, T(source), values);

    public static string Describe(string description, FormattableString? format) =>
        format == null ? T(description) : T(format.Format, format.GetArguments());

    public static string Purchase(PurchaseResult result) => result.Status switch
    {
        PurchaseStatus.Purchased => T("购入成功  −¥{0}", result.CashSpent),
        PurchaseStatus.InsufficientFunds => T("余额不足"),
        PurchaseStatus.SoldOut => T("已达到最高等级"),
        PurchaseStatus.Closed => T("收工后可以购买"),
        PurchaseStatus.StaleOffer => T("价格已更新，请重新选择"),
        PurchaseStatus.Locked => T("需要先完成前置装备升级"),
        _ => T("商品不存在")
    };

    public static string Skill(SkillResult result) => result.Succeeded
        ? T("学会了  −¥{0}", result.CashSpent)
        : result.Status switch
        {
            SkillStatus.Learned => T("已经学会"),
            SkillStatus.Locked => T("需要先学习前置技能"),
            SkillStatus.InsufficientFunds => T("余额不足"),
            SkillStatus.Closed => T("收工后可以学习"),
            _ => T("技能不存在")
        };

    /// <summary>将规则提供的缺失前置转换为提示，不在界面重新决定商品是否解锁。</summary>
    public static string LockReason(ShopOffer offer, IReadOnlyList<ShopOffer> catalog) =>
        string.Join(T("、"), (offer.MissingRequirements ?? Array.Empty<ProductRequirement>()).Select(requirement =>
        {
            string productName = catalog.FirstOrDefault(item => item.Product.Id == requirement.ProductId)?.Product.Name
                ?? requirement.ProductId;
            return $"{T(productName)} Lv.{requirement.Level}";
        }));

    public static string Deadline(RentDue due) => due switch
    {
        { Paid: true } => T("已付"),
        { Failed: true } => T("未付"),
        { DaysAway: 0 } => T("今日到期"),
        { DaysAway: 1 } => T("明天"),
        _ => T("{0} 天后", due.DaysAway)
    };
}
