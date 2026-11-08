using Godot;
using System;
using static WorkshopStyle;

public partial class WorkshopHud : Control
{
    public event Action? EndDay, Pause, Ledger;
    private Label _day = null!, _wallet = null!, _rent = null!, _stamina = null!, _hint = null!;
    private StaminaMeter _meter = null!;
    private Button _end = null!, _ledger = null!, _pause = null!;
    private float _hintTime;
    public string HintText => _hint.Text;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _day = Label("第 1 天", 30);
        _wallet = Label("¥ 0", 40, Brass);
        _wallet.HorizontalAlignment = HorizontalAlignment.Right;
        _rent = Label("第 2 天交租  ¥28", 18, Muted);
        _rent.HorizontalAlignment = HorizontalAlignment.Right;
        _stamina = Label("体力", 17, Muted);
        _meter = new StaminaMeter { MouseFilter = MouseFilterEnum.Ignore };
        _hint = Label("", 18, Text);
        _hint.HorizontalAlignment = HorizontalAlignment.Center;
        _end = Button("收工  [E]", () => EndDay?.Invoke(), "EndDay");
        _ledger = Button("账单  [B]", () => Ledger?.Invoke(), "Ledger");
        _pause = Button("暂停  [Esc]", () => Pause?.Invoke(), "Pause");
        foreach (var control in new Control[] { _day, _wallet, _rent, _stamina, _meter, _hint, _end, _ledger, _pause }) AddChild(control);
        Resized += Arrange;
        Arrange();
    }

    private static void Place(Control control, float x, float y, float width, float height)
    {
        control.Position = new(x, y);
        control.Size = new(width, height);
    }

    private void Arrange()
    {
        float w = Size.X, h = Size.Y;
        Place(_day, 54, 38, 280, 50);
        Place(_wallet, w - 350, 31, 290, 60);
        Place(_rent, w - 410, 95, 350, 35);
        Place(_stamina, 54, h - 98, 300, 30);
        Place(_meter, 54, h - 58, 350, 18);
        Place(_hint, w / 2 - 325, h - 80, 650, 38);
        Place(_ledger, w - 486, h - 87, 132, 45);
        Place(_pause, w - 340, h - 87, 145, 45);
        Place(_end, w - 180, h - 87, 128, 45);
    }

    public void Refresh(SessionSnapshot state)
    {
        _day.Text = $"第 {state.Day} 天" + (state.Endless ? "  ·  自由经营" : "");
        _wallet.Text = $"¥ {state.Wallet}";
        _rent.Text = state.NextBill is Bill bill ? $"第 {bill.Shift} 天交租  ¥{bill.Amount}" : "租约已完成";
        _stamina.Text = $"体力   {state.Stamina} / {state.Capacity}";
        _meter.SetValue(state.Stamina, state.Capacity);
    }

    public void Notify(string text)
    {
        _hint.Text = text;
        _hintTime = 4;
        _hint.Modulate = Colors.White;
    }

    public override void _Process(double delta)
    {
        _hintTime = Mathf.Max(0, _hintTime - (float)delta);
        _hint.Modulate = new Color(1, 1, 1, Mathf.Min(1, _hintTime));
    }
}
