using System;

/// <summary>
/// 一次扣血的结果，明确区分未命中、受伤和本次击碎。
/// </summary>
public readonly record struct DamageResult(bool WasHit, bool Destroyed, int RemainingHealth);

/// <summary>
/// 封装单只猪的耐久。击碎只能发生一次，重生必须显式恢复耐久，外部不能直接赋值生命值。
/// </summary>
public sealed class PigState
{
    public PigDefinition Definition { get; }
    public int Health { get; private set; }
    public bool IsAlive => Health > 0;

    public PigState(PigDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (string.IsNullOrWhiteSpace(definition.Id) || definition.MaxHealth <= 0 ||
            definition.Coins < 0 || !float.IsFinite(definition.Speed) || definition.Speed <= 0 || !float.IsFinite(definition.RespawnSeconds) || definition.RespawnSeconds <= 0 || !float.IsFinite(definition.HitRadius) || definition.HitRadius < 0)
        {
            throw new ArgumentException("A pig requires a valid ID, positive health and speed, and nonnegative rewards.", nameof(definition));
        }

        Definition = definition;
        Respawn();
    }

    public DamageResult ApplyDamage(int damage)
    {
        if (damage <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage));
        }

        if (!IsAlive)
        {
            return new(false, false, 0);
        }

        Health = Math.Max(0, Health - damage);
        return new(true, Health == 0, Health);
    }

    public void Respawn() => Health = Definition.MaxHealth;
}
