using Godot;
using System;

/// <summary>Connects the session, workbench, input and interface.</summary>
public partial class Workshop : Node2D
{
    internal GameSession Session { get; private set; } = new();
    internal PigHerd Herd { get; private set; } = new(HerdLayout.Initial);
    internal WorkbenchView Bench { get; private set; } = null!;
    internal WorkshopUi Interface { get; private set; } = null!;
    private readonly HammerSwing _hammer = new();
    private ImpactAudio _audio = null!;
    private float _settleDelay;
    private Vector2 _pointerPosition = new(-1, -1);
    private SessionSnapshot Snapshot => SessionSnapshot.From(Session);

    public override void _Ready()
    {
        Bench = new WorkbenchView();
        _audio = new ImpactAudio();
        Interface = new WorkshopUi();
        AddChild(Bench);
        AddChild(_audio);
        AddChild(Interface);
        Bench.Bind(Herd.Pigs);
        var menu = Interface.Menu;
        var hud = Interface.Hud;
        menu.Start += Begin;
        menu.EndDay += RequestEnd;
        menu.Restart += Restart;
        menu.Resume += Resume;
        menu.Equipment += OpenEquipment;
        menu.Skills += OpenSkills;
        menu.Purchase += Purchase;
        menu.Learn += Learn;
        menu.Choose += Choose;
        menu.Fullscreen += ToggleFullscreen;
        menu.Quit += () => GetTree().Quit();
        hud.EndDay += RequestEnd;
        hud.Pause += Pause;
        hud.Ledger += () => menu.ShowLedger(Snapshot);
        GetWindow().FocusExited += OnFocusLost;
        Refresh();
        menu.ShowTitle();
        hud.Hide();
#if DEBUG
        if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--verify") >= 0)
            _ = ScenarioRunner.Run(this);
#endif
    }

    public override void _ExitTree() => GetWindow().FocusExited -= OnFocusLost;
    private void OnFocusLost()
    {
        if (Session.Phase == ShiftPhase.Working && !Interface.Menu.IsOpen) Pause();
    }
    private void Refresh() => Interface.Hud.Refresh(Snapshot);
    private void OpenEquipment() => Interface.Menu.ShowEquipment(Session, Snapshot);
    private void OpenSkills() => Interface.Menu.ShowSkills(Session, Snapshot);

    private void Purchase(string id, int level)
    {
        var result = Session.TryBuy(id, level);
        Refresh();
        OpenEquipment();
        Interface.Menu.Notify(GameText.Purchase(result));
    }
    private void Learn(string id)
    {
        var result = Session.TryLearn(id);
        Refresh();
        OpenSkills();
        Interface.Menu.Notify(GameText.Skill(result));
    }
    private void Choose(Perk perk)
    {
        if (!Session.Choose(perk)) return;
        Refresh();
        Interface.Menu.ShowReceipt(Snapshot);
    }
    private void Begin()
    {
        if (!Session.BeginShift()) return;
        _hammer.Reset();
        _settleDelay = 0;
        _pointerPosition = new(-1, -1);
        ResetHerd();
        Bench.Radius = 1.43f + Session.ExtraRadius;
        Interface.Menu.Close();
        Interface.Hud.Show();
        Refresh();
        if (Session.Shift == 1) Interface.Hud.Notify("移动鼠标瞄准 · 锤子自动挥动");
    }
    private void Restart()
    {
        Session = new GameSession();
        _hammer.Reset();
        _settleDelay = 0;
        ResetHerd();
        Refresh();
        Interface.Menu.ShowTitle();
    }
    private void Pause()
    {
        if (Session.Phase == ShiftPhase.Working && !Interface.Menu.IsOpen) Interface.Menu.ShowPause();
    }
    private void ResetHerd()
    {
        Herd = new PigHerd(HerdLayout.Initial);
        Bench.Bind(Herd.Pigs);
    }
    private void Resume()
    {
        _pointerPosition = new(-1, -1);
        Interface.Menu.Close();
        Interface.Hud.Visible = Session.Phase == ShiftPhase.Working;
    }
    private void RequestEnd()
    {
        if (Session.Phase != ShiftPhase.Working) return;
        if (_hammer.Pending)
        {
            Interface.Hud.Notify("等待落锤完成");
            return;
        }
        if (Session.Stamina > 0 && Interface.Menu.Page != MenuPage.EndDay)
            Interface.Menu.ShowEndDay(Session.Stamina);
        else Settle();
    }
    private void Settle()
    {
        if (Session.Phase != ShiftPhase.Working) return;
        _hammer.Reset();
        _settleDelay = 0;
        Session.CloseShift();
        Interface.Hud.Hide();
        Refresh();
        if (Session.Phase == ShiftPhase.Choosing) Interface.Menu.ShowRewards(Snapshot);
        else Interface.Menu.ShowReceipt(Snapshot);
        Bench.ShowAim = false;
    }

    internal void ToggleFullscreen()
    {
        var window = GetWindow();
        if (window.Mode == Window.ModeEnum.Fullscreen || window.Mode == Window.ModeEnum.ExclusiveFullscreen)
        {
            window.Mode = Window.ModeEnum.Windowed;
            window.Size = new(1280, 720);
            window.MoveToCenter();
        }
        else window.Mode = Window.ModeEnum.Fullscreen;
    }

    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseMotion motion)
        {
            _pointerPosition = motion.Position;
            return;
        }
        if (input is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.Keycode == Key.F11)
        {
            ToggleFullscreen();
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.Escape)
        {
            if (!Interface.Menu.GoBack()) Pause();
            GetViewport().SetInputAsHandled();
        }
        else if (Session.Phase == ShiftPhase.Working && !Interface.Menu.IsOpen)
        {
            if (key.Keycode == Key.E) RequestEnd();
            if (key.Keycode == Key.B) Interface.Menu.ShowLedger(Snapshot);
        }
    }
    public override void _Process(double delta)
    {
        float dt = (float)Math.Min(delta, .05);
        bool active = Session.Phase == ShiftPhase.Working && !Interface.Menu.IsOpen;
        Interface.Hud.Visible = active;
        Bench.ShowAim = active;
        Bench.ShowPigs = Session.Phase != ShiftPhase.Ready;
        if (active)
        {
            Herd.Advance(dt);
            Bench.Tick(dt);
            if (Session.Stamina > 0 && Bench.PlayArea.HasPoint(_pointerPosition))
                _hammer.Begin(_pointerPosition);
            if (_hammer.Advance(dt, Session.RecoveryReduction) &&
                Session.TrySwing(Herd.Targets(Bench.WorldPosition(_hammer.Aim), Bench.Radius), out var result, out int coins))
            {
                Herd.Apply(result!);
                Bench.Impact(_hammer.Aim, result!, coins);
                _audio.Play(result!.HitCount, coins);
                Refresh();
                if (Session.Stamina == 0) _settleDelay = .75f;
            }
            if (_settleDelay > 0)
            {
                _settleDelay -= dt;
                if (_settleDelay <= 0) Settle();
            }
        }
        Bench.Aim = _hammer.Pending ? _hammer.Aim : _pointerPosition;
        Bench.Windup = _hammer.Windup;
        Bench.Cooldown = _hammer.Cooldown;
        Bench.QueueRedraw();
    }
}
