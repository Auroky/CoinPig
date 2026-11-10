using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 管理三维碎片、金币、火花与冲击环。金币收集仅触发表现事件，规则收益已先结算。
/// </summary>
public partial class ImpactBurst : Node3D
{
    private sealed class Pulse
    {
        public MeshInstance3D Node = null!;
        public float Age;
        public float Delay;
        public float Radius;
        public Vector3 Direction;
        public bool Ray;
    }

    private readonly List<DebrisParticle> _particles = new();
    private readonly List<Pulse> _pulses = new();
    private readonly Random _random = new(53);
    private TabletopMaterials _materials = null!;
    private ArrayMesh _shard = null!;
    private CylinderMesh _coin = null!;
    private CylinderMesh _coinFace = null!;
    private StandardMaterial3D _soft = null!;
    private StandardMaterial3D _strong = null!;

    public event Action? CoinCollected;

    public int ActiveCount => _particles.Count + _pulses.Count;

    public void Assemble(TabletopMaterials materials)
    {
        _materials = materials;
        _shard = MakeShard();
        _coin = new CylinderMesh { BottomRadius = .145f, TopRadius = .145f, Height = .045f, RadialSegments = 20, Rings = 0 };
        _coinFace = new CylinderMesh { BottomRadius = .105f, TopRadius = .105f, Height = .008f, RadialSegments = 20, Rings = 0 };
        _soft = new StandardMaterial3D { AlbedoColor = new("d5b68a"), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        _strong = new StandardMaterial3D { AlbedoColor = new("ffd783"), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
    }

    public void Clear()
    {
        foreach (var particle in _particles)
        {
            particle.Hide();
            particle.QueueFree();
        }
        foreach (var pulse in _pulses)
        {
            pulse.Node.Hide();
            pulse.Node.QueueFree();
        }
        _particles.Clear();
        _pulses.Clear();
    }

    private float Range(float minimum, float maximum) => minimum + (float)_random.NextDouble() * (maximum - minimum);

    private DebrisParticle Piece(Mesh mesh, Vector3 position, Material material)
    {
        var particle = new DebrisParticle { Mesh = mesh, Position = position, MaterialOverride = material };
        AddChild(particle);
        _particles.Add(particle);
        return particle;
    }

    public void Break(System.Numerics.Vector2 point, System.Numerics.Vector2 impact, Color glaze, Color interior, float size, int coinCount, Vector3 destination)
    {
        var position = new Vector3(point.X, 0, point.Y);
        Vector3 outward = (position - new Vector3(impact.X, 0, impact.Y)).Normalized();
        for (int i = 0; i < 11; i++)
        {
            float angle = i * Mathf.Tau / 11 + Range(-.18f, .18f);
            Vector3 radial = new(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            var particle = new DebrisParticle { Mesh = _shard, Position = position + radial * .22f + Vector3.Up * Range(.27f, .73f) };
            AddChild(particle);
            _particles.Add(particle);
            particle.SetSurfaceOverrideMaterial(0, _materials.Paint(glaze));
            particle.SetSurfaceOverrideMaterial(1, _materials.Paint(interior));
            particle.Scale = Vector3.One * Range(.85f, 1.45f) * size;
            particle.Rotation = new(Range(-1, 1), angle, Range(-1, 1));
            particle.Launch(radial * Range(1.7f, 3.8f) + outward * .75f + Vector3.Up * Range(2.1f, 4.5f),
                new(Range(-9, 9), Range(-8, 8), Range(-7, 7)), 0, Range(.68f, 1));
        }
        for (int i = 0; i < Mathf.Clamp(coinCount, 2, 6); i++)
        {
            var coin = Piece(_coin, position + Vector3.Up * .48f, _materials.Paint("efb544"));
            coin.Scale = Vector3.One * Range(.90f, 1.15f);
            SceneShapes.Mesh(coin, _coinFace, new(0, .028f, 0), _materials.Paint("ffe099"));
            SceneShapes.Mesh(coin, _coinFace, new(0, -.028f, 0), _materials.Paint("ffe099"));
            float angle = Range(0, Mathf.Tau);
            coin.Rotation = new(.55f, angle, .30f);
            coin.Launch(new(Mathf.Cos(angle) * Range(1, 2), Range(4.2f, 5.4f), Mathf.Sin(angle) * Range(1, 2)),
                new(Range(5, 10), Range(-4, 4), Range(-3, 3)), i * .018f, 1.2f, destination);
            coin.Collected += () => CoinCollected?.Invoke();
        }
    }

    public void HitSparks(System.Numerics.Vector2 point)
    {
        for (int i = 0; i < 6; i++)
        {
            float angle = Range(0, Mathf.Tau);
            var spark = Piece(new BoxMesh { Size = new(.024f, .14f, .024f) }, new Vector3(point.X, .8f, point.Y), _materials.Paint("eee2bc"));
            spark.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            spark.Launch(new(Mathf.Cos(angle) * 2, Range(2, 3.4f), Mathf.Sin(angle) * 2), new(4, 3, 5), 0, .24f);
        }
    }

    public void Strike(System.Numerics.Vector2 point, SwingResult result, float radius)
    {
        var position = new Vector3(point.X, 0, point.Y);
        bool strong = result.DestroyedCount >= 2;
        Ring(position, radius, strong, 0);
        if (strong)
        {
            Ring(position, radius * .76f, true, .045f);
        }

        if (result.HitCount == 0)
        {
            return;
        }

        int rays = strong ? 10 : 6;
        for (int i = 0; i < rays; i++)
        {
            float angle = i * Mathf.Tau / rays + Range(-.12f, .12f);
            Vector3 direction = new(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            var ray = SceneShapes.Beam(this, position + Vector3.Up * .07f + direction * .18f,
                position + Vector3.Up * .07f + direction * (strong ? .72f : .49f), .028f, _strong);
            ray.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            _pulses.Add(new Pulse { Node = ray, Direction = direction, Ray = true });
        }
    }

    private void Ring(Vector3 position, float radius, bool strong, float delay)
    {
        var ring = SceneShapes.Mesh(this, new TorusMesh { InnerRadius = .95f, OuterRadius = 1, Rings = 64, RingSegments = 6 }, position + Vector3.Up * .04f, strong ? _strong : _soft);
        ring.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        ring.Scale = Vector3.Zero;
        _pulses.Add(new Pulse { Node = ring, Radius = radius, Delay = delay });
    }

    public void Resonance(System.Numerics.Vector2 point, float radius)
    {
        var at = new Vector3(point.X, 0, point.Y);
        for (int i = 0; i < 3; i++)
        {
            Ring(at, radius * (1 - i * .18f), true, i * .065f);
        }
    }

    public void Tick(float delta)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            if (_particles[i].Tick(delta))
            {
                continue;
            }

            _particles[i].Hide();
            _particles[i].QueueFree();
            _particles.RemoveAt(i);
        }
        for (int i = _pulses.Count - 1; i >= 0; i--)
        {
            var pulse = _pulses[i];
            pulse.Age += delta;
            float age = pulse.Age - pulse.Delay;
            if (age < 0)
            {
                continue;
            }

            float duration = pulse.Ray ? .13f : .24f;
            if (age >= duration)
            {
                pulse.Node.Hide();
                pulse.Node.QueueFree();
                _pulses.RemoveAt(i);
                continue;
            }
            if (pulse.Ray)
            {
                pulse.Node.Position += pulse.Direction * (.30f / .13f) * delta;
                pulse.Node.Scale = Vector3.One * (1 - age / duration);
            }
            else if (age < .14f)
            {
                float t = age / .14f;
                pulse.Node.Scale = Vector3.One * pulse.Radius * (1 - (1 - t) * (1 - t));
            }
            else
            {
                pulse.Node.Scale = new(pulse.Radius, pulse.Radius * (1 - (age - .14f) / .1f), pulse.Radius);
            }
        }
    }

    private static ArrayMesh MakeShard()
    {
        Vector3[] top = { new(-.16f, .035f, -.13f), new(.19f, .02f, -.07f), new(-.04f, .06f, .21f) };
        var mesh = new ArrayMesh();
        using var surface = new SurfaceTool();
        void Face(Vector3 a, Vector3 b, Vector3 c)
        {
            var normal = (b - a).Cross(c - a).Normalized();
            foreach (var point in new[] { a, b, c })
            {
                surface.SetNormal(normal);
                surface.AddVertex(point);
            }
        }
        surface.Begin(Mesh.PrimitiveType.Triangles);
        Face(top[0], top[2], top[1]);
        surface.Commit(mesh);
        surface.Clear();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 thickness = Vector3.Up * .055f;
        Face(top[0] - thickness, top[1] - thickness, top[2] - thickness);
        for (int i = 0; i < 3; i++)
        {
            int next = (i + 1) % 3;
            Face(top[i], top[next], top[next] - thickness);
            Face(top[i], top[next] - thickness, top[i] - thickness);
        }
        surface.Commit(mesh);
        return mesh;
    }

    public override void _ExitTree()
    {
        _shard?.Dispose();
        _coin?.Dispose();
        _coinFace?.Dispose();
        _soft?.Dispose();
        _strong?.Dispose();
    }
}
