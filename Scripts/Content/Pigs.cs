public static class PigContent
{
    public static PigRoster Roster { get; } = new(new[]
    {
        new PigArchetype(new("ceramic", "普通陶瓷猪", 1, 1, 2, "陶瓷"),
            new(), HerdLayout.Middle(1), HerdLayout.Middle(-1)),
        new PigArchetype(new("tin", "铁皮储金猪", .65f, 3, 6, "铁皮"),
            new(Body3D: "738b92", Body2D: "7b8a83", Nose2D: "9eaca2", Scale: 1.12f, Interior: "a5afb0", Metal: true),
            HerdLayout.Back(1)),
        new PigArchetype(new("swift", "金鼻快跑猪", 1.5f, 2, 4, "金鼻"),
            new(Body3D: "e8bd84", Body2D: "c4ad79", Nose2D: "deba62", Scale: .86f, Nose3D: "e0a638", Collar: true),
            HerdLayout.Front(-1))
    });
    public static PigDefinition Get(string id) => Roster.Get(id);
}
