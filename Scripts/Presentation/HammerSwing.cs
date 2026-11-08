using Godot;

/// <summary>Handles windup, locked aim and recovery independently from input and economy.</summary>
public sealed class HammerSwing
{
    public bool Pending { get; private set; }
    public float Windup { get; private set; }
    public float Cooldown { get; private set; }
    public Vector2 Aim { get; private set; }

    public bool Begin(Vector2 aim)
    {
        if (Pending || Cooldown > 0) return false;
        Aim = aim;
        Windup = .19f;
        Pending = true;
        return true;
    }

    public bool Advance(float delta, float recoveryReduction)
    {
        Cooldown = Mathf.Max(0, Cooldown - delta);
        if (!Pending) return false;
        Windup -= delta;
        if (Windup > 0) return false;
        Pending = false;
        Windup = 0;
        Cooldown = Mathf.Max(.12f, .39f - recoveryReduction);
        return true;
    }

    public void Reset()
    {
        Pending = false;
        Windup = Cooldown = 0;
    }
}
