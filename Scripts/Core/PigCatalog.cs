using System;
using System.Collections.Generic;
using System.Linq;

public sealed record PigDefinition(string Id, string Name, float Speed, int MaxHealth,
    int Coins, string ShortName = "");

/// <summary>猪种的速度、耐久和奖励配置。外观由表现层管理。</summary>
public static class PigCatalog
{
    public static IReadOnlyList<PigDefinition> All { get; } = Array.AsReadOnly(new PigDefinition[]
    {
        new("ceramic", "普通陶瓷猪", 1.0f, 1, 2, "陶瓷"),
        new("tin", "铁皮储金猪", 0.65f, 3, 6, "铁皮"),
        new("swift", "金鼻快跑猪", 1.5f, 2, 4, "金鼻")
    });

    public static PigDefinition Get(string id) => All.Single(pig => pig.Id == id);
}
