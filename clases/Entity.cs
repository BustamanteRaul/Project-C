namespace MySharpVenture;

public abstract class Entity
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; }
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; }
    public int MaxStamina => StaminaContent.Maximum;
    public int Stamina { get; private set; } = StaminaContent.Maximum;
    public virtual int MaxShield => 0;
    public int Shield { get; private set; }

    internal void RecoverShield(int turns = 1, int bonus = 0) =>
        Shield = Math.Min(MaxShield, Shield + (CyberwareContent.ShieldPerTurn + bonus) * turns);

    internal void RestoreShield() => Shield = MaxShield;

    protected void ClampShield() => Shield = Math.Min(Shield, MaxShield);

    internal void RecoverStamina(int turns = 1, int bonus = 0) =>
        Stamina = Math.Min(MaxStamina, Stamina + (StaminaContent.Regeneration + bonus) * turns);

    internal void RestoreStamina() => Stamina = MaxStamina;

    internal void SpendStamina(int cost)
    {
        if (cost <= 0 || cost > Stamina)
            throw new ArgumentOutOfRangeException(nameof(cost));
        Stamina -= cost;
    }
    public Inventory Inventory { get; } = new();
    public Equipment Equipment { get; }

    protected Entity(string name, int maxHealth)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHealth);
        Name = name;
        CurrentHealth = MaxHealth = maxHealth;
        Equipment = new Equipment(Inventory);
    }

    protected Entity(Entity source) : this(source.Name, source.MaxHealth)
    {
        CurrentHealth = source.CurrentHealth;
        Stamina = source.Stamina;
        Shield = source.Shield;
        var weapons = new Dictionary<Weapon, Weapon>();
        foreach (var item in source.Inventory.Items)
        {
            if (item is Weapon weapon)
            {
                var copy = new Weapon(weapon.WeaponDefinition, weapon.LoadedAmmo, weapon.HasExtendedMagazine)
                {
                    HasReflexSight = weapon.HasReflexSight
                };
                Inventory.Add(copy);
                weapons.Add(weapon, copy);
            }
            else if (item is Consumable stack)
                Inventory.Add(new Consumable(stack.ConsumableDefinition, stack.Quantity));
            else
                throw new InvalidOperationException("No se puede copiar un objeto desconocido para la práctica.");
        }
        foreach (var weapon in source.Equipment.Weapons)
            if (!Equipment.TryEquip(weapons[weapon], out var error))
                throw new InvalidOperationException(error);
    }

    public DamageResult ReceiveDamage(int damage, bool bypassShield = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(damage);
        var prevented = bypassShield || MaxShield == 0 ? 0
            : (int)Math.Min(Shield, (long)damage * Shield / (2L * MaxShield));
        Shield -= prevented;
        var applied = Math.Min(CurrentHealth, damage - prevented);
        CurrentHealth -= applied;
        return new DamageResult(damage, applied, CurrentHealth, CurrentHealth == 0, prevented);
    }

    public int Heal(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (CurrentHealth == 0)
            throw new InvalidOperationException("No se puede curar a una entidad muerta.");
        var applied = Math.Min(MaxHealth - CurrentHealth, amount);
        CurrentHealth += applied;
        return applied;
    }

    /// <summary>Calcula cuántas balas compatibles caben en un arma equipada, sin modificar recursos.</summary>
    public int PreviewReload(Weapon? weapon, out string error)
    {
        if (CurrentHealth == 0 || weapon is null || !Inventory.Contains(weapon)
            || !Equipment.Weapons.Contains(weapon))
            error = "Elegí un arma equipada que te pertenezca.";
        else if (weapon.WeaponDefinition.IsMelee)
            error = "Las armas cuerpo a cuerpo no se recargan.";
        else if (weapon.LoadedAmmo == weapon.MagazineCapacity)
            error = "El cargador ya está lleno.";
        else
        {
            var reserve = AmmoReserve(weapon);
            if (reserve == 0)
                error = "No tenés munición compatible en reserva.";
            else
            {
                error = string.Empty;
                return (int)Math.Min(weapon.MagazineCapacity - weapon.LoadedAmmo, reserve);
            }
        }
        return 0;
    }

    internal long AmmoReserve(Weapon weapon) => Inventory.Items.OfType<Consumable>()
        .Where(item => item.Type == ConsumableType.Ammunition && item.CatalogItemId == weapon.WeaponDefinition.AmmoCatalogItemId)
        .Sum(item => (long)item.Quantity);

    /// <summary>Recarga un arma equipada desde la reserva; un rechazo no consume munición.</summary>
    public bool TryReload(Weapon? weapon, out int reloaded, out string error)
    {
        reloaded = PreviewReload(weapon, out error);
        if (reloaded == 0)
            return false;
        var remaining = reloaded;
        foreach (var stack in Inventory.Items.OfType<Consumable>()
            .Where(item => item.Type == ConsumableType.Ammunition
                && item.CatalogItemId == weapon!.WeaponDefinition.AmmoCatalogItemId).ToArray())
        {
            if (remaining == 0)
                break;
            var taken = Math.Min(remaining, stack.Quantity);
            Consume(stack, taken);
            remaining -= taken;
        }
        weapon!.LoadAmmo(reloaded);
        return true;
    }

    // El consumo total retira la pila agotada y no permite reutilizarla.
    internal void Consume(Consumable item, int quantity)
    {
        if (!Inventory.Contains(item) || quantity <= 0 || quantity > item.Quantity)
            throw new InvalidOperationException("Cantidad o consumible no disponible.");
        if (quantity < item.Quantity)
            item.Reduce(quantity);
        else
        {
            Inventory.Remove(item);
            item.MarkDiscarded();
        }
    }
}
