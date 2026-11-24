using Spectre.Console;

namespace MySharpVenture.Pantallas;

internal static class ServicioUi
{
    internal const string Back = "Volver";

    internal static string? FaltanFondos(Person player, int price) =>
        player.Money < price ? $"Te faltan {price - player.Money} eurodólares" : null;

    internal static string Estado(string? reason, bool soldOut = false) =>
        reason is null ? "[green]Disponible[/]"
            : $"[{(soldOut ? "grey" : "yellow")}]{Markup.Escape(reason)}[/]";

    internal static void Resultado(string message, bool success = false)
    {
        UiHelpers.MostrarPanel(success ? "Listo" : "Atención", Markup.Escape(message),
            success ? Color.Green : Color.Yellow);
        UiHelpers.Pausar();
    }
}
