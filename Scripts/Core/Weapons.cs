using System;
using System.Collections.Generic;
using System.Numerics;

public readonly record struct AttackStats(int Damage, float Radius, int StaminaCost);

/// <summary>Weapon behavior is injected. New targeting or timing rules implement this contract.</summary>
public interface IWeaponAttack
{
    AttackStats Stats(GameTuning tuning, Bonus bonus);
    SwingTiming Timing(SwingTiming baseline);
    IReadOnlyList<PigState> Targets(PigHerd herd, Vector2 point, AttackStats stats);
}

/// <summary>Existing tools share circular targeting; their parameters differ, not the session control flow.</summary>
public sealed class CircularWeaponAttack : IWeaponAttack
{
    private readonly int _damage, _cost;
    private readonly float _radius;
    private readonly float? _windup, _recovery, _lock;
    public CircularWeaponAttack(int extraDamage = 0, float extraRadius = 0, int staminaCost = 1,
        float? windup = null, float? recovery = null, float? aimLock = null)
    {
        if (extraDamage < 0 || staminaCost <= 0 || !float.IsFinite(extraRadius) ||
            (windup.HasValue && (!float.IsFinite(windup.Value) || windup <= 0)) ||
            (recovery.HasValue && (!float.IsFinite(recovery.Value) || recovery <= 0)) ||
            (aimLock.HasValue && (!float.IsFinite(aimLock.Value) || aimLock < 0)))
            throw new ArgumentException("Attack parameters must be finite and valid.");
        _damage = extraDamage; _radius = extraRadius; _cost = staminaCost;
        _windup = windup; _recovery = recovery; _lock = aimLock;
    }
    public AttackStats Stats(GameTuning tuning, Bonus bonus)
    {
        var result = new AttackStats(checked(tuning.BaseDamage + bonus.Damage + _damage),
            tuning.BaseRadius + bonus.Radius + _radius, _cost);
        if (!float.IsFinite(result.Radius) || result.Radius <= 0)
            throw new InvalidOperationException("Weapon radius must remain finite and positive.");
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

public sealed record WeaponDefinition(string Id, string Name, IWeaponAttack Attack,
    string Appearance = "hammer", string Motion = "swing", string AudioCue = "hammer");
public sealed record EquipmentPalette(string Body, string Trim, string Metal, string Grip, string Glow);
public sealed record EquipmentTier(string Name, EquipmentPalette Palette, string Description = "",
    IReadOnlyList<CombatEffectDefinition>? Effects = null, Bonus? UpgradeBonus = null);
public sealed record ProductRequirement(string ProductId, int Level);
public sealed record WeaponLoadout(WeaponDefinition Weapon, int Level, EquipmentPalette Palette, string? ProductId);

public enum EquipStatus { Equipped, AlreadyEquipped, Closed, UnknownProduct, NotOwned, NotAWeapon }
public sealed record EquipResult(EquipStatus Status)
{
    public bool Succeeded => Status == EquipStatus.Equipped || Status == EquipStatus.AlreadyEquipped;
}
public interface IWeaponLoadout
{
    EquipResult TryEquip(string productId);
}
public interface IEquipmentStatusView
{
    WeaponLoadout EquippedWeapon { get; }
    EquipmentTier? EquippedAccessory { get; }
    int StaminaCost { get; }
}
