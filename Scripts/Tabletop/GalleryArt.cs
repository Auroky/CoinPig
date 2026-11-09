using Godot;

public sealed class GalleryArt : System.IDisposable
{
    private readonly TabletopMaterials _materials;
    public GalleryArt(TabletopMaterials materials) => _materials = materials;
    private MeshInstance3D Mesh(Node3D parent, Mesh mesh, Vector3 at, Color color) => SceneShapes.Mesh(parent, mesh, at, _materials.Paint(color));
    private MeshInstance3D Box(Node3D parent, Vector3 at, Vector3 size, Color color) => SceneShapes.Box(parent, at, size, _materials.Paint(color));
    private MeshInstance3D Cylinder(Node3D parent, Vector3 at, float bottom, float top, float height, Color color, int sides = 8) => SceneShapes.Cylinder(parent, at, bottom, top, height, _materials.Paint(color), sides);
    private MeshInstance3D Beam(Node3D parent, Vector3 a, Vector3 b, float width, Color color) => SceneShapes.Beam(parent, a, b, width, _materials.Paint(color));
    public void Dispose() => _font.Dispose();

    private readonly SystemFont _font = new()
    {
        FontNames = new[] { "Microsoft YaHei", "Noto Sans CJK SC", "sans-serif" },
        FontWeight = 600
    };

    public Label3D Label(Node3D parent, string text, Vector3 at, int size = 40, float scale = 0.01f)
    {
        var label = new Label3D
        {
            Text = text,
            Position = at,
            Font = _font,
            FontSize = size,
            PixelSize = scale,
            OutlineSize = 3,
            OutlineModulate = new("1b1917"),
            Modulate = new("f5dca6"),
            Shaded = false
        };
        parent.AddChild(label);
        return label;
    }

    public MeshInstance3D Wood(Node3D parent, Vector3 at, Vector3 size, Color color)
    {
        var plank = Box(parent, at, size, color);
        plank.MaterialOverride = _materials.Cabinet(color);
        return plank;
    }

    public void Cabinet(Node3D root, int columns, int tiers)
    {
        Color timber = new("65412a");
        for (int i = 0; i < 16; i++)
            Wood(root, new(-5.5f + i * 0.73f, 3.60f, -1.0f), new(0.72f, 6.8f, 0.14f), new(i % 2 == 0 ? "4b3022" : "523526"));
        for (int column = 0; column <= columns; column++)
            Wood(root, new(-5.9f + column * 11.8f / columns, 3.5f, 0), new(0.20f, 7.0f, 2.2f), timber);
        for (int tier = 0; tier <= tiers; tier++)
        {
            float y = tier == tiers ? 6.9f : 0.45f + tier * 6.3f / tiers;
            Wood(root, new(0, y, 0), new(12, 0.16f, 2.3f), timber);
            Wood(root, new(0, y - 0.10f, 1.12f), new(12.1f, 0.22f, 0.16f), new("815334"));
        }
        Wood(root, new(0, 7.10f, 0.16f), new(12.5f, 0.26f, 2.5f), new("68442c"));
        foreach (float x in new[] { -5.9f, 5.9f })
            for (int i = 0; i < 3; i++)
                Mesh(root, new SphereMesh { Radius = 0.052f, Height = 0.104f }, new(x, 0.7f + i * 2.1f, 1.12f), new("d0a46a"));
    }

    public void Hammer(Node3D parent, int tier)
    {
        var tool = new Node3D { Position = new(0, 0.1f, 0.12f), RotationDegrees = new(-10, 7, -9 + tier * 5) };
        parent.AddChild(tool);
        var metal = new Color(tier == 0 ? "86918c" : tier == 1 ? "516a6b" : "3b4246");
        float width = 0.72f + tier * 0.14f;
        Box(tool, new(-0.85f, 0.18f, 0), new(width, 0.84f, 0.64f), metal);
        foreach (float side in new[] { -1f, 1f })
            Box(tool, new(-0.85f, 0.18f + side * 0.38f, 0), new(width + 0.10f, 0.12f, 0.70f), new("b5b6a0"));
        Box(tool, new(-0.85f, 0.18f, 0.33f), new(0.20f, 0.62f, 0.045f), new("ce9c54"));
        Beam(tool, new(-0.55f, 0.18f, 0), new(1.40f, 0.18f, 0), 0.19f, new("b6824e"));
        Beam(tool, new(0.57f, 0.18f, 0), new(1.40f, 0.18f, 0), 0.25f, new(tier == 1 ? "8d4034" : "3a3633"));
        for (int i = 0; i < 5; i++)
            Box(tool, new(0.65f + i * 0.15f, 0.18f, 0), new(0.026f, 0.26f, 0.26f), new("bc9566"));
        Box(parent, new(-0.85f, -0.36f, 0.04f), new(0.54f, 0.13f, 0.78f), new("302923"));
    }

    public void Cuff(Node3D parent, int tier)
    {
        var cuff = new Node3D { Position = new(0, 0.15f, 0.2f), RotationDegrees = new(-16, 18, 5) };
        parent.AddChild(cuff);
        var leather = new Color(tier == 0 ? "754b32" : tier == 1 ? "3e555b" : "3f3832");
        Mesh(cuff, LeatherTube(), Vector3.Zero, leather);
        foreach (float z in new[] { -0.33f, 0.33f })
        {
            var trim = Mesh(cuff, new TorusMesh { InnerRadius = 0.47f, OuterRadius = 0.59f, Rings = 32, RingSegments = 8 },
                new(0, 0, z), new("bb985e"));
            trim.RotationDegrees = new(90, 0, 0);
        }
        var buckle = new Node3D { Position = new(0.27f, -0.31f, 0.37f), RotationDegrees = new(0, 0, -30) };
        cuff.AddChild(buckle);
        Box(buckle, Vector3.Zero, new(0.40f, 0.36f, 0.09f), new("d1b272"));
        Box(buckle, new(0, 0, 0.05f), new(0.26f, 0.22f, 0.02f), new("594532"));
        Box(buckle, new(0, 0, 0.07f), new(0.05f, 0.25f, 0.025f), new("d1b272"));
        Box(parent, new(0, -0.47f, 0), new(1.55f, 0.14f, 1.12f), new("352d25"));
    }

    private ArrayMesh LeatherTube()
    {
        using var surface = new SurfaceTool();
        surface.Begin(Godot.Mesh.PrimitiveType.Triangles);
        const int segments = 32;
        const float outer = 0.58f, inner = 0.42f, depth = 0.33f;
        Vector3 Point(float angle, float radius, float z) => new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, z);
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            foreach (var point in new[] { a, b, c, a, c, d })
            {
                surface.SetNormal(normal);
                surface.AddVertex(point);
            }
        }
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.Tau / segments, b = (i + 1) * Mathf.Tau / segments;
            Vector3 normal = Point((a + b) / 2, 1, 0);
            Quad(Point(a, outer, -depth), Point(b, outer, -depth), Point(b, outer, depth), Point(a, outer, depth), normal);
            Quad(Point(a, inner, depth), Point(b, inner, depth), Point(b, inner, -depth), Point(a, inner, -depth), -normal);
            Quad(Point(a, inner, depth), Point(a, outer, depth), Point(b, outer, depth), Point(b, inner, depth), Vector3.Back);
            Quad(Point(a, outer, -depth), Point(a, inner, -depth), Point(b, inner, -depth), Point(b, outer, -depth), Vector3.Forward);
        }
        return surface.Commit();
    }

    public void SkillIcon(Node3D parent, string icon, Color color)
    {
        if (icon == "hammer")
        {
            Beam(parent, new(-0.20f, -0.23f, 0), new(0.16f, 0.19f, 0), 0.10f, color);
            var head = Box(parent, new(0.15f, 0.20f, 0), new(0.45f, 0.18f, 0.11f), color);
            head.RotationDegrees = new(0, 0, -40);
        }
        else if (icon == "cuff")
        {
            Box(parent, Vector3.Zero, new(0.15f, 0.50f, 0.10f), color);
            Box(parent, Vector3.Zero, new(0.49f, 0.15f, 0.10f), color);
        }
        else
        {
            var coin = Cylinder(parent, Vector3.Zero, 0.26f, 0.26f, 0.11f, color, 24);
            coin.RotationDegrees = new(90, 0, 0);
            Box(parent, new(0, 0, 0.075f), new(0.08f, 0.31f, 0.02f), new("665032"));
        }
    }
}
