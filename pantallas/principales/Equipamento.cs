using Spectre.Console;

namespace MySharpVenture.Pantallas;

public class Equipamento(GameState state) : IPantalla
{
    private readonly GameState _state = state;

    public PantallaId Show()
    {
        UiHelpers.MostrarTitulo();

        AnsiConsole.MarkupLine($"[lime]Equipamento de {_state.Player.Nombre}[/]");

        if (_state.Inventory.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No tenes items todavia.[/]");
        }
        else
        {
            foreach (var item in _state.Inventory)
            {
                AnsiConsole.MarkupLine($"- {item}");
            }
        }

        UiHelpers.Pausar();
        return PantallaId.MenuPrincipal;
    }
}
