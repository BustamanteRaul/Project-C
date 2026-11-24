using Spectre.Console;

namespace MySharpVenture.Pantallas;

/// <summary>Recarga desde la reserva en preparación o en las pausas, sin turno enemigo.</summary>
public sealed class RecargaPantalla(GameState state) : IPantalla
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
        var options = player.Equipment.Weapons
            .Where(weapon => !weapon.WeaponDefinition.IsMelee)
            .Select((weapon, index) =>
            {
                var amount = player.PreviewReload(weapon, out var error);
                return new WeaponOption(weapon,
                    $"#{index + 1} · {Markup.Escape(weapon.Definition.Name)}"
                    + $" · {weapon.LoadedAmmo}/{weapon.MagazineCapacity}"
                    + (amount > 0 ? $" · +{amount} balas" : $" · {Markup.Escape(error)}"));
            }).ToList();
        if (options.Count == 0)
        {
            UiHelpers.MostrarPanel("Atención", "No tenés armas de fuego equipadas.", Color.Yellow);
            UiHelpers.Pausar();
            return;
        }

        options.Add(new WeaponOption(null, "Cancelar"));
        var selection = AnsiConsole.Prompt(UiHelpers.Menu<WeaponOption>(
                "Elegí un arma para recargar:", player.Origin)
            .UseConverter(option => option.Text)
            .AddChoices(options));
        if (selection.Weapon is not { } weapon)
            return;

        var success = player.TryReload(weapon, out var reloaded, out var error);
        UiHelpers.MostrarPanel(success ? "Recarga" : "Atención",
            success ? $"{Markup.Escape(weapon.Definition.Name)}: recargaste {reloaded} bala(s)."
                : Markup.Escape(error), success ? Color.Green : Color.Yellow);
        UiHelpers.Pausar();
    }

    private sealed record WeaponOption(Weapon? Weapon, string Text);
}
