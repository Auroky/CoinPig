using Godot;

public sealed class WorkshopSet
{
    private readonly TabletopMaterials _materials;
    private WorkshopSet(TabletopMaterials materials) => _materials = materials;
    public static Camera3D Build(Node3D root, TabletopMaterials materials) => new WorkshopSet(materials).BuildDesk(root);
    private MeshInstance3D Mesh(Node3D parent, Mesh mesh, Vector3 at, Color color) => SceneShapes.Mesh(parent, mesh, at, _materials.Paint(color));
    private MeshInstance3D Box(Node3D parent, Vector3 at, Vector3 size, Color color) => SceneShapes.Box(parent, at, size, _materials.Paint(color));
    private MeshInstance3D Cylinder(Node3D parent, Vector3 at, float bottom, float top, float height, Color color, int sides = 8) => SceneShapes.Cylinder(parent, at, bottom, top, height, _materials.Paint(color), sides);
    private MeshInstance3D Beam(Node3D parent, Vector3 a, Vector3 b, float width, Color color)
    {
        var beam = Box(parent, (a + b) / 2, new(width, a.DistanceTo(b), width), color);
        beam.Quaternion = new Quaternion(Vector3.Up, (b - a).Normalized());
        return beam;
    }

    private MeshInstance3D Oval(Node3D parent, Vector3 at, Vector3 radii, Color tint)
    {
        var shape = Mesh(parent, new SphereMesh { Radius = 1, Height = 2, RadialSegments = 24, Rings = 14 }, at, tint);
        shape.Scale = radii;
        return shape;
    }

    private Camera3D BuildDesk(Node3D root)
    {
        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color("302a28"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new("e2d9d7"),
            AmbientLightEnergy = 0.34f,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled
        };
        root.AddChild(new WorldEnvironment { Environment = environment });
        root.AddChild(new DirectionalLight3D
        {
            RotationDegrees = new(-47, -38, 0),
            LightColor = new("ffe4ce"),
            LightEnergy = 0.47f,
            ShadowEnabled = true,
            ShadowOpacity = 0.62f,
            DirectionalShadowMaxDistance = 38
        });
        var camera = new Camera3D
        {
            Name = "DeskCamera",
            Current = true,
            Projection = Camera3D.ProjectionType.Orthogonal,
            KeepAspect = Camera3D.KeepAspectEnum.Width,
            Size = 18,
            Position = new(0, 15.5f, 9.5f)
        };
        root.AddChild(camera);
        camera.LookAt(Vector3.Zero, Vector3.Up);
        Box(root, new(0, -0.27f, 0), new(20.2f, 0.42f, 13.2f), new("3d291f"));
        string[] palette = { "785135", "795137", "80573b", "765038", "7d543a", "785137", "7c5439", "79513a" };
        for (int i = 0; i < 8; i++)
        {
            var plank = Box(root, new(0, -0.065f, (i - 3.5f) * 1.57f), new(20, 0.13f, 1.563f), Colors.White);
            plank.MaterialOverride = _materials.Wood(palette[i], i * 8.31f);
        }
        // Desk-edge props establish scale, leaving the running lanes open.
        Lamp(root, new(-7.66f, 0, -4.92f));
        Coffee(root, new(7.62f, 0, -4.88f));
        Papers(root, new(7.76f, 0, 4.82f));
        Pencil(root, new(-7.49f, 0, 4.55f));
        return camera;
    }

    private void Lamp(Node3D desk, Vector3 pos)
    {
        var lamp = new Node3D { Position = pos, RotationDegrees = new(0, 12, 0) };
        desk.AddChild(lamp);
        var brass = new Color("bf9153");
        var enamel = new Color("255652");
        Cylinder(lamp, new(0, 0.09f, 0), 0.62f, 0.66f, 0.17f, new("363c36"), 36);
        Cylinder(lamp, new(0, 0.21f, 0), 0.63f, 0.50f, 0.13f, enamel, 36);
        Cylinder(lamp, new(0, 0.86f, -0.10f), 0.085f, 0.07f, 1.27f, brass, 16);
        Beam(lamp, new(0, 1.42f, -0.1f), new(0, 1.67f, 0.39f), 0.11f, brass);
        var shade = Oval(lamp, new(0, 1.63f, 0.43f), new(1.19f, 0.38f, 0.43f), enamel);
        shade.RotationDegrees = new(-12, 0, 0);
        Oval(lamp, new(0, 1.49f, 0.68f), new(1.02f, 0.10f, 0.22f), new("edd6a1"));
    }

    private void Coffee(Node3D desk, Vector3 pos)
    {
        var cup = new Node3D { Position = pos };
        desk.AddChild(cup);
        Cylinder(cup, new(0, 0.06f, 0), 0.70f, 0.77f, 0.10f, new("a9b8aa"), 40);
        Cylinder(cup, new(0, 0.37f, 0), 0.37f, 0.50f, 0.61f, new("c6d3c5"), 40);
        Cylinder(cup, new(0, 0.681f, 0), 0.445f, 0.445f, 0.015f, new("442f24"), 40);
        var handle = Mesh(cup, new TorusMesh { InnerRadius = 0.13f, OuterRadius = 0.239f, Rings = 20, RingSegments = 8 }, new(0.50f, 0.44f, 0.08f), new("c6d3c5"));
        handle.RotationDegrees = new(38, -16, 0);
    }

    private void Papers(Node3D desk, Vector3 pos)
    {
        var stack = new Node3D { Position = pos, RotationDegrees = new(0, -17, 0) };
        desk.AddChild(stack);
        Box(stack, new(0, 0.025f, 0), new(2.27f, 0.04f, 1.8f), new("b2a386")).RotationDegrees = new(0, 6, 0);
        Box(stack, new(0, 0.055f, 0), new(2.23f, 0.035f, 1.8f), new("e2cfaa"));
        Box(stack, new(-0.39f, 0.075f, -0.48f), new(0.98f, 0.005f, 0.11f), new("889388"));
        for (int i = 0; i < 4; i++)
            Box(stack, new(0, 0.076f, -0.19f + i * 0.18f), new(1.72f, 0.005f, 0.018f), new("b1aa90"));
    }
    private void Pencil(Node3D desk, Vector3 pos)
    {
        var pen = new Node3D { Position = pos + new Vector3(0, 0.07f, 0), RotationDegrees = new(0, 25, 82) };
        desk.AddChild(pen);
        Cylinder(pen, Vector3.Zero, 0.077f, 0.077f, 2.02f, new("d4974d"), 6);
        Cylinder(pen, new(0, 1.12f, 0), 0.077f, 0.012f, 0.23f, new("d1af7d"), 6);
        Cylinder(pen, new(0, 1.255f, 0), 0.025f, 0.002f, 0.05f, new("333c3a"), 6);
        Cylinder(pen, new(0, -1.03f, 0), 0.08f, 0.08f, 0.15f, new("c6bb9b"), 12);
        Cylinder(pen, new(0, -1.16f, 0), 0.074f, 0.07f, 0.12f, new("bd7771"), 12);
    }
}
