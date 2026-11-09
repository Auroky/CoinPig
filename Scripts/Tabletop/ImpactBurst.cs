using Godot;
using System;
using System.Collections.Generic;

/// <summary>Owns short-lived 3D fragments, coins and floating rewards.</summary>
public partial class ImpactBurst : Node3D
{
    private sealed class Fragment
    {
        public MeshInstance3D Node = null!;
        public Vector3 Velocity, Spin;
        public float Life;
        public bool Coin;
    }
    private sealed class Caption
    {
        public Label3D Node = null!;
        public float Life = 1;
    }
    private readonly List<Fragment> _fragments = new();
    private readonly List<Caption> _captions = new();
    private readonly Random _random = new();
    private TabletopMaterials _materials = null!;
    private Font _font = null!;
    private Mesh _shard = null!, _coin = null!;

    public void Assemble(TabletopMaterials materials)
    {
        _materials = materials;
        _font = WorkshopStyle.BodyFont();
        _shard = new PrismMesh { Size = new(.15f, .10f, .12f) };
        _coin = new CylinderMesh { TopRadius = .095f, BottomRadius = .095f, Height = .025f, RadialSegments = 16, Rings = 0 };
    }

    public void Clear()
    {
        foreach (var fragment in _fragments) { fragment.Node.Hide(); fragment.Node.QueueFree(); }
        foreach (var caption in _captions) { caption.Node.Hide(); caption.Node.QueueFree(); }
        _fragments.Clear();
        _captions.Clear();
    }

    public void Break(System.Numerics.Vector2 point)
    {
        var center = new Vector3(point.X, .48f, point.Y);
        for (int i = 0; i < 16; i++)
        {
            bool coin = i >= 11;
            var material = coin ? _materials.Paint("d9b15f", .25f, .8f) : _materials.Paint(i % 2 == 0 ? "ce9386" : "ead8bd", .5f);
            var node = SceneShapes.Mesh(this, coin ? _coin : _shard, center, material);
            node.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
            float angle = (float)_random.NextDouble() * Mathf.Tau;
            float speed = 1.3f + (float)_random.NextDouble() * 2.2f;
            _fragments.Add(new Fragment
            {
                Node = node, Velocity = new(Mathf.Cos(angle) * speed, 2 + (float)_random.NextDouble() * 2.5f, Mathf.Sin(angle) * speed),
                Spin = new(4 + i % 3, i % 5 - 2, 3), Life = coin ? 1.05f : .95f, Coin = coin
            });
        }
    }

    public void Strike(System.Numerics.Vector2 point, SwingResult result, int coins)
    {
        string text = coins > 0 ? (result.DestroyedCount >= 2 ? $"{result.DestroyedCount} 连碎  +¥{coins}" : $"+¥{coins}") : result.HitCount > 0 ? "耐久下降" : "空锤";
        var label = new Label3D
        {
            Text = text, Font = _font, FontSize = 44, OutlineSize = 8, PixelSize = .007f,
            Position = new(point.X, 1.5f, point.Y),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Modulate = new("f3dc9b"), OutlineModulate = new("32291a"), NoDepthTest = true
        };
        AddChild(label);
        _captions.Add(new Caption { Node = label });
    }

    public void Tick(float delta)
    {
        for (int i = _fragments.Count - 1; i >= 0; i--)
        {
            var fragment = _fragments[i];
            fragment.Life -= delta;
            if (fragment.Life <= 0)
            {
                fragment.Node.Hide(); fragment.Node.QueueFree(); _fragments.RemoveAt(i); continue;
            }
            if (fragment.Coin && fragment.Life < .45f)
            {
                var destination = new Vector3(7.7f, 1.1f, -4.1f);
                fragment.Velocity = fragment.Node.Position.DirectionTo(destination) * 23;
            }
            else fragment.Velocity.Y -= 9.5f * delta;
            fragment.Node.Position += fragment.Velocity * delta;
            fragment.Node.Rotation += fragment.Spin * delta;
            if (fragment.Node.Position.Y < .055f)
            {
                var at = fragment.Node.Position; at.Y = .055f; fragment.Node.Position = at;
                fragment.Velocity.Y = Mathf.Abs(fragment.Velocity.Y) * .32f;
                fragment.Velocity.X *= .65f; fragment.Velocity.Z *= .65f;
            }
        }
        for (int i = _captions.Count - 1; i >= 0; i--)
        {
            var caption = _captions[i];
            caption.Life -= delta;
            caption.Node.Position += Vector3.Up * delta * .7f;
            caption.Node.Modulate = new Color(new Color("f3dc9b"), Mathf.Clamp(caption.Life * 2, 0, 1));
            if (caption.Life <= 0) { caption.Node.Hide(); caption.Node.QueueFree(); _captions.RemoveAt(i); }
        }
    }

    public override void _ExitTree()
    {
        _font?.Dispose(); _shard?.Dispose(); _coin?.Dispose();
    }
}
