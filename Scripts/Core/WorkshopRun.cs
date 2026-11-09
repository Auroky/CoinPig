using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public sealed record StrikeOutcome(Vector2 Point, SwingResult Strike, int Coins);

/// <summary>Engine-independent application flow; input and presentation are adapters.</summary>
public sealed class WorkshopRun
{
    private readonly IGameFactory _factory;
    private readonly SwingTiming _timing;
    private float _settleDelay;
    private Vector2 _strikePoint;
    private IReadOnlyList<ICombatEffect> _effects = Array.Empty<ICombatEffect>();
    public IReadOnlyList<EffectOutcome> EffectOutcomes { get; private set; } = Array.Empty<EffectOutcome>();
    public GameSession Session { get; private set; }
    public PigHerd Herd { get; private set; }
    public StrikeCycle Hammer { get; private set; }
    public AutomaticAim AutomaticAim { get; }

    public WorkshopRun(IGameFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
        _timing = factory.Timing;
        Hammer = new StrikeCycle(_timing);
        AutomaticAim = new AutomaticAim(_timing);
        Session = factory.CreateSession(); Herd = factory.CreateHerd();
    }

    public bool BeginShift()
    {
        if (!Session.BeginShift()) return false;
        Hammer = new StrikeCycle(Session.AttackTiming(_timing)); AutomaticAim.Reset();
        _settleDelay = Session.CanAttack ? 0 : _timing.SettlementDelay; Herd = _factory.CreateHerd();
        _effects = Session.ActiveEffects.Select(e => e.Create() ?? throw new InvalidOperationException("Effect factories must return an instance.")).ToArray();
        EffectOutcomes = Array.Empty<EffectOutcome>();
        return true;
    }
    public void Restart()
    {
        Session = _factory.CreateSession(); Herd = _factory.CreateHerd();
        _effects = Array.Empty<ICombatEffect>(); EffectOutcomes = Array.Empty<EffectOutcome>();
        Hammer.Reset(); AutomaticAim.Reset(); _settleDelay = 0;
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
        EffectOutcomes = Array.Empty<EffectOutcome>();
        if (Session.Phase != ShiftPhase.Working) return null;
        Herd.Advance(delta);
        bool overPig = target.HasValue && Session.Targets(Herd, target.Value).Count > 0;
        bool ready = AutomaticAim.Advance(delta, target, overPig);
        if (Hammer.Pending && !IsAimLocked)
        {
            if (!target.HasValue) Hammer.CancelWindup();
            else _strikePoint = target.Value;
        }
        if (Session.CanAttack && ready && target.HasValue && Hammer.Begin()) _strikePoint = target.Value;
        StrikeOutcome? outcome = null;
        if (Hammer.Advance(delta, Session.RecoveryReduction))
        {
            var targets = Session.Targets(Herd, _strikePoint);
            // The opening swing is free to abort if its target escaped during windup.
            if (!AutomaticAim.HasLanded && targets.Count == 0) Hammer.Reset();
            else if (Session.TrySwing(targets, out var strike, out int coins))
            {
                AutomaticAim.RecordLanding();
                Herd.Apply(strike!);
                outcome = new(_strikePoint, strike!, coins);
                if (!Session.CanAttack) _settleDelay = _timing.SettlementDelay;
            }
        }
        var effectResults = new List<EffectOutcome>();
        var context = new EffectContext(delta, Herd, target, outcome);
        foreach (var effect in _effects)
            foreach (var pulse in effect.Advance(context))
                if (Session.ApplyEffect(pulse, out var effectStrike, out int effectCoins))
                {
                    Herd.Apply(effectStrike!);
                    effectResults.Add(new(pulse.Point, pulse.Radius, pulse.Visual, effectStrike!, effectCoins, pulse.Name, pulse.AudioCue));
                }
        EffectOutcomes = Array.AsReadOnly(effectResults.ToArray());
        if (_settleDelay > 0)
        {
            _settleDelay -= delta;
            if (_settleDelay <= 0) EndShift();
        }
        return outcome;
    }
    public Vector2 StrikePoint => _strikePoint;
    public bool IsAimLocked => Hammer.Pending && Hammer.Windup <= Hammer.AimLockSeconds + .00001f;
}

