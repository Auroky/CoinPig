using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

internal static class SolidExtensionChecks
{
    private sealed class OpeningGift : ISwingRewardRule
    {
        public SwingPayout Calculate(StrikeSummary strike, Bonus bonus, IRewardBudget budget) =>
            new(strike.HitCount > 0 ? budget.Consume("sample.opening", 1, 1) * 7 : 0, 0);
    }
    private sealed class FailingRule : ISwingRewardRule
    {
        public SwingPayout Calculate(StrikeSummary strike, Bonus bonus, IRewardBudget budget)
        {
            int gift = budget.Consume("sample.failure", 1, 1);
            if (strike.HitCount > 0) throw new InvalidOperationException("Unsuccessful extension.");
            return new(gift, 0);
        }
    }
    private sealed class InvalidReward : ISwingRewardRule
    {
        private readonly int _coins;
        public InvalidReward(int coins) => _coins = coins;
        public SwingPayout Calculate(StrikeSummary strike, Bonus bonus, IRewardBudget budget) => new(_coins, 0);
    }
    private sealed class TestFactory : IGameFactory
    {
        private readonly SessionDefinition _definition;
        private readonly IReadOnlyList<PigSpawn> _spawns;
        public SwingTiming Timing { get; } = new(.1f, .15f, .05f, .2f);
        public TestFactory(SessionDefinition definition, IReadOnlyList<PigSpawn> spawns) { _definition = definition; _spawns = spawns; }
        public GameSession CreateSession() => new(_definition, new Random(23));
        public PigHerd CreateHerd(int shift = 1) => new(_spawns);
    }
    private static PigState Pig() => new(PigContent.Get("ceramic"));
    private static bool Rejects<T>(Action action) where T : Exception
    {
        try { action(); return false; } catch (T) { return true; }
    }

    public static void Run(Action<bool, string> check)
    {
        var path = new HarmonicPath(Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero);
        var definition = new PigDefinition("jade", "Jade pig", 2, 2, 5, RespawnSeconds: .3f, HitRadius: .5f);
        var appearance = new PigAppearance(Body3D: "339977", Body2D: "339977", Nose2D: "55aa88", Scale: 1.2f, Metal: true, Collar: true);
        var routes = new IMotionPath[] { path };
        var archetype = new PigArchetype(definition, appearance, routes);
        routes[0] = HerdLayout.Back(1);
        var registrations = new List<PigArchetype> { archetype };
        var roster = new PigRoster(registrations);
        registrations.Clear();
        check(roster.Get("jade") == definition && roster.GetAppearance("jade") == appearance && roster.Spawns.Count == 1,
            "one unknown-species registration supplies rules, appearance, audio traits and routes");
        check(roster.Spawns[0].Path == path, "registration snapshots protect routes and species lists from caller edits");
        check(roster.GetAppearance("future-unregistered") == new PigAppearance(), "unknown appearances have a safe fallback");
        check(Rejects<ArgumentException>(() => new PigRoster(new[] { archetype, archetype })), "duplicate species registrations fail at composition");
        check(Rejects<ArgumentException>(() => new PigArchetype(definition, appearance with { Body3D = "bad" })), "invalid appearance data fails before scene construction");
        check(Rejects<ArgumentException>(() => new PigState(definition with { RespawnSeconds = 0 })), "invalid lifecycle tuning cannot enter a herd");
        var customHerd = new PigHerd(roster.Spawns);
        check(customHerd.Targets(new(.49f, 0), 0).Length == 1, "species-specific target radius is used without target-selection branches");
        customHerd.Apply(StrikeResolver.Resolve(new[] { customHerd.Pigs[0].State }, 2));
        customHerd.Advance(.31f);
        check(customHerd.Pigs[0].Active && customHerd.Pigs[0].State.Health == 2, "custom respawn timing restores the registered durability");

        var branch = new SkillBranch("tempo", "节奏", "55aa88", 5, "cuff");
        var product = new ShopProduct("training-wrap", "Wrap", "", 0, 0, 1, new(Damage: 1, Stamina: 1), "cuff");
        var skill = new SkillDefinition("rhythm", "Rhythm", "", null, branch, 0, 0, new(PigCoins: 2, Recovery: .02f));
        var perk = new PerkCard("fresh-perk-id", "New perk", "", new(Stamina: 2, Radius: .2f, PairCoins: 3), "pair");
        var config = DefaultGame.Definition(products: new[] { product }, skills: new[] { skill }, perks: new[] { perk },
            bills: new[] { new Bill(2, 1), new Bill(4, 2) }, tuning: new(5, 1, 2));
        check(Rejects<ArgumentException>(() => DefaultGame.Definition(products: new[] { product with { Bonus = new(Damage: -1) } })),
            "equipment validates every field in the unified bonus model");
        check(Rejects<ArgumentException>(() => DefaultGame.Definition(skills: new[] { skill with { Bonus = new(SingleCoins: -1) } })),
            "skills validate every field in the unified bonus model");
        check(Rejects<ArgumentException>(() => DefaultGame.Definition(perks: new[] { perk with { Bonus = new(Recovery: float.NaN) } })),
            "perks validate every field in the unified bonus model");
        var session = new GameSession(config, new Random(1));
        session.BeginShift(); session.TrySwing(new[] { Pig(), Pig() }, out _, out _); session.CloseShift();
        check(session.TryBuy(product.Id, 0).Succeeded && session.TryLearn(skill.Id).Succeeded,
            "unregistered equipment and a new skill branch use the existing purchase contracts");
        session.BeginShift();
        check(session.Capacity == 6 && session.Damage == 2 && session.Radius == 2 && MathF.Abs(session.RecoveryReduction - .02f) < .0001f,
            "the shared bonus model applies equipment and skill effects through one composition path");
        session.TrySwing(new[] { Pig(), Pig() }, out _, out int coins); session.CloseShift();
        check(coins == 8 && session.Offers.Single().Id == perk.Id && session.Choose(perk.Id),
            "a new perk ID needs no enum member or session branch");
        session.BeginShift();
        check(session.Capacity == 8 && MathF.Abs(session.Radius - 2.2f) < .0001f, "perk passives compose with the same equipment and training bonuses");
        session.TrySwing(new[] { Pig(), Pig() }, out _, out coins);
        check(coins == 11, "configured income effects from skills and perks compose without identity checks");

        var rewardRules = DefaultGame.RewardRules.Concat(new[] { new OpeningGift() }).ToArray();
        var rewarding = DefaultGame.Definition(rewardRules: rewardRules);
        var firstSession = new GameSession(rewarding); var secondSession = new GameSession(rewarding);
        firstSession.BeginShift(); secondSession.BeginShift();
        firstSession.TrySwing(new[] { Pig() }, out _, out coins);
        check(coins == 9, "a separately implemented reward rule participates without changing the session or payout engine");
        firstSession.TrySwing(new[] { Pig() }, out _, out coins);
        check(coins == 2, "an extension can use a named daily budget");
        secondSession.TrySwing(new[] { Pig() }, out _, out coins);
        check(coins == 9, "shared immutable reward rules do not share usage between sessions");
        firstSession.CloseShift(); firstSession.BeginShift(); firstSession.TrySwing(new[] { Pig() }, out _, out coins);
        check(coins == 9, "extension budgets reset at the next shift");

        session = new GameSession(DefaultGame.Definition(rewardRules: new ISwingRewardRule[] { new FailingRule() }));
        session.BeginShift(); var target = Pig();
        check(Rejects<InvalidOperationException>(() => session.TrySwing(new[] { target }, out _, out _)), "an unsuccessful reward extension reports its error");
        check(target.Health == 1 && session.Stamina == session.Capacity && session.Wallet == 0 && session.ShiftPigs == 0,
            "failed extensions cannot partially damage targets or spend resources");
        session.TrySwing(Array.Empty<PigState>(), out _, out coins);
        check(coins == 1, "a failed extension does not consume its daily budget");
        foreach (int invalid in new[] { -1, int.MaxValue })
        {
            session = new GameSession(DefaultGame.Definition(rewardRules: new ISwingRewardRule[] { new InvalidReward(invalid) }));
            session.BeginShift(); target = Pig();
            bool rejected = invalid < 0
                ? Rejects<InvalidOperationException>(() => session.TrySwing(new[] { target }, out _, out _))
                : Rejects<OverflowException>(() => session.TrySwing(new[] { target }, out _, out _));
            check(rejected && target.Health == 1 && session.Stamina == session.Capacity && session.Wallet == 0,
                "invalid or overflowing custom payouts fail before any game-state mutation");
        }

        var runConfig = DefaultGame.Definition(tuning: new(3, 1, 1.9f), bills: new[] { new Bill(2, 1) });
        IGameFactory[] factories = { new GameContent(runConfig, roster, new(.1f, .15f, .05f, .2f)), new TestFactory(runConfig, roster.Spawns) };
        foreach (var factory in factories)
        {
            var run = new WorkshopRun(factory);
            var oldSession = run.Session;
            check(run.BeginShift() && !run.BeginShift(), "alternative factories obey the same shift-start contract");
            run.Advance(.05f, null);
            check(run.Session.Stamina == 3 && !run.Hammer.Pending, "no pointer target leaves the simulation idle");
            run.Advance(.05f, Vector2.Zero);
            check(run.Hammer.Pending && !run.EndShift(), "pending strikes prevent early settlement");
            var hit = run.Advance(.05f, new Vector2(100, 100));
            check(hit != null && hit.Point == Vector2.Zero && run.Herd.Pigs.Single().State.Health == 1,
                "aim is locked during windup without relying on Godot input");
            check(run.EndShift(), "a completed strike permits settlement");
            run.Restart();
            check(!ReferenceEquals(oldSession, run.Session) && run.Session.Phase == ShiftPhase.Ready && run.Session.Wallet == 0 &&
                run.Session.Stamina == 3 && run.Session.Radius == 1.9f && run.Herd.Pigs.Single().State.Definition.Id == "jade" &&
                run.Herd.Pigs.Single().State.Health == 2 && !run.Hammer.Pending && run.Hammer.Cooldown == 0,
                "restart clears mutable state while retaining the injected content and tuning");
            run.BeginShift(); int impacts = 0;
            for (int frame = 0; frame < 80 && run.Session.Phase == ShiftPhase.Working; frame++)
                if (run.Advance(.05f, Vector2.Zero) != null) impacts++;
            check(impacts == 3 && run.Session.Phase == ShiftPhase.Shopping && run.Session.Receipt != null,
                "engine-free simulation spends all stamina and automatically settles once");
        }
        session = new GameSession(DefaultGame.Definition(bills: new[] { new Bill(1, 0) }));
        session.BeginShift(); session.CloseShift();
        check(session.Phase == ShiftPhase.Won && session.PaidBills == 1 && session.ContractLength == 1,
            "an injected zero-cost final bill still completes its contract");
        check(Rejects<ArgumentException>(() => DefaultGame.Definition(tuning: new(BaseRadius: float.NaN))), "nonfinite session tuning fails at composition");
        check(Rejects<ArgumentException>(() => new StrikeCycle(new(Windup: 0))), "invalid strike timing fails at composition");
    }
}
