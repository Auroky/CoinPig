using Godot;
using System.Collections.Generic;
using WorldPoint = System.Numerics.Vector2;

/// <summary>3D adapter for the same planar tabletop rules used by the canvas view.</summary>
public partial class TabletopView : Node3D, IWorkbench
{
    private readonly TabletopMaterials _materials = new();
    private readonly Dictionary<PigState, PigModel> _models = new();
    private Camera3D _camera = null!;
    private HammerModel _hammer = null!;
    private ImpactBurst _effects = null!;
    private float _delta;
    public Vector2 Aim { get; set; }
    public bool ShowPigs { get; set; } = true;
    public bool ShowAim { get; set; }
    public float Radius { get; set; } = 1.43f;
    public float Windup { get; set; }
    public float Cooldown { get; set; }

    public override void _Ready()
    {
        _camera = WorkshopSet.Build(this, _materials);
        _hammer = new HammerModel(); AddChild(_hammer); _hammer.Assemble(_materials);
        _effects = new ImpactBurst(); AddChild(_effects); _effects.Assemble(_materials);
        var shadeLayer = new CanvasLayer { Layer = 1 };
        AddChild(shadeLayer);
        var shade = new ColorRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Scripts/Tabletop/Vignette.gdshader") }
        };
        shadeLayer.AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _hammer.Hide();
    }

    public void Bind(IReadOnlyList<PigMotion> pigs)
    {
        foreach (var model in _models.Values) { model.Hide(); model.QueueFree(); }
        _models.Clear();
        _effects.Clear();
        foreach (var pig in pigs)
        {
            var model = new PigModel(); AddChild(model); model.Assemble(pig, _materials);
            _models.Add(pig.State, model);
        }
        Windup = Cooldown = 0;
    }

    public bool TryWorldPosition(Vector2 screenPoint, out WorldPoint worldPoint)
    {
        worldPoint = default;
        if (!GetViewport().GetVisibleRect().HasPoint(screenPoint)) return false;
        var origin = _camera.ProjectRayOrigin(screenPoint);
        var direction = _camera.ProjectRayNormal(screenPoint);
        if (Mathf.Abs(direction.Y) < .00001f) return false;
        float distance = -origin.Y / direction.Y;
        if (distance < 0) return false;
        var point = origin + direction * distance;
        worldPoint = new(point.X, point.Z);
        return point.X >= -7.4f && point.X < 7.4f && point.Z >= -3.85f && point.Z < 3.85f;
    }

    public Vector2 ScreenPosition(WorldPoint point) => _camera.UnprojectPosition(new(point.X, 0, point.Y));
    public Vector2 ScreenPosition(PigMotion pig) => ScreenPosition(pig.Position);

    public void Tick(float delta)
    {
        _delta = delta;
        foreach (var model in _models.Values) model.Tick(delta);
        _effects.Tick(delta);
    }

    public void Impact(WorldPoint point, SwingResult strike, int coins)
    {
        foreach (var hit in strike.Targets)
        {
            if (!_models.TryGetValue(hit.Pig, out var model)) continue;
            model.Hit();
            if (hit.Result.Destroyed) _effects.Break(model.Motion.Position);
        }
        _effects.Strike(point, strike, coins);
    }

    public void Present()
    {
        foreach (var model in _models.Values)
        {
            model.Present(_delta);
            model.Visible = ShowPigs && model.Motion.Active;
        }
        _effects.Visible = ShowAim;
        if (ShowAim && TryWorldPosition(Aim, out var point))
        {
            _hammer.Show();
            _hammer.Present(point, Radius, Windup, Cooldown);
        }
        else _hammer.Hide();
        _delta = 0;
    }

    public override void _ExitTree() => _materials.Dispose();
}
