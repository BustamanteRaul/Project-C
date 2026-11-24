using Spectre.Console;

namespace MySharpVenture.Pantallas;

/// <summary>Transferencias persistentes accesibles solamente desde preparación.</summary>
public sealed class StashPantalla(GameState state, StashService stash) : IPantalla
{
    public PantallaId Show()
    {
        if (state.Player is not { } player)
            return PantallaId.Creacion;

        while (true)
        {
            UiHelpers.MostrarTitulo();
            UiHelpers.MostrarInventario(player);
            IReadOnlyList<StashEntry> entries;
            try
            {
                entries = stash.Entries();
            }
            catch (Exception error) when (error is IOException or InvalidDataException
                or InvalidOperationException
                or Microsoft.Data.Sqlite.SqliteException)
            {
                UiHelpers.MostrarPanel("Error", Markup.Escape(error.Message), Color.Red);
                UiHelpers.Pausar();
                return PantallaId.MenuPrincipal;
            }
            UiHelpers.MostrarPanel("Stash compartido",
                (entries.Count == 0 ? "El stash está vacío."
                    : string.Join('\n', entries.Select(entry =>
                         $"{Markup.Escape(EntryName(entry.Definition))} × {entry.Quantity}")))
                + $"\n\nComponentes transportados: {player.Components}"
                + $"\nComponentes guardados: {entries.FirstOrDefault(entry => entry.Definition is ComponentDefinition)?.Quantity ?? 0}",
                Color.Blue);
            var choice = AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Qué querés hacer con el stash?", player.Origin)
                .AddChoices("Depositar", "Retirar", "Volver a preparación"));
            if (choice == "Volver a preparación")
                return PantallaId.MenuPrincipal;
            if (choice == "Depositar")
                Deposit(player);
            else
                Withdraw(player, entries);
        }
    }

    private void Deposit(Person player)
    {
        var options = player.Inventory.Items.Select((item, index) =>
             new ItemOption(item, $"#{index + 1} · {Markup.Escape(EntryName(item.Definition))}"
                + (item is Consumable stack ? $" × {stack.Quantity}"
                     : item is Weapon { WeaponDefinition.IsMelee: false } weapon
                         ? $" · Cargador {weapon.LoadedAmmo}/{weapon.MagazineCapacity}" : "")))
            .ToList();
        foreach (var accessory in new[] { AccessoryDefinition.Magazine, AccessoryDefinition.Sight })
            if (accessory.SpareCount(player) > 0)
                options.Add(new(null, $"{Markup.Escape(accessory.Name)} × {accessory.SpareCount(player)} (sueltos)", accessory));
        if (player.Components > 0)
            options.Add(new(null, $"Componentes × {player.Components} (transportados)", ComponentDefinition.Workshop));
        foreach (var (implant, index) in player.SpareCyberware.Select((implant, index) => (implant, index)))
            options.Add(new(null, $"Implante #{index + 1} · {Markup.Escape(implant.Name)} · Tier {implant.Tier} (extraído)",
                Implant: implant));
        foreach (var hack in player.OwnedQuickhacks.Where(hack => !player.IsQuickhackAssigned(hack.Action)))
            options.Add(new(null, $"{Markup.Escape(hack.Name)} (quickhack sin asignar)", Quickhack: hack));
        if (options.Count == 0)
        {
            Message("No tenés objetos para depositar.", success: false);
            return;
        }
        options.Add(new ItemOption(null, "Cancelar"));
        var selection = AnsiConsole.Prompt(UiHelpers.Menu<ItemOption>("¿Qué objeto querés depositar?", player.Origin)
            .UseConverter(option => option.Text)
            .AddChoices(options));
        var item = selection.Item;
        var supply = selection.Supply;
        var implantToDeposit = selection.Implant;
        var quickhackToDeposit = selection.Quickhack;
        if (item is null && supply is null && implantToDeposit is null && quickhackToDeposit is null)
            return;
        var name = implantToDeposit is not null ? $"{implantToDeposit.Name} (Tier {implantToDeposit.Tier})"
            : quickhackToDeposit?.Name ?? EntryName((ItemDefinition?)supply ?? item!.Definition);
        var quantity = supply is not null ? AskQuantity(supply.SpareCount(player))
            : item is Consumable stack ? AskQuantity(stack.Quantity) : 1;
        if (quantity == 0)
            return;
        if (item is Weapon { WeaponDefinition.IsMelee: true })
            UiHelpers.MostrarPanel("Depósito", $"Vas a depositar {Markup.Escape(name)}; se desequipará.", Color.Yellow);
        else if (item is Weapon weapon)
            UiHelpers.MostrarPanel("Aviso",
                $"El arma se desequipará y se guardará vacía. Sus {weapon.LoadedAmmo} balas cargadas"
                + " pasarán al stash como munición separada; la reserva del inventario no cambia."
                + (weapon.HasExtendedMagazine ? "\nSe guardará por separado: cargador ampliado." : "")
                + (weapon.HasReflexSight ? "\nSe guardará por separado: mira réflex." : ""),
                Color.Yellow);
        else
        {
            var attached = implantToDeposit is null ? [] : player.QuickhacksOn(implantToDeposit).ToArray();
            UiHelpers.MostrarPanel("Depósito",
                $"Vas a depositar {quantity} × {Markup.Escape(name)}."
                + (attached.Length > 0 ? "\nSe guardarán por separado: "
                    + string.Join(", ", attached.Select(hack => Markup.Escape(hack.Name))) + "." : ""), Color.Yellow);
        }
        var confirmation = AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Confirmás el depósito?", player.Origin)
            .AddChoices("Cancelar", "Confirmar depósito"));
        if (confirmation != "Confirmar depósito")
            return;
        string error;
        var success = implantToDeposit is not null ? stash.TryDepositImplant(player, implantToDeposit, out error)
            : quickhackToDeposit is not null ? stash.TryDepositQuickhack(player, quickhackToDeposit.Action, out error)
            : supply is not null ? stash.TryDepositSupply(player, supply, quantity, out error)
            : stash.TryDeposit(player, item, quantity, out error);
        if (success)
            Transiciones.Barra("Guardando en el stash…", Color.Teal);
        Message(success ? $"Depositaste {quantity} × {name}." : error, success);
    }

    private void Withdraw(Person player, IReadOnlyList<StashEntry> entries)
    {
        if (entries.Count == 0)
        {
            Message("El stash está vacío.", success: false);
            return;
        }
        var options = entries.Select(entry => new StashOption(entry,
             $"{Markup.Escape(EntryName(entry.Definition))} × {entry.Quantity}")).ToList();
        options.Add(new StashOption(null, "Cancelar"));
        var selection = AnsiConsole.Prompt(UiHelpers.Menu<StashOption>("¿Qué querés retirar?", player.Origin)
            .UseConverter(option => option.Text)
            .AddChoices(options));
        if (selection.Entry is not { } selected)
            return;
        var quantity = selected.Definition is CybergearStashDefinition ? 1 : AskQuantity(selected.Quantity);
        if (quantity == 0)
            return;
        UiHelpers.MostrarPanel("Retirada",
             $"Vas a retirar {quantity} × {Markup.Escape(EntryName(selected.Definition))}"
             + (selected.Definition switch
             {
                 WeaponDefinition => ". Cada arma volverá con el cargador vacío y sin accesorios.",
                 CybergearStashDefinition { Implant.MaxRam: > 0 } => ".\nEl deck volverá sin instalar y con sus ranuras vacías.",
                 CybergearStashDefinition { Implant: not null } => ". El implante volverá sin instalar.",
                 CybergearStashDefinition => ". El quickhack volverá sin asignar.",
                 _ => "."
             }), Color.Yellow);
        var confirmation = AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Confirmás la retirada?", player.Origin)
            .AddChoices("Cancelar", "Confirmar retirada"));
        if (confirmation != "Confirmar retirada")
            return;
        var success = stash.TryWithdraw(player, selected.Definition.CatalogItemId, quantity, out var error);
        if (success)
            Transiciones.Barra("Retirando del stash…", Color.Teal);
        Message(success ? $"Retiraste {quantity} × {EntryName(selected.Definition)}." : error, success);
    }

    private static string EntryName(ItemDefinition definition) => definition.Tier is > 1
        ? $"{definition.Name} (Tier {definition.Tier})" : definition.Name;

    private static int AskQuantity(int available)
    {
        while (true)
        {
            var input = AnsiConsole.Prompt(new TextPrompt<string>(
                $"Cantidad (1–{available}; 0 cancela):"));
            if (int.TryParse(input, out var quantity) && quantity >= 0 && quantity <= available)
                return quantity;
            UiHelpers.MostrarPanel("Atención", "Ingresá una cantidad entera disponible.", Color.Yellow);
        }
    }

    private static void Message(string text, bool success)
    {
        UiHelpers.MostrarPanel(success ? "Listo" : "Atención",
            Markup.Escape(text), success ? Color.Green : Color.Yellow);
        UiHelpers.Pausar();
    }

    private sealed record ItemOption(Item? Item, string Text, StashSupplyDefinition? Supply = null,
        CyberwareDefinition? Implant = null, QuickhackDefinition? Quickhack = null);
    private sealed record StashOption(StashEntry? Entry, string Text);
}
