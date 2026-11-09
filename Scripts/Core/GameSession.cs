using System;
using System.Collections.Generic;

public enum ShiftPhase { Ready, Working, Choosing, Shopping, Won, Bankrupt }
public sealed record ShiftReceipt(int Shift, int OpeningCash, int Earnings, int Due, int Paid, int Shortfall, int Pigs);

/// <summary>协调一局的回合、体力和钱包；合法命令交给伤害、账单和成长对象处理。</summary>
public sealed class GameSession : IShop, ISkillTree
{
    private readonly PerkProgress _perks;
    private readonly SwingRewards _rewards = new();
    private readonly RentContract _contract = new(GameRules.Bills);
    private readonly SkillProgress _skills;
    private readonly ShopInventory _inventory;

    public GameSession(Random? random = null, IReadOnlyList<ShopProduct>? products = null,
        IReadOnlyList<SkillDefinition>? skills = null, IReadOnlyList<PerkCard>? perks = null)
    {
        _perks = new PerkProgress(perks ?? GameRules.Cards, random ?? new Random());
        _skills = new SkillProgress(skills ?? SkillCatalog.Nodes);
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
    public int Capacity => GameRules.StartingStamina + _inventory.TotalBonus.Stamina + _skills.TotalBonus.Stamina + _perks.TotalBonus.Stamina;
    public float ExtraRadius => _inventory.TotalBonus.Radius + _skills.TotalBonus.Radius + _perks.TotalBonus.Radius;
    public Bill? NextBill => _contract.Next;
    public ShiftReceipt? Receipt { get; private set; }
    public IReadOnlyList<PerkCard> Offers => _perks.Offers;

    public bool Has(Perk perk) => _perks.Has(perk);

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

    private int SettleSwing(SwingResult strike)
    {
        if (strike.IsSettled) return 0;
        if (Phase != ShiftPhase.Working || Stamina == 0)
            throw new InvalidOperationException("Cannot record this swing.");

        strike.MarkSettled();
        Stamina--;
        int broken = strike.DestroyedCount;
        ShiftPigs += broken;
        var payout = _rewards.Calculate(strike, _skills.TotalBonus, _perks.TotalBonus);
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
        coins = SettleSwing(strike);
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
        _perks.OfferChoices();
        Phase = Offers.Count > 0 ? ShiftPhase.Choosing : ShiftPhase.Shopping;
    }

    public bool Choose(Perk kind)
    {
        if (Phase != ShiftPhase.Choosing || !_perks.Choose(kind)) return false;
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

