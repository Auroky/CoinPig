using System;
using System.Linq;
using System.Numerics;

internal static class PigVariantChecks
{
    private sealed class StraightPath : IMotionPath
    {
        public Vector2 Sample(float elapsed) => new(elapsed, 0);
    }

    public static void Run(Action<bool, string> check)
    {
        foreach (var (id, hits) in new[] { ("ceramic", 1), ("tin", 4), ("swift", 3), ("armored", 10) })
        {
            var herd = new PigHerd(new[] { new PigSpawn(PigContent.Get(id), new StraightPath()) });
            var pig = herd.Pigs.Single();
            var session = DefaultGame.CreateSession();
            session.BeginShift();
            for (int hit = 1; hit <= hits; hit++)
            {
                check(herd.Targets(pig.Position, 0).Contains(pig.State), $"{id} remains targetable before hit {hit}");
                session.TrySwing(new[] { pig.State }, out var strike, out int coins);
                herd.Apply(strike!);
                bool destroyed = hit == hits;
                check(pig.State.Health == hits - hit && pig.Active == !destroyed,
                    $"{id} survives until exactly {hits} base-damage hits");
                check(coins == (destroyed ? pig.State.Definition.Coins : 0) && session.ShiftPigs == (destroyed ? 1 : 0),
                    $"{id} grants coins and a kill only on destruction");
                if (!destroyed)
                {
                    var at = pig.Position;
                    herd.Advance(.2f);
                    check(pig.Position != at && pig.State.Health == hits - hit, $"{id} keeps moving without healing after nonlethal damage");
                }
            }
            check(!herd.Targets(pig.Position, 0).Contains(pig.State), $"{id} leaves the target set when destroyed");
            herd.Advance(.5f);
            check(!pig.Active, $"{id} waits before respawning");
            herd.Advance(.36f);
            check(pig.Active && pig.State.Health == hits, $"{id} respawns with full durability");
        }

        var skillBonus = SkillContent.Nodes.Aggregate(new Bonus(), (total, skill) => total.Add(skill.Bonus));
        var cuff = EquipmentContent.Products.Single(product => product.Id == "Stamina").TierAt(2);
        foreach (var product in EquipmentContent.Products.Where(product => product.Weapon != null))
        {
            for (int level = 1; level <= product.MaxLevel; level++)
            {
                var stats = product.Weapon!.Attack.Stats(BalanceContent.Tuning, product.BonusThrough(level).Add(skillBonus));
                var pig = new PigState(PigContent.Get("armored"));
                int hits = 0;
                while (pig.IsAlive && hits < 20) { StrikeResolver.Resolve(new[] { pig }, stats.Damage); hits++; }
                check(hits >= 2 && hits <= 4, $"{product.Id} tier {level} with all damage skills needs two to four attacks on fresh armor");

                var herd = new PigHerd(new[] { new PigSpawn(PigContent.Get("armored"), new StraightPath()) });
                var target = herd.Pigs.Single();
                var strike = StrikeResolver.Resolve(new[] { target.State }, stats.Damage);
                herd.Apply(strike);
                var context = new EffectContext(0, herd, Vector2.Zero, new StrikeOutcome(Vector2.Zero, strike, 0));
                foreach (var effect in cuff.Effects!)
                    foreach (var pulse in effect.Create().Advance(context))
                        herd.Apply(StrikeResolver.Resolve(pulse.Targets, pulse.Damage));
                check(target.Active, $"{product.Id} tier {level} plus the strongest opening impact resonance cannot instantly destroy fresh armor");
            }
        }

        var motions = PigContent.Roster.All.Select(p => p.Definition).Select(definition => new PigMotion(new(definition, new StraightPath()))).ToArray();
        foreach (var motion in motions) motion.Advance(1);
        var ordinary = motions.Single(p => p.State.Definition.Id == "ceramic");
        var heavy = motions.Single(p => p.State.Definition.Id == "tin");
        var swift = motions.Single(p => p.State.Definition.Id == "swift");
        check(heavy.Position.X < ordinary.Position.X && swift.Position.X > ordinary.Position.X,
            "heavy pig moves slower and swift pig moves faster on the same route");
        check(MathF.Abs(swift.Position.X / ordinary.Position.X - 1.5f) < .0001f,
            "swift pig travels at 1.5 times ordinary speed");

        var armored = new PigState(PigContent.Get("tin"));
        var runner = new PigState(PigContent.Get("swift"));
        var upgraded = StrikeResolver.Resolve(new[] { armored, runner }, 3);
        check(armored.Health == 1 && armored.IsAlive && !runner.IsAlive && upgraded.DestroyedCount == 1,
            "new pig variants respect the existing hammer damage upgrades");
    }
}
