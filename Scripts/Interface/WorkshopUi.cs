using Godot;
using System;
using System.Collections.Generic;

public partial class WorkshopUi : CanvasLayer
{
    public IReadOnlyList<PigDefinition> PigDefinitions { get; set; } = Array.Empty<PigDefinition>();
    public bool SpatialCatalogs { get; set; }
    public WorkshopHud Hud { get; private set; } = null!;
    public WorkshopMenu Menu { get; private set; } = null!;
    public override void _Ready()
    {
        Layer = 5;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Theme = WorkshopStyle.CreateTheme() };
        AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        Hud = new WorkshopHud();
        Menu = new WorkshopMenu { SpatialCatalogs = SpatialCatalogs, PigDefinitions = PigDefinitions };
        root.AddChild(Hud);
        root.AddChild(Menu);
        Hud.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        Menu.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }
}
