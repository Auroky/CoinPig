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
