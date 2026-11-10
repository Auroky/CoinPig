using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// 纯平面几何约束：分离胶囊碰撞体、处理侧向让行和桌面边界，不参与伤害或奖励。
/// </summary>
public sealed class HerdSpacing
{
    public Vector2 HalfSize { get; }

    // 只用于避免浮点误差反复判定为穿插，不是额外的可见间距。
    private const float Clearance = .001f;
    private const int MaximumSeparationPasses = 256;

    public HerdSpacing(Vector2? halfSize = null)
    {
        HalfSize = halfSize ?? new Vector2(7.4f, 3.85f);
        if (!float.IsFinite(HalfSize.X) || !float.IsFinite(HalfSize.Y) || HalfSize.X <= 0 || HalfSize.Y <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(halfSize));
        }
    }

    private void ClampToTable(PigMotion pig, Vector2 position)
    {
        var extent = Vector2.Abs(pig.Forward) * pig.BodyHalfLength + new Vector2(pig.BodyRadius);
        var limit = HalfSize - extent;
        pig.ConstrainPosition(Vector2.Clamp(position, -limit, limit));
    }

    // 求两条胶囊中心线段的最近点差。先处理线段退化为点的情况，再把投影参数限制在 [0, 1]。
    private static Vector2 FindContactOffset(PigMotion firstPig, PigMotion secondPig)
    {
        var firstStart = firstPig.Position - firstPig.Forward * firstPig.BodyHalfLength;
        var secondStart = secondPig.Position - secondPig.Forward * secondPig.BodyHalfLength;
        var firstSegment = firstPig.Forward * (2 * firstPig.BodyHalfLength);
        var secondSegment = secondPig.Forward * (2 * secondPig.BodyHalfLength);
        var startOffset = firstStart - secondStart;
        float firstLengthSquared = firstSegment.LengthSquared();
        float secondLengthSquared = secondSegment.LengthSquared();
        float secondProjection = Vector2.Dot(secondSegment, startOffset);
        float firstFraction;
        float secondFraction;
        if (firstLengthSquared <= .000001f && secondLengthSquared <= .000001f)
        {
            return secondStart - firstStart;
        }

        if (firstLengthSquared <= .000001f)
        {
            firstFraction = 0;
            secondFraction = Math.Clamp(secondProjection / secondLengthSquared, 0, 1);
        }
        else
        {
            float firstProjection = Vector2.Dot(firstSegment, startOffset);
            if (secondLengthSquared <= .000001f)
            {
                secondFraction = 0;
                firstFraction = Math.Clamp(-firstProjection / firstLengthSquared, 0, 1);
            }
            else
            {
                float segmentDotProduct = Vector2.Dot(firstSegment, secondSegment);
                float parallelDeterminant = firstLengthSquared * secondLengthSquared - segmentDotProduct * segmentDotProduct;
                firstFraction = parallelDeterminant > .000001f ? Math.Clamp((segmentDotProduct * secondProjection - firstProjection * secondLengthSquared) / parallelDeterminant, 0, 1) : 0;
                secondFraction = (segmentDotProduct * firstFraction + secondProjection) / secondLengthSquared;
                if (secondFraction < 0)
                {
                    secondFraction = 0;
                    firstFraction = Math.Clamp(-firstProjection / firstLengthSquared, 0, 1);
                }
                else if (secondFraction > 1)
                {
                    secondFraction = 1;
                    firstFraction = Math.Clamp((segmentDotProduct - firstProjection) / firstLengthSquared, 0, 1);
                }
            }
        }
        return (secondStart + secondSegment * secondFraction) - (firstStart + firstSegment * firstFraction);
    }

    /// <summary>先限制桌面边界，再迭代分离接触的胶囊；同一对猪每个子步只执行一次侧向让行。</summary>
    public void Resolve(IReadOnlyList<PigMotion> pigs, float delta)
    {
        ArgumentNullException.ThrowIfNull(pigs);
        if (!float.IsFinite(delta) || delta < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta));
        }

        foreach (var pig in pigs)
        {
            if (pig.BodyRadius + pig.BodyHalfLength >= MathF.Min(HalfSize.X, HalfSize.Y))
            {
                throw new ArgumentException("A pig's body must fit inside the table.", nameof(pigs));
            }

            if (pig.Active && pig.BodyRadius > 0)
            {
                ClampToTable(pig, pig.Position);
            }
        }
        // 一次推开可能让另一对猪重新接触；迭代到本轮无人移动，或达到拥挤情况下的计算上限。
        for (int iteration = 0; iteration < MaximumSeparationPasses; iteration++)
        {
            bool moved = false;
            for (int i = 0; i < pigs.Count; i++)
            {
                var firstPig = pigs[i];
                if (!firstPig.Active || firstPig.BodyRadius == 0)
                {
                    continue;
                }

                for (int j = i + 1; j < pigs.Count; j++)
                {
                    var secondPig = pigs[j];
                    if (!secondPig.Active || secondPig.BodyRadius == 0)
                    {
                        continue;
                    }

                    float minimumDistance = firstPig.BodyRadius + secondPig.BodyRadius;
                    var centerOffset = secondPig.Position - firstPig.Position;
                    float broadPhaseDistance = minimumDistance + firstPig.BodyHalfLength + secondPig.BodyHalfLength;
                    if (centerOffset.LengthSquared() >= broadPhaseDistance * broadPhaseDistance)
                    {
                        continue;
                    }

                    var contactOffset = FindContactOffset(firstPig, secondPig);
                    if (contactOffset.LengthSquared() >= minimumDistance * minimumDistance)
                    {
                        continue;
                    }

                    float contactDistance = contactOffset.Length();
                    // 出生点完全重合时，按稳定顺序选一个方向，避免归一化零向量。
                    float fallbackAngle = (i * pigs.Count + j) * 2.399963f;
                    var separationDirection = contactDistance > .00001f ? contactOffset / contactDistance :
                        centerOffset.LengthSquared() > .000001f ? Vector2.Normalize(centerOffset) : new Vector2(MathF.Cos(fallbackAngle), MathF.Sin(fallbackAngle));
                    if (iteration == 0 && Vector2.Dot(firstPig.PreferredStep - secondPig.PreferredStep, separationDirection) > 0)
                    {
                        // 迎面接近时各向一侧轻微让行，避免两只猪持续顶住。
                        var sideStep = new Vector2(-separationDirection.Y, separationDirection.X) * (.8f * MathF.Min(delta, 1f / 30));
                        ClampToTable(firstPig, firstPig.Position + sideStep);
                        ClampToTable(secondPig, secondPig.Position - sideStep);
                        contactOffset = FindContactOffset(firstPig, secondPig);
                        contactDistance = contactOffset.Length();
                        if (contactDistance > .00001f)
                        {
                            separationDirection = contactOffset / contactDistance;
                        }
                    }
                    if (contactDistance >= minimumDistance)
                    {
                        continue;
                    }

                    var correction = separationDirection * ((minimumDistance - contactDistance + Clearance) * .5f);
                    ClampToTable(firstPig, firstPig.Position - correction);
                    ClampToTable(secondPig, secondPig.Position + correction);
                    moved = true;
                }
            }
            if (!moved)
            {
                break;
            }
        }
    }
}
