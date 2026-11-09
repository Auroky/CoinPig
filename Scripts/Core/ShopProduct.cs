public sealed record ShopProduct(string Id, string Name, string Description, int BasePrice, int PriceStep,
    int MaxLevel, Bonus Bonus, string Appearance = "hammer")
{
    public int PriceAt(int level) => checked(BasePrice + PriceStep * level);
}