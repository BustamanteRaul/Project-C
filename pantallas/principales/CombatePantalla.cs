using Spectre.Console;

namespace MySharpVenture.Pantallas;

// Solo presentación: el controlador decide costes, turnos y finalización.
public sealed class CombatePantalla(CombatController combat)
{
    public CombatStatus Show(bool allowWithdrawal = false)
    {
        UiHelpers.MostrarTitulo();
        var turnNumber = 1;
        while (combat.Status == CombatStatus.InProgress)
        {
            if (combat.SandevistanActive)
                UiHelpers.MostrarPanel("[bold]SANDEVISTAN ACTIVO[/]",
                    "Te queda 1 acción en este turno.\nAcierto garantizado si ejecutás un ataque físico."
                    + "\nEl enemigo no responderá. Sigue recuperando recursos.", Color.Aqua);
            UiHelpers.MostrarPanel($"Turno {turnNumber} · Combatientes",
                $"[red]{Markup.Escape(combat.Player.Name)}: {combat.Player.CurrentHealth}/{combat.Player.MaxHealth}[/]\n"
                + $"[{UiHelpers.ShieldColor}]Escudo: {combat.Player.Shield}/{combat.Player.MaxShield}[/]\n"
                 + $"Stamina: {combat.Player.Stamina}/{combat.Player.MaxStamina}"
                 + UiHelpers.RamLine(combat.Player)
                + (combat.Player.IsOverweight
                     ? $"\n[yellow]Sobrepeso: +{StaminaContent.OverweightCost} stamina por ataque físico."
                         + $" −{StaminaContent.OverweightRecoveryPenalty} stamina recuperada por turno."
                         + $" Prob. base de crítico si acertás: {DamageCalculator.OverweightCriticalPercent} %.[/]" : "")
                 + CyberwareStatus(combat.Player, combat.PlayerSelfIceReady)
                 + (combat.GlitchedPlayerWeapon is { } glitched
                     ? $"\nFallo de arma: {Markup.Escape(glitched.Definition.Name)} no puede disparar ni recargar." : "")
                  + (combat.PlayerOpticsDisrupted
                      ? combat.SandevistanActive ? "\nSandevistan supera el límite de Reboot Optics."
                          : "\nReboot Optics: acierto físico limitado al 5 %." : "")
                + (combat.HasSandevistan && !combat.SandevistanActive ? "\n" + SandevistanStatus() : "")
                + "\n"
                + $"[yellow]{Markup.Escape(combat.Enemy.Name)} "
                + $"({Markup.Escape(combat.EnemyAffiliation)}): "
                + $"{combat.Enemy.CurrentHealth}/{combat.Enemy.MaxHealth}[/]"
                + (combat.Enemy.MaxShield > 0
                    ? $"\n[{UiHelpers.ShieldColor}]Escudo enemigo: {combat.Enemy.Shield}/{combat.Enemy.MaxShield}[/]" : "")
                + $"\nStamina enemiga: {combat.Enemy.Stamina}/{combat.Enemy.MaxStamina}"
                + (combat.Enemy.IsBot ? "\nTipo de enemigo: Bot" : "")
                + CyberwareStatus(combat.Enemy, combat.EnemySelfIceReady)
                + (combat.OverheatPulses > 0 ? "\nSobrecalentado · "
                    + (combat.OverheatPulses == 1 ? "1 pulso pendiente" : $"{combat.OverheatPulses} pulsos pendientes") : ""),
                UiHelpers.ColorOrigen(combat.Player.Origin));
            UiHelpers.MostrarPanel("Intención enemiga",
                combat.SandevistanActive ? "Turno enemigo sin acciones: Sandevistan activo."
                : $"Intención: {Markup.Escape(combat.EnemyIntention)}.\n"
                + (combat.PlannedEnemyAction == CombatAction.Reload
                    ? "La recarga no inflige daño."
                    : combat.PlannedEnemyAction == CombatAction.Wait ? "No atacará en este turno."
                    : QuickhackContent.Find(combat.PlannedEnemyAction) is not null
                        ? "Quickhack: no depende del acierto físico."
                    : $"Daño aproximado enemigo si acierta: {combat.EnemyDamageEstimate}"
                        + $" · Fallo estimado: {combat.EnemyMissChance:P0}"), Color.Yellow);
            var selectedWeapon = combat.SelectedWeapon is { } weapon
                ? $"Arma seleccionada: {Markup.Escape(weapon.Definition.Name)}"
                     + (weapon.WeaponDefinition.IsMelee ? " · cuerpo a cuerpo" : $" · Cargador {weapon.LoadedAmmo}/{weapon.MagazineCapacity}")
                     + (weapon.WeaponDefinition.IsMelee ? "" : $" · Reserva: {combat.Player.AmmoReserve(weapon)}")
                   + (weapon.HasExtendedMagazine ? " · Ampliado" : "")
                   + (weapon.HasReflexSight ? " · Mira réflex" : "")
                    + (weapon.WeaponDefinition.Technology is { } technology ? $" · {technology}" : "")
                 : "Arma seleccionada: ninguna";
            UiHelpers.MostrarPanel("Equipo", selectedWeapon, UiHelpers.ColorOrigen(combat.Player.Origin));
             var reload = combat.Preview(CombatAction.Reload);
             var reloadText = reload.Available && combat.SelectedWeapon is { } reloading
                 ? $"[yellow]+{CantidadMunicion(reloading, reload.ReloadAmount)}[/] al cargador desde la reserva · 1 acción"
                 : $"[grey]No disponible: {Markup.Escape(reload.Reason!)}[/]";
            var heal = combat.Preview(CombatAction.Heal);
            var basic = combat.Preview(CombatAction.BasicAttack);
            CombatChoice[] choices =
            [
                new CombatChoice(CombatAction.WeaponAttack,
                     $"Atacar con arma — {PreviewText(combat.Preview(CombatAction.WeaponAttack), combat.SelectedWeapon)}"),
                new CombatChoice(CombatAction.BasicAttack,
                    $"{Markup.Escape(basic.ArmAttack ?? "Ataque básico")} — {PreviewText(basic)}"),
                .. combat.Player.EquippedQuickhacks.Select(QuickhackChoice),
                .. combat.HasSandevistan ? new[] { SandevistanChoice() } : Array.Empty<CombatChoice>(),
                .. combat.SelectedWeapon is { WeaponDefinition.IsMelee: true } ? [] : new[] {
                    new CombatChoice(CombatAction.Reload,
                        $"Recargar — {reloadText}") },
                new CombatChoice(CombatAction.Heal,
                    $"Usar curación — {(heal.Available ? $"[green]+{heal.HealAmount} salud[/] · 1 acción" : $"[grey]No disponible: {Markup.Escape(heal.Reason!)}[/]")}"),
                new CombatChoice(null, "Elegir arma — Gratis · no avanza el turno"),
                new CombatChoice(CombatAction.Wait, "Esperar — 1 acción · recuperás recursos al inicio de tu próximo turno"),
                .. allowWithdrawal ? new[] { new CombatChoice(null, Aventura.WithdrawLabel, Withdraw: true) }
                    : Array.Empty<CombatChoice>()
            ];
            var choice = AnsiConsole.Prompt(UiHelpers.Menu<CombatChoice>(
                    "Elegí tu acción:", combat.Player.Origin)
                .UseConverter(option => option.Text)
                .AddChoices(choices));
            if (choice.Withdraw)
            {
                if (Aventura.ConfirmWithdrawal(combat.Player))
                    combat.Withdraw();
                continue;
            }
            if (choice.Action is null)
            {
                SelectWeapon();
                continue;
            }

            var turn = combat.TakeTurn(choice.Action.Value);
            if (!turn.PlayerAction.Valid)
            {
                UiHelpers.MostrarPanel("Atención",
                    Markup.Escape(turn.PlayerAction.Rejection!), Color.Yellow);
                UiHelpers.Pausar();
                continue;
            }
            if (choice.Action == CombatAction.ActivateSandevistan)
                continue;

            var summary = new List<string> { $"Tu acción: {ActionText(turn.PlayerAction, combat.Player.Name)}" };
            AddShieldResult(summary, turn.PlayerAction.Damage, "enemigo");
            if (turn.OverheatPulse is { } pulse)
            {
                summary.Add($"Pulso: Sobrecalentamiento inflige {pulse.Applied} de daño.");
                AddShieldResult(summary, pulse, "enemigo");
                if (pulse.Killed)
                    summary.Add("El enemigo cae antes de responder.");
            }
            if (turn.EnemyAction is { } enemyAction)
            {
                summary.Add($"Respuesta enemiga: {ActionText(enemyAction, combat.Enemy.Name)}");
                AddShieldResult(summary, enemyAction.Damage, "del jugador");
            }
            if (turn.BlockedEnemyAction is { } blocked)
                summary.Add(blocked == CombatAction.Reload
                    ? "Arma bloqueada: el enemigo no recarga." : "Arma bloqueada: el enemigo no dispara.");
            if (turn.EnemySkippedBySandevistan)
                summary.Add("Sandevistan: el enemigo pierde su acción.");
            if (turn.PlayerCyberwareRestored)
                summary.Add("Tu cyberware se reactivó.");
            if (turn.EnemyCyberwareRestored)
                summary.Add("Cyberware enemigo reactivado.");
            Transiciones.Resumen($"Resumen del turno {turnNumber}", summary,
                UiHelpers.ColorOrigen(combat.Player.Origin));

            if (turn.Status != CombatStatus.InProgress)
                UiHelpers.MostrarPanel("Resultado", turn.Status == CombatStatus.Victory
                    ? "Victoria en el combate."
                    : "Derrota en el combate.",
                    turn.Status == CombatStatus.Victory ? Color.Green : Color.Red);
            UiHelpers.Pausar();
            turnNumber++;
        }

        return combat.Status;
    }

    private static void AddShieldResult(List<string> summary, DamageResult? damage, string owner)
    {
        if (damage is { ShieldPrevented: > 0 })
            summary.Add($"[{UiHelpers.ShieldColor}]Escudo {owner}: evitó {damage.ShieldPrevented} de daño.[/]");
    }

    private string SandevistanStatus() => combat.Player.CyberwareDisabledTurns > 0
        ? "Sandevistan desactivado"
        : combat.SandevistanCooldown > 0
            ? $"Sandevistan · enfriamiento: {combat.SandevistanCooldown}/{CyberwareContent.SandevistanCooldown}"
            : "Sandevistan listo";

    private CombatChoice SandevistanChoice()
    {
        var preview = combat.Preview(CombatAction.ActivateSandevistan);
        return new(CombatAction.ActivateSandevistan, "Activar Sandevistan — "
             + (preview.Available ? "[aqua]Siguiente ataque físico: 100 % de acierto · el enemigo pierde su acción[/]"
                : $"[grey]{Markup.Escape(preview.Reason!)}[/]"));
    }

    private static string CyberwareStatus(Person combatant, bool selfIceReady) =>
        combatant.CyberwareDisabledTurns > 0
            ? $"\nCyberware desactivado · turnos: {combatant.CyberwareDisabledTurns}"
            : combatant.HasActiveSelfIce
                ? selfIceReady ? "\nSelf-ICE listo" : "\nSelf-ICE · reduce daño de quickhacks"
                : "";

    private CombatChoice QuickhackChoice(QuickhackDefinition hack)
    {
        var preview = combat.Preview(hack.Action);
        var damage = hack.Pulses > 0
            ? $"{preview.Damage} ahora + {hack.Pulses} pulsos de {hack.PulseDamage}"
            : hack.Action == CombatAction.Suicide ? "Ejecución instantánea"
            : preview.Damage is { } direct ? $"{direct} de daño"
            : hack.Action == CombatAction.CyberwareMalfunction ? "Desactiva implantes · 2 turnos"
            : "Bloquea disparo o recarga";
        if (combat.EnemySelfIceReady)
            damage = "Self-ICE bloqueará el efecto";
        var detail = preview.Available
            ? $"[{UiHelpers.RamColor}]{preview.RamCost} RAM[/] · [yellow]{damage}[/] · {hack.Description}"
            : $"[grey]No disponible: {Markup.Escape(preview.Reason!)}[/]";
        return new(hack.Action, $"{Markup.Escape(hack.Name)} — {detail}");
    }

    private static string PreviewText(CombatActionPreview preview, Weapon? weapon = null)
    {
        if (!preview.Available)
            return $"[grey]No disponible: {Markup.Escape(preview.Reason!)}[/]";

         return $"Daño normal: [yellow]{preview.Damage}[/]"
             + $" · Acierto [green]{preview.Accuracy:P0}[/]"
             + $" · Prob. de crítico si acertás: {preview.CriticalPercent} %"
             + $" · Daño crítico: [yellow]{DamageCalculator.CriticalDamage(preview.Damage!.Value)}[/] (×1,5)"
             + (preview.AmmoCost > 0 && weapon is not null ? $" · [cyan]Gasta: {CantidadMunicion(weapon, preview.AmmoCost)}[/]" : "")
             + $" · {preview.StaminaCost} stamina";
    }

    private static string CantidadMunicion(Weapon weapon, int quantity)
    {
        var shell = weapon.WeaponDefinition.Variant is WeaponVariant.PumpShotgun or WeaponVariant.DoubleBarrel;
        var unit = shell ? (quantity == 1 ? "cartucho" : "cartuchos") : (quantity == 1 ? "bala" : "balas");
        return $"{quantity} {unit}";
    }

    private void SelectWeapon()
    {
        var weapons = combat.Player.Equipment.Weapons
            .Select((weapon, index) => new WeaponOption(weapon,
                $"#{index + 1} · {Markup.Escape(weapon.Definition.Name)}"
                  + (weapon.WeaponDefinition.IsMelee ? " · cuerpo a cuerpo"
                      : $" · Cargador: {weapon.LoadedAmmo}/{weapon.MagazineCapacity} · Reserva: {combat.Player.AmmoReserve(weapon)}")))
            .ToList();
        if (weapons.Count == 0)
        {
            UiHelpers.MostrarPanel("Atención", "No tenés armas equipadas.", Color.Yellow);
            UiHelpers.Pausar();
            return;
        }

        weapons.Add(new WeaponOption(null, "Cancelar"));
        var selected = AnsiConsole.Prompt(UiHelpers.Menu<WeaponOption>(
                "Elegí un arma equipada:", combat.Player.Origin)
            .UseConverter(option => option.Text)
            .AddChoices(weapons));
        if (selected.Weapon is not { } weapon)
            return;
        if (combat.TrySelectWeapon(weapon, out var error))
            UiHelpers.MostrarPanel("Arma seleccionada",
                $"Arma seleccionada: {Markup.Escape(weapon.Definition.Name)}.", Color.Green);
        else
            UiHelpers.MostrarPanel("Atención", Markup.Escape(error), Color.Yellow);
        UiHelpers.Pausar();
    }

    private static string ActionText(ActionResult result, string actor)
    {
        if (!result.Valid)
            return "";
        var name = Markup.Escape(actor);
        if (result.BlockedBySelfIce && result.Action is { } blockedAction)
            return $"{name} ejecuta {Markup.Escape(QuickhackContent.Find(blockedAction)!.Name)} "
                + $"(gasta {result.RamSpent} RAM).\nSelf-ICE bloqueó el quickhack.";
        return result.Action switch
        {
            CombatAction.Wait => $"{name} espera.",
            CombatAction.Reload => $"{name} recarga {result.Reloaded} bala(s).",
            CombatAction.Heal => $"{name} recupera {result.Healed} de salud.",
             CombatAction.WeaponGlitch => $"{name} ejecuta Fallo de arma (gasta {result.RamSpent} RAM).",
             CombatAction.RebootOptics => $"{name} ejecuta Reboot Optics (gasta {result.RamSpent} RAM).",
            CombatAction.CyberwareMalfunction => $"{name} ejecuta Fallo de cyberware "
                + $"(gasta {result.RamSpent} RAM).\nImplantes desactivados.",
            CombatAction.Suicide => $"{name} ejecuta Suicidio (gasta {result.RamSpent} RAM).\nEl enemigo cae sin responder.",
            _ when result.ArmAttack is { } arms =>
                $"{(result.Critical ? "[yellow]¡Crítico![/] " : "")}{name} usa {Markup.Escape(arms)}: "
                    + (result.Hit == true ? $"{result.Damage!.Applied} de daño" : "falla")
                    + $" (gasta {result.StaminaSpent} stamina).",
            _ when result.Action is { } action && QuickhackContent.Find(action) is { } hack =>
                $"{name} ejecuta {Markup.Escape(hack.Name)}: "
                    + $"{result.Damage!.Applied} de daño (gasta {result.RamSpent} RAM).",
            _ when result.Hit == false => $"{name} falla su ataque"
                + (result.AmmoSpent > 0 ? $" y gasta {result.AmmoSpent} bala(s)." : "."),
            _ => $"{(result.Critical ? "[yellow]¡Crítico![/] " : "")}{name} acierta e inflige {result.Damage!.Applied} de daño"
                + (result.AmmoSpent > 0 ? $" (gasta {result.AmmoSpent} bala(s))." : ".")
        };
    }

    private sealed record WeaponOption(Weapon? Weapon, string Text);
    private sealed record CombatChoice(CombatAction? Action, string Text, bool Withdraw = false);
}
