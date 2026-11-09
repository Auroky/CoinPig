using System;
using System.Linq;

static class Program
{
    private static int _checks;
    static void Check(bool pass, string name) { if (!pass) throw new Exception(name); _checks++; }
    static PigState Pig(string id = "ceramic") => new(PigCatalog.Get(id));
    static void Earn(GameSession run)
    {
        int safety = 0;
        while (run.Stamina > 0 && safety++ < 80)
            run.TrySwing(Enumerable.Range(0, 8).Select(_ => Pig("swift")), out _, out _);
        Check(safety < 80, "finite stamina even with refunds");
    }
    static void Main()
    {
        var run = new GameSession(new Random(42)); var tin = Pig("tin");
        Check(!run.TrySwing(new[] { tin }, out _, out _) && tin.Health == 3, "invalid phase cannot damage pigs");
        Check(run.TryBuy("Rim", 0).Status == PurchaseStatus.Closed && run.Wallet == 0, "closed shop preserves wallet");
        Check(run.BeginShift() && !run.BeginShift(), "begin is not repeatable during work");
        Check(run.TrySwing(new[] { tin, tin }, out var first, out int coins) && tin.Health == 2 && coins == 0 && run.Stamina == 11, "one damage and one stamina for duplicate targets");
        int stamina = run.Stamina;
        Check(first!.IsSettled && typeof(GameSession).GetMethod("LandSwing") == null, "settlement is completed internally and has no public replay entry");
        run.TrySwing(new[] { tin }, out _, out _); run.TrySwing(new[] { tin }, out _, out coins);
        Check(!tin.IsAlive && coins == 6 && run.Wallet == 6, "tin pays only at destruction");
        run.TrySwing(new[] { tin }, out _, out coins);
        Check(coins == 0 && run.Wallet == 6, "dead pig cannot pay twice");
        tin.Respawn(); Check(tin.Health == 3, "respawn restores maximum health");
        stamina = run.Stamina; run.TrySwing(Array.Empty<PigState>(), out _, out coins);
        Check(coins == 0 && run.Stamina == stamina - 1, "miss costs one stamina");
        run.CloseShift();
        Check(run.TryBuy("Rim", 0).Status == PurchaseStatus.InsufficientFunds && run.Wallet == 6 && run.ExtraRadius == 0, "unaffordable purchase changes nothing");
        Check(run.TryBuy("missing", 0).Status == PurchaseStatus.UnknownProduct, "unknown item rejected");
        Check(!run.TryLearn("power2").Succeeded && run.Wallet == 6 && run.Damage == 1, "locked skill changes nothing");
        Check(!run.TryLearn("missing").Succeeded && run.Wallet == 6, "unknown skill rejected");
        run.BeginShift(); run.CloseShift();
        Check(run.Phase == ShiftPhase.Bankrupt && run.Receipt!.Shortfall == 22 && run.Wallet == 6, "rent failure keeps unpaid cash and records shortfall");
        Check(!run.BeginShift() && !run.TrySwing(new[] { tin }, out _, out _), "bankruptcy is terminal");

        run = new GameSession(new Random(7)); run.BeginShift(); Earn(run); run.CloseShift();
        int before = run.Wallet;
        Check(run.TryBuy("Rim", 0).Succeeded && run.Wallet == before - 18, "purchase price debited once");
        before = run.Wallet;
        Check(run.TryBuy("Rim", 0).Status == PurchaseStatus.StaleOffer && run.Wallet == before, "stale double click does not debit twice");
        int previousStamina = run.Stamina;
        Check(run.TryBuy("Stamina", 0).Succeeded && run.Capacity == 13 && run.Stamina == previousStamina, "capacity upgrade does not refill current day");
        Check(run.TryLearn("wide").Succeeded && run.TryLearn("power1").Succeeded && run.Damage == 2, "skill prerequisites unlock damage upgrade");
        before = run.Wallet; Check(!run.TryLearn("wide").Succeeded && run.Wallet == before, "skill cannot be bought twice");
        run.BeginShift(); Check(run.Stamina == run.Capacity, "new day refills upgraded capacity");
        for (int day = 2; day <= 10; day++)
        {
            Earn(run); run.CloseShift();
            Check(run.Phase != ShiftPhase.Bankrupt, "funded run survives rent");
            if (day % 2 == 0) Check(run.PaidBills == day / 2, "one bill paid on due date");
            if (run.Phase == ShiftPhase.Choosing)
            {
                Check(run.Offers.Count > 0 && run.Offers.All(x => !run.Has(x.Kind)), "offers exclude owned perks");
                var pick = run.Offers[0].Kind; Check(run.Choose(pick) && !run.Choose(pick), "perk picked once");
            }
            if (day < 10) Check(run.BeginShift(), "next day starts");
        }
        Check(run.Phase == ShiftPhase.Won && run.PaidBills == 5 && run.NextBill == null, "ten-day victory");
        before = run.Wallet; Check(run.BeginShift() && run.IsEndless && run.Shift == 11, "endless preserves successful run");
        run.CloseShift(); Check(run.Wallet == before && run.Phase == ShiftPhase.Shopping, "endless has no extra bill");
        var reset = new GameSession(); Check(reset.Wallet == 0 && reset.Damage == 1 && reset.Capacity == 12 && !reset.IsEndless, "restart state is clean");
        var contract = new RentContract(new[] { new Bill(2, 10), new Bill(4, 20) });
        Check(contract.Settle(1, 100).Paid == 0 && contract.PaidCount == 0, "rent cannot be collected early");
        Check(contract.Settle(2, 9).Shortfall == 1 && contract.PaidCount == 0, "failed rent preserves outstanding bill");
        Check(contract.Settle(2, 10).Paid == 10 && contract.PaidCount == 1, "exact rent advances bill");
        Check(contract.Settle(2, 100).Paid == 0 && contract.PaidCount == 1, "same bill cannot be collected twice");
        var herd = new PigHerd(HerdLayout.Initial);
        Check(herd.Pigs.Count == 2 && herd.Pigs.All(p => p.State.Definition.Id == "ceramic"), "default herd contains exactly two ordinary pigs");
        var at = herd.Pigs[0].Position;
        herd.Advance(.2f);
        Check(herd.Pigs[0].Position != at, "world movement runs without a rendering engine");
        var pig = herd.Pigs[0];
        var hit = StrikeResolver.Resolve(new[] { pig.State }, 1);
        herd.Apply(hit);
        Check(!pig.Active, "destroyed pig leaves target set");
        herd.Advance(.9f);
        Check(pig.Active && pig.State.Health == pig.State.Definition.MaxHealth, "world lifecycle respawns pig");
        var custom = new ShopProduct("gloves", "Gloves", "Extra stamina", 1, 0, 1, new UpgradeBonus(Stamina: 3));
        var configured = new GameSession(products: new[] { custom });
        configured.BeginShift(); configured.TrySwing(new[] { Pig() }, out _, out _); configured.CloseShift();
        IShop shop = configured;
        Check(shop.GetOffers().Count == 1 && shop.GetOffers()[0].Product.Id == "gloves", "shop lists injected catalogue through interface");
        Check(shop.TryBuy("gloves", 0).Succeeded && configured.Capacity == 15, "new data variant uses unchanged purchase workflow");
        ArchitectureChecks.Run(Check);
        Console.WriteLine($"PASS: {_checks} core checks. No Godot runtime required.");
    }
}
