namespace MySharpVenture;

public enum WeaponVariant
{
    Pistol,
    SubmachineGun,
    PumpShotgun,
    Revolver,
    DoubleBarrel,
    AssaultRifle,
    PrecisionRifle,
    Sword,
    Bat
}

public enum ConsumableType
{
    Ammunition,
    Healing
}

// Datos compartidos e inmutables. El estado de uso pertenece a Item.
public abstract record ItemDefinition(
    int CatalogItemId, string Code, string Name, int? Tier);

public sealed record WeaponDefinition(
    int CatalogItemId, string Code, string Name, int? Tier,
    WeaponVariant Variant, int MagazineCapacity, int AmmoCatalogItemId,
    int Damage, decimal Accuracy, int AmmoPerAttack)
    : ItemDefinition(CatalogItemId, Code, Name, Tier)
{
    /// <summary>Etiqueta descriptiva sin efectos sobre el combate.</summary>
    public string? Technology => Variant switch
    {
        WeaponVariant.PrecisionRifle => "Tech",
        WeaponVariant.AssaultRifle => "Smart",
        WeaponVariant.Sword or WeaponVariant.Bat => null,
        _ => "Power"
    };

    /// <summary>Indica si el arma no usa cargador ni munición.</summary>
    public bool IsMelee => Variant is WeaponVariant.Sword or WeaponVariant.Bat;
    internal bool SupportsExtendedMagazine => Variant is WeaponVariant.Pistol or WeaponVariant.AssaultRifle;
    internal bool SupportsReflexSight => Variant is WeaponVariant.Pistol or WeaponVariant.AssaultRifle or WeaponVariant.PrecisionRifle;
}

public sealed record ConsumableDefinition(
    int CatalogItemId, string Code, string Name, ConsumableType Type, int HealAmount = 0)
    : ItemDefinition(CatalogItemId, Code, Name, null);
