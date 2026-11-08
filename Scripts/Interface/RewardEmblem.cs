using Godot;

public partial class RewardEmblem : Control
{
    public Perk Kind { get; set; }
    public override void _Draw()
    {
        var c = Size / 2;
        DrawCircle(c + new Vector2(3, 4), 48, new Color(0, 0, 0, .3f));
        DrawCircle(c, 48, new("78603b"));
        DrawCircle(c, 43, new("c4a064"));
        DrawArc(c, 36, 0, Mathf.Tau, 64, new("78603b"), 2, true);
        var ink = new Color("514026");
        switch (Kind)
        {
            case Perk.PairBonus:
                DrawArc(c + new Vector2(-9, 0), 13, 0, Mathf.Tau, 32, ink, 4, true);
                DrawArc(c + new Vector2(9, 0), 13, 0, Mathf.Tau, 32, ink, 4, true);
                break;
            case Perk.SecondWind:
                DrawArc(c, 19, .1f, 5.3f, 40, ink, 5, true);
                DrawColoredPolygon(new[] { c + new Vector2(7, -25), c + new Vector2(7, -9), c + new Vector2(21, -14) }, ink);
                break;
            case Perk.CoinValue:
                DrawArc(c, 20, 0, Mathf.Tau, 40, ink, 4, true);
                DrawLine(c + new Vector2(0, -12), c + new Vector2(0, 12), ink, 5);
                break;
            case Perk.Lunch:
                DrawRect(new(c + new Vector2(-16, -8), new(29, 25)), ink, false, 4);
                DrawArc(c + new Vector2(15, 2), 8, -1.5f, 1.5f, 20, ink, 3, true);
                DrawLine(c + new Vector2(-6, -22), c + new Vector2(-6, -13), ink, 3);
                break;
            case Perk.Precision:
                DrawArc(c, 20, 0, Mathf.Tau, 40, ink, 3, true);
                DrawCircle(c, 6, ink);
                DrawLine(c - new Vector2(27, 0), c + new Vector2(27, 0), ink, 2);
                DrawLine(c - new Vector2(0, 27), c + new Vector2(0, 27), ink, 2);
                break;
            case Perk.WideRim:
                DrawRect(new(c + new Vector2(-24, -16), new(48, 19)), ink);
                DrawLine(c + new Vector2(0, 3), c + new Vector2(0, 24), ink, 7);
                break;
        }
    }
}
