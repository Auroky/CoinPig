using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;

record Profile(string Name, float Reaction, float AimError, float TravelSpeed, float PairAwareness, float MissChance, bool Upgrades = true);
record Result(int LastDay, bool Won, int Cash, int Swings, int Misses, int Pigs, int[] Earnings, int[] Wallets);

static class Program
{
    static readonly Profile[] Profiles = {
        new("learning", .38f, .72f, 9, .15f, .14f),
        new("regular", .23f, .40f, 13, .5f, .07f),
        new("practiced", .12f, .18f, 20, .90f, .02f),
        new("no-upgrades", .23f, .40f, 13, .5f, .07f, false),
        new("careless", .5f, 1.1f, 7, .05f, .30f)
    };

    static PigRoster Roster(int population)
    {
        if (population == 4) return PigContent.Roster;
        var normal = PigContent.Get("ceramic");
        var routes = new List<IMotionPath> { HerdLayout.Middle(1), HerdLayout.Middle(-1) };
        if (population >= 6) { routes.Add(HerdLayout.Back(-1)); routes.Add(HerdLayout.Front(1)); }
        if (population >= 8) { routes.Add(HerdLayout.Side(1)); routes.Add(HerdLayout.Side(-1)); }
        if (population >= 10) { routes.Add(HerdLayout.Back(1)); routes.Add(HerdLayout.Front(-1)); }
        return new PigRoster(new[] {
            new PigArchetype(normal, new(), routes.ToArray()),
            new PigArchetype(PigContent.Get("tin"), PigContent.Roster.GetAppearance("tin"), HerdLayout.Back(1)),
            new PigArchetype(PigContent.Get("swift"), PigContent.Roster.GetAppearance("swift"), HerdLayout.Front(-1))
        });
    }

    static Vector2 Noise(Random random, float magnitude)
    {
        float angle = (float)random.NextDouble() * MathF.Tau;
        float distance = MathF.Sqrt((float)random.NextDouble()) * magnitude;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
    }

    static (Vector2 Point, PigMotion[] Targets) Pick(WorkshopRun run, Profile profile, Random random)
    {
        var visible = run.Herd.Pigs.Where(p => p.Active).ToArray();
        if (visible.Length == 0) return (Vector2.Zero, Array.Empty<PigMotion>());
        bool noticePairs = random.NextDouble() < profile.PairAwareness;
        var candidates = visible.Select(p => p.Position).ToList();
        if (noticePairs)
            for (int a = 0; a < visible.Length; a++)
                for (int b = a + 1; b < visible.Length; b++)
                    if (Vector2.Distance(visible[a].Position, visible[b].Position) < run.Session.Radius * 1.7f)
                        candidates.Add((visible[a].Position + visible[b].Position) / 2);
        Vector2 best = visible[0].Position; float score = float.MinValue;
        foreach (var point in candidates)
        {
            var hits = visible.Where(p => Vector2.Distance(p.Position, point) < run.Session.Radius + .1f);
            float value = hits.Sum(p => p.State.Definition.Coins / MathF.Ceiling(p.State.Health / (float)run.Session.Damage));
            value *= .7f + (float)random.NextDouble() * .6f; // Imperfect value judgement; not an omniscient optimizer.
            if (value > score) { score = value; best = point; }
        }
        var targets = visible.Where(p => Vector2.Distance(p.Position, best) <= run.Session.Radius + .1f).ToArray();
        Vector2 offset = Noise(random, profile.AimError);
        if (random.NextDouble() < profile.MissChance) offset += Noise(random, 3) + new Vector2(1.8f, 0);
        return (best + offset, targets);
    }

    static void Shop(GameSession session, Profile profile)
    {
        if (!profile.Upgrades) return;
        int daysLeft = Math.Max(1, (session.NextBill?.Shift ?? 11) - session.Shift);
        // Keep part of an upcoming payment; an ordinary player still invests before having the whole bill saved.
        int reserve = (int)Math.Ceiling((session.NextBill?.Amount ?? 0) * (daysLeft == 1 ? .55f : .30f));
        foreach (string id in new[] { "wide", "endurance", "pairs", "power1", "coins", "breath", "quick", "rebound", "reserve", "power2", "jackpot" })
        {
            var skill = session.GetSkills().First(s => s.Skill.Id == id);
            if (skill.Status == SkillStatus.Available && session.Wallet - skill.Skill.Cost >= reserve)
                session.TryLearn(id);
        }
        foreach (var offer in session.GetOffers().OrderBy(o => o.Price))
            if (offer.Status == PurchaseStatus.Available && session.Wallet - offer.Price >= reserve)
                session.TryBuy(offer.Product.Id, offer.Level);
    }

    static Result Play(GameContent content, Profile profile, int seed)
    {
        var random = new Random(seed * 43 + 7);
        var run = new WorkshopRun(new SeededFactory(content, seed));
        int swings = 0, misses = 0, pigs = 0;
        var earnings = new List<int>(); var wallets = new List<int>();
        while (run.Session.Shift <= 10)
        {
            run.BeginShift();
            Vector2 cursor = new(0, 3.5f), destination = cursor;
            PigMotion[] focus = Array.Empty<PigMotion>();
            Vector2 aimOffset = Vector2.Zero;
            float errorAge = 0;
            float decisionIn = .6f + (float)random.NextDouble() * .5f;
            int frames = 0;
            while (run.Session.Phase == ShiftPhase.Working && frames++ < 12000)
            {
                decisionIn -= .05f;
                errorAge -= .05f;
                if (decisionIn <= 0)
                {
                    var remaining = focus.Where(p => p.Active).ToArray();
                    if (remaining.Length == 0)
                    {
                        (destination, focus) = Pick(run, profile, random);
                        if (focus.Length > 0)
                            aimOffset = destination - focus.Aggregate(Vector2.Zero, (sum, pig) => sum + pig.Position) / focus.Length;
                        errorAge = 1 + (float)random.NextDouble() * .6f;
                    }
                    else
                    {
                        var anchor = remaining.OrderBy(p => p.State.Health).ThenBy(p => Vector2.Distance(p.Position, cursor)).First();
                        focus = remaining.Where(p => Vector2.Distance(p.Position, anchor.Position) < run.Session.Radius * 1.6f).ToArray();
                        if (errorAge <= 0)
                        {
                            aimOffset = Noise(random, profile.AimError);
                            if (random.NextDouble() < profile.MissChance) aimOffset += Noise(random, 3) + new Vector2(1.8f, 0);
                            errorAge = 1 + (float)random.NextDouble() * .6f;
                        }
                        destination = focus.Aggregate(Vector2.Zero, (sum, pig) => sum + pig.Position) / focus.Length + aimOffset;
                    }
                    decisionIn = profile.Reaction * (.8f + (float)random.NextDouble() * .4f);
                    destination = Vector2.Clamp(destination, new(-7.2f, -3.6f), new(7.2f, 3.6f));
                }
                var travel = destination - cursor;
                float step = profile.TravelSpeed * .05f;
                cursor += travel.Length() <= step ? travel : Vector2.Normalize(travel) * step;
                Vector2? aim = frames > 2 ? cursor : null; // Same mouse-only automatic play in both versions.
                var outcome = run.Advance(.05f, aim);
                if (outcome == null) continue;
                swings++; misses += outcome.Strike.HitCount == 0 ? 1 : 0; pigs += outcome.Strike.DestroyedCount;
                // A kill is noticed only at the next reaction interval, which may cause an extra swing.
            }
            if (frames >= 12000) throw new Exception("Simulation failed to finish a shift.");
            earnings.Add(run.Session.ShiftEarnings); wallets.Add(run.Session.Wallet);
            if (run.Session.Phase is ShiftPhase.Won or ShiftPhase.Bankrupt) break;
            if (run.Session.Phase == ShiftPhase.Choosing)
            {
                // Plausible choice with some variation, rather than always selecting the mathematically best perk.
                var choices = run.Session.Offers.OrderBy(_ => random.Next()).ToArray();
                run.Session.Choose(choices[0].Id);
            }
            Shop(run.Session, profile);
        }
        return new(run.Session.Shift, run.Session.Phase == ShiftPhase.Won, run.Session.Wallet, swings, misses, pigs, earnings.ToArray(), wallets.ToArray());
    }
    sealed class SeededFactory : IGameFactory
    {
        readonly GameContent _content; readonly int _seed;
        public SeededFactory(GameContent content, int seed) { _content = content; _seed = seed; }
        public SwingTiming Timing => _content.Timing;
        public GameSession CreateSession() => new(_content.Definition, new Random(_seed));
        public PigHerd CreateHerd() => _content.CreateHerd();
    }

    static void Main(string[] args)
    {
        int seeds = args.Contains("--quick") ? 40 : args.Contains("--thorough") ? 500 : 200;
        var all = new List<object>();
        var rates = new Dictionary<string, (double Wins, double ReachDay4, double Misses)>();
        var populations = args.Contains("--sweep") ? new[] { 4, 6, 8, 10 } : new[] { PigContent.Roster.Spawns.Count };
        var capacities = args.Contains("--sweep") ? new[] { 12, 14, 16, 18 } : new[] { BalanceContent.Tuning.StartingStamina };
        foreach (int population in populations)
        foreach (int stamina in capacities)
        {
            var content = new GameContent(DefaultGame.Definition(tuning: BalanceContent.Tuning with { StartingStamina = stamina }), Roster(population), BalanceContent.Timing);
            foreach (var profile in Profiles)
            {
                var results = Enumerable.Range(1, seeds).Select(seed => Play(content, profile, seed)).ToArray();
                var summary = new {
                    population, stamina, profile = profile.Name, runs = seeds,
                    winRate = Math.Round(results.Count(r => r.Won) * 100.0 / seeds, 1),
                    reachDay4 = Math.Round(results.Count(r => r.LastDay >= 4) * 100.0 / seeds, 1),
                    medianLastDay = results.OrderBy(r => r.LastDay).ElementAt(seeds / 2).LastDay,
                    day1Income = Math.Round(results.Average(r => r.Earnings[0]), 1),
                    incomePerSwing = Math.Round(results.Sum(r => r.Earnings.Sum()) / (double)results.Sum(r => r.Swings), 2),
                    missRate = Math.Round(results.Sum(r => r.Misses) * 100.0 / results.Sum(r => r.Swings), 1),
                    medianEndingCash = results.OrderBy(r => r.Cash).ElementAt(seeds / 2).Cash
                };
                all.Add(summary); Console.WriteLine(JsonSerializer.Serialize(summary));
                if (population == PigContent.Roster.Spawns.Count && stamina == BalanceContent.Tuning.StartingStamina)
                    rates[profile.Name] = (summary.winRate, summary.reachDay4, summary.missRate);
            }
        }
        if (args.Contains("--save"))
            System.IO.File.WriteAllText(args.Last(), JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true }));
        if (args.Contains("--verify"))
        {
            if (seeds < 200) throw new Exception("Balance verification requires at least 200 seeds per profile.");
            void Check(bool pass, string label) { if (!pass) throw new Exception("Balance regression: " + label); }
            Check(rates["regular"].Wins >= 65 && rates["regular"].Wins <= 95, "regular play should be achievable without becoming automatic victory");
            Check(rates["practiced"].Wins >= 90, "practiced play should reliably complete the contract");
            Check(rates["learning"].Wins >= 10 && rates["learning"].Wins <= 60, "learning play should have a chance and room to improve");
            Check(rates["learning"].ReachDay4 >= 95 && rates["regular"].ReachDay4 >= 98, "early shifts should tolerate ordinary reaction errors");
            Check(rates["careless"].Wins <= 15, "frequent major mistakes should still have consequences");
            Check(rates["no-upgrades"].Wins < rates["regular"].Wins && rates["no-upgrades"].Wins < 80, "upgrades should remain useful");
            Check(rates["regular"].Misses <= 10 && rates["practiced"].Misses <= 5, "control-related empty strikes should be uncommon");
            Console.WriteLine($"BALANCE PASS: {seeds * Profiles.Length} seeded automatic-play runs; 7 balance criteria.");
        }
    }
}
