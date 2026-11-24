using Spectre.Console;

namespace MySharpVenture.Pantallas;

public sealed class Creacion(GameState state, ItemCatalog catalog) : IPantalla
{
    public PantallaId Show()
    {
        if (state.Player is not null)
            return PantallaId.MenuPrincipal;

        try
        {
            while (true)
            {
                UiHelpers.MostrarTitulo();
                UiHelpers.MostrarPanel("Origen", "Elegí un origen para revisar su kit antes de crear el personaje.", Color.Yellow);
                var origin = AnsiConsole.Prompt(UiHelpers.Menu<OriginPath>("Elegí tu [yellow]origen[/]:")
                    .UseConverter(value => $"{value} — {Markup.Escape(catalog.Get(StartingKit.WeaponCode(value)).Name)}")
                    .AddChoices(Enum.GetValues<OriginPath>()));
                MostrarKit(origin);
                var answer = AnsiConsole.Prompt(UiHelpers.Menu<string>($"¿Confirmás el origen {origin}?", origin)
                    .AddChoices("Volver a elegir", "Crear personaje"));
                if (answer == "Volver a elegir")
                    continue;
                state.CreateCharacter(origin, catalog);
                Transiciones.Barra($"Preparando el equipo de {origin}…", UiHelpers.ColorOrigen(origin));
                return PantallaId.MenuPrincipal;
            }
        }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException)
        {
            UiHelpers.MostrarPanel("Error",
                $"No se pudo crear el personaje: {Markup.Escape(error.Message)}", Color.Red);
            UiHelpers.Pausar();
            return PantallaId.Salir;
        }
    }

    private void MostrarKit(OriginPath origin)
    {
        var weapon = catalog.Get(StartingKit.WeaponCode(origin)) as WeaponDefinition
            ?? throw new InvalidOperationException("El arma inicial no es válida.");
        var ammo = catalog.Get(weapon.AmmoCatalogItemId);
        var skill = ProgressionContent.Get(StartingKit.SkillCode(origin));
        var deck = CyberwareContent.StartingDeck;
        var hack = QuickhackContent.Find(StartingKit.Quickhack)!;
        var color = UiHelpers.ColorOrigen(origin);

        UiHelpers.MostrarPanel($"[bold]Kit inicial · {origin}[/]",
            $"[bold]Arma equipada:[/] {Markup.Escape(weapon.Name)} · Tier {weapon.Tier}"
            + $"\nDaño base: [yellow]{weapon.Damage}[/] · Acierto base: [green]{weapon.Accuracy:P0}[/]"
            + $"\nCargador: {weapon.MagazineCapacity}/{weapon.MagazineCapacity} · Reserva: {weapon.MagazineCapacity}"
            + $"\nMunición: {Markup.Escape(ammo.Name)}"
            + $"\n[bold]Habilidad inicial:[/] {Markup.Escape(skill.Name)}"
            + $"\n+{skill.Percent}% {ProgressionContent.EffectName(skill.Effect)}", color);
        UiHelpers.MostrarPanel("Incluido en todos los orígenes",
            $"{StartingKit.HealingCount} curaciones"
            + $"\n{Markup.Escape(deck.Name)} con {Markup.Escape(hack.Name)}"
            + $"\n[{UiHelpers.RamColor}]{deck.MaxRam} RAM máxima · +{deck.RamPerTurn}/turno[/]"
            + $"\nPuntos de atributo: {StartingKit.AttributePoints} · Puntos de habilidad: {StartingKit.SkillPoints}", color);
    }
}
