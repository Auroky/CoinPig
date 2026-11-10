using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 管理本局拥有的随机能力、待选列表和累计加成；新会话创建全新状态。
/// </summary>
public sealed class PerkProgress
{
    private readonly IReadOnlyList<PerkCard> _catalog;
    private readonly Random _random;
    private readonly HashSet<string> _owned = new();

    public Bonus TotalBonus { get; private set; } = new();
    public IReadOnlyList<PerkCard> Offers { get; private set; } = Array.Empty<PerkCard>();

    public PerkProgress(IReadOnlyList<PerkCard> cards, Random random)
    {
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(random);
        var snapshot = cards.ToArray();
        if (snapshot.Any(card => card == null || card.Bonus == null ||
            !card.Bonus.IsValid || string.IsNullOrWhiteSpace(card.Id)) ||
            snapshot.Select(card => card.Id).Distinct().Count() != snapshot.Length)
        {
            throw new ArgumentException("Perks must have unique identities and nonnegative finite bonuses.", nameof(cards));
        }

        _catalog = Array.AsReadOnly(snapshot);
        _random = random;
    }

    public bool Has(string kind) => _owned.Contains(kind);

    public void OfferChoices()
    {
        Offers = Array.AsReadOnly(_catalog.Where(card => !Has(card.Id))
            .OrderBy(_ => _random.Next()).Take(3).ToArray());
    }

    public bool Choose(string kind)
    {
        var card = Offers.FirstOrDefault(offer => offer.Id == kind);
        if (card == null || Has(kind))
        {
            return false;
        }

        var updated = TotalBonus.Add(card.Bonus);
        _owned.Add(kind);
        TotalBonus = updated;
        Offers = Array.Empty<PerkCard>();
        return true;
    }
}
