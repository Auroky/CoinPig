public sealed record PigDefinition(string Id, string Name, float Speed, int MaxHealth,
    int Coins, string ShortName = "", float RespawnSeconds = .85f, float HitRadius = .33f);