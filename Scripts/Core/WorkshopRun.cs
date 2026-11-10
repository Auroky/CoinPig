using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 已完成结算的一次主武器命中，包含实际落点、伤害快照和本次金币收益。
/// </summary>
public sealed record StrikeOutcome(Vector2 Point, SwingResult Strike, int Coins);

/// <summary>
/// 与引擎无关的运行流程：推进猪群、瞄准、落锤、装备效果和收工延时，返回可供表现播放的结果。
/// </summary>
public sealed class WorkshopRun
{
    private readonly IGameFactory _factory;
    private readonly SwingTiming _timing;
    private float _settlementRemainingSeconds;
    private Vector2 _lockedStrikePoint;
    private IReadOnlyList<ICombatEffect> _activeEffects = Array.Empty<ICombatEffect>();

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
        Session = factory.CreateSession();
        Herd = factory.CreateHerd(Session.Shift);
    }

    /// <summary>为新回合创建猪群、攻击计时和独立装备效果，再重置瞄准准备状态。</summary>
    public bool BeginShift()
    {
        if (!Session.BeginShift())
        {
            return false;
        }

        Hammer = new StrikeCycle(Session.AttackTiming(_timing));
        AutomaticAim.Reset();
        _settlementRemainingSeconds = Session.CanAttack ? 0 : _timing.SettlementDelay;
        Herd = _factory.CreateHerd(Session.Shift);
        _activeEffects = Session.ActiveEffects.Select(e => e.Create() ?? throw new InvalidOperationException("Effect factories must return an instance.")).ToArray();
        EffectOutcomes = Array.Empty<EffectOutcome>();
        return true;
    }

    public void Restart()
    {
        Session = _factory.CreateSession();
        Herd = _factory.CreateHerd(Session.Shift);
        _activeEffects = Array.Empty<ICombatEffect>();
        EffectOutcomes = Array.Empty<EffectOutcome>();
        Hammer.Reset();
        AutomaticAim.Reset();
        _settlementRemainingSeconds = 0;
    }

    /// <summary>仅在营业中且没有待完成攻击时允许收工，避免截断本次结算。</summary>
    public bool EndShift()
    {
        if (Session.Phase != ShiftPhase.Working || Hammer.Pending)
        {
            return false;
        }

        Hammer.Reset();
        _settlementRemainingSeconds = 0;
        Session.CloseShift();
        return true;
    }

    /// <summary>推进一帧规则，返回本帧已完成结算的主武器命中；空目标表示鼠标离开桌面。</summary>
    public StrikeOutcome? Advance(float delta, Vector2? target)
    {
        if (!float.IsFinite(delta) || delta < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta));
        }
        if (target is Vector2 point && (!float.IsFinite(point.X) || !float.IsFinite(point.Y)))
        {
            throw new ArgumentOutOfRangeException(nameof(target));
        }

        EffectOutcomes = Array.Empty<EffectOutcome>();
        if (Session.Phase != ShiftPhase.Working)
        {
            return null;
        }

        // 顺序有意义：先移动猪，再更新瞄准和落锤，随后处理护腕效果，最后判断是否收工。
        Herd.Advance(delta);
        UpdateAimAndWindup(delta, target);
        var outcome = ResolveHammerImpact(delta);
        AdvanceEquipmentEffects(delta, target, outcome);
        AdvanceSettlementDelay(delta);
        return outcome;
    }

    private void UpdateAimAndWindup(float delta, Vector2? target)
    {
        bool overPig = target.HasValue && Session.Targets(Herd, target.Value).Count > 0;
        bool ready = AutomaticAim.Advance(delta, target, overPig);

        // 起手早期仍跟随目标；进入锁定窗口后保持本次落点，移动鼠标只影响下一次攻击。
        if (Hammer.Pending && !IsAimLocked)
        {
            if (!target.HasValue)
            {
                Hammer.CancelWindup();
            }
            else
            {
                _lockedStrikePoint = target.Value;
            }
        }
        if (Session.CanAttack && ready && target.HasValue && Hammer.Begin())
        {
            _lockedStrikePoint = target.Value;
        }
    }

    private StrikeOutcome? ResolveHammerImpact(float delta)
    {
        if (!Hammer.Advance(delta, Session.RecoveryReduction))
        {
            return null;
        }

        var targets = Session.Targets(Herd, _lockedStrikePoint);
        // 第一次落锤前允许目标逃离并取消，不让玩家在刚开局时无意损失体力。
        if (!AutomaticAim.HasLanded && targets.Count == 0)
        {
            Hammer.Reset();
            return null;
        }
        if (!Session.TrySwing(targets, out var strike, out int coins))
        {
            return null;
        }

        AutomaticAim.RecordLanding();
        Herd.Apply(strike!);
        var outcome = new StrikeOutcome(_lockedStrikePoint, strike!, coins);
        if (!Session.CanAttack)
        {
            _settlementRemainingSeconds = _timing.SettlementDelay;
        }
        return outcome;
    }

    private void AdvanceEquipmentEffects(float delta, Vector2? target, StrikeOutcome? weaponStrike)
    {
        var effectResults = new List<EffectOutcome>();
        var context = new EffectContext(delta, Herd, target, weaponStrike);
        foreach (var effect in _activeEffects)
        {
            foreach (var pulse in effect.Advance(context))
            {
                // 装备效果也走会话的统一伤害与奖励结算，不另开一套扣血、发钱逻辑。
                if (Session.ApplyEffect(pulse, out var effectStrike, out int effectCoins))
                {
                    Herd.Apply(effectStrike!);
                    effectResults.Add(new(pulse.Point, pulse.Radius, pulse.Visual, effectStrike!, effectCoins, pulse.Name, pulse.AudioCue));
                }
            }
        }
        EffectOutcomes = Array.AsReadOnly(effectResults.ToArray());
    }

    private void AdvanceSettlementDelay(float delta)
    {
        if (_settlementRemainingSeconds <= 0)
        {
            return;
        }

        // 体力用尽后留出最后一击的反馈时间，而不是立即切掉攻击动画。
        _settlementRemainingSeconds -= delta;
        if (_settlementRemainingSeconds <= 0)
        {
            EndShift();
        }
    }

    public Vector2 StrikePoint => _lockedStrikePoint;
    public bool IsAimLocked => Hammer.Pending && Hammer.Windup <= Hammer.AimLockSeconds + .00001f;
}
