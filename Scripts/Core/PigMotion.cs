using System;
using System.Numerics;

/// <summary>
/// 保存单只猪的路线进度、实际位置、朝向和重生倒计时。避让位移逐渐回归路线，避免位置跳回。
/// </summary>
public sealed class PigMotion
{
    public PigState State { get; }
    public Vector2 Position { get; private set; }
    public float Time { get; private set; }
    public float BodyRadius { get; }
    public float BodyHalfLength { get; }
    public Vector2 Forward { get; private set; } = Vector2.UnitX;
    internal Vector2 PreferredStep { get; private set; }

    private Vector2 _pathPosition;

    public bool Active => State.IsAlive && _respawnIn <= 0;

    private readonly IMotionPath _path;
    private float _respawnIn;

    public PigMotion(PigSpawn spawn)
    {
        ArgumentNullException.ThrowIfNull(spawn);
        ArgumentNullException.ThrowIfNull(spawn.Path);
        if (!float.IsFinite(spawn.BodyRadius) || spawn.BodyRadius < 0 || !float.IsFinite(spawn.BodyHalfLength) || spawn.BodyHalfLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spawn), "Body dimensions must be finite and nonnegative.");
        }

        BodyRadius = spawn.BodyRadius;
        BodyHalfLength = spawn.BodyHalfLength;
        _path = spawn.Path;
        State = new PigState(spawn.Definition);
        Position = _pathPosition = _path.Sample(0);
        if (!float.IsFinite(Position.X) || !float.IsFinite(Position.Y))
        {
            throw new ArgumentException("Motion paths must return a finite starting position.", nameof(spawn));
        }
    }

    /// <summary>按秒推进路线和重生倒计时；实际位置保留上次避让修正，再平滑回归目标路线。</summary>
    public void Advance(float delta)
    {
        if (!float.IsFinite(delta) || delta < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta));
        }

        float nextTime = Time + delta * State.Definition.Speed;
        if (!float.IsFinite(nextTime))
        {
            throw new ArgumentOutOfRangeException(nameof(delta));
        }

        var nextPosition = _path.Sample(nextTime);
        if (!float.IsFinite(nextPosition.X) || !float.IsFinite(nextPosition.Y))
        {
            throw new InvalidOperationException("Motion paths must return finite positions.");
        }
        // 保留上一帧的避让位移，再逐渐回归路线，避免路线交叉时直接跳回并穿过其他猪。
        var proposed = BodyRadius > 0 && Active
            ? Vector2.Lerp(Position + nextPosition - _pathPosition, nextPosition, 1 - MathF.Exp(-2 * delta))
            : nextPosition;
        if (!float.IsFinite(proposed.X) || !float.IsFinite(proposed.Y))
        {
            throw new InvalidOperationException("Movement must remain finite.");
        }

        PreferredStep = proposed - Position;
        if (PreferredStep.LengthSquared() > .000001f && delta > 0)
        {
            float heading = MathF.Atan2(Forward.Y, Forward.X);
            float turn = MathF.IEEERemainder(MathF.Atan2(PreferredStep.Y, PreferredStep.X) - heading, MathF.Tau);
            heading += Math.Clamp(turn, -6 * delta, 6 * delta);
            Forward = new Vector2(MathF.Cos(heading), MathF.Sin(heading));
        }
        _pathPosition = nextPosition;
        Time = nextTime;
        if (_respawnIn > 0)
        {
            _respawnIn -= delta;
            if (_respawnIn <= 0)
            {
                State.Respawn();
            }
        }
        Position = proposed;
    }

    internal void ConstrainPosition(Vector2 position) => Position = position;

    internal void Receive(DamageResult hit)
    {
        if (hit.Destroyed && !State.IsAlive && _respawnIn <= 0)
        {
            _respawnIn = State.Definition.RespawnSeconds;
        }
    }
}
