namespace MySharpVenture;

public class GameState
{
    public Person? Player { get; private set; }
    private ItemCatalog? _catalog;
    public Expedition? ActiveExpedition { get; private set; }
    /// <summary>Existencia de Ex-Disk en preparación; se repone al volver con éxito.</summary>
    public bool ExDiskInStock => HasImplantStock(CyberwareContent.ExDisk);
    private readonly HashSet<CyberwareDefinition> _soldImplants = new();
    /// <summary>Ofertas de la tienda común; independientes del comerciante de aventura.</summary>
    public IReadOnlyList<MerchantOffer> ShopOffers { get; private set; } = Array.Empty<MerchantOffer>();
    public IReadOnlyList<CraftingRecipe> CraftingRecipes { get; private set; } = Array.Empty<CraftingRecipe>();

    public void CreateCharacter(OriginPath origin, ItemCatalog catalog)
    {
        if (Player is not null)
            throw new InvalidOperationException("Ya existe un personaje activo.");

        var factory = new ItemFactory(catalog);
        var weapon = factory.CreateWeapon(StartingKit.WeaponCode(origin), loaded: true);
        var reserve = factory.CreateConsumable(weapon.WeaponDefinition.AmmoCatalogItemId,
            weapon.MagazineCapacity);
        var healing = factory.CreateConsumable(catalog.Get("curacion").CatalogItemId, StartingKit.HealingCount);

        // Se prepara el kit completo antes de publicar el personaje en la sesión.
        var player = new Person(origin.ToString(), DevelopmentContent.StartingHealth, origin);
        player.Inventory.Add(weapon);
        player.Inventory.Add(reserve);
        player.Inventory.Add(healing);
        if (!player.Equipment.TryEquip(weapon, out var error))
            throw new InvalidOperationException(error);

        ShopOffers = Array.AsReadOnly(catalog.Consumables.OrderBy(item => item.CatalogItemId)
            .Select(item => item.Type == ConsumableType.Healing
                ? new MerchantOffer(item, 12, 4) : new MerchantOffer(item, 2, 24)).ToArray());
        var ammoAndHealing = catalog.Consumables.OrderBy(item => item.CatalogItemId)
            .Select(item => item.Type == ConsumableType.Healing
                ? new CraftingRecipe(item, 1, 2) : new CraftingRecipe(item, 6, 1));
        var weapons = catalog.Definitions.OfType<WeaponDefinition>().Where(weapon => weapon.Tier == 1)
            .OrderBy(weapon => weapon.CatalogItemId)
            .Select(weapon => new CraftingRecipe(weapon, 1, WeaponUpgradeContent.CraftCost(weapon.Variant)));
        CraftingRecipes = Array.AsReadOnly(ammoAndHealing.Where(recipe => recipe.Output != WeaponUpgradeContent.RifleAmmo)
            .Concat(weapons.Where(recipe => WeaponUpgradeContent.StartingCodes.Contains(recipe.Output.Code)))
            .Concat(CybergearStashContent.All.Where(gear => gear.Quickhack is not null)
                .Select(gear => new CraftingRecipe(gear, 1, QuickhackContent.Find(gear.Quickhack!.Value)!.ComponentCost)))
            .Concat(ammoAndHealing.Where(recipe => recipe.Output == WeaponUpgradeContent.RifleAmmo))
            .Concat(weapons.Where(recipe => !WeaponUpgradeContent.StartingCodes.Contains(recipe.Output.Code)))
            .ToArray());
        _catalog = catalog;
        Player = player;
        _soldImplants.Clear();
    }

    /// <summary>Consulta el siguiente tier y su coste en componentes sin modificar el arma.</summary>
    public (WeaponDefinition Next, int Cost)? PreviewWeaponUpgrade(Weapon weapon)
    {
        if (Player is not { CurrentHealth: > 0 } player || ActiveExpedition is not null
            || !player.Inventory.Contains(weapon) || _catalog?.NextWeaponTier(weapon.WeaponDefinition) is not { } next)
            return null;
        return (next, UpgradeCost(weapon.Definition.Tier!.Value));
    }

    private static int UpgradeCost(int tier) => 2 + 2 * tier;

    /// <summary>Consulta la mejora de un implante propio y el posible bloqueo por capacidad.</summary>
    public (CyberwareDefinition Next, int Cost, string? BlockedReason)? PreviewImplantUpgrade(CyberwareDefinition implant)
    {
        if (Player is not { CurrentHealth: > 0 } player || ActiveExpedition is not null || implant is null)
            return null;
        var installed = ReferenceEquals(player.InstalledCyberware.GetValueOrDefault(implant.Zone), implant);
        if ((!installed && !player.SpareCyberware.Any(candidate => ReferenceEquals(candidate, implant)))
            || CyberwareContent.NextTier(implant) is not { } next)
            return null;
        var exceedsCapacity = installed
            && player.UsedCyberwareCapacity - implant.CapacityCost + next.CapacityCost > player.CyberwareCapacity;
        return (next, UpgradeCost(implant.Tier), exceedsCapacity ? "Capacidad de cyberware insuficiente." : null);
    }

    /// <summary>Mejora un implante propio en preparación, sin rellenar RAM ni cambiar su ubicación.</summary>
    public bool TryUpgradeImplant(CyberwareDefinition implant, out string error)
    {
        if (PreviewImplantUpgrade(implant) is not { } upgrade)
        {
            error = "Ese implante no puede mejorarse en este momento.";
            return false;
        }
        var player = Player!;
        if (upgrade.BlockedReason is { } reason)
            error = reason;
        else if (player.Components < upgrade.Cost)
            error = "No tenés suficientes componentes.";
        else
        {
            player.UpgradeOwnedImplant(implant, upgrade.Next);
            player.Components -= upgrade.Cost;
            error = string.Empty;
            return true;
        }
        return false;
    }

    /// <summary>Mejora un arma propia sin reemplazar la instancia ni cambiar el cargador.</summary>
    public bool TryUpgradeWeapon(Weapon weapon, out string error)
    {
        if (PreviewWeaponUpgrade(weapon) is not { } upgrade)
        {
            error = "Esa arma no puede mejorarse en este momento.";
            return false;
        }
        var player = Player!;
        if (player.Components < upgrade.Cost)
        {
            error = "No tenés suficientes componentes.";
            return false;
        }
        weapon.UpgradeTo(upgrade.Next);
        player.Components -= upgrade.Cost;
        error = string.Empty;
        return true;
    }

    public void RemoveDeadCharacter(Person character)
    {
        if (!ReferenceEquals(Player, character) || character.CurrentHealth != 0)
            throw new InvalidOperationException("Solo se puede retirar al personaje activo tras morir.");
        Player = null;
        ActiveExpedition = null;
        ShopOffers = Array.Empty<MerchantOffer>();
        CraftingRecipes = Array.Empty<CraftingRecipe>();
    }

    public void StartExpedition(Expedition expedition)
    {
        ArgumentNullException.ThrowIfNull(expedition);
        if (Player is null || Player.CurrentHealth == 0 || ActiveExpedition is not null
            || !ExpeditionContent.Zones.Contains(expedition.Zone)
            || !ReferenceEquals(expedition.Player, Player)
            || expedition.Status != ExpeditionStatus.Active)
            throw new InvalidOperationException("No se puede comenzar esta aventura.");
        if (Player.IsOverweight)
            throw new InvalidOperationException("No podés iniciar otra aventura con sobrepeso.");
        ActiveExpedition = expedition;
    }

    public void CompleteExpedition(Expedition expedition)
    {
        if (!ReferenceEquals(ActiveExpedition, expedition)
            || expedition.Status != ExpeditionStatus.Success || Player is null)
            throw new InvalidOperationException("La aventura todavía no terminó con éxito.");
        Player.Heal(Player.MaxHealth - Player.CurrentHealth);
        Player.RestoreRam();
        Player.RestoreStamina();
        Player.RestoreShield();
        ActiveExpedition = null;
        _soldImplants.Clear();
        foreach (var offer in ShopOffers)
            offer.Restock();
    }

    /// <summary>Abandona la aventura activa y pierde armas, munición cargada y dinero sin recuperar recursos.</summary>
    public void WithdrawExpedition(Expedition expedition)
    {
        ArgumentNullException.ThrowIfNull(expedition);
        if (!ReferenceEquals(ActiveExpedition, expedition))
            throw new InvalidOperationException("La aventura no está activa en esta sesión.");
        expedition.Withdraw();
        var player = expedition.Player;
        foreach (var weapon in player.Inventory.Items.OfType<Weapon>().ToArray())
            if (!player.TryDiscard(weapon, 1, out var error))
                throw new InvalidOperationException(error);
        player.SpendMoney(player.Money);
        ActiveExpedition = null;
    }

    /// <summary>Compra un quickhack de la tienda sin equiparlo ni cobrar si ya se posee.</summary>
    public bool TryBuyQuickhack(CombatAction action, out string error)
    {
        if (!TryGetPreparationCustomer(0, out var player, out error))
            return false;
        if (QuickhackContent.Find(action) is not { Price: > 0 } hack)
            error = "Ese quickhack no está a la venta.";
        else if (player!.OwnsQuickhack(action))
            error = $"Ya tenés {hack.Name}.";
        else if (player.Money < hack.Price)
            error = "No tenés suficientes eurodólares.";
        else
        {
            player.ReceiveQuickhack(action);
            player.SpendMoney(hack.Price);
            return true;
        }
        return false;
    }

    /// <summary>Devuelve el valor de reventa de un quickhack del catálogo del jugador.</summary>
    /// <param name="hack">Quickhack que se va a valorar.</param>
    /// <returns>Precio de reventa en eurodólares.</returns>
    public static int QuickhackSalePrice(QuickhackDefinition hack) => hack.Price > 0 ? hack.Price / 2 : 5;

    /// <summary>Vende un quickhack propio que no esté asignado a ningún deck.</summary>
    /// <param name="action">Acción del quickhack a vender.</param>
    /// <param name="error">Motivo del rechazo, si corresponde.</param>
    /// <returns>Si se completó la venta.</returns>
    public bool TrySellQuickhack(CombatAction action, out string error)
    {
        if (!TryGetPreparationCustomer(0, out var player, out error))
            return false;
        if (QuickhackContent.Find(action) is not { } hack || !player!.OwnsQuickhack(action)
            || player.IsQuickhackAssigned(action))
        {
            error = "Solo podés vender quickhacks propios sin asignar.";
            return false;
        }
        return TryFinishSale(player, QuickhackSalePrice(hack), () => player.RemoveQuickhack(action), out error);
    }

    /// <summary>Devuelve el valor de reventa de un implante.</summary>
    /// <param name="implant">Implante que se va a valorar.</param>
    /// <returns>Precio de reventa en eurodólares.</returns>
    public static int ImplantSalePrice(CyberwareDefinition implant) => 3 * implant.CapacityCost;

    /// <summary>Vende un implante suelto propio si no tiene quickhacks asociados.</summary>
    /// <param name="implant">Instancia suelta a vender.</param>
    /// <param name="error">Motivo del rechazo, si corresponde.</param>
    /// <returns>Si se completó la venta.</returns>
    public bool TrySellImplant(CyberwareDefinition implant, out string error)
    {
        if (!TryGetPreparationCustomer(0, out var player, out error))
            return false;
        if (!player!.SpareCyberware.Any(candidate => ReferenceEquals(candidate, implant))
            || player.QuickhacksOn(implant).Any())
        {
            error = "Solo podés vender implantes propios, sueltos y sin quickhacks asignados.";
            return false;
        }
        return TryFinishSale(player, ImplantSalePrice(implant), () => player.RemoveSpareImplant(implant), out error);
    }

    private static bool TryFinishSale(Person player, int price, Action remove, out string error)
    {
        if (price > int.MaxValue - player.Money)
        {
            error = "No podés llevar más eurodólares.";
            return false;
        }
        remove();
        player.EarnMoney(price);
        error = string.Empty;
        return true;
    }

    /// <summary>Compra unidades de una oferta de la tienda común durante la preparación.</summary>
    public bool TryBuyFromShop(MerchantOffer offer, int quantity, out string error)
    {
        if (!TryGetPreparationCustomer(0, out var player, out error))
            return false;
        if (!ShopOffers.Contains(offer))
        {
            error = "Esa oferta no pertenece a la tienda.";
            return false;
        }
        return offer.TryPurchase(player!, quantity, out error);
    }

    /// <summary>Precio unitario provisional que paga la tienda por consumibles.</summary>
    public static int ShopSalePrice(ConsumableType type) => type == ConsumableType.Healing ? 5 : 1;

    /// <summary>Vende una cantidad de una pila propia, sin afectar el stock a la venta.</summary>
    public bool TrySellToShop(Consumable stack, int quantity, out string error)
    {
        if (!TryGetPreparationCustomer(0, out var player, out error))
            return false;
        if (stack is null || !player!.Inventory.Contains(stack)
            || !ShopOffers.Any(offer => offer.Definition == stack.ConsumableDefinition))
        {
            error = "La tienda solo compra munición y curaciones de tu inventario.";
            return false;
        }
        var proceeds = (long)quantity * ShopSalePrice(stack.Type);
        if (proceeds > int.MaxValue - player.Money)
        {
            error = "La venta supera el saldo máximo.";
            return false;
        }
        if (!player.TryDiscard(stack, quantity, out error))
            return false;
        player.EarnMoney((int)proceeds);
        return true;
    }

    /// <summary>Compra un Ex-Disk suelto, sin instalarlo.</summary>
    public bool TryBuyExDisk(out string error) => TryBuyImplant(CyberwareContent.ExDisk, out error);

    public bool HasImplantStock(CyberwareDefinition implant) => !_soldImplants.Contains(implant);

    public bool TryBuyImplant(CyberwareDefinition implant, out string error)
    {
        var price = CyberwareContent.ShopOffers.FirstOrDefault(offer => offer.Implant == implant).Price;
        if (price == 0)
        {
            error = "Ese implante no está a la venta.";
            return false;
        }
        if (!TryGetPreparationCustomer(price, out var player, out error))
            return false;
        if (!HasImplantStock(implant))
        {
            error = $"No queda {implant.Name} en stock.";
            return false;
        }

        player!.ReceiveCyberware(implant with { });
        player.SpendMoney(price);
        _soldImplants.Add(implant);
        return true;
    }

    /// <summary>Instala un implante suelto propio desde preparación.</summary>
    public bool TryInstallImplant(CyberwareDefinition implant, out string error)
    {
        if (!TryGetPreparationCustomer(CyberwareContent.ProcedureFee, out var player, out error))
            return false;
        var owned = player!.SpareCyberware.FirstOrDefault(candidate => ReferenceEquals(candidate, implant));
        if (owned is null)
        {
            error = "No tenés ese implante suelto.";
            return false;
        }
        if (player.CyberwareFitError(owned) is { } reason)
        {
            error = reason;
            return false;
        }

        player.InstallOwnedCyberware(owned);
        player.SpendMoney(CyberwareContent.ProcedureFee);
        return true;
    }

    /// <summary>Extrae un implante instalado y lo devuelve al inventario de cyberware.</summary>
    public bool TryExtractImplant(CyberwareZone zone, out string error)
    {
        if (!TryGetPreparationCustomer(CyberwareContent.ProcedureFee, out var player, out error))
            return false;
        if (!player!.InstalledCyberware.ContainsKey(zone))
        {
            error = "No hay un implante para extraer en esa zona.";
            return false;
        }

        player.ExtractCyberware(zone);
        player.SpendMoney(CyberwareContent.ProcedureFee);
        return true;
    }

    /// <summary>Rendimiento unitario; cero identifica un objeto que no se puede desarmar.</summary>
    public static int DisassemblyYield(Item item) => item switch
    {
        Weapon { Definition.Tier: { } tier } => Math.Max(4, 2 + tier),
        Consumable { Type: ConsumableType.Healing } => 1,
        _ => 0
    };

    /// <summary>Desarma una unidad de un accesorio suelto y entrega un componente.</summary>
    /// <param name="accessory">Accesorio de catálogo.</param>
    /// <param name="error">Motivo del rechazo, si corresponde.</param>
    /// <returns>Si el desarme se completó.</returns>
    public bool TryDisassembleAccessory(AccessoryDefinition accessory, out string error)
    {
        if (!TryGetPreparationCustomer(0, out var player, out error))
            return false;
        if ((accessory != AccessoryDefinition.Magazine && accessory != AccessoryDefinition.Sight)
            || accessory.SpareCount(player!) == 0 || player!.Components == int.MaxValue)
        {
            error = "No tenés ese accesorio suelto o no podés llevar más componentes.";
            return false;
        }
        accessory.AdjustSpare(player, -1);
        player.Components++;
        return true;
    }

    /// <summary>Desarma una cantidad de un objeto propio, solo en preparación.</summary>
    public bool TryDisassemble(Item item, int quantity, out string error)
    {
        if (!TryGetPreparationCustomer(0, out var player, out error))
            return false;
        var yield = DisassemblyYield(item);
        if (yield == 0 || quantity <= 0)
        {
            error = "Elegí un objeto desmontable y una cantidad positiva.";
            return false;
        }
        var proceeds = (long)yield * quantity;
        if (proceeds > int.MaxValue - player!.Components)
        {
            error = "No podés llevar más componentes.";
            return false;
        }
        if (!player.TryDiscard(item, quantity, out error))
            return false;
        player.Components += (int)proceeds;
        return true;
    }

    /// <summary>Fabrica lotes de una receta disponible y cobra solo si se añade el resultado.</summary>
    public bool TryCraft(CraftingRecipe recipe, int batches, out string error)
    {
        if (!TryGetPreparationCustomer(0, out var player, out error))
            return false;
        if (batches <= 0 || !CraftingRecipes.Contains(recipe)
            || (recipe.Output is not ConsumableDefinition && batches != 1))
        {
            error = "La receta o la cantidad no es válida.";
            return false;
        }
        if (recipe.Output is CybergearStashDefinition { Quickhack: { } action } && player!.OwnsQuickhack(action))
        {
            error = $"Ya tenés {recipe.Output.Name}.";
            return false;
        }
        var cost = (long)recipe.ComponentCost * batches;
        if (cost > player!.Components)
        {
            error = "No tenés suficientes componentes.";
            return false;
        }
        var units = (long)recipe.Units * batches;
        if (units > int.MaxValue)
        {
            error = "La cantidad supera el máximo permitido.";
            return false;
        }
        if (recipe.Output is WeaponDefinition weapon)
            player.Inventory.Add(new Weapon(weapon, loadedAmmo: 0));
        else if (recipe.Output is CybergearStashDefinition { Quickhack: { } quickhack })
            player.ReceiveQuickhack(quickhack);
        else if (!player.Inventory.TryAddToStack(new Consumable((ConsumableDefinition)recipe.Output, (int)units), out error))
            return false;
        player.Components -= (int)cost;
        return true;
    }

    /// <summary>Fabrica un cargador ampliado en preparación.</summary>
    public bool TryCraftExtendedMagazine(out string error)
        => TryCraftAccessory(reflexSight: false, out error);

    /// <summary>Fabrica una mira réflex en preparación.</summary>
    public bool TryCraftReflexSight(out string error)
        => TryCraftAccessory(reflexSight: true, out error);

    private bool TryCraftAccessory(bool reflexSight, out string error)
    {
        if (!TryGetPreparationCustomer(0, out var player, out error))
            return false;
        var cost = reflexSight ? ReflexSightContent.ComponentCost : MagazineContent.ComponentCost;
        var spare = reflexSight ? player!.SpareReflexSights : player!.SpareExtendedMagazines;
        if (player.Components < cost)
            error = "No tenés suficientes componentes.";
        else if (spare == int.MaxValue)
            error = "No podés llevar más accesorios de ese tipo.";
        else
        {
            if (reflexSight)
                player.SpareReflexSights++;
            else
                player.SpareExtendedMagazines++;
            player.Components -= cost;
            return true;
        }
        return false;
    }

    /// <summary>Monta o quita una mira de un arma compatible sin modificar su cargador.</summary>
    public bool TryChangeReflexSight(Weapon weapon, bool mount, out string error)
    {
        error = string.Empty;
        if (Player is not { CurrentHealth: > 0 } player || weapon is null || !player.Inventory.Contains(weapon))
            error = "El arma no pertenece al personaje activo.";
        else if (!weapon.WeaponDefinition.SupportsReflexSight)
            error = "Esa arma no admite la mira réflex.";
        else if (mount == weapon.HasReflexSight)
            error = mount ? "La pistola ya tiene una mira réflex." : "La pistola no tiene una mira réflex.";
        else if (mount && player.SpareReflexSights == 0)
            error = "No tenés miras réflex sueltas.";
        else if (!mount && player.SpareReflexSights == int.MaxValue)
            error = "No podés llevar más miras réflex.";
        else
        {
            weapon.HasReflexSight = mount;
            player.SpareReflexSights += mount ? -1 : 1;
            return true;
        }
        return false;
    }

    /// <summary>Monta o quita el cargador de un arma propia; devuelve el sobrante a la reserva.</summary>
    public bool TryChangeExtendedMagazine(Weapon weapon, bool mount, out string error)
    {
        error = string.Empty;
        if (Player is not { CurrentHealth: > 0 } player || weapon is null || !player.Inventory.Contains(weapon))
            error = "El arma no pertenece al personaje activo.";
        else if (!weapon.WeaponDefinition.SupportsExtendedMagazine
            || weapon.WeaponDefinition.MagazineCapacity > int.MaxValue - MagazineContent.ExtraCapacity)
            error = "Ese arma no admite el cargador ampliado.";
        else if (mount == weapon.HasExtendedMagazine)
            error = mount ? "La pistola ya tiene un cargador ampliado." : "La pistola no tiene un cargador ampliado.";
        else if (mount && player.SpareExtendedMagazines == 0)
            error = "No tenés cargadores ampliados sueltos.";
        else if (!mount && player.SpareExtendedMagazines == int.MaxValue)
            error = "No podés llevar más cargadores ampliados.";
        else
        {
            var overflow = mount ? 0 : Math.Max(0, weapon.LoadedAmmo - weapon.WeaponDefinition.MagazineCapacity);
            if (overflow > 0)
            {
                var ammo = (ConsumableDefinition)_catalog!.Get(weapon.WeaponDefinition.AmmoCatalogItemId);
                if (!player.Inventory.TryAddToStack(new Consumable(ammo, overflow), out error))
                    return false;
                weapon.SpendAmmo(overflow);
            }
            weapon.HasExtendedMagazine = mount;
            player.SpareExtendedMagazines += mount ? -1 : 1;
            return true;
        }
        return false;
    }

    private bool TryGetPreparationCustomer(int cost, out Person? player, out string error)
    {
        player = Player;
        if (player is null || player.CurrentHealth == 0 || ActiveExpedition is not null)
            error = "Esta operación solo está disponible para personajes vivos en preparación.";
        else if (player.Money < cost)
            error = "No tenés suficientes eurodólares.";
        else
        {
            error = string.Empty;
            return true;
        }
        return false;
    }
}
