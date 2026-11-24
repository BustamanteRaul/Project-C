namespace MySharpVenture;

/// <summary>Balance provisional de carga; todos los cálculos usan gramos enteros.</summary>
internal static class CarryWeight
{
    private const int Ammo = 10;
    internal const int AccessoryGrams = 250;
    internal const int SpareImplantGrams = 750;
    internal const int ComponentGrams = 50;

    internal static int WeaponGrams(WeaponDefinition weapon) => weapon.Variant switch
    {
        WeaponVariant.Pistol or WeaponVariant.Revolver => 1500,
        WeaponVariant.SubmachineGun => 3000,
        WeaponVariant.PrecisionRifle => 5500,
        WeaponVariant.Sword or WeaponVariant.Bat => 2000,
        _ => 4500
    };

    internal static long Grams(Person player) =>
        player.Inventory.Items.Sum(ItemGrams)
        + ((long)player.SpareExtendedMagazines + player.SpareReflexSights) * AccessoryGrams
        + ComponentGrams * (long)player.Components + (long)SpareImplantGrams * player.SpareCyberware.Count;

    internal static long ItemGrams(Item item) => item switch
    {
        Weapon weapon => WeaponGrams(weapon.WeaponDefinition) + (long)weapon.LoadedAmmo * Ammo
            + (weapon.HasExtendedMagazine ? AccessoryGrams : 0)
            + (weapon.HasReflexSight ? AccessoryGrams : 0),
        Consumable stack => (long)stack.Quantity * (stack.Type == ConsumableType.Ammunition ? Ammo : 250),
        _ => 0
    };
}
