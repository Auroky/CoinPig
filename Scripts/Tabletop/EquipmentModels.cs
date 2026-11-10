using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 一件装备的模型根节点与可选动画回调；商店展示和手持工具共用。
/// </summary>
public sealed class EquipmentModelParts
{
    public Node3D Root { get; }

    private readonly Action<float, float>? _animate;

    public EquipmentModelParts(Node3D root, Action<float, float>? animate = null)
    {
        Root = root;
        _animate = animate;
    }

    public void Animate(float time, float activity) => _animate?.Invoke(time, activity);
}

/// <summary>
/// 构建装备造型的能力；返回附着于指定父节点的模型及其动画部件。
/// </summary>
public interface IEquipmentModelBuilder
{
    EquipmentModelParts Build(Node3D parent, TabletopMaterials materials, EquipmentPalette palette, int level);
}

/// <summary>
/// 按外观键选择模型构建器。默认造型在组装入口登记，未知键使用已登记的锤子造型。
/// </summary>
public sealed class EquipmentModelRegistry
{
    private readonly Dictionary<string, IEquipmentModelBuilder> _builders = new();

    public EquipmentModelRegistry Register(string key, IEquipmentModelBuilder builder)
    {
        _builders.Add(key, builder);
        return this;
    }

    public EquipmentModelParts Build(string key, Node3D parent, TabletopMaterials materials, EquipmentPalette palette, int level) =>
        (_builders.TryGetValue(key, out var builder) ? builder : _builders["hammer"]).Build(parent, materials, palette, level);

    public static EquipmentModelRegistry CreateDefault() => new EquipmentModelRegistry()
        .Register("hammer", new HammerEquipmentBuilder()).Register("cuff", new CuffEquipmentBuilder())
        .Register("chainsaw", new ChainsawEquipmentBuilder()).Register("grinder", new GrinderEquipmentBuilder());
}

/// <summary>
/// 装备构建器共用的基础几何方法，集中处理节点归属与材质创建。
/// </summary>
internal sealed class EquipmentShapes
{
    public Node3D Root { get; } = new();

    private readonly TabletopMaterials _materials;

    public EquipmentShapes(Node3D parent, TabletopMaterials materials)
    {
        parent.AddChild(Root);
        _materials = materials;
    }

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
        ring.RotationDegrees = new(90, 0, 0);
        return ring;
    }

    public MeshInstance3D Disc(Node3D parent, Vector3 at, float radius, float height, string color)
    {
        var disc = Mesh(parent, new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = 40 }, at, color);
        disc.RotationDegrees = new(90, 0, 0);
        return disc;
    }
}

/// <summary>
/// 用基本几何体组装锤子，等级仅影响可见造型细节。
/// </summary>
public sealed class HammerEquipmentBuilder : IEquipmentModelBuilder
{
    public EquipmentModelParts Build(Node3D parent, TabletopMaterials materials, EquipmentPalette palette, int level)
    {
        var shapes = new EquipmentShapes(parent, materials);
        float width = .99f + level * .065f;
        shapes.Box(Vector3.Zero, new(width, .46f, .48f), palette.Body);
        foreach (float side in new[] { -1f, 1f })
        {
            shapes.Box(new(side * width / 2, 0, 0), new(.11f, .51f, .52f), palette.Metal);
        }

        shapes.Box(new(0, 0, .255f), new(.17f, .46f, .035f), palette.Trim);
        Vector3 end = new(1.08f, .63f, 1.13f);
        shapes.Beam(new(0, 0, .12f), end, .175f, palette.Body);
        shapes.Beam(end * .64f, end, .215f, palette.Grip);
        for (int i = 0; i < 3 + level; i++)
        {
            shapes.Beam(end * (.64f + i * .32f / (3 + level)), end * (.66f + i * .32f / (3 + level)), .22f, palette.Trim);
        }

        for (int i = 0; i < level; i++)
        {
            shapes.Oval(new(-.24f + i * .18f, .04f, .27f), new(.035f, .08f, .025f), palette.Glow);
        }

        return new(shapes.Root);
    }
}

/// <summary>
/// 组装护腕与共振装饰；实际效果次数和伤害来自核心配置。
/// </summary>
public sealed class CuffEquipmentBuilder : IEquipmentModelBuilder
{
    public EquipmentModelParts Build(Node3D parent, TabletopMaterials materials, EquipmentPalette palette, int level)
    {
        var shapes = new EquipmentShapes(parent, materials);
        shapes.Ring(Vector3.Zero, .46f, .74f, palette.Body);
        shapes.Ring(new(0, 0, -.15f), .48f, .73f, palette.Grip);
        shapes.Ring(new(0, 0, .16f), .55f, .76f, palette.Trim);
        shapes.Oval(new(0, 0, .21f), new(.19f, .19f, .09f), palette.Glow);
        for (int i = 0; i < Math.Max(1, level); i++)
        {
            shapes.Ring(new(0, 0, .225f + i * .008f), .25f + i * .085f, .27f + i * .085f, palette.Trim);
        }

        shapes.Box(new(.55f, -.2f, .25f), new(.32f, .35f, .12f), palette.Metal);
        shapes.Box(new(.55f, -.2f, .32f), new(.17f, .22f, .035f), palette.Grip);
        for (int i = 0; i < level + 2; i++)
        {
            float angle = .55f + i * .5f;
            shapes.Oval(new(Mathf.Cos(angle) * .64f, Mathf.Sin(angle) * .64f, .17f), new(.045f, .045f, .04f), palette.Glow);
        }
        return new(shapes.Root);
    }
}

/// <summary>
/// 组装电锯并返回锯齿动画回调，展示转速与攻击状态分离。
/// </summary>
public sealed class ChainsawEquipmentBuilder : IEquipmentModelBuilder
{
    public EquipmentModelParts Build(Node3D parent, TabletopMaterials materials, EquipmentPalette palette, int level)
    {
        var shapes = new EquipmentShapes(parent, materials);
        shapes.Box(new(-.52f, .02f, 0), new(1.04f, .65f, .56f), palette.Body);
        shapes.Box(new(-.52f, .04f, .3f), new(.78f, .40f, .06f), palette.Trim);
        for (int i = 0; i < 5; i++)
        {
            shapes.Box(new(-.84f + i * .14f, -.1f, .35f), new(.05f, .28f, .025f), palette.Grip);
        }

        shapes.Beam(new(-1, -.15f, 0), new(-1.22f, .45f, 0), .13f, palette.Grip);
        shapes.Beam(new(-1.22f, .45f, 0), new(-.55f, .58f, 0), .13f, palette.Grip);
        shapes.Oval(new(.59f, 0, .03f), new(.97f, .23f, .11f), palette.Metal);
        shapes.Box(new(.55f, 0, .16f), new(1.05f, .09f, .025f), palette.Trim);
        var teeth = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new BoxMesh { Size = new(.075f, .095f, .16f) },
            InstanceCount = 32
        };
        shapes.Root.AddChild(new MultiMeshInstance3D { Multimesh = teeth, MaterialOverride = materials.Paint(palette.Grip, .43f, .28f) });
        return new(shapes.Root, (time, activity) =>
        {
            for (int i = 0; i < teeth.InstanceCount; i++)
            {
                float angle = i * Mathf.Tau / teeth.InstanceCount + time * (2 + activity * 9);
                teeth.SetInstanceTransform(i, new(new Basis(Vector3.Back, angle), new(.59f + Mathf.Cos(angle) * .98f, Mathf.Sin(angle) * .26f, .04f)));
            }
        });
    }
}

/// <summary>
/// 组装砂轮、握把与防护罩，并提供砂轮旋转动画。
/// </summary>
public sealed class GrinderEquipmentBuilder : IEquipmentModelBuilder
{
    public EquipmentModelParts Build(Node3D parent, TabletopMaterials materials, EquipmentPalette palette, int level)
    {
        var shapes = new EquipmentShapes(parent, materials);
        shapes.Box(new(-.58f, .10f, -.08f), new(1.2f, .48f, .51f), palette.Body);
        shapes.Box(new(-1.1f, .1f, 0), new(.54f, .40f, .45f), palette.Grip);
        for (int i = 0; i < 4; i++)
        {
            shapes.Box(new(-1.28f + i * .12f, .11f, .235f), new(.035f, .28f, .025f), palette.Trim);
        }

        shapes.Beam(new(-.05f, .18f, -.05f), new(-.05f, .93f, -.05f), .18f, palette.Grip);
        var wheel = new Node3D { Position = new(.52f, 0, .22f) };
        shapes.Root.AddChild(wheel);
        shapes.Disc(wheel, Vector3.Zero, .70f + level * .025f, .12f, palette.Metal);
        shapes.Disc(wheel, new(0, 0, .07f), .58f, .025f, palette.Grip);
        shapes.Ring(new(0, 0, .09f), .61f, .67f, palette.Trim, wheel);
        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.Tau / 10;
            shapes.Beam(new(Mathf.Cos(a) * .25f, Mathf.Sin(a) * .25f, .1f),
                new(Mathf.Cos(a + .2f) * .55f, Mathf.Sin(a + .2f) * .55f, .1f), .025f, palette.Body, wheel);
        }
        shapes.Disc(wheel, new(0, 0, .12f), .16f, .045f, palette.Glow);
        for (int i = 0; i < 10; i++)
        {
            float a = .15f + i * Mathf.Pi / 10;
            var guard = shapes.Box(new(.52f + Mathf.Cos(a) * .76f, Mathf.Sin(a) * .76f, .13f), new(.20f, .12f, .25f), palette.Body);
            guard.Rotation = new(0, 0, a + Mathf.Pi / 2);
        }
        return new(shapes.Root, (time, activity) => wheel.Rotation = new(0, 0, time * (5 + activity * 24)));
    }
}

/// <summary>
/// 武器的表现姿态：高度使用世界单位，Tilt 和 Yaw 使用角度。
/// </summary>
public readonly record struct WeaponPose(float Height, float Tilt, float Yaw);

/// <summary>
/// 武器动画所需的时间快照，所有时间字段均为秒。
/// </summary>
public readonly record struct WeaponMotionFrame(float Windup, float WindupDuration, float Cooldown, float RecoveryDuration, float Time);

/// <summary>
/// 根据攻击阶段采样表现姿态，不能改变攻击计时、伤害或命中目标。
/// </summary>
public interface IWeaponMotion
{
    WeaponPose Pose(WeaponMotionFrame frame);
}

/// <summary>
/// 锤子的抬起、加速落下、触地停顿和回弹曲线，只负责表现姿态。
/// </summary>
public sealed class SwingWeaponMotion : IWeaponMotion
{
    public WeaponPose Pose(WeaponMotionFrame frame)
    {
        float height = .78f;
        float tilt = -9;
        if (frame.Windup > 0)
        {
            float progress = Mathf.Clamp(1 - frame.Windup / frame.WindupDuration, 0, .99f);
            if (progress < .64f)
            {
                float easedProgress = 1 - Mathf.Pow(1 - progress / .64f, 3);
                height = Mathf.Lerp(.78f, 1.78f, easedProgress);
                tilt = Mathf.Lerp(-9, -64, easedProgress);
            }
            else
            {
                float easedProgress = Mathf.Pow((progress - .64f) / .36f, 3);
                height = Mathf.Lerp(1.78f, .235f, easedProgress);
                tilt = Mathf.Lerp(-64, 6, easedProgress);
            }
        }
        else if (frame.Cooldown > 0)
        {
            float progress = 1 - Mathf.Clamp(frame.Cooldown / frame.RecoveryDuration, 0, 1);
            if (progress < .1f)
            {
                height = .235f;
                tilt = 6;
            }
            else if (progress < .4f)
            {
                float easedProgress = 1 - Mathf.Pow(1 - (progress - .1f) / .3f, 3);
                height = Mathf.Lerp(.235f, 1.02f, easedProgress);
                tilt = Mathf.Lerp(6, -17, easedProgress);
            }
            else
            {
                float easedProgress = Mathf.SmoothStep(0, 1, (progress - .4f) / .6f);
                height = Mathf.Lerp(1.02f, .78f, easedProgress);
                tilt = Mathf.Lerp(-17, -9, easedProgress);
            }
        }
        return new(height, tilt, 24);
    }
}

/// <summary>
/// 电锯的近桌面切割姿态和轻微振动。
/// </summary>
public sealed class CuttingWeaponMotion : IWeaponMotion
{
    public WeaponPose Pose(WeaponMotionFrame frame) => new(.36f + Mathf.Sin(frame.Time * 46) * .012f, -3, -12);
}

/// <summary>
/// 砂轮的研磨姿态和持续微振动。
/// </summary>
public sealed class GrindingWeaponMotion : IWeaponMotion
{
    public WeaponPose Pose(WeaponMotionFrame frame) => new(.33f + Mathf.Sin(frame.Time * 36) * .01f, -7, 12);
}

/// <summary>
/// 按动作键登记姿态策略；未知键回退到已登记的挥锤动作。
/// </summary>
public sealed class WeaponMotionRegistry
{
    private readonly Dictionary<string, IWeaponMotion> _motions = new();

    public WeaponMotionRegistry Register(string key, IWeaponMotion motion)
    {
        _motions.Add(key, motion);
        return this;
    }

    public IWeaponMotion Get(string key) => _motions.TryGetValue(key, out var motion) ? motion : _motions["swing"];

    public static WeaponMotionRegistry CreateDefault() => new WeaponMotionRegistry().Register("swing", new SwingWeaponMotion())
        .Register("cut", new CuttingWeaponMotion()).Register("grind", new GrindingWeaponMotion());
}
