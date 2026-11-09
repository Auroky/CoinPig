using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

/// <summary>A pig's world-space movement and respawn lifecycle.</summary>
public sealed class PigMotion
{
    public PigState State { get; }
    public Vector2 Position { get; private set; }
    public float Time { get; private set; }
    public bool Active => State.IsAlive && _respawnIn <= 0;
    private readonly IMotionPath _path;
    private float _respawnIn;

    public PigMotion(PigSpawn spawn)
    {
        ArgumentNullException.ThrowIfNull(spawn);
        ArgumentNullException.ThrowIfNull(spawn.Path);
        _path = spawn.Path;
        State = new PigState(spawn.Definition);
        Position = _path.Sample(0);
        if (!float.IsFinite(Position.X) || !float.IsFinite(Position.Y))
            throw new ArgumentException("Motion paths must return a finite starting position.", nameof(spawn));
    }

    public void Advance(float delta)
    {
        if (!float.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        float nextTime = Time + delta * State.Definition.Speed;
        if (!float.IsFinite(nextTime)) throw new ArgumentOutOfRangeException(nameof(delta));
        var nextPosition = _path.Sample(nextTime);
        if (!float.IsFinite(nextPosition.X) || !float.IsFinite(nextPosition.Y))
            throw new InvalidOperationException("Motion paths must return finite positions.");
        Time = nextTime;
        if (_respawnIn > 0)
        {
            _respawnIn -= delta;
            if (_respawnIn <= 0) State.Respawn();
        }
        Position = nextPosition;
    }

    internal void Receive(DamageResult hit)
    {
        if (hit.Destroyed && !State.IsAlive && _respawnIn <= 0) _respawnIn = .85f;
    }
}

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
        return _pigs.Where(pig => pig.Active && Vector2.Distance(pig.Position, point) <= radius + .33f)
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
