using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

internal static class ArchitectureChecks
{
    private sealed class StraightPath : IMotionPath
    {
        public Vector2 Sample(float elapsed)
        {
            if (!float.IsFinite(elapsed) || elapsed < 0) throw new ArgumentOutOfRangeException(nameof(elapsed));
            return new(elapsed / 10, 2);
        }
    }

    private static bool Rejects<T>(Action action) where T : Exception
    {
        try { action(); return false; }
        catch (T) { return true; }
    }

    private static PigState Pig() => new(PigCatalog.Get("ceramic"));

    private static IEnumerable<PigState> InterruptedTargets(PigState pig)
    {
        yield return pig;
        throw new InvalidOperationException("Interrupted target source.");
    }

    private static void FundDay(GameSession session)
    {
        while (session.Stamina > 0)
            session.TrySwing(Enumerable.Range(0, 4).Select(_ => Pig()), out _, out _);
        session.CloseShift();
    }

    public static void Run(Action<bool, string> check)
    {
        var product = new ShopProduct("gloves", "Gloves", "", 3, 0, 1, new(Stamina: 1));
        var products = new List<ShopProduct> { product };
        var session = new GameSession(products: products);
        products[0] = product with { BasePrice = 0, Bonus = new(Stamina: 99) };
        products.Clear();
        check(session.GetOffers().Single().Product == product, "shop owns a snapshot rather than the caller's mutable catalogue");
        check(Rejects<ArgumentException>(() => new GameSession(products: new[] { product with { BasePrice = -1 } })), "negative prices are rejected before a session can mint money");
        check(Rejects<ArgumentException>(() => new GameSession(products: new[] { product, product })), "duplicate product identities are rejected");
        check(Rejects<ArgumentException>(() => new GameSession(products: new[] { product with { BasePrice = int.MaxValue, PriceStep = 10 } })), "overflowing price configuration is rejected");
        check(Rejects<ArgumentException>(() => new PigState(PigCatalog.Get("ceramic") with { MaxHealth = 0 })), "invalid pig health cannot enter the world");

        // The same client contract works with both default and injected catalogues.
        foreach (var candidate in new[] { new GameSession(), session })
        {
            IShop shop = candidate;
            string id = shop.GetOffers()[0].Product.Id;
            check(shop.TryBuy(id, 0).Status == PurchaseStatus.Closed && shop.Wallet == 0, "shop contract rejects closed purchases without charging");
            candidate.BeginShift(); FundDay(candidate);
            int balance = shop.Wallet;
            var quote = shop.GetOffers()[0];
            var purchase = shop.TryBuy(id, quote.Level);
            check(purchase.Succeeded && shop.Wallet == balance - quote.Price, "shop contract charges the quoted price exactly once");
            balance = shop.Wallet;
            check(!shop.TryBuy(id, quote.Level).Succeeded && shop.Wallet == balance, "shop contract preserves balance for a stale offer");
        }

        var root = new SkillDefinition("practice", "Practice", "", null, SkillBranch.Hammer, 0, 1, new(Damage: 1));
        var child = new SkillDefinition("collect", "Collect", "", "practice", SkillBranch.Income, 1, 2, new(PigCoins: 1));
        var skills = new List<SkillDefinition> { root, child };
        session = new GameSession(skills: skills);
        skills.Clear();
        check(session.GetSkills().Count == 2, "skill catalogue is injectable and protected from external edits");
        check(Rejects<ArgumentException>(() => new GameSession(skills: new[] { child })), "missing skill prerequisites are rejected");
        check(Rejects<ArgumentException>(() => new GameSession(skills: new[] { root with { ParentId = "collect" }, child })), "cyclic skill prerequisites are rejected");
        check(Rejects<ArgumentException>(() => new GameSession(skills: new[] { root with { Cost = -1 } })), "negative skill cost is rejected");
        ISkillTree tree = session;
        check(tree.TryLearn("practice").Status == SkillStatus.Closed, "skill contract rejects learning during a closed phase");
        session.BeginShift(); FundDay(session);
        int cash = session.Wallet;
        check(!tree.TryLearn("collect").Succeeded && session.Wallet == cash, "skill contract preserves state when prerequisites are missing");
        check(tree.TryLearn("practice").Succeeded && session.Damage == 2, "an injected skill uses the existing learning flow");
        cash = session.Wallet;
        check(!tree.TryLearn("practice").Succeeded && session.Wallet == cash, "skill contract rejects a duplicate purchase");
        check(tree.TryLearn("collect").Succeeded && session.Wallet == cash - 2, "injected prerequisite links unlock the dependent skill");

        IMotionPath[] paths = { HerdLayout.Middle(1), HerdLayout.Back(-1), HerdLayout.Front(1), HerdLayout.Side(-1), new StraightPath() };
        foreach (var path in paths)
        {
            foreach (float time in new[] { 0f, .25f, 10f })
            {
                Vector2 position = path.Sample(time);
                check(float.IsFinite(position.X) && float.IsFinite(position.Y) && path.Sample(time) == position,
                    "motion implementations return deterministic finite world positions");
            }
            check(Rejects<ArgumentOutOfRangeException>(() => path.Sample(-1)), "motion implementations share the nonnegative-time contract");
        }
        var customPig = new PigDefinition("clockwork", "Clockwork", 2, 1, 2);
        var herd = new PigHerd(new[] { new PigSpawn(customPig, new StraightPath()) });
        herd.Advance(.5f);
        check(Vector2.Distance(herd.Pigs[0].Position, new Vector2(.1f, 2)) < .00001f, "a different path and species work without modifying the herd");
        var motion = herd.Pigs[0];
        var strike = StrikeResolver.Resolve(new[] { motion.State }, 1);
        herd.Apply(strike); herd.Advance(.5f); herd.Apply(strike); herd.Advance(.36f);
        check(motion.Active, "replaying a hit notification does not restart the respawn timer");
        herd.Apply(strike);
        check(motion.Active, "an old hit notification cannot hide a respawned pig");

        // One identity can carry combined data effects; no type branch is added to the session or payout code.
        var card = new PerkCard(Perk.Lunch, "Packed lunch", "", new(Stamina: 3, Radius: .25f,
            PairCoins: 4, SingleCoins: 3, Refunds: 1, BonusCoinLimit: 3));
        var cards = new List<PerkCard> { card };
        session = new GameSession(perks: cards); cards.Clear();
        session.BeginShift(); FundDay(session); session.BeginShift(); FundDay(session);
        check(session.Offers.Single() == card, "reward offers use a protected injected catalogue");
        check(Rejects<NotSupportedException>(() => ((IList<PerkCard>)session.Offers)[0] = card with { Bonus = new(Stamina: 99) }), "available rewards cannot be replaced through a mutable cast");
        check(session.Choose(Perk.Lunch) && session.Capacity == 15 && session.ExtraRadius == .25f, "perk values determine passive bonuses instead of enum-specific branches");
        session.BeginShift();
        session.TrySwing(new[] { Pig(), Pig() }, out _, out int coins);
        check(coins == 10 && session.Stamina == 15, "configured pair reward, limited bonus and refund compose correctly");
        session.TrySwing(new[] { Pig() }, out _, out coins);
        check(coins == 6, "single-target reward consumes only the remaining limited bonus");
        session.TrySwing(new[] { Pig() }, out _, out coins);
        check(coins == 5, "limited coin bonus stops at its configured cap");
        session.CloseShift(); session.BeginShift();
        session.TrySwing(new[] { Pig(), Pig() }, out _, out coins);
        check(coins == 10 && session.Stamina == 15, "per-day reward limits reset together on a new day");

        session = new GameSession(); session.BeginShift();
        var target = Pig();
        check(Rejects<InvalidOperationException>(() => session.TrySwing(InterruptedTargets(target), out _, out _)), "a broken target source rejects the command");
        check(target.Health == 1 && session.Stamina == 12 && session.Wallet == 0, "target enumeration failure leaves health and resources unchanged");
        check(Rejects<ArgumentException>(() => session.TrySwing(new[] { target, null! }, out _, out _)), "null targets are rejected before damage");
        check(target.Health == 1 && session.Stamina == 12 && session.Wallet == 0, "invalid target validation cannot partially apply a swing");
    }
}
