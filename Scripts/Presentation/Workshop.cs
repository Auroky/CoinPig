using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Godot 场景入口：组装运行流程与表现、转换输入坐标、连接界面命令，再呈现规则结果。
/// </summary>
public partial class Workshop : Node2D
{
    [Export]
    public bool SpatialCatalogs { get; set; }
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
        CreatePresentation();
        ConnectInterfaceCommands();
        RefreshHud();
        Interface.Menu.ShowTitle();
        Interface.Hud.Hide();
#if DEBUG
        if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--verify-equipment") >= 0)
        {
            _ = EquipmentScenario.Run(this);
        }
        else if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--verify") >= 0)
        {
            _ = ScenarioRunner.Run(this);
        }
#endif
    }

    // 场景入口负责组装：规则对象不认识节点，表现对象也不决定本局的经济规则。
    private void CreatePresentation()
    {
        Bench = GetNode("Workbench") as IWorkbench ??
            throw new InvalidOperationException("The scene needs a workbench presentation.");
        if (Bench is IPigAppearanceConsumer appearanceConsumer)
        {
            appearanceConsumer.Appearances = Content.Pigs;
        }

        _audio = new ImpactAudio { Appearances = Content.Pigs };
        Interface = new WorkshopUi
        {
            SpatialCatalogs = SpatialCatalogs,
            PigDefinitions = Content.Pigs.All.Select(pig => pig.Definition).ToArray()
        };
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
        if (_rewardFeedback != null)
        {
            _rewardFeedback.CoinCollected += Interface.Hud.PulseWallet;
        }
    }

    // 界面发出操作意图；这里转交给会话，再把结果刷新回界面。
    private void ConnectInterfaceCommands()
    {
        var menu = Interface.Menu;
        var hud = Interface.Hud;
        menu.Start += BeginShift;
        menu.EndDay += RequestEndShift;
        menu.Restart += RestartRun;
        menu.Resume += ResumeRun;
        menu.Equipment += OpenEquipment;
        menu.Skills += OpenSkills;
        menu.Purchase += PurchaseEquipment;
        menu.Equip += EquipWeapon;
        menu.Learn += LearnSkill;
        menu.Choose += ChoosePerk;
        menu.Fullscreen += ToggleFullscreen;
        menu.Quit += () => GetTree().Quit();
        hud.EndDay += RequestEndShift;
        hud.Pause += PauseRun;
        hud.Ledger += () => menu.ShowLedger(Snapshot);
        GetWindow().FocusExited += HandleFocusLost;
    }

    public override void _ExitTree() => GetWindow().FocusExited -= HandleFocusLost;

    private void HandleFocusLost()
    {
        if (Session.Phase == ShiftPhase.Working && !Interface.Menu.IsOpen)
        {
            PauseRun();
        }
    }

    private void RefreshHud() => Interface.Hud.Refresh(Snapshot);

    private void OpenEquipment() => Interface.Menu.ShowEquipment(Session, Snapshot);

    private void OpenSkills() => Interface.Menu.ShowSkills(Session, Snapshot);

    private void PurchaseEquipment(string id, int level)
    {
        var result = Session.TryBuy(id, level);
        RefreshHud();
        OpenEquipment();
        Interface.Menu.Notify(GameText.Purchase(result));
    }

    private void EquipWeapon(string id)
    {
        var result = Session.TryEquip(id);
        RefreshHud();
        OpenEquipment();
        Interface.Menu.Notify(result.Succeeded ? $"已装备 {Session.EquippedWeapon.Weapon.Name} · 无需额外金币" : "尚未拥有这件武器");
    }

    private void LearnSkill(string id)
    {
        var result = Session.TryLearn(id);
        RefreshHud();
        OpenSkills();
        Interface.Menu.Notify(GameText.Skill(result));
    }

    private void ChoosePerk(string perk)
    {
        if (!Session.Choose(perk))
        {
            return;
        }

        RefreshHud();
        Interface.Menu.ShowReceipt(Snapshot);
    }

    private void BeginShift()
    {
        if (!_run.BeginShift())
        {
            return;
        }

        _audio.Clear();
        _pointerPosition = new(-1, -1);
        BindHerdPresentation();
        Bench.Radius = Session.Radius;
        Interface.Menu.Close();
        Interface.Hud.Show();
        RefreshHud();
        var arrivals = Content.Pigs.All.Where(pig => pig.FirstShift == Session.Shift && pig.FirstShift > 1).ToArray();
        Interface.Hud.Notify(arrivals.Length > 0
            ? string.Join(" · ", arrivals.Select(pig => $"{pig.Definition.Name}出现 · 耐久 {pig.Definition.MaxHealth}"))
            : "开局准备 · 瞄准一只猪后自动挥锤，无需点击");
    }

    private void RestartRun()
    {
        _run.Restart();
        _audio.Clear();
        Interface.Hud.ResetFeedback();
        BindHerdPresentation();
        RefreshHud();
        Interface.Menu.ShowTitle();
    }

    private void PauseRun()
    {
        if (Session.Phase == ShiftPhase.Working && !Interface.Menu.IsOpen)
        {
            Interface.Menu.ShowPause();
        }
    }

    private void BindHerdPresentation()
    {
        Bench.Bind(Herd.Pigs);
        if (Bench is IEquipmentPresentation equipment)
        {
            equipment.SetEquipment(Session.EquippedWeapon, Session.EquippedAccessory);
        }

        _audio.SetWeapon(Session.EquippedWeapon);
    }

    private void ResumeRun()
    {
        _pointerPosition = new(-1, -1);
        Interface.Menu.Close();
        Interface.Hud.Visible = Session.Phase == ShiftPhase.Working;
    }

    private void RequestEndShift()
    {
        if (Session.Phase != ShiftPhase.Working)
        {
            return;
        }

        if (_run.Hammer.Pending)
        {
            Interface.Hud.Notify("等待本次攻击完成");
            return;
        }
        if (Session.CanAttack && Interface.Menu.Page != MenuPage.EndDay)
        {
            Interface.Menu.ShowEndDay(Session.Stamina);
        }
        else if (_run.EndShift())
        {
            ShowSettlement();
        }
    }

    private void ShowSettlement()
    {
        Interface.Hud.Hide();
        RefreshHud();
        if (Session.Phase == ShiftPhase.Choosing)
        {
            Interface.Menu.ShowRewards(Snapshot);
        }
        else
        {
            Interface.Menu.ShowReceipt(Snapshot);
        }

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
        else
        {
            window.Mode = Window.ModeEnum.Fullscreen;
        }
    }

    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseMotion motion)
        {
            if (!motion.Relative.IsZeroApprox())
            {
                _pointerPosition = motion.Position;
            }

            return;
        }
        if (input is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        if (key.Keycode == Key.F11)
        {
            ToggleFullscreen();
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.Escape)
        {
            if (!Interface.Menu.GoBack())
            {
                PauseRun();
            }

            GetViewport().SetInputAsHandled();
        }
        else if (Session.Phase == ShiftPhase.Working && !Interface.Menu.IsOpen)
        {
            if (key.Keycode == Key.E)
            {
                RequestEndShift();
            }

            if (key.Keycode == Key.B)
            {
                Interface.Menu.ShowLedger(Snapshot);
            }
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)Math.Min(delta, .05);
        // 暂停或打开菜单时停止推进规则，但仍更新画面显示与鼠标模式。
        bool active = Session.Phase == ShiftPhase.Working && !Interface.Menu.IsOpen;
        Interface.Hud.Visible = active;
        Bench.ShowAim = active;
        Bench.ShowPigs = Session.Phase != ShiftPhase.Ready;
        if (_rewardFeedback != null)
        {
            _rewardFeedback.CoinDestination = Interface.Hud.WalletAnchor;
        }

        bool overTable = Bench.TryWorldPosition(_pointerPosition, out var worldPoint);
        var mouseMode = active && overTable ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;
        if (Input.MouseMode != mouseMode)
        {
            Input.MouseMode = mouseMode;
        }

        if (!active)
        {
            _audio.StopSwing();
        }

        if (active)
        {
            Bench.Tick(dt);
            bool wasPending = _run.Hammer.Pending;
            // 返回值已经完成扣血和收益结算。下面只播放反馈，不再修改钱包。
            var outcome = _run.Advance(dt, overTable ? worldPoint : null);
            if (!wasPending && _run.Hammer.Pending)
            {
                _audio.PlaySwing();
            }

            if (!_run.AutomaticAim.HasLanded && !_run.Hammer.Pending)
            {
                Interface.Hud.Notify(_run.AutomaticAim.OpeningRemaining > 0
                                ? $"开局准备 {_run.AutomaticAim.OpeningRemaining:0.0}s · 移动鼠标瞄准"
                                : "瞄准一只猪，工具会自动攻击 · 无需点击");
            }

            if (outcome != null)
            {
                _strikeFeedback.Present(outcome);
                RefreshHud();
            }
            foreach (var effect in _run.EffectOutcomes)
            {
                if (Bench is ICombatEffectFeedback feedback)
                {
                    feedback.PresentEffect(effect);
                }

                _audio.PlayEffect(effect);
                if (effect.Coins > 0)
                {
                    Interface.Hud.ShowEffect(() => Bench.ScreenPosition(effect.Point), effect);
                }

                RefreshHud();
            }
            if (Session.Phase != ShiftPhase.Working)
            {
                ShowSettlement();
            }
        }
        Bench.Aim = _run.IsAimLocked ? Bench.ScreenPosition(_run.StrikePoint) : _pointerPosition;
        Bench.Windup = _run.Hammer.Windup;
        Bench.Cooldown = _run.Hammer.Cooldown;
        Bench.Present();
    }
}
