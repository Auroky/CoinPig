/// <summary>
/// 猪种的基础规则数值。Speed 为速度倍率，HitRadius 为攻击判定补偿半径，与模型占位分开。
/// </summary>
public sealed record PigDefinition(string Id, string Name, float Speed, int MaxHealth,
    int Coins, string ShortName = "", float RespawnSeconds = .85f, float HitRadius = .33f);
