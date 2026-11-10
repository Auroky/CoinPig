using Godot;

/// <summary>
/// 短促镜头震动。拾取和投影临时排除震动偏移，确保镜头反馈不会改变真实瞄准点。
/// </summary>
public sealed class CameraFeedback
{
    private readonly Camera3D _camera;
    private float _age;
    private float _strength;
    private const float Duration = .13f;

    public CameraFeedback(Camera3D camera) => _camera = camera;

    public void Punch(int hits, int broken)
    {
        if (hits == 0)
        {
            return;
        }

        _age = 0;
        _strength = broken == 0 ? .025f : Mathf.Min(.085f, .035f + broken * .014f);
    }

    public void Reset()
    {
        _strength = 0;
        _camera.HOffset = _camera.VOffset = 0;
    }

    public void Tick(float delta)
    {
        if (_strength <= 0)
        {
            return;
        }

        _age += delta;
        if (_age >= Duration)
        {
            Reset();
            return;
        }
        float envelope = Mathf.Pow(1 - _age / Duration, 2);
        _camera.HOffset = Mathf.Sin(_age * 105) * _strength * envelope;
        _camera.VOffset = Mathf.Cos(_age * 86) * _strength * envelope * .65f;
    }

    /// <summary>暂时移除镜头震动，将屏幕射线与指定高度的水平面求交，并在退出时恢复偏移。</summary>
    public bool PointAt(Vector2 screen, float height, out Vector3 point)
    {
        float horizontal = _camera.HOffset;
        float vertical = _camera.VOffset;
        _camera.HOffset = _camera.VOffset = 0;
        try
        {
            Vector3 origin = _camera.ProjectRayOrigin(screen);
            Vector3 ray = _camera.ProjectRayNormal(screen);
            if (Mathf.Abs(ray.Y) < .00001f)
            {
                point = default;
                return false;
            }
            float distance = (height - origin.Y) / ray.Y;
            point = origin + ray * distance;
            return distance >= 0;
        }
        finally { _camera.HOffset = horizontal; _camera.VOffset = vertical; }
    }

    public Vector2 Project(Vector3 point)
    {
        float horizontal = _camera.HOffset;
        float vertical = _camera.VOffset;
        _camera.HOffset = _camera.VOffset = 0;
        try
        {
            return _camera.UnprojectPosition(point);
        }
        finally { _camera.HOffset = horizontal; _camera.VOffset = vertical; }
    }
}
