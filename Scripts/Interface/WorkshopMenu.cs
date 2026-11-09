using Godot;
using System;
using System.Linq;
using static WorkshopStyle;

public enum MenuPage { None, Title, Receipt, Equipment, Skills, Reward, Ledger, Pause, Help, EndDay, Restart }

/// <summary>Owns menu navigation and display. It emits commands without changing a game session.</summary>
public partial class WorkshopMenu : Control
{
    public bool SpatialCatalogs { get; set; }
    public event Action? Start, EndDay, Restart, Resume, Equipment, Skills, Fullscreen, Quit;
    public event Action<string, int>? Purchase;
    public event Action<string>? Learn;
    public event Action<Perk>? Choose;
    public MenuPage Page { get; private set; }
    public bool IsOpen => Page != MenuPage.None;
    public string NoticeText => _notice?.Text ?? "";
    private ColorRect _shade = null!;
    private VBoxContainer? _body;
    private PanelContainer? _panel;
    private Label? _notice;
    private Action? _back;
    private Vector2 _bodySize;
    private bool _titleLayout, _fullGallery;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        AddChild(new BackBufferCopy { CopyMode = BackBufferCopy.CopyModeEnum.Viewport });
        _shade = new ColorRect
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Scripts/Interface/ModalBlur.gdshader") }
        };
        AddChild(_shade);
        _shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Resized += Arrange;
        Close();
    }

    private VBoxContainer Open(MenuPage page, string heading, string subtitle, Vector2 size, Action? back = null)
    {
        _panel?.Hide();
        _panel?.QueueFree();
        Page = page;
        _back = back;
        _titleLayout = page == MenuPage.Title;
        _fullGallery = false;
        _bodySize = size;
        _panel = new PanelContainer();
        var surface = Surface(new Color(.10f,.075f,.055f,.86f), new Color(.68f,.49f,.27f,.42f), 30);
        if (page == MenuPage.Reward) { surface.BgColor = Colors.Transparent; surface.SetBorderWidthAll(0); }
        surface.SetCornerRadiusAll(16);
        surface.ShadowColor = new Color(0,0,0,.36f); surface.ShadowSize=page == MenuPage.Reward ? 0 : 20; surface.ShadowOffset=new(0,8);
        _panel.AddThemeStyleboxOverride("panel", surface);
        AddChild(_panel);
        _panel.Resized += Arrange;
        _body = Column(_panel, 18);
        var title = Label(heading, _titleLayout ? 46 : 36, Brass);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _body.AddChild(title);
        _notice = Label(subtitle, 18, Muted, true);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _body.AddChild(_notice);
        Show();
        Arrange();
        return _body;
    }

    private void Arrange()
    {
        if (_panel == null) return;
        _panel.Size = _fullGallery ? Size : _bodySize;
        _panel.Position = _fullGallery ? Vector2.Zero : new((Size.X - _panel.Size.X) / 2, Mathf.Max(12, (Size.Y - _panel.Size.Y) / 2));
    }

    public void Close()
    {
        _panel?.Hide();
        _panel?.QueueFree();
        _panel = null;
        _body = null;
        _notice = null;
        _back = null;
        Page = MenuPage.None;
        Hide();
    }

    public bool GoBack()
    {
        if (_back == null) return false;
        _back();
        return true;
    }

    public void Notify(string message)
    {
        if (_notice == null) return;
        _notice.Text = message;
        _notice.AddThemeColorOverride("font_color", Brass);
    }

    private static void Rule(Control parent)
    {
        parent.AddChild(new ColorRect { Color = new Color(.74f, .63f, .43f, .3f), CustomMinimumSize = new(0, 1), MouseFilter = MouseFilterEnum.Ignore });
    }

    private static void Back(Control parent, Action action, string text = "返回")
    {
        parent.AddChild(Button(text, action, "Back"));
    }

    public void ShowTitle()
    {
        var body = Open(MenuPage.Title, "COINPIG", "还 款 日", new(680, 540));
        body.AddChild(new Control { CustomMinimumSize = new(0, 24), MouseFilter = MouseFilterEnum.Ignore });
        body.AddChild(Button("开始游戏", () => Start?.Invoke(), "Start", true));
        body.AddChild(Button("玩法", () => ShowHelp(ShowTitle), "Help"));
        body.AddChild(Button("全屏 / 窗口   [F11]", () => Fullscreen?.Invoke(), "Fullscreen"));
        body.AddChild(Button("退出游戏", () => Quit?.Invoke(), "Quit"));
    }

    public void ShowReceipt(SessionSnapshot state)
    {
        var receipt = state.Receipt!;
        bool failed = state.Phase == ShiftPhase.Bankrupt;
        bool complete = state.Phase == ShiftPhase.Won;
        string title = failed ? "租约终止" : complete ? "账单已清" : $"第 {state.Day} 天 · 结算";
        var body = Open(MenuPage.Receipt, title, failed ? $"本期租金还差 ¥{receipt.Shortfall}" : complete ? "十天租约完成" : "今日营业结束", new(600, 570));
        Rule(body);
        AddLedgerLine(body, "击碎", $"{receipt.Pigs} 只");
        AddLedgerLine(body, "收入", $"+ ¥{receipt.Earnings}");
        AddLedgerLine(body, "租金", $"− ¥{receipt.Paid}");
        Rule(body);
        AddLedgerLine(body, "余额", $"¥ {state.Wallet}", 30);
        if (!failed && !complete)
        {
            var row = Row(body);
            var shop = Button("装备", () => Equipment?.Invoke(), "Equipment");
            var skills = Button("技能", () => Skills?.Invoke(), "Skills");
            shop.SizeFlagsHorizontal = skills.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(shop);
            row.AddChild(skills);
        }
        if (!failed) body.AddChild(Button(complete ? "继续经营" : "开始下一天", () => Start?.Invoke(), "Next", true));
        if (failed || complete) body.AddChild(Button("重新开始", () => Restart?.Invoke(), "Restart", failed));
    }

    private static void AddLedgerLine(Control parent, string label, string value, int size = 23)
    {
        var row = Row(parent);
        var left = Label(label, size, Muted);
        left.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(left);
        row.AddChild(Label(value, size));
    }

    public void ShowEquipment(IShopView shop, SessionSnapshot state)
    {
        if (SpatialCatalogs)
        {
            ShowGallery(shop, null, state);
            return;
        }
        var body = Open(MenuPage.Equipment, "装备", $"余额 ¥{shop.Wallet}", new(1000, 650), () => ShowReceipt(state));
        Rule(body);
        var row = Row(body, 34);
        foreach (var offer in shop.GetOffers())
        {
            var column = Column(row, 18);
            column.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var illustration = new EquipmentSketch { ProductId = offer.Product.Id, CustomMinimumSize = new(0, 235), MouseFilter = MouseFilterEnum.Ignore };
            column.AddChild(illustration);
            column.AddChild(Label(offer.Product.Name, 27));
            column.AddChild(Label($"等级 {offer.Level} / {offer.Product.MaxLevel}", 17, Muted));
            column.AddChild(Label(offer.Product.Description, 18, Muted, true));
            var button = Button(offer.Status == PurchaseStatus.SoldOut ? "已升满" : $"购入  ¥{offer.Price}", () => Purchase?.Invoke(offer.Product.Id, offer.Level), "Buy_" + offer.Product.Id, true);
            button.Disabled = offer.Status == PurchaseStatus.SoldOut;
            column.AddChild(button);
        }
        Back(body, () => ShowReceipt(state), "返回结算");
    }

    public void ShowSkills(ISkillTreeView skills, SessionSnapshot state)
    {
        if (SpatialCatalogs)
        {
            ShowGallery(null, skills, state);
            return;
        }
        var body = Open(MenuPage.Skills, "技能", $"余额 ¥{state.Wallet}", new(1280, 710), () => ShowReceipt(state));
        Rule(body);
        var columns = Row(body, 22);
        var offers = skills.GetSkills();
        foreach (var group in offers.GroupBy(offer => offer.Skill.Branch).OrderBy(group => group.Key))
        {
            var branch = group.Key;
            var column = Column(columns, 14);
            column.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            string name = branch switch { SkillBranch.Hammer => "锤法", SkillBranch.Stamina => "耐力", SkillBranch.Income => "收益", SkillBranch.Heavy => "重击", _ => branch.ToString() };
            column.AddChild(Label(name, 24, Brass));
            foreach (var offer in group)
            {
                var skill = offer.Skill;
                var panel = new PanelContainer { CustomMinimumSize = new(0, 150) };
                panel.AddThemeStyleboxOverride("panel", Surface(new Color(.17f, .15f, .11f, .7f), new Color(.5f, .42f, .29f, .4f), 12));
                column.AddChild(panel);
                var content = Column(panel, 6);
                content.AddChild(Label(skill.Name, 21));
                content.AddChild(Label(skill.Description, 16, Muted, true));
                if (skill.ParentId != null)
                    content.AddChild(Label("前置：" + (offers.FirstOrDefault(s => s.Skill.Id == skill.ParentId)?.Skill.Name ?? skill.ParentId), 14, Muted));
                string label = offer.Status switch { SkillStatus.Learned => "已学会", SkillStatus.Locked => "未解锁", _ => $"学习 ¥{skill.Cost}" };
                var button = Button(label, () => Learn?.Invoke(skill.Id), "Learn_" + skill.Id);
                button.CustomMinimumSize = new(0, 36);
                button.AddThemeFontSizeOverride("font_size", 17);
                button.Disabled = offer.Status == SkillStatus.Learned;
                content.AddChild(button);
            }
        }
        Back(body, () => ShowReceipt(state), "返回结算");
    }

    private void ShowGallery(IShopView? shop, ISkillTreeView? skills, SessionSnapshot state)
    {
        bool training = skills != null;
        var body = Open(training ? MenuPage.Skills : MenuPage.Equipment, "", "", Size, () => ShowReceipt(state));
        _fullGallery = true;
        _panel!.MouseFilter = MouseFilterEnum.Ignore;
        _panel.AddThemeStyleboxOverride("panel", Surface(Colors.Transparent, null, 0));
        body.MouseFilter = MouseFilterEnum.Ignore;
        body.AddThemeConstantOverride("separation", 0);
        body.GetChild<Control>(0).Hide();
        body.RemoveChild(_notice!);
        var gallery = new UpgradeGallery { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        _panel.AddChild(gallery);
        _panel.MoveChild(gallery, 0);
        var bar = new PanelContainer();
        var barStyle = Surface(new Color(.07f,.055f,.04f,.86f), null, 18);
        barStyle.SetCornerRadiusAll(0);
        bar.AddThemeStyleboxOverride("panel", barStyle);
        body.AddChild(bar);
        var tabs = Row(bar, 16);
        var back = Button("返回结算", () => ShowReceipt(state), "Back", false, true);
        back.CustomMinimumSize = new(160, 50);
        tabs.AddChild(back);
        var equipmentTab = Button("商店", () => Equipment?.Invoke(), "EquipmentTab");
        var skillTab = Button("技能树", () => Skills?.Invoke(), "SkillsTab");
        equipmentTab.CustomMinimumSize = skillTab.CustomMinimumSize = new(150, 50);
        equipmentTab.Disabled = !training; skillTab.Disabled = training;
        StyleTab(equipmentTab); StyleTab(skillTab);
        tabs.AddChild(equipmentTab); tabs.AddChild(skillTab);
        var balance = Label($"金币 ¥{state.Wallet}", 22, Brass);
        balance.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        balance.VerticalAlignment = VerticalAlignment.Center;
        tabs.AddChild(balance);
        tabs.AddChild(Button("查看租金", () => ShowLedger(state, () => { if (training) Skills?.Invoke(); else Equipment?.Invoke(); }), "ViewRent", false, true));
        tabs.AddChild(Button($"进入第 {state.Day + 1} 局", () => Start?.Invoke(), "Next", true, true));
        body.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
        var footer = new PanelContainer { CustomMinimumSize = new(0, 82) };
        footer.AddThemeStyleboxOverride("panel", barStyle);
        body.AddChild(footer);
        _notice!.Text = training ? "沿金线解锁技能，使用金币购买。" : "点击柜中的锤子或护腕购买，自上而下逐阶解锁。";
        _notice.VerticalAlignment = VerticalAlignment.Center;
        footer.AddChild(_notice);
        gallery.Purchase += (id, level) => Purchase?.Invoke(id, level);
        gallery.Learn += id => Learn?.Invoke(id);
        gallery.Message += Notify;
        if (training) gallery.ShowSkills(skills!); else gallery.ShowProducts(shop!);
        Arrange();
    }

    public void ShowRewards(SessionSnapshot state)
    {
        var body = Open(MenuPage.Reward, "选择一项能力", $"租金已付 ¥{state.Receipt?.Paid ?? 0} · 当前金币 ¥{state.Wallet}", new(1180, 620));
        var row = Row(body, 24);
        foreach (var card in state.Perks)
        {
            var button = Button("", () => Choose?.Invoke(card.Kind), "Choose_" + card.Kind);
            button.CustomMinimumSize = new(0, 410);
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            button.ClipContents = true;
            var normal = Surface(new Color(.065f,.046f,.033f,.84f), new("ab773ab0"), 0);
            normal.SetCornerRadiusAll(12);
            var hover = Surface(new Color(.28f,.18f,.075f,.90f), Brass, 0);
            hover.SetCornerRadiusAll(12);
            hover.ShadowColor = new Color(.85f,.56f,.21f,.18f); hover.ShadowSize = 12;
            button.AddThemeStyleboxOverride("normal", normal);
            button.AddThemeStyleboxOverride("hover", hover);
            row.AddChild(button);
            var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
            button.AddChild(margin); margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            foreach (string side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 22);
            var column = Column(margin, 18); column.MouseFilter = MouseFilterEnum.Ignore;
            var title = Label(card.Name, 27, Brass); title.HorizontalAlignment = HorizontalAlignment.Center;
            column.AddChild(title);
            column.AddChild(new RewardEmblem { Kind = card.Kind, CustomMinimumSize = new(0, 160), MouseFilter = MouseFilterEnum.Ignore });
            Rule(column);
            var description = Label(card.Description, 20, Text, true);
            description.CustomMinimumSize = new(0, 104);
            description.HorizontalAlignment = HorizontalAlignment.Center;
            description.VerticalAlignment = VerticalAlignment.Center;
            column.AddChild(description);
        }
    }

    public void ShowLedger(SessionSnapshot state, Action? back = null)
    {
        Action returnToPrevious = back ?? (() => Resume?.Invoke());
        var body = Open(MenuPage.Ledger, "租约", $"余额 ¥{state.Wallet}", new(650, 600), returnToPrevious);
        Rule(body);
        foreach (var due in state.Schedule)
            AddLedgerLine(body, $"第 {due.Day} 天  ·  ¥{due.Amount}", GameText.Deadline(due));
        Rule(body);
        Back(body, returnToPrevious, back == null ? "返回营业" : "返回工坊");
    }

    public void ShowPause()
    {
        var body = Open(MenuPage.Pause, "暂停", "", new(440, 420), () => Resume?.Invoke());
        body.AddChild(Button("继续游戏", () => Resume?.Invoke(), "Resume", true));
        body.AddChild(Button("玩法", () => ShowHelp(ShowPause), "Help"));
        body.AddChild(Button("全屏 / 窗口   [F11]", () => Fullscreen?.Invoke(), "Fullscreen"));
        body.AddChild(Button("重新开始", ShowRestart, "Restart"));
        body.AddChild(Button("退出游戏", () => Quit?.Invoke(), "Quit"));
    }

    private void ShowRestart()
    {
        var body = Open(MenuPage.Restart, "重新开始？", "本局的金币和升级将清空。", new(500, 270), ShowPause);
        body.AddChild(Button("重新开始", () => Restart?.Invoke(), "ConfirmRestart", true));
        Back(body, ShowPause, "取消");
    }

    public void ShowEndDay(int stamina)
    {
        var body = Open(MenuPage.EndDay, "提前收工？", $"还剩 {stamina} 点体力。收工后结算收入，并支付到期租金。", new(600, 270), () => Resume?.Invoke());
        body.AddChild(Button("收工", () => EndDay?.Invoke(), "ConfirmEnd", true));
        Back(body, () => Resume?.Invoke(), "继续营业");
    }

    private void ShowHelp(Action back)
    {
        var body = Open(MenuPage.Help, "玩法", "", new(780, 620), back);
        string pigs = string.Join("\n", PigCatalog.All.Select(pig =>
            $"{pig.Name}：{pig.MaxHealth} 点耐久，速度 ×{pig.Speed:0.##}，击碎 ¥{pig.Coins}。"));
        body.AddChild(Label($"移动鼠标瞄准，锤子会自动连续挥动，无需点击。\n空锤也消耗体力；将鼠标移出桌面可停止新的挥锤。\n\n{pigs}\n基础锤击每次造成 1 点伤害。击碎后会不断重生。\n\n第 2、4、6、8、10 天收工时交租。余额不足则破产。\n收工后可买装备、学技能。完成全部账单即获胜。", 21, Text, true));
        Back(body, back);
    }
}
