using Microsoft.Data.Sqlite;

namespace MySharpVenture;

/// <summary>Coordina inventario y stash; solo las cantidades del stash persisten.</summary>
public sealed class StashService(SqliteGameStore store, ItemCatalog catalog)
{
    private readonly SqliteGameStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly ItemCatalog _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    private readonly ItemFactory _factory = new(catalog);

    /// <summary>Devuelve entradas agrupadas por definición, obtenidas de SQLite.</summary>
    public IReadOnlyList<StashEntry> Entries() => _store.ReadStash(_catalog)
        .OrderBy(entry => entry.Key)
        .Select(entry => new StashEntry(_catalog.Get(entry.Key), entry.Value))
        .ToArray();

    internal IEnumerable<CybergearStashDefinition> Cybergear => _catalog.Definitions.OfType<CybergearStashDefinition>();

    /// <summary>Deposita un implante extraído y separa los quickhacks de ese deck en la misma transacción.</summary>
    public bool TryDepositImplant(Person player, CyberwareDefinition implant, out string error)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (player.CurrentHealth == 0 || !player.SpareCyberware.Any(item => ReferenceEquals(item, implant))
            || Cybergear.FirstOrDefault(gear => gear.Implant == implant) is not { } definition)
        {
            error = "Solo podés depositar implantes propios extraídos por el ripperdoc.";
            return false;
        }
        var hacks = player.QuickhacksOn(implant).ToArray();
        var ids = hacks.Select(hack => Cybergear.Single(gear => gear.Quickhack == hack.Action).CatalogItemId).ToArray();
        if (!TryDepositCybergear(definition.CatalogItemId, ids, out error))
            return false;
        foreach (var hack in hacks)
            player.RemoveQuickhack(hack.Action);
        player.RemoveSpareImplant(implant);
        return true;
    }

    /// <summary>Deposita un quickhack propio que no está configurado en ningún deck.</summary>
    public bool TryDepositQuickhack(Person player, CombatAction action, out string error)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (player.CurrentHealth == 0 || !player.OwnsQuickhack(action) || player.IsQuickhackAssigned(action)
            || Cybergear.FirstOrDefault(gear => gear.Quickhack == action) is not { } definition)
        {
            error = "Solo podés depositar quickhacks propios sin asignar a un deck.";
            return false;
        }
        if (!TryDepositCybergear(definition.CatalogItemId, [], out error))
            return false;
        player.RemoveQuickhack(action);
        return true;
    }

    private bool TryDepositCybergear(int id, IReadOnlyList<int> separated, out string error)
    {
        try
        {
            _store.Deposit(id, 1, separatedQuickhacks: separated);
            error = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is SqliteException or IOException or InvalidOperationException)
        {
            error = $"No se pudo confirmar el depósito: {exception.Message}";
            return false;
        }
    }

    /// <summary>Deposita una pila o arma; las balas y accesorios montados se guardan por separado.</summary>
    public bool TryDeposit(Person player, Item? item, int quantity, out string error)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (player.CurrentHealth == 0 || item is null || !player.Inventory.Contains(item))
            error = "El objeto no pertenece al personaje activo.";
        else if (quantity <= 0 || (item is Weapon && quantity != 1)
            || (item is Consumable stack && quantity > stack.Quantity))
            error = "La cantidad elegida no está disponible.";
        else if (item is not Weapon && item is not Consumable)
            error = "Este objeto no se puede guardar.";
        else
        {
            var persisted = false;
            try
            {
                var entries = _store.ReadStash(_catalog);
                var old = entries.GetValueOrDefault(item.CatalogItemId);
                if (quantity > int.MaxValue - old)
                {
                    error = "La cantidad del stash supera el máximo permitido.";
                    return false;
                }
                var weapon = item as Weapon;
                var ammoId = weapon is { WeaponDefinition.IsMelee: false } ? (int?)weapon.WeaponDefinition.AmmoCatalogItemId : null;
                var ammo = weapon?.LoadedAmmo ?? 0;
                if (ammoId is not null && ammo > int.MaxValue - entries.GetValueOrDefault(ammoId.Value))
                {
                    error = "No hay espacio para guardar las balas cargadas.";
                    return false;
                }
                _store.Deposit(item.CatalogItemId, quantity, ammoId, ammo,
                    magazine: weapon?.HasExtendedMagazine == true, sight: weapon?.HasReflexSight == true);
                persisted = true;
                if (!player.TryDiscard(item, quantity, out error))
                    throw new InvalidOperationException($"El inventario cambió después del depósito: {error}");
                if (weapon is not null)
                {
                    if (ammo > 0)
                        weapon.SpendAmmo(ammo);
                    weapon.HasExtendedMagazine = false;
                    weapon.HasReflexSight = false;
                }
                error = string.Empty;
                return true;
            }
            catch (Exception exception) when (!persisted && exception is SqliteException or IOException
                or InvalidDataException or InvalidOperationException)
            {
                error = $"No se pudo confirmar el depósito: {exception.Message}";
                return false;
            }
        }
        return false;
    }

    /// <summary>Deposita suministros sueltos, sin cambiar la cantidad transportada si SQLite rechaza el depósito.</summary>
    public bool TryDepositSupply(Person player, StashSupplyDefinition supply, int quantity, out string error)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (player.CurrentHealth == 0 || supply is null || !_catalog.Definitions.Contains(supply)
            || quantity <= 0 || quantity > supply.SpareCount(player))
        {
            error = "No tenés esa cantidad disponible para depositar.";
            return false;
        }
        try
        {
            _store.Deposit(supply.CatalogItemId, quantity);
        }
        catch (Exception exception) when (exception is SqliteException or IOException or InvalidOperationException)
        {
            error = $"No se pudo confirmar el depósito: {exception.Message}";
            return false;
        }
        supply.AdjustSpare(player, -quantity);
        error = string.Empty;
        return true;
    }

    /// <summary>Retira una cantidad; armas nuevas están vacías y consumibles se apilan.</summary>
    public bool TryWithdraw(Person player, int catalogItemId, int quantity, out string error)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (player.CurrentHealth == 0)
        {
            error = "Un personaje muerto no puede retirar objetos.";
            return false;
        }
        if (quantity <= 0)
        {
            error = "La cantidad debe ser positiva.";
            return false;
        }
        var persisted = false;
        try
        {
            var entries = _store.ReadStash(_catalog);
            if (!entries.TryGetValue(catalogItemId, out var available) || quantity > available)
            {
                error = "El stash no contiene esa cantidad.";
                return false;
            }
            var definition = _catalog.Get(catalogItemId);
            if (definition is CybergearStashDefinition gear)
            {
                if (quantity != 1 || (gear.Quickhack is { } hack && player.OwnsQuickhack(hack)))
                {
                    error = quantity != 1 ? "Retirá un implante o quickhack por vez." : "Ya tenés ese quickhack.";
                    return false;
                }
                _store.Withdraw(catalogItemId, 1);
                persisted = true;
                if (gear.Implant is { } implant)
                    player.ReceiveCyberware(implant with { });
                else
                    player.ReceiveQuickhack(gear.Quickhack!.Value);
                error = string.Empty;
                return true;
            }
            if (definition is StashSupplyDefinition supply)
            {
                if (quantity > int.MaxValue - supply.SpareCount(player))
                {
                    error = $"No podés llevar más unidades de {supply.Name}.";
                    return false;
                }
                _store.Withdraw(catalogItemId, quantity);
                persisted = true;
                supply.AdjustSpare(player, quantity);
                error = string.Empty;
                return true;
            }
            var items = definition switch
            {
                WeaponDefinition weapon => Enumerable.Range(0, quantity)
                    .Select(_ => (Item)_factory.CreateWeapon(weapon.Code)).ToArray(),
                ConsumableDefinition => [(Item)_factory.CreateConsumable(catalogItemId, quantity)],
                _ => throw new InvalidDataException("Tipo de objeto no admitido.")
            };
            if (items[0] is Consumable && player.Inventory.Items.OfType<Consumable>()
                .FirstOrDefault(stack => stack.CatalogItemId == catalogItemId) is { } existing
                && quantity > int.MaxValue - existing.Quantity)
            {
                error = "La pila del inventario supera el máximo permitido.";
                return false;
            }

            _store.Withdraw(catalogItemId, quantity);
            persisted = true;
            foreach (var item in items)
            {
                if (item is Consumable stack)
                {
                    if (!player.Inventory.TryAddToStack(stack, out error))
                        throw new InvalidOperationException($"El inventario cambió después de la retirada: {error}");
                }
                else
                    player.Inventory.Add(item);
            }
            error = string.Empty;
            return true;
        }
        catch (Exception exception) when (!persisted && exception is SqliteException or IOException
            or InvalidDataException or InvalidOperationException)
        {
            error = $"No se pudo confirmar la retirada: {exception.Message}";
            return false;
        }
    }
}

/// <summary>Una definición y su cantidad disponible en el stash.</summary>
public sealed record StashEntry(ItemDefinition Definition, int Quantity);
