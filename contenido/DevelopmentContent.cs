namespace MySharpVenture;

// Contenido provisional para M01/M02. M04 lo reemplaza por lectura desde SQLite.
// Salud y capacidades son balance provisional, no valores fijados por el SDD.
public static class DevelopmentContent
{
    public const int StartingHealth = 100;
    public static AttackParameters BasicAttack { get; } = new(5, 0.85m);
    public static EnemyProfile MaelstromTest { get; } =
        new("Merodeador", "Maelstrom", 40, new AttackParameters(6, 0.75m));

    public static ItemCatalog CreateCatalog()
    {
        ItemDefinition[] definitions =
        [
        new WeaponDefinition(1, "pistola-t1", "Pistola", 1,
            WeaponVariant.Pistol, 12, 101, 12, 0.85m, 1),
        new WeaponDefinition(2, "subfusil-t1", "Subfusil", 1,
            WeaponVariant.SubmachineGun, 30, 102, 10, 0.85m, 1),
        new WeaponDefinition(3, "escopeta-corredera-t1", "Escopeta de corredera", 1,
            WeaponVariant.PumpShotgun, 6, 103, 18, 0.75m, 1),
        new ConsumableDefinition(101, "municion-pistola", "Munición de pistola",
            ConsumableType.Ammunition),
        new ConsumableDefinition(102, "municion-subfusil", "Munición de subfusil",
            ConsumableType.Ammunition),
        new ConsumableDefinition(103, "cartuchos-escopeta", "Cartuchos de escopeta",
            ConsumableType.Ammunition),
        new ConsumableDefinition(104, "curacion", "Curación", ConsumableType.Healing, 25)
        ];
        return new ItemCatalog(definitions.Concat(
            definitions.OfType<WeaponDefinition>().SelectMany(WeaponUpgradeContent.HigherTiers))
            .Append(WeaponUpgradeContent.RifleAmmo)
            .Concat(WeaponUpgradeContent.ExtraFirearms.SelectMany(WeaponUpgradeContent.AllTiers))
            .Concat(WeaponUpgradeContent.MeleeWeapons.SelectMany(WeaponUpgradeContent.AllTiers))
            .Concat(CybergearStashContent.All));
    }
}
