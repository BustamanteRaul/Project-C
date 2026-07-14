using Spectre.Console;

namespace MySharpVenture.Pantallas;

public class MenuPrincipal(GameState state) : IPantalla
{
    private readonly GameState _state = state;

    public PantallaId Show()
    {
        UiHelpers.MostrarTitulo();

        _state.Player.GetInfo();

        var input = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .PageSize(10)
                .Title("¿Que desea hacer, ir de [teal]aventura[/] o [lime]prepararse[/] para ella?")
                .HighlightStyle(new Style(foreground: Color.White))
                .AddChoices("[lime]Equipamento[/]", "[teal]Aventura[/]", "[red]Des-login[/]"))
            .RemoveMarkup();

        return input switch
        {
            "Equipamento" => PantallaId.Equipamento,
            "Aventura" => PantallaId.Aventura,
            "Des-login" => PantallaId.Salir,
            _ => PantallaId.MenuPrincipal
        };
    }
}
