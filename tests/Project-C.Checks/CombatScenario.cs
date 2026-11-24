using MySharpVenture;
using MySharpVenture.Pantallas;
using Spectre.Console;

internal static class CombatScenario
{
    public static int Run(ItemCatalog catalog, bool multipleWeapons = false,
        bool guaranteedHits = false, CombatAction? quickhack = null, bool sandevistan = false, bool bot = false,
        bool shield = false, bool critical = false, CyberwareDefinition? arms = null, bool control = false,
        bool melee = false)
    {
        var origin = AnsiConsole.Prompt(new SelectionPrompt<OriginPath>()
            .Title("Elegí el origen del combate de prueba:")
            .AddChoices(Enum.GetValues<OriginPath>()));
        var state = new GameState();
        state.CreateCharacter(origin, catalog);
        var player = state.Player!;
        if (multipleWeapons)
        {
            var factory = new ItemFactory(catalog);
            var second = factory.CreateWeapon("pistola-t1");
            player.Inventory.Add(second);
            player.Equipment.TryEquip(second, out _);
        }

        var profile = DevelopmentContent.MaelstromTest;
        if (melee)
        {
            var sword = new ItemFactory(catalog).CreateWeapon("espada-t1");
            player.Inventory.Add(sword);
            if (!player.Equipment.TryUnequip(player.Equipment.Weapons.Single(), out _)
                || !player.Equipment.TryEquip(sword, out _))
                return 1;
            profile = profile with { MaxHealth = 16 };
        }
        if (sandevistan || shield || arms is not null)
        {
            var expedition = new Expedition(ExpeditionContent.Watson, player,
                Enumerable.Range(0, 5).Select(_ => new EventOccurrence(EventKind.Combat, [], profile)));
            state.StartExpedition(expedition);
            do { expedition.ResolveCurrent(); } while (expedition.Advance());
            state.CompleteExpedition(expedition);
            var implant = arms ?? (shield ? CyberwareContent.SubdermalArmor : CyberwareContent.Sandevistan);
            if (!state.TryBuyImplant(implant, out _)
                || (sandevistan && !state.TryExtractImplant(CyberwareZone.OperatingSystem, out _))
                || !state.TryInstallImplant(player.SpareCyberware.Single(x => x == implant), out _))
                return 1;
            if (shield)
            {
                var second = new Expedition(ExpeditionContent.Watson, player,
                    Enumerable.Range(0, 5).Select(_ => new EventOccurrence(EventKind.Discovery, [])));
                state.StartExpedition(second);
                do { second.ResolveCurrent(); } while (second.Advance());
                state.CompleteExpedition(second);
            }
            profile = profile with { MaxHealth = arms is not null ? 23 : 24 };
        }
        if (quickhack is { } action)
        {
            var cyberware = action == CombatAction.CyberwareMalfunction;
            var execution = action == CombatAction.Suicide;
            for (var i = 0; i < (cyberware || execution ? 3 : 1); i++)
            {
                var expedition = new Expedition(ExpeditionContent.Watson, player,
                    Enumerable.Range(0, 5).Select(_ => new EventOccurrence(EventKind.Discovery, [])));
                state.StartExpedition(expedition);
                do { expedition.ResolveCurrent(); } while (expedition.Advance());
                state.CompleteExpedition(expedition);
            }
            if (!state.TryBuyQuickhack(action, out _) || !player.TryConfigureQuickhack(action, true, out _))
                return 1;
            if (execution)
            {
                if (!state.TryBuyExDisk(out _) || !state.TryInstallImplant(player.SpareCyberware.Single(), out _))
                    return 1;
                player.RestoreRam();
            }
            profile = profile with { MaxHealth = action == CombatAction.Overheat ? 18 : 26 };
            if (cyberware)
            {
                if (!state.TryBuyImplant(CyberwareContent.SelfIce, out _)
                    || !state.TryInstallImplant(player.SpareCyberware.Single(), out _))
                    return 1;
                profile = profile with { Name = "Netrunner", Netrunner = true };
            }
            if (action == CombatAction.WeaponGlitch)
            {
                var pistol = (WeaponDefinition)catalog.Get("pistola-t1");
                profile = profile with { Loadout = new(pistol,
                    (ConsumableDefinition)catalog.Get(pistol.AmmoCatalogItemId), 1, 1) };
            }
        }
        if (bot)
            profile = profile with { Name = "Centinela", Affiliation = "Barghest", IsBot = true, MaxHealth = 18 };
        if (control)
            profile = profile with { Name = "Netrunner", Netrunner = true, ControlQuickhacks = true, MaxHealth = 12 };
        var combat = new CombatController(player, profile,
            DevelopmentContent.BasicAttack, guaranteedHits ? new AlwaysHits(critical) : new Random());
        var status = new CombatePantalla(combat).Show();
        Console.WriteLine(status == CombatStatus.Victory
            ? "Combate de prueba finalizado: victoria."
            : "Combate de prueba finalizado: derrota.");
        return 0;
    }

    private sealed class AlwaysHits(bool critical) : Random
    {
        public override double NextDouble() => 0;
        public override int Next(int maxValue) => critical ? 0 : maxValue - 1;
    }
}
