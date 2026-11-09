using Godot;
using System.Collections.Generic;

/// <summary>Mesh pose, damage feedback and respawn appearance, driven by the shared model.</summary>
public partial class PigModel : Node3D
{
    private enum PigSurface { Ceramic, Tin, GoldenNose }
    private sealed record Finish(string Color, float Scale, PigSurface Surface);
    private static readonly IReadOnlyDictionary<string, Finish> Finishes = new Dictionary<string, Finish>
    {
        ["ceramic"] = new("ea9a95", 1, PigSurface.Ceramic),
        ["tin"] = new("738b92", 1.12f, PigSurface.Tin),
        ["swift"] = new("e8bd84", .86f, PigSurface.GoldenNose)
    };
    private PigMotion _motion = null!;
    private Finish _finish = null!;
    private TabletopMaterials _materials = null!;
    private Node3D _body = null!, _key = null!, _cracks = null!;
    private readonly Node3D[] _legs = new Node3D[4];
    private Label3D _healthLabel = null!;
    private Font _font = null!;
    private System.Numerics.Vector2 _previous;
    private int _index;
    private float _yaw, _hurtTime, _healthSeconds, _spawnAge = .16f;
    private bool _wasActive;
    public PigMotion Motion => _motion;
    public PigState State => _motion.State;
    public Color Glaze { get; private set; }
    public Color Interior => new(_finish.Surface == PigSurface.Tin ? "a5afb0" : "e0cbb9");
    public float BodyScale => _finish.Scale;
    private MeshInstance3D Mesh(Node3D parent, Mesh mesh, Vector3 at, Color color) => SceneShapes.Mesh(parent, mesh, at, _materials.Paint(color));
    private MeshInstance3D Box(Node3D parent, Vector3 at, Vector3 size, Color color) => SceneShapes.Box(parent, at, size, _materials.Paint(color));
    private MeshInstance3D Cylinder(Node3D parent, Vector3 at, float bottom, float top, float height, Color color, int sides = 8) => SceneShapes.Cylinder(parent, at, bottom, top, height, _materials.Paint(color), sides);
    private MeshInstance3D Beam(Node3D parent, Vector3 a, Vector3 b, float width, Color color) => SceneShapes.Beam(parent, a, b, width, _materials.Paint(color));
    private MeshInstance3D Oval(Node3D parent, Vector3 at, Vector3 radius, Color color) => SceneShapes.Ellipsoid(parent, at, radius, _materials.Paint(color));
    public void Assemble(PigMotion motion, int index, TabletopMaterials materials)
    {
        _index = index;
        _motion = motion;
        _materials = materials;
        _previous = motion.Position;
        _wasActive = motion.Active;
        _yaw = motion.Position.X > 0 ? -Mathf.Pi / 2 : Mathf.Pi / 2;
        _finish = Finishes.TryGetValue(motion.State.Definition.Id, out var known) ? known : Finishes["ceramic"];
        _font = WorkshopStyle.BodyFont();
        Glaze = new Color(_finish.Color);
        var contactShade = materials.ContactShade();
        AddChild(new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new(1.25f, 1.80f) },
            Position = new(0.035f, 0.014f, 0.02f),
            MaterialOverride = contactShade,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        });
        _body = new Node3D();
        AddChild(_body);
        var rosy = Glaze.Darkened(0.13f);
        Color nose = _finish.Surface == PigSurface.GoldenNose ? new Color("e0a638") : rosy;
        Oval(_body, new(0, 0.51f, -0.09f), new(0.49f, 0.44f, 0.64f), Glaze);
        Oval(_body, new(0, 0.57f, 0.35f), new(0.41f, 0.37f, 0.37f), Glaze);
        Oval(_body, new(0, 0.50f, 0.695f), new(0.234f, 0.154f, 0.125f), nose);
        foreach (float side in new[] { -1f, 1f })
        {
            Oval(_body, new(side * 0.085f, 0.51f, 0.802f), new(0.032f, 0.044f, 0.018f), new("835553"));
            Oval(_body, new(side * 0.285f, 0.66f, 0.595f), new(0.056f, 0.059f, 0.052f), new("322d30"));
            Oval(_body, new(side * 0.297f - 0.012f, 0.682f, 0.626f), new(0.014f, 0.015f, 0.010f), new("fff0d6"));
            var ear = Cylinder(_body, new(side * 0.263f, 0.865f, 0.318f), 0.155f, 0.022f, 0.295f, Glaze, 3);
            ear.RotationDegrees = new(-12, side * 24, -side * 21);
            var inside = Cylinder(ear, new(0, 0.006f, 0.040f), 0.094f, 0.012f, 0.203f, new("ba7776"), 3);
            inside.RotationDegrees = new(-7, 0, 0);
        }
        Box(_body, new(0, 0.947f, -0.10f), new(0.074f, 0.015f, 0.29f), new("755557"));
        for (int i = 0; i < 4; i++)
        {
            float side = i % 2 == 0 ? -1 : 1;
            _legs[i] = new Node3D { Position = new(side * 0.25f, 0.32f, i < 2 ? 0.27f : -0.37f) };
            _body.AddChild(_legs[i]);
            Oval(_legs[i], new(0, -0.11f, 0), new(0.106f, 0.178f, 0.115f), rosy);
            Oval(_legs[i], new(0, -0.231f, 0.018f), new(0.096f, 0.063f, 0.106f), new("995e56"));
        }
        _key = new Node3D { Position = new(0, 0.62f, -0.64f) };
        _body.AddChild(_key);
        var brass = new Color("d6a14d");
        Beam(_key, Vector3.Zero, new(0, 0, -0.21f), 0.069f, brass);
        foreach (float x in new[] { -0.096f, 0.096f })
        {
            var loop = Mesh(_key, new TorusMesh { InnerRadius = 0.054f, OuterRadius = 0.10f, Rings = 18, RingSegments = 7 }, new(x, 0, -0.237f), brass);
            loop.RotationDegrees = new(65, 0, 0);
        }
        BuildDurabilityDisplay();
        AddSpeciesDetails();
        _body.Scale = Vector3.One * _finish.Scale;
    }

    private void BuildDurabilityDisplay()
    {
        _healthLabel = new Label3D
        {
            Position = new(0, 1.40f * _finish.Scale, 0),
            Font = _font,
            FontSize = 30,
            PixelSize = 0.009f,
            OutlineSize = 5,
            Modulate = new("fff0bb"),
            OutlineModulate = new("283c40"),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true
        };
        AddChild(_healthLabel);
        _healthLabel.Hide();
        _cracks = new Node3D();
        _body.AddChild(_cracks);
        Vector3[] crack =
        {
            new(-0.28f, 0.85f, 0.10f), new(-0.13f, 0.94f, 0.03f),
            new(-0.04f, 0.95f, -0.22f), new(0.11f, 0.91f, -0.29f), new(0.22f, 0.83f, -0.38f)
        };
        for (int i = 1; i < crack.Length; i++)
            Beam(_cracks, crack[i - 1], crack[i], 0.029f, new("303637"));
        _cracks.Hide();
    }

    private void AddSpeciesDetails()
    {
        if (_finish.Surface == PigSurface.Tin)
        {
            var darkMetal = new Color("3d535d");
            foreach (float side in new[] { -1f, 1f })
            {
                var plate = Box(_body, new(side * 0.465f, 0.54f, -0.12f), new(0.06f, 0.23f, 0.53f), darkMetal);
                plate.RotationDegrees = new(0, 0, side * -6);
                for (int i = 0; i < 3; i++)
                    Oval(_body, new(side * 0.501f, 0.60f, -0.32f + i * 0.20f),
                        new(0.032f, 0.035f, 0.032f), new("d1c29f"));
            }
        }
        else if (_finish.Surface == PigSurface.GoldenNose)
        {
            var collar = Mesh(_body, new TorusMesh { InnerRadius = 0.28f, OuterRadius = 0.32f, Rings = 24, RingSegments = 8 },
                new(0, 0.50f, 0.32f), new("d9a34e"));
            collar.RotationDegrees = new(90, 0, 0);
        }
    }

    public void Hit(DamageResult damage)
    {
        if (!damage.WasHit) return;
        if (damage.Destroyed) { _healthLabel.Hide(); return; }
        _hurtTime = .18f;
        _healthSeconds = 1.5f;
        _healthLabel.Text = $"耐久 {damage.RemainingHealth} / {State.Definition.MaxHealth}";
    }

    public void Tick(float delta)
    {
        _hurtTime = Mathf.Max(0, _hurtTime - delta);
        _healthSeconds = Mathf.Max(0, _healthSeconds - delta);
        _spawnAge += delta;
    }

    public void Present()
    {
        var movement = _motion.Position - _previous;
        if (movement.LengthSquared() > .0000001f) _yaw = Mathf.Atan2(movement.X, movement.Y);
        _previous = _motion.Position;
        Position = new(_motion.Position.X, 0, _motion.Position.Y);
        Rotation = new(0, _yaw, 0);
        if (_motion.Active && !_wasActive) _spawnAge = 0;
        _wasActive = _motion.Active;
        float t = Mathf.Clamp(_spawnAge / .16f, 0, 1) - 1;
        float back = 1 + 2.70158f * t * t * t + 1.70158f * t * t;
        float spawn = Mathf.Lerp(.45f, 1, back);
        float squash = Mathf.Clamp(_hurtTime / .18f, 0, 1);
        float stride = _motion.Time * (11 + _index % 3) + _index;
        _body.Scale = new Vector3(1 + .12f * squash, 1 - .15f * squash, 1 + .12f * squash) * _finish.Scale * spawn;
        _body.Position = new(0, .045f * Mathf.Abs(Mathf.Sin(stride)), 0);
        _body.Rotation = new(Mathf.Sin(stride * 2) * .03f, 0, Mathf.Cos(stride) * .047f);
        for (int i = 0; i < _legs.Length; i++)
            _legs[i].Rotation = new(Mathf.Sin(stride + (i == 0 || i == 3 ? 0 : Mathf.Pi)) * .45f, 0, 0);
        _key.Rotation = new(0, 0, _motion.Time * 2.2f + _index);
        _healthLabel.Visible = _healthSeconds > 0 && _motion.Active && State.Health < State.Definition.MaxHealth;
        _cracks.Visible = _motion.Active && State.Health < State.Definition.MaxHealth;
    }

    public override void _ExitTree() => _font?.Dispose();
}
