using Spectre.Console;

namespace MySharpVenture;

// Static helpers shared across screens: titles, common prompts, pauses, etc.
public static class UiHelpers
{
    public static void MostrarTitulo()
    {
        Console.Clear();
        AnsiConsole.Write(
            new FigletText("MySharpVenture 2.0")
                .Centered()
                .Color(Color.Yellow));
    }

    // Example of a reusable prompt pattern, e.g. "press any key to continue"
    public static void Pausar()
    {
        AnsiConsole.MarkupLine("\n[grey]Presione una tecla para continuar...[/]");
        Console.ReadKey(true);
    }
}
