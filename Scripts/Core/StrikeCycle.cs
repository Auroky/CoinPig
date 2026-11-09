using System;

public sealed class StrikeCycle
{
    private readonly SwingTiming _timing;
    public bool Pending { get; private set; }
    public float Windup { get; private set; }
    public float Cooldown { get; private set; }
    public StrikeCycle(SwingTiming timing)
    {
        ArgumentNullException.ThrowIfNull(timing);
        if (!float.IsFinite(timing.Windup) || timing.Windup <= 0 ||
            !float.IsFinite(timing.Recovery) || timing.Recovery <= 0 ||
            !float.IsFinite(timing.MinimumRecovery) || timing.MinimumRecovery <= 0 ||
            timing.MinimumRecovery > timing.Recovery || !float.IsFinite(timing.SettlementDelay) || timing.SettlementDelay <= 0)
            throw new ArgumentException("Swing timing must be finite and positive.", nameof(timing));
        _timing = timing;
    }
    public bool Begin()
    {
        if (Pending || Cooldown > 0) return false;
        Pending = true; Windup = _timing.Windup;
        return true;
    }
    public bool Advance(float delta, float recoveryReduction)
    {
        if (!float.IsFinite(delta) || delta < 0 || !float.IsFinite(recoveryReduction) || recoveryReduction < 0)
            throw new ArgumentOutOfRangeException(nameof(delta));
        Cooldown = MathF.Max(0, Cooldown - delta);
        if (!Pending) return false;
        Windup -= delta;
        if (Windup > 0) return false;
        Pending = false; Windup = 0;
        Cooldown = MathF.Max(_timing.MinimumRecovery, _timing.Recovery - recoveryReduction);
        return true;
    }
    public void Reset() { Pending = false; Windup = Cooldown = 0; }
}
