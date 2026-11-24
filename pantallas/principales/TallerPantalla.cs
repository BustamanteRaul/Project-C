using Spectre.Console;

namespace MySharpVenture.Pantallas;

/// <summary>Permite fabricar suministros y recuperar componentes en preparación.</summary>
public sealed class TallerPantalla(GameState state) : IPantalla
{
    public PantallaId Show()
    {
        if (state.Player is not { } player)
            return PantallaId.Creacion;
        if (state.ActiveExpedition is not null)
            return PantallaId.Aventura;

        while (true)
        {
            UiHelpers.MostrarTitulo();
            UiHelpers.MostrarPanel("[bold orange1]Taller[/]", $"[yellow]Componentes: {player.Components}[/]"
                + $"\n[aqua]Cargadores ampliados sueltos: {player.SpareExtendedMagazines}[/]"
                + $"\n[aqua]Miras réflex sueltas: {player.SpareReflexSights}[/]", Color.Orange1);
            var action = AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Qué querés hacer en el taller?", player.Origin)
                .AddChoices("Fabricar", "Desarmar", "Mejorar arma", "Mejorar implante",
                     $"Fabricar cargador ampliado · {MagazineContent.ComponentCost} componentes",
                     $"Fabricar mira réflex · {ReflexSightContent.ComponentCost} componentes", "Volver a preparación"));
            switch (action)
            {
                case "Fabricar":
                    Craft(player);
                    break;
                case "Desarmar":
                    Disassemble(player);
                    break;
                case "Mejorar arma":
                    Upgrade(player);
                    break;
                case "Mejorar implante":
                    UpgradeImplant(player);
                    break;
                default:
                     if (action == "Volver a preparación")
                         return PantallaId.MenuPrincipal;
                     var sight = action.StartsWith("Fabricar mira réflex", StringComparison.Ordinal);
                     if (AnsiConsole.Prompt(UiHelpers.Menu<string>(
                             sight ? "¿Fabricar una mira réflex para pistola (+5 puntos de acierto)?"
                                 : $"¿Fabricar un cargador para pistola (+{MagazineContent.ExtraCapacity} balas)?", player.Origin)
                        .AddChoices("Cancelar", "Confirmar fabricación")) == "Confirmar fabricación")
                    {
                         var success = sight ? state.TryCraftReflexSight(out var error)
                             : state.TryCraftExtendedMagazine(out error);
                         ShowResult(success, error, sight ? "Fabricaste una mira réflex." : "Fabricaste un cargador ampliado.");
                    }
                    break;
            }
        }
    }

    private void UpgradeImplant(Person player)
    {
        var options = player.InstalledCyberware.Values.Concat(player.SpareCyberware)
            .Select((implant, index) => new Option<CyberwareDefinition>(implant,
                $"#{index + 1} · {implant.Name} · Tier {implant.Tier} · "
                + (ReferenceEquals(player.InstalledCyberware.GetValueOrDefault(implant.Zone), implant) ? "instalado" : "suelto")
                + (state.PreviewImplantUpgrade(implant) is { } upgrade ? $" · {upgrade.Cost} componentes"
                    : implant.Tier == 5 ? " · tier máximo" : " · sin mejora disponible")))
            .Append(new(null, "Cancelar"));
        var selected = Select("¿Qué implante querés mejorar?", player, options);
        if (selected is null)
            return;
        if (state.PreviewImplantUpgrade(selected) is not { } next)
        {
            ShowResult(false, "Ese implante no tiene otra mejora disponible.", "");
            return;
        }
        if (next.BlockedReason is not null || player.Components < next.Cost)
        {
            ShowResult(false, next.BlockedReason ?? "No tenés suficientes componentes.", "");
            return;
        }
        var effect = selected.MaxRam > 0
            ? $"RAM del deck: {selected.MaxRam} → {next.Next.MaxRam}."
            : selected.BonusRam > 0
                ? $"Bonificación de RAM: +{selected.BonusRam} → +{next.Next.BonusRam} con deck."
                : $"{CyberwareContent.Describe(selected)} → {CyberwareContent.Describe(next.Next)}.";
        UiHelpers.MostrarPanel("Mejora de implante",
            $"{Markup.Escape(selected.Name)}: Tier {selected.Tier} → Tier {next.Next.Tier}."
             + $"\n{effect}\nCapacidad requerida: {selected.CapacityCost} → {next.Next.CapacityCost}."
            + $"\nCoste: {next.Cost} componentes. La RAM actual no se rellena.", Color.Yellow);
        if (AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Confirmás la mejora del implante?", player.Origin)
            .AddChoices("Cancelar", "Confirmar mejora")) != "Confirmar mejora")
            return;
        var success = state.TryUpgradeImplant(selected, out var error);
        ShowResult(success, error, $"{selected.Name} mejorado a Tier {next.Next.Tier}.");
    }

    private void Upgrade(Person player)
    {
        var weapons = player.Inventory.Items.Select((item, index) => (item, index))
            .Where(entry => entry.item is Weapon)
            .Select(entry =>
            {
                var weapon = (Weapon)entry.item;
                var detail = state.PreviewWeaponUpgrade(weapon) is { } next
                    ? $"→ Tier {next.Next.Tier} · daño {weapon.WeaponDefinition.Damage} → {next.Next.Damage} · {next.Cost} componentes"
                    : weapon.Definition.Tier == 5 ? "tier máximo" : "sin mejora disponible";
                return new Option<Weapon>(weapon, $"#{entry.index + 1} · {weapon.Definition.Name}"
                    + $" · Tier {weapon.Definition.Tier} · {detail}");
            }).ToArray();
        if (weapons.Length == 0)
        {
            ShowResult(false, "No tenés armas para mejorar.", "");
            return;
        }
        var selected = Select("¿Qué arma querés mejorar?", player, weapons.Append(new(null, "Cancelar")));
        if (selected is null)
            return;
        if (state.PreviewWeaponUpgrade(selected) is not { } upgrade)
        {
            ShowResult(false, "Esa arma no tiene otra mejora disponible.", "");
            return;
        }
        if (player.Components < upgrade.Cost)
        {
            ShowResult(false, "No tenés suficientes componentes.", "");
            return;
        }
        UiHelpers.MostrarPanel("Mejora de arma",
            $"{Markup.Escape(selected.Definition.Name)}: Tier {selected.Definition.Tier} → Tier {upgrade.Next.Tier}."
            + $"\nDaño: {selected.WeaponDefinition.Damage} → {upgrade.Next.Damage}."
            + $"\nCoste: {upgrade.Cost} componentes. Las balas cargadas se conservan.", Color.Yellow);
        if (AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Confirmás la mejora?", player.Origin)
            .AddChoices("Cancelar", "Confirmar mejora")) != "Confirmar mejora")
            return;
        var success = state.TryUpgradeWeapon(selected, out var error);
        ShowResult(success, error, $"{selected.Definition.Name} mejorada a Tier {upgrade.Next.Tier}.");
    }

    private void Craft(Person player)
    {
        var options = state.CraftingRecipes.Select(recipe => new Option<CraftingRecipe>(recipe,
            recipe.Output switch
            {
                WeaponDefinition => $"{recipe.Output.Name} · Tier 1 · {recipe.ComponentCost} componentes",
                CybergearStashDefinition { Quickhack: { } action } =>
                    $"{recipe.Output.Name} · quickhack · {recipe.ComponentCost} componentes"
                    + (player.OwnsQuickhack(action) ? " · ya lo tenés" : ""),
                _ => $"{recipe.Output.Name} × {recipe.Units} · {recipe.ComponentCost} componente(s) por lote"
            }))
            .Append(new(null, "Cancelar"));
        var recipe = Select("¿Qué querés fabricar?", player, options);
        if (recipe is null)
            return;
        if (recipe.Output is CybergearStashDefinition { Quickhack: { } owned } && player.OwnsQuickhack(owned))
        {
            ShowResult(false, $"Ya tenés {recipe.Output.Name}.", "");
            return;
        }
        var limit = Math.Min(player.Components / recipe.ComponentCost, int.MaxValue / recipe.Units);
        if (limit == 0)
        {
            ShowResult(false, "No tenés suficientes componentes.", "");
            return;
        }
        if (recipe.Output is WeaponDefinition weapon)
        {
            var weight = CarryWeight.WeaponGrams(weapon);
            var after = player.CarryWeightGrams + weight - (long)recipe.ComponentCost * CarryWeight.ComponentGrams;
            UiHelpers.MostrarPanel("Fabricar arma",
                $"{Markup.Escape(weapon.Name)} · Tier 1 · {recipe.ComponentCost} componentes."
                 + $"\nPeso: {UiHelpers.WeightText(weight)} kg. "
                 + (weapon.IsMelee ? "Sin munición; no se equipa." : "Cargador vacío; no se equipa.")
                + $"\nCarga resultante: {UiHelpers.WeightText(after)}/{UiHelpers.WeightText(player.CarryLimitGrams)} kg."
                + (after > player.CarryLimitGrams
                    ? "\nSobrepeso: no podrás iniciar otra aventura hasta aligerar la carga." : ""), Color.Yellow);
            if (AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Confirmás la fabricación del arma?", player.Origin)
                .AddChoices("Cancelar", "Confirmar fabricación")) != "Confirmar fabricación")
                return;
            var crafted = state.TryCraft(recipe, 1, out var rejection);
            ShowResult(crafted, rejection, $"Fabricaste {weapon.Name} Tier 1.");
            return;
        }
        if (recipe.Output is CybergearStashDefinition { Quickhack: not null })
        {
            UiHelpers.MostrarPanel("Fabricar quickhack",
                $"{Markup.Escape(recipe.Output.Name)} · {recipe.ComponentCost} componentes."
                + "\nSe añade al inventario sin asignar a un deck.", Color.Yellow);
            if (AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Confirmás la fabricación del quickhack?", player.Origin)
                .AddChoices("Cancelar", "Confirmar fabricación")) != "Confirmar fabricación")
                return;
            var crafted = state.TryCraft(recipe, 1, out var rejection);
            ShowResult(crafted, rejection, $"Fabricaste {recipe.Output.Name}. Equipalo desde el inventario.");
            return;
        }
        var batches = ReadQuantity("Cantidad de lotes", limit);
        if (batches == 0)
            return;
        var success = state.TryCraft(recipe, batches, out var error);
        ShowResult(success, error, $"Fabricaste {recipe.Units * batches} × {recipe.Output.Name}.");
    }

    private void Disassemble(Person player)
    {
        var options = player.Inventory.Items.Select((item, index) => (item, index))
            .Where(entry => GameState.DisassemblyYield(entry.item) > 0)
            .Select(entry => new Option<object>(entry.item,
                $"#{entry.index + 1} · {entry.item.Definition.Name} · +{GameState.DisassemblyYield(entry.item)} componente(s) por unidad"
                 + (entry.item is Weapon { WeaponDefinition.IsMelee: false } weapon
                     ? $" · se pierden {weapon.LoadedAmmo} bala(s) cargadas" : "")))
            .Concat(new[] { AccessoryDefinition.Magazine, AccessoryDefinition.Sight }
                .Where(accessory => accessory.SpareCount(player) > 0)
                .Select(accessory => new Option<object>(accessory, $"{accessory.Name} suelto · +1 componente")))
            .ToArray();
        if (options.Length == 0)
        {
            ShowResult(false, "No tenés objetos para desarmar.", "");
            return;
        }
        var selectedItem = Select("¿Qué querés desarmar?", player, options.Append(new(null, "Cancelar")));
        if (selectedItem is AccessoryDefinition accessory)
        {
            if (AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Confirmás el desarme?", player.Origin)
                .AddChoices("Cancelar", "Confirmar desarme")) == "Confirmar desarme")
            {
                var done = state.TryDisassembleAccessory(accessory, out var rejection);
                ShowResult(done, rejection, "Obtuviste 1 componente(s).");
            }
            return;
        }
        if (selectedItem is not Item item)
            return;
        var quantity = item is Consumable stack ? ReadQuantity("Cantidad a desarmar", stack.Quantity) : 1;
        if (quantity == 0)
            return;
        var yield = (long)GameState.DisassemblyYield(item) * quantity;
        UiHelpers.MostrarPanel("Confirmar desarme",
            $"Se destruirá: {quantity} × {Markup.Escape(item.Definition.Name)}.\nRecibirás {yield} componente(s)."
            + (item is Weapon selected ? $"\nPerderás {selected.LoadedAmmo} bala(s) cargadas."
                 + (selected.HasExtendedMagazine ? " También perderás el cargador ampliado." : "")
                 + (selected.HasReflexSight ? " También perderás la mira réflex." : "") : ""), Color.Yellow);
        if (AnsiConsole.Prompt(UiHelpers.Menu<string>("¿Confirmás el desarme?", player.Origin)
            .AddChoices("Cancelar", "Confirmar desarme")) != "Confirmar desarme")
            return;
        var success = state.TryDisassemble(item, quantity, out var error);
        ShowResult(success, error, $"Obtuviste {yield} componente(s).");
    }

    private static T? Select<T>(string title, Person player, IEnumerable<Option<T>> options) where T : class =>
        AnsiConsole.Prompt(UiHelpers.Menu<Option<T>>(title, player.Origin)
            .UseConverter(option => Markup.Escape(option.Label)).AddChoices(options)).Value;

    private static int ReadQuantity(string label, int limit) =>
        AnsiConsole.Prompt(new TextPrompt<int>($"{label} (1–{limit}; 0 cancela):")
            .Validate(value => value >= 0 && value <= limit
                ? ValidationResult.Success() : ValidationResult.Error("Elegí una cantidad dentro del rango.")));

    private static void ShowResult(bool success, string error, string message)
    {
        if (success)
            Transiciones.Barra("Trabajo de taller…", Color.Orange1);
        UiHelpers.MostrarPanel(success ? "Listo" : "Atención", Markup.Escape(success ? message : error),
            success ? Color.Green : Color.Yellow);
        UiHelpers.Pausar();
    }

    private sealed record Option<T>(T? Value, string Label) where T : class;
}
