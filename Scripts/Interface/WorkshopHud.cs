using Godot;
using System;
using static GameText;
using static WorkshopStyle;

/// <summary>
/// 布局并刷新局内信息，以及击中、金币到账等短时提示；不执行经济结算。
/// </summary>
public partial class WorkshopHud : Control
{
    public event Action? EndDay, Pause, Ledger;

    private Label _round = null!;
    private Label _wallet = null!;
    private Label _score = null!;
    private Label _hint = null!;
    private Label _impact = null!;
    private PanelContainer _roundPanel = null!;
    private PanelContainer _walletPanel = null!;
    private PanelContainer _scorePanel = null!;
    private PanelContainer _hintPanel = null!;
    private Button _end = null!;
    private Button _ledger = null!;
    private Button _pause = null!;
    private Tween? _walletTween;
    private Func<Vector2>? _impactAnchor;
    private float _impactLeft;
    private float _impactAge;
    private int _lastBroken;
    private int _bestBroken;
    private bool _combo;

    public string HintText => _hint.Text;
    public Vector2 WalletAnchor => _wallet.GetGlobalRect().GetCenter();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _round = Label("", 20, Brass);
        _wallet = Label("", 24, Brass);
        _score = Label("", 22);
        _hint = Label("", 18, Muted);
        _roundPanel = Pill(_round);
        _walletPanel = Pill(_wallet);
        _scorePanel = Pill(_score);
        _hintPanel = Pill(_hint, 10);
        _end = Button("", () => EndDay?.Invoke(), "EndDay", false, true);
        _ledger = Button("", () => Ledger?.Invoke(), "Ledger", false, true);
        _pause = Button("", () => Pause?.Invoke(), "Pause", false, true);
        _impact = Label("", 32, Text, true);
        _impact.HorizontalAlignment = HorizontalAlignment.Center;
        _impact.AddThemeColorOverride("font_outline_color", new("57382a"));
        _impact.AddThemeConstantOverride("outline_size", 7);
        _impact.Hide();
        foreach (var node in new Control[] { _end, _ledger, _pause, _impact })
        {
            AddChild(node);
        }

        Resized += Arrange;
        Arrange();
        ResetFeedback();
        RefreshLanguage();
    }

    public void RefreshLanguage()
    {
        _end.Text = T("提前收工  [E]");
        _pause.Text = T("暂停  [Esc]");
        _impactLeft = 0;
        _impact.Hide();
    }

    private PanelContainer Pill(Label label, int radius = 29)
    {
        var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        var style = Surface(new Color(.13f, .10f, .075f, .8f), new Color(.68f, .49f, .27f, .42f), 12);
        style.SetCornerRadiusAll(radius);
        panel.AddThemeStyleboxOverride("panel", style);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        panel.AddChild(label);
        AddChild(panel);
        return panel;
    }

    private static void Place(Control node, float x, float y, float width, float height)
    {
        node.Position = new(x, y);
        node.Size = new(width, height);
    }

    private void Arrange()
    {
        float w = Size.X;
        float h = Size.Y;
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
        string day = state.Endless ? T("无尽 · 第 {0} 局", state.Day) : T("第 {0}/{1} 局", state.Day, state.ContractLength);
        _round.Text = T("{0}   体力 {1} / {2}", day, state.Stamina, state.Capacity);
        _wallet.Text = T("余额  ¥{0}", state.Wallet);
        _score.Text = T("击碎 {0} 只   /   最佳 {1} 只", _lastBroken, _bestBroken);
        if (state.NextBill is Bill bill)
        {
            int remaining = bill.Shift - state.Day;
            string due = remaining <= 0 ? T("今天") : remaining == 1 ? T("明天") : T("{0} 天后", remaining);
            _ledger.Text = T("租金 ¥{0} · {1}  [B]", bill.Amount, due);
        }
        else
        {
            _ledger.Text = T("租金已缴清 · 查看  [B]");
        }

        _hint.Text = T("{0} Lv.{1} · 伤害 {2} · 耗能 {3} · 自动攻击", T(state.WeaponName), state.WeaponLevel, state.Damage, state.AttackCost);
    }

    public void Notify(string text) => _hint.Text = text;

    public void ShowImpact(Func<Vector2> anchor, SwingResult strike, int coins)
    {
        _impactAnchor = anchor;
        _lastBroken = strike.DestroyedCount;
        _bestBroken = Math.Max(_bestBroken, _lastBroken);
        _score.Text = T("击碎 {0} 只   /   最佳 {1} 只", _lastBroken, _bestBroken);
        _combo = _lastBroken >= 2;
        _impact.Text = _combo ? $"{(_lastBroken == 2 ? T("双响！") : T("连碎！"))} ×{_lastBroken}\n+¥{coins}" :
            strike.HitCount == 0 ? T("砸空了") : _lastBroken == 1 ? T("击碎 1 只  +¥{0}", coins) : T("命中 {0} 只 · 击碎 0 只", strike.HitCount);
        _impact.AddThemeFontSizeOverride("font_size", _combo ? 40 : 30);
        _impactLeft = _combo ? 1.10f : .85f;
        _impactAge = 0;
        _impact.Show();
    }

    public void ShowEffect(Func<Vector2> anchor, EffectOutcome effect)
    {
        ShowImpact(anchor, effect.Strike, effect.Coins);
        _impact.Text = T(effect.Name) + "\n" + _impact.Text;
    }

    public void PulseWallet()
    {
        if (!IsVisibleInTree())
        {
            return;
        }

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
        _wallet.Scale = Vector2.One;
        _wallet.Modulate = Colors.White;
    }

    public override void _Process(double delta)
    {
        if (_impactLeft <= 0 || _impactAnchor == null)
        {
            _impact.Hide();
            return;
        }
        float dt = (float)delta;
        _impactAge += dt;
        _impactLeft = Mathf.Max(0, _impactLeft - dt);
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
