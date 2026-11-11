using Godot;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

internal static class EquipmentScenario
{
    private static int _checks;
    private static void Check(bool pass, string label) { if (!pass) throw new Exception(label); _checks++; GD.Print("EQUIPMENT CHECK: " + label); }
    private static void Press(Workshop g, string name) => g.Interface.FindChildren(name, "Button", true, false)
        .OfType<Button>().First(b => b.IsVisibleInTree()).EmitSignal(Button.SignalName.Pressed);
    private static void Tick(Workshop g, float seconds) { for (int i = 0; i < (int)Math.Ceiling(seconds / .05f); i++) g._Process(.05); }
    private static void Move(Workshop g, Vector2 at) => g.GetViewport().PushInput(new InputEventMouseMotion
        { Position = at, GlobalPosition = at, Relative = Vector2.One }, true);
    private static void End(Workshop g)
    {
        Press(g, "EndDay"); if (g.Interface.Menu.Page == MenuPage.EndDay) Press(g, "ConfirmEnd");
        if (g.Session.Phase == ShiftPhase.Choosing)
            Press(g, "Choose_" + g.Session.Offers.First(p => p.Bonus.Refunds == 0).Id);
    }
    private static async Task Capture(Workshop g, string name)
    {
        if (DisplayServer.GetName() == "headless") return;
        string[] args = OS.GetCmdlineUserArgs(); int at = Array.IndexOf(args, "--capture-dir");
        if (at < 0 || at + 1 >= args.Length) return;
        await g.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Directory.CreateDirectory(args[at + 1]);
        using var image = g.GetViewport().GetTexture().GetImage();
        Check(image.SavePng(Path.Combine(args[at + 1], name + ".png")) == Error.Ok, "rendered " + name);
    }
    public static async Task Run(Workshop g)
    {
        try
        {
            await g.ToSignal(g.GetTree(), SceneTree.SignalName.ProcessFrame);
            EquipmentAudioChecks.Run(g, Check);
            if (DisplayServer.GetName() != "headless")
            {
                g.GetWindow().Mode = Window.ModeEnum.Windowed;
                g.GetWindow().Size = new(1280, 800);
                await g.ToSignal(g.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            Press(g, "Start"); Move(g, new(-100, -100));
            while (g.Session.CanAttack)
                g.Session.TrySwing(Enumerable.Range(0, 32).Select(_ => new PigState(PigContent.Get("ceramic"))), out _, out _);
            End(g); Press(g, "Equipment");
            Check(g.Interface.FindChildren("Buy_*", "Button", true, false).OfType<Button>().Count(b => b.IsVisibleInTree()) == 12,
                "shop renders all four product columns and three upgrade levels");
            var catalog = g.Session.GetOffers();
            Check(GameText.LockReason(catalog.Single(offer => offer.Product.Id == "Chainsaw"), catalog) ==
                $"{GameText.T("加宽锤面")} Lv.2{GameText.T("、")}{GameText.T("共振护腕")} Lv.1",
                "the UI formats prerequisite data without moving unlock rules out of the core");
            int wallet = g.Session.Wallet;
            Press(g, "Buy_Grinder");
            Check(g.Session.Wallet == wallet && g.Interface.Menu.NoticeText.StartsWith(GameText.T("解锁需要：{0}", "")), "locked final-stage equipment explains prerequisites without charging");
            await Capture(g, "01-shop-catalog");
            g.Interface.FindChildren("Buy_Stamina_Tier_1", "Button", true, false).OfType<Button>().First(b => b.IsVisibleInTree()).GrabFocus();
            await Capture(g, "01b-cuff-details");
            Press(g, "Buy_Rim"); Press(g, "Buy_Rim");
            Press(g, "Buy_Stamina");
            Check(g.Session.EquippedAccessory?.Name == "鎏金振纹护腕", "first cuff purchase routes through the real shop UI");
            Press(g, "Buy_Stamina");
            Check(g.Session.EquippedAccessory?.Name == "共振护腕", "second cuff purchase installs automatic resonance");
            Press(g, "Buy_Stamina"); Press(g, "Buy_Chainsaw"); Press(g, "Buy_Chainsaw"); Press(g, "Buy_Grinder");
            Check(g.Session.EquippedWeapon.Weapon.Id == "grinder" && g.Session.EquippedAccessory?.Name == "金鸣护腕",
                "purchasing through the real UI progresses to grinder and golden-chime cuff");
            wallet = g.Session.Wallet;
            Press(g, "Equip_Rim_Tier_1");
            Check(g.Session.EquippedWeapon.Weapon.Id == "hammer" && g.Session.Wallet == wallet, "owned hammer can be re-equipped for free from its shelf");
            Press(g, "Equip_Chainsaw_Tier_1");
            Check(g.Session.EquippedWeapon.Weapon.Id == "chainsaw" && g.Session.Wallet == wallet, "chainsaw selection changes the loadout without another purchase");
            await Capture(g, "02-shop-progression");
            Press(g, "Next");
            var renderer = ((Node)g.Bench).GetChildren().OfType<WeaponModel>().Single();
            Check(renderer.Appearance == "chainsaw", "the equipped chainsaw appears in the actual 3D game scene");
            int stamina = g.Session.Stamina;
            for (int i = 0; i < 80 && g.Session.Stamina == stamina; i++)
            {
                Move(g, g.Bench.ScreenPosition(g.Herd.Pigs.First(p => p.Active))); g._Process(.05);
            }
            Check(g.Session.Stamina == stamina - 1, "chainsaw cuts automatically with its configured one-energy cost, without a mouse button");
            var circle = (TorusMesh)renderer.GetChildren().OfType<MeshInstance3D>().Single(m => m.Mesh is TorusMesh).Mesh;
            float sawRadius = g.Session.Radius; int sawCapacity = g.Session.Capacity;
            Check(MathF.Abs(circle.OuterRadius - (sawRadius + .014f)) < .001f && sawRadius is >= 1.85f and <= 2.2f,
                $"the actual 3D chainsaw circle displays the balanced damage radius (ring={circle.OuterRadius:0.000}, damage={sawRadius:0.000})");
            await Capture(g, "03-chainsaw-gameplay");
            Move(g, new(-100, -100)); Tick(g, 1);
            stamina = g.Session.Stamina; wallet = g.Session.Wallet;
            Tick(g, 3.6f);
            Check(g.Session.Stamina == stamina && g.Session.Wallet > wallet, "golden-chime resonance cracks pigs and earns coins while the pointer is outside the table");
            await Capture(g, "04-resonance-gameplay");
            End(g); Press(g, "Equipment"); Press(g, "Equip_Grinder_Tier_0"); Press(g, "Next");
            Check(renderer.Appearance == "grinder", "switching weapons updates the in-game model and motion strategy");
            stamina = g.Session.Stamina;
            for (int i = 0; i < 80 && g.Session.Stamina == stamina; i++)
            {
                Move(g, g.Bench.ScreenPosition(g.Herd.Pigs.First(p => p.Active))); g._Process(.05);
            }
            Check(g.Session.Stamina == stamina - 1 && g.Session.Damage == 5,
                "grinder uses area-grinding damage and one-energy cost in the actual scene");
            circle = (TorusMesh)renderer.GetChildren().OfType<MeshInstance3D>().Single(m => m.Mesh is TorusMesh).Mesh;
            Check(g.Session.Radius > sawRadius && g.Session.Capacity > sawCapacity && MathF.Abs(circle.OuterRadius - (g.Session.Radius + .014f)) < .001f,
                "moving from upgraded chainsaw to grinder increases both the visible circle and energy capacity");
            await Capture(g, "05-grinder-gameplay");
            var registry = EquipmentModelRegistry.CreateDefault().Register("custom-tool", new ChainsawEquipmentBuilder());
            renderer.Models = registry;
            renderer.SetEquipment(new(new("custom", "Custom", new CircularWeaponAttack(), "custom-tool"), 2,
                EquipmentContent.Products[0].TierAt(1).Palette, null), null);
            Check(renderer.Appearance == "custom-tool", "a new registered model key is accepted without editing the weapon renderer");
            // Audio playback is released by the mixer asynchronously; drain it before the test quits.
            g.SetProcess(false);
            foreach (var audio in g.GetChildren().OfType<ImpactAudio>()) audio.Clear();
            await g.ToSignal(g.GetTree().CreateTimer(.08), SceneTreeTimer.SignalName.Timeout);
            GD.Print($"EQUIPMENT VERIFICATION PASS: {_checks} checks.");
            g.GetTree().Quit(0);
        }
        catch (Exception ex) { GD.PushError("EQUIPMENT VERIFICATION FAILED: " + ex); g.GetTree().Quit(1); }
    }
}


