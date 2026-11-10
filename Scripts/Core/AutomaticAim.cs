using System;
using System.Numerics;

/// <summary>
/// 将鼠标意图转换为自动攻击许可：开局准备、首次目标确认，以及快速换目标后的短暂稳定时间。
/// </summary>
public sealed class AutomaticAim
{
    private readonly SwingTiming _timing;
    private Vector2? _previous;
    private float _settled;

    public float OpeningRemaining { get; private set; }
    public bool HasLanded { get; private set; }

    public AutomaticAim(SwingTiming timing)
    {
        _timing = timing;
        Reset();
    }

    public void Reset()
    {
        OpeningRemaining = _timing.OpeningDelay;
        HasLanded = false;
        _previous = null;
        _settled = 0;
    }

    public bool Advance(float delta, Vector2? target, bool overPig)
    {
        OpeningRemaining = MathF.Max(0, OpeningRemaining - delta);
        if (!target.HasValue)
        {
            _previous = null;
            _settled = 0;
            return false;
        }
        bool movingQuickly = _previous.HasValue &&
            Vector2.Distance(_previous.Value, target.Value) > _timing.RetargetSpeed * MathF.Max(delta, .00001f);
        _settled = movingQuickly ? 0 : _settled + delta;
        _previous = target;
        return OpeningRemaining <= 0 && _settled + .00001f >= _timing.RetargetSettleSeconds && (HasLanded || overPig);
    }

    public void RecordLanding() => HasLanded = true;
}
