using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public sealed record CombatEffectDefinition(string Id, Func<ICombatEffect> Create);
public sealed record EffectContext(float Delta, PigHerd Herd, Vector2? Aim, StrikeOutcome? WeaponStrike);
public sealed record EffectPulse(Vector2 Point, float Radius, int Damage, IReadOnlyList<PigState> Targets, string Visual, string Name = "共振波", string AudioCue = "resonance");
public sealed record EffectOutcome(Vector2 Point, float Radius, string Visual, SwingResult Strike, int Coins, string Name = "共振波", string AudioCue = "resonance");
public interface ICombatEffect
{
    IEnumerable<EffectPulse> Advance(EffectContext context);
}

/// <summary>Finite, per-shift on-hit radial pulses. Contains no equipment ID checks.</summary>
public sealed class ImpactResonance : ICombatEffect
{
    private readonly float _radius, _interval;
    private readonly int _damage, _limit;
    private int _used;
    private float _cooldown;
    public ImpactResonance(float radius, int damage, float interval, int limit)
    {
        if (!float.IsFinite(radius) || radius <= 0 || damage <= 0 || !float.IsFinite(interval) || interval <= 0 || limit <= 0)
            throw new ArgumentException("Resonance values must be finite and positive.");
        _radius = radius; _damage = damage; _interval = interval; _limit = limit;
    }
    public IEnumerable<EffectPulse> Advance(EffectContext context)
    {
        _cooldown = MathF.Max(0, _cooldown - context.Delta);
        if (_used >= _limit || _cooldown > 0 || context.WeaponStrike?.Strike.HitCount is not > 0) yield break;
        _used++; _cooldown = _interval;
        var at = context.WeaponStrike.Point;
        yield return new(at, _radius, _damage, context.Herd.Targets(at, _radius), "gold-wave");
    }
}

/// <summary>Automatic cracking with a bounded daily energy budget; fresh instances belong to each shift.</summary>
public sealed class TimedResonance : ICombatEffect
{
    private readonly float _radius, _interval;
    private readonly int _damage, _limit, _targets;
    private float _elapsed;
    private int _used;
    public TimedResonance(float radius, int damage, float interval, int limit, int targets)
    {
        if (!float.IsFinite(radius) || radius <= 0 || damage <= 0 || !float.IsFinite(interval) || interval <= 0 || limit <= 0 || targets <= 0)
            throw new ArgumentException("Passive resonance values must be finite and positive.");
        _radius = radius; _damage = damage; _interval = interval; _limit = limit; _targets = targets;
    }
    public IEnumerable<EffectPulse> Advance(EffectContext context)
    {
        _elapsed += context.Delta;
        if (_used >= _limit || _elapsed < _interval) yield break;
        var anchor = context.Herd.Pigs.Where(p => p.Active).OrderBy(p => p.State.Health).FirstOrDefault();
        if (anchor == null) yield break;
        _elapsed = 0; _used++;
        var targets = context.Herd.Pigs.Where(p => p.Active && Vector2.Distance(p.Position, anchor.Position) <= _radius + p.State.Definition.HitRadius)
            .OrderBy(p => Vector2.Distance(p.Position, anchor.Position)).Take(_targets).Select(p => p.State).ToArray();
        yield return new(anchor.Position, _radius, _damage, targets, "gold-wave");
    }
}
