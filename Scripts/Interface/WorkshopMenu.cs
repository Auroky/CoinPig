using Godot;
using System;
using System.Linq;
using static WorkshopStyle;

public enum MenuPage { None, Title, Receipt, Equipment, Skills, Reward, Ledger, Pause, Help, EndDay, Restart }

/// <summary>Owns menu navigation and display. It emits commands without changing a game session.</summary>
public partial class WorkshopMenu : Control
{
    public event Action? Start, EndDay, Restart, Resume, Equipment, Skills, Fullscreen, Quit;
    public event Action<string, int>? Purchase;
    public event Action<string>? Learn;
    public event Action<Perk>? Choose;
    public MenuPage Page { get; private set; }
    public bool IsOpen => Page != MenuPage.None;
    public string NoticeText => _notice?.Text ?? "";
    private ColorRect _shade = null!;
    private VBoxContainer? _body;
    private Label? _notice;
    private Action? _back;
    private Vector2 _bodySize;
    private bool _titleLayout;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        _shade = new ColorRect { Color = new Color(.025f, .019f, .013f, .87f), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_shade);
        _shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Resized += Arrange;
        Close();
    }

    private VBoxContainer Open(MenuPage page, string heading, string subtitle, Vector2 size, Action? back = null)
    {
        _body?.QueueFree();
        if (_body != null) _body.Hide();
        Page = page;
        _back = back;
        _titleLayout = page == MenuPage.Title;
        _shade.Color = new Color(.025f, .019f, .013f, _titleLayout ? .35f : .88f);
        _bodySize = size;
        _body = Column(this, 18);
        _body.AddChild(Label(heading, _titleLayout ? 88 : 36, Brass));
        _notice = Label(subtitle, 18, Muted, true);
        _body.AddChild(_notice);
        Show();
        Arrange();
        return _body;
    }

    private void Arrange()
    {
        if (_body == null) return;
        _body.Size = _bodySize;
        _body.Position = _titleLayout ? new(Size.X * .12f, Size.Y * .24f) : (Size - _bodySize) / 2;
    }

    public void Close()
    {
        _body?.Hide();
        _body?.QueueFree();
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
        var body = Open(MenuPage.Title, "COINPIG", "还 款 日", new(430, 430));
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

    public void ShowRewards(SessionSnapshot state)
    {
        var body = Open(MenuPage.Reward, "租金已付", "领取一项奖励", new(1120, 510));
        Rule(body);
        var row = Row(body, 24);
        foreach (var card in state.Perks)
        {
            var column = Column(row, 22);
            column.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            column.AddChild(new RewardEmblem { Kind = card.Kind, CustomMinimumSize = new(0, 150), MouseFilter = MouseFilterEnum.Ignore });
            column.AddChild(Label(card.Name, 26, Brass));
            column.AddChild(Label(card.Description, 18, Text, true));
            column.AddChild(Button("选择", () => Choose?.Invoke(card.Kind), "Choose_" + card.Kind, true));
        }
    }

    public void ShowLedger(SessionSnapshot state)
    {
        var body = Open(MenuPage.Ledger, "租约", $"余额 ¥{state.Wallet}", new(650, 600), () => Resume?.Invoke());
        Rule(body);
        foreach (var due in state.Schedule)
            AddLedgerLine(body, $"第 {due.Day} 天  ·  ¥{due.Amount}", GameText.Deadline(due));
        Rule(body);
        Back(body, () => Resume?.Invoke(), "返回营业");
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
        var body = Open(MenuPage.Help, "玩法", "", new(780, 560), back);
        body.AddChild(Label("移动鼠标瞄准，锤子会自动连续挥动，无需点击。\n空锤也消耗体力；将鼠标移出桌面可停止新的挥锤。\n\n初始有两只普通陶瓷猪：1 点耐久，击碎获得 ¥2。\n它们会不断重生，交汇时可以一锤击碎两只。\n\n第 2、4、6、8、10 天收工时交租。余额不足则破产。\n收工后可买装备、学技能。完成全部账单即获胜。", 21, Text, true));
        Back(body, back);
    }
}
