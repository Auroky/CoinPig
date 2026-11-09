using Godot;
using System;
using System.Collections.Generic;

/// <summary>Optional flat adapter for the same appearance keys. New keys register a drawing delegate.</summary>
public sealed class EquipmentIcons
{
    private readonly Dictionary<string, Action<CanvasItem, EquipmentPalette>> _drawers = new();
    public EquipmentIcons Register(string id, Action<CanvasItem, EquipmentPalette> draw) { _drawers.Add(id, draw); return this; }
    public void Draw(CanvasItem canvas, string appearance, EquipmentPalette palette, Vector2 at, float rotation, float scale)
    {
        canvas.DrawSetTransform(at, rotation, Vector2.One * scale);
        (_drawers.TryGetValue(appearance, out var draw) ? draw : _drawers["hammer"])(canvas, palette);
        canvas.DrawSetTransform(Vector2.Zero);
    }
    public static EquipmentIcons CreateDefault() => new EquipmentIcons()
        .Register("hammer", (c, p) => {
            c.DrawLine(new(6, -2), new(39, 40), new(p.Body), 10, true);
            c.DrawLine(new(25, 22), new(40, 41), new(p.Grip), 13, true);
            c.DrawRect(new(-29, -22, 60, 35), new(p.Body));
            c.DrawRect(new(-27, -20, 8, 31), new(p.Metal)); c.DrawRect(new(21, -20, 8, 31), new(p.Metal));
            c.DrawRect(new(-3, -21, 7, 33), new(p.Trim));
        }).Register("chainsaw", (c, p) => {
            c.DrawRect(new(-36, -20, 45, 38), new(p.Body));
            c.DrawLine(new(2, 0), new(56, 0), new(p.Metal), 20, true);
            c.DrawCircle(new(56, 0), 10, new(p.Metal));
            for (int i = 0; i < 10; i++) { c.DrawRect(new(i * 6, -14, 3, 5), new(p.Grip)); c.DrawRect(new(i * 6, 9, 3, 5), new(p.Grip)); }
            c.DrawArc(new(-26, -10), 18, 2.2f, 5.2f, 12, new(p.Grip), 6, true);
        }).Register("grinder", (c, p) => {
            c.DrawLine(new(-45, 0), new(4, 0), new(p.Body), 24, true);
            c.DrawCircle(new(24, 0), 29, new(p.Metal)); c.DrawCircle(new(24, 0), 23, new(p.Grip));
            c.DrawCircle(new(24, 0), 7, new(p.Glow)); c.DrawArc(new(24, 0), 30, Mathf.Pi, Mathf.Tau, 24, new(p.Body), 6, true);
        }).Register("cuff", (c, p) => {
            c.DrawArc(Vector2.Zero, 25, 0, Mathf.Tau, 32, new(p.Body), 13, true);
            c.DrawArc(Vector2.Zero, 29, 0, Mathf.Tau, 32, new(p.Trim), 3, true);
            c.DrawCircle(Vector2.Zero, 8, new(p.Glow));
        });
}
