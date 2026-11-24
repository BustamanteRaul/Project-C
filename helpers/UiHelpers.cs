using Spectre.Console;
using Spectre.Console.Rendering;

namespace MySharpVenture;

public static class UiHelpers
{
    internal const string RamColor = "springgreen3";
    internal const string ShieldColor = "cyan";
    public static void MostrarTitulo()
    {
        if (!Console.IsOutputRedirected)
            AnsiConsole.Clear();
        var width = AnsiConsole.Profile.Width;
        IRenderable title = width >= 72
            ? new Rows(
                new FigletText("20").Color(Color.Yellow2).LeftJustified(),
                new FigletText("SharPunk").Color(Color.DarkViolet).Centered(),
                new FigletText("67").Color(Color.BlueViolet).RightJustified())
            : Align.Center(new Markup(""));
        MostrarPanel("", title, Color.Aquamarine1, 130);
    }

    public static void MostrarPanel(string title, string content, Color color) =>
        MostrarPanel(title, new Markup(content), color);

    public static void MostrarPanel(string title, IRenderable content, Color color, int maxWidth = 90)
    {
        AnsiConsole.Write(CrearPanel(title, content, color, maxWidth));
        AnsiConsole.WriteLine();
    }

    internal static IRenderable CrearPanel(string title, IRenderable content, Color color, int maxWidth = 90)
    {
        var panel = new Panel(content)
        {
            Width = Math.Min(maxWidth, Math.Max(10, AnsiConsole.Profile.Width - 2)),
            BorderStyle = new Style(color),
            Border = BoxBorder.Rounded
        };
        panel.Header(title, Justify.Center);
        return Align.Center(panel);
    }

    public static Color ColorOrigen(OriginPath? origin) => origin switch
    {
        OriginPath.Corpo => Color.Blue,
        OriginPath.Street => Color.Yellow,
        OriginPath.Nomad => Color.Green,
        _ => Color.Blue
    };

    public static SelectionPrompt<T> Menu<T>(string title, OriginPath? origin = null) where T : notnull =>
        new SelectionPrompt<T>()
            .Title(title)
            .WrapAround()
            .HighlightStyle(new Style(foreground: ColorOrigen(origin)));

    public static string Tier(int? tier) => tier switch
    {
        1 => "[grey]Tier 1[/]",
        2 => "[green]Tier 2[/]",
        3 => "[blue]Tier 3[/]",
        4 => "[purple]Tier 4[/]",
        5 => "[yellow]Tier 5[/]",
        null => "—",
        _ => $"Tier {tier}"
    };

    public static void MostrarPersonaje(Person player)
    {
        var table = new Table()
            .Title("Preparación")
            .AddColumn("Personaje")
            .AddColumn("Estado")
            .AddRow("Origen", Markup.Escape(player.Origin?.ToString() ?? "Sin origen"))
            .AddRow("Salud", $"{player.CurrentHealth}/{player.MaxHealth}")
            .AddRow("Escudo", $"[{ShieldColor}]{player.Shield}/{player.MaxShield}[/]")
            .AddRow("Stamina", $"{player.Stamina}/{player.MaxStamina}")
            .AddRow("Carga", $"{WeightText(player.CarryWeightGrams)}/{WeightText(player.CarryLimitGrams)} kg")
            .AddRow("Nivel", player.Level.ToString())
            .AddRow("XP", $"{player.Experience}/{(player.Level == 20
                ? ProgressionContent.ExperienceForLevel(20) : player.ExperienceToNextLevel)}")
            .AddRow("Capacidad de cyberware",
                $"{player.UsedCyberwareCapacity}/{player.CyberwareCapacity}");
        if (player.Cyberdeck is not null)
            table.AddRow("RAM", $"[{RamColor}]{player.Ram}/{player.MaxRam}[/]");
        else if (player.InstalledCyberdeck is not null)
            table.AddRow("Cyberdeck", "[yellow]Desactivado[/]");
        table.AddRow("Eurodólares", player.Money.ToString())
            .AddRow("Puntos atributo / habilidad", $"{player.AttributePoints} / {player.SkillPoints}")
            .AddRow("Armas equipadas", $"{player.Equipment.Weapons.Count}/3");
        table.BorderStyle = new Style(ColorOrigen(player.Origin));
        AnsiConsole.Write(Align.Center(table));
        AnsiConsole.WriteLine();
        if (player.IsOverweight)
            MostrarCarga(player);
    }

    internal static string RamLine(Person player) => player.Cyberdeck is not null
        ? $"\n[{RamColor}]RAM: {player.Ram}/{player.MaxRam}[/]"
        : player.InstalledCyberdeck is not null ? "\n[yellow]Cyberdeck desactivado[/]" : "";

    internal static string WeightText(long grams) =>
        (grams / 1000m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static string InventoryWeight(long grams) => grams < 1000 ? $"{grams} g"
        : (grams / 1000m).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " kg";

    public static void MostrarCarga(Person player)
    {
        var weight = player.CarryWeightGrams;
        var limit = player.CarryLimitGrams;
        MostrarPanel("Carga", $"{WeightText(weight)}/{WeightText(limit)} kg"
            + (weight > limit ? $"\nSobrepeso: {WeightText(weight - limit)} kg."
                + "\nPara iniciar otra aventura, guardá objetos en el stash o descartalos." : ""),
            weight > limit ? Color.Yellow : ColorOrigen(player.Origin));
    }

    public static void MostrarInventario(Person player)
    {
        MostrarCarga(player);
        MostrarPesoSuelto(player);
        if (player.Inventory.Items.Count == 0)
        {
            MostrarPanel("Inventario", "[grey]El inventario está vacío.[/]\nArmas equipadas: [lime]0/3[/]",
                ColorOrigen(player.Origin));
            return;
        }

        if (AnsiConsole.Profile.Width < 80)
        {
            for (var index = 0; index < player.Inventory.Items.Count; index++)
            {
                var item = player.Inventory.Items[index];
                var details = item is Weapon weapon
                    ? $"{Tier(item.Definition.Tier)} · " + (weapon.WeaponDefinition.IsMelee
                        ? "cuerpo a cuerpo" : $"Cargador {weapon.LoadedAmmo}/{weapon.MagazineCapacity}")
                      + (weapon.HasExtendedMagazine ? " · Ampliado" : "")
                      + (weapon.HasReflexSight ? " · Mira réflex" : "")
                      + (player.Equipment.Weapons.Contains(weapon) ? " · Equipada" : "")
                    : item is Consumable stack ? $"Cantidad {stack.Quantity}" : "—";
                MostrarPanel($"Objeto #{index + 1}",
                    $"{Markup.Escape(item.Definition.Name)}\n{details}\nPeso: {InventoryWeight(CarryWeight.ItemGrams(item))}",
                    item is Weapon
                        ? item.Definition.Tier switch
                        {
                            1 => Color.Grey,
                            2 => Color.Green,
                            3 => Color.Blue,
                            4 => Color.Purple,
                            _ => Color.Yellow
                        }
                        : item is Consumable { Type: ConsumableType.Healing }
                            ? Color.Green : Color.Teal);
            }
            MostrarPanel("Equipo y reserva",
                $"Armas equipadas: [lime]{player.Equipment.Weapons.Count}/3[/]\n"
                + "[grey]La munición del inventario es la reserva; el cargador se muestra por separado.[/]",
                Color.Teal);
            return;
        }

        var table = new Table()
            .Title("Inventario")
            .AddColumn("#")
            .AddColumn("Objeto")
            .AddColumn("Tier")
            .AddColumn("Cantidad")
            .AddColumn("Cargador")
            .AddColumn("Peso")
            .AddColumn("Equipo");

        for (var index = 0; index < player.Inventory.Items.Count; index++)
        {
            var item = player.Inventory.Items[index];
            table.AddRow(
                (index + 1).ToString(),
                Markup.Escape(item.Definition.Name) + (item is Weapon { HasReflexSight: true } ? " · Mira réflex" : ""),
                Tier(item.Definition.Tier),
                item is Consumable consumable ? consumable.Quantity.ToString() : "1",
                item is Weapon { WeaponDefinition.IsMelee: false } weapon ? $"{weapon.LoadedAmmo}/{weapon.MagazineCapacity}"
                    + (weapon.HasExtendedMagazine ? " · Ampliado" : "") : "—",
                InventoryWeight(CarryWeight.ItemGrams(item)),
                item is Weapon equipped && player.Equipment.Weapons.Contains(equipped) ? "Equipada" : "—");
        }

        table.BorderStyle = new Style(ColorOrigen(player.Origin));
        AnsiConsole.Write(Align.Center(table));
        AnsiConsole.WriteLine();
        MostrarPanel("Equipo y reserva",
            $"Armas equipadas: [lime]{player.Equipment.Weapons.Count}/3[/]\n"
            + "[grey]La munición del inventario es la reserva; el cargador se muestra por separado.[/]",
            Color.Teal);
    }

    private static void MostrarPesoSuelto(Person player)
    {
        var rows = new List<string>();
        void Add(string name, int quantity, int unitWeight)
        {
            if (quantity > 0)
                rows.Add($"{Markup.Escape(name)} × {quantity}: {InventoryWeight((long)quantity * unitWeight)}");
        }

        Add("Componentes", player.Components, CarryWeight.ComponentGrams);
        Add("Cargadores ampliados", player.SpareExtendedMagazines, CarryWeight.AccessoryGrams);
        Add("Miras réflex", player.SpareReflexSights, CarryWeight.AccessoryGrams);
        foreach (var group in player.SpareCyberware.GroupBy(implant => (implant.Name, implant.Tier)))
            Add($"{group.Key.Name} (Tier {group.Key.Tier})", group.Count(), CarryWeight.SpareImplantGrams);
        if (rows.Count > 0)
            MostrarPanel("Peso de objetos sueltos", string.Join('\n', rows), ColorOrigen(player.Origin));
    }

    public static void Pausar()
    {
        MostrarPanel("Continuar", "[grey]Presione una tecla para continuar...[/]", Color.Grey);
        Console.ReadKey(true);
    }
}
