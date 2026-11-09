public interface IEquipmentPresentation
{
    void SetEquipment(WeaponLoadout weapon, EquipmentTier? accessory);
}
public interface ICombatEffectFeedback
{
    void PresentEffect(EffectOutcome effect);
}
