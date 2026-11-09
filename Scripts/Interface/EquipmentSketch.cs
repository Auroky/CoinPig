using Godot;

public partial class EquipmentSketch : Control
{
    public string ProductId { get; set; } = "";
    public override void _Draw()
    {
        var c = Size / 2;
        DrawArc(c, 85, 0, Mathf.Tau, 64, new Color(.7f, .6f, .4f, .17f), 1, true);
        var appearance = EquipmentVisuals.For(ProductId);
        if (appearance == EquipmentAppearance.Hammer)
        {
            DrawLine(c + new Vector2(-10, -30), c + new Vector2(65, 65), new("62412c"), 24, true);
            DrawLine(c + new Vector2(-12, -33), c + new Vector2(60, 60), new("aa7849"), 15, true);
            DrawLine(c + new Vector2(37, 30), c + new Vector2(62, 64), new("342b24"), 22, true);
            DrawRect(new(c + new Vector2(-78, -69), new(130, 56)), new("86938c"));
            DrawRect(new(c + new Vector2(-70, -61), new(114, 40)), new("374840"));
            DrawRect(new(c + new Vector2(-25, -69), new(18, 56)), WorkshopStyle.Brass);
        }
        else
        {
            DrawRect(new(c + new Vector2(-67, -49), new(134, 98)), new("694b34"));
            DrawRect(new(c + new Vector2(-58, -43), new(116, 86)), new("805b3b"));
            DrawRect(new(c + new Vector2(-30, -49), new(60, 98)), new("46382a"));
            DrawRect(new(c + new Vector2(-20, -30), new(40, 60)), WorkshopStyle.Brass, false, 5);
            DrawLine(c + new Vector2(-18, 0), c + new Vector2(21, 0), WorkshopStyle.Brass, 5);
            for (int i = 0; i < 7; i++)
            {
                DrawCircle(c + new Vector2(-49 + i * 16, -36), 1, WorkshopStyle.Brass);
                DrawCircle(c + new Vector2(-49 + i * 16, 36), 1, WorkshopStyle.Brass);
            }
        }
    }
}
