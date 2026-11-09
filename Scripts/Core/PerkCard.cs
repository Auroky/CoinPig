public sealed record Bill(int Shift, int Amount);
public sealed record RentDue(int Day, int Amount, bool Paid, int DaysAway, bool Failed, bool Renewal = false);
public sealed record PerkCard(string Id, string Name, string Description, Bonus Bonus, string Emblem = "rim");
