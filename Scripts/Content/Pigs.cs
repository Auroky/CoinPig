/// <summary>
/// 默认猪种及路线配置。调节耐久、收益或出场回合不需要修改会话与绘制逻辑。
/// </summary>
public static class PigContent
{
    public static PigRoster Roster { get; } = new(new[]
    {
        new PigArchetype(
            new(
                Id: "ceramic",
                Name: "普通陶瓷猪",
                Speed: 1,
                MaxHealth: 1,
                Coins: 2,
                ShortName: "陶瓷"
            ),
            new(),
            HerdLayout.Middle(1),
            HerdLayout.Middle(-1),
            HerdLayout.Back(-1),
            HerdLayout.Front(1)
        ),
        new PigArchetype(
            new(
                Id: "tin",
                Name: "铁皮储金猪",
                Speed: .65f,
                MaxHealth: 4,
                Coins: 8,
                ShortName: "铁皮"
            ),
            new(Body3D: "738b92", Body2D: "7b8a83", Nose2D: "9eaca2", Scale: 1.12f, Interior: "a5afb0", Metal: true),
            HerdLayout.Back(1)
        ),
        new PigArchetype(
            new(
                Id: "swift",
                Name: "金鼻快跑猪",
                Speed: 1.5f,
                MaxHealth: 3,
                Coins: 7,
                ShortName: "金鼻"
            ),
            new(Body3D: "e8bd84", Body2D: "c4ad79", Nose2D: "deba62", Scale: .86f, Nose3D: "e0a638", Collar: true),
            HerdLayout.Front(-1)
        ),
        new PigArchetype(
            new(
                Id: "armored",
                Name: "装甲储金猪",
                Speed: .5f,
                MaxHealth: 10,
                Coins: 20,
                ShortName: "装甲"
            ),
            new(
                Body3D: "414d60",
                Body2D: "526075",
                Nose2D: "ad9264",
                Scale: 1.22f,
                Scale2D: 1.15f,
                Interior: "8f9aaa",
                Nose3D: "b79a65",
                Metal: true,
                Collar: true
            ),
            6,
            HerdLayout.Side(-1)
        )
    });

    public static PigDefinition Get(string id) => Roster.Get(id);
}
