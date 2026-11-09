using Godot;
using System.Collections.Generic;
using WorldPoint = System.Numerics.Vector2;

/// <summary>Projects the shared tabletop model and presents its outcomes without changing rules.</summary>
public interface IWorkbench
{
    Vector2 Aim { get; set; }
    bool ShowPigs { get; set; }
    bool ShowAim { get; set; }
    float Radius { get; set; }
    float Windup { get; set; }
    float Cooldown { get; set; }
    void Bind(IReadOnlyList<PigMotion> pigs);
    bool TryWorldPosition(Vector2 screenPoint, out WorldPoint worldPoint);
    Vector2 ScreenPosition(WorldPoint worldPoint);
    Vector2 ScreenPosition(PigMotion pig);
    void Tick(float delta);
    void Impact(WorldPoint point, SwingResult strike);
    void Present();
}
