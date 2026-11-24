using Spectre.Console;

namespace MySharpVenture.Pantallas;

public class Equipamento(GameState state) : IPantalla
{
    private readonly GameState _state = state;

    public PantallaId Show()
    {
        if (_state.Player is not { } player)
            return PantallaId.Creacion;
        Manage(player, "Volver a preparación");
        return PantallaId.MenuPrincipal;
    }

    public void Manage(Person player, string backLabel)
    {
        while (true)
        {
            UiHelpers.MostrarTitulo();
            UiHelpers.MostrarPanel("Portador", $"Inventario de {Markup.Escape(player.Name)}",
                UiHelpers.ColorOrigen(player.Origin));
            UiHelpers.MostrarInventario(player);

            var choices = new List<string> { "Equipar arma", "Desequipar arma", "Descartar objeto", "Quickhacks" };
            if (player.SpareExtendedMagazines > 0 || player.Inventory.Items.OfType<Weapon>().Any(weapon => weapon.HasExtendedMagazine))
                choices.Add("Cargadores ampliados");
            if (player.SpareReflexSights > 0 || player.Inventory.Items.OfType<Weapon>().Any(weapon => weapon.HasReflexSight))
                choices.Add("Miras réflex");
            choices.Add(backLabel);
            var action = AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Qué querés hacer con tu equipo?", player.Origin)
                .AddChoices(choices));

            if (action == backLabel)
                return;

            if (action == "Descartar objeto")
                Descartar(player);
            else if (action == "Quickhacks")
                ConfigurarQuickhacks(player);
            else if (action == "Cargadores ampliados")
                ChangeAccessory(player, reflexSight: false);
            else if (action == "Miras réflex")
                ChangeAccessory(player, reflexSight: true);
            else
                CambiarEquipo(player, equipar: action == "Equipar arma");
        }
    }

    private void ChangeAccessory(Person player, bool reflexSight)
    {
        var accessory = reflexSight ? "mira réflex" : "cargador ampliado";
        var spare = reflexSight ? player.SpareReflexSights : player.SpareExtendedMagazines;
        bool Mounted(Weapon weapon) => reflexSight ? weapon.HasReflexSight : weapon.HasExtendedMagazine;
        UiHelpers.MostrarPanel(reflexSight ? "Miras réflex" : "Cargadores ampliados", $"Disponibles: {spare}",
            UiHelpers.ColorOrigen(player.Origin));
        var options = player.Inventory.Items.Select((item, i) => (item, i))
            .Where(entry => entry.item is Weapon { WeaponDefinition.IsMelee: false })
            .Select(entry =>
            {
                var weapon = (Weapon)entry.item;
                var compatible = reflexSight ? weapon.WeaponDefinition.SupportsReflexSight
                    : weapon.WeaponDefinition.SupportsExtendedMagazine;
                var detail = !compatible ? "no compatible"
                    : Mounted(weapon) ? $"Quitar {accessory}" : $"Montar {accessory}";
                return new OpcionArma(weapon, $"#{entry.i + 1} · {Markup.Escape(weapon.Definition.Name)}"
                    + $" · Tier {weapon.Definition.Tier} · {weapon.LoadedAmmo}/{weapon.MagazineCapacity} · {detail}");
            }).Append(new OpcionArma(null, "Cancelar"));
        var selected = AnsiConsole.Prompt(UiHelpers.Menu<OpcionArma>($"Elegí un arma para montar o quitar: {accessory}.", player.Origin)
            .UseConverter(option => option.Texto).AddChoices(options));
        if (selected.Arma is not { } weapon)
            return;
        var mount = !Mounted(weapon);
        var overflow = Math.Max(0, weapon.LoadedAmmo - weapon.WeaponDefinition.MagazineCapacity);
        var success = reflexSight ? _state.TryChangeReflexSight(weapon, mount, out var error)
            : _state.TryChangeExtendedMagazine(weapon, mount, out error);
        MostrarResultado(!success ? error : reflexSight ? (mount ? "Montaste la mira réflex." : "Quitaste la mira réflex.")
            : mount ? "Montaste el cargador ampliado."
            : $"Quitaste el cargador ampliado. Volvieron {overflow} bala(s) a la reserva.", success);
    }

    private static void ConfigurarQuickhacks(Person player)
    {
        if (player.Cyberdeck is null)
        {
            var owned = string.Join(", ", player.OwnedQuickhacks.Select(hack => hack.Name));
            MostrarResultado($"No hay un cyberdeck instalado.\nQuickhacks en inventario: {owned}.", false);
            return;
        }

        while (true)
        {
            UiHelpers.MostrarPanel("Quickhacks",
                $"Ranuras: {player.EquippedQuickhacks.Count()}/{QuickhackContent.DeckSlots}",
                UiHelpers.ColorOrigen(player.Origin));
            var choices = player.OwnedQuickhacks.Select(hack =>
            {
                var equipped = player.IsQuickhackEquipped(hack.Action);
                return new OpcionQuickhack(hack, !equipped, $"{hack.Name} · {hack.RamCost} RAM · "
                    + (equipped ? "Quitar del deck" : player.IsQuickhackAssigned(hack.Action)
                        ? "Asignado a otro deck" : "Equipar"));
            }).Append(new(null, false, "Volver al inventario"));
            var selected = AnsiConsole.Prompt(UiHelpers.Menu<OpcionQuickhack>(
                    "Configuración del cyberdeck", player.Origin)
                .UseConverter(option => Markup.Escape(option.Texto)).AddChoices(choices));
            if (selected.Hack is not { } hack)
                return;
            var success = player.TryConfigureQuickhack(hack.Action, selected.Equipar, out var error);
            MostrarResultado(success
                ? $"{hack.Name}: {(selected.Equipar ? "equipado" : "quitado del deck")}."
                : error, success);
        }
    }

    private sealed record OpcionQuickhack(QuickhackDefinition? Hack, bool Equipar, string Texto);

    private static void Descartar(Person player)
    {
        if (player.Inventory.Items.Count == 0)
        {
            MostrarResultado("No tenés objetos para descartar.", success: false);
            return;
        }

        var options = player.Inventory.Items
            .Select((item, index) => new OpcionObjeto(item,
                $"#{index + 1} · {Markup.Escape(item.Definition.Name)}"
                + (item is Weapon weapon
                     ? $" · {UiHelpers.Tier(item.Definition.Tier)}"
                         + (weapon.WeaponDefinition.IsMelee ? " · cuerpo a cuerpo" : $" · Cargador {weapon.LoadedAmmo}/{weapon.MagazineCapacity}")
                      + (player.Equipment.Weapons.Contains(weapon) ? " · Equipada" : "")
                    : item is Consumable consumable ? $" · Cantidad {consumable.Quantity}" : "")))
            .ToList();
        options.Add(new OpcionObjeto(null, "Cancelar"));

        var selected = AnsiConsole.Prompt(UiHelpers.Menu<OpcionObjeto>("¿Qué objeto querés descartar?", player.Origin)
            .UseConverter(option => option.Texto)
            .AddChoices(options));
        if (selected.Objeto is not { } item)
            return;

        var quantity = 1;
        if (item is Consumable stack)
        {
            while (true)
            {
                var input = AnsiConsole.Prompt(new TextPrompt<string>(
                    $"¿Cuántos querés descartar? ([yellow]1–{stack.Quantity}[/]; [grey]0 cancela[/])"));
                if (int.TryParse(input, out quantity) && quantity >= 0 && quantity <= stack.Quantity)
                    break;

                UiHelpers.MostrarPanel("Atención",
                    "Ingresá una cantidad entera dentro del rango.", Color.Yellow);
            }

            if (quantity == 0)
                return;
        }

        var detail = item is Weapon selectedWeapon
            ? $" (se perderán también {selectedWeapon.LoadedAmmo} balas cargadas; la reserva no cambia)"
                + (selectedWeapon.HasExtendedMagazine ? " También perderás el cargador ampliado." : "")
                + (selectedWeapon.HasReflexSight ? " También perderás la mira réflex." : "")
            : "";
        UiHelpers.MostrarPanel("Confirmar descarte",
            $"Vas a descartar {quantity} × {Markup.Escape(item.Definition.Name)}"
            + $"{Markup.Escape(detail)}.", Color.Yellow);
        var confirmation = AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Confirmás el descarte?", player.Origin)
            .AddChoices("Cancelar", "Confirmar descarte"));
        if (confirmation != "Confirmar descarte")
            return;

        var success = player.TryDiscard(item, quantity, out var error);
        MostrarResultado(success
            ? $"Descartaste {quantity} × {item.Definition.Name}."
            : error, success);
    }

    private static void CambiarEquipo(Person player, bool equipar)
    {
        var options = player.Inventory.Items
            .Select((item, index) => (Item: item, Number: index + 1))
            .Where(entry => entry.Item is Weapon weapon
                && player.Equipment.Weapons.Contains(weapon) != equipar)
            .Select(entry => new OpcionArma((Weapon)entry.Item,
                $"#{entry.Number} · {Markup.Escape(entry.Item.Definition.Name)}"
                 + $" · {UiHelpers.Tier(entry.Item.Definition.Tier)}"
                 + (((Weapon)entry.Item).WeaponDefinition.IsMelee ? " · cuerpo a cuerpo"
                     : $" · Cargador {((Weapon)entry.Item).LoadedAmmo}/{((Weapon)entry.Item).MagazineCapacity}")))
            .ToList();

        if (options.Count == 0)
        {
            MostrarResultado(equipar
                ? "No tenés armas sin equipar en el inventario."
                : "No tenés armas equipadas.", success: false);
            return;
        }

        if (equipar && player.Equipment.Weapons.Count >= 3)
        {
            MostrarResultado("Solo podés equipar hasta tres armas. Desequipá una para liberar una ranura.",
                success: false);
            return;
        }

        options.Add(new OpcionArma(null, "Cancelar"));
        var selected = AnsiConsole.Prompt(UiHelpers.Menu<OpcionArma>(
                equipar ? "Elegí el arma que querés equipar:" : "Elegí el arma que querés desequipar:",
                player.Origin)
            .UseConverter(option => option.Texto)
            .AddChoices(options));

        if (selected.Arma is not { } weapon)
            return;

        string error;
        var success = equipar
            ? player.Equipment.TryEquip(weapon, out error)
            : player.Equipment.TryUnequip(weapon, out error);
        MostrarResultado(success
            ? $"{weapon.Definition.Name}: {(equipar ? "equipada" : "desequipada")}."
            : error, success);
    }

    private static void MostrarResultado(string message, bool success)
    {
        UiHelpers.MostrarPanel(success ? "Listo" : "Atención",
            Markup.Escape(message), success ? Color.Green : Color.Yellow);
        UiHelpers.Pausar();
    }

    // La opción conserva la instancia; el texto y su número solo sirven para presentarla.
    private sealed record OpcionArma(Weapon? Arma, string Texto);
    private sealed record OpcionObjeto(Item? Objeto, string Texto);
}
