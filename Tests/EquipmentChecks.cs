using System;
using System.Linq;
using System.Numerics;

internal static class EquipmentChecks
{
    private sealed class SingleTargetAttack : IWeaponAttack
    {
        public AttackStats Stats(GameTuning tuning, Bonus bonus) => new(4 + bonus.Damage, .6f, 1);
        public SwingTiming Timing(SwingTiming baseline) => baseline with { Windup = .08f, Recovery = .16f, MinimumRecovery = .08f, AimLockSeconds = .01f };
        public System.Collections.Generic.IReadOnlyList<PigState> Targets(PigHerd herd, Vector2 point, AttackStats stats) =>
            herd.Pigs.Where(p => p.Active).OrderBy(p => Vector2.Distance(p.Position, point)).Take(1).Select(p => p.State).ToArray();
    }
    private sealed class FixedFactory : IGameFactory
    {
        readonly SessionDefinition _definition; readonly PigSpawn[] _spawns;
        public FixedFactory(SessionDefinition definition, PigSpawn[] spawns) { _definition = definition; _spawns = spawns; }
        public GameSession CreateSession() => new(_definition, new Random(8));
        public PigHerd CreateHerd(int shift = 1) => new(_spawns);
        public SwingTiming Timing => BalanceContent.Timing;
    }
    private static PigSpawn At(string species, Vector2 point) => new(PigContent.Get(species), new HarmonicPath(point, Vector2.Zero, Vector2.Zero, Vector2.Zero));
    private static void Fund(GameSession session)
    {
        session.BeginShift();
        while (session.CanAttack)
            session.TrySwing(Enumerable.Range(0, 40).Select(_ => new PigState(PigContent.Get("ceramic"))), out _, out _);
        session.CloseShift();
    }
    private static bool Rejects<T>(Action action) where T : Exception
    {
        try { action(); return false; } catch (T) { return true; }
    }
    public static void Run(Action<bool, string> check)
    {
        var progression = DefaultGame.CreateSession(new Random(6)); Fund(progression);
        progression.TryBuy("Stamina", 0);
        float previousRadius = progression.Radius;
        int previousDamage = progression.Damage, previousCapacity = progression.Capacity;
        foreach (var item in EquipmentContent.Products.Where(p => p.Weapon != null).OrderBy(p => p.Stage))
        {
            for (int level = 0; level < item.MaxLevel; level++)
            {
                check(progression.TryBuy(item.Id, level).Succeeded, $"{item.Id} tier {level + 1} can be purchased through the normal progression");
                check(progression.Radius > previousRadius && progression.Damage >= previousDamage &&
                    progression.Capacity > previousCapacity && progression.StaminaCost == 1,
                    $"{item.Id} tier {level + 1} improves reach and stamina without losing damage or affordable attacks");
                previousRadius = progression.Radius; previousDamage = progression.Damage; previousCapacity = progression.Capacity;
            }
        }
        progression.TryBuy("Stamina", 1); progression.TryBuy("Stamina", 2);
        int fullCapacity = progression.Capacity;
        check(progression.Radius <= 2.5f && progression.Capacity <= 26,
            "fully upgraded equipment keeps a focused attack circle and a bounded energy pool before skills and perks");
        progression.TryEquip("Rim");
        check(progression.Capacity == BalanceContent.Tuning.StartingStamina + 6 && progression.Capacity < fullCapacity,
            "switching tools removes the previous tool's capacity bonus instead of stacking every purchased weapon");
        var tieredAccessory = new ShopProduct("tiered", "Tiered", "", 0, 0, 2, new(Stamina: 1),
            Tiers: new[] { new EquipmentTier("One", ShopProduct.StandardPalette, UpgradeBonus: new(Stamina: 2)),
                new EquipmentTier("Two", ShopProduct.StandardPalette, UpgradeBonus: new(Stamina: 3)) });
        var varied = DefaultGame.CreateSession(products: new[] { tieredAccessory });
        varied.BeginShift(); varied.CloseShift(); varied.TryBuy("tiered", 0); varied.TryBuy("tiered", 1);
        check(varied.Capacity == BalanceContent.Tuning.StartingStamina + 5,
            "new products can configure unequal tier bonuses without changing purchase or session code");
        check(Rejects<ArgumentException>(() => new ShopInventory(new[] { tieredAccessory with
            { Tiers = new[] { tieredAccessory.TierAt(0), tieredAccessory.TierAt(1) with { UpgradeBonus = new(Damage: -1) } } } })),
            "invalid tier bonuses are rejected before opening the shop");
        var session = DefaultGame.CreateSession(new Random(3)); Fund(session);
        int balance = session.Wallet;
        check(session.TryBuy("Chainsaw", 0).Status == PurchaseStatus.Locked && session.Wallet == balance,
            "chainsaw prerequisites are enforced by the purchase command before charging");
        check(session.GetOffers().Single(o => o.Product.Id == "Chainsaw").LockReason.Contains("Lv.2"), "locked offers explain their prerequisite levels");
        check(session.TryEquip("Chainsaw").Status == EquipStatus.NotOwned && session.Wallet == balance, "unowned weapons cannot be equipped for free");
        check(session.TryBuy("Rim", 0).Succeeded && session.TryBuy("Rim", 1).Succeeded && session.TryBuy("Stamina", 0).Succeeded,
            "stage-one equipment uses the existing upgrade workflow");
        check(session.EquippedWeapon.Weapon.Id == "hammer" && session.EquippedWeapon.Level == 2 && session.Radius is >= 1.6f and <= 1.75f,
            "hammer upgrades are active only through the selected weapon loadout");
        check(session.EquippedAccessory?.Name == "鎏金振纹护腕" && session.ActiveEffects.Count == 1, "first cuff level installs golden impact resonance");
        check(session.TryBuy("Chainsaw", 0).Succeeded && session.EquippedWeapon.Weapon.Id == "chainsaw", "buying a higher weapon stage equips it automatically");
        check(session.Damage == 3 && session.StaminaCost == 1 && session.Radius is >= 1.75f and <= 2.0f,
            "chainsaw has forgiving broad cutting stats and does not inherit hammer-only radius upgrades");
        var edgeHerd = new PigHerd(new[] { At("ceramic", new(2.05f, 0)), At("ceramic", new(4, 0)) });
        check(session.Targets(edgeHerd, Vector2.Zero).SequenceEqual(new[] { edgeHerd.Pigs[0].State }),
            "copper chainsaw hits a near-edge target that the previous narrow circle missed while excluding distant pigs");
        check(session.AttackTiming(BalanceContent.Timing).Windup == .10f && session.AttackTiming(BalanceContent.Timing).Recovery == .23f,
            "weapon timing is supplied by the attack strategy");
        check(session.TryBuy("Rim", 2).Succeeded && session.EquippedWeapon.Weapon.Id == "chainsaw", "upgrading an older weapon does not unexpectedly replace a higher-stage weapon");
        balance = session.Wallet;
        check(session.TryEquip("Rim").Succeeded && session.EquippedWeapon.Level == 3 && session.Damage == 3 && session.Wallet == balance,
            "switching to an owned weapon is free and restores its own upgraded stats");
        check(session.TryEquip("Stamina").Status == EquipStatus.NotAWeapon && session.Wallet == balance, "accessories cannot replace the selected weapon");
        check(session.TryBuy("Stamina", 1).Succeeded && session.EquippedAccessory?.Name == "共振护腕" && session.ActiveEffects.Count == 2,
            "second cuff level adds automatic cracking without stacking the obsolete first-level effect instance");
        check(session.TryBuy("Stamina", 2).Succeeded && session.EquippedAccessory?.Name == "金鸣护腕" && session.Capacity == 20,
            "third cuff level installs broad resonance and keeps the cumulative stamina bonus");
        check(session.TryBuy("Grinder", 0).Status == PurchaseStatus.Locked, "grinder stays locked until the configured chainsaw level");
        check(session.TryBuy("Chainsaw", 1).Succeeded && session.TryBuy("Grinder", 0).Succeeded,
            "the final stage unlocks through data-driven prerequisite links");
        check(session.EquippedWeapon.Weapon.Id == "grinder" && session.Damage == 5 && session.StaminaCost == 1 && session.Radius > 2,
            "grinder applies broad high damage with one-energy cost");
        balance = session.Wallet;
        check(session.TryBuy("Grinder", 0).Status == PurchaseStatus.StaleOffer && session.Wallet == balance, "new weapons retain stale-offer protection");
        session.BeginShift();
        check(session.TryEquip("Rim").Status == EquipStatus.Closed, "equipment changes cannot alter an in-flight working shift");
        var tin = new PigState(PigContent.Get("tin")); int stamina = session.Stamina;
        session.TrySwing(new[] { tin }, out _, out int coins);
        check(!tin.IsAlive && session.Stamina == stamina - 1 && coins == PigContent.Get("tin").Coins, "grinder damage and energy are settled once through the same atomic command");

        var bad = new ShopProduct("a", "A", "", 0, 0, 1, new(), Requirements: new[] { new ProductRequirement("missing", 1) });
        check(Rejects<ArgumentException>(() => new ShopInventory(new[] { bad })), "missing equipment prerequisites are rejected at composition");
        var a = bad with { Requirements = new[] { new ProductRequirement("b", 1) } };
        var b = bad with { Id = "b", Requirements = new[] { new ProductRequirement("a", 1) } };
        check(Rejects<ArgumentException>(() => new ShopInventory(new[] { a, b })), "cyclic equipment progression is rejected");
        var orb = new WeaponDefinition("orb", "Test orb", new SingleTargetAttack(), "new-orb", "new-motion");
        var product = new ShopProduct("orb-item", "Test orb", "", 0, 0, 1, new(), Stage: 4, Weapon: orb);
        var configured = DefaultGame.Definition(products: new[] { product });
        session = new GameSession(configured); session.BeginShift(); session.CloseShift(); session.TryBuy(product.Id, 0); session.BeginShift();
        var herd = new PigHerd(new[] { At("tin", Vector2.Zero), At("tin", new(1, 0)) });
        session.TrySwing(session.Targets(herd, Vector2.Zero), out _, out coins);
        check(session.EquippedWeapon.Weapon == orb && !herd.Pigs[0].Active && herd.Pigs[1].Active && coins == PigContent.Get("tin").Coins,
            "an unknown weapon and a new targeting strategy require no branch in the session or herd");

        var run = new WorkshopRun(new FixedFactory(DefaultGame.Definition(tuning: new(3), startingWeapon: new WeaponDefinition("two-energy", "Test", new CircularWeaponAttack(extraDamage: 2, staminaCost: 2))),
            new[] { At("tin", Vector2.Zero) }));
        run.BeginShift(); int frames = 0;
        while (run.Session.Phase == ShiftPhase.Working && frames++ < 100) run.Advance(.05f, Vector2.Zero);
        check(run.Session.Phase == ShiftPhase.Shopping && run.Session.Stamina == 1 && run.Session.ShiftPigs == 0 && run.Herd.Pigs.Single().State.Health == 1,
            "a multi-energy weapon settles an odd leftover even if the target survives");

        run = new WorkshopRun(new FixedFactory(DefaultGame.Definition(), new[] { At("tin", Vector2.Zero) }));
        Fund(run.Session); run.Session.TryBuy("Stamina", 0); run.Session.TryBuy("Stamina", 1); run.BeginShift();
        balance = run.Session.Wallet; stamina = run.Session.Stamina;
        for (int i = 0; i < 61; i++) run.Advance(.05f, null);
        check(run.Herd.Pigs[0].State.Health == PigContent.Get("tin").MaxHealth - 1 && run.Session.Wallet == balance && run.Session.Stamina == stamina,
            "automatic resonance gradually cracks a heavy pig without a pointer or energy charge");
        for (int i = 0; i < (PigContent.Get("tin").MaxHealth - 1) * 61; i++) run.Advance(.05f, null);
        check(run.Session.Wallet == balance + PigContent.Get("tin").Coins && run.Session.Stamina == stamina,
            "AFK cracking grants currency only when the pig actually breaks");
        for (int i = 0; i < 1200; i++) run.Advance(.05f, null);
        int capped = run.Session.Wallet;
        for (int i = 0; i < 1200; i++) run.Advance(.05f, null);
        check(run.Session.Wallet == capped && capped > balance && run.Session.Phase == ShiftPhase.Working,
            "the visible daily resonance budget prevents unlimited AFK currency");

        run = new WorkshopRun(new FixedFactory(DefaultGame.Definition(), new[] { At("tin", Vector2.Zero), At("tin", new(1, 0)), At("tin", new(2, 0)) }));
        Fund(run.Session); for (int level = 0; level < 3; level++) run.Session.TryBuy("Stamina", level);
        run.BeginShift(); stamina = run.Session.Stamina; balance = run.Session.Wallet;
        StrikeOutcome? strike = null;
        for (int i = 0; i < 50 && strike == null; i++) strike = run.Advance(.05f, Vector2.Zero);
        var wave = run.EffectOutcomes.Single();
        check(strike != null && wave.Radius == 3.2f && wave.Strike.DestroyedCount == 0 && run.Herd.Pigs.Select(p => p.State.Health).SequenceEqual(new[] { 1, 1, 2 }),
            "golden chime cuff damages the broad cluster without instantly breaking fresh tin pigs");
        check(run.Session.Stamina == stamina - 1 && run.Session.Wallet == balance + strike!.Coins + wave.Coins,
            "resonance reuses damage and coin settlement without charging or paying twice");
        var firstInstance = run.Session.ActiveEffects[0].Create(); var secondInstance = run.Session.ActiveEffects[0].Create();
        check(!ReferenceEquals(firstInstance, secondInstance), "effect factories provide independent mutable budgets for new shifts and runs");
    }
}


