using System;
using System.Linq;
using System.Numerics;

internal static class GameplayControlChecks
{
    private sealed class EscapingPath : IMotionPath
    {
        public Vector2 Sample(float elapsed) => elapsed < 1.12f ? Vector2.Zero : new(6, 0);
    }
    private static IMotionPath Stationary(Vector2 at) => new HarmonicPath(at, Vector2.Zero, Vector2.Zero, Vector2.Zero);
    private static WorkshopRun Run(SwingTiming timing, params IMotionPath[] routes)
    {
        var roster = new PigRoster(new[] { new PigArchetype(PigContent.Get("ceramic"), new(), routes) });
        return new WorkshopRun(new GameContent(DefaultGame.Definition(), roster, timing));
    }
    public static void Run(Action<bool, string> check)
    {
        var run = Run(BalanceContent.Timing, Stationary(Vector2.Zero));
        run.BeginShift(); int capacity = run.Session.Capacity;
        for (int i = 0; i < 18; i++) run.Advance(.05f, Vector2.Zero);
        check(run.Session.Stamina == capacity && !run.Hammer.Pending, "opening grace prevents immediate automatic strikes even when a pig is under the pointer");
        for (int i = 0; i < 10; i++) run.Advance(.05f, Vector2.Zero);
        check(run.Session.Stamina == capacity - 1 && run.AutomaticAim.HasLanded, "after preparation mouse aim alone starts automatic striking without a button");

        run = Run(BalanceContent.Timing, Stationary(Vector2.Zero)); run.BeginShift();
        for (int i = 0; i < 100; i++) run.Advance(.05f, new Vector2(6, 0));
        check(run.Session.Stamina == capacity && !run.Hammer.Pending, "opening over an empty part of the table never spends the first stamina");
        run.EndShift(); run.BeginShift();
        check(run.AutomaticAim.OpeningRemaining == BalanceContent.Timing.OpeningDelay && !run.AutomaticAim.HasLanded,
            "each new day receives fresh opening protection");

        run = Run(BalanceContent.Timing, new EscapingPath()); run.BeginShift();
        for (int i = 0; i < 55; i++) run.Advance(.025f, Vector2.Zero);
        check(run.Session.Stamina == capacity && !run.AutomaticAim.HasLanded,
            "a pig escaping during the opening windup cannot turn the first strike into a charged miss");
        for (int i = 0; i < 20; i++) run.Advance(.025f, new Vector2(6, 0));
        check(run.Session.Stamina == capacity - 1 && run.Session.ShiftPigs == 1, "opening protection resumes normally after a valid target is reacquired");

        var responsive = new SwingTiming(Windup: .2f, AimLockSeconds: .06f);
        run = Run(responsive, Stationary(Vector2.Zero), Stationary(new(3, 0))); run.BeginShift();
        run.Advance(.03f, Vector2.Zero);
        check(run.Hammer.Pending && !run.IsAimLocked, "early windup remains steerable");
        run.Advance(.03f, new Vector2(3, 0));
        check(run.StrikePoint == new Vector2(3, 0), "changing targets during early windup moves the logical impact point");
        for (int i = 0; i < 9 && !run.IsAimLocked; i++) run.Advance(.01f, new Vector2(3, 0));
        StrikeOutcome? impact = null;
        for (int i = 0; i < 10 && impact == null; i++) impact = run.Advance(.01f, Vector2.Zero);
        check(impact != null && impact.Point == new Vector2(3, 0) && run.Herd.Pigs[0].Active && !run.Herd.Pigs[1].Active,
            "only the final drop locks aim; changing the pointer cannot move a committed strike");

        run = Run(responsive, Stationary(Vector2.Zero)); run.BeginShift();
        run.Advance(.03f, Vector2.Zero); run.Advance(.03f, null);
        check(!run.Hammer.Pending && run.Session.Stamina == capacity, "leaving the table during early windup cancels cleanly without spending stamina");

        run = Run(responsive with { RetargetSettleSeconds = .10f }, Stationary(Vector2.Zero)); run.BeginShift();
        run.Advance(.05f, Vector2.Zero); run.Advance(.05f, Vector2.Zero);
        while (run.Hammer.Pending) run.Advance(.01f, Vector2.Zero);
        int beforeTravel = run.Session.Stamina;
        Vector2 cursor = Vector2.Zero;
        for (int i = 0; i < 20; i++)
        {
            cursor = i % 2 == 0 ? new Vector2(4, 0) : new Vector2(-4, 0);
            run.Advance(.05f, cursor);
        }
        check(run.Session.Stamina == beforeTravel && !run.Hammer.Pending,
            "rapid retarget travel does not start extra automatic swings after a kill");
        run.Advance(.05f, cursor); run.Advance(.05f, cursor);
        check(run.Hammer.Pending, "automatic striking resumes after the brief retarget settling interval");
        StrikeOutcome? miss = null;
        for (int i = 0; i < 30 && miss == null; i++) miss = run.Advance(.01f, cursor);
        check(miss != null && miss.Strike.HitCount == 0 && run.Session.Stamina == beforeTravel - 1,
            "opening protection does not remove the stamina cost of ordinary later misses");
        run.Restart();
        check(!run.AutomaticAim.HasLanded && !run.Hammer.Pending && run.AutomaticAim.OpeningRemaining == responsive.OpeningDelay,
            "restart resets automatic aiming state");
    }
}
