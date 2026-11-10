using Godot;

/// <summary>
/// 绘制能力卡上的二维图案，依据图案键选择画法，不解释能力的玩法效果。
/// </summary>
public partial class RewardEmblem : Control
{
    public string Emblem { get; set; } = "rim";

    private readonly Color _gold = new("cda06b");
    private readonly Color _shade = new("715535");

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw;
    }

    public override void _Draw()
    {
        float scale = Mathf.Min(Size.X, Size.Y) / 140;
        DrawSetTransform(Size / 2 - new Vector2(64, 64) * scale, 0, Vector2.One * scale);
        DrawCircle(new(64, 64), 54, new Color(0.65f, 0.39f, 0.14f, 0.06f), true, -1, true);
        switch (Emblem)
        {
            case "pair":
                Pig(new(43, 57), 0.72f);
                Pig(new(84, 78), 0.72f);
                break;
            case "return":
                Hammer();
                DrawArc(new(64, 64), 49, -2.8f, 1.0f, 32, _gold, 4, true);
                DrawColoredPolygon(new Vector2[] { new(85, 107), new(101, 94), new(104, 113) }, _gold);
                break;
            case "coins":
                for (int i = 0; i < 3; i++)
                {
                    Vector2 center = new(46 + i * 18, 79 - i * 16);
                    DrawCircle(center + new Vector2(0, 4), 22, _shade, true, -1, true);
                    DrawCircle(center, 22, _gold, true, -1, true);
                    DrawArc(center, 16, 0, Mathf.Tau, 32, _shade, 2, true);
                    DrawLine(center + new Vector2(0, -8), center + new Vector2(0, 8), _shade, 4, true);
                }
                break;
            case "cup":
                CupBody();
                DrawArc(new(84, 70), 15, -Mathf.Pi / 2, Mathf.Pi / 2, 18, _gold, 7, true);
                DrawLine(new(27, 100), new(94, 100), _gold, 4, true);
                for (int i = 0; i < 3; i++)
                {
                    DrawPolyline(new Vector2[] { new(40 + i * 17, 43), new(36 + i * 17, 34), new(42 + i * 17, 24) }, _gold, 3, true);
                }

                break;
            case "target":
                Pig(new(64, 64), 0.72f);
                DrawArc(new(64, 64), 42, 0, Mathf.Tau, 48, _gold, 2, true);
                for (int i = 0; i < 4; i++)
                {
                    Vector2 direction = Vector2.FromAngle(i * Mathf.Pi / 2);
                    DrawLine(new Vector2(64, 64) + direction * 35, new Vector2(64, 64) + direction * 53, _gold, 4, true);
                }
                break;
            default:
                DrawArc(new(64, 67), 46, 0, Mathf.Tau, 48, _shade, 2, true);
                Hammer();
                break;
        }
    }

    private void CupBody()
    {
        using var surface = WorkshopStyle.Surface(_gold, null, 0);
        surface.SetCornerRadiusAll(7);
        DrawStyleBox(surface, new Rect2(30, 51, 55, 43));
    }

    private void Hammer()
    {
        DrawLine(new(45, 100), new(78, 42), _gold, 13, true);
        DrawColoredPolygon(new Vector2[] { new(45, 31), new(56, 16), new(103, 48), new(91, 67) }, _gold);
        DrawLine(new(46, 89), new(54, 94), _shade, 3, true);
        DrawLine(new(52, 79), new(60, 84), _shade, 3, true);
    }

    private void Pig(Vector2 center, float scale)
    {
        DrawCircle(center, 29 * scale, _gold, true, -1, true);
        DrawRect(new(center + new Vector2(23, -4) * scale, new Vector2(16, 16) * scale), _gold);
        DrawColoredPolygon(new Vector2[] { center + new Vector2(1, -23) * scale, center + new Vector2(12, -40) * scale, center + new Vector2(22, -20) * scale }, _gold);
        DrawRect(new(center + new Vector2(-20, 17) * scale, new Vector2(9, 20) * scale), _gold);
        DrawRect(new(center + new Vector2(11, 17) * scale, new Vector2(9, 20) * scale), _gold);
        DrawCircle(center + new Vector2(17, -8) * scale, 2.6f * scale, _shade, true, -1, true);
        DrawLine(center + new Vector2(-8, -20) * scale, center + new Vector2(4, -20) * scale, _shade, 3, true);
    }
}
