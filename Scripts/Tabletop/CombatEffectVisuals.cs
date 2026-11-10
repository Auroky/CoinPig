using System;
using System.Collections.Generic;

/// <summary>
/// 按效果键登记画面播放方式，避免在工作台适配器中按装备编号增加分支。
/// </summary>
public sealed class CombatEffectVisuals
{
    private readonly Dictionary<string, Action<ImpactBurst, EffectOutcome>> _visuals = new();

    public CombatEffectVisuals Register(string key, Action<ImpactBurst, EffectOutcome> render)
    {
        _visuals.Add(key, render);
        return this;
    }

    public void Present(ImpactBurst burst, EffectOutcome effect) =>
        (_visuals.TryGetValue(effect.Visual, out var render) ? render : _visuals["gold-wave"])(burst, effect);

    public static CombatEffectVisuals CreateDefault() => new CombatEffectVisuals()
        .Register("gold-wave", (burst, effect) => burst.Resonance(effect.Point, effect.Radius));
}
