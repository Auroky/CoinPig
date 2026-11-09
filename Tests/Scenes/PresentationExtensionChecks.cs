using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>Extension smoke checks exercise both real adapters with IDs absent from built-in content.</summary>
internal static class PresentationExtensionChecks
{
    private sealed class Stock : IShopView
    {
        public int Wallet => 10;
        public IReadOnlyList<ShopOffer> GetOffers() => new[]
        {
            new ShopOffer(new("new-wrap-id", "Custom wrap", "", 1, 0, 1, new(Damage: 1), "cuff"), 0, 1, PurchaseStatus.Available)
        };
    }
    private sealed class Training : ISkillTreeView
    {
        public IReadOnlyList<SkillOffer> GetSkills() => new[]
        {
            new SkillOffer(new("new-lesson-id", "Custom training", "", null,
                new("new-branch-id", "Custom branch", "55aa88", 10, "cuff"), 0, 1, new()), SkillStatus.Available)
        };
    }
    public static async Task Run(Workshop game, Action<bool, string> check)
    {
        var appearance = new PigAppearance(Body3D: "339977", Body2D: "55aa88", Nose2D: "88ccaa",
            Scale: 1.2f, Metal: true, Collar: true);
        var roster = new PigRoster(new[] { new PigArchetype(new("custom-pig-id", "Custom pig", 1, 2, 5), appearance,
            new HarmonicPath(System.Numerics.Vector2.Zero, System.Numerics.Vector2.Zero,
                System.Numerics.Vector2.Zero, System.Numerics.Vector2.Zero)) });
        foreach (Node adapter in new Node[] { new TabletopView(), new WorkbenchView() })
        {
            var viewport = new SubViewport { Size = new(1600, 900), OwnWorld3D = true };
            game.AddChild(viewport); viewport.AddChild(adapter);
            ((IPigAppearanceConsumer)adapter).Appearances = roster;
            var view = (IWorkbench)adapter;
            var herd = new PigHerd(roster.Spawns);
            view.Bind(herd.Pigs); view.ShowPigs = true; view.ShowAim = true;
            view.Aim = view.ScreenPosition(System.Numerics.Vector2.Zero);
            view.Tick(.05f); view.Present();
            if (adapter is TabletopView)
            {
                var hammer = adapter.GetChildren().OfType<WeaponModel>().Single();
                hammer.LandAt(System.Numerics.Vector2.Zero);
                hammer.Present(new System.Numerics.Vector2(3, 0), 1.43f, 0, .39f, .01f, true);
                check(hammer.VisualPosition.X > .1f, "3D hammer follows the next target immediately during recovery without an impact-position hold");
            }
            if (adapter is TabletopView)
            {
                var model = adapter.GetChildren().OfType<PigModel>().Single();
                check(model.Glaze == new Color(appearance.Body3D) && model.BodyScale == appearance.Scale,
                    "3D adapter uses injected appearance for a species absent from built-in content");
            }
            var strike = StrikeResolver.Resolve(new[] { herd.Pigs[0].State }, 1);
            herd.Apply(strike); view.Impact(System.Numerics.Vector2.Zero, strike); view.Tick(.05f); view.Present();
            check(herd.Pigs[0].Active && herd.Pigs[0].State.Health == 1,
                adapter.GetType().Name + " supports nonlethal feedback for unknown species");
            strike = StrikeResolver.Resolve(new[] { herd.Pigs[0].State }, 1);
            herd.Apply(strike); view.Impact(System.Numerics.Vector2.Zero, strike); herd.Advance(1);
            view.Tick(1); view.Present();
            check(herd.Pigs[0].Active && herd.Pigs[0].State.Health == 2,
                adapter.GetType().Name + " supports destruction and respawn for unknown species");
            viewport.QueueFree();
            await game.ToSignal(game.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        var snapshot = SessionSnapshot.From(game.Session);
        int visits = 0, observedWallet = game.Session.Wallet;
        var feedback = new CompositeStrikeFeedback(new IStrikeFeedback[]
        {
            new ActionStrikeFeedback(_ => visits++), new ActionStrikeFeedback(_ => visits++)
        });
        feedback.Present(new(System.Numerics.Vector2.Zero, StrikeResolver.Resolve(Array.Empty<PigState>(), 1), 0));
        check(visits == 2 && game.Session.Wallet == observedWallet,
            "additional presentation observers compose without changing the simulation or granting money");
        game.Interface.Menu.ShowEquipment(new Stock(), snapshot);
        check(game.Interface.FindChildren("Buy_new-wrap-id", "Button", true, false).OfType<Button>().Any(b => b.IsVisibleInTree()),
            "equipment screens accept a new product ID with configured appearance");
        game.Interface.Menu.ShowSkills(new Training(), snapshot);
        check(game.Interface.FindChildren("Learn_new-lesson-id", "Button", true, false).OfType<Button>().Any(b => b.IsVisibleInTree()),
            "skill screens accept a new branch without a branch-name registry");
        game.Interface.Menu.ShowRewards(snapshot with { Perks = new[] { new PerkCard("new-perk-id", "Custom reward", "", new(), "pair") } });
        check(game.Interface.FindChildren("Choose_new-perk-id", "Button", true, false).OfType<Button>().Any(b => b.IsVisibleInTree()),
            "reward screens accept arbitrary perk IDs and reuse a configured emblem");
    }
}
