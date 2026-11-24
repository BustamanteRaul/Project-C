using Spectre.Console;

namespace MySharpVenture.Pantallas;

/// <summary>Asignación permanente de atributos y habilidades fuera del combate.</summary>
public sealed class ProgressionPantalla(GameState state) : IPantalla
{
    public PantallaId Show()
    {
        if (state.Player is not { } player)
            return PantallaId.Creacion;
        Manage(player, "Volver a preparación");
        return PantallaId.MenuPrincipal;
    }

    public void Manage(Person player, string backLabel)
    {
        while (true)
        {
            UiHelpers.MostrarTitulo();
            UiHelpers.MostrarPanel("Progresión",
                $"Nivel {player.Level}/20 (personaje) · XP {player.Experience}"
                + (player.Level < 20 ? $"/{player.ExperienceToNextLevel}" : " (máximo)")
                + (player.Level < 20
                    ? $" · faltan {player.ExperienceToNextLevel - player.Experience} XP" : "")
                + $"\nPuntos de atributo: {player.AttributePoints}"
                + $" · Puntos de habilidad: {player.SkillPoints}\n"
                + string.Join('\n', Enum.GetValues<AttributeType>().Select(attribute =>
                    $"{ProgressionContent.AttributeName(attribute)}: {player.Attribute(attribute)}/10")),
                UiHelpers.ColorOrigen(player.Origin));
            UiHelpers.MostrarPanel("Habilidades adquiridas",
                string.Join(", ", ProgressionContent.Skills
                    .Where(skill => player.HasSkill(skill.Code)).Select(skill => Markup.Escape(skill.Name))),
                Color.Teal);
            var choice = AnsiConsole.Prompt(UiHelpers.Menu<string>(
                    "¿Qué querés mejorar?", player.Origin)
                .AddChoices("Asignar punto de atributo", "Habilidades",
                    "Cyberware y RAM", backLabel));
            if (choice == backLabel)
                return;
            if (choice == "Asignar punto de atributo")
                AssignAttribute(player);
            else if (choice == "Habilidades")
                BrowseSkills(player);
            else
                ManageCyberware(player);
        }
    }

    private static void AssignAttribute(Person player)
    {
        if (player.AttributePoints == 0)
        {
            Message("No tenés puntos de atributo disponibles.", false);
            return;
        }
        var options = Enum.GetValues<AttributeType>()
            .Select(attribute => new AttributeOption(attribute,
                $"{ProgressionContent.AttributeName(attribute)} · {player.Attribute(attribute)}/10"))
            .ToList();
        options.Add(new AttributeOption(null, "Cancelar"));
        var selected = AnsiConsole.Prompt(UiHelpers.Menu<AttributeOption>(
                "¿Qué atributo querés aumentar?", player.Origin)
            .UseConverter(option => option.Label)
            .AddChoices(options));
        if (selected.Attribute is not { } attribute)
            return;
        var success = player.TryIncreaseAttribute(attribute, out var error);
        Message(success ? $"{ProgressionContent.AttributeName(attribute)} aumentó a {player.Attribute(attribute)}."
            : error, success);
    }

    private static void BrowseSkills(Person player)
    {
        var branches = ProgressionContent.Skills.GroupBy(skill => skill.Attribute).ToArray();
        while (true)
        {
            var options = branches.Select(branch => new AttributeOption(branch.Key,
                $"{ProgressionContent.AttributeName(branch.Key)} · {player.Attribute(branch.Key)}/10"
                + $" · {branch.Count(skill => player.HasSkill(skill.Code))}/{branch.Count()} adquiridas"))
                .Append(new AttributeOption(null, "Volver a progresión"));
            var selected = AnsiConsole.Prompt(UiHelpers.Menu<AttributeOption>(
                    $"Elegí un atributo · Puntos de habilidad: {player.SkillPoints}", player.Origin)
                .UseConverter(option => option.Label)
                .AddChoices(options));
            if (selected.Attribute is not { } attribute)
                return;
            BrowseSkillBranch(player, attribute, branches.Single(branch => branch.Key == attribute));
        }
    }

    private static void BrowseSkillBranch(Person player, AttributeType attribute, IEnumerable<SkillDefinition> skills)
    {
        var name = ProgressionContent.AttributeName(attribute);
        while (true)
        {
            var options = skills.Select(skill => new SkillOption(skill,
                $"{Markup.Escape(skill.Name)} · +{skill.Percent}% {ProgressionContent.EffectName(skill.Effect)}"
                + $" · {SkillStatus(player, skill)} · {ProgressionContent.ExplainEffect(skill.Effect)}"))
                .Append(new SkillOption(null, "Volver a los atributos"));
            var selected = AnsiConsole.Prompt(UiHelpers.Menu<SkillOption>(
                    $"{name} · Puntos de habilidad: {player.SkillPoints}", player.Origin)
                .UseConverter(option => option.Label)
                .AddChoices(options));
            if (selected.Skill is not { } skill)
                return;
            var success = player.TryUnlockSkill(skill.Code, out var error);
            Message(success ? $"Aprendiste {skill.Name}." : error, success);
        }
    }

    private static string SkillStatus(Person player, SkillDefinition skill)
    {
        if (player.HasSkill(skill.Code))
            return "adquirida";
        var requirements = new List<string>();
        if (player.Attribute(skill.Attribute) < skill.RequiredAttribute)
            requirements.Add($"requiere {ProgressionContent.AttributeName(skill.Attribute)} {skill.RequiredAttribute}");
        if (skill.Prerequisite is not null && !player.HasSkill(skill.Prerequisite))
            requirements.Add($"requiere {Markup.Escape(ProgressionContent.Get(skill.Prerequisite).Name)}");
        if (player.SkillPoints == 0)
            requirements.Add("requiere 1 punto de habilidad");
        return requirements.Count == 0 ? "disponible" : $"bloqueada ({string.Join(", ", requirements)})";
    }

    private static void ManageCyberware(Person player)
    {
        var implants = player.InstalledCyberware.Values
            .Select(implant => $"{Markup.Escape(implant.Name)} (Tier {implant.Tier})").ToArray();
        UiHelpers.MostrarPanel("Cyberware y RAM",
            $"Capacidad: {player.UsedCyberwareCapacity}/{player.CyberwareCapacity}"
            + $"\nInstalados: {(implants.Length == 0 ? "ninguno" : string.Join(", ", implants))}"
            + $"\n[{UiHelpers.ShieldColor}]Escudo: {player.Shield}/{player.MaxShield}[/]"
            + UiHelpers.RamLine(player)
            + (player.Cyberdeck is { } deck
                ? $"\nRegeneración: {deck.RamPerTurn} RAM/turno."
                    + $"\nQuickhacks equipados: {player.EquippedQuickhacks.Count()}/{QuickhackContent.DeckSlots}"
                 : player.InstalledCyberdeck is null ? "\nQuickhacks: no hay un cyberdeck instalado."
                     : "\nQuickhacks: no disponibles hasta que se reactive el cyberdeck.")
            + "\nInstalación y extracción: ripperdoc en preparación.",
            Color.Cyan);
        UiHelpers.Pausar();
    }

    private static void Message(string text, bool success)
    {
        UiHelpers.MostrarPanel(success ? "Listo" : "Atención",
            Markup.Escape(text), success ? Color.Green : Color.Yellow);
        UiHelpers.Pausar();
    }

    private sealed record AttributeOption(AttributeType? Attribute, string Label);
    private sealed record SkillOption(SkillDefinition? Skill, string Label);
}
