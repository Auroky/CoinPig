using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 与引擎无关的外观数据。颜色使用六位十六进制字符串，尺寸为相对基础造型的倍率。
/// </summary>
public sealed record PigAppearance(string Body3D = "ea9a95", string Body2D = "c18d7a",
    string Nose2D = "d3a18b", float Scale = 1, string Interior = "e0cbb9",
    string? Nose3D = null, bool Metal = false, bool Collar = false, float Scale2D = 1);

/// <summary>
/// 按猪种编号查询外观的只读能力，供二维、三维和音效适配器共享。
/// </summary>
public interface IPigAppearanceSource
{
    PigAppearance GetAppearance(string speciesId);
}

/// <summary>
/// 一类猪的内容登记：规则数值、外观、路线及首次出场回合。负责将外观尺寸转换为碰撞占位。
/// </summary>
public sealed class PigArchetype
{
    public PigDefinition Definition { get; }
    public PigAppearance Appearance { get; }
    public IReadOnlyList<IMotionPath> Routes { get; }
    public int FirstShift { get; }
    // 占位覆盖模型的鼻子、发条和动作余量，并兼顾二维轮廓。
    // 外观尺寸到碰撞尺寸的转换属于内容组装，核心不读取渲染节点。
    public float BodyRadius => MathF.Max(.68f * Appearance.Scale, .67f * Appearance.Scale2D);
    public float BodyHalfLength => .5f * Appearance.Scale;

    public PigArchetype(PigDefinition definition, PigAppearance appearance, params IMotionPath[] routes)
        : this(definition, appearance, 1, routes) { }

    public PigArchetype(PigDefinition definition, PigAppearance appearance, int firstShift, params IMotionPath[] routes)
    {
        if (firstShift <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(firstShift));
        }

        FirstShift = firstShift;
        ArgumentNullException.ThrowIfNull(appearance);
        ArgumentNullException.ThrowIfNull(routes);
        _ = new PigState(definition);
        if (!float.IsFinite(appearance.Scale) || appearance.Scale <= 0 || !float.IsFinite(appearance.Scale2D) || appearance.Scale2D <= 0)
        {
            throw new ArgumentException("Pig scale must be positive and finite.", nameof(appearance));
        }

        foreach (string color in new[] { appearance.Body3D, appearance.Body2D, appearance.Nose2D, appearance.Interior, appearance.Nose3D ?? appearance.Body3D })
        {
            if (color == null || color.Length != 6 || !color.All(Uri.IsHexDigit))
            {
                throw new ArgumentException("Appearance colors must be six-digit hex values.", nameof(appearance));
            }
        }

        foreach (var route in routes)
        {
            _ = new PigMotion(new(definition, route));
        }

        Definition = definition;
        Appearance = appearance;
        Routes = Array.AsReadOnly(routes.ToArray());
    }
}

/// <summary>
/// 保存经过校验的猪种登记快照，按回合生成出生配置，同时提供只读外观查询。
/// </summary>
public sealed class PigRoster : IPigAppearanceSource
{
    private readonly IReadOnlyDictionary<string, PigArchetype> _byId;
    private readonly PigAppearance _fallback;

    public IReadOnlyList<PigArchetype> All { get; }
    public IReadOnlyList<PigSpawn> Spawns { get; }

    public PigRoster(IEnumerable<PigArchetype> pigs, PigAppearance? fallback = null)
    {
        ArgumentNullException.ThrowIfNull(pigs);
        var snapshot = pigs.ToArray();
        if (snapshot.Any(pig => pig == null) || snapshot.Select(pig => pig.Definition.Id).Distinct().Count() != snapshot.Length)
        {
            throw new ArgumentException("Pig registrations require unique IDs.", nameof(pigs));
        }

        All = Array.AsReadOnly(snapshot);
        _byId = snapshot.ToDictionary(pig => pig.Definition.Id);
        _fallback = fallback ?? new PigAppearance();
        Spawns = SpawnsForShift(1);
    }

    public IReadOnlyList<PigSpawn> SpawnsForShift(int shift)
    {
        if (shift <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shift));
        }

        return Array.AsReadOnly(All.Where(pig => pig.FirstShift <= shift)
                    .SelectMany(pig => pig.Routes.Select(route => new PigSpawn(pig.Definition, route, pig.BodyRadius, pig.BodyHalfLength))).ToArray());
    }

    public PigDefinition Get(string id) => _byId.TryGetValue(id, out var pig) ? pig.Definition :
        throw new KeyNotFoundException($"Unknown pig species: {id}");

    public PigAppearance GetAppearance(string speciesId) => _byId.TryGetValue(speciesId, out var pig) ? pig.Appearance : _fallback;
}
