using System;
using System.Collections.Generic;
using System.Linq;

public enum ShiftPhase { Ready, Working, Choosing, Shopping, Won, Bankrupt }
public sealed record ShiftReceipt(int Shift, int OpeningCash, int Earnings, int Due, int Paid, int Shortfall, int Pigs);

/// <summary>一局的体力、钱包与还款顺序。场景负责提供每次落锤的伤害结果，界面只读取结算结果。</summary>
public sealed class GameSession : IShop, ISkillTree
{
    private readonly Random _random;
    private readonly HashSet<Perk> _perks = new();
    private readonly SwingRewards _rewards = new();
    private readonly RentContract _contract = new(GameRules.Bills);
    private readonly SkillProgress _skills = new();
    private readonly ShopInventory _inventory;

    public GameSession(Random? random = null, IReadOnlyList<ShopProduct>? products = null)
    {
        _random = random ?? new Random();
        _inventory = new ShopInventory(products ?? ShopCatalog.Products);
    }

    public ShiftPhase Phase { get; private set; } = ShiftPhase.Ready;
    public bool IsEndless { get; private set; }
    public int Shift { get; private set; } = 1;
    public int Wallet { get; private set; }
    public int ShiftEarnings { get; private set; }
    public int Stamina { get; private set; } = GameRules.StartingStamina;
    public int Damage => 1 + _skills.TotalBonus.Damage;
    public int ShiftPigs { get; private set; }
    public float RecoveryReduction => _skills.TotalBonus.Recovery;
    public int PaidBills => _contract.PaidCount;
    public int Capacity => GameRules.StartingStamina + _inventory.TotalBonus.Stamina + _skills.TotalBonus.Stamina + (Has(Perk.Lunch) ? GameRules.LunchStamina : 0);
    public float ExtraRadius => _inventory.TotalBonus.Radius + _skills.TotalBonus.Radius + (Has(Perk.WideRim) ? GameRules.PerkRimGain : 0);
    public Bill? NextBill => _contract.Next;
    public ShiftReceipt? Receipt { get; private set; }
    public IReadOnlyList<PerkCard> Offers { get; private set; } = Array.Empty<PerkCard>();

    public bool Has(Perk perk) => _perks.Contains(perk);

    public IReadOnlyList<RentDue> GetRentSchedule() => _contract.Schedule(Shift, Phase == ShiftPhase.Bankrupt);

    public bool BeginShift()
    {
        if (Phase != ShiftPhase.Ready && Phase != ShiftPhase.Shopping && Phase != ShiftPhase.Won) return false;
        if (Phase == ShiftPhase.Won) IsEndless = true;
        if (Phase != ShiftPhase.Ready) Shift++;
        Phase = ShiftPhase.Working;
        ShiftEarnings = 0;
        ShiftPigs = 0;
        Stamina = Capacity;
        _rewards.BeginDay();
        return true;
    }

    public int LandSwing(SwingResult strike)
    {
        if (strike.IsSettled) return 0;
        if (Phase != ShiftPhase.Working || Stamina == 0)
            throw new InvalidOperationException("Cannot record this swing.");

        strike.MarkSettled();
        Stamina--;
        int broken = strike.DestroyedCount;
        ShiftPigs += broken;
        var payout = _rewards.Calculate(strike, _skills.TotalBonus, Has);
        int coins = payout.Coins;
        Stamina += payout.StaminaRefund;
        Wallet += coins;
        ShiftEarnings += coins;
        return coins;
    }

    /// <summary>先验证回合状态，再修改猪的血量。空锤也消耗体力；无效命令不改变任何状态。</summary>
    public bool TrySwing(IEnumerable<PigState> targets, out SwingResult? strike, out int coins)
    {
        strike = null;
        coins = 0;
        if (Phase != ShiftPhase.Working || Stamina <= 0) return false;
        strike = StrikeResolver.Resolve(targets, Damage);
        coins = LandSwing(strike);
        return true;
    }

    public void CloseShift()
    {
        if (Phase != ShiftPhase.Working) return;
        var payment = _contract.Settle(Shift, Wallet);
        int due = payment.Due, missing = payment.Shortfall, paid = payment.Paid;
        Receipt = new(Shift, Wallet - ShiftEarnings, ShiftEarnings, due, paid, missing,
            ShiftPigs);
        Wallet -= paid;

        if (missing > 0) { Phase = ShiftPhase.Bankrupt; return; }
        if (due == 0) { Phase = ShiftPhase.Shopping; return; }

        if (NextBill == null) { Phase = ShiftPhase.Won; return; }
        Offers = GameRules.Cards.Where(card => !Has(card.Kind))
            .OrderBy(_ => _random.Next()).Take(3).ToArray();
        Phase = Offers.Count > 0 ? ShiftPhase.Choosing : ShiftPhase.Shopping;
    }

    public bool Choose(Perk kind)
    {
        if (Phase != ShiftPhase.Choosing || !Offers.Any(card => card.Kind == kind)) return false;
        _perks.Add(kind);
        Offers = Array.Empty<PerkCard>();
        Phase = ShiftPhase.Shopping;
        return true;
    }

    public IReadOnlyList<SkillOffer> GetSkills() => _skills.Quote(Phase == ShiftPhase.Shopping, Wallet);
    public SkillResult TryLearn(string id)
    {
        // 技能状态验证前置和金币，成功后由回合钱包同步扣款。
        var result = _skills.TryLearn(id, Phase == ShiftPhase.Shopping, Wallet);
        if (result.Succeeded) Wallet -= result.CashSpent;
        return result;
    }

    public IReadOnlyList<ShopOffer> GetOffers() => _inventory.Quote(Wallet, Phase == ShiftPhase.Shopping);

    public PurchaseResult TryBuy(string productId, int expectedLevel)
    {
        var result = _inventory.TryPurchase(productId, expectedLevel, Wallet, Phase == ShiftPhase.Shopping);
        if (result.Succeeded) Wallet -= result.CashSpent;
        return result;
    }
}

