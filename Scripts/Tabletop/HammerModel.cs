using Godot;

public partial class HammerModel : Node3D
{
    private Node3D _head = null!;
    private MeshInstance3D _ring = null!;
    private StandardMaterial3D _ringPaint = null!;
    private float _lastRadius;

    public void Assemble(TabletopMaterials materials)
    {
        _ringPaint = new StandardMaterial3D
        {
            AlbedoColor = new("e7d29c"), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha, NoDepthTest = false
        };
        _ring = SceneShapes.Mesh(this, new TorusMesh(), new(0, .025f, 0), _ringPaint);
        _ring.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        _head = new Node3D();
        AddChild(_head);
        var iron = materials.Paint("34443f", .35f, .65f);
        var steel = materials.Paint("a2aca0", .28f, .8f);
        var brass = materials.Paint("c2a062", .3f, .7f);
        SceneShapes.Box(_head, Vector3.Zero, new(1.05f, .42f, .52f), iron);
        SceneShapes.Box(_head, new(-.53f, 0, 0), new(.09f, .47f, .57f), steel);
        SceneShapes.Box(_head, new(.53f, 0, 0), new(.09f, .47f, .57f), steel);
        SceneShapes.Box(_head, Vector3.Zero, new(.15f, .45f, .54f), brass);
        var end = new Vector3(.55f, .75f, 1.45f);
        SceneShapes.Rod(_head, new(0, 0, .12f), end, .095f, materials.Paint("a47747", .6f));
        SceneShapes.Rod(_head, end * .65f, end, .12f, materials.Paint("3c3026", .8f));
    }

    public void Present(System.Numerics.Vector2 point, float radius, float windup, float cooldown)
    {
        Position = new(point.X, 0, point.Y);
        if (!Mathf.IsEqualApprox(radius, _lastRadius))
        {
            _lastRadius = radius;
            _ring.Mesh = new TorusMesh { InnerRadius = radius - .015f, OuterRadius = radius + .015f, Rings = 96, RingSegments = 8 };
        }
        _ringPaint.AlbedoColor = windup > 0 ? new("edc783") : cooldown > 0 ? new Color(.8f, .75f, .6f, .35f) : new Color(.9f, .84f, .65f, .75f);
        float lift = windup > 0 ? .25f + 1.1f * windup / .19f : .25f + .55f * (1 - Mathf.Clamp(cooldown / .39f, 0, 1));
        _head.Position = new(0, lift, 0);
        _head.Rotation = new(-windup * 2.3f, -.2f, windup * -.7f);
    }

    public override void _ExitTree() => _ringPaint?.Dispose();
}
