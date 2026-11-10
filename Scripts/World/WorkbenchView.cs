using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 二维工作台适配器：映射共享坐标，按深度绘制猪和攻击提示，不推进猪群规则。
/// </summary>
public partial class WorkbenchView : Node2D, IWorkbench, IPigAppearanceConsumer, IEquipmentPresentation, ICombatEffectFeedback
{
    public IPigAppearanceSource Appearances { private get; set; } = new PigRoster(Array.Empty<PigArchetype>());

    private IReadOnlyList<PigMotion> _pigs = Array.Empty<PigMotion>();

    public Vector2 Aim { get; set; }
    public bool ShowPigs { get; set; } = true;

    private bool _showAim;

    public bool ShowAim
    {
        get => _showAim;
        set
        {
            _showAim = value;
            if (_effects != null)
            {
                _effects.Visible = value;
            }
        }
    }
    public float Radius { get; set; } = 1.43f;
    public float Windup { get; set; }
    public float Cooldown { get; set; }
    public float UnitScale => Mathf.Min(GetViewportRect().Size.X / 19.5f, GetViewportRect().Size.Y / 11f);
    public Vector2 Origin => GetViewportRect().Size / 2 + new Vector2(0, 12);
    public Rect2 PlayArea => new(Origin + new Vector2(-7.4f, -3.85f) * UnitScale, new Vector2(14.8f, 7.7f) * UnitScale);

    private readonly Dictionary<PigState, float> _flashes = new();
    private StrikeEffects _effects = null!;
    private readonly EquipmentIcons _icons = EquipmentIcons.CreateDefault();
    private WeaponLoadout _weapon = new(new("basic", "基础工具", new CircularWeaponAttack()), 0, ShopProduct.StandardPalette, null);

    public override void _Ready()
    {
        AddChild(new WorkbenchBackdrop());
        _effects = new StrikeEffects();
        AddChild(_effects);
    }

    public void Bind(IReadOnlyList<PigMotion> pigs)
    {
        _pigs = pigs;
        _flashes.Clear();
        _effects.Clear();
        Windup = Cooldown = 0;
        QueueRedraw();
    }

    public Vector2 ScreenPosition(PigMotion pig) => ScreenPosition(pig.Position);

    public Vector2 ScreenPosition(System.Numerics.Vector2 point) => Origin + new Vector2(point.X, point.Y) * UnitScale;

    public bool TryWorldPosition(Vector2 screenPoint, out System.Numerics.Vector2 worldPoint)
    {
        Vector2 local = (screenPoint - Origin) / UnitScale;
        worldPoint = new(local.X, local.Y);
        return PlayArea.HasPoint(screenPoint);
    }

    public void Tick(float delta)
    {
        foreach (var key in _flashes.Keys.ToArray())
        {
            _flashes[key] = Mathf.Max(0, _flashes[key] - delta);
        }

        _effects.Tick(delta);
        QueueRedraw();
    }

    public void Impact(System.Numerics.Vector2 point, SwingResult strike)
    {
        foreach (var hit in strike.Targets)
        {
            var pig = _pigs.FirstOrDefault(p => ReferenceEquals(p.State, hit.Pig));
            if (pig == null)
            {
                continue;
            }

            _flashes[hit.Pig] = .22f;
            if (hit.Result.Destroyed)
            {
                _effects.Break(ScreenPosition(pig), new Color(Appearances.GetAppearance(hit.Pig.Definition.Id).Body2D));
            }
        }
        _effects.Strike(ScreenPosition(point), Radius * UnitScale);
    }

    public void Present() => QueueRedraw();

    public void SetEquipment(WeaponLoadout weapon, EquipmentTier? accessory) => _weapon = weapon;

    public void PresentEffect(EffectOutcome effect)
    {
        foreach (var hit in effect.Strike.Targets)
        {
            var pig = _pigs.FirstOrDefault(p => ReferenceEquals(p.State, hit.Pig));
            if (pig == null)
            {
                continue;
            }

            _flashes[hit.Pig] = .22f;
            if (hit.Result.Destroyed)
            {
                _effects.Break(ScreenPosition(pig), new Color(Appearances.GetAppearance(hit.Pig.Definition.Id).Body2D));
            }
        }
        _effects.Strike(ScreenPosition(effect.Point), effect.Radius * UnitScale);
    }

    // 后方先画、前方后画，保证二维遮挡顺序与桌面深度一致。
    public override void _Draw()
    {
        if (ShowPigs)
        {
            foreach (var pig in _pigs.OrderBy(pig => pig.Position.Y))
            {
                if (pig.Active)
                {
                    PigPainter.Draw(this, pig, ScreenPosition(pig), UnitScale / 55, _flashes.GetValueOrDefault(pig.State), Appearances.GetAppearance(pig.State.Definition.Id));
                }
            }
        }

        if (!ShowAim || !PlayArea.HasPoint(Aim))
        {
            return;
        }

        float radius = Radius * UnitScale;
        Color ring = Windup > 0 ? new("e0b76d") : Cooldown > 0 ? new Color(.7f, .65f, .5f, .35f) : new Color(.95f, .85f, .63f, .75f);
        DrawArc(Aim, radius, 0, Mathf.Tau, 96, ring, 1.5f, true);
        DrawLine(Aim - new Vector2(5, 0), Aim + new Vector2(5, 0), ring, 1);
        DrawLine(Aim - new Vector2(0, 5), Aim + new Vector2(0, 5), ring, 1);
        _icons.Draw(this, _weapon.Weapon.Appearance, _weapon.Palette, Aim + new Vector2(20, -23 - Windup * 150), Windup * -2, UnitScale / 65);
    }

}
