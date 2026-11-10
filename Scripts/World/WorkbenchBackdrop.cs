using Godot;

/// <summary>
/// 绘制二维木桌、边框和装饰背景，不保存玩法状态。
/// </summary>
public partial class WorkbenchBackdrop : Node2D
{
    private ColorRect _wood = null!;
    private Vector2 _size;

    public override void _Ready()
    {
        ZIndex = -5;
        _wood = new ColorRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Scripts/World/Wood.gdshader") },
            ZIndex = -1
        };
        AddChild(_wood);
        GetViewport().SizeChanged += Resize;
        Resize();
    }

    public override void _ExitTree() => GetViewport().SizeChanged -= Resize;

    private void Resize()
    {
        _size = GetViewportRect().Size;
        _wood.Size = _size;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Color iron = new("25261f");
        Color edge = new("585344");
        DrawRect(new(0, 0, _size.X, 18), iron);
        DrawRect(new(0, _size.Y - 21, _size.X, 21), iron);
        DrawLine(new(0, 18), new(_size.X, 18), edge, 2);
        DrawLine(new(0, _size.Y - 22), new(_size.X, _size.Y - 22), edge, 2);
        foreach (float x in new[] { 25f, _size.X - 25 })
        {
            DrawRect(new(x - 11, 17, 22, _size.Y - 38), new("3c392e"));
            for (int i = 0; i < 5; i++)
            {
                var at = new Vector2(x, 50 + (_size.Y - 100) * i / 4);
                DrawCircle(at + Vector2.One, 5, new("161811"));
                DrawCircle(at, 4, new("8f7b54"));
                DrawLine(at - new Vector2(2, 0), at + new Vector2(2, 0), new("3a3427"), 1);
            }
        }
        float y = _size.Y - 135;
        DrawLine(new(55, y), new(_size.X - 55, y), new Color(.8f, .7f, .48f, .16f), 1);
        for (int i = 0; i < (_size.X - 120) / 20; i++)
        {
            float x = 60 + i * 20;
            DrawLine(new(x, y), new(x, y + (i % 5 == 0 ? 11 : 5)), new Color(.8f, .7f, .48f, .2f), 1);
        }
        // 粉笔磨痕与杯底水印作为桌面装饰绘制，不创建可交互对象。
        DrawArc(new(_size.X - 115, _size.Y - 220), 45, .3f, 5.5f, 60, new Color(.75f, .64f, .45f, .08f), 4, true);
        DrawArc(new(_size.X - 118, _size.Y - 222), 43, 2, 6, 40, new Color(.75f, .64f, .45f, .06f), 2, true);
    }
}
