using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>Connects the session, workbench, input and interface.</summary>
public partial class Workshop : Node2D
{
    [Export] public bool SpatialCatalogs { get; set; }
    public GameContent Content { get; init; } = DefaultGame.Content;
    public IReadOnlyList<IStrikeFeedback> AdditionalFeedback { get; init; } = Array.Empty<IStrikeFeedback>();
    private WorkshopRun _run = null!;
    internal GameSession Session => _run.Session;
    internal PigHerd Herd => _run.Herd;
    internal IWorkbench Bench { get; private set; } = null!;
    internal WorkshopUi Interface { get; private set; } = null!;
    private ImpactAudio _audio = null!;
    private IRewardFeedback? _rewardFeedback;
    private IStrikeFeedback _strikeFeedback = null!;
    private Vector2 _pointerPosition = new(-1, -1);
    private SessionSnapshot Snapshot => SessionSnapshot.From(Session);

    public override void _Ready()
    {
        _run = new WorkshopRun(Content);
        Bench = GetNode("Workbench") as IWorkbench ??
            throw new InvalidOperationException("The scene needs a workbench presentation.");
        if (Bench is IPigAppearanceConsumer appearanceConsumer) appearanceConsumer.Appearances = Content.Pigs;
        _audio = new ImpactAudio { Appearances = Content.Pigs };
        Interface = new WorkshopUi { SpatialCatalogs = SpatialCatalogs,
            PigDefinitions = Content.Pigs.All.Select(pig => pig.Definition).ToArray() };
        AddChild(_audio);
        AddChild(Interface);
        _strikeFeedback = new CompositeStrikeFeedback(new IStrikeFeedback[]
        {
            new ActionStrikeFeedback(outcome => Bench.Impact(outcome.Point, outcome.Strike)),
            new ActionStrikeFeedback(outcome => _audio.PlayImpact(outcome.Strike)),
            new ActionStrikeFeedback(outcome => Interface.Hud.ShowImpact(
                () => Bench.ScreenPosition(outcome.Point), outcome.Strike, outcome.Coins))
        }.Concat(AdditionalFeedback));
        Bench.Bind(Herd.Pigs);
        _rewardFeedback = Bench as IRewardFeedback;
        if (_rewardFeedback != null) _rewardFeedback.CoinCollected += Interface.Hud.PulseWallet;
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
    private void Choose(string perk)
    {
        if (!Session.Choose(perk)) return;
        Refresh();
        Interface.Menu.ShowReceipt(Snapshot);
    }
    private void Begin()
    {
        if (!_run.BeginShift()) return;
        _audio.Clear();
        _pointerPosition = new(-1, -1);
        BindHerd();
        Bench.Radius = Session.Radius;
        Interface.Menu.Close();
        Interface.Hud.Show();
        Refresh();
        if (Session.Shift == 1) Interface.Hud.Notify("移动鼠标瞄准 · 锤子自动挥动");
    }
    private void Restart()
    {
        _run.Restart();
        _audio.Clear();
        Interface.Hud.ResetFeedback();
        BindHerd();
        Refresh();
        Interface.Menu.ShowTitle();
    }
    private void Pause()
    {
        if (Session.Phase == ShiftPhase.Working && !Interface.Menu.IsOpen) Interface.Menu.ShowPause();
    }
    private void BindHerd()
    {
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
        if (_run.Hammer.Pending)
        {
            Interface.Hud.Notify("等待落锤完成");
            return;
        }
        if (Session.Stamina > 0 && Interface.Menu.Page != MenuPage.EndDay)
            Interface.Menu.ShowEndDay(Session.Stamina);
        else if (_run.EndShift()) ShowSettlement();
    }
    private void ShowSettlement()
    {
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
            if (!motion.Relative.IsZeroApprox()) _pointerPosition = motion.Position;
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
        if (_rewardFeedback != null) _rewardFeedback.CoinDestination = Interface.Hud.WalletAnchor;
        bool overTable = Bench.TryWorldPosition(_pointerPosition, out var worldPoint);
        var mouseMode = active && overTable ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;
        if (Input.MouseMode != mouseMode) Input.MouseMode = mouseMode;
        if (!active) _audio.StopSwing();
        if (active)
        {
            Bench.Tick(dt);
            bool startsSwing = Session.Stamina > 0 && overTable && !_run.Hammer.Pending && _run.Hammer.Cooldown <= 0;
            var outcome = _run.Advance(dt, overTable ? worldPoint : null);
            if (startsSwing) _audio.PlaySwing();
            if (outcome != null)
            {
                _strikeFeedback.Present(outcome);
                Refresh();
            }
            if (Session.Phase != ShiftPhase.Working) ShowSettlement();
        }
        Bench.Aim = _run.Hammer.Pending ? Bench.ScreenPosition(_run.StrikePoint) : _pointerPosition;
        Bench.Windup = _run.Hammer.Windup;
        Bench.Cooldown = _run.Hammer.Cooldown;
        Bench.Present();
    }
}
