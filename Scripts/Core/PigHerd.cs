using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public sealed class PigHerd
{
    private readonly IReadOnlyList<PigMotion> _pigs;
    public IReadOnlyList<PigMotion> Pigs => _pigs;

    public PigHerd(IEnumerable<PigSpawn> spawns)
    {
        ArgumentNullException.ThrowIfNull(spawns);
        _pigs = Array.AsReadOnly(spawns.Select(spawn => new PigMotion(spawn)).ToArray());
    }

    public void Advance(float delta)
    {
        foreach (var pig in _pigs) pig.Advance(delta);
    }

    public PigState[] Targets(Vector2 point, float radius)
    {
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y) || !float.IsFinite(radius) || radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius));
        return _pigs.Where(pig => pig.Active && Vector2.Distance(pig.Position, point) <= radius + pig.State.Definition.HitRadius)
            .Select(pig => pig.State).ToArray();
    }

    public void Apply(SwingResult strike)
    {
        foreach (var hit in strike.Targets)
        {
            var pig = _pigs.FirstOrDefault(p => ReferenceEquals(p.State, hit.Pig));
            pig?.Receive(hit.Result);
        }
    }
}
