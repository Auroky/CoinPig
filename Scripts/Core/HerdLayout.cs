using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// 单只猪的出生参数。路线使用桌面平面坐标；BodyRadius 为零时不参与体积避让。
/// </summary>
public sealed record PigSpawn(PigDefinition Definition, IMotionPath Path, float BodyRadius = 0, float BodyHalfLength = 0);

/// <summary>
/// 可复用的桌面路线工厂。此处定义路径形状，运动状态与避让由 PigMotion 和 PigHerd 管理。
/// </summary>
public static class HerdLayout
{
    private static void ValidateSide(float side)
    {
        if (side != 1 && side != -1)
        {
            throw new ArgumentOutOfRangeException(nameof(side));
        }
    }

    public static IMotionPath Middle(float side)
    {
        ValidateSide(side);
        return new HarmonicPath(new(0, side * .64f), new(side * 5.72f, side * .14f),
            new(.54f, 1.08f), new(MathF.PI / 2 + (side < 0 ? .055f : 0), 0));
    }

    public static IMotionPath Back(float side)
    {
        ValidateSide(side);
        return new HarmonicPath(new(.5f, -2.2f), new(side * 5.73f, side * .67f),
            new(.42f, .42f), new(.72f, .72f + MathF.PI / 2));
    }

    public static IMotionPath Front(float side)
    {
        ValidateSide(side);
        return new HarmonicPath(new(-.35f, 2.57f), new(side * 5.82f, side * .63f),
            new(.45f, .45f), new(-.62f, -.62f + MathF.PI / 2));
    }

    public static IMotionPath Side(float side)
    {
        ValidateSide(side);
        return new HarmonicPath(new(side * 4.12f, 0), new(side * 1.44f, side * 2.9f),
            new(.69f, .69f), new(.35f + MathF.PI / 2, .35f));
    }
}
