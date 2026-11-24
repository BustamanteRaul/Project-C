using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace MySharpVenture;

internal static class Transiciones
{
    internal static bool Enabled { get; set; } =
        Environment.GetEnvironmentVariable("MYSHARPVENTURE_NO_ANIMATIONS") != "1";

    private static bool CanAnimate => Enabled && !Console.IsInputRedirected && !Console.IsOutputRedirected
        && AnsiConsole.Profile.Capabilities.Ansi && AnsiConsole.Profile.Capabilities.Interactive;

    internal static void Barra(string text, Color color, int milliseconds = 1000, string[]? stages = null)
    {
        const int steps = 10;
        if (!CanAnimate)
            return;
        var width = Math.Min(90, Math.Max(10, AnsiConsole.Profile.Width - 2)) - 4;
        IRenderable Frame(int step)
        {
            var bar = new BreakdownChart().Width(width).HideTags()
                .AddItem("Completado", step, color).AddItem("Pendiente", steps - step, Color.Grey);
            var stage = stages is { Length: > 0 } ? stages[Math.Min(stages.Length - 1, step * stages.Length / steps)] : "";
            return UiHelpers.CrearPanel(Markup.Escape(text),
                new Rows(bar, new Markup($"{step * 10}%  {Markup.Escape(stage)}\n[grey]Espacio: saltar[/]")), color);
        }

        AnsiConsole.Live(Frame(0)).AutoClear(true).Start(context =>
        {
            context.Refresh();
            for (var step = 1; step <= steps; step++)
            {
                if (Esperar(milliseconds / steps))
                    break;
                context.UpdateTarget(Frame(step));
                context.Refresh();
            }
        });
    }

    internal static void Resumen(string title, IReadOnlyList<string> sections, Color color)
    {
        var text = string.Join('\n', sections);
        if (!CanAnimate || sections.Count < 2)
        {
            UiHelpers.MostrarPanel(title, text, color);
            return;
        }

        IRenderable Frame(string content, bool complete = false) => UiHelpers.CrearPanel(title,
            new Markup(complete ? content : content + "\n[grey]Espacio: mostrar todo[/]"), color);
        AnsiConsole.Live(Frame(sections[0])).AutoClear(false).Start(context =>
        {
            context.Refresh();
            for (var count = 2; count <= sections.Count; count++)
            {
                if (Esperar(350))
                {
                    context.UpdateTarget(Frame(text, complete: true));
                    context.Refresh();
                    return;
                }
                context.UpdateTarget(Frame(string.Join('\n', sections.Take(count)), count == sections.Count));
                context.Refresh();
            }
        });
        AnsiConsole.WriteLine();
    }

    private static bool Esperar(int milliseconds)
    {
        var timer = Stopwatch.StartNew();
        do
        {
            var skip = false;
            while (Console.KeyAvailable)
                skip |= Console.ReadKey(intercept: true).Key == ConsoleKey.Spacebar;
            if (skip)
                return true;
            Thread.Sleep(10);
        } while (timer.ElapsedMilliseconds < milliseconds);
        return false;
    }
}
