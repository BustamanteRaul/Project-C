using Spectre.Console;

namespace MySharpVenture.Pantallas;

public class Aventura(GameState state) : IPantalla
{
    private readonly GameState _state = state;

    public PantallaId Show()
    {
        UiHelpers.MostrarTitulo();

        AnsiConsole.MarkupLine($"[teal]{_state.Player.Nombre} sale a la aventura...[/]");

        // TODO: actual adventure logic (combat, encounters, choices, etc.)

        UiHelpers.Pausar();
        return PantallaId.MenuPrincipal;
    }
}
