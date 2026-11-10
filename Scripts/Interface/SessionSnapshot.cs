using System.Collections.Generic;

/// <summary>
/// HUD 和菜单使用的会话展示快照。通过只读接口采集，界面不直接持有可写规则状态。
/// </summary>
public sealed record SessionSnapshot(
    ShiftPhase Phase, int Day, int ContractLength, int Wallet, int Stamina, int Capacity, int Earnings,
    int Pigs, int Damage, float Radius, bool Endless, Bill? NextBill, ShiftReceipt? Receipt,
    IReadOnlyList<RentDue> Schedule, IReadOnlyList<PerkCard> Perks,
    string WeaponName = "敲击锤", int WeaponLevel = 0, string AccessoryName = "", int AttackCost = 1)
{
    public static SessionSnapshot From(ISessionView session) => new(
        session.Phase,
        session.Shift,
        session.ContractLength,
        session.Wallet,
        session.Stamina,
        session.Capacity,
        session.ShiftEarnings,
        session.ShiftPigs,
        session.Damage,
        session.Radius,
        session.IsEndless,
        session.NextBill,
        session.Receipt,
        session.GetRentSchedule(),
        session.Offers,
        (session as IEquipmentStatusView)?.EquippedWeapon.Weapon.Name ?? "工具",
        (session as IEquipmentStatusView)?.EquippedWeapon.Level ?? 0,
        (session as IEquipmentStatusView)?.EquippedAccessory?.Name ?? "",
        (session as IEquipmentStatusView)?.StaminaCost ?? 1
    );
}
