using Godot;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

/// <summary>仅在 --verify 启动参数下运行，驱动同一套按钮、瞄准与更新流程。不会注入金币或绕过规则。</summary>
internal static class ScenarioRunner
{
    private static int _checks;
    private static void Check(bool pass, string label) { if (!pass) throw new Exception(label); _checks++; GD.Print("CHECK: " + label); }
    private static void Press(Workshop g, string id)
    {
        var button = g.Interface.FindChildren(id, "Button", true, false).OfType<Button>().First(b => b.IsVisibleInTree());
        button.EmitSignal(Button.SignalName.Pressed);
    }
    private static void MovePointer(Workshop g, Vector2 position)
    {
        g.GetViewport().PushInput(new InputEventMouseMotion { Position = position, GlobalPosition = position }, true);
    }
    private static void Tick(Workshop g, float seconds) { for (int i = 0; i < (int)Math.Ceiling(seconds / .05); i++) g._Process(.05); }
    private static async Task Capture(Workshop g, string name)
    {
        if (DisplayServer.GetName() == "headless") return;
        await g.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var bounds = g.GetViewportRect();
        foreach (var control in g.Interface.FindChildren("*", "Control", true, false).OfType<Control>())
        {
            if (!control.IsVisibleInTree() || (control is not Button && control is not Label)) continue;
            var rect = control.GetGlobalRect();
            if (rect.Position.X < -1 || rect.Position.Y < -1 || rect.End.X > bounds.End.X + 1 || rect.End.Y > bounds.End.Y + 1)
                throw new Exception($"Control outside viewport in {name}: {control.Name} {rect}");
        }
        string[] args = OS.GetCmdlineUserArgs(); int at = Array.IndexOf(args, "--capture-dir");
        if (at < 0 || at + 1 >= args.Length) return;
        Directory.CreateDirectory(args[at + 1]);
        using var image = g.GetViewport().GetTexture().GetImage();
        var error = image.SavePng(Path.Combine(args[at + 1], name + ".png"));
        Check(error == Error.Ok, "capture " + name);
    }
    public static async Task Run(Workshop g)
    {
        try
        {
            await g.ToSignal(g.GetTree(), SceneTree.SignalName.ProcessFrame);
            await Capture(g, "01-intro");
            Press(g, "Start"); Check(g.Session.Phase == ShiftPhase.Working && !g.Interface.Menu.IsOpen, "start button begins playable day");
            await Capture(g, "02-table");
            if (DisplayServer.GetName() != "headless")
            {
                Check(g.GetWindow().Mode == Window.ModeEnum.Fullscreen, "starts in fullscreen");
                Press(g, "Pause");
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.F11, Pressed = true });
                await g.ToSignal(g.GetTree(), SceneTree.SignalName.ProcessFrame);
                Check(g.GetWindow().Mode == Window.ModeEnum.Windowed, "F11 returns to windowed mode");
                g.GetWindow().Size = new(1280, 800);
                await g.ToSignal(g.GetTree(), SceneTree.SignalName.ProcessFrame);
                await Capture(g, "09-window");
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.F11, Pressed = true });
                await g.ToSignal(g.GetTree(), SceneTree.SignalName.ProcessFrame);
                Check(g.GetWindow().Mode == Window.ModeEnum.Fullscreen, "F11 restores fullscreen");
                Press(g, "Resume");
            }
            Check(g.Herd.Pigs.Count == 2 && g.Herd.Pigs.All(p => p.State.Definition.Id == "ceramic"), "starts with two ordinary pigs");
            int stamina = g.Session.Stamina;
            Tick(g, .8f);
            Check(g.Session.Stamina == stamina, "starting or resuming waits for pointer movement");
            MovePointer(g, new(-100, -100)); Tick(g, .8f);
            Check(g.Session.Stamina == stamina, "outside pointer does not spend stamina");
            Press(g, "Pause");
            MovePointer(g, g.Bench.ScreenPosition(g.Herd.Pigs[0]));
            var position = g.Herd.Pigs[0].Position; Tick(g, 1);
            Check(g.Session.Stamina == stamina && g.Herd.Pigs[0].Position == position, "pause freezes automatic swings and actors");
            Press(g, "Help");
            Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = true });
            await g.ToSignal(g.GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(g.Interface.Menu.Page == MenuPage.Pause, "help returns to pause without checking text");
            Press(g, "Resume");
            var target = g.Bench.ScreenPosition(g.Herd.Pigs[0]);
            var screenTarget = g.GetViewport().GetScreenTransform() * g.GetGlobalTransformWithCanvas() * target;
            Input.ParseInputEvent(new InputEventMouseMotion { Position = screenTarget, GlobalPosition = screenTarget });
            Input.FlushBufferedEvents();
            Tick(g, .25f);
            Check(g.Session.Stamina == stamina - 1 && g.Session.ShiftPigs >= 1, "mouse movement alone lands a hit without a button press");
            Tick(g, .6f);
            Check(g.Session.Stamina == stamina - 2, "stationary pointer continues at the recovery cadence");
            MovePointer(g, new(-100, -100)); Tick(g, .8f);
            Check(g.Session.Stamina == stamina - 2, "leaving the table stops new automatic swings");
            Press(g, "EndDay"); Press(g, "ConfirmEnd");
            Check(g.Session.Phase == ShiftPhase.Shopping, "early end reaches settlement");
            await Capture(g, "03-settlement");
            var snapshot = SessionSnapshot.From(g.Session);
            int originalCash = g.Session.Wallet;
            g.Interface.Menu.ShowEquipment(new ReadOnlyStock(), snapshot);
            Check(g.Interface.FindChildren("Buy_spare", "Button", true, false).OfType<Button>().Any(b => b.IsVisibleInTree()),
                "equipment display accepts a read-only provider with no purchase methods");
            g.Interface.Menu.ShowSkills(new ReadOnlyTraining(), snapshot);
            Check(g.Interface.FindChildren("Learn_lesson", "Button", true, false).OfType<Button>().Any(b => b.IsVisibleInTree()),
                "skill display accepts a read-only provider with no learning methods");
            Check(g.Session.Wallet == originalCash, "replacing display providers cannot spend session money");
            Press(g, "Back");
            Press(g, "Equipment"); int wallet = g.Session.Wallet;
            Press(g, "Buy_Rim"); Check(g.Session.Wallet == wallet && g.Interface.Menu.NoticeText.Contains("余额不足"), "unaffordable shop action is visible and non-destructive");
            await Capture(g, "04-shop");
            Press(g, "Back"); Press(g, "Skills");
            Press(g, "Learn_power2"); Check(g.Session.Damage == 1 && g.Session.Wallet == wallet, "locked skill cannot be purchased through UI");
            await Capture(g, "05-skills");
            if (DisplayServer.GetName() != "headless")
            {
                g.ToggleFullscreen();
                g.GetWindow().Size = new(1280, 720);
                await g.ToSignal(g.GetTree(), SceneTree.SignalName.ProcessFrame);
                await Capture(g, "10-skills-wide");
                g.ToggleFullscreen();
                await g.ToSignal(g.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            Press(g, "Back"); Press(g, "Next"); Press(g, "EndDay"); Press(g, "ConfirmEnd");
            Check(g.Session.Phase == ShiftPhase.Bankrupt && g.Interface.Menu.Page == MenuPage.Receipt, "unpaid day two reaches failure screen");
            await Capture(g, "06-bankrupt"); Press(g, "Restart"); Press(g, "Start");

            int totalFrames = 0;
            bool rewardCaptured = false;
            while (g.Session.Phase != ShiftPhase.Won && totalFrames < 70000)
            {
                if (g.Session.Phase == ShiftPhase.Bankrupt) throw new Exception("heuristic bot bankrupt on day " + g.Session.Shift);
                if (g.Session.Phase == ShiftPhase.Choosing)
                {
                    if (!rewardCaptured) { await Capture(g, "11-reward"); rewardCaptured = true; }
                    var best = g.Session.Offers.OrderBy(c => c.Kind switch { Perk.PairBonus => 0, Perk.SecondWind => 1, Perk.Lunch => 2, Perk.CoinValue => 3, Perk.WideRim => 4, _ => 5 }).First();
                    Press(g, "Choose_" + best.Kind); continue;
                }
                if (g.Session.Phase == ShiftPhase.Shopping)
                {
                    // Keep the next payment covered and favour income for a small herd.
                    GD.Print($"DAY {g.Session.Shift}: income {g.Session.ShiftEarnings}, wallet {g.Session.Wallet}");
                    int reserve = g.Session.NextBill?.Amount ?? 0;
                    foreach (string id in new[] { "pairs", "coins", "endurance", "breath", "wide", "reserve" })
                    {
                        var skill = g.Session.GetSkills().First(s => s.Skill.Id == id);
                        if (skill.Status != SkillStatus.Available || g.Session.Wallet - skill.Skill.Cost < reserve) continue;
                        Press(g, "Skills"); Press(g, "Learn_" + id); Press(g, "Back");
                    }
                    var offer = g.Session.GetOffers().FirstOrDefault(o => o.Status == PurchaseStatus.Available && g.Session.Wallet - o.Price >= reserve);
                    if (offer != null) { Press(g, "Equipment"); Press(g, "Buy_" + offer.Product.Id); Press(g, "Back"); }
                    Press(g, "Next"); await g.ToSignal(g.GetTree(), SceneTree.SignalName.ProcessFrame); continue;
                }
                var visible = g.Herd.Pigs.Where(p => p.Active).ToArray();
                System.Numerics.Vector2 bestPoint = default; float bestScore = -1;
                var candidates = visible.Select(p => p.Position).Concat(visible.SelectMany(a => visible.Select(b => (a.Position + b.Position) / 2)));
                foreach (var point in candidates)
                {
                    // Allow room for the pigs to keep moving during the windup.
                    var hits = visible.Where(p => System.Numerics.Vector2.Distance(p.Position, point) <= g.Bench.Radius - .4f).Select(p => p.State);
                    float score = hits.Sum(p => p.Definition.Coins / (float)Math.Ceiling(p.Health / (float)g.Session.Damage));
                    if (score > bestScore) { bestScore = score; bestPoint = point; }
                }
                MovePointer(g, bestScore >= 4 ? g.Bench.ScreenPosition(bestPoint) : new Vector2(-100, -100));
                g._Process(.05); totalFrames++;
            }
            Check(g.Session.Phase == ShiftPhase.Won && g.Session.PaidBills == 5, "normal aiming bot completes ten-day contract");
            await Capture(g, "07-victory");
            Press(g, "Next"); Check(g.Session.IsEndless && g.Session.Shift == 11, "victory continues into endless mode");
            Press(g, "Ledger"); await Capture(g, "08-rent"); Press(g, "Back");
            GD.Print($"VERIFICATION PASS: {_checks} checks; {totalFrames} simulated frames; wallet {g.Session.Wallet}.");
            g.GetTree().Quit(0);
        }
        catch (Exception ex) { GD.PushError("VERIFICATION FAILED: " + ex); g.GetTree().Quit(1); }
    }
}
