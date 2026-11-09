using System;
using System.Numerics;

/// <summary>
/// Samples a world-space position for finite, nonnegative elapsed time.
/// Implementations are deterministic, return finite coordinates and do not change game state.
/// </summary>
public interface IMotionPath
{
    Vector2 Sample(float elapsed);
}

/// <summary>A configurable pair of independent sinusoidal movements.</summary>
public sealed class HarmonicPath : IMotionPath
{
    private readonly Vector2 _center, _amplitude, _frequency, _phase;

    public HarmonicPath(Vector2 center, Vector2 amplitude, Vector2 frequency, Vector2 phase)
    {
        if (!Finite(center) || !Finite(amplitude) || !Finite(frequency) || !Finite(phase) ||
            frequency.X < 0 || frequency.Y < 0 ||
            (double)MathF.Abs(center.X) + MathF.Abs(amplitude.X) > float.MaxValue ||
            (double)MathF.Abs(center.Y) + MathF.Abs(amplitude.Y) > float.MaxValue)
            throw new ArgumentException("Path parameters must produce finite coordinates with nonnegative frequencies.");
        _center = center;
        _amplitude = amplitude;
        _frequency = frequency;
        _phase = phase;
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

    public Vector2 Sample(float elapsed)
    {
        if (!float.IsFinite(elapsed) || elapsed < 0) throw new ArgumentOutOfRangeException(nameof(elapsed));
        return new(
            (float)(_center.X + _amplitude.X * Math.Sin((double)elapsed * _frequency.X + _phase.X)),
            (float)(_center.Y + _amplitude.Y * Math.Sin((double)elapsed * _frequency.Y + _phase.Y)));
    }
}
