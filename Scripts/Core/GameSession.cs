using System;
using System.Collections.Generic;

public enum ShiftPhase { Ready, Working, Choosing, Shopping, Won, Bankrupt }
public sealed record ShiftReceipt(int Shift, int OpeningCash, int Earnings, int Due, int Paid, int Shortfall, int Pigs);

/// <summary>协调一局的回合、体力和钱包；合法命令交给伤害、账单和成长对象处理。</summary>
public sealed class GameSession : IShop, ISkillTree, ISessionView, IWeaponLoadout, IEquipmentStatusView
{
    private readonly PerkProgress _perks;
    private readonly SwingRewards _rewards;
    private readonly RentContract _contract;
    private readonly SessionDefinition _definition;
    private readonly SkillProgress _skills;
    private readonly ShopInventory _inventory;

    public GameSession(SessionDefinition definition, Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definition = definition;
        _perks = new PerkProgress(definition.Perks, random ?? new Random());
        _skills = new SkillProgress(definition.Skills);
        _inventory = new ShopInventory(definition.Products);
        _contract = new RentContract(definition.Bills);
        _rewards = new SwingRewards(definition.RewardRules);
        Stamina = definition.Tuning.StartingStamina;
    }

    public ShiftPhase Phase { get; private set; } = ShiftPhase.Ready;
    public bool IsEndless { get; private set; }
    public int Shift { get; private set; } = 1;
    public int Wallet { get; private set; }
    public int ShiftEarnings { get; private set; }
    public int Stamina { get; private set; }
    public Bonus TotalBonus => _inventory.TotalBonus.Add(_inventory.EquippedBonus).Add(_skills.TotalBonus).Add(_perks.TotalBonus);
    public WeaponLoadout EquippedWeapon
    {
        get
        {
            var product = _inventory.EquippedProduct;
            int level = product == null ? 0 : _inventory.Level(product.Id);
            return new(product?.Weapon ?? _definition.StartingWeapon, level,
                product?.TierAt(level - 1).Palette ?? ShopProduct.StandardPalette, product?.Id);
        }
    }
    public AttackStats AttackStats
    {
        get
        {
            var stats = EquippedWeapon.Weapon.Attack.Stats(_definition.Tuning, TotalBonus);
            if (stats.Damage <= 0 || stats.StaminaCost <= 0 || !float.IsFinite(stats.Radius) || stats.Radius <= 0)
                throw new InvalidOperationException("Weapon strategies must return positive finite attack values.");
            return stats;
        }
    }
    public int StaminaCost => AttackStats.StaminaCost;
    public bool CanAttack => Stamina >= StaminaCost;
    public IReadOnlyList<CombatEffectDefinition> ActiveEffects => _inventory.ActiveEffects;
    public EquipmentTier? EquippedAccessory => _inventory.AccessoryTier;
    public SwingTiming AttackTiming(SwingTiming baseline) => EquippedWeapon.Weapon.Attack.Timing(baseline);
    public IReadOnlyList<PigState> Targets(PigHerd herd, System.Numerics.Vector2 point) => EquippedWeapon.Weapon.Attack.Targets(herd, point, AttackStats);
    public int ContractLength => _definition.ContractLength;
    public float Radius => AttackStats.Radius;
    public int Damage => AttackStats.Damage;
    public int ShiftPigs { get; private set; }
    public float RecoveryReduction => TotalBonus.Recovery;
    public int PaidBills => _contract.PaidCount;
    public int Capacity => checked(_definition.Tuning.StartingStamina + TotalBonus.Stamina);
    public float ExtraRadius => TotalBonus.Radius;
    public Bill? NextBill => _contract.Next;
    public ShiftReceipt? Receipt { get; private set; }
    public IReadOnlyList<PerkCard> Offers => _perks.Offers;

    public bool Has(string perk) => _perks.Has(perk);

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

    /// <summary>先验证回合状态，再修改猪的血量。空锤也消耗体力；无效命令不改变任何状态。</summary>
    public bool TrySwing(IEnumerable<PigState> targets, out SwingResult? strike, out int coins)
    {
        strike = null;
        coins = 0;
        if (Phase != ShiftPhase.Working || !CanAttack) return false;
        return ResolveStrike(targets, Damage, StaminaCost, out strike, out coins);
    }
    internal bool ApplyEffect(EffectPulse pulse, out SwingResult? strike, out int coins)
    {
        strike = null; coins = 0;
        if (Phase != ShiftPhase.Working) return false;
        return ResolveStrike(pulse.Targets, pulse.Damage, 0, out strike, out coins);
    }
    private bool ResolveStrike(IEnumerable<PigState> targets, int damage, int staminaSpent, out SwingResult? strike, out int coins)
    {
        var prepared = StrikeResolver.Prepare(targets, damage);
        var quote = _rewards.Prepare(prepared, TotalBonus, staminaSpent);
        int nextWallet = checked(Wallet + quote.Payout.Coins);
        int nextEarnings = checked(ShiftEarnings + quote.Payout.Coins);
        int nextStamina = checked(Stamina - staminaSpent + quote.Payout.StaminaRefund);
        int nextPigs = checked(ShiftPigs + prepared.DestroyedCount);
        // Validate every extension before committing health, budgets, counters or money.
        StrikeResolver.Apply(prepared, damage);
        _rewards.Commit(quote);
        prepared.MarkSettled();
        Wallet = nextWallet; ShiftEarnings = nextEarnings; Stamina = nextStamina; ShiftPigs = nextPigs;
        strike = prepared;
        coins = quote.Payout.Coins;
        return true;
    }

    public void CloseShift()
    {
        if (Phase != ShiftPhase.Working) return;
        int previouslyPaid = _contract.PaidCount;
        var payment = _contract.Settle(Shift, Wallet);
        int due = payment.Due, missing = payment.Shortfall, paid = payment.Paid;
        Receipt = new(Shift, Wallet - ShiftEarnings, ShiftEarnings, due, paid, missing,
            ShiftPigs);
        Wallet -= paid;

        if (missing > 0) { Phase = ShiftPhase.Bankrupt; return; }
        if (_contract.PaidCount == previouslyPaid) { Phase = ShiftPhase.Shopping; return; }

        if (NextBill == null) { Phase = ShiftPhase.Won; return; }
        _perks.OfferChoices();
        Phase = Offers.Count > 0 ? ShiftPhase.Choosing : ShiftPhase.Shopping;
    }

    public bool Choose(string kind)
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
    public EquipResult TryEquip(string productId) => Phase == ShiftPhase.Shopping
        ? _inventory.TryEquip(productId) : new(EquipStatus.Closed);
}

