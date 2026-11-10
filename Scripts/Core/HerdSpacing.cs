using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>Planar body separation and table boundaries, independent of rendering and hit rewards.</summary>
public sealed class HerdSpacing
{
    public Vector2 HalfSize { get; }
    private const float Clearance = .001f;

    public HerdSpacing(Vector2? halfSize = null)
    {
        HalfSize = halfSize ?? new Vector2(7.4f, 3.85f);
        if (!float.IsFinite(HalfSize.X) || !float.IsFinite(HalfSize.Y) || HalfSize.X <= 0 || HalfSize.Y <= 0)
            throw new ArgumentOutOfRangeException(nameof(halfSize));
    }

    private void Place(PigMotion pig, Vector2 position)
    {
        var extent = Vector2.Abs(pig.Forward) * pig.BodyHalfLength + new Vector2(pig.BodyRadius);
        var limit = HalfSize - extent;
        pig.ConstrainPosition(Vector2.Clamp(position, -limit, limit));
    }

    private static Vector2 ContactOffset(PigMotion a, PigMotion b)
    {
        var p = a.Position - a.Forward * a.BodyHalfLength;
        var q = b.Position - b.Forward * b.BodyHalfLength;
        var u = a.Forward * (2 * a.BodyHalfLength);
        var v = b.Forward * (2 * b.BodyHalfLength);
        var r = p - q;
        float aa = u.LengthSquared(), ee = v.LengthSquared(), f = Vector2.Dot(v, r);
        float s, t;
        if (aa <= .000001f && ee <= .000001f) return q - p;
        if (aa <= .000001f) { s = 0; t = Math.Clamp(f / ee, 0, 1); }
        else
        {
            float c = Vector2.Dot(u, r);
            if (ee <= .000001f) { t = 0; s = Math.Clamp(-c / aa, 0, 1); }
            else
            {
                float bb = Vector2.Dot(u, v), denominator = aa * ee - bb * bb;
                s = denominator > .000001f ? Math.Clamp((bb * f - c * ee) / denominator, 0, 1) : 0;
                t = (bb * s + f) / ee;
                if (t < 0) { t = 0; s = Math.Clamp(-c / aa, 0, 1); }
                else if (t > 1) { t = 1; s = Math.Clamp((bb - c) / aa, 0, 1); }
            }
        }
        return (q + v * t) - (p + u * s);
    }

    public void Resolve(IReadOnlyList<PigMotion> pigs, float delta)
    {
        ArgumentNullException.ThrowIfNull(pigs);
        if (!float.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        foreach (var pig in pigs)
        {
            if (pig.BodyRadius + pig.BodyHalfLength >= MathF.Min(HalfSize.X, HalfSize.Y))
                throw new ArgumentException("A pig's body must fit inside the table.", nameof(pigs));
            if (pig.Active && pig.BodyRadius > 0) Place(pig, pig.Position);
        }
        for (int iteration = 0; iteration < 256; iteration++)
        {
            bool moved = false;
            for (int i = 0; i < pigs.Count; i++)
            {
                var a = pigs[i];
                if (!a.Active || a.BodyRadius == 0) continue;
                for (int j = i + 1; j < pigs.Count; j++)
                {
                    var b = pigs[j];
                    if (!b.Active || b.BodyRadius == 0) continue;
                    float needed = a.BodyRadius + b.BodyRadius;
                    var centers = b.Position - a.Position;
                    float reach = needed + a.BodyHalfLength + b.BodyHalfLength;
                    if (centers.LengthSquared() >= reach * reach) continue;
                    var offset = ContactOffset(a, b);
                    if (offset.LengthSquared() >= needed * needed) continue;
                    float distance = offset.Length();
                    // Stable fallback for coincident spawn points; never normalize a zero vector.
                    float angle = (i * pigs.Count + j) * 2.399963f;
                    var normal = distance > .00001f ? offset / distance :
                        centers.LengthSquared() > .000001f ? Vector2.Normalize(centers) : new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                    if (iteration == 0 && Vector2.Dot(a.PreferredStep - b.PreferredStep, normal) > 0)
                    {
                        // A small consistent side step lets head-on traffic pass instead of locking in place.
                        var side = new Vector2(-normal.Y, normal.X) * (.8f * MathF.Min(delta, 1f / 30));
                        Place(a, a.Position + side); Place(b, b.Position - side);
                        offset = ContactOffset(a, b); distance = offset.Length();
                        if (distance > .00001f) normal = offset / distance;
                    }
                    if (distance >= needed) continue;
                    var correction = normal * ((needed - distance + Clearance) * .5f);
                    Place(a, a.Position - correction); Place(b, b.Position + correction);
                    moved = true;
                }
            }
            if (!moved) break;
        }
    }
}
