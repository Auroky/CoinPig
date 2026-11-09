using Godot;
using System;
using System.Collections.Generic;

public partial class StrikeEffects : Node2D
{
    private sealed class Particle
    {
        public Vector2 Position, Velocity;
        public Color Color;
        public float Life;
        public bool Coin;
    }
    private readonly Random _random = new();
    private readonly List<Particle> _particles = new();
    private float _ripple, _radius;
    private Vector2 _point;

    public override void _Ready()
    {
        ZIndex = 2;
    }

    public void Clear()
    {
        _particles.Clear();
        _ripple = 0;
        QueueRedraw();
    }

    public void Break(Vector2 at, Color color)
    {
        for (int i = 0; i < 13; i++)
        {
            float angle = (float)_random.NextDouble() * Mathf.Tau;
            _particles.Add(new Particle { Position = at, Velocity = Vector2.FromAngle(angle) * _random.Next(100, 320), Color = color, Life = .65f });
        }
        for (int i = 0; i < 4; i++)
            _particles.Add(new Particle { Position = at, Velocity = new(_random.Next(-170, 170), _random.Next(-230, -70)), Color = WorkshopStyle.Brass, Life = 1, Coin = true });
    }

    public void Strike(Vector2 point, float radius)
    {
        _ripple = .3f;
        _point = point;
        _radius = radius;
    }

    public void Tick(float delta)
    {
        _ripple = Mathf.Max(0, _ripple - delta);
        foreach (var particle in _particles)
        {
            particle.Life -= delta;
            if (particle.Coin && particle.Life < .6f)
                particle.Velocity = particle.Position.DirectionTo(new(GetViewportRect().Size.X - 100, 60)) * 1000;
            else particle.Velocity *= Mathf.Pow(.2f, delta);
            particle.Position += particle.Velocity * delta;
        }
        _particles.RemoveAll(p => p.Life <= 0);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_ripple > 0)
        {
            float a = _ripple / .3f;
            DrawArc(_point, _radius * (1.4f - a * .45f), 0, Mathf.Tau, 80, new Color(.95f, .8f, .5f, a * .7f), 2, true);
        }
        foreach (var particle in _particles)
        {
            var color = new Color(particle.Color, Mathf.Clamp(particle.Life * 4, 0, 1));
            if (particle.Coin)
            {
                DrawCircle(particle.Position, 6, color);
                DrawArc(particle.Position, 3.5f, 0, Mathf.Tau, 12, new("8b642b"), 1);
            }
            else DrawRect(new(particle.Position, new(7, 5)), color);
        }
    }
}
