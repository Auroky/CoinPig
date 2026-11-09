using Godot;
using System;
using System.Collections.Generic;

/// <summary>A ceramic bank model driven entirely by a read-only pig motion object.</summary>
public partial class PigModel : Node3D
{
    private sealed record Finish(string Body, string Nose, float Scale, float Metallic);
    private static readonly IReadOnlyDictionary<string, Finish> Finishes = new Dictionary<string, Finish>
    {
        ["ceramic"] = new("d3998d", "dfaa9d", 1, 0),
        ["tin"] = new("899d9b", "a8b9b3", 1.12f, .65f),
        ["swift"] = new("d4bb82", "d9ad51", .86f, .1f)
    };
    private readonly List<Node3D> _feet = new();
    private PigMotion _motion = null!;
    private StandardMaterial3D _glaze = null!;
    private Color _color;
    private System.Numerics.Vector2 _previous;
    private float _yaw, _flash;
    public PigMotion Motion => _motion;

    public void Assemble(PigMotion motion, TabletopMaterials materials)
    {
        _motion = motion;
        _previous = motion.Position;
        _yaw = motion.Position.X > 0 ? -Mathf.Pi / 2 : Mathf.Pi / 2;
        var finish = Finishes.TryGetValue(motion.State.Definition.Id, out var known) ? known : Finishes["ceramic"];
        _color = new(finish.Body);
        _glaze = new StandardMaterial3D
        {
            AlbedoColor = _color, Roughness = .32f, Metallic = finish.Metallic,
            MetallicSpecular = .45f, ClearcoatEnabled = true, Clearcoat = .3f, ClearcoatRoughness = .35f
        };
        Scale = Vector3.One * finish.Scale;
        var nose = materials.Paint(finish.Nose, .3f, finish.Metallic);
        var dark = materials.Paint("362c2c", .5f);
        var black = materials.Paint("171a17", .15f);
        var brass = materials.Paint("b69a5d", .3f, .7f);

        SceneShapes.Ellipsoid(this, new(0, .52f, -.08f), new(.43f, .42f, .57f), _glaze);
        SceneShapes.Ellipsoid(this, new(0, .54f, .35f), new(.32f, .32f, .32f), _glaze);
        var snout = SceneShapes.Cylinder(this, new(0, .46f, .65f), .19f, .18f, .19f, nose, 40);
        snout.RotationDegrees = new(90, 0, 0);
        foreach (float side in new[] { -1f, 1f })
        {
            SceneShapes.Ellipsoid(this, new(side * .07f, .47f, .752f), new(.027f, .037f, .008f), dark);
            SceneShapes.Ellipsoid(this, new(side * .235f, .66f, .535f), new(.043f, .052f, .026f), black);
            SceneShapes.Ellipsoid(this, new(side * .231f, .678f, .558f), Vector3.One * .012f, materials.Paint("f4ead6", .2f));
            var ear = SceneShapes.Cylinder(this, new(side * .23f, .85f, .26f), .13f, .018f, .31f, _glaze, 3);
            ear.RotationDegrees = new(-10, side * 15, side * -18);
            var innerEar = SceneShapes.Cylinder(this, new(side * .23f, .861f, .282f), .082f, .01f, .20f, nose, 3);
            innerEar.RotationDegrees = ear.RotationDegrees;
            foreach (float front in new[] { -1f, 1f })
            {
                var foot = SceneShapes.Cylinder(this, new(side * .245f, .115f, front * .32f), .11f, .12f, .23f, _glaze, 20);
                _feet.Add(foot);
            }
        }
        SceneShapes.Box(this, new(0, .94f, -.07f), new(.09f, .014f, .31f), brass);
        SceneShapes.Box(this, new(0, .95f, -.07f), new(.038f, .012f, .26f), dark);
        var tail = SceneShapes.Mesh(this, new TorusMesh { InnerRadius = .043f, OuterRadius = .083f, Rings = 24, RingSegments = 10 }, new(.035f, .54f, -.655f), _glaze);
        tail.RotationDegrees = new(90, 0, 0);
        SceneShapes.Rod(this, new(0, .53f, -.58f), new(.035f, .54f, -.67f), .026f, _glaze);
        var key = new Node3D { Position = new(-.44f, .51f, -.15f), RotationDegrees = new(0, 0, 90) };
        AddChild(key);
        SceneShapes.Cylinder(key, Vector3.Zero, .035f, .035f, .22f, brass, 16);
        foreach (float side in new[] { -1f, 1f })
        {
            var loop = SceneShapes.Mesh(key, new TorusMesh { InnerRadius = .045f, OuterRadius = .075f, Rings = 16, RingSegments = 8 }, new(side * .065f, .14f, 0), brass);
            loop.RotationDegrees = new(90, 0, 0);
        }
        Present(0);
    }

    public void Hit() => _flash = .18f;

    public void Tick(float delta)
    {
        _flash = Mathf.Max(0, _flash - delta);
    }

    public void Present(float delta)
    {
        var movement = _motion.Position - _previous;
        if (movement.LengthSquared() > .0000001f)
        {
            float desired = Mathf.Atan2(movement.X, movement.Y);
            _yaw = Mathf.LerpAngle(_yaw, desired, 1 - Mathf.Exp(-12 * delta));
        }
        _previous = _motion.Position;
        Position = new(_motion.Position.X, .015f + Mathf.Sin(_motion.Time * 16) * .012f, _motion.Position.Y);
        Rotation = new(0, _yaw, Mathf.Sin(_motion.Time * 8) * .015f);
        for (int i = 0; i < _feet.Count; i++)
        {
            var at = _feet[i].Position;
            at.Y = .115f + Mathf.Max(0, Mathf.Sin(_motion.Time * 8 + (i is 0 or 3 ? 0 : Mathf.Pi))) * .055f;
            _feet[i].Position = at;
        }
        _glaze.AlbedoColor = _color.Lerp(Colors.White, (_flash / .18f) * .75f);
    }

    public override void _ExitTree() => _glaze?.Dispose();
}
