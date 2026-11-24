using Spectre.Console;

namespace MySharpVenture.Pantallas;

public sealed class Bienvenida : IPantalla
{
    public PantallaId Show()
    {
        UiHelpers.MostrarTitulo();
        Transiciones.Barra("Iniciando MySharpVenture…", Color.Aqua, 2500,
            ["Comprobando la sesión", "Abriendo terminal", "Night City en línea"]);
        UiHelpers.MostrarPanel("[bold aqua]Bienvenido a Night City[/]",
            "[bold yellow]MySharpVenture[/]\n\n"
            + "Afuera llueve neón. Elegí tu origen, prepará tu equipo y salí a ganarte un nombre.\n\n"
            + "[yellow]Solo el stash se guarda al salir.[/] Tu personaje y lo que lleve encima no se conservan.",
            Color.BlueViolet);
        var choice = AnsiConsole.Prompt(UiHelpers.Menu<string>("Terminal de entrada")
            .AddChoices("Empezar", "Salir"));
        return choice == "Empezar" ? PantallaId.Creacion : PantallaId.Salir;
    }
}
