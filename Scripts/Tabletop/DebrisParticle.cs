using Godot;
using System;

/// <summary>Ballistic debris or a decorative coin flight. Arrival is a visual notification only.</summary>
public partial class DebrisParticle : MeshInstance3D
{
    private Vector3 _velocity, _spin, _baseScale, _flightFrom;
    private Vector3? _destination;
    private float _age, _delay, _duration;
    private bool _collecting, _finished;
    public event Action? Collected;

    public void Launch(Vector3 velocity, Vector3 spin, float delay, float duration, Vector3? destination = null)
    {
        _velocity = velocity; _spin = spin; _delay = delay; _duration = duration;
        _destination = destination; _baseScale = Scale; Visible = delay <= 0;
    }

    public bool Tick(float delta)
    {
        if (_finished) return false;
        if (_delay > 0)
        {
            _delay -= delta;
            if (_delay > 0) return true;
            Show();
        }
        _age += delta;
        if (_destination is Vector3 destination && _age >= .40f)
        {
            if (!_collecting) { _collecting = true; _flightFrom = Position; }
            float travel = Mathf.Clamp((_age - .40f) / .30f, 0, 1);
            Position = _flightFrom.Lerp(destination, travel * travel) + Vector3.Up * Mathf.Sin(travel * Mathf.Pi) * .85f;
            Rotation += _spin * delta;
            Scale = _baseScale * Mathf.Lerp(1, .30f, travel);
            if (travel >= 1) { _finished = true; Collected?.Invoke(); return false; }
            return true;
        }
        _velocity.Y -= 17.5f * delta;
        Position += _velocity * delta;
        Rotation += _spin * delta;
        if (Position.Y < .055f)
        {
            Position = new(Position.X, .055f, Position.Z);
            _velocity = new(_velocity.X * .57f, Mathf.Abs(_velocity.Y) * .28f, _velocity.Z * .57f);
            _spin *= .48f;
        }
        if (_destination == null)
        {
            Scale = _baseScale * Mathf.Clamp((_duration - _age) / .24f, 0, 1);
            if (_age >= _duration) { _finished = true; return false; }
        }
        return true;
    }
}
