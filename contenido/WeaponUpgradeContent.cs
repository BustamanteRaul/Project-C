namespace MySharpVenture;

internal static class WeaponUpgradeContent
{
    internal static readonly string[] StartingCodes = ["pistola-t1", "subfusil-t1", "escopeta-corredera-t1"];
    internal static ConsumableDefinition RifleAmmo { get; } = new(105, "municion-rifle", "Munición de rifle",
        ConsumableType.Ammunition);
    internal static IReadOnlyList<WeaponDefinition> ExtraFirearms { get; } =
    [
        new(4, "revolver-t1", "Revólver", 1, WeaponVariant.Revolver, 6, 101, 16, 0.82m, 1),
        new(5, "escopeta-doble-t1", "Escopeta de doble cañón", 1, WeaponVariant.DoubleBarrel, 2, 103, 23, 0.73m, 1),
        new(6, "rifle-asalto-t1", "Rifle de asalto", 1, WeaponVariant.AssaultRifle, 24, 105, 17, 0.81m, 2),
        new(7, "rifle-precision-t1", "Rifle de precisión", 1, WeaponVariant.PrecisionRifle, 5, 105, 24, 0.90m, 1)
    ];
    internal static IReadOnlyList<WeaponDefinition> MeleeWeapons { get; } =
    [
        new(8, "espada-t1", "Espada", 1, WeaponVariant.Sword, 0, 0, 16, 0.85m, 0),
        new(9, "bate-t1", "Bate", 1, WeaponVariant.Bat, 0, 0, 11, 0.90m, 0)
    ];

    internal static IEnumerable<WeaponDefinition> AllTiers(WeaponDefinition weapon) =>
        new[] { weapon }.Concat(HigherTiers(weapon));

    internal static int CraftCost(WeaponVariant variant) => variant switch
    {
        WeaponVariant.Pistol => 8,
        WeaponVariant.SubmachineGun or WeaponVariant.Revolver => 10,
        WeaponVariant.AssaultRifle => 14,
        WeaponVariant.PrecisionRifle => 16,
        WeaponVariant.Bat => 7,
        _ => 12
    };

    internal static IEnumerable<WeaponDefinition> HigherTiers(WeaponDefinition starting)
    {
        if (starting.Tier != 1)
            throw new InvalidDataException($"El arma inicial {starting.Code} no es Tier 1.");
        for (var tier = 2; tier <= 5; tier++)
            yield return starting with
            {
                CatalogItemId = 200 + (int)starting.Variant * 10 + tier,
                Code = CodeForTier(starting.Code, tier),
                Tier = tier,
                Damage = checked(starting.Damage + 2 * (tier - 1))
            };
    }

    internal static string CodeForTier(string code, int tier) => $"{code[..^1]}{tier}";
}
