using System;
using System.Collections.Generic;

/// <summary>
/// 会话允许的阶段。命令通过阶段判断是否合法，界面根据阶段选择对应页面。
/// </summary>
public enum ShiftPhase
{
    Ready, Working, Choosing, Shopping, Won, Bankrupt
}

/// <summary>
/// 一次收工的不可变结算记录；区分应付、实付和缺口，便于失败时保留正确余额。
/// </summary>
public sealed record ShiftReceipt(int Shift, int OpeningCash, int Earnings, int Due, int Paid, int Shortfall, int Pigs);

/// <summary>
/// 拥有回合、体力和钱包，协调伤害、租金与成长事务。所有经济变更通过合法命令统一提交。
/// </summary>
public sealed class GameSession : IShop, ISkillTree, ISessionView, IWeaponLoadout, IEquipmentStatusView
{
    private readonly PerkProgress _perkProgress;
    private readonly SwingRewards _swingRewards;
    private readonly RentContract _rentContract;
    private readonly SessionDefinition _definition;
    private readonly SkillProgress _skillProgress;
    private readonly ShopInventory _shopInventory;

    public GameSession(SessionDefinition definition, Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definition = definition;
        _perkProgress = new PerkProgress(definition.Perks, random ?? new Random());
        _skillProgress = new SkillProgress(definition.Skills);
        _shopInventory = new ShopInventory(definition.Products);
        _rentContract = new RentContract(definition.Bills, definition.RentContinuation);
        _swingRewards = new SwingRewards(definition.RewardRules);
        Stamina = definition.Tuning.StartingStamina;
    }

    public ShiftPhase Phase { get; private set; } = ShiftPhase.Ready;
    public bool IsEndless { get; private set; }
    public int Shift { get; private set; } = 1;
    public int Wallet { get; private set; }
    public int ShiftEarnings { get; private set; }
    public int Stamina { get; private set; }
    public Bonus TotalBonus => _shopInventory.TotalBonus.Add(_shopInventory.EquippedBonus).Add(_skillProgress.TotalBonus).Add(_perkProgress.TotalBonus);
    public WeaponLoadout EquippedWeapon
    {
        get
        {
            var product = _shopInventory.EquippedProduct;
            int level = product == null ? 0 : _shopInventory.Level(product.Id);
            return new(
                product?.Weapon ?? _definition.StartingWeapon,
                level,
                product?.TierAt(level - 1).Palette ?? ShopProduct.StandardPalette,
                product?.Id
            );
        }
    }
    public AttackStats AttackStats
    {
        get
        {
            var stats = EquippedWeapon.Weapon.Attack.Stats(_definition.Tuning, TotalBonus);
            if (stats.Damage <= 0 || stats.StaminaCost <= 0 || !float.IsFinite(stats.Radius) || stats.Radius <= 0)
            {
                throw new InvalidOperationException("Weapon strategies must return positive finite attack values.");
            }

            return stats;
        }
    }
    public int StaminaCost => AttackStats.StaminaCost;
    public bool CanAttack => Stamina >= StaminaCost;
    public IReadOnlyList<CombatEffectDefinition> ActiveEffects => _shopInventory.ActiveEffects;
    public EquipmentTier? EquippedAccessory => _shopInventory.AccessoryTier;

    public SwingTiming AttackTiming(SwingTiming baseline) => EquippedWeapon.Weapon.Attack.Timing(baseline);

    public IReadOnlyList<PigState> Targets(PigHerd herd, System.Numerics.Vector2 point) => EquippedWeapon.Weapon.Attack.Targets(herd, point, AttackStats);

    public int ContractLength => _definition.ContractLength;
    public float Radius => AttackStats.Radius;
    public int Damage => AttackStats.Damage;
    public int ShiftPigs { get; private set; }
    public float RecoveryReduction => TotalBonus.Recovery;
    public int PaidBills => _rentContract.PaidCount;
    public int Capacity => checked(_definition.Tuning.StartingStamina + TotalBonus.Stamina);
    public float ExtraRadius => TotalBonus.Radius;
    public Bill? NextBill => _rentContract.Next;
    public ShiftReceipt? Receipt { get; private set; }
    public IReadOnlyList<PerkCard> Offers => _perkProgress.Offers;

    public bool Has(string perk) => _perkProgress.Has(perk);

    public IReadOnlyList<RentDue> GetRentSchedule() => _rentContract.Schedule(Shift, Phase == ShiftPhase.Bankrupt);

    /// <summary>开始下一局并重置本局计数；从通关阶段继续时进入无尽模式，金币和成长保留。</summary>
    public bool BeginShift()
    {
        if (Phase != ShiftPhase.Ready && Phase != ShiftPhase.Shopping && Phase != ShiftPhase.Won)
        {
            return false;
        }

        if (Phase == ShiftPhase.Won)
        {
            IsEndless = true;
        }

        if (Phase != ShiftPhase.Ready)
        {
            Shift++;
        }

        Phase = ShiftPhase.Working;
        ShiftEarnings = 0;
        ShiftPigs = 0;
        Stamina = Capacity;
        _swingRewards.BeginDay();
        return true;
    }

    /// <summary>先验证回合状态，再修改猪的血量。空锤也消耗体力；无效命令不改变任何状态。</summary>
    public bool TrySwing(IEnumerable<PigState> targets, out SwingResult? strike, out int coins)
    {
        strike = null;
        coins = 0;
        if (Phase != ShiftPhase.Working || !CanAttack)
        {
            return false;
        }

        return ResolveStrike(targets, Damage, StaminaCost, out strike, out coins);
    }

    internal bool ApplyEffect(EffectPulse pulse, out SwingResult? strike, out int coins)
    {
        strike = null;
        coins = 0;
        if (Phase != ShiftPhase.Working)
        {
            return false;
        }

        return ResolveStrike(pulse.Targets, pulse.Damage, 0, out strike, out coins);
    }

    private bool ResolveStrike(IEnumerable<PigState> targets, int damage, int staminaSpent, out SwingResult? strike, out int coins)
    {
        // 先在临时结果里计算伤害、奖励及溢出风险；任何验证失败都不改变猪、钱包或奖励次数。
        var preparedStrike = StrikeResolver.Prepare(targets, damage);
        var rewardQuote = _swingRewards.Prepare(preparedStrike, TotalBonus, staminaSpent);
        int nextWallet = checked(Wallet + rewardQuote.Payout.Coins);
        int nextEarnings = checked(ShiftEarnings + rewardQuote.Payout.Coins);
        int nextStamina = checked(Stamina - staminaSpent + rewardQuote.Payout.StaminaRefund);
        int nextPigs = checked(ShiftPigs + preparedStrike.DestroyedCount);
        // 所有计算成功后再统一提交。本方法是本次攻击写入耐久和经济状态的唯一入口。
        StrikeResolver.Apply(preparedStrike, damage);
        _swingRewards.Commit(rewardQuote);
        preparedStrike.MarkSettled();
        Wallet = nextWallet;
        ShiftEarnings = nextEarnings;
        Stamina = nextStamina;
        ShiftPigs = nextPigs;
        strike = preparedStrike;
        coins = rewardQuote.Payout.Coins;
        return true;
    }

    /// <summary>收工并处理到期租金。失败终止本局；中途交租成功提供三选一；最后一笔结清则通关。</summary>
    public void CloseShift()
    {
        if (Phase != ShiftPhase.Working)
        {
            return;
        }

        int previousPaidBillCount = _rentContract.PaidCount;
        // 先交租，再决定失败、胜利或三选一；最终房租结清后直接进入通关状态。
        var payment = _rentContract.Settle(Shift, Wallet);
        int due = payment.Due;
        int missing = payment.Shortfall;
        int paid = payment.Paid;
        Receipt = new(Shift, Wallet - ShiftEarnings, ShiftEarnings, due, paid, missing,
            ShiftPigs);
        Wallet -= paid;

        if (missing > 0)
        {
            Phase = ShiftPhase.Bankrupt;
            return;
        }
        if (_rentContract.PaidCount == previousPaidBillCount)
        {
            Phase = ShiftPhase.Shopping;
            return;
        }

        if (_rentContract.ContractPaid && !IsEndless)
        {
            Phase = ShiftPhase.Won;
            return;
        }
        _perkProgress.OfferChoices();
        Phase = Offers.Count > 0 ? ShiftPhase.Choosing : ShiftPhase.Shopping;
    }

    public bool Choose(string kind)
    {
        if (Phase != ShiftPhase.Choosing || !_perkProgress.Choose(kind))
        {
            return false;
        }

        Phase = ShiftPhase.Shopping;
        return true;
    }

    public IReadOnlyList<SkillOffer> GetSkills() => _skillProgress.Quote(Phase == ShiftPhase.Shopping, Wallet);

    public SkillResult TryLearn(string id)
    {
        // 技能状态验证前置和金币，成功后由回合钱包同步扣款。
        var result = _skillProgress.TryLearn(id, Phase == ShiftPhase.Shopping, Wallet);
        if (result.Succeeded)
        {
            Wallet -= result.CashSpent;
        }

        return result;
    }

    public IReadOnlyList<ShopOffer> GetOffers() => _shopInventory.Quote(Wallet, Phase == ShiftPhase.Shopping);

    public PurchaseResult TryBuy(string productId, int expectedLevel)
    {
        var result = _shopInventory.TryPurchase(productId, expectedLevel, Wallet, Phase == ShiftPhase.Shopping);
        if (result.Succeeded)
        {
            Wallet -= result.CashSpent;
        }

        return result;
    }

    public EquipResult TryEquip(string productId) => Phase == ShiftPhase.Shopping
        ? _shopInventory.TryEquip(productId) : new(EquipStatus.Closed);
}
