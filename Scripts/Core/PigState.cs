using System;

public readonly record struct DamageResult(bool WasHit, bool Destroyed, int RemainingHealth);

/// <summary>单只猪的耐久。只允许从活着转为击碎一次，重生必须显式恢复。</summary>
public sealed class PigState
{
    public PigDefinition Definition { get; }
    public int Health { get; private set; }
    public bool IsAlive => Health > 0;

    public PigState(PigDefinition definition)
    {
        Definition = definition;
        Respawn();
    }

    public DamageResult ApplyDamage(int damage)
    {
        if (damage <= 0) throw new ArgumentOutOfRangeException(nameof(damage));
        if (!IsAlive) return new(false, false, 0);
        Health = Math.Max(0, Health - damage);
        return new(true, Health == 0, Health);
    }

    public void Respawn() => Health = Definition.MaxHealth;
}
