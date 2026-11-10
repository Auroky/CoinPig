using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// 一次攻击使用的最终伤害、世界空间半径和体力消耗。
/// </summary>
public readonly record struct AttackStats(int Damage, float Radius, int StaminaCost);

/// <summary>
/// 可替换的武器规则：计算合法攻击数值、节奏和目标集合；目标查询不直接扣血或发钱。
/// </summary>
public interface IWeaponAttack
{
    AttackStats Stats(GameTuning tuning, Bonus bonus);
    SwingTiming Timing(SwingTiming baseline);
    IReadOnlyList<PigState> Targets(PigHerd herd, Vector2 point, AttackStats stats);
}

/// <summary>
/// 共用圆形范围查询的武器行为。不同工具用参数改变伤害、范围与节奏。
/// </summary>
public sealed class CircularWeaponAttack : IWeaponAttack
{
    private readonly int _damage;
    private readonly int _cost;
    private readonly float _radius;
    private readonly float? _windup;
    private readonly float? _recovery;
    private readonly float? _lock;

    public CircularWeaponAttack(int extraDamage = 0, float extraRadius = 0, int staminaCost = 1,
        float? windup = null, float? recovery = null, float? aimLock = null)
    {
        if (extraDamage < 0 || staminaCost <= 0 || !float.IsFinite(extraRadius) ||
            (windup.HasValue && (!float.IsFinite(windup.Value) || windup <= 0)) ||
            (recovery.HasValue && (!float.IsFinite(recovery.Value) || recovery <= 0)) ||
            (aimLock.HasValue && (!float.IsFinite(aimLock.Value) || aimLock < 0)))
        {
            throw new ArgumentException("Attack parameters must be finite and valid.");
        }

        _damage = extraDamage;
        _radius = extraRadius;
        _cost = staminaCost;
        _windup = windup;
        _recovery = recovery;
        _lock = aimLock;
    }

    public AttackStats Stats(GameTuning tuning, Bonus bonus)
    {
        var result = new AttackStats(checked(tuning.BaseDamage + bonus.Damage + _damage),
            tuning.BaseRadius + bonus.Radius + _radius, _cost);
        if (!float.IsFinite(result.Radius) || result.Radius <= 0)
        {
            throw new InvalidOperationException("Weapon radius must remain finite and positive.");
        }

        return result;
    }

    public SwingTiming Timing(SwingTiming baseline) => baseline with
    {
        Windup = _windup ?? baseline.Windup,
        Recovery = _recovery ?? baseline.Recovery,
        MinimumRecovery = MathF.Min(baseline.MinimumRecovery, _recovery ?? baseline.Recovery),
        AimLockSeconds = _lock ?? MathF.Min(baseline.AimLockSeconds, _windup ?? baseline.Windup)
    };

    public IReadOnlyList<PigState> Targets(PigHerd herd, Vector2 point, AttackStats stats) => herd.Targets(point, stats.Radius);
}

/// <summary>
/// 武器身份、攻击策略，以及表现层使用的造型、动作和声音键。
/// </summary>
public sealed record WeaponDefinition(string Id, string Name, IWeaponAttack Attack,
    string Appearance = "hammer", string Motion = "swing", string AudioCue = "hammer");

/// <summary>
/// 装备的颜色配置；由模型和图标适配器解释，不参与战斗规则。
/// </summary>
public sealed record EquipmentPalette(string Body, string Trim, string Metal, string Grip, string Glow);

/// <summary>
/// 某一级装备的外观、效果和属性增量。Effects 表示当前等级效果，不叠加已替代的旧等级。
/// </summary>
public sealed record EquipmentTier(string Name, EquipmentPalette Palette, string Description = "",
    IReadOnlyList<CombatEffectDefinition>? Effects = null, Bonus? UpgradeBonus = null);

/// <summary>
/// 商品解锁需要的前置商品编号和已购买等级。
/// </summary>
public sealed record ProductRequirement(string ProductId, int Level);

/// <summary>
/// 当前装备武器的只读快照，供界面和模型同步显示。
/// </summary>
public sealed record WeaponLoadout(WeaponDefinition Weapon, int Level, EquipmentPalette Palette, string? ProductId);

/// <summary>
/// 切换装备的结果；只有已拥有的武器可切换，护腕不能替代主武器。
/// </summary>
public enum EquipStatus
{
    Equipped, AlreadyEquipped, Closed, UnknownProduct, NotOwned, NotAWeapon
}

/// <summary>
/// 切换装备命令的结果；已经装备目标武器也视为成功。
/// </summary>
public sealed record EquipResult(EquipStatus Status)
{
    public bool Succeeded => Status == EquipStatus.Equipped || Status == EquipStatus.AlreadyEquipped;
}

/// <summary>
/// 切换已拥有武器的能力，与商店展示和购买报价分开。
/// </summary>
public interface IWeaponLoadout
{
    EquipResult TryEquip(string productId);
}

/// <summary>
/// 装备展示需要的只读状态，避免视图依赖会话全部命令。
/// </summary>
public interface IEquipmentStatusView
{
    WeaponLoadout EquippedWeapon { get; }
    EquipmentTier? EquippedAccessory { get; }
    int StaminaCost { get; }
}
