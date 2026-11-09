using Godot;

public partial class WorkshopUi : CanvasLayer
{
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
        Menu = new WorkshopMenu { SpatialCatalogs = SpatialCatalogs };
        root.AddChild(Hud);
        root.AddChild(Menu);
        Hud.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        Menu.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }
}
