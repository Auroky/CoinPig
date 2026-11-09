using Godot;
using System;
using System.Collections.Generic;

public sealed class EquipmentModelParts
{
    public Node3D Root { get; }
    private readonly Action<float, float>? _animate;
    public EquipmentModelParts(Node3D root, Action<float, float>? animate = null) { Root = root; _animate = animate; }
    public void Animate(float time, float activity) => _animate?.Invoke(time, activity);
}
public interface IEquipmentModelBuilder
{
    EquipmentModelParts Build(Node3D parent, TabletopMaterials materials, EquipmentPalette palette, int level);
}
public sealed class EquipmentModelRegistry
{
    private readonly Dictionary<string, IEquipmentModelBuilder> _builders = new();
    public EquipmentModelRegistry Register(string key, IEquipmentModelBuilder builder) { _builders.Add(key, builder); return this; }
    public EquipmentModelParts Build(string key, Node3D parent, TabletopMaterials materials, EquipmentPalette palette, int level) =>
        (_builders.TryGetValue(key, out var builder) ? builder : _builders["hammer"]).Build(parent, materials, palette, level);
    public static EquipmentModelRegistry CreateDefault() => new EquipmentModelRegistry()
        .Register("hammer", new HammerEquipmentBuilder()).Register("cuff", new CuffEquipmentBuilder())
        .Register("chainsaw", new ChainsawEquipmentBuilder()).Register("grinder", new GrinderEquipmentBuilder());
}
internal sealed class EquipmentShapes
{
    public Node3D Root { get; } = new();
    private readonly TabletopMaterials _materials;
    public EquipmentShapes(Node3D parent, TabletopMaterials materials) { parent.AddChild(Root); _materials = materials; }
    public MeshInstance3D Mesh(Node3D parent, Mesh mesh, Vector3 at, string color) => SceneShapes.Mesh(parent, mesh, at, _materials.Paint(color, .43f, .28f));
    public MeshInstance3D Box(Vector3 at, Vector3 size, string color, Node3D? parent = null) =>
        Mesh(parent ?? Root, new BoxMesh { Size = size }, at, color);
    public MeshInstance3D Oval(Vector3 at, Vector3 radius, string color, Node3D? parent = null) =>
        SceneShapes.Ellipsoid(parent ?? Root, at, radius, _materials.Paint(color, .4f, .2f));
    public MeshInstance3D Beam(Vector3 a, Vector3 b, float width, string color, Node3D? parent = null) =>
        SceneShapes.Beam(parent ?? Root, a, b, width, _materials.Paint(color, .48f, .12f));
    public MeshInstance3D Ring(Vector3 at, float inner, float outer, string color, Node3D? parent = null)
    {
        var ring = Mesh(parent ?? Root, new TorusMesh { InnerRadius = inner, OuterRadius = outer, Rings = 40, RingSegments = 8 }, at, color);
        ring.RotationDegrees = new(90, 0, 0); return ring;
    }
    public MeshInstance3D Disc(Node3D parent, Vector3 at, float radius, float height, string color)
    {
        var disc = Mesh(parent, new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = 40 }, at, color);
        disc.RotationDegrees = new(90, 0, 0); return disc;
    }
}
public sealed class HammerEquipmentBuilder : IEquipmentModelBuilder
{
    public EquipmentModelParts Build(Node3D parent, TabletopMaterials materials, EquipmentPalette p, int level)
    {
        var s = new EquipmentShapes(parent, materials);
        float width = .99f + level * .065f;
        s.Box(Vector3.Zero, new(width, .46f, .48f), p.Body);
        foreach (float side in new[] { -1f, 1f })
            s.Box(new(side * width / 2, 0, 0), new(.11f, .51f, .52f), p.Metal);
        s.Box(new(0, 0, .255f), new(.17f, .46f, .035f), p.Trim);
        Vector3 end = new(1.08f, .63f, 1.13f);
        s.Beam(new(0, 0, .12f), end, .175f, p.Body);
        s.Beam(end * .64f, end, .215f, p.Grip);
        for (int i = 0; i < 3 + level; i++)
            s.Beam(end * (.64f + i * .32f / (3 + level)), end * (.66f + i * .32f / (3 + level)), .22f, p.Trim);
        for (int i = 0; i < level; i++) s.Oval(new(-.24f + i * .18f, .04f, .27f), new(.035f, .08f, .025f), p.Glow);
        return new(s.Root);
    }
}
public sealed class CuffEquipmentBuilder : IEquipmentModelBuilder
{
    public EquipmentModelParts Build(Node3D parent, TabletopMaterials materials, EquipmentPalette p, int level)
    {
        var s = new EquipmentShapes(parent, materials);
        s.Ring(Vector3.Zero, .46f, .74f, p.Body);
        s.Ring(new(0, 0, -.15f), .48f, .73f, p.Grip);
        s.Ring(new(0, 0, .16f), .55f, .76f, p.Trim);
        s.Oval(new(0, 0, .21f), new(.19f, .19f, .09f), p.Glow);
        for (int i = 0; i < Math.Max(1, level); i++)
            s.Ring(new(0, 0, .225f + i * .008f), .25f + i * .085f, .27f + i * .085f, p.Trim);
        s.Box(new(.55f, -.2f, .25f), new(.32f, .35f, .12f), p.Metal);
        s.Box(new(.55f, -.2f, .32f), new(.17f, .22f, .035f), p.Grip);
        for (int i = 0; i < level + 2; i++)
        {
            float angle = .55f + i * .5f;
            s.Oval(new(Mathf.Cos(angle) * .64f, Mathf.Sin(angle) * .64f, .17f), new(.045f, .045f, .04f), p.Glow);
        }
        return new(s.Root);
    }
}
public sealed class ChainsawEquipmentBuilder : IEquipmentModelBuilder
{
    public EquipmentModelParts Build(Node3D parent, TabletopMaterials materials, EquipmentPalette p, int level)
    {
        var s = new EquipmentShapes(parent, materials);
        s.Box(new(-.52f, .02f, 0), new(1.04f, .65f, .56f), p.Body);
        s.Box(new(-.52f, .04f, .3f), new(.78f, .40f, .06f), p.Trim);
        for (int i = 0; i < 5; i++) s.Box(new(-.84f + i * .14f, -.1f, .35f), new(.05f, .28f, .025f), p.Grip);
        s.Beam(new(-1, -.15f, 0), new(-1.22f, .45f, 0), .13f, p.Grip);
        s.Beam(new(-1.22f, .45f, 0), new(-.55f, .58f, 0), .13f, p.Grip);
        s.Oval(new(.59f, 0, .03f), new(.97f, .23f, .11f), p.Metal);
        s.Box(new(.55f, 0, .16f), new(1.05f, .09f, .025f), p.Trim);
        var teeth = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new BoxMesh { Size = new(.075f, .095f, .16f) }, InstanceCount = 32 };
        s.Root.AddChild(new MultiMeshInstance3D { Multimesh = teeth, MaterialOverride = materials.Paint(p.Grip, .43f, .28f) });
        return new(s.Root, (time, activity) =>
        {
            for (int i = 0; i < teeth.InstanceCount; i++)
            {
                float angle = i * Mathf.Tau / teeth.InstanceCount + time * (2 + activity * 9);
                teeth.SetInstanceTransform(i, new(new Basis(Vector3.Back, angle), new(.59f + Mathf.Cos(angle) * .98f, Mathf.Sin(angle) * .26f, .04f)));
            }
        });
    }
}
public sealed class GrinderEquipmentBuilder : IEquipmentModelBuilder
{
    public EquipmentModelParts Build(Node3D parent, TabletopMaterials materials, EquipmentPalette p, int level)
    {
        var s = new EquipmentShapes(parent, materials);
        s.Box(new(-.58f, .10f, -.08f), new(1.2f, .48f, .51f), p.Body);
        s.Box(new(-1.1f, .1f, 0), new(.54f, .40f, .45f), p.Grip);
        for (int i = 0; i < 4; i++) s.Box(new(-1.28f + i * .12f, .11f, .235f), new(.035f, .28f, .025f), p.Trim);
        s.Beam(new(-.05f, .18f, -.05f), new(-.05f, .93f, -.05f), .18f, p.Grip);
        var wheel = new Node3D { Position = new(.52f, 0, .22f) }; s.Root.AddChild(wheel);
        s.Disc(wheel, Vector3.Zero, .70f + level * .025f, .12f, p.Metal);
        s.Disc(wheel, new(0, 0, .07f), .58f, .025f, p.Grip);
        s.Ring(new(0, 0, .09f), .61f, .67f, p.Trim, wheel);
        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.Tau / 10;
            s.Beam(new(Mathf.Cos(a) * .25f, Mathf.Sin(a) * .25f, .1f),
                new(Mathf.Cos(a + .2f) * .55f, Mathf.Sin(a + .2f) * .55f, .1f), .025f, p.Body, wheel);
        }
        s.Disc(wheel, new(0, 0, .12f), .16f, .045f, p.Glow);
        for (int i = 0; i < 10; i++)
        {
            float a = .15f + i * Mathf.Pi / 10;
            var guard = s.Box(new(.52f + Mathf.Cos(a) * .76f, Mathf.Sin(a) * .76f, .13f), new(.20f, .12f, .25f), p.Body);
            guard.Rotation = new(0, 0, a + Mathf.Pi / 2);
        }
        return new(s.Root, (time, activity) => wheel.Rotation = new(0, 0, time * (5 + activity * 24)));
    }
}

public readonly record struct WeaponPose(float Height, float Tilt, float Yaw);
public readonly record struct WeaponMotionFrame(float Windup, float WindupDuration, float Cooldown, float RecoveryDuration, float Time);
public interface IWeaponMotion { WeaponPose Pose(WeaponMotionFrame frame); }
public sealed class SwingWeaponMotion : IWeaponMotion
{
    public WeaponPose Pose(WeaponMotionFrame f)
    {
        float height = .78f, tilt = -9;
        if (f.Windup > 0)
        {
            float p = Mathf.Clamp(1 - f.Windup / f.WindupDuration, 0, .99f);
            if (p < .64f) { float t = 1 - Mathf.Pow(1 - p / .64f, 3); height = Mathf.Lerp(.78f, 1.78f, t); tilt = Mathf.Lerp(-9, -64, t); }
            else { float t = Mathf.Pow((p - .64f) / .36f, 3); height = Mathf.Lerp(1.78f, .235f, t); tilt = Mathf.Lerp(-64, 6, t); }
        }
        else if (f.Cooldown > 0)
        {
            float p = 1 - Mathf.Clamp(f.Cooldown / f.RecoveryDuration, 0, 1);
            if (p < .1f) { height = .235f; tilt = 6; }
            else if (p < .4f) { float t = 1 - Mathf.Pow(1 - (p - .1f) / .3f, 3); height = Mathf.Lerp(.235f, 1.02f, t); tilt = Mathf.Lerp(6, -17, t); }
            else { float t = Mathf.SmoothStep(0, 1, (p - .4f) / .6f); height = Mathf.Lerp(1.02f, .78f, t); tilt = Mathf.Lerp(-17, -9, t); }
        }
        return new(height, tilt, 24);
    }
}
public sealed class CuttingWeaponMotion : IWeaponMotion
{
    public WeaponPose Pose(WeaponMotionFrame f) => new(.36f + Mathf.Sin(f.Time * 46) * .012f, -3, -12);
}
public sealed class GrindingWeaponMotion : IWeaponMotion
{
    public WeaponPose Pose(WeaponMotionFrame f) => new(.33f + Mathf.Sin(f.Time * 36) * .01f, -7, 12);
}
public sealed class WeaponMotionRegistry
{
    private readonly Dictionary<string, IWeaponMotion> _motions = new();
    public WeaponMotionRegistry Register(string key, IWeaponMotion motion) { _motions.Add(key, motion); return this; }
    public IWeaponMotion Get(string key) => _motions.TryGetValue(key, out var motion) ? motion : _motions["swing"];
    public static WeaponMotionRegistry CreateDefault() => new WeaponMotionRegistry().Register("swing", new SwingWeaponMotion())
        .Register("cut", new CuttingWeaponMotion()).Register("grind", new GrindingWeaponMotion());
}
