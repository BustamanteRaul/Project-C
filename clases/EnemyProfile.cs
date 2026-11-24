namespace MySharpVenture;

// Perfil reutilizable: cada combate recibe una Person con salud propia.
public sealed record EnemyProfile(
    string Name, string Affiliation, int MaxHealth, AttackParameters BasicAttack, int Level = 1,
    EnemyLoadout? Loadout = null, bool Netrunner = false, bool IsBot = false, bool ControlQuickhacks = false)
{
    public Person CreateEnemy()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(Affiliation);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxHealth);
        if (Level is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(Level));
        BasicAttack.Validate();
        var enemy = new Person(Name, MaxHealth, level: Level) { IsBot = IsBot };
        if (Netrunner)
            enemy.AddNetrunnerCyberware(ControlQuickhacks);
        if (Loadout is { } gear)
        {
            if (gear.Weapon.IsMelee || gear.Ammo.Type != ConsumableType.Ammunition
                || gear.Ammo.CatalogItemId != gear.Weapon.AmmoCatalogItemId)
                throw new ArgumentException("La munición no es compatible con el arma enemiga.", nameof(Loadout));
            ArgumentOutOfRangeException.ThrowIfNegative(gear.Reserve);
            var weapon = new Weapon(gear.Weapon, gear.Loaded);
            enemy.Inventory.Add(weapon);
            if (!enemy.Equipment.TryEquip(weapon, out var error))
                throw new InvalidOperationException(error);
            if (gear.Reserve > 0)
                enemy.Inventory.Add(new Consumable(gear.Ammo, gear.Reserve));
        }
        return enemy;
    }
}

/// <summary>Equipo inicial del enemigo; cada combate crea su propia arma y reserva.</summary>
public sealed record EnemyLoadout(WeaponDefinition Weapon, ConsumableDefinition Ammo, int Loaded, int Reserve);
