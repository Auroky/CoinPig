using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

/// <summary>
/// 协调猪群推进、体积避让、范围目标查询和受击后的重生安排；视图只读取计算完成的位置。
/// </summary>
public sealed class PigHerd
{
    private readonly IReadOnlyList<PigMotion> _pigs;

    public IReadOnlyList<PigMotion> Pigs => _pigs;

    private readonly HerdSpacing _spacing;

    public PigHerd(IEnumerable<PigSpawn> spawns, HerdSpacing? spacing = null)
    {
        ArgumentNullException.ThrowIfNull(spawns);
        _pigs = Array.AsReadOnly(spawns.Select(spawn => new PigMotion(spawn)).ToArray());
        _spacing = spacing ?? new HerdSpacing();
        _spacing.Resolve(_pigs, 0);
    }

    /// <summary>将一帧拆成短子步，逐步移动并解决接触，减少低帧率下穿过另一只猪的机会。</summary>
    public void Advance(float delta)
    {
        if (!float.IsFinite(delta) || delta < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta));
        }
        // 即使渲染出现长帧，也分段处理移动和接触，避免一次跨过另一只猪。
        int steps = Math.Max(1, (int)MathF.Ceiling(MathF.Min(delta * 60, 600)));
        float step = delta / steps;
        for (int i = 0; i < steps; i++)
        {
            foreach (var pig in _pigs)
            {
                pig.Advance(step);
            }

            _spacing.Resolve(_pigs, step);
        }
    }

    public PigState[] Targets(Vector2 point, float radius)
    {
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y) || !float.IsFinite(radius) || radius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius));
        }

        return _pigs.Where(pig => pig.Active && Vector2.Distance(pig.Position, point) <= radius + pig.State.Definition.HitRadius)
                    .Select(pig => pig.State).ToArray();
    }

    public void Apply(SwingResult strike)
    {
        foreach (var hit in strike.Targets)
        {
            var pig = _pigs.FirstOrDefault(p => ReferenceEquals(p.State, hit.Pig));
            pig?.Receive(hit.Result);
        }
    }
}
