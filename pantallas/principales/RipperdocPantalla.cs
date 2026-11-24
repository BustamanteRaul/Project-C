using Spectre.Console;

namespace MySharpVenture.Pantallas;

/// <summary>Compra, instala, extrae y vende implantes durante la preparación.</summary>
public sealed class RipperdocPantalla(GameState state) : IPantalla
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
            var action = AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Qué querés hacer en el ripperdoc?", player.Origin)
                .AddChoices("Comprar", "Instalar", "Extraer", "Vender", "Volver a preparación"));
            if (action == "Volver a preparación")
                return PantallaId.MenuPrincipal;
            if (action == "Comprar")
                Comprar(player);
            else
                Gestionar(player, action);
        }
    }

    private static void Cabecera(Person player)
    {
        UiHelpers.MostrarTitulo();
        UiHelpers.MostrarPanel("[bold fuchsia]Ripperdoc[/]",
            $"[yellow]Eurodólares: {player.Money}[/]"
            + $"\n[aqua]Capacidad: {player.UsedCyberwareCapacity}/{player.CyberwareCapacity}[/]"
            + $"\n[{UiHelpers.ShieldColor}]Escudo: {player.Shield}/{player.MaxShield}[/]"
            + UiHelpers.RamLine(player)
            + $"\nTarifa de instalación o extracción: {CyberwareContent.ProcedureFee}"
            + "\nComprar deja el implante suelto. Para instalarlo necesitás una zona libre y capacidad suficiente.", Color.Fuchsia);
    }

    private void Comprar(Person player)
    {
        var zones = CyberwareContent.ShopOffers.GroupBy(offer => offer.Implant.Zone)
            .OrderBy(group => group.Key).ToArray();
        while (true)
        {
            var zone = AnsiConsole.Prompt(UiHelpers.Menu<string>("Comprar implantes · Elegí una zona", player.Origin)
                .AddChoices(zones.Select(group => Zona(group.Key)).Append(ServicioUi.Back)));
            if (zone == ServicioUi.Back)
                return;
            var offers = zones.Single(group => Zona(group.Key) == zone);
            while (true)
            {
                Cabecera(player);
                var options = offers.Select(offer =>
                {
                    var (implant, price) = offer;
                    var stock = state.HasImplantStock(implant) ? 1 : 0;
                    return new Option(implant, $"Comprar {Label(implant)} ({price}) · stock {stock}",
                        stock == 0 ? "Agotado" : ServicioUi.FaltanFondos(player, price), stock == 0);
                });
                var selected = Elegir(player, $"Comprar · {zone}", options);
                if (selected.Implant is not { } implant)
                    break;
                if (selected.Reason is not null)
                {
                    ServicioUi.Resultado(selected.Reason);
                    continue;
                }
                var done = state.TryBuyImplant(implant, out var error);
                ServicioUi.Resultado(done ? $"Compraste {implant.Name}. Está suelto, aún no instalado." : error, done);
            }
        }
    }

    private void Gestionar(Person player, string action)
    {
        const int fee = CyberwareContent.ProcedureFee;
        while (true)
        {
            Cabecera(player);
            var implants = action == "Extraer" ? player.InstalledCyberware.Values : player.SpareCyberware;
            var options = implants.Select(implant => action switch
            {
                "Instalar" => new Option(implant, $"Instalar {Label(implant)} ({fee}) · {Zona(implant.Zone)}"
                    + $" · {implant.CapacityCost} de capacidad", BloqueoInstalacion(player, implant) ?? ServicioUi.FaltanFondos(player, fee)),
                "Extraer" => new Option(implant, $"Extraer {Label(implant)} ({fee}) · {Zona(implant.Zone)}",
                    ServicioUi.FaltanFondos(player, fee)),
                _ => new Option(implant, $"Vender {Label(implant)} ({GameState.ImplantSalePrice(implant)})",
                    player.QuickhacksOn(implant).Any() ? "Tiene quickhacks asignados" : null)
            }).ToArray();
            if (options.Length == 0)
                UiHelpers.MostrarPanel(action, action == "Extraer" ? "No tenés implantes instalados."
                    : "No tenés implantes sueltos.", Color.Grey);
            var selected = Elegir(player, $"{action} implantes", options);
            if (selected.Implant is not { } implant)
                return;
            if (selected.Reason is not null)
            {
                ServicioUi.Resultado(selected.Reason);
                continue;
            }
            if (action == "Vender" && AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Confirmás la venta?", player.Origin)
                .AddChoices("Cancelar", "Confirmar venta")) != "Confirmar venta")
                continue;
            if (action is "Instalar" or "Extraer" && AnsiConsole.Prompt(UiHelpers.Menu<string>(
                    $"¿Confirmás {action.ToLowerInvariant()} {Markup.Escape(implant.Name)}?", player.Origin)
                .AddChoices("Cancelar", "Confirmar procedimiento")) != "Confirmar procedimiento")
                continue;
            string error;
            var success = action switch
            {
                "Instalar" => state.TryInstallImplant(implant, out error),
                "Extraer" => state.TryExtractImplant(implant.Zone, out error),
                _ => state.TrySellImplant(implant, out error)
            };
            if (success && action is "Instalar" or "Extraer")
            {
                var verb = action == "Instalar" ? "Instalando" : "Extrayendo";
                Transiciones.Barra($"{verb} {implant.Name}…", Color.BlueViolet, 1500);
                UiHelpers.MostrarPanel(action == "Instalar" ? "Instalación completada" : "Extracción completada",
                    $"Capacidad: {player.UsedCyberwareCapacity}/{player.CyberwareCapacity}"
                    + $"\n[{UiHelpers.ShieldColor}]Escudo: {player.Shield}/{player.MaxShield}[/]"
                    + UiHelpers.RamLine(player), Color.BlueViolet);
            }
            var message = action switch
            {
                "Instalar" => $"Instalaste {implant.Name}.",
                "Extraer" => $"Extrajiste {implant.Name}. Está en tu inventario de cyberware.",
                _ => $"Vendiste {implant.Name}."
            };
            ServicioUi.Resultado(success ? message : error, success);
        }
    }

    private static Option Elegir(Person player, string title, IEnumerable<Option> options) =>
        AnsiConsole.Prompt(UiHelpers.Menu<Option>(title, player.Origin).UseConverter(option =>
        {
            if (option.Implant is not { } implant)
                return ServicioUi.Back;
            var effects = CyberwareContent.Describe(implant);
            return $"{Markup.Escape(option.Label)} · {ServicioUi.Estado(option.Reason, option.SoldOut)}"
                + (effects.Length > 0 ? $" · [aqua]{Markup.Escape(effects)}[/]" : "");
        }).AddChoices(options.Append(new(null, ServicioUi.Back))));

    private static string? BloqueoInstalacion(Person player, CyberwareDefinition implant)
    {
        if (player.CyberwareFitError(implant) is null)
            return null;
        return player.InstalledCyberware.TryGetValue(implant.Zone, out var installed)
            ? $"Zona ocupada: {installed.Name}"
            : $"Te faltan {implant.CapacityCost - (player.CyberwareCapacity - player.UsedCyberwareCapacity)} de capacidad";
    }

    private static string Label(CyberwareDefinition implant) =>
        implant.Tier > 1 ? $"{implant.Name} (Tier {implant.Tier})" : implant.Name;

    private static string Zona(CyberwareZone zone) => zone switch
    {
        CyberwareZone.OperatingSystem => "Sistema operativo",
        CyberwareZone.FrontalCortex => "Corteza frontal",
        CyberwareZone.Eyes => "Ojos",
        CyberwareZone.Circulatory => "Sistema circulatorio",
        CyberwareZone.NervousSystem => "Sistema nervioso",
        CyberwareZone.Integumentary => "Sistema tegumentario",
        CyberwareZone.Skeleton => "Esqueleto",
        CyberwareZone.Hands => "Manos",
        CyberwareZone.Arms => "Brazos",
        _ => "Piernas"
    };

    private sealed record Option(CyberwareDefinition? Implant, string Label, string? Reason = null, bool SoldOut = false);
}
