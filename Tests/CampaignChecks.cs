using System;
using System.Linq;

internal static class CampaignChecks
{
    public static void Run(Action<bool, string> check)
    {
        var run = new WorkshopRun(DefaultGame.Content);
        for (int day = 1; day <= 10; day++)
        {
            check(run.BeginShift() && run.Session.Shift == day, "campaign starts the expected day");
            var armored = run.Herd.Pigs.Where(p => p.State.Definition.Id == "armored").ToArray();
            check(armored.Length == (day >= 6 ? 1 : 0), "armored pig unlocks after five completed days");
            if (armored.Length > 0) check(armored[0].State.Health == 5, "new shift restores armored durability");
            if (day == 1) run.Session.TrySwing(new[] { new PigState(new("fund", "Fund", 1, 1, 452, "fund")) }, out _, out _);
            run.EndShift();
            check(run.Session.Receipt!.Paid == (BalanceContent.Bills.SingleOrDefault(bill => bill.Shift == day)?.Amount ?? 0),
                "rent is charged only on its scheduled day, including the final bill");
            if (day < 10)
            {
                check(run.Session.Wallet == 452 - BalanceContent.Bills.Where(bill => bill.Shift <= day).Sum(bill => bill.Amount) && run.Session.Phase == (day % 2 == 0 ? ShiftPhase.Choosing : ShiftPhase.Shopping),
                    "intermediate payments preserve the correct balance and grant growth choices");
                if (run.Session.Phase == ShiftPhase.Choosing) run.Session.Choose(run.Session.Offers[0].Id);
            }
        }
        check(run.Session.Phase == ShiftPhase.Won && run.Session.Wallet == 0 && run.Session.NextBill == null, "day ten wins only after paying the final rent");
        for (int day = 11; day <= 14; day++)
        {
            run.BeginShift(); run.EndShift();
            check(run.Session.IsEndless && run.Session.Phase == ShiftPhase.Shopping && run.Session.Receipt!.Due == 0,
                "endless remains playable with zero money and no new bills");
        }
        var shortRun = DefaultGame.CreateSession();
        for (int day = 1; day <= 10; day++)
        {
            shortRun.BeginShift();
            if (day == 1) shortRun.TrySwing(new[] { new PigState(new("fund", "Fund", 1, 1, 451, "fund")) }, out _, out _);
            shortRun.CloseShift();
            if (day < 10) check(shortRun.Phase != ShiftPhase.Bankrupt, "funds cover every intermediate bill before final shortfall");
            if (shortRun.Phase == ShiftPhase.Choosing) shortRun.Choose(shortRun.Offers[0].Id);
        }
        check(shortRun.Phase == ShiftPhase.Bankrupt && shortRun.Wallet == 155 && shortRun.PaidBills == 4 &&
            shortRun.Receipt!.Due == 156 && shortRun.Receipt.Paid == 0 && shortRun.Receipt.Shortfall == 1 && !shortRun.BeginShift(),
            "missing one coin on the final bill prevents victory and endless without a partial debit");
        run.Restart(); run.BeginShift();
        check(run.Session.Shift == 1 && run.Herd.Pigs.All(p => p.State.Definition.Id != "armored") && run.Session.Wallet == 0,
            "restart clears progression and removes the late pig");
        var roster = new PigRoster(new[] { new PigArchetype(new("custom", "Custom", 1, 5, 1, "custom"), new(), 3, HerdLayout.Side(1)) });
        check(roster.SpawnsForShift(2).Count == 0 && roster.SpawnsForShift(3).Count == 1,
            "custom species can choose another unlock day without changes to the domain");
        bool invalid = false;
        try { roster.SpawnsForShift(0); } catch (ArgumentOutOfRangeException) { invalid = true; }
        check(invalid, "invalid spawn day is rejected");
        var pig = new PigState(PigContent.Get("armored"));
        StrikeResolver.Resolve(new[] { pig }, 3);
        check(pig.Health == 2 && pig.IsAlive, "upgraded damage still reduces armored durability normally");
        StrikeResolver.Resolve(new[] { pig }, 3);
        check(!pig.IsAlive, "damage upgrades can defeat armored pigs in fewer strikes");
    }
}
