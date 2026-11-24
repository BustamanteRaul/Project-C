using Spectre.Console;

namespace MySharpVenture.Pantallas;

public class MenuPrincipal(GameState state, StashService? stash = null) : IPantalla
{
    private readonly GameState _state = state;

    public PantallaId Show()
    {
        if (_state.Player is not { } player)
            return PantallaId.Creacion;

        while (true)
        {
            UiHelpers.MostrarTitulo();
            UiHelpers.MostrarPersonaje(player);
            var category = AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Qué querés hacer?", player.Origin)
                .AddChoices("Ir de aventura", "Personaje", "Preparación", "Servicios", "Guía y práctica", "Salir"));
            PantallaId? destination = category switch
            {
                "Ir de aventura" => PantallaId.Aventura,
                "Personaje" => Select(player, category,
                    [new(PantallaId.Equipamento, "Inventario y equipo"), new(PantallaId.Progresion, "Atributos y habilidades")]),
                "Preparación" => Select(player, category,
                    [new(PantallaId.Curacion, "Usar curación"), new(PantallaId.Recarga, "Recargar arma"),
                        .. stash is null ? [] : new[] { new MenuOption(PantallaId.Stash, "Stash") }]),
                "Servicios" => Select(player, category,
                    [new(PantallaId.Tienda, "Tienda de suministros"), new(PantallaId.Taller, "Taller"),
                        new(PantallaId.Ripperdoc, "Visitar al ripperdoc")]),
                "Guía y práctica" => PantallaId.Guia,
                _ => PantallaId.Salir
            };
            if (destination is { } next)
                return next;
        }
    }

    private static PantallaId? Select(Person player, string category, MenuOption[] options) =>
        AnsiConsole.Prompt(UiHelpers.Menu<MenuOption>($"{category} · Elegí una opción", player.Origin)
            .UseConverter(option => option.Label)
            .AddChoices(options.Append(new(null, "Volver al menú principal")))).Destination;

    private sealed record MenuOption(PantallaId? Destination, string Label);
}
