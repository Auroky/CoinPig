using Godot;

public sealed record PigLook(Color Body, Color Nose, bool Metal);

public static class PigPainter
{
    public static void Draw(Node2D canvas, PigMotion pig, Vector2 position, float scale, float flash, PigAppearance appearance)
    {
        var look = new PigLook(new(appearance.Body2D), new(appearance.Nose2D), appearance.Metal);
        scale *= appearance.Scale2D;
        var body = flash > 0 ? look.Body.Lerp(Colors.White, .65f) : look.Body;
        position += new Vector2(0, Mathf.Sin(pig.Time * 9) * 1.8f * scale);
        canvas.DrawSetTransform(position + new Vector2(5, 16) * scale, 0, new Vector2(1.35f, .65f) * scale);
        canvas.DrawCircle(Vector2.Zero, 25, new Color(0, 0, 0, .32f));
        canvas.DrawSetTransform(position, 0, Vector2.One * scale);
        canvas.DrawCircle(new(-17, 16), 7, body.Darkened(.35f));
        canvas.DrawCircle(new(13, 16), 7, body.Darkened(.35f));
        canvas.DrawArc(new(-29, -1), 6, 0, Mathf.Pi * 1.65f, 16, body, 3, true);
        canvas.DrawSetTransform(position, 0, new Vector2(1.2f, 1) * scale);
        canvas.DrawCircle(Vector2.Zero, 24, body.Darkened(.4f));
        canvas.DrawCircle(new(0, -3), 22, body);
        canvas.DrawCircle(new(-7, -10), 7, new Color(1, 1, 1, .12f));
        canvas.DrawSetTransform(position, 0, Vector2.One * scale);
        canvas.DrawColoredPolygon(new[] { new Vector2(4, -18), new Vector2(10, -32), new Vector2(19, -16) }, body.Darkened(.2f));
        canvas.DrawColoredPolygon(new[] { new Vector2(-16, -17), new Vector2(-12, -29), new Vector2(-4, -20) }, body.Darkened(.15f));
        canvas.DrawCircle(new(20, 4), 12, flash > 0 ? Colors.White : look.Nose);
        canvas.DrawCircle(new(19, 3), 2, body.Darkened(.55f));
        canvas.DrawCircle(new(25, 3), 2, body.Darkened(.55f));
        canvas.DrawCircle(new(12, -8), 3, new("24231f"));
        canvas.DrawCircle(new(13, -9), 1, Colors.White);
        canvas.DrawLine(new(-8, -15), new(2, -15), body.Darkened(.6f), 3, true);
        if (look.Metal)
        {
            canvas.DrawArc(new(-2, -1), 19, 1.7f, 4.6f, 20, new("b5c0ac"), 2, true);
        }
        if (pig.State.Definition.MaxHealth > 1)
        {
            for (int i = 0; i < pig.State.Definition.MaxHealth; i++)
                canvas.DrawCircle(new(-9 + i * 9, 33), 3, i < pig.State.Health ? new("d9bd7d") : new Color(0, 0, 0, .3f));
        }
        canvas.DrawSetTransform(Vector2.Zero);
    }
}
