using Godot;

/// <summary>Interpolated mallet poses; timing and the locked impact point come from the coordinator.</summary>
public partial class HammerModel : Node3D
{
    private Node3D _head = null!, _cursor = null!;
    private MeshInstance3D _ring = null!;
    private StandardMaterial3D _idle = null!, _windup = null!, _recovery = null!;
    private Vector3 _visualPoint, _swingFrom, _impactPoint;
    private Vector2 _sway;
    private bool _hasPosition, _swinging;
    private float _lastRadius, _lastCooldown, _recoveryDuration = .39f;

    public void Assemble(TabletopMaterials materials)
    {
        _idle = RingPaint("f8e7bf");
        _windup = RingPaint("ffb358");
        _recovery = RingPaint("968974");
        _ring = SceneShapes.Mesh(this, new TorusMesh(), Vector3.Zero, _idle);
        _ring.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        _cursor = new Node3D();
        AddChild(_cursor);
        foreach (var size in new[] { new Vector3(.17f, .006f, .025f), new(.025f, .006f, .17f) })
            SceneShapes.Box(_cursor, Vector3.Zero, size, _idle).CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        _head = new Node3D();
        AddChild(_head);
        SceneShapes.Box(_head, Vector3.Zero, new(.99f, .46f, .48f), materials.Paint("445456"));
        foreach (float x in new[] { -.51f, .51f })
            SceneShapes.Box(_head, new(x, 0, 0), new(.11f, .51f, .52f), materials.Paint("8a9995"));
        SceneShapes.Box(_head, Vector3.Zero, new(.16f, .48f, .49f), materials.Paint("c89b5c"));
        Vector3 end = new(1.08f, .63f, 1.13f);
        SceneShapes.Beam(_head, new(0, 0, .12f), end, .175f, materials.Paint("ae7843"));
        SceneShapes.Beam(_head, end * .64f, end, .215f, materials.Paint("4c3832"));
        for (int i = 0; i < 3; i++)
            SceneShapes.Beam(_head, end * (.68f + i * .12f), end * (.707f + i * .12f), .22f, materials.Paint("b28b60"));
    }

    private static StandardMaterial3D RingPaint(string color) => new()
    {
        AlbedoColor = new(color), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
    };

    public void ResetPose()
    {
        _hasPosition = _swinging = false;
        _sway = Vector2.Zero;
        _lastCooldown = 0;
    }

    public void LandAt(System.Numerics.Vector2 point)
    {
        _impactPoint = new(point.X, 0, point.Y);
        _visualPoint = _impactPoint;
    }

    public void Present(System.Numerics.Vector2 point, float radius, float windup, float cooldown, float delta, bool hasAim)
    {
        Vector3 aim = new(point.X, 0, point.Y);
        if (!_hasPosition) { _visualPoint = aim; _hasPosition = true; }
        bool swinging = windup > 0;
        if (swinging && !_swinging) { _swingFrom = _visualPoint; _impactPoint = aim; }
        if (cooldown > _lastCooldown) _recoveryDuration = cooldown;
        float recoveryProgress = 1 - Mathf.Clamp(cooldown / _recoveryDuration, 0, 1);
        Vector3 before = _visualPoint;
        if (swinging)
        {
            float travel = Mathf.Clamp((.19f - windup) / .085f, 0, 1);
            _visualPoint = _swingFrom.Lerp(_impactPoint, 1 - Mathf.Pow(1 - travel, 3));
        }
        else if (cooldown > 0 && recoveryProgress < .1f) _visualPoint = _impactPoint;
        else if (hasAim) _visualPoint = _visualPoint.Lerp(aim, 1 - Mathf.Exp(-45 * delta));
        var velocity = (_visualPoint - before) / Mathf.Max(delta, .001f);
        var sway = new Vector2(Mathf.Clamp(velocity.X * .28f, -9, 9), Mathf.Clamp(velocity.Z * .22f, -6, 6));
        _sway = _sway.Lerp(sway, 1 - Mathf.Exp(-18 * delta));

        float height = .78f, tilt = -9;
        if (swinging)
        {
            float p = Mathf.Clamp(1 - windup / .19f, 0, .99f);
            if (p < .64f)
            {
                float lift = 1 - Mathf.Pow(1 - p / .64f, 3);
                height = Mathf.Lerp(.78f, 1.78f, lift);
                tilt = Mathf.Lerp(-9, -64, lift);
            }
            else
            {
                float drop = Mathf.Pow((p - .64f) / .36f, 3);
                height = Mathf.Lerp(1.78f, .235f, drop);
                tilt = Mathf.Lerp(-64, 6, drop);
            }
        }
        else if (cooldown > 0)
        {
            float p = recoveryProgress;
            if (p < .1f) { height = .235f; tilt = 6; }
            else if (p < .4f)
            {
                float bounce = 1 - Mathf.Pow(1 - (p - .1f) / .3f, 3);
                height = Mathf.Lerp(.235f, 1.02f, bounce);
                tilt = Mathf.Lerp(6, -17, bounce);
            }
            else
            {
                float settle = Mathf.SmoothStep(0, 1, (p - .4f) / .6f);
                height = Mathf.Lerp(1.02f, .78f, settle);
                tilt = Mathf.Lerp(-17, -9, settle);
            }
        }
        _head.Position = _visualPoint + Vector3.Up * height;
        float motion = swinging ? 0 : 1;
        _head.RotationDegrees = new(_sway.Y * motion, 24 + _sway.X * motion, tilt - _sway.X * motion);
        if (!Mathf.IsEqualApprox(radius, _lastRadius))
        {
            _lastRadius = radius;
            _ring.Mesh = new TorusMesh { InnerRadius = radius - .041f, OuterRadius = radius + .014f, Rings = 96, RingSegments = 8 };
        }
        bool locked = swinging || (cooldown > 0 && recoveryProgress < .1f);
        _ring.Visible = locked || hasAim;
        _ring.Position = (locked ? _impactPoint : aim) + Vector3.Up * .035f;
        _ring.MaterialOverride = swinging ? _windup : cooldown > 0 ? _recovery : _idle;
        _cursor.Visible = hasAim;
        _cursor.Position = aim + Vector3.Up * .045f;
        _swinging = swinging;
        _lastCooldown = cooldown;
    }

    public override void _ExitTree()
    {
        _idle?.Dispose(); _windup?.Dispose(); _recovery?.Dispose();
    }
}
