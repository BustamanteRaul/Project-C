namespace MySharpVenture;

public enum CyberwareZone { OperatingSystem, FrontalCortex, Eyes, Circulatory, NervousSystem, Integumentary, Skeleton, Hands, Arms, Legs }

/// <summary>Definición de un implante de sesión y sus efectos de RAM.</summary>
public sealed record CyberwareDefinition(string Code, string Name, CyberwareZone Zone,
    int CapacityCost, int MaxRam = 0, int RamPerTurn = 0, int BonusRam = 0, int Tier = 1, int MaxShield = 0,
    int ArmDamage = 0, int ArmStaminaCost = 0, decimal ArmAccuracyOffset = 0,
    int AccuracyPoints = 0, int CriticalPoints = 0, int HealingPercent = 0, int HealOnKill = 0,
    int ShieldRecovery = 0, int StaminaRecovery = 0, int BasicDamagePercent = 0, int CarryBonusGrams = 0);

public static class CyberwareContent
{
    public const int StartingCapacity = 20;
    public const int ProcedureFee = 5;
    public const int ExDiskPrice = 30;
    public const int SelfIcePrice = 40;
    public const int SubdermalArmorPrice = 50;
    internal const int ShieldPerTurn = 2;
    public const int SandevistanPrice = 60;
    public const int SandevistanCooldown = 4;
    public const int SelfIceDamagePercent = 75;
    public static CyberwareDefinition StartingDeck { get; } =
        new("militech-paraline", "Militech Paraline", CyberwareZone.OperatingSystem, 8, 6, 1);
    public static CyberwareDefinition ExDisk { get; } =
        new("ex-disk", "Ex-Disk", CyberwareZone.FrontalCortex, 4, BonusRam: 2);
    public static CyberwareDefinition SelfIce { get; } =
        new("self-ice", "Self-ICE", CyberwareZone.FrontalCortex, 5);
    public static CyberwareDefinition SubdermalArmor { get; } =
        new("subdermal-armor", "Subdermal Armor", CyberwareZone.Integumentary, 6, MaxShield: 12);
    public static CyberwareDefinition Sandevistan { get; } =
        new("dynalar-sandevistan", "Dynalar Sandevistan", CyberwareZone.OperatingSystem, 10);
    public static CyberwareDefinition GorillaArms { get; } =
        new("gorilla-arms", "Gorilla Arms", CyberwareZone.Arms, 6, ArmDamage: 9, ArmStaminaCost: 7);
    public static CyberwareDefinition MantisBlades { get; } =
        new("mantis-blades", "Mantis Blades", CyberwareZone.Arms, 7, ArmDamage: 11, ArmStaminaCost: 8, ArmAccuracyOffset: 0.05m);
    public static CyberwareDefinition Monowire { get; } =
        new("monowire", "Monowire", CyberwareZone.Arms, 7, ArmDamage: 8, ArmStaminaCost: 6);
    public static CyberwareDefinition ProjectileLaunchSystem { get; } =
        new("projectile-launch-system", "Projectile Launch System", CyberwareZone.Arms, 9,
            ArmDamage: 15, ArmStaminaCost: 10, ArmAccuracyOffset: -0.10m);
    /// <summary>Implante de esqueleto que aumenta el límite de carga mientras está activo.</summary>
    public static CyberwareDefinition TitaniumBones { get; } =
        new("titanium-bones", "Titanium Bones", CyberwareZone.Skeleton, 5, CarryBonusGrams: 4000);
    internal static IReadOnlyList<(CyberwareDefinition Implant, int Price)> ExtraOffers { get; } =
    [
        (GorillaArms, 45), (MantisBlades, 55), (Monowire, 60), (ProjectileLaunchSystem, 65),
        (new("tetratronic-rippler", "Tetratronic Rippler", CyberwareZone.OperatingSystem, 12, 9, 2), 70),
        (new("kiroshi", "Kiroshi básico", CyberwareZone.Eyes, 3, AccuracyPoints: 3), 25),
        (new("kiroshi-cockatrice", "Kiroshi Cockatrice", CyberwareZone.Eyes, 6, CriticalPoints: 5), 50),
        (new("blood-pump", "Blood Pump", CyberwareZone.Circulatory, 5, HealingPercent: 20), 35),
        (new("heal-on-kill", "Heal-On-Kill", CyberwareZone.Circulatory, 6, HealOnKill: 5), 45),
        (new("neofiber", "NeoFiber", CyberwareZone.NervousSystem, 5, MaxShield: 4), 35),
        (new("reflex-tuner", "Reflex Tuner", CyberwareZone.NervousSystem, 6, AccuracyPoints: 5), 45),
        (new("shock-n-awe", "Shock-n-Awe", CyberwareZone.Integumentary, 7, MaxShield: 8, ShieldRecovery: 1), 55),
        (new("dense-marrow", "Dense Marrow", CyberwareZone.Skeleton, 5, BasicDamagePercent: 20), 35),
        (new("microgenerator", "Microgenerator", CyberwareZone.Hands, 5, CriticalPoints: 4), 35),
        (new("immovable-force", "Immovable Force", CyberwareZone.Hands, 5, AccuracyPoints: 4), 40),
        (new("jenkins-tendons", "Jenkins’ Tendons", CyberwareZone.Legs, 6, StaminaRecovery: 1), 40),
        (new("leeroy-ligament-system", "Leeroy Ligament System", CyberwareZone.Legs, 5, BasicDamagePercent: 15), 35),
        (TitaniumBones, 40)
    ];

    internal static IReadOnlyList<(CyberwareDefinition Implant, int Price)> ShopOffers { get; } =
    [
        (ExDisk, ExDiskPrice), (SelfIce, SelfIcePrice), (SubdermalArmor, SubdermalArmorPrice),
        (Sandevistan, SandevistanPrice), .. ExtraOffers
    ];

    internal static CyberwareDefinition? NextTier(CyberwareDefinition implant)
    {
        if (implant.Tier is < 1 or >= 5)
            return null;
        return implant.Code switch
        {
            "militech-paraline" => implant with
            {
                Tier = implant.Tier + 1, CapacityCost = implant.CapacityCost + 2, MaxRam = implant.MaxRam + 1
            },
            "ex-disk" => implant with
            {
                Tier = implant.Tier + 1, CapacityCost = implant.CapacityCost + 1, BonusRam = implant.BonusRam + 1
            },
            "dynalar-sandevistan" => implant with { Tier = implant.Tier + 1, CapacityCost = implant.CapacityCost - 1 },
            "self-ice" => implant with { Tier = implant.Tier + 1, CapacityCost = implant.CapacityCost + 1 },
            _ => implant with
            {
                Tier = implant.Tier + 1, CapacityCost = implant.CapacityCost + 1,
                MaxRam = Grow(implant.MaxRam), MaxShield = Grow(implant.MaxShield),
                ArmDamage = Grow(implant.ArmDamage), AccuracyPoints = Grow(implant.AccuracyPoints),
                CriticalPoints = Grow(implant.CriticalPoints), HealingPercent = Grow(implant.HealingPercent),
                HealOnKill = Grow(implant.HealOnKill), ShieldRecovery = Grow(implant.ShieldRecovery),
                StaminaRecovery = Grow(implant.StaminaRecovery), BasicDamagePercent = Grow(implant.BasicDamagePercent),
                CarryBonusGrams = implant.CarryBonusGrams > 0 ? implant.CarryBonusGrams + 1000 : 0
            }
        };
    }

    private static int Grow(int value) => value > 0 ? value + 1 : 0;

    internal static int ReduceQuickhackDamage(int damage, int tier) =>
        (int)((long)damage * (SelfIceDamagePercent - 5 * (tier - 1)) / 100);

    internal static string Describe(CyberwareDefinition implant) => string.Join(" · ", new[]
    {
        implant.MaxRam > 0 ? $"Permite equipar hasta {QuickhackContent.DeckSlots} quickhacks. {implant.MaxRam} RAM máxima; recupera {implant.RamPerTurn} por turno" : null,
        implant.BonusRam > 0 ? $"+{implant.BonusRam} RAM máxima con deck" : null,
        implant.Code == SelfIce.Code ? $"Bloquea el primer quickhack recibido en cada combate; reduce un {25 + 5 * (implant.Tier - 1)} % el daño de los posteriores" : null,
        implant.Code == Sandevistan.Code ? $"Garantiza el acierto de tu siguiente ataque físico y le quita su acción al enemigo. Enfriamiento: {SandevistanCooldown} turnos propios" : null,
        implant.MaxShield > 0 ? $"+{implant.MaxShield} escudo máximo" : null,
        implant.ArmDamage > 0 ? $"Reemplaza el ataque básico sin ocupar una ranura de arma. {implant.ArmDamage} de daño base; cuesta {implant.ArmStaminaCost} stamina" : null,
        implant.AccuracyPoints > 0 ? $"+{implant.AccuracyPoints} puntos de acierto físico" : null,
        implant.CriticalPoints > 0 ? $"+{implant.CriticalPoints} puntos de crítico" : null,
        implant.HealingPercent > 0 ? $"+{implant.HealingPercent} % de curación" : null,
        implant.HealOnKill > 0 ? $"+{implant.HealOnKill} salud al vencer" : null,
        implant.ShieldRecovery > 0 ? $"+{implant.ShieldRecovery} escudo/turno" : null,
        implant.StaminaRecovery > 0 ? $"+{implant.StaminaRecovery} stamina/turno" : null,
        implant.BasicDamagePercent > 0 ? $"+{implant.BasicDamagePercent} % daño básico" : null,
        implant.CarryBonusGrams > 0 ? $"+{implant.CarryBonusGrams / 1000} kg de carga" : null
    }.Where(text => text is not null));
}
