using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 同一外观键的二维绘制适配器；新增图标通过登记绘制回调扩展。
/// </summary>
public sealed class EquipmentIcons
{
    private readonly Dictionary<string, Action<CanvasItem, EquipmentPalette>> _drawers = new();

    public EquipmentIcons Register(string id, Action<CanvasItem, EquipmentPalette> draw)
    {
        _drawers.Add(id, draw);
        return this;
    }

    public void Draw(CanvasItem canvas, string appearance, EquipmentPalette palette, Vector2 at, float rotation, float scale)
    {
        canvas.DrawSetTransform(at, rotation, Vector2.One * scale);
        (_drawers.TryGetValue(appearance, out var draw) ? draw : _drawers["hammer"])(canvas, palette);
        canvas.DrawSetTransform(Vector2.Zero);
    }

    public static EquipmentIcons CreateDefault() => new EquipmentIcons()
        .Register("hammer", (canvas, palette) =>
        {
            canvas.DrawLine(new(6, -2), new(39, 40), new(palette.Body), 10, true);
            canvas.DrawLine(new(25, 22), new(40, 41), new(palette.Grip), 13, true);
            canvas.DrawRect(new(-29, -22, 60, 35), new(palette.Body));
            canvas.DrawRect(new(-27, -20, 8, 31), new(palette.Metal));
            canvas.DrawRect(new(21, -20, 8, 31), new(palette.Metal));
            canvas.DrawRect(new(-3, -21, 7, 33), new(palette.Trim));
        }).Register("chainsaw", (canvas, palette) =>
        {
            canvas.DrawRect(new(-36, -20, 45, 38), new(palette.Body));
            canvas.DrawLine(new(2, 0), new(56, 0), new(palette.Metal), 20, true);
            canvas.DrawCircle(new(56, 0), 10, new(palette.Metal));
            for (int i = 0; i < 10; i++)
            {
                canvas.DrawRect(new(i * 6, -14, 3, 5), new(palette.Grip));
                canvas.DrawRect(new(i * 6, 9, 3, 5), new(palette.Grip));
            }
            canvas.DrawArc(new(-26, -10), 18, 2.2f, 5.2f, 12, new(palette.Grip), 6, true);
        }).Register("grinder", (canvas, palette) =>
        {
            canvas.DrawLine(new(-45, 0), new(4, 0), new(palette.Body), 24, true);
            canvas.DrawCircle(new(24, 0), 29, new(palette.Metal));
            canvas.DrawCircle(new(24, 0), 23, new(palette.Grip));
            canvas.DrawCircle(new(24, 0), 7, new(palette.Glow));
            canvas.DrawArc(new(24, 0), 30, Mathf.Pi, Mathf.Tau, 24, new(palette.Body), 6, true);
        }).Register("cuff", (canvas, palette) =>
        {
            canvas.DrawArc(Vector2.Zero, 25, 0, Mathf.Tau, 32, new(palette.Body), 13, true);
            canvas.DrawArc(Vector2.Zero, 29, 0, Mathf.Tau, 32, new(palette.Trim), 3, true);
            canvas.DrawCircle(Vector2.Zero, 8, new(palette.Glow));
        });
}
