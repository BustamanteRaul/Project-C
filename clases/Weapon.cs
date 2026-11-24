namespace MySharpVenture;

public sealed class Weapon : Item
{
    public WeaponDefinition WeaponDefinition => (WeaponDefinition)Definition;
    public int MagazineCapacity => WeaponDefinition.MagazineCapacity
        + (HasExtendedMagazine ? MagazineContent.ExtraCapacity : 0);
    /// <summary>Indica si esta instancia lleva montado el cargador ampliado.</summary>
    public bool HasExtendedMagazine { get; internal set; }
    public bool HasReflexSight { get; internal set; }
    public bool HasAccessories => HasExtendedMagazine || HasReflexSight;

    internal decimal AccuracyWith(int skillBonus) => Math.Min(1m,
        WeaponDefinition.Accuracy * (1m + skillBonus / 100m)
        + (HasReflexSight ? ReflexSightContent.AccuracyBonus : 0));
    public int LoadedAmmo { get; private set; }

    internal Weapon(WeaponDefinition definition, int loadedAmmo, bool extendedMagazine = false) : base(definition)
    {
        HasExtendedMagazine = extendedMagazine;
        if (loadedAmmo < 0 || loadedAmmo > MagazineCapacity)
            throw new ArgumentOutOfRangeException(nameof(loadedAmmo));
        LoadedAmmo = loadedAmmo;
    }

    internal void SpendAmmo(int quantity)
    {
        if (quantity <= 0 || quantity > LoadedAmmo)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        LoadedAmmo -= quantity;
    }

    internal void UpgradeTo(WeaponDefinition definition) => Definition = definition;

    internal void LoadAmmo(int quantity)
    {
        if (quantity <= 0 || quantity > MagazineCapacity - LoadedAmmo)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        LoadedAmmo += quantity;
    }
}
