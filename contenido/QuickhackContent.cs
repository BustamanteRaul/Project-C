namespace MySharpVenture;

/// <summary>Coste y daño de un quickhack, incluidos los pulsos por turno enemigo.</summary>
public sealed record QuickhackDefinition(CombatAction Action, string Name, int RamCost,
    int Damage, int PulseDamage = 0, int Pulses = 0, int Price = 0, int ComponentCost = 0)
{
    internal string Description => Action switch
    {
        CombatAction.ShortCircuit => "Inflige daño eléctrico inmediato; hace más daño a los bots.",
        CombatAction.Overheat => "Inflige daño inmediato y después aplica un pulso al inicio de cada turno enemigo.",
        CombatAction.SynapseBurnout => "Inflige más daño según la RAM ya gastada en este combate, hasta un límite.",
        CombatAction.WeaponGlitch => "Impide que el arma afectada dispare o recargue en el siguiente turno.",
        CombatAction.CyberwareMalfunction => "Anula temporalmente los implantes activos y sus bonificaciones; no los extrae.",
        CombatAction.Suicide => "Elimina a un objetivo humano si no lo bloquea Self-ICE; no funciona contra bots.",
        CombatAction.RebootOptics => "Limita el acierto físico durante el siguiente turno; no afecta a los quickhacks.",
        _ => ""
    };

    /// <summary>Daño inmediato previsto a partir de la RAM gastada antes de lanzar el quickhack.</summary>
    public int DamageFor(long ramSpent) => Action == CombatAction.SynapseBurnout
        ? Damage + (int)(2 * Math.Clamp(ramSpent, 0, 8)) : Damage;
}

/// <summary>Balance de los quickhacks disponibles en esta entrega.</summary>
public static class QuickhackContent
{
    public const int DeckSlots = 4;
    public const int ShortCircuitBotDamage = 18;
    public static QuickhackDefinition ShortCircuit { get; } = new(CombatAction.ShortCircuit, "Cortocircuito", 3, 12, ComponentCost: 4);
    public static QuickhackDefinition Overheat { get; } = new(CombatAction.Overheat, "Sobrecalentamiento", 4, 6, 6, 2, Price: 25, ComponentCost: 6);
    public static QuickhackDefinition SynapseBurnout { get; } =
        new(CombatAction.SynapseBurnout, "Quemado sináptico", 3, 8, Price: 35, ComponentCost: 8);
    public static QuickhackDefinition WeaponGlitch { get; } =
        new(CombatAction.WeaponGlitch, "Fallo de arma", 3, 0, Price: 30, ComponentCost: 8);
    public static QuickhackDefinition CyberwareMalfunction { get; } =
        new(CombatAction.CyberwareMalfunction, "Fallo de cyberware", 4, 0, Price: 45, ComponentCost: 10);
    public const int CyberwareDisableTurns = 2;
    public static QuickhackDefinition Suicide { get; } =
        new(CombatAction.Suicide, "Suicidio", 8, 0, Price: 75, ComponentCost: 16);
    internal static QuickhackDefinition RebootOptics { get; } =
        new(CombatAction.RebootOptics, "Reboot Optics", 2, 0);
    internal const decimal DisruptedAccuracy = 0.05m;
    public static IReadOnlyList<QuickhackDefinition> All { get; } =
        Array.AsReadOnly(new[] { ShortCircuit, Overheat, SynapseBurnout, WeaponGlitch, CyberwareMalfunction, Suicide });

    public static QuickhackDefinition? Find(CombatAction action) => action == CombatAction.RebootOptics
        ? RebootOptics : All.FirstOrDefault(hack => hack.Action == action);
}
