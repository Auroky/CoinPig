using System;
using System.Linq;

internal static class LeaseChecks
{
    private sealed class StepRent : IRentContinuation
    {
        public Bill NextAfter(Bill previous) => new(previous.Shift + 2, previous.Amount + 5);
    }
    private sealed class InvalidRent : IRentContinuation
    {
        public Bill NextAfter(Bill previous) => previous;
    }
    private static bool Rejects<T>(Action action) where T : Exception
    { try { action(); return false; } catch (T) { return true; } }
    public static void Run(Action<bool, string> check)
    {
        check(BalanceContent.Bills.Count == 15 && BalanceContent.Bills.Last().Shift == 30,
            "default campaign contains thirty shifts and fifteen visible rent payments");
        check(BalanceContent.Bills.Zip(BalanceContent.Bills.Skip(1), (a, b) => b.Shift == a.Shift + 2 && b.Amount > a.Amount).All(x => x),
            "campaign rents increase on a predictable two-day cycle");
        var maxBonus = EquipmentContent.Products.Single(p => p.Id == "Grinder").BonusThrough(3)
            .Add(EquipmentContent.Products.Single(p => p.Id == "Stamina").BonusThrough(3));
        foreach (var skill in SkillContent.Nodes) maxBonus = maxBonus.Add(skill.Bonus);
        foreach (var perk in PerkContent.Cards) maxBonus = maxBonus.Add(perk.Bonus);
        check(BalanceContent.Tuning.StartingStamina + maxBonus.Stamina <= 31 && SkillContent.Nodes.Sum(s => s.Bonus.Stamina) == 3,
            "all equipment, skill and perk stamina stacks stay bounded while every endurance tier still contributes");
        check(EquipmentContent.Grinder.Attack.Stats(BalanceContent.Tuning, maxBonus).Radius <= 2.75f,
            "even a fully stacked attack circle stays focused in the thirty-day campaign");
        var growth = new CompoundingRent(2, 8, 20, 10);
        var next = growth.NextAfter(new(30, 900))!;
        check(next == new Bill(32, 980) && growth.NextAfter(next) == new Bill(34, 1060),
            "continuing after the formal contract generates rounded compound rent");
        check(growth.NextAfter(new(2, 10)) == new Bill(4, 30), "small renewal bills still have a meaningful minimum increase");
        check(growth.NextAfter(new(int.MaxValue - 3, int.MaxValue - 1))?.Amount == int.MaxValue,
            "extreme renewed prices saturate safely instead of overflowing");
        check(growth.NextAfter(new(int.MaxValue - 1, 20)) == null, "renewal day overflow terminates the schedule safely");
        check(Rejects<ArgumentException>(() => new CompoundingRent(0, 8, 20)) &&
            Rejects<ArgumentException>(() => new CompoundingRent(2, -1, 20)), "invalid renewal configuration is rejected");
        var contract = new RentContract(new[] { new Bill(1, 10) }, new StepRent());
        check(contract.Settle(1, 200).Paid == 10 && contract.ContractPaid && contract.Next == new Bill(3, 15),
            "finishing the finite contract does not remove renewal obligations");
        var schedule = contract.Schedule(2, false);
        check(contract.PaidCount == 1 && schedule.Count == 4 && schedule.Skip(1).All(r => r.Renewal && !r.Paid),
            "ledger previews three renewal bills without charging or advancing the contract");
        check(contract.Settle(2, 190).Paid == 0 && contract.PaidCount == 1, "renewal cannot be charged before its due day");
        check(contract.Settle(3, 14).Shortfall == 1 && contract.PaidCount == 1 && contract.Next!.Amount == 15,
            "failed renewal preserves the amount and paid count");
        check(contract.Settle(3, 15).Paid == 15 && contract.Next == new Bill(5, 20), "successful renewal advances exactly once");
        check(contract.Settle(3, 200).Paid == 0 && contract.PaidCount == 2, "duplicate settlement cannot pay the same renewal again");
        var invalid = new RentContract(new[] { new Bill(1, 0) }, new InvalidRent()); invalid.Settle(1, 0);
        check(Rejects<InvalidOperationException>(() => { _ = invalid.Next; }) && invalid.PaidCount == 1,
            "invalid injected renewal rules are rejected without advancing payment");
        var definition = DefaultGame.Definition(bills: new[] { new Bill(1, 10) }, perks: Array.Empty<PerkCard>(),
            products: Array.Empty<ShopProduct>(), skills: Array.Empty<SkillDefinition>(), rentContinuation: new StepRent());
        var run = new WorkshopRun(new GameContent(definition, PigContent.Roster));
        run.BeginShift(); run.Session.TrySwing(new[] { new PigState(new("test-bank", "Bank", 1, 1, 200, "test")) }, out _, out _);
        run.EndShift();
        check(run.Session.Phase == ShiftPhase.Won && run.Session.Wallet == 190 && run.Session.NextBill == new Bill(3, 15),
            "an unknown continuation policy composes through the normal session and victory path");
        run.BeginShift(); run.EndShift();
        check(run.Session.IsEndless && run.Session.Phase == ShiftPhase.Shopping && run.Session.Wallet == 190,
            "renewal keeps money and upgrades while its first rest day has no early debit");
        run.BeginShift(); run.EndShift();
        check(run.Session.Wallet == 175 && run.Session.Phase == ShiftPhase.Shopping && run.Session.NextBill == new Bill(5, 20),
            "renewal mode charges real rent and does not repeatedly declare victory");
        run.BeginShift(); run.EndShift(); run.BeginShift(); run.EndShift();
        check(run.Session.Wallet == 155 && run.Session.PaidBills == 3, "multiple renewals continue increasing and settling");
        run.Restart();
        check(run.Session.Phase == ShiftPhase.Ready && run.Session.Wallet == 0 && run.Session.PaidBills == 0 && run.Session.NextBill == new Bill(1, 10),
            "restarting clears generated renewal state without changing the injected policy");
        var finite = new RentContract(new[] { new Bill(1, 5) }); finite.Settle(1, 5);
        check(finite.ContractPaid && finite.Next == null && finite.Schedule(1, false).Count == 1,
            "explicit finite custom contracts retain their existing terminal behavior");
    }
}
