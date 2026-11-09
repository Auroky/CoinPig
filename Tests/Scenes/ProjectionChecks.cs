using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

internal static class ProjectionChecks
{
    public static string Run(IWorkbench surface)
    {
        var trace = new List<string>();
        foreach (bool paired in new[] { true, false })
        {
            var direct = new GameSession(new Random(4702));
            var projected = new GameSession(new Random(4702));
            for (int day = 1; day <= 10; day++)
            {
                direct.BeginShift(); projected.BeginShift();
                var firstHerd = new PigHerd(HerdLayout.Initial);
                var secondHerd = new PigHerd(HerdLayout.Initial);
                int frames = 0;
                while (direct.Stamina > 0 && frames++ < 20000)
                {
                    firstHerd.Advance(.05f); secondHerd.Advance(.05f);
                    Vector2 target = paired ? (firstHerd.Pigs[0].Position + firstHerd.Pigs[1].Position) / 2 : new(7.3f, 3.7f);
                    float radius = 1.43f + direct.ExtraRadius;
                    if (paired && firstHerd.Pigs.Take(2).Any(p => !p.Active || Vector2.Distance(p.Position, target) > radius - .4f)) continue;
                    if (!surface.TryWorldPosition(surface.ScreenPosition(target), out var recovered))
                        throw new Exception("A reachable world target could not be picked through the view.");
                    direct.TrySwing(firstHerd.Targets(target, radius), out var first, out int a);
                    projected.TrySwing(secondHerd.Targets(recovered, radius), out var second, out int b);
                    firstHerd.Apply(first!); secondHerd.Apply(second!);
                    string stateA = State(direct, firstHerd, a);
                    string stateB = State(projected, secondHerd, b);
                    if (stateA != stateB) throw new Exception("View projection changed rule outcomes: " + stateA + " / " + stateB);
                    trace.Add((paired ? "paired;" : "missed;") + stateB);
                }
                if (frames >= 20000) throw new Exception("Projection check could not finish a workday.");
                direct.CloseShift(); projected.CloseShift();
                if (direct.Receipt != projected.Receipt || direct.Phase != projected.Phase || direct.Wallet != projected.Wallet)
                    throw new Exception("View projection changed a settlement.");
                trace.Add($"settled;{paired};{day};{projected.Phase};{projected.Wallet};{projected.Receipt!.Paid}");
                if (direct.Phase == ShiftPhase.Choosing)
                {
                    if (!direct.Offers.Select(p => p.Kind).SequenceEqual(projected.Offers.Select(p => p.Kind)))
                        throw new Exception("Seeded reward offers differ.");
                    var choice = direct.Offers[0].Kind;
                    direct.Choose(choice); projected.Choose(choice);
                }
                if (direct.Phase is ShiftPhase.Won or ShiftPhase.Bankrupt) break;
            }
            if ((paired && projected.Phase != ShiftPhase.Won) || (!paired && projected.Phase != ShiftPhase.Bankrupt))
                throw new Exception("Projection scenario reached an unexpected ending.");
        }
        return string.Join("\n", trace);
    }

    private static string State(GameSession session, PigHerd herd, int coins) =>
        $"{session.Shift};{session.Phase};{session.Wallet};{session.Stamina};{session.ShiftPigs};{coins};{session.PaidBills};" +
        string.Join(",", herd.Pigs.Select(p => p.State.Health));
}
