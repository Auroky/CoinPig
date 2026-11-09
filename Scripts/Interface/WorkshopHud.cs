using Godot;
using System;
using static WorkshopStyle;

public partial class WorkshopHud : Control
{
    public event Action? EndDay, Pause, Ledger;
    private Label _round = null!, _wallet = null!, _score = null!, _hint = null!, _impact = null!;
    private PanelContainer _roundPanel = null!, _walletPanel = null!, _scorePanel = null!, _hintPanel = null!;
    private Button _end = null!, _ledger = null!, _pause = null!;
    private Tween? _walletTween;
    private Func<Vector2>? _impactAnchor;
    private float _impactLeft, _impactAge;
    private int _lastBroken, _bestBroken;
    private bool _combo;
    public string HintText => _hint.Text;
    public Vector2 WalletAnchor => _wallet.GetGlobalRect().GetCenter();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _round = Label("", 22, Brass); _wallet = Label("", 24, Brass); _score = Label("", 22);
        _hint = Label("", 18, Muted);
        _roundPanel = Pill(_round); _walletPanel = Pill(_wallet); _scorePanel = Pill(_score); _hintPanel = Pill(_hint, 10);
        _end = Button("提前收工  [E]", () => EndDay?.Invoke(), "EndDay", false, true);
        _ledger = Button("查看账单  [B]", () => Ledger?.Invoke(), "Ledger", false, true);
        _pause = Button("暂停  [Esc]", () => Pause?.Invoke(), "Pause", false, true);
        _impact = Label("", 32, Text, true);
        _impact.HorizontalAlignment = HorizontalAlignment.Center;
        _impact.AddThemeColorOverride("font_outline_color", new("57382a"));
        _impact.AddThemeConstantOverride("outline_size", 7);
        _impact.Hide();
        foreach (var node in new Control[] { _end, _ledger, _pause, _impact }) AddChild(node);
        Resized += Arrange;
        Arrange();
        ResetFeedback();
    }

    private PanelContainer Pill(Label label, int radius = 29)
    {
        var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        var style = Surface(new Color(.13f, .10f, .075f, .8f), new Color(.68f, .49f, .27f, .42f), 12);
        style.SetCornerRadiusAll(radius);
        panel.AddThemeStyleboxOverride("panel", style);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        panel.AddChild(label); AddChild(panel); return panel;
    }

    private static void Place(Control node, float x, float y, float width, float height)
    {
        node.Position = new(x, y); node.Size = new(width, height);
    }

    private void Arrange()
    {
        float w = Size.X, h = Size.Y;
        Place(_roundPanel, 35, 28, 340, 58);
        Place(_scorePanel, w / 2 - 190, 28, 380, 58);
        Place(_walletPanel, w - 280, 28, 245, 58);
        Place(_ledger, 35, h - 86, 300, 54);
        Place(_hintPanel, 370, h - 82, Mathf.Max(260, w - 830), 47);
        Place(_pause, w - 415, h - 86, 160, 54);
        Place(_end, w - 240, h - 86, 205, 54);
        _impact.Size = new(420, 110);
    }

    public void Refresh(SessionSnapshot state)
    {
        string day = state.Endless ? $"无尽 {state.Day} 局" : $"第 {state.Day}/{GameRules.ContractLength} 局";
        _round.Text = $"{day}   体力 {state.Stamina} / {state.Capacity}";
        _wallet.Text = $"余额  ¥{state.Wallet}";
        _score.Text = $"击碎 {_lastBroken} 只   /   最佳 {_bestBroken} 只";
        if (state.NextBill is Bill bill)
        {
            int remaining = bill.Shift - state.Day;
            string due = remaining <= 0 ? "今天" : remaining == 1 ? "明天" : $"{remaining} 天后";
            _ledger.Text = $"租金 ¥{bill.Amount} · {due}  [B]";
        }
        else _ledger.Text = "租金已缴清 · 查看  [B]";
        _hint.Text = $"锤击伤害 {state.Damage} · 移动鼠标瞄准，自动挥锤";
    }

    public void Notify(string text) => _hint.Text = text;

    public void ShowImpact(Func<Vector2> anchor, SwingResult strike, int coins)
    {
        _impactAnchor = anchor;
        _lastBroken = strike.DestroyedCount;
        _bestBroken = Math.Max(_bestBroken, _lastBroken);
        _score.Text = $"击碎 {_lastBroken} 只   /   最佳 {_bestBroken} 只";
        _combo = _lastBroken >= 2;
        _impact.Text = _combo ? $"{(_lastBroken == 2 ? "双响！" : "连碎！")} ×{_lastBroken}\n+¥{coins}" :
            strike.HitCount == 0 ? "砸空了" : _lastBroken == 1 ? $"击碎 1 只  +¥{coins}" : $"命中 {strike.HitCount} 只 · 击碎 0 只";
        _impact.AddThemeFontSizeOverride("font_size", _combo ? 40 : 30);
        _impactLeft = _combo ? 1.10f : .85f;
        _impactAge = 0;
        _impact.Show();
    }

    public void PulseWallet()
    {
        if (!IsVisibleInTree()) return;
        _walletTween?.Kill();
        _wallet.PivotOffset = _wallet.Size / 2;
        _wallet.Scale = Vector2.One * 1.07f;
        _wallet.Modulate = new("ffe0a0");
        _walletTween = CreateTween().SetParallel();
        _walletTween.TweenProperty(_wallet, "scale", Vector2.One, .18).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _walletTween.TweenProperty(_wallet, "modulate", Colors.White, .18);
    }

    public void ResetFeedback()
    {
        _walletTween?.Kill();
        _lastBroken = _bestBroken = 0;
        _impactLeft = 0;
        _impact.Hide();
        _wallet.Scale = Vector2.One; _wallet.Modulate = Colors.White;
    }

    public override void _Process(double delta)
    {
        if (_impactLeft <= 0 || _impactAnchor == null) { _impact.Hide(); return; }
        float dt = (float)delta;
        _impactAge += dt; _impactLeft = Mathf.Max(0, _impactLeft - dt);
        float height = (_combo ? 140 : 100) + _impactAge * 35;
        var position = _impactAnchor() - new Vector2(_impact.Size.X / 2, height);
        position.X = Mathf.Clamp(position.X, 12, Size.X - _impact.Size.X - 12);
        position.Y = Mathf.Clamp(position.Y, 95, Size.Y - _impact.Size.Y - 12);
        _impact.Position = position;
        _impact.PivotOffset = _impact.Size / 2;
        float scale = _impactAge < .08f ? Mathf.Lerp(_combo ? .82f : .95f, _combo ? 1.13f : 1.03f, _impactAge / .08f) :
            Mathf.Lerp(_combo ? 1.13f : 1.03f, 1, Mathf.Clamp((_impactAge - .08f) / .17f, 0, 1));
        _impact.Scale = Vector2.One * scale;
        _impact.Modulate = new Color(_combo ? new Color("ffdfa0") : Colors.White, Mathf.Min(1, _impactLeft * 4.5f));
    }
}
