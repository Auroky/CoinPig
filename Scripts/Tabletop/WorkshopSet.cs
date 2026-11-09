using Godot;

/// <summary>Builds the desk, lighting and camera; it has no game-state responsibilities.</summary>
public static class WorkshopSet
{
    public static Camera3D Build(Node3D root, TabletopMaterials materials)
    {
        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new("171915"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new("d7d8ca"),
            AmbientLightEnergy = .28f,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
            TonemapMode = Godot.Environment.ToneMapper.Linear
        };
        root.AddChild(new WorldEnvironment { Environment = environment });
        root.AddChild(new DirectionalLight3D
        {
            RotationDegrees = new(-58, -32, 0), LightColor = new("ffe7ce"), LightEnergy = .48f,
            ShadowEnabled = true, ShadowOpacity = .75f, DirectionalShadowMaxDistance = 32,
            ShadowBias = .03f, ShadowNormalBias = .6f
        });
        root.AddChild(new OmniLight3D
        {
            Position = new(6, 6, 1), LightColor = new("b2c4cc"), LightEnergy = .18f,
            OmniRange = 18, ShadowEnabled = false
        });
        var camera = new Camera3D
        {
            Name = "DeskCamera", Current = true,
            Projection = Camera3D.ProjectionType.Orthogonal,
            KeepAspect = Camera3D.KeepAspectEnum.Width,
            Size = 18.6f, Position = new(0, 13.5f, 11.5f), Near = .1f, Far = 60
        };
        root.AddChild(camera);
        camera.LookAt(new(0, 0, .15f), Vector3.Up);

        var darkWood = materials.Paint("39291e", .8f);
        var iron = materials.Paint("333b32", .46f, .65f);
        var brass = materials.Paint("b5975b", .34f, .7f);
        SceneShapes.Box(root, new(0, -.30f, 0), new(20, .55f, 13.0f), darkWood);
        string[] tones = { "644730", "5e422e", "62452e", "684a32", "60422c", "63452f", "5f432e", "654831" };
        for (int i = 0; i < 8; i++)
            SceneShapes.Box(root, new(0, -.045f, (i - 3.5f) * 1.6f), new(19.8f, .09f, 1.592f), materials.Wood(tones[i], i * 7.13f));
        foreach (float x in new[] { -9.3f, 9.3f })
        {
            SceneShapes.Box(root, new(x, .015f, 0), new(.16f, .03f, 12.85f), iron);
            for (int i = 0; i < 6; i++) SceneShapes.Cylinder(root, new(x, .038f, -5.7f + i * 2.28f), .045f, .045f, .025f, brass, 12);
        }
        // The measuring strip makes the surface read as a physical workbench.
        SceneShapes.Box(root, new(0, .016f, 4.6f), new(15, .025f, .30f), materials.Paint("9a7b4d", .55f, .4f));
        for (int i = 0; i <= 60; i++)
            SceneShapes.Box(root, new(-7.3f + i * .243f, .031f, 4.56f), new(.012f, .004f, i % 5 == 0 ? .20f : .10f), darkWood);
        Lamp(root, materials, new(-7.65f, 0, -4.45f));
        Mug(root, materials, new(7.45f, 0, -4.15f));
        Ledger(root, materials, new(7.55f, 0, 3.5f));
        Pencil(root, materials, new(-7.6f, .06f, 3.7f));
        return camera;
    }

    private static void Lamp(Node3D parent, TabletopMaterials materials, Vector3 position)
    {
        var lamp = new Node3D { Position = position, RotationDegrees = new(0, -12, 0) };
        parent.AddChild(lamp);
        var green = materials.Paint("294b3e", .33f, .25f);
        var brass = materials.Paint("ad8850", .35f, .65f);
        SceneShapes.Cylinder(lamp, new(0, .12f, 0), .63f, .53f, .23f, green, 40);
        SceneShapes.Rod(lamp, new(0, .2f, -.1f), new(0, 1.45f, -.15f), .055f, brass);
        SceneShapes.Rod(lamp, new(0, 1.45f, -.15f), new(0, 1.65f, .55f), .06f, brass);
        SceneShapes.Ellipsoid(lamp, new(0, 1.64f, .58f), new(.9f, .28f, .4f), green);
        SceneShapes.Ellipsoid(lamp, new(0, 1.49f, .69f), new(.75f, .045f, .24f), materials.Paint("f3d99c", .6f));
        var bulb = new OmniLight3D { Position = new(.3f, 1.3f, 1), LightColor = new("ffd8a0"), LightEnergy = .65f, OmniRange = 7 };
        lamp.AddChild(bulb);
    }

    private static void Mug(Node3D parent, TabletopMaterials materials, Vector3 position)
    {
        var mug = new Node3D { Position = position, RotationDegrees = new(0, 28, 0) };
        parent.AddChild(mug);
        var glaze = materials.Paint("c6c4ad", .3f);
        SceneShapes.Cylinder(mug, new(0, .04f, 0), .62f, .66f, .08f, glaze, 40);
        SceneShapes.Cylinder(mug, new(0, .37f, 0), .35f, .43f, .65f, glaze, 40);
        SceneShapes.Cylinder(mug, new(0, .704f, 0), .37f, .37f, .013f, materials.Paint("37281c", .22f), 40);
        var handle = SceneShapes.Mesh(mug, new TorusMesh { InnerRadius = .12f, OuterRadius = .23f, Rings = 24, RingSegments = 12 }, new(.43f, .42f, 0), glaze);
        handle.RotationDegrees = new(90, 0, 0);
    }

    private static void Ledger(Node3D parent, TabletopMaterials materials, Vector3 position)
    {
        var ledger = new Node3D { Position = position, RotationDegrees = new(0, -12, 0) };
        parent.AddChild(ledger);
        var paper = materials.Paint("cdbd99", .95f);
        SceneShapes.Box(ledger, new(0, .06f, 0), new(1.8f, .1f, 2.2f), materials.Paint("34443a", .8f));
        SceneShapes.Box(ledger, new(0, .12f, 0), new(1.65f, .025f, 2.04f), paper);
        for (int i = 0; i < 7; i++)
            SceneShapes.Box(ledger, new(0, .135f, -.7f + i * .22f), new(1.3f, .004f, .015f), materials.Paint("92886b", .95f));
    }

    private static void Pencil(Node3D parent, TabletopMaterials materials, Vector3 position)
    {
        var pencil = new Node3D { Position = position, RotationDegrees = new(0, -20, 90) };
        parent.AddChild(pencil);
        SceneShapes.Cylinder(pencil, Vector3.Zero, .055f, .055f, 1.8f, materials.Paint("bd8f46", .7f), 6);
        SceneShapes.Cylinder(pencil, new(0, .99f, 0), .055f, 0, .18f, materials.Paint("d6ba80", .9f), 6);
        SceneShapes.Cylinder(pencil, new(0, 1.075f, 0), .018f, 0, .06f, materials.Paint("292820", .8f), 6);
    }
}
