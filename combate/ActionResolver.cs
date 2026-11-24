namespace MySharpVenture;

public sealed class ActionResolver(DamageCalculator calculator)
{
    public CombatActionPreview Preview(Entity attacker, Entity target, CombatAction action,
        Weapon? weapon, AttackParameters basicAttack, Consumable? healing = null, bool overheatActive = false,
        long ramSpent = 0, bool selfIceReady = false, int? projectedRam = null,
        Weapon? glitchedWeapon = null, bool opticsDisrupted = false, bool guaranteedPhysicalHit = false)
    {
        static CombatActionPreview Reject(string reason) =>
            new(false, reason, null, null, 0, 0, 0);

        if (attacker.CurrentHealth == 0 || target.CurrentHealth == 0)
            return Reject("El combate ya terminó.");

        if (action == CombatAction.Wait)
            return new(true, null, null, null, 0, 0, 0);

        if (QuickhackContent.Find(action) is { } hack)
        {
            if (attacker is Person { CyberwareDisabledTurns: > 0 })
                return Reject("Tu cyberware está desactivado.");
            if (attacker is not Person { Cyberdeck: not null } hacker)
                return Reject("No tenés un cyberdeck instalado.");
            if (action == CombatAction.Suicide && hacker.Origin is null)
                return Reject("Este quickhack es exclusivo del jugador.");
            if (action == CombatAction.RebootOptics && hacker.Origin is not null)
                return Reject("Este quickhack es exclusivo del enemigo.");
            if (!hacker.IsQuickhackEquipped(action))
                return Reject($"{hack.Name} no está equipado.");
            if (action == CombatAction.Suicide && target is Person { IsBot: true })
                return Reject("Suicidio solo afecta a objetivos humanos.");
            if (action == CombatAction.Overheat && overheatActive)
                return Reject("Sobrecalentamiento ya está activo.");
            if (action == CombatAction.WeaponGlitch && !target.Equipment.Weapons.Any(weapon => !weapon.WeaponDefinition.IsMelee))
                return Reject("El enemigo no tiene un arma equipada.");
            if (action == CombatAction.CyberwareMalfunction
                && target is not Person { InstalledCyberware.Count: > 0, CyberwareDisabledTurns: 0 })
                return Reject("El objetivo no tiene cyberware activo.");
            if ((projectedRam ?? hacker.Ram) < hack.RamCost)
                return Reject($"RAM insuficiente para {hack.Name}.");
            var damage = action == CombatAction.ShortCircuit && target is Person { IsBot: true }
                ? QuickhackContent.ShortCircuitBotDamage : hack.DamageFor(ramSpent);
            var iceTier = (target as Person)?.SelfIceTier ?? 0;
            var defended = iceTier > 0;
            if (action == CombatAction.Suicide)
                return new(true, null, defended && selfIceReady ? 0 : target.CurrentHealth,
                    null, 0, 0, 0, hack.RamCost);
            return new(true, null, damage > 0
                ? defended ? selfIceReady ? 0 : CyberwareContent.ReduceQuickhackDamage(damage, iceTier) : damage
                : null, null, 0, 0, 0, hack.RamCost);
        }

        if (action == CombatAction.Heal)
        {
            if (healing is null || !attacker.Inventory.Contains(healing)
                || healing.Type != ConsumableType.Healing || healing.Quantity < 1)
                return Reject("No tenés esa curación disponible.");
            if (attacker.CurrentHealth == attacker.MaxHealth)
                return Reject("La salud ya está completa.");
            var healAmount = attacker is Person healer
                ? healer.ModifiedAmount(healing.ConsumableDefinition.HealAmount, SkillEffect.Healing)
                : healing.ConsumableDefinition.HealAmount;
            return new(true, null, null, null, 0, 0,
                Math.Min(healAmount, attacker.MaxHealth - attacker.CurrentHealth));
        }

        if (action == CombatAction.BasicAttack)
        {
            basicAttack.Validate();
            var arms = attacker is Person { CyberwareDisabledTurns: 0 } user
                ? user.InstalledCyberware.GetValueOrDefault(CyberwareZone.Arms) : null;
            if (arms is not { ArmDamage: > 0 } || attacker.Stamina < PhysicalCost(attacker, arms.ArmStaminaCost))
                arms = null;
            var cost = PhysicalCost(attacker, arms?.ArmStaminaCost ?? StaminaContent.BasicCost);
            if (attacker.Stamina < cost)
                return Reject($"Stamina insuficiente: necesitás {cost}.") with { StaminaCost = cost };
            return new(true, null,
                attacker is Person person ? person.ModifiedAmount(arms?.ArmDamage ?? basicAttack.Damage, SkillEffect.BasicDamage)
                    : basicAttack.Damage,
                LimitAccuracy(attacker, basicAttack.Accuracy * (1m + ((attacker as Person)?.Bonus(SkillEffect.BasicAccuracy) ?? 0) / 100m)
                    + (arms?.ArmAccuracyOffset ?? 0), opticsDisrupted, guaranteedPhysicalHit), 0, 0, 0, StaminaCost: cost,
                CriticalPercent: DamageCalculator.CriticalChance(attacker), ArmAttack: arms?.Name);
        }

        if (action is not CombatAction.WeaponAttack and not CombatAction.Reload)
            return Reject("Acción inválida.");

        if (weapon is null || !attacker.Inventory.Contains(weapon)
            || !attacker.Equipment.Weapons.Contains(weapon))
            return Reject("Elegí un arma equipada que te pertenezca.");
        if (weapon == glitchedWeapon)
            return Reject("Fallo de arma: esta arma no puede disparar ni recargar.");

        var definition = weapon.WeaponDefinition;
        if (action == CombatAction.WeaponAttack)
        {
            if (weapon.LoadedAmmo < definition.AmmoPerAttack)
                return Reject("El arma no tiene suficiente munición cargada.");
            var cost = PhysicalCost(attacker, definition.IsMelee ? StaminaContent.BasicCost : StaminaContent.WeaponCost);
            if (attacker.Stamina < cost)
                return Reject($"Stamina insuficiente: necesitás {cost}.") with { StaminaCost = cost };
            return new(true, null,
                attacker is Person person ? person.ModifiedAmount(definition.Damage, SkillEffect.WeaponDamage)
                    : definition.Damage,
                LimitAccuracy(attacker, weapon.AccuracyWith(attacker is Person marksman ? marksman.Bonus(SkillEffect.WeaponAccuracy) : 0),
                    opticsDisrupted, guaranteedPhysicalHit),
                definition.AmmoPerAttack, 0, 0, StaminaCost: cost,
                CriticalPercent: DamageCalculator.CriticalChance(attacker));
        }

        var amount = attacker.PreviewReload(weapon, out var error);
        if (amount == 0)
            return Reject(error);
        return new(true, null, null, null, 0,
            amount, 0);
    }

    private static int PhysicalCost(Entity attacker, int cost) =>
        cost + (attacker is Person { Origin: not null, IsOverweight: true } ? StaminaContent.OverweightCost : 0);

    private static decimal LimitAccuracy(Entity attacker, decimal accuracy, bool disrupted, bool guaranteed) =>
        guaranteed ? 1m : Math.Clamp(accuracy + ((attacker as Person)?.ImplantBonus(implant => implant.AccuracyPoints) ?? 0) / 100m,
            0m, disrupted ? QuickhackContent.DisruptedAccuracy : 1m);

    public ActionResult Resolve(Entity attacker, Entity target, CombatAction action, Weapon? weapon,
        AttackParameters basicAttack, Consumable? healing = null, bool overheatActive = false, long ramSpent = 0,
        bool selfIceReady = false, Weapon? glitchedWeapon = null, bool opticsDisrupted = false, bool guaranteedPhysicalHit = false)
    {
        var preview = Preview(attacker, target, action, weapon, basicAttack, healing, overheatActive, ramSpent, selfIceReady,
            glitchedWeapon: glitchedWeapon, opticsDisrupted: opticsDisrupted, guaranteedPhysicalHit: guaranteedPhysicalHit);
        if (!preview.Available)
            return ActionResult.Rejected(preview.Reason!);

        switch (action)
        {
            case CombatAction.Wait:
                return ActionResult.Waited();
            case CombatAction.Reload:
            {
                if (!attacker.TryReload(weapon, out var reloaded, out var error))
                    return ActionResult.Rejected(error);
                return new(true, null, true, 0, null, null, action,
                    Reloaded: reloaded);
            }
            case CombatAction.Heal:
                attacker.Consume(healing!, 1);
                var recovered = attacker.Heal(preview.HealAmount);
                return new(true, null, true, 0, null, null, action, Healed: recovered);
            case CombatAction.ShortCircuit:
            case CombatAction.Overheat:
            case CombatAction.SynapseBurnout:
            case CombatAction.WeaponGlitch:
            case CombatAction.RebootOptics:
            case CombatAction.CyberwareMalfunction:
            case CombatAction.Suicide:
                ((Person)attacker).SpendRam(preview.RamCost);
                if (selfIceReady && target is Person { HasActiveSelfIce: true })
                    return new(true, null, true, 0, null, null, action,
                        RamSpent: preview.RamCost, BlockedBySelfIce: true);
                if (action == CombatAction.CyberwareMalfunction)
                    ((Person)target).DisableCyberware(QuickhackContent.CyberwareDisableTurns);
                return new(true, null, true, 0, preview.Damage.HasValue ? true : null,
                    preview.Damage is > 0 ? target.ReceiveDamage(preview.Damage.Value,
                        bypassShield: action == CombatAction.Suicide) : null,
                    action, RamSpent: preview.RamCost);
            case CombatAction.BasicAttack:
            case CombatAction.WeaponAttack:
                attacker.SpendStamina(preview.StaminaCost);
                if (preview.AmmoCost > 0)
                    weapon!.SpendAmmo(preview.AmmoCost);
                var attack = new AttackParameters(preview.Damage!.Value, preview.Accuracy!.Value);
                var hit = calculator.Hits(attack);
                var critical = hit && calculator.IsCritical(preview.CriticalPercent);
                var damage = hit ? target.ReceiveDamage(critical ? DamageCalculator.CriticalDamage(attack.Damage) : attack.Damage) : null;
                return new(true, null, true, preview.AmmoCost, hit, damage, action,
                    StaminaSpent: preview.StaminaCost, Critical: critical, ArmAttack: preview.ArmAttack);
            default:
                throw new InvalidOperationException("La acción validada no tiene resolución.");
        }
    }
}
