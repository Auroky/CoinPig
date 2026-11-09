using Godot;

/// <summary>Common aim-following renderer; geometry and motion are registered independently.</summary>
public partial class WeaponModel : Node3D
{
    private Node3D _head = null!, _cursor = null!;
    private Node3D? _accessoryRoot;
    private MeshInstance3D _ring = null!;
    private StandardMaterial3D _idle = null!, _windup = null!, _recovery = null!;
    private TabletopMaterials _materials = null!;
    public EquipmentModelRegistry Models { get; set; } = EquipmentModelRegistry.CreateDefault();
    public WeaponMotionRegistry Motions { get; set; } = WeaponMotionRegistry.CreateDefault();
    private EquipmentModelParts? _parts;
    private IWeaponMotion _motion = new SwingWeaponMotion();
    private Vector3 _visualPoint, _swingFrom, _impactPoint;
    private Vector2 _sway;
    private bool _hasPosition, _swinging;
    private float _lastRadius, _lastCooldown, _time, _recoveryDuration = .39f, _windupDuration = .19f;
    internal Vector3 VisualPosition => _visualPoint;
    internal string Appearance { get; private set; } = "hammer";
    public void Assemble(TabletopMaterials materials)
    {
        _materials = materials;
        _idle = RingPaint("f8e7bf"); _windup = RingPaint("ffb358"); _recovery = RingPaint("968974");
        _ring = SceneShapes.Mesh(this, new TorusMesh(), Vector3.Zero, _idle);
        _ring.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        _cursor = new Node3D(); AddChild(_cursor);
        foreach (var size in new[] { new Vector3(.17f, .006f, .025f), new(.025f, .006f, .17f) })
            SceneShapes.Box(_cursor, Vector3.Zero, size, _idle).CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        _head = new Node3D(); AddChild(_head);
        SetEquipment(new(new("basic", "基础锤", new CircularWeaponAttack()), 0, ShopProduct.StandardPalette, null), null);
    }
    public void SetEquipment(WeaponLoadout loadout, EquipmentTier? accessory)
    {
        _parts?.Root.Hide(); _parts?.Root.QueueFree();
        Appearance = loadout.Weapon.Appearance;
        _parts = Models.Build(Appearance, _head, _materials, loadout.Palette, loadout.Level);
        _motion = Motions.Get(loadout.Weapon.Motion);
        _accessoryRoot?.Hide(); _accessoryRoot?.QueueFree(); _accessoryRoot = null;
        if (accessory != null)
        {
            _accessoryRoot = new Node3D { Scale = Vector3.One * .28f, RotationDegrees = new(-20, 0, 20) };
            AddChild(_accessoryRoot);
            Models.Build("cuff", _accessoryRoot, _materials, accessory.Palette, 3);
        }
        ResetPose();
    }
    private static StandardMaterial3D RingPaint(string color) => new()
    { AlbedoColor = new(color), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
    public void ResetPose() { _hasPosition = _swinging = false; _sway = Vector2.Zero; _lastCooldown = 0; }
    public void LandAt(System.Numerics.Vector2 point) { _impactPoint = new(point.X, 0, point.Y); _visualPoint = _impactPoint; }
    public void Present(System.Numerics.Vector2 point, float radius, float windup, float cooldown, float delta, bool hasAim)
    {
        Vector3 aim = new(point.X, 0, point.Y);
        if (!_hasPosition) { _visualPoint = aim; _hasPosition = true; }
        bool swinging = windup > 0;
        if (swinging && !_swinging) { _swingFrom = _visualPoint; _windupDuration = Mathf.Max(windup + delta, .001f); }
        if (swinging) _impactPoint = aim;
        if (cooldown > _lastCooldown) _recoveryDuration = cooldown;
        Vector3 before = _visualPoint;
        if (swinging)
        {
            float travel = Mathf.Clamp((_windupDuration - windup) / (_windupDuration * .45f), 0, 1);
            _visualPoint = _swingFrom.Lerp(_impactPoint, 1 - Mathf.Pow(1 - travel, 3));
        }
        else if (hasAim) _visualPoint = _visualPoint.Lerp(aim, 1 - Mathf.Exp(-45 * delta));
        var velocity = (_visualPoint - before) / Mathf.Max(delta, .001f);
        var sway = new Vector2(Mathf.Clamp(velocity.X * .28f, -9, 9), Mathf.Clamp(velocity.Z * .22f, -6, 6));
        _sway = _sway.Lerp(sway, 1 - Mathf.Exp(-18 * delta));
        _time += delta;
        var pose = _motion.Pose(new(windup, _windupDuration, cooldown, _recoveryDuration, _time));
        _head.Position = _visualPoint + Vector3.Up * pose.Height;
        float motion = swinging ? 0 : 1;
        _head.RotationDegrees = new(_sway.Y * motion, pose.Yaw + _sway.X * motion, pose.Tilt - _sway.X * motion);
        _parts?.Animate(_time, swinging || cooldown > 0 ? 1 : .2f);
        if (_accessoryRoot != null) _accessoryRoot.Position = _visualPoint + new Vector3(1.1f, .45f, 1.0f);
        if (!Mathf.IsEqualApprox(radius, _lastRadius))
        {
            _lastRadius = radius;
            _ring.Mesh = new TorusMesh { InnerRadius = radius - .041f, OuterRadius = radius + .014f, Rings = 96, RingSegments = 8 };
        }
        _ring.Visible = swinging || hasAim;
        _ring.Position = (swinging ? _impactPoint : aim) + Vector3.Up * .035f;
        _ring.MaterialOverride = swinging ? _windup : cooldown > 0 ? _recovery : _idle;
        _cursor.Visible = hasAim; _cursor.Position = aim + Vector3.Up * .045f;
        _swinging = swinging; _lastCooldown = cooldown;
    }
    public override void _ExitTree() { _idle?.Dispose(); _windup?.Dispose(); _recovery?.Dispose(); }
}
