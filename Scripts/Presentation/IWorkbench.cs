using Godot;
using System.Collections.Generic;
using WorldPoint = System.Numerics.Vector2;

/// <summary>
/// 工作台适配器组合：屏幕投影与表现能力。2D 和 3D 都呈现相同的共享模型。
/// </summary>
public interface IWorkbench : IWorkbenchProjection, IWorkbenchPresentation
{
}

/// <summary>
/// 桌面坐标与屏幕坐标的转换。桌面 Vector2 的 Y 对应三维世界的 Z；拾取失败返回 false。
/// </summary>
public interface IWorkbenchProjection
{
    bool TryWorldPosition(Vector2 screenPoint, out WorldPoint worldPoint);
    Vector2 ScreenPosition(WorldPoint worldPoint);
    Vector2 ScreenPosition(PigMotion pig);
}

/// <summary>
/// 向表现适配器提供猪种外观查询，适配器不依赖默认猪种目录。
/// </summary>
public interface IPigAppearanceConsumer
{
    IPigAppearanceSource Appearances { set; }
}

/// <summary>
/// 接收猪群、工具姿态和已结算反馈的显示能力。Tick 只推进表现时间，不推进游戏规则。
/// </summary>
public interface IWorkbenchPresentation
{
    /// <summary>当前屏幕瞄准位置，由鼠标输入或锁定落点的投影提供。</summary>
    Vector2 Aim { get; set; }
    bool ShowPigs { get; set; }
    bool ShowAim { get; set; }
    /// <summary>攻击范围的世界空间半径，不是屏幕像素。</summary>
    float Radius { get; set; }
    /// <summary>剩余起手时间，单位为秒。</summary>
    float Windup { get; set; }
    /// <summary>剩余恢复时间，单位为秒。</summary>
    float Cooldown { get; set; }
    void Bind(IReadOnlyList<PigMotion> pigs);
    void Tick(float delta);
    void Impact(WorldPoint point, SwingResult strike);
    void Present();
}
