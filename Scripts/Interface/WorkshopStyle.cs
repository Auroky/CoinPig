using Godot;
using System;

/// <summary>
/// 共用字体、颜色、面板和控件构建方法，集中维护界面的视觉样式。
/// </summary>
public static class WorkshopStyle
{
    public static readonly Color Text = new("f0e7d8");
    public static readonly Color Muted = new("bda994");
    public static readonly Color Brass = new("e7bb7f");
    public static readonly Color Ink = new("26221c");
    public static readonly Color Paper = new("d6c5a0");

    public static SystemFont BodyFont() => new()
    {
        FontNames = new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }
    };

    public static Theme CreateTheme() => new() { DefaultFont = BodyFont(), DefaultFontSize = 20 };

    public static Label Label(string text, int size = 20, Color? color = null, bool wrap = false)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off
        };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color ?? Text);
        return label;
    }

    public static StyleBoxFlat Surface(Color fill, Color? border = null, int margin = 16)
    {
        var style = new StyleBoxFlat { BgColor = fill, BorderColor = border ?? new Color(0, 0, 0, 0) };
        style.SetBorderWidthAll(border.HasValue ? 1 : 0);
        style.SetCornerRadiusAll(8);
        style.ContentMarginLeft = style.ContentMarginRight = margin;
        style.ContentMarginTop = style.ContentMarginBottom = margin;
        return style;
    }

    public static Button Button(string text, Action action, string name, bool solid = false, bool pill = false)
    {
        var button = new Button
        {
            Name = name,
            Text = text,
            CustomMinimumSize = new(0, 46),
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            FocusMode = Control.FocusModeEnum.All
        };
        button.AddThemeFontSizeOverride("font_size", 21);
        button.AddThemeColorOverride("font_color", solid ? Brass : Text);
        button.AddThemeColorOverride("font_hover_color", Brass);
        button.AddThemeColorOverride("font_pressed_color", Brass);
        button.AddThemeColorOverride("font_disabled_color", new("8d7a67"));
        button.AddThemeStyleboxOverride("normal", Surface(new(solid ? "62482de8" : "241d17d6"), new(solid ? "d4a369" : "785a3ba6"), 10));
        button.AddThemeStyleboxOverride("hover", Surface(new(solid ? "82603cf0" : "483521eb"), Brass, 10));
        button.AddThemeStyleboxOverride("pressed", Surface(new("392b1ff2"), Brass, 10));
        button.AddThemeStyleboxOverride("disabled", Surface(new("211c1780"), new("76614c40"), 10));
        button.AddThemeStyleboxOverride("focus", Surface(new Color(0, 0, 0, 0), Brass, 10));
        if (pill)
        {
            foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            {
                ((StyleBoxFlat)button.GetThemeStylebox(state)).SetCornerRadiusAll(27);
            }
        }

        button.Pressed += action;
        return button;
    }

    public static void StyleTab(Button button)
    {
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled" })
        {
            var style = Surface(Colors.Transparent, null, 10);
            style.BorderColor = Brass;
            style.BorderWidthBottom = state == "disabled" ? 3 : 0;
            button.AddThemeStyleboxOverride(state, style);
        }
        button.AddThemeColorOverride("font_disabled_color", Brass);
    }

    public static VBoxContainer Column(Control parent, int gap = 14)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", gap);
        parent.AddChild(box);
        return box;
    }

    public static HBoxContainer Row(Control parent, int gap = 16)
    {
        var box = new HBoxContainer();
        box.AddThemeConstantOverride("separation", gap);
        parent.AddChild(box);
        return box;
    }
}
