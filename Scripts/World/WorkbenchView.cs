using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Maps world-space pigs to the workbench and draws the aiming tool.</summary>
public partial class WorkbenchView : Node2D
{
    private IReadOnlyList<PigMotion> _pigs = Array.Empty<PigMotion>();
    public Vector2 Aim { get; set; }
    public bool ShowPigs { get; set; } = true;
    private bool _showAim;
    public bool ShowAim
    {
        get => _showAim;
        set
        {
            _showAim = value;
            if (_effects != null) _effects.Visible = value;
        }
    }
    public float Radius { get; set; } = 1.43f;
    public float Windup { get; set; }
    public float Cooldown { get; set; }
    public float UnitScale => Mathf.Min(GetViewportRect().Size.X / 19.5f, GetViewportRect().Size.Y / 11f);
    public Vector2 Origin => GetViewportRect().Size / 2 + new Vector2(0, 12);
    public Rect2 PlayArea => new(Origin + new Vector2(-7.4f, -3.85f) * UnitScale, new Vector2(14.8f, 7.7f) * UnitScale);
    private readonly Dictionary<PigState, float> _flashes = new();
    private StrikeEffects _effects = null!;

    public override void _Ready()
    {
        AddChild(new WorkbenchBackdrop());
        _effects = new StrikeEffects();
        AddChild(_effects);
    }

    public void Bind(IReadOnlyList<PigMotion> pigs)
    {
        _pigs = pigs;
        _flashes.Clear();
        _effects.Clear();
        Windup = Cooldown = 0;
        QueueRedraw();
    }

    public Vector2 ScreenPosition(PigMotion pig) => Origin + new Vector2(pig.Position.X, pig.Position.Y) * UnitScale;
    public System.Numerics.Vector2 WorldPosition(Vector2 screenPoint)
    {
        Vector2 local = (screenPoint - Origin) / UnitScale;
        return new(local.X, local.Y);
    }

    public void Tick(float delta)
    {
        foreach (var key in _flashes.Keys.ToArray()) _flashes[key] = Mathf.Max(0, _flashes[key] - delta);
        _effects.Tick(delta);
        QueueRedraw();
    }

    public void Impact(Vector2 point, SwingResult strike, int coins)
    {
        foreach (var hit in strike.Targets)
        {
            _flashes[hit.Pig] = .22f;
            var pig = _pigs.First(p => ReferenceEquals(p.State, hit.Pig));
            if (hit.Result.Destroyed) _effects.Break(ScreenPosition(pig), PigPainter.Look(hit.Pig.Definition.Id).Body);
        }
        _effects.Strike(point, Radius * UnitScale, strike.DestroyedCount, strike.HitCount, coins);
    }

    public override void _Draw()
    {
        if (ShowPigs)
            foreach (var pig in _pigs.OrderBy(pig => pig.Position.Y))
                if (pig.Active) PigPainter.Draw(this, pig, ScreenPosition(pig), UnitScale / 55, _flashes.GetValueOrDefault(pig.State));
        if (!ShowAim || !PlayArea.HasPoint(Aim)) return;
        float radius = Radius * UnitScale;
        Color ring = Windup > 0 ? new("e0b76d") : Cooldown > 0 ? new Color(.7f, .65f, .5f, .35f) : new Color(.95f, .85f, .63f, .75f);
        DrawArc(Aim, radius, 0, Mathf.Tau, 96, ring, 1.5f, true);
        DrawLine(Aim - new Vector2(5, 0), Aim + new Vector2(5, 0), ring, 1);
        DrawLine(Aim - new Vector2(0, 5), Aim + new Vector2(0, 5), ring, 1);
        DrawHammer(Aim + new Vector2(20, -23 - Windup * 150), Windup * -2);
    }

    private void DrawHammer(Vector2 position, float rotation)
    {
        DrawSetTransform(position, rotation, Vector2.One * (UnitScale / 65));
        DrawLine(new(8, 0), new(42, 44), new Color(0, 0, 0, .3f), 13, true);
        DrawLine(new(6, -2), new(39, 40), new("a77945"), 10, true);
        DrawLine(new(25, 22), new(40, 41), new("342b22"), 13, true);
        DrawRect(new(-29, -22, 60, 35), new("252e28"));
        DrawRect(new(-27, -20, 8, 31), new("9da28a"));
        DrawRect(new(21, -20, 8, 31), new("9da28a"));
        DrawRect(new(-3, -21, 7, 33), new("bfa06b"));
        DrawSetTransform(Vector2.Zero);
    }
}
