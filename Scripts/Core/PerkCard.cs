/// <summary>
/// 三选一能力的静态定义；加成由核心读取，图案编号由表现层解释。
/// </summary>
public sealed record PerkCard(string Id, string Name, string Description, Bonus Bonus, string Emblem = "rim");
