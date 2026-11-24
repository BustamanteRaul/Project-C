namespace MySharpVenture;

public enum CombatAction
{
    WeaponAttack,
    BasicAttack,
    Reload,
    Heal,
    Wait,
    ShortCircuit,
    Overheat,
    SynapseBurnout,
    WeaponGlitch,
    CyberwareMalfunction,
    ActivateSandevistan,
    Suicide,
    RebootOptics
}

public enum CombatStatus
{
    InProgress,
    Victory,
    Defeat,
    Withdrawn
}

public sealed record AttackParameters(int Damage, decimal Accuracy)
{
    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Damage);
        if (Accuracy is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(Accuracy));
    }
}

public sealed record DamageResult(int Incoming, int Applied, int RemainingHealth, bool Killed, int ShieldPrevented = 0);

public sealed record ActionResult(
    bool Valid, string? Rejection, bool ConsumedAction, int AmmoSpent,
    bool? Hit, DamageResult? Damage, CombatAction? Action = null,
    int Reloaded = 0, int Healed = 0, int RamSpent = 0, bool BlockedBySelfIce = false, int StaminaSpent = 0,
    bool Critical = false, string? ArmAttack = null)
{
    public static ActionResult Rejected(string reason) =>
        new(false, reason, false, 0, null, null);

    public static ActionResult Waited() =>
        new(true, null, true, 0, null, null, CombatAction.Wait);
}

public sealed record CombatActionPreview(
    bool Available, string? Reason, int? Damage, decimal? Accuracy,
    int AmmoCost, int ReloadAmount, int HealAmount, int RamCost = 0, int StaminaCost = 0, int CriticalPercent = 0,
    string? ArmAttack = null);

public sealed record CombatTurnResult(
    ActionResult PlayerAction, ActionResult? EnemyAction, CombatStatus Status,
    DamageResult? OverheatPulse = null, CombatAction? BlockedEnemyAction = null,
    bool EnemyCyberwareRestored = false, bool PlayerCyberwareRestored = false,
    bool EnemySkippedBySandevistan = false);
