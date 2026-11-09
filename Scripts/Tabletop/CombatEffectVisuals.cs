using System;
using System.Collections.Generic;

/// <summary>Presentation extensions register a visual key; combat and view orchestration stay unchanged.</summary>
public sealed class CombatEffectVisuals
{
    private readonly Dictionary<string, Action<ImpactBurst, EffectOutcome>> _visuals = new();
    public CombatEffectVisuals Register(string key, Action<ImpactBurst, EffectOutcome> render) { _visuals.Add(key, render); return this; }
    public void Present(ImpactBurst burst, EffectOutcome effect) =>
        (_visuals.TryGetValue(effect.Visual, out var render) ? render : _visuals["gold-wave"])(burst, effect);
    public static CombatEffectVisuals CreateDefault() => new CombatEffectVisuals()
        .Register("gold-wave", (burst, effect) => burst.Resonance(effect.Point, effect.Radius));
}
