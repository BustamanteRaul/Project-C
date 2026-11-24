namespace MySharpVenture;

public sealed class CombatController
{
    private readonly ActionResolver _resolver;
    private readonly AttackParameters _playerBasic;
    private readonly AttackParameters _enemyBasic;
    private readonly Weapon? _enemyWeapon;
    private long _ramSpent;
    private bool _enemySelfIceReady = true;
    private bool _playerSelfIceReady = true;
    public Weapon? GlitchedPlayerWeapon { get; private set; }
    public bool PlayerOpticsDisrupted { get; private set; }

    public Person Player { get; }
    public Person Enemy { get; }
    public string EnemyAffiliation { get; }
    /// <summary>Acción prevista según el cargador y la reserva enemigos actuales.</summary>
    public CombatAction PlannedEnemyAction
        => ChooseEnemyAction(Math.Min(Enemy.MaxStamina, Enemy.Stamina + StaminaContent.Regeneration),
            Enemy.RamAfterRecovery());

    private CombatAction ChooseEnemyAction(int stamina, int ram)
    {
        foreach (var hack in new[] { CombatAction.WeaponGlitch, CombatAction.RebootOptics,
            CombatAction.CyberwareMalfunction, CombatAction.ShortCircuit })
            if (Enemy.IsQuickhackEquipped(hack)
                && (hack != CombatAction.WeaponGlitch || GlitchedPlayerWeapon is null)
                && (hack != CombatAction.RebootOptics || !PlayerOpticsDisrupted)
                && _resolver.Preview(Enemy, Player, hack, null, _enemyBasic, projectedRam: ram).Available)
                return hack;
        if (_enemyWeapon is not null)
        {
            if (stamina >= StaminaContent.WeaponCost && _enemyWeapon.LoadedAmmo >= _enemyWeapon.WeaponDefinition.AmmoPerAttack)
                return CombatAction.WeaponAttack;
            if (Enemy.PreviewReload(_enemyWeapon, out _) > 0)
                return CombatAction.Reload;
        }
        return stamina >= StaminaContent.BasicCost ? CombatAction.BasicAttack : CombatAction.Wait;
    }
    public string EnemyIntention => PlannedEnemyAction switch
    {
        CombatAction.WeaponAttack => "Prepara un disparo",
        CombatAction.Reload => "Prepara una recarga",
        CombatAction.CyberwareMalfunction => "Prepara Fallo de cyberware",
        CombatAction.ShortCircuit => "Prepara Cortocircuito",
        CombatAction.WeaponGlitch => "Prepara Fallo de arma",
        CombatAction.RebootOptics => "Prepara Reboot Optics",
        CombatAction.Wait => "Espera para recuperar stamina",
        _ => "Prepara un ataque cuerpo a cuerpo"
    };
    public string EnemyDamageEstimate
    {
        get
        {
            var damage = PlannedEnemyAction == CombatAction.WeaponAttack
                ? _enemyWeapon!.WeaponDefinition.Damage : _enemyBasic.Damage;
            var upper = (((long)damage - 1) / 10 + 1) * 10;
            return $"{upper - 9}–{upper}";
        }
    }
    public decimal EnemyMissChance => 1 - (PlannedEnemyAction == CombatAction.WeaponAttack
        ? _enemyWeapon!.AccuracyWith(Enemy.Bonus(SkillEffect.WeaponAccuracy)) : _enemyBasic.Accuracy);
    public Weapon? SelectedWeapon { get; private set; }
    public CombatStatus Status { get; private set; } = CombatStatus.InProgress;
    /// <summary>Pulsos pendientes de Sobrecalentamiento en este combate.</summary>
    public int OverheatPulses { get; private set; }
    public bool EnemySelfIceReady => Enemy.HasActiveSelfIce && _enemySelfIceReady;
    public bool PlayerSelfIceReady => Player.HasActiveSelfIce && _playerSelfIceReady;
    public bool HasSandevistan => Player.InstalledCyberware.GetValueOrDefault(CyberwareZone.OperatingSystem)
        ?.Code == CyberwareContent.Sandevistan.Code;
    /// <summary>Indica que queda una acción del jugador antes de omitir la respuesta enemiga.</summary>
    public bool SandevistanActive { get; private set; }
    /// <summary>Turnos propios completos de enfriamiento, sin contar el de activación.</summary>
    public int SandevistanCooldown { get; private set; }

    public CombatController(Person player, EnemyProfile profile,
        AttackParameters playerBasic, Random random)
    {
        Player = player;
        Enemy = profile.CreateEnemy();
        _enemyWeapon = Enemy.Equipment.Weapons.SingleOrDefault();
        EnemyAffiliation = profile.Affiliation;
        playerBasic.Validate();
        _playerBasic = playerBasic;
        _enemyBasic = profile.BasicAttack;
        _resolver = new ActionResolver(new DamageCalculator(random));
        SelectedWeapon = player.Equipment.Weapons.FirstOrDefault();
        if (player.CurrentHealth == 0)
            Status = CombatStatus.Defeat;
        else
            Player.RecoverTurnResources();
    }

    public bool TrySelectWeapon(Weapon? weapon, out string error)
    {
        if (Status != CombatStatus.InProgress)
            error = "El combate ya terminó.";
        else if (weapon is null || !Player.Inventory.Contains(weapon)
            || !Player.Equipment.Weapons.Contains(weapon))
            error = "Solo podés seleccionar un arma propia y equipada.";
        else
        {
            SelectedWeapon = weapon;
            error = string.Empty;
            return true;
        }

        return false;
    }

    public CombatActionPreview Preview(CombatAction action, Consumable? healing = null)
    {
        if (Status != CombatStatus.InProgress)
            return new(false, "El combate ya terminó.", null, null, 0, 0, 0);
        if (action == CombatAction.ActivateSandevistan)
        {
            var reason = !HasSandevistan ? "No tenés un Sandevistan instalado."
                : Player.CyberwareDisabledTurns > 0 ? "Tu cyberware está desactivado."
                : SandevistanActive ? "Sandevistan ya está activo."
                : SandevistanCooldown > 0 ? "Sandevistan en enfriamiento." : null;
            return new(reason is null, reason, null, null, 0, 0, 0);
        }
        return _resolver.Preview(Player, Enemy, action, SelectedWeapon,
            _playerBasic, healing ?? GetHealing(), OverheatPulses > 0, _ramSpent, EnemySelfIceReady,
            glitchedWeapon: GlitchedPlayerWeapon, opticsDisrupted: PlayerOpticsDisrupted, guaranteedPhysicalHit: SandevistanActive);
    }

    private Consumable? GetHealing() => Player.Inventory.Items.OfType<Consumable>()
        .FirstOrDefault(item => item.Type == ConsumableType.Healing);

    public CombatTurnResult TakeTurn(CombatAction action, Consumable? healing = null)
    {
        if (Status != CombatStatus.InProgress)
            return new CombatTurnResult(ActionResult.Rejected("El combate ya terminó."), null, Status);
        if (action == CombatAction.ActivateSandevistan)
        {
            var preview = Preview(action);
            if (!preview.Available)
                return new(ActionResult.Rejected(preview.Reason!), null, Status);
            SandevistanActive = true;
            SandevistanCooldown = CyberwareContent.SandevistanCooldown;
            return new(new ActionResult(true, null, true, 0, null, null, action), null, Status);
        }

        var result = _resolver.Resolve(Player, Enemy, action, SelectedWeapon,
            _playerBasic, healing ?? GetHealing(), OverheatPulses > 0, _ramSpent, EnemySelfIceReady,
            GlitchedPlayerWeapon, PlayerOpticsDisrupted, SandevistanActive);
        if (!result.ConsumedAction)
            return new CombatTurnResult(result, null, Status);
        var skipEnemy = SandevistanActive;
        SandevistanActive = false;
        if (!skipEnemy && SandevistanCooldown > 0)
            SandevistanCooldown--;
        _ramSpent += result.RamSpent;
        if (result.BlockedBySelfIce)
            _enemySelfIceReady = false;
        var playerWasDisabled = Player.CyberwareDisabledTurns > 0;

        DamageResult? pulse = null;
        if (Enemy.CurrentHealth > 0)
        {
            if (action == CombatAction.Overheat && !result.BlockedBySelfIce)
                OverheatPulses = QuickhackContent.Overheat.Pulses;
            if (OverheatPulses > 0)
            {
                OverheatPulses--;
                var damage = QuickhackContent.Overheat.PulseDamage;
                pulse = Enemy.ReceiveDamage(Enemy.HasActiveSelfIce
                    ? CyberwareContent.ReduceQuickhackDamage(damage, Enemy.SelfIceTier) : damage);
            }
        }
        if (Enemy.CurrentHealth == 0)
        {
            Player.Heal(Player.ImplantBonus(implant => implant.HealOnKill));
            Status = CombatStatus.Victory;
            ClearCombatEffects();
            return new CombatTurnResult(result, null, Status, pulse);
        }

        Enemy.RecoverTurnResources();
        var planned = ChooseEnemyAction(Enemy.Stamina, Enemy.Ram);
        var blocked = action == CombatAction.WeaponGlitch && !result.BlockedBySelfIce
            && planned is CombatAction.WeaponAttack or CombatAction.Reload;
        var response = blocked || skipEnemy ? null : _resolver.Resolve(Enemy, Player, planned, _enemyWeapon, _enemyBasic,
            selfIceReady: PlayerSelfIceReady);
        if (response?.BlockedBySelfIce == true)
            _playerSelfIceReady = false;
        GlitchedPlayerWeapon = response is { Valid: true, BlockedBySelfIce: false, Action: CombatAction.WeaponGlitch }
            ? SelectedWeapon is { WeaponDefinition.IsMelee: false } selected ? selected
                : Player.Equipment.Weapons.FirstOrDefault(weapon => !weapon.WeaponDefinition.IsMelee)
            : null;
        PlayerOpticsDisrupted = response is { Valid: true, BlockedBySelfIce: false, Action: CombatAction.RebootOptics };
        var playerRestored = playerWasDisabled && Player.TickCyberware();
        var enemyRestored = Enemy.TickCyberware();
        if (Player.CurrentHealth == 0)
        {
            Status = CombatStatus.Defeat;
            ClearCombatEffects();
        }
        else
            Player.RecoverTurnResources();
        return new CombatTurnResult(result, response, Status, pulse, blocked ? planned : null,
            enemyRestored, playerRestored, EnemySkippedBySandevistan: skipEnemy);
    }

    /// <summary>Termina el combate por retirada sin resolver otra acción ni recuperar recursos.</summary>
    public void Withdraw()
    {
        if (Status != CombatStatus.InProgress || Player.CurrentHealth == 0 || Enemy.CurrentHealth == 0)
            throw new InvalidOperationException("El combate ya terminó.");
        Status = CombatStatus.Withdrawn;
        ClearCombatEffects();
    }

    private void ClearCombatEffects()
    {
        OverheatPulses = 0;
        SandevistanActive = false;
        SandevistanCooldown = 0;
        GlitchedPlayerWeapon = null;
        PlayerOpticsDisrupted = false;
        Player.DisableCyberware(0);
        Enemy.DisableCyberware(0);
    }
}
