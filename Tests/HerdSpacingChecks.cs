using System;
using System.Linq;
using System.Numerics;

internal static class HerdSpacingChecks
{
    private sealed class Line(Vector2 start, Vector2 velocity) : IMotionPath
    {
        public Vector2 Sample(float elapsed) => start + velocity * elapsed;
    }
    private static PigSpawn Spawn(Vector2 start, Vector2 velocity, float radius = .8f) =>
        new(PigContent.Get("ceramic"), new Line(start, velocity), radius);

    // Independent endpoint/intersection check rather than calling the solver's closest-point routine.
    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
    private static float PointDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        var line = b - a;
        float t = line.LengthSquared() > .000001f ? Math.Clamp(Vector2.Dot(p - a, line) / line.LengthSquared(), 0, 1) : 0;
        return Vector2.Distance(p, a + t * line);
    }
    private static float BodyDistance(PigMotion a, PigMotion b)
    {
        var a0 = a.Position - a.Forward * a.BodyHalfLength; var a1 = a.Position + a.Forward * a.BodyHalfLength;
        var b0 = b.Position - b.Forward * b.BodyHalfLength; var b1 = b.Position + b.Forward * b.BodyHalfLength;
        float denominator = Cross(a1 - a0, b1 - b0);
        if (MathF.Abs(denominator) > .000001f)
        {
            float t = Cross(b0 - a0, b1 - b0) / denominator, u = Cross(b0 - a0, a1 - a0) / denominator;
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1) return 0;
        }
        return MathF.Min(MathF.Min(PointDistance(a0, b0, b1), PointDistance(a1, b0, b1)),
            MathF.Min(PointDistance(b0, a0, a1), PointDistance(b1, a0, a1)));
    }

    private static void RequireClear(PigHerd herd)
    {
        var pigs = herd.Pigs.Where(p => p.Active && p.BodyRadius > 0).ToArray();
        for (int i = 0; i < pigs.Length; i++)
        {
            var a = pigs[i];
            var extent = Vector2.Abs(a.Forward) * a.BodyHalfLength + new Vector2(a.BodyRadius);
            if (!float.IsFinite(a.Position.X) || !float.IsFinite(a.Position.Y) ||
                MathF.Abs(a.Position.X) + extent.X > 7.401f || MathF.Abs(a.Position.Y) + extent.Y > 3.851f)
                throw new Exception("A pig's body left the table.");
            for (int j = i + 1; j < pigs.Length; j++)
                if (BodyDistance(a, pigs[j]) + .001f < a.BodyRadius + pigs[j].BodyRadius)
                    throw new Exception($"Pig bodies overlap: {i}/{j} at {a.Position}/{pigs[j].Position}.");
        }
    }

    public static void Run(Action<bool, string> check)
    {
        foreach (int day in new[] { 1, 6 })
        foreach (float dt in new[] { 1f / 30, 1f / 60, 1f / 144, .2f })
        {
            var herd = DefaultGame.Content.CreateHerd(day);
            RequireClear(herd);
            for (int frame = 0; frame < (int)(60 / dt); frame++)
            {
                if (frame % Math.Max(1, (int)(3 / dt)) == 0)
                {
                    var pig = herd.Pigs.Last();
                    if (pig.Active) herd.Apply(StrikeResolver.Resolve(new[] { pig.State }, pig.State.Health));
                }
                herd.Advance(dt);
                try { RequireClear(herd); } catch (Exception e) { throw new Exception($"day={day}, dt={dt}, frame={frame}: " + e.Message); }
            }
            check(true, $"day {day} bodies remain separate and bounded at dt={dt}, including repeated respawns");
        }
        var sideBySide = new PigHerd(new[] {
            new PigSpawn(PigContent.Get("ceramic"), new Line(Vector2.Zero, Vector2.Zero), .6f, .5f),
            new PigSpawn(PigContent.Get("ceramic"), new Line(new(0, 1.21f), Vector2.Zero), .6f, .5f) });
        RequireClear(sideBySide);
        check(Vector2.Distance(sideBySide.Pigs[0].Position, sideBySide.Pigs[1].Position) < 1.22f,
            "capsules allow close parallel bodies instead of using their full length as a circular radius");
        var endToEnd = new PigHerd(new[] {
            new PigSpawn(PigContent.Get("ceramic"), new Line(Vector2.Zero, Vector2.Zero), .6f, .5f),
            new PigSpawn(PigContent.Get("ceramic"), new Line(new(1.21f, 0), Vector2.Zero), .6f, .5f) });
        RequireClear(endToEnd);
        check(Vector2.Distance(endToEnd.Pigs[0].Position, endToEnd.Pigs[1].Position) >= 2.199f,
            "nose-to-tail bodies include the full capsule length in separation");
        var crowded = new PigHerd(Enumerable.Range(0, 7).Select(_ => Spawn(Vector2.Zero, Vector2.Zero)));
        RequireClear(crowded); crowded.Advance(1); RequireClear(crowded);
        check(true, "coincident spawn points resolve without NaNs or overlapping bodies");
        var corner = new PigHerd(Enumerable.Range(0, 3).Select(_ => Spawn(new(7.3f, 3.7f), Vector2.Zero, 1.2f)));
        RequireClear(corner); corner.Advance(.5f); RequireClear(corner);
        check(true, "crowded table edges keep the full bodies inside the playable area");
        var crossing = new PigHerd(new[] { Spawn(new(-3, 0), new(3, 0)), Spawn(new(3, 0), new(-3, 0)) });
        for (int i = 0; i < 60; i++) { crossing.Advance(.1f); RequireClear(crossing); }
        check(crossing.Pigs[0].Position.X > crossing.Pigs[1].Position.X, "head-on routes pass on opposite sides instead of remaining stuck");
        var dead = new PigHerd(new[] { Spawn(Vector2.Zero, Vector2.Zero), Spawn(Vector2.Zero, Vector2.Zero) });
        var victim = dead.Pigs[1]; dead.Apply(StrikeResolver.Resolve(new[] { victim.State }, 1));
        dead.Advance(.8f);
        check(!victim.Active && dead.Pigs[0].Position.Length() < .2f, "a destroyed pig releases collision space before respawning");
        dead.Advance(.1f); RequireClear(dead);
        check(victim.Active && dead.Targets(victim.Position, 0).Contains(victim.State), "respawn is separated before its rendered and attackable position is published");
        var grouped = new PigHerd(new[] { Spawn(Vector2.Zero, Vector2.Zero), Spawn(Vector2.Zero, Vector2.Zero) });
        Vector2 midpoint = (grouped.Pigs[0].Position + grouped.Pigs[1].Position) / 2;
        check(grouped.Targets(midpoint, BalanceContent.Tuning.BaseRadius).Length == 2, "separation still allows intentional multi-pig hits");
        check(PigContent.Roster.All.Single(p => p.Definition.Id == "armored").BodyRadius >
            PigContent.Roster.All.Single(p => p.Definition.Id == "ceramic").BodyRadius, "armored body size has a larger footprint without altering attack radius");
    }
}
