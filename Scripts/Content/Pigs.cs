public static class PigContent
{
    public static PigRoster Roster { get; } = new(new[]
    {
        new PigArchetype(new("ceramic", "普通陶瓷猪", 1, 1, 2, "陶瓷"),
            new(), HerdLayout.Middle(1), HerdLayout.Middle(-1), HerdLayout.Back(-1), HerdLayout.Front(1)),
        new PigArchetype(new("tin", "铁皮储金猪", .65f, 4, 8, "铁皮"),
            new(Body3D: "738b92", Body2D: "7b8a83", Nose2D: "9eaca2", Scale: 1.12f, Interior: "a5afb0", Metal: true),
            HerdLayout.Back(1)),
        new PigArchetype(new("swift", "金鼻快跑猪", 1.5f, 3, 7, "金鼻"),
            new(Body3D: "e8bd84", Body2D: "c4ad79", Nose2D: "deba62", Scale: .86f, Nose3D: "e0a638", Collar: true),
            HerdLayout.Front(-1)),
        new PigArchetype(new("armored", "装甲储金猪", .5f, 10, 20, "装甲"),
            new(Body3D: "414d60", Body2D: "526075", Nose2D: "ad9264", Scale: 1.22f,
                Scale2D: 1.15f, Interior: "8f9aaa", Nose3D: "b79a65", Metal: true, Collar: true),
            6, HerdLayout.Side(-1))
    });
    public static PigDefinition Get(string id) => Roster.Get(id);
}
