using System;
using System.Collections.Generic;

/// <summary>Composition root: immutable content creates isolated session and herd instances.</summary>
public sealed class GameContent : IGameFactory
{
    public SessionDefinition Definition { get; }
    public PigRoster Pigs { get; }
    public SwingTiming Timing { get; }
    public GameContent(SessionDefinition definition, PigRoster pigs, SwingTiming? timing = null)
    {
        ArgumentNullException.ThrowIfNull(definition); ArgumentNullException.ThrowIfNull(pigs);
        Definition = definition; Pigs = pigs; Timing = timing ?? new();
        _ = new StrikeCycle(Timing);
    }
    public GameSession CreateSession() => new(Definition);
    public PigHerd CreateHerd() => new(Pigs.Spawns);
}

public static class DefaultGame
{
    public static IReadOnlyList<ISwingRewardRule> RewardRules { get; } = Array.AsReadOnly(new ISwingRewardRule[]
    {
        new PerPigReward(), new LimitedPigReward(),
        new ComboReward(3, bonus => bonus.TripleCoins),
        new ComboReward(1, bonus => bonus.SingleCoins, maximum: 1),
        new ComboReward(2, bonus => bonus.PairCoins), new ComboRefund()
    });
    public static GameContent Content { get; } = new(Definition(), PigContent.Roster, BalanceContent.Timing);

    public static SessionDefinition Definition(IReadOnlyList<ShopProduct>? products = null,
        IReadOnlyList<SkillDefinition>? skills = null, IReadOnlyList<PerkCard>? perks = null,
        IReadOnlyList<Bill>? bills = null, GameTuning? tuning = null,
        IReadOnlyList<ISwingRewardRule>? rewardRules = null, WeaponDefinition? startingWeapon = null) =>
        new(tuning ?? BalanceContent.Tuning, bills ?? BalanceContent.Bills, products ?? EquipmentContent.Products,
            skills ?? SkillContent.Nodes, perks ?? PerkContent.Cards, rewardRules ?? RewardRules, startingWeapon ?? EquipmentContent.Hammer);

    public static GameSession CreateSession(Random? random = null, IReadOnlyList<ShopProduct>? products = null,
        IReadOnlyList<SkillDefinition>? skills = null, IReadOnlyList<PerkCard>? perks = null) =>
        new(Definition(products, skills, perks), random);
}
