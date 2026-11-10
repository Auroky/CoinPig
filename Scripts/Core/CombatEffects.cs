using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

/// <summary>
/// 装备效果的身份和创建工厂；每次创建必须返回独立实例，避免跨回合共享计时与次数。
/// </summary>
public sealed record CombatEffectDefinition(string Id, Func<ICombatEffect> Create);

/// <summary>
/// 装备效果本帧可观察的上下文。Delta 为秒，Aim 和命中点使用桌面平面坐标。
/// </summary>
public sealed record EffectContext(float Delta, PigHerd Herd, Vector2? Aim, StrikeOutcome? WeaponStrike);

/// <summary>
/// 效果产生的一次伤害请求。它描述目标和伤害，实际扣血与奖励交给 GameSession。
/// </summary>
public sealed record EffectPulse(Vector2 Point, float Radius, int Damage, IReadOnlyList<PigState> Targets, string Visual, string Name = "共振波", string AudioCue = "resonance");

/// <summary>
/// 已完成结算的效果结果，供画面和声音播放使用，不能据此再次发钱。
/// </summary>
public sealed record EffectOutcome(Vector2 Point, float Radius, string Visual, SwingResult Strike, int Coins, string Name = "共振波", string AudioCue = "resonance");

/// <summary>
/// 有独立计时或次数的装备行为。只产出伤害请求，不直接修改猪的耐久、体力或钱包。
/// </summary>
public interface ICombatEffect
{
    IEnumerable<EffectPulse> Advance(EffectContext context);
}

/// <summary>
/// 由主武器命中触发的范围共振，实例保存冷却和本回合已使用次数。
/// </summary>
public sealed class ImpactResonance : ICombatEffect
{
    private readonly float _radius;
    private readonly float _interval;
    private readonly int _damage;
    private readonly int _limit;
    private int _used;
    private float _cooldown;

    public ImpactResonance(float radius, int damage, float interval, int limit)
    {
        if (!float.IsFinite(radius) || radius <= 0 || damage <= 0 || !float.IsFinite(interval) || interval <= 0 || limit <= 0)
        {
            throw new ArgumentException("Resonance values must be finite and positive.");
        }

        _radius = radius;
        _damage = damage;
        _interval = interval;
        _limit = limit;
    }

    public IEnumerable<EffectPulse> Advance(EffectContext context)
    {
        _cooldown = MathF.Max(0, _cooldown - context.Delta);
        if (_used >= _limit || _cooldown > 0 || context.WeaponStrike?.Strike.HitCount is not > 0)
        {
            yield break;
        }

        _used++;
        _cooldown = _interval;
        var at = context.WeaponStrike.Point;
        yield return new(at, _radius, _damage, context.Herd.Targets(at, _radius), "gold-wave");
    }
}

/// <summary>
/// 按时间触发的有限次共振，优先选择存活目标；无目标时保留本次触发机会。
/// </summary>
public sealed class TimedResonance : ICombatEffect
{
    private readonly float _radius;
    private readonly float _interval;
    private readonly int _damage;
    private readonly int _limit;
    private readonly int _targets;
    private float _elapsed;
    private int _used;

    public TimedResonance(float radius, int damage, float interval, int limit, int targets)
    {
        if (!float.IsFinite(radius) || radius <= 0 || damage <= 0 || !float.IsFinite(interval) || interval <= 0 || limit <= 0 || targets <= 0)
        {
            throw new ArgumentException("Passive resonance values must be finite and positive.");
        }

        _radius = radius;
        _damage = damage;
        _interval = interval;
        _limit = limit;
        _targets = targets;
    }

    public IEnumerable<EffectPulse> Advance(EffectContext context)
    {
        _elapsed += context.Delta;
        if (_used >= _limit || _elapsed < _interval)
        {
            yield break;
        }

        var anchor = context.Herd.Pigs.Where(p => p.Active).OrderBy(p => p.State.Health).FirstOrDefault();
        if (anchor == null)
        {
            yield break;
        }

        _elapsed = 0;
        _used++;
        var targets = context.Herd.Pigs.Where(p => p.Active && Vector2.Distance(p.Position, anchor.Position) <= _radius + p.State.Definition.HitRadius)
            .OrderBy(p => Vector2.Distance(p.Position, anchor.Position)).Take(_targets).Select(p => p.State).ToArray();
        yield return new(anchor.Position, _radius, _damage, targets, "gold-wave");
    }
}
