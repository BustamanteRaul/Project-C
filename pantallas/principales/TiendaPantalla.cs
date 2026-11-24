using Spectre.Console;

namespace MySharpVenture.Pantallas;

/// <summary>Compra y venta de suministros durante la preparación.</summary>
public sealed class TiendaPantalla(GameState state) : IPantalla
{
    public PantallaId Show()
    {
        if (state.Player is not { } player)
            return PantallaId.Creacion;
        if (state.ActiveExpedition is not null)
            return PantallaId.Aventura;

        while (true)
        {
            Cabecera(player);
            var action = AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Qué querés hacer en la tienda?", player.Origin)
                .AddChoices("Comprar", "Vender", "Volver a preparación"));
            if (action == "Volver a preparación")
                return PantallaId.MenuPrincipal;
            while (true)
            {
                var category = AnsiConsole.Prompt(UiHelpers.Menu<string>($"{action} · Elegí una categoría", player.Origin)
                    .AddChoices("Munición", "Curaciones", "Quickhacks", ServicioUi.Back));
                if (category == ServicioUi.Back)
                    break;
                Operar(player, action == "Comprar", category);
            }
        }
    }

    private static void Cabecera(Person player)
    {
        UiHelpers.MostrarTitulo();
        UiHelpers.MostrarPanel("[bold aqua]Tienda de suministros[/]", $"[yellow]Eurodólares: {player.Money}[/]"
            + "\nLa munición comprada va a la reserva: recargá para pasarla al arma."
            + "\nLos quickhacks se compran sin asignar. Configuralos desde Inventario y equipo.", Color.Teal);
    }

    private void Operar(Person player, bool buying, string category)
    {
        var title = $"{(buying ? "Comprar" : "Vender")} · {category}";
        while (true)
        {
            Cabecera(player);
            var options = Offers(player, buying, category).ToArray();
            if (options.Length == 0)
                UiHelpers.MostrarPanel(category, "No tenés artículos de esta categoría para vender.", Color.Grey);
            var selected = AnsiConsole.Prompt(UiHelpers.Menu<Option>(title, player.Origin)
                .UseConverter(option => option.IsBack ? ServicioUi.Back : Markup.Escape(option.Text)
                    + $" · {ServicioUi.Estado(option.Reason, option.SoldOut)}")
                .AddChoices(options.Append(new(ServicioUi.Back, 0, IsBack: true))));
            if (selected.IsBack)
                return;
            if (selected.Reason is not null)
            {
                ServicioUi.Resultado(selected.Reason);
                continue;
            }
            if (selected.Quickhack is { } hack)
            {
                var selling = !buying;
                if (selling && AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Confirmás la venta?", player.Origin)
                    .AddChoices("Cancelar", "Confirmar venta")) != "Confirmar venta")
                    continue;
                var done = selling ? state.TrySellQuickhack(hack.Action, out var rejection)
                    : state.TryBuyQuickhack(hack.Action, out rejection);
                UiHelpers.MostrarPanel(done ? "Listo" : "Atención",
                    !done ? Markup.Escape(rejection) : selling ? $"Vendiste {Markup.Escape(hack.Name)}."
                        : $"Compraste {Markup.Escape(hack.Name)}. Equipalo desde el inventario.",
                    done ? Color.Green : Color.Yellow);
                UiHelpers.Pausar();
                continue;
            }
            var verb = buying ? "comprar" : "vender";
            var quantity = AnsiConsole.Prompt(new TextPrompt<int>(
                    $"Cantidad a {verb} (1–{selected.Limit}; 0 cancela):")
                .Validate(value => value >= 0 && value <= selected.Limit
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Elegí una cantidad dentro del rango.")));
            if (quantity == 0)
                continue;

            var success = selected.Offer is { } offer
                ? state.TryBuyFromShop(offer, quantity, out var error)
                : state.TrySellToShop(selected.Stack!, quantity, out error);
            var name = (selected.Offer?.Definition ?? selected.Stack!.ConsumableDefinition).Name;
            UiHelpers.MostrarPanel(success ? "Listo" : "Atención",
                Markup.Escape(success ? $"{(buying ? "Compraste" : "Vendiste")} {quantity} × {name}." : error),
                success ? Color.Green : Color.Yellow);
            UiHelpers.Pausar();
        }
    }

    private IEnumerable<Option> Offers(Person player, bool buying, string category)
    {
        if (category == "Quickhacks")
        {
            return buying
                ? QuickhackContent.All.Where(hack => hack.Price > 0)
                    .Select(hack => new Option($"Comprar {hack.Name} · {hack.Price} · stock 1 · {hack.RamCost} RAM · {hack.Description}", 1, Quickhack: hack,
                        Reason: player.OwnsQuickhack(hack.Action) ? "Ya lo tenés" : ServicioUi.FaltanFondos(player, hack.Price)))
                : player.OwnedQuickhacks.Select(hack => new Option($"Vender {hack.Name} · {GameState.QuickhackSalePrice(hack)}",
                    1, Quickhack: hack, Reason: player.IsQuickhackAssigned(hack.Action) ? "Asignado a un cyberdeck" : null));
        }
        var type = category == "Munición" ? ConsumableType.Ammunition : ConsumableType.Healing;
        var offers = state.ShopOffers.Where(offer => offer.Definition is ConsumableDefinition item && item.Type == type);
        if (!buying)
            return player.Inventory.Items.OfType<Consumable>()
                .Where(stack => offers.Any(offer => offer.Definition == stack.ConsumableDefinition))
                .Select(stack => new Option($"Vender {stack.Definition.Name} · {GameState.ShopSalePrice(stack.Type)} por unidad"
                    + $" · tenés {stack.Quantity}", stack.Quantity, Stack: stack));

        return offers.Select(offer =>
        {
            var limit = Math.Min(offer.Stock, player.Money / offer.UnitPrice);
            return new Option($"Comprar {offer.Definition.Name} · {offer.UnitPrice} por unidad · stock {offer.Stock}"
                + $" · podés pagar {limit}", limit, Offer: offer,
                Reason: offer.Stock == 0 ? "Agotado" : ServicioUi.FaltanFondos(player, offer.UnitPrice), SoldOut: offer.Stock == 0);
        });
    }

    private sealed record Option(string Text, int Limit, MerchantOffer? Offer = null,
        Consumable? Stack = null, QuickhackDefinition? Quickhack = null, string? Reason = null,
        bool SoldOut = false, bool IsBack = false);
}
