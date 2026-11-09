using System;
using System.Numerics;

public sealed record StrikeOutcome(Vector2 Point, SwingResult Strike, int Coins);

/// <summary>Engine-independent application flow; input and presentation are adapters.</summary>
public sealed class WorkshopRun
{
    private readonly IGameFactory _factory;
    private readonly SwingTiming _timing;
    private float _settleDelay;
    private Vector2 _strikePoint;
    public GameSession Session { get; private set; }
    public PigHerd Herd { get; private set; }
    public StrikeCycle Hammer { get; }

    public WorkshopRun(IGameFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
        _timing = factory.Timing;
        Hammer = new StrikeCycle(_timing);
        Session = factory.CreateSession(); Herd = factory.CreateHerd();
    }

    public bool BeginShift()
    {
        if (!Session.BeginShift()) return false;
        Hammer.Reset(); _settleDelay = 0; Herd = _factory.CreateHerd();
        return true;
    }
    public void Restart()
    {
        Session = _factory.CreateSession(); Herd = _factory.CreateHerd();
        Hammer.Reset(); _settleDelay = 0;
    }
    public bool EndShift()
    {
        if (Session.Phase != ShiftPhase.Working || Hammer.Pending) return false;
        Hammer.Reset(); _settleDelay = 0; Session.CloseShift();
        return true;
    }
    public StrikeOutcome? Advance(float delta, Vector2? target)
    {
        if (!float.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        if (target is Vector2 point && (!float.IsFinite(point.X) || !float.IsFinite(point.Y)))
            throw new ArgumentOutOfRangeException(nameof(target));
        if (Session.Phase != ShiftPhase.Working) return null;
        Herd.Advance(delta);
        if (Session.Stamina > 0 && target.HasValue && Hammer.Begin()) _strikePoint = target.Value;
        StrikeOutcome? outcome = null;
        if (Hammer.Advance(delta, Session.RecoveryReduction) &&
            Session.TrySwing(Herd.Targets(_strikePoint, Session.Radius), out var strike, out int coins))
        {
            Herd.Apply(strike!);
            outcome = new(_strikePoint, strike!, coins);
            if (Session.Stamina == 0) _settleDelay = _timing.SettlementDelay;
        }
        if (_settleDelay > 0)
        {
            _settleDelay -= delta;
            if (_settleDelay <= 0) EndShift();
        }
        return outcome;
    }
    public Vector2 StrikePoint => _strikePoint;
}

