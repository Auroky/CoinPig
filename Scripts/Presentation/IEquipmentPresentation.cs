/// <summary>
/// 接收装备快照并更新造型的可选表现能力。
/// </summary>
public interface IEquipmentPresentation
{
    void SetEquipment(WeaponLoadout weapon, EquipmentTier? accessory);
}

/// <summary>
/// 播放已结算装备效果的可选表现能力，不再次造成伤害或收益。
/// </summary>
public interface ICombatEffectFeedback
{
    void PresentEffect(EffectOutcome effect);
}
