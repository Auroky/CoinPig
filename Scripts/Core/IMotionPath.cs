using System;
using System.Numerics;

/// <summary>
/// 按非负有限时间采样桌面坐标。实现须确定、返回有限坐标，且不能修改游戏状态。
/// </summary>
public interface IMotionPath
{
    Vector2 Sample(float elapsed);
}

/// <summary>
/// 两个轴独立的正弦路线。振幅和中心使用世界单位，频率为每秒弧度，初相位为弧度。
/// </summary>
public sealed class HarmonicPath : IMotionPath
{
    private readonly Vector2 _center;
    private readonly Vector2 _amplitude;
    private readonly Vector2 _frequency;
    private readonly Vector2 _phase;

    public HarmonicPath(Vector2 center, Vector2 amplitude, Vector2 frequency, Vector2 phase)
    {
        if (!Finite(center) || !Finite(amplitude) || !Finite(frequency) || !Finite(phase) ||
            frequency.X < 0 || frequency.Y < 0 ||
            (double)MathF.Abs(center.X) + MathF.Abs(amplitude.X) > float.MaxValue ||
            (double)MathF.Abs(center.Y) + MathF.Abs(amplitude.Y) > float.MaxValue)
        {
            throw new ArgumentException("Path parameters must produce finite coordinates with nonnegative frequencies.");
        }

        _center = center;
        _amplitude = amplitude;
        _frequency = frequency;
        _phase = phase;
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

    public Vector2 Sample(float elapsed)
    {
        if (!float.IsFinite(elapsed) || elapsed < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        return new(
            (float)(_center.X + _amplitude.X * Math.Sin((double)elapsed * _frequency.X + _phase.X)),
            (float)(_center.Y + _amplitude.Y * Math.Sin((double)elapsed * _frequency.Y + _phase.Y)));
    }
}
