using Spectre.Console;

namespace MySharpVenture.Pantallas;

public sealed class Aventura(GameState state, ItemCatalog catalog,
    Func<Random>? randomFactory = null) : IPantalla
{
    internal const string WithdrawLabel = "Retirarse de la aventura";
    private readonly Equipamento _inventoryScreen = new(state);
    private readonly CuracionPantalla _healingScreen = new(state);
    private readonly RecargaPantalla _reloadScreen = new(state);
    private readonly ProgressionPantalla _progressionScreen = new(state);

    public PantallaId Show()
    {
        if (state.Player is not { } player)
            return PantallaId.Creacion;

        var random = randomFactory?.Invoke() ?? new Random();
        if (state.ActiveExpedition is null)
        {
            if (player.IsOverweight)
            {
                UiHelpers.MostrarTitulo();
                UiHelpers.MostrarCarga(player);
                UiHelpers.Pausar();
                return PantallaId.MenuPrincipal;
            }
            var zones = ExpeditionContent.Zones.Select(zone => new ZoneOption(zone,
                $"{zone.Name} · {zone.Affiliation} · enemigos nivel {zone.MinLevel}–{zone.MaxLevel}"))
                .Append(new ZoneOption(null, "Volver a preparación"));
            var selected = AnsiConsole.Prompt(UiHelpers.Menu<ZoneOption>("Elegí una zona:", player.Origin)
                .UseConverter(option => option.Text)
                .AddChoices(zones));
            if (selected.Zone is not { } zone)
                return PantallaId.MenuPrincipal;
            state.StartExpedition(ExpeditionContent.Create(zone, random, catalog, player));
            Transiciones.Barra($"Rumbo a {zone.Name}…", Color.Teal, 1800);
        }
        var expedition = state.ActiveExpedition!;
        var zoneName = Markup.Escape(expedition.Zone.Name);
        var factory = new ItemFactory(catalog);

        while (expedition.Status == ExpeditionStatus.Active)
        {
            var current = expedition.Current;
            if (!current.Resolved)
            {
                var outcome = RunEvent(expedition, player, random);
                if (outcome == CombatStatus.Withdrawn)
                    return Withdraw(expedition);
                if (outcome == CombatStatus.Defeat)
                {
                    state.RemoveDeadCharacter(player);
                    return PantallaId.Creacion;
                }
            }

            UiHelpers.MostrarTitulo();
            if (player.IsOverweight)
                UiHelpers.MostrarCarga(player);
            UiHelpers.MostrarPanel($"Pausa en {zoneName}",
                $"[teal]{zoneName} · Evento {expedition.Position + 1}/5 resuelto[/]\n"
                + $"[red]Salud: {player.CurrentHealth}/{player.MaxHealth}[/]"
                + $"\nNivel: {player.Level}\nXP: {player.Experience}"
                + $"\nPuntos: {player.AttributePoints}/{player.SkillPoints}"
                + $"\nEurodólares: {player.Money}"
                + (expedition.Position + 1 < expedition.Events.Count
                    ? $"\nSiguiente evento: {EventName(expedition.Events[expedition.Position + 1].Kind)}" : ""),
                UiHelpers.ColorOrigen(player.Origin));
            PauseAction[] actions = [PauseAction.Inventory, PauseAction.Heal,
                PauseAction.Reload, PauseAction.Progression, PauseAction.Continue, PauseAction.Withdraw];
            if (current.Kind != EventKind.Merchant)
            {
                MostrarBotin(current);
                actions = [PauseAction.Loot, .. actions];
            }
            var choice = AnsiConsole.Prompt(UiHelpers.Menu<PauseAction>(
                    "Pausa: elegí qué hacer antes de continuar.", player.Origin)
                .UseConverter(action => action switch
                {
                    PauseAction.Loot => "Tomar botín",
                    PauseAction.Inventory => "Inventario y equipo",
                    PauseAction.Heal => "Usar curación",
                    PauseAction.Reload => "Recargar arma",
                    PauseAction.Progression => "Atributos y habilidades",
                    PauseAction.Withdraw => WithdrawLabel,
                    _ => "Continuar"
                })
                .AddChoices(actions));
            switch (choice)
            {
                case PauseAction.Loot:
                    TomarBotin(expedition, factory);
                    break;
                case PauseAction.Inventory:
                    _inventoryScreen.Manage(player, "Volver a aventura");
                    break;
                case PauseAction.Heal:
                    _healingScreen.Use(player);
                    break;
                case PauseAction.Reload:
                    _reloadScreen.Use(player);
                    break;
                case PauseAction.Progression:
                    _progressionScreen.Manage(player, "Volver a aventura");
                    break;
                case PauseAction.Continue:
                    if (!expedition.Advance())
                    {
                        state.CompleteExpedition(expedition);
                        UiHelpers.MostrarPanel("Victoria",
                             $"Completaste los cinco eventos de {zoneName}."
                            + " Regresás a preparación con salud completa.", Color.Green);
                        UiHelpers.Pausar();
                        return PantallaId.MenuPrincipal;
                    }
                    break;
                case PauseAction.Withdraw:
                    if (ConfirmWithdrawal(player))
                        return Withdraw(expedition);
                    break;
            }
        }
        throw new InvalidOperationException("La aventura terminó sin cerrar su sesión.");
    }

    private static CombatStatus RunEvent(Expedition expedition, Person player, Random random)
    {
        var current = expedition.Current;
        var zoneName = Markup.Escape(expedition.Zone.Name);
        var moneyBefore = player.Money;
        UiHelpers.MostrarTitulo();
        if (expedition.Position > 0)
            Transiciones.Barra($"Abriendo evento {expedition.Position + 1}/5", Color.Teal, 700);
        UiHelpers.MostrarPanel(zoneName,
            $"{zoneName} · Evento {expedition.Position + 1}/5", Color.Teal);
        if (expedition.Position > 0)
        {
            var recovery = expedition.LastRecovery;
            UiHelpers.MostrarPanel("Recuperación entre eventos",
                $"+{recovery.Stamina} stamina"
                + $" · [{UiHelpers.ShieldColor}]+{recovery.Shield} escudo[/]"
                + (player.Cyberdeck is not null ? $" · [{UiHelpers.RamColor}]+{recovery.Ram} RAM[/]" : ""), Color.Teal);
        }
        if (current.Kind == EventKind.Combat)
        {
            UiHelpers.MostrarPanel("Encuentro",
                $"Aparece {Markup.Escape(current.Enemy!.Name)}"
                + $" de {Markup.Escape(current.Enemy.Affiliation)} (nivel {current.Enemy.Level}).", Color.Red);
            UiHelpers.Pausar();
            var combat = new CombatController(player, current.Enemy,
                DevelopmentContent.BasicAttack, random);
            var outcome = new CombatePantalla(combat).Show(allowWithdrawal: true);
            if (outcome != CombatStatus.Victory)
                return outcome;
        }
        else if (current.Kind == EventKind.Discovery)
        {
            UiHelpers.MostrarPanel("Hallazgo", "Encontraste un escondite con objetos.", Color.Yellow);
            UiHelpers.Pausar();
        }
        else
            VisitMerchant(expedition);
        expedition.ResolveCurrent();
        if (expedition.LastExperienceReward is { } reward)
            UiHelpers.MostrarPanel("Experiencia ganada",
                $"+{reward.Gained} XP · Nivel del personaje {player.Level}"
                + (player.Level < 20
                    ? $" · faltan {player.ExperienceToNextLevel - player.Experience} XP"
                    : " · nivel máximo")
                + (reward.LevelsGained > 0
                    ? $"\n¡Subiste {reward.LevelsGained} nivel(es)! "
                      + $"+{reward.LevelsGained} punto(s) de atributo y "
                      + $"+{reward.LevelsGained} punto(s) de habilidad."
                    : "")
                + $"\n+{player.Money - moneyBefore} eurodólares.", Color.Green);
        return CombatStatus.Victory;
    }

    internal static bool ConfirmWithdrawal(Person player)
    {
        UiHelpers.MostrarPanel("Confirmar retirada", "Perderás todas las armas transportadas, "
            + "sus accesorios montados, su munición cargada y tu dinero."
            + "\nNo recuperarás salud ni RAM. El stash no cambia.", Color.Yellow);
        return AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Confirmás la retirada?", player.Origin)
            .AddChoices("Cancelar", "Confirmar retirada")) == "Confirmar retirada";
    }

    private PantallaId Withdraw(Expedition expedition)
    {
        var player = expedition.Player;
        var weapons = player.Inventory.Items.OfType<Weapon>().ToArray();
        var loaded = weapons.Sum(weapon => (long)weapon.LoadedAmmo);
        var money = player.Money;
        state.WithdrawExpedition(expedition);
        UiHelpers.MostrarPanel("Retirada de la aventura",
            $"Perdiste {weapons.Length} arma(s), {loaded} bala(s) cargadas y {money} eurodólares."
            + "\nVolvés a preparación sin recuperación.", Color.Yellow);
        UiHelpers.Pausar();
        return PantallaId.MenuPrincipal;
    }

    private static string EventName(EventKind kind) => kind switch
    {
        EventKind.Combat => "combate",
        EventKind.Discovery => "hallazgo",
        _ => "comerciante"
    };

    private static void VisitMerchant(Expedition expedition)
    {
        while (true)
        {
            UiHelpers.MostrarPanel("Comerciante de aventura",
                $"Eurodólares: {expedition.Player.Money}", Color.Teal);
            var options = expedition.Current.Offers.Where(offer => offer.Stock > 0)
                .Select(offer => new OfferOption(offer, $"{Markup.Escape(offer.Definition.Name)}"
                    + (offer.Definition is WeaponDefinition weapon ? $" · Tier {weapon.Tier} · cargador vacío" : "")
                    + $" · {offer.UnitPrice} eurodólares/u · stock {offer.Stock}"
                    + (offer.Definition is WeaponDefinition { Technology: { } technology } ? $" · {technology}" : "")
                    + (offer.Definition is CybergearStashDefinition { Quickhack: { } hack }
                        && expedition.Player.OwnsQuickhack(hack) ? " · ya lo tenés" : "")))
                .Append(new OfferOption(null, "Dejar al comerciante"));
            var selected = AnsiConsole.Prompt(UiHelpers.Menu<OfferOption>(
                    "¿Qué querés comprar?", expedition.Player.Origin)
                .UseConverter(option => option.Text)
                .AddChoices(options));
            if (selected.Offer is not { } offer)
                return;
            var quantity = AnsiConsole.Prompt(new TextPrompt<int>(
                    $"Cantidad a comprar (1–{offer.Stock}; 0 cancela):")
                .Validate(value => value >= 0 && value <= offer.Stock
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Elegí una cantidad dentro del stock.")));
            if (quantity == 0)
                continue;
            var success = expedition.TryBuy(offer, quantity, out var error);
            UiHelpers.MostrarPanel(success ? "Compra realizada" : "Atención",
                 success ? $"Compraste {quantity} × {Markup.Escape(offer.Definition.Name)}"
                    + (offer.Definition is WeaponDefinition ? $" Tier {offer.Definition.Tier}" : "") + "."
                    : Markup.Escape(error), success ? Color.Green : Color.Yellow);
            UiHelpers.Pausar();
        }
    }

    private static void MostrarBotin(EventOccurrence current)
    {
        var remaining = current.Loot.Where(entry => entry.Remaining > 0).ToArray();
        if (remaining.Length == 0)
        {
            UiHelpers.MostrarPanel("Botín", "No queda botín por recoger.", Color.Grey);
            return;
        }
        UiHelpers.MostrarPanel("Botín",
            "Botín disponible (lo que dejes se pierde al continuar):\n"
            + string.Join('\n', remaining.Select(entry =>
                $"- {Markup.Escape(entry.Definition.Name)} × {entry.Remaining}")), Color.Yellow);
    }

    private static void TomarBotin(Expedition expedition, ItemFactory factory)
    {
        var options = expedition.Current.Loot.Where(entry => entry.Remaining > 0)
            .Select(entry => new LootOption(entry,
                $"{Markup.Escape(entry.Definition.Name)} · {entry.Remaining} disponible(s)"))
            .ToList();
        if (options.Count == 0)
        {
            UiHelpers.MostrarPanel("Botín", "Ya no queda botín.", Color.Yellow);
            UiHelpers.Pausar();
            return;
        }

        options.Add(new LootOption(null, "Cancelar"));
        var selection = AnsiConsole.Prompt(UiHelpers.Menu<LootOption>("¿Qué querés recoger?")
            .UseConverter(option => option.Text)
            .AddChoices(options));
        if (selection.Entry is not { } entry)
            return;

        var quantity = 0;
        while (true)
        {
            var input = AnsiConsole.Prompt(new TextPrompt<string>(
                $"Cantidad a recoger (1–{entry.Remaining}; 0 cancela):"));
            if (int.TryParse(input, out quantity) && quantity >= 0 && quantity <= entry.Remaining)
                break;
            UiHelpers.MostrarPanel("Atención", "Ingresá una cantidad entera disponible.", Color.Yellow);
        }
        if (quantity == 0)
            return;
        if (expedition.TryCollect(entry, quantity, factory, out var error))
            UiHelpers.MostrarPanel("Botín recogido",
                $"Recogiste {quantity} × {Markup.Escape(entry.Definition.Name)}.", Color.Green);
        else
            UiHelpers.MostrarPanel("Atención", Markup.Escape(error), Color.Yellow);
        UiHelpers.Pausar();
    }

    private sealed record LootOption(LootEntry? Entry, string Text);
    private sealed record ZoneOption(Zone? Zone, string Text);
    private sealed record OfferOption(MerchantOffer? Offer, string Text);
    private enum PauseAction { Loot, Inventory, Heal, Reload, Progression, Continue, Withdraw }
}
