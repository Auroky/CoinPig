using Godot;
using System;
using System.Collections.Generic;
using WorldPoint = System.Numerics.Vector2;

/// <summary>3D adapter for the same planar tabletop rules used by the canvas view.</summary>
public partial class TabletopView : Node3D, IWorkbench, IRewardFeedback, IPigAppearanceConsumer
{
    public IPigAppearanceSource Appearances { private get; set; } = new PigRoster(Array.Empty<PigArchetype>());
    private readonly TabletopMaterials _materials = new();
    private readonly Dictionary<PigState, PigModel> _models = new();
    private HammerModel _hammer = null!;
    private ImpactBurst _effects = null!;
    private CameraFeedback _feedback = null!;
    private WorldPoint _lastAim;
    private bool _hasAim;
    private float _delta;
    public Vector2 CoinDestination { get; set; }
    public event Action? CoinCollected;
    public Vector2 Aim { get; set; }
    public bool ShowPigs { get; set; } = true;
    public bool ShowAim { get; set; }
    public float Radius { get; set; } = 1.43f;
    public float Windup { get; set; }
    public float Cooldown { get; set; }

    public override void _Ready()
    {
        var camera = WorkshopSet.Build(this, _materials);
        _feedback = new CameraFeedback(camera);
        _hammer = new HammerModel(); AddChild(_hammer); _hammer.Assemble(_materials);
        _effects = new ImpactBurst { Name = "ImpactEffects" }; AddChild(_effects); _effects.Assemble(_materials);
        _effects.CoinCollected += () => CoinCollected?.Invoke();
        _hammer.Hide();
    }

    public void Bind(IReadOnlyList<PigMotion> pigs)
    {
        foreach (var model in _models.Values) { model.Hide(); model.QueueFree(); }
        _models.Clear();
        _effects.Clear();
        _feedback.Reset();
        _hammer.ResetPose();
        _hasAim = false;
        int index = 0;
        foreach (var pig in pigs)
        {
            var model = new PigModel(); AddChild(model); model.Assemble(pig, index++, _materials, Appearances.GetAppearance(pig.State.Definition.Id));
            _models.Add(pig.State, model);
        }
        Windup = Cooldown = 0;
    }

    public bool TryWorldPosition(Vector2 screenPoint, out WorldPoint worldPoint)
    {
        worldPoint = default;
        if (!GetViewport().GetVisibleRect().HasPoint(screenPoint)) return false;
        if (!_feedback.PointAt(screenPoint, 0, out var point)) return false;
        worldPoint = new(point.X, point.Z);
        return point.X >= -7.4f && point.X < 7.4f && point.Z >= -3.85f && point.Z < 3.85f;
    }

    public Vector2 ScreenPosition(WorldPoint point) => _feedback.Project(new(point.X, 0, point.Y));
    public Vector2 ScreenPosition(PigMotion pig) => ScreenPosition(pig.Position);

    public void Tick(float delta)
    {
        _delta = delta;
        _feedback.Tick(delta);
        foreach (var model in _models.Values) model.Tick(delta);
        _effects.Tick(delta);
    }

    public void Impact(WorldPoint point, SwingResult strike)
    {
        _hammer.LandAt(point);
        _lastAim = point; _hasAim = true;
        _feedback.Punch(strike.HitCount, strike.DestroyedCount);
        _feedback.PointAt(CoinDestination, .8f, out var destination);
        foreach (var hit in strike.Targets)
        {
            if (!_models.TryGetValue(hit.Pig, out var model)) continue;
            model.Hit(hit.Result);
            if (hit.Result.Destroyed)
                _effects.Break(model.Motion.Position, point, model.Glaze, model.Interior, model.BodyScale, hit.Pig.Definition.Coins, destination);
            else _effects.HitSparks(model.Motion.Position);
        }
        _effects.Strike(point, strike, Radius);
    }

    public void Present()
    {
        foreach (var model in _models.Values)
        {
            model.Present();
            model.Visible = ShowPigs && model.Motion.Active;
        }
        _effects.Visible = ShowAim;
        bool valid = TryWorldPosition(Aim, out var point);
        if (valid) { _lastAim = point; _hasAim = true; }
        if (ShowAim && (valid || (Cooldown > 0 && _hasAim)))
        {
            _hammer.Show();
            _hammer.Present(valid ? point : _lastAim, Radius, Windup, Cooldown, _delta, valid);
        }
        else _hammer.Hide();
        if (!ShowAim) _feedback.Reset();
        _delta = 0;
    }

    public override void _ExitTree() => _materials.Dispose();
}
