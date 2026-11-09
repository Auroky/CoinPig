using System;
using System.Collections.Generic;
using System.Linq;

public sealed record PerkBonus(int Stamina = 0, float Radius = 0, int PairCoins = 0,
    int SingleCoins = 0, int Refunds = 0, int BonusCoinLimit = 0)
{
    public PerkBonus Add(PerkBonus other) => new(
        checked(Stamina + other.Stamina), Radius + other.Radius,
        checked(PairCoins + other.PairCoins), checked(SingleCoins + other.SingleCoins),
        checked(Refunds + other.Refunds), checked(BonusCoinLimit + other.BonusCoinLimit));
}

/// <summary>Owns the available choices and the accumulated effects of selected perks.</summary>
public sealed class PerkProgress
{
    private readonly IReadOnlyList<PerkCard> _catalog;
    private readonly Random _random;
    private readonly HashSet<Perk> _owned = new();
    public PerkBonus TotalBonus { get; private set; } = new();
    public IReadOnlyList<PerkCard> Offers { get; private set; } = Array.Empty<PerkCard>();

    public PerkProgress(IReadOnlyList<PerkCard> cards, Random random)
    {
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(random);
        var snapshot = cards.ToArray();
        if (snapshot.Any(card => card == null || card.Bonus == null ||
            card.Bonus.Stamina < 0 || card.Bonus.PairCoins < 0 || card.Bonus.SingleCoins < 0 ||
            card.Bonus.Refunds < 0 || card.Bonus.BonusCoinLimit < 0 ||
            !float.IsFinite(card.Bonus.Radius) || card.Bonus.Radius < 0) ||
            snapshot.Select(card => card.Kind).Distinct().Count() != snapshot.Length)
            throw new ArgumentException("Perks must have unique identities and nonnegative finite bonuses.", nameof(cards));
        _catalog = Array.AsReadOnly(snapshot);
        _random = random;
    }

    public bool Has(Perk kind) => _owned.Contains(kind);

    public void OfferChoices()
    {
        Offers = Array.AsReadOnly(_catalog.Where(card => !Has(card.Kind))
            .OrderBy(_ => _random.Next()).Take(3).ToArray());
    }

    public bool Choose(Perk kind)
    {
        var card = Offers.FirstOrDefault(offer => offer.Kind == kind);
        if (card == null || Has(kind)) return false;
        var updated = TotalBonus.Add(card.Bonus);
        _owned.Add(kind);
        TotalBonus = updated;
        Offers = Array.Empty<PerkCard>();
        return true;
    }
}
