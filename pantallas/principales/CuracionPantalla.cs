using Spectre.Console;

namespace MySharpVenture.Pantallas;

public sealed class CuracionPantalla(GameState state) : IPantalla
{
    public PantallaId Show()
    {
        if (state.Player is not { } player)
            return PantallaId.Creacion;
        Use(player);
        return PantallaId.MenuPrincipal;
    }

    public void Use(Person player)
    {
        UiHelpers.MostrarTitulo();
        UiHelpers.MostrarPanel("Curación",
            $"Salud: {player.CurrentHealth}/{player.MaxHealth}", Color.Green);
        var options = player.Inventory.Items.OfType<Consumable>()
            .Where(item => item.Type == ConsumableType.Healing)
            .Select(item => new HealingOption(item,
                $"{Markup.Escape(item.Definition.Name)} · {item.Quantity} disponible(s)"))
            .ToList();
        if (options.Count == 0)
        {
            UiHelpers.MostrarPanel("Atención", "No tenés curaciones disponibles.", Color.Yellow);
            UiHelpers.Pausar();
            return;
        }

        options.Add(new HealingOption(null, "Cancelar"));
        var selection = AnsiConsole.Prompt(UiHelpers.Menu<HealingOption>(
                $"Salud: {player.CurrentHealth}/{player.MaxHealth}. Elegí una curación:", player.Origin)
            .UseConverter(option => option.Text)
            .AddChoices(options));
        if (selection.Item is null)
            return;

        if (player.TryUseHealing(selection.Item, out var healed, out var error))
            UiHelpers.MostrarPanel("Curación aplicada", $"Recuperaste {healed} de salud.", Color.Green);
        else
            UiHelpers.MostrarPanel("Atención", Markup.Escape(error), Color.Yellow);
        UiHelpers.Pausar();
    }

    private sealed record HealingOption(Consumable? Item, string Text);
}
