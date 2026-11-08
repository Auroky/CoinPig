using Godot;
using System;

public static class WorkshopStyle
{
    public static readonly Color Text = new("eee2c9");
    public static readonly Color Muted = new("aca28e");
    public static readonly Color Brass = new("d8b36c");
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
        style.ContentMarginLeft = style.ContentMarginRight = margin;
        style.ContentMarginTop = style.ContentMarginBottom = margin;
        return style;
    }

    public static Button Button(string text, Action action, string name, bool solid = false)
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
        button.AddThemeColorOverride("font_color", solid ? Ink : Text);
        button.AddThemeColorOverride("font_hover_color", solid ? Ink : Brass);
        button.AddThemeColorOverride("font_pressed_color", solid ? Ink : Brass);
        button.AddThemeColorOverride("font_disabled_color", new("777062"));
        button.AddThemeStyleboxOverride("normal", Surface(solid ? Brass : new Color(0, 0, 0, 0), solid ? null : new Color(.75f, .65f, .46f, .3f), 10));
        button.AddThemeStyleboxOverride("hover", Surface(solid ? new("ead0a0") : new Color(.8f, .7f, .5f, .08f), Brass, 10));
        button.AddThemeStyleboxOverride("pressed", Surface(solid ? new("ad8e55") : new Color(.8f, .7f, .5f, .15f), Brass, 10));
        button.AddThemeStyleboxOverride("disabled", Surface(new Color(.2f, .18f, .14f, .3f), null, 10));
        button.AddThemeStyleboxOverride("focus", Surface(new Color(0, 0, 0, 0), Brass, 10));
        button.Pressed += action;
        return button;
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
