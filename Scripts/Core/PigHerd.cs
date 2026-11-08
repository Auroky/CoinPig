using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public enum PigLane { Middle, Back, Front, Side }
public sealed record PigSpawn(PigLane Lane, float Side, string Species);

/// <summary>A pig's world-space movement and respawn lifecycle.</summary>
public sealed class PigMotion
{
    public PigState State { get; }
    public Vector2 Position { get; private set; }
    public float Time { get; private set; }
    public bool Active => State.IsAlive && _respawnIn <= 0;
    private readonly PigSpawn _spawn;
    private float _respawnIn;

    public PigMotion(PigSpawn spawn)
    {
        _spawn = spawn;
        State = new PigState(PigCatalog.Get(spawn.Species));
        Position = PositionAt(0);
    }

    public void Advance(float delta)
    {
        if (delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        Time += delta * State.Definition.Speed;
        if (_respawnIn > 0)
        {
            _respawnIn -= delta;
            if (_respawnIn <= 0) State.Respawn();
        }
        Position = PositionAt(Time);
    }

    public void Receive(DamageResult hit)
    {
        if (hit.Destroyed) _respawnIn = .85f;
    }

    private Vector2 PositionAt(float time)
    {
        float side = _spawn.Side;
        return _spawn.Lane switch
        {
            PigLane.Middle => new(side * 5.72f * MathF.Cos(time * .54f + (side < 0 ? .055f : 0)), side * (.64f + .14f * MathF.Sin(time * 1.08f))),
            PigLane.Back => new(.5f + side * 5.73f * MathF.Sin(time * .42f + .72f), -2.2f + side * .67f * MathF.Cos(time * .42f + .72f)),
            PigLane.Front => new(-.35f + side * 5.82f * MathF.Sin(time * .45f - .62f), 2.57f + side * .63f * MathF.Cos(time * .45f - .62f)),
            _ => new(side * (4.12f + 1.44f * MathF.Cos(time * .69f + .35f)), side * 2.9f * MathF.Sin(time * .69f + .35f))
        };
    }
}

public sealed class PigHerd
{
    private readonly IReadOnlyList<PigMotion> _pigs;
    public IReadOnlyList<PigMotion> Pigs => _pigs;

    public PigHerd(IEnumerable<PigSpawn>? spawns = null)
    {
        spawns ??= new[]
        {
            new PigSpawn(PigLane.Middle, 1, "ceramic"),
            new(PigLane.Middle, -1, "ceramic")
        };
        _pigs = Array.AsReadOnly(spawns.Select(spawn => new PigMotion(spawn)).ToArray());
    }

    public void Advance(float delta)
    {
        foreach (var pig in _pigs) pig.Advance(delta);
    }

    public PigState[] Targets(Vector2 point, float radius) => _pigs
        .Where(pig => pig.Active && Vector2.Distance(pig.Position, point) <= radius + .33f)
        .Select(pig => pig.State).ToArray();

    public void Apply(SwingResult strike)
    {
        foreach (var hit in strike.Targets)
        {
            var pig = _pigs.FirstOrDefault(p => ReferenceEquals(p.State, hit.Pig));
            pig?.Receive(hit.Result);
        }
    }
}
