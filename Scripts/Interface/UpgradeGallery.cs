using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Read-only catalogue presentation with projected, keyboard-accessible selection regions.</summary>
public partial class UpgradeGallery : Control
{
    private sealed record Choice(Node3D Model, Vector2 Extent, Button Button);
    private readonly TabletopMaterials _materials = new();
    private readonly List<Choice> _choices = new();
    private SubViewport _viewport = null!;
    private Camera3D _camera = null!;
    private Node3D _world = null!;
    private Control _hotspots = null!;
    private GalleryArt _art = null!;
    public event Action<string, int>? Purchase;
    public event Action<string>? Learn;
    public event Action<string>? Message;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _viewport = new SubViewport
        {
            OwnWorld3D = true,
            TransparentBg = false,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            Msaa3D = Viewport.Msaa.Msaa4X,
            Size = new(1600, 1000)
        };
        AddChild(_viewport);
        var image = new TextureRect { Texture = _viewport.GetTexture(), MouseFilter = MouseFilterEnum.Ignore, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize };
        AddChild(image); image.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _hotspots = new Control { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_hotspots); _hotspots.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _world = new Node3D(); _viewport.AddChild(_world);
        _art = new GalleryArt(_materials);
        _world.AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new("302a28"),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new("e2d9d7"),
                AmbientLightEnergy = .34f,
                ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled
            }
        });
        _world.AddChild(new DirectionalLight3D { RotationDegrees = new(-47, -38, 0), LightColor = new("ffe4ce"), LightEnergy = .47f, ShadowEnabled = true, ShadowOpacity = .62f });
        _world.AddChild(new OmniLight3D { Position = new(-2, 5.7f, 4), LightColor = new("ffe4bc"), LightEnergy = .70f, OmniRange = 11, ShadowEnabled = true });
        _camera = new Camera3D { Current = true, Fov = 47, Near = .1f, Far = 40, Position = new(0, 4.65f, 12.4f) };
        _world.AddChild(_camera); _camera.LookAt(new(0, 3.55f, 0));
        Resized += Arrange;
        Arrange();
    }

    private void Arrange()
    {
        if (_viewport == null || Size.X <= 0 || Size.Y <= 0) return;
        _viewport.Size = new(Mathf.CeilToInt(Size.X * 1.25f), Mathf.CeilToInt(Size.Y * 1.25f));
        foreach (var choice in _choices)
        {
            Vector3 center = choice.Model.GlobalPosition + new Vector3(0, 0, 1.2f);
            var a = Project(center + new Vector3(-choice.Extent.X / 2, choice.Extent.Y / 2, 0));
            var b = Project(center + new Vector3(choice.Extent.X / 2, -choice.Extent.Y / 2, 0));
            choice.Button.Position = a;
            choice.Button.Size = b - a;
        }
    }

    private Vector2 Project(Vector3 world) => _camera.UnprojectPosition(world) / (Vector2)_viewport.Size * Size;

    private void AddChoice(Node3D model, Vector2 extent, string name, string description, Action selected, bool disabled = false)
    {
        var button = new Button { Name = name, Flat = true, TooltipText = description, MouseDefaultCursorShape = CursorShape.PointingHand, Disabled = disabled };
        button.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        button.AddThemeStyleboxOverride("pressed", new StyleBoxEmpty());
        button.AddThemeStyleboxOverride("disabled", new StyleBoxEmpty());
        var hover = WorkshopStyle.Surface(new Color(.85f, .6f, .25f, .06f), new Color(.85f, .6f, .25f, .35f), 0);
        hover.SetCornerRadiusAll(6);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("focus", hover);
        button.MouseEntered += () => Message?.Invoke(description);
        button.FocusEntered += () => Message?.Invoke(description);
        button.Pressed += selected;
        _hotspots.AddChild(button);
        _choices.Add(new Choice(model, extent, button));
    }

    public void ShowProducts(IShopView shop)
    {
        var offers = shop.GetOffers();
        if (offers.Count == 0) return;
        int tiers = offers.Max(offer => offer.Product.MaxLevel);
        var cabinet = new Node3D(); _world.AddChild(cabinet);
        _art.Cabinet(cabinet, offers.Count, tiers);
        float columnWidth = 11.84f / offers.Count, rowHeight = 6.3f / tiers;
        float modelScale = Mathf.Min(1, Mathf.Min(2f / offers.Count, 3f / tiers));
        for (int column = 0; column < offers.Count; column++)
        {
            var offer = offers[column];
            for (int tier = 0; tier < offer.Product.MaxLevel; tier++)
            {
                int selectedTier = tier;
                var item = new Node3D { Position = new(-5.92f + (column + .5f) * columnWidth, 6.61f - (tier + .5f) * rowHeight, .1f) };
                _world.AddChild(item);
                var display = new Node3D { Scale = Vector3.One * modelScale }; item.AddChild(display);
                if (offer.Product.Appearance == "cuff") _art.Cuff(display, tier); else _art.Hammer(display, tier);
                SceneShapes.Box(display, new(0, -.68f, 1.08f), new(3, .70f, .06f), _materials.Paint("282c29"));
                string status = tier < offer.Level ? "已购入" : tier > offer.Level ? "未解锁" :
                    offer.Status == PurchaseStatus.InsufficientFunds ? $"¥{offer.Price} · 余额不足" : $"¥{offer.Price} · 点击购买";
                var caption = _art.Label(item, $"{offer.Product.Name} {tier + 1}\n{status}", new(0, -.68f * modelScale, 1.15f), 33, .01f * modelScale);
                caption.Modulate = new(tier < offer.Level ? "b6c9a1" : tier == offer.Level ? "ffe0a0" : "9a8a73");
                string description = $"{offer.Product.Name} · 第 {tier + 1} 阶    {offer.Product.Description}    {status}";
                string name = tier == offer.Level ? "Buy_" + offer.Product.Id : $"Buy_{offer.Product.Id}_Tier_{tier}";
                AddChoice(item, new(columnWidth - .82f, rowHeight - .24f), name, description, () =>
                {
                    if (selectedTier < offer.Level) Message?.Invoke("这件升级已购入并自动装备。");
                    else if (selectedTier > offer.Level) Message?.Invoke("先购买上一阶，再解锁这一阶。");
                    else Purchase?.Invoke(offer.Product.Id, selectedTier);
                });
            }
        }
        Arrange();
    }

    public void ShowSkills(ISkillTreeView tree)
    {
        var offers = tree.GetSkills();
        var board = new Node3D(); _world.AddChild(board);
        _art.Wood(board, new(0, 3.7f, -.18f), new(12, 7.1f, .30f), new("302622"));
        foreach (float x in new[] { -6.1f, 6.1f }) _art.Wood(board, new(x, 3.7f, 0), new(.20f, 7.4f, .42f), new("815737"));
        foreach (float y in new[] { .07f, 7.33f }) _art.Wood(board, new(0, y, 0), new(12.4f, .20f, .42f), new("815737"));
        var branches = offers.Select(offer => offer.Skill.Branch).Distinct().OrderBy(branch => branch.Order).ToArray();
        var positions = new Dictionary<string, Vector3>();
        foreach (var offer in offers)
        {
            var skill = offer.Skill;
            int column = Array.IndexOf(branches, skill.Branch);
            float x = -6 + (column + .5f) * 12 / branches.Length;
            positions[skill.Id] = new(x + (skill.Tier == 1 ? .16f : -.10f), 1.15f + skill.Tier * 1.82f, .15f);
        }
        for (int i = 0; i < branches.Length; i++)
            _art.Label(board, branches[i].Name, new(-6 + (i + .5f) * 12 / branches.Length, 6.76f, .15f), 43, .009f);
        foreach (var offer in offers)
        {
            var skill = offer.Skill;
            if (skill.ParentId != null && positions.TryGetValue(skill.ParentId, out var parent))
            {
                Vector3 start = parent + new Vector3(0, .55f, -.05f), end = positions[skill.Id] + new Vector3(0, -.70f, -.05f);
                Vector3 elbow = new(end.X, start.Y + .12f, start.Z);
                SceneShapes.Beam(board, start, elbow, .027f, _materials.Paint("c59d56"));
                SceneShapes.Beam(board, elbow, end, .027f, _materials.Paint("c59d56"));
            }
            var item = new Node3D { Position = positions[skill.Id] }; board.AddChild(item);
            var branch = skill.Branch;
            bool learned = offer.Status == SkillStatus.Learned, available = offer.Status == SkillStatus.Available;
            var plate = SceneShapes.Box(item, new(0, .14f, 0), new(1.02f, .80f, .13f), _materials.Paint(learned ? "498369" : available ? branch.Color : "393937"));
            plate.RotationDegrees = new(0, 0, skill.Tier == 2 ? 45 : 0);
            var icon = new Node3D { Position = new(0, .17f, .12f) }; item.AddChild(icon);
            _art.SkillIcon(icon, skill.Branch.Icon, new("dab982"));
            string status = learned ? "已学会" : offer.Status == SkillStatus.Locked ? "前置未解锁" : $"¥{skill.Cost}";
            var caption = _art.Label(item, $"{skill.Name}\n{status}", new(0, -.36f, .25f), 29, .009f);
            caption.Modulate = new(learned ? "a8d8b1" : available ? "ffdea1" : "9d9182");
            string prerequisite = skill.ParentId == null ? "" : "前置：" + (offers.FirstOrDefault(s => s.Skill.Id == skill.ParentId)?.Skill.Name ?? skill.ParentId);
            AddChoice(item, new(Mathf.Min(2.35f, 11f / Mathf.Max(1, branches.Length)), 1.50f), "Learn_" + skill.Id,
                $"{skill.Name}    {skill.Description}    ¥{skill.Cost}  {prerequisite}", () => Learn?.Invoke(skill.Id), learned);
        }
        Arrange();
    }

    public override void _ExitTree()
    {
        _art?.Dispose(); _materials.Dispose();
    }
}
