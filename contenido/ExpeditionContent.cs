namespace MySharpVenture;

/// <summary>Zonas y balance provisional de encuentros y recompensas.</summary>
public static class ExpeditionContent
{
    public static Zone Watson { get; } = new("watson", "Watson", "Maelstrom", 1, 5);
    public static Zone Japantown { get; } = new("japantown", "Japantown", "Tyger Claws", 6, 12);
    public static Zone Dogtown { get; } = new("dogtown", "Dogtown", "Barghest", 13, 20);
    public static IReadOnlyList<Zone> Zones { get; } = Array.AsReadOnly(new[] { Watson, Japantown, Dogtown });

    public static Expedition Create(Zone zone, Random random, ItemCatalog catalog, Person player)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(player);
        if (!Zones.Contains(zone))
            throw new ArgumentException("Zona desconocida.", nameof(zone));
        var weaponCode = player.Origin switch
        {
            OriginPath.Corpo => "pistola-t1",
            OriginPath.Street => "subfusil-t1",
            OriginPath.Nomad => "escopeta-corredera-t1",
            _ => throw new InvalidOperationException("El personaje necesita un origen válido.")
        };
        var startingWeapon = (WeaponDefinition)catalog.Get(weaponCode);
        var ammo = catalog.Get(startingWeapon.AmmoCatalogItemId);
        var healing = catalog.Get("curacion");
        var pistol = (WeaponDefinition)catalog.Get("pistola-t1");
        var shooter = zone == Japantown ? new EnemyLoadout(pistol,
            (ConsumableDefinition)catalog.Get(pistol.AmmoCatalogItemId),
            (int)Math.Min(pistol.MagazineCapacity, 2L * pistol.AmmoPerAttack),
            pistol.MagazineCapacity) : null;
        var kinds = new List<EventKind> { EventKind.Combat, EventKind.Combat, EventKind.Combat };
        if (random.Next(3) == 0)
            kinds.Add(EventKind.Merchant);
        while (kinds.Count < 5)
            kinds.Add(random.Next(2) == 0 ? EventKind.Discovery : EventKind.Combat);
        for (var i = kinds.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (kinds[i], kinds[j]) = (kinds[j], kinds[i]);
        }

        var fights = 0;
        var events = kinds.Select(kind =>
        {
            if (kind == EventKind.Merchant)
            {
                var weapons = catalog.Definitions.OfType<WeaponDefinition>()
                    .Where(weapon => !weapon.IsMelee
                        && (zone == Dogtown ? weapon.Tier >= 4 : weapon.Tier == (zone == Japantown ? 3 : 2)))
                    .OrderBy(weapon => weapon.CatalogItemId).ToArray();
                var hacks = catalog.Definitions.OfType<CybergearStashDefinition>()
                    .Where(gear => gear.Quickhack is { } action && QuickhackContent.Find(action) is { Price: > 0 })
                    .OrderBy(gear => gear.CatalogItemId).ToArray();
                var specialHack = hacks.Length > 0 ? hacks[random.Next(hacks.Length)] : null;
                var offers = new List<MerchantOffer>
                {
                    new(healing, 10, 2),
                    new(ammo, 1, startingWeapon.MagazineCapacity)
                };
                if (weapons.Length > 0)
                {
                    var weapon = weapons[random.Next(weapons.Length)];
                    offers.Add(new(weapon, 15 * weapon.Tier!.Value - 5, 1));
                }
                if (specialHack is not null)
                    offers.Add(new(specialHack, QuickhackContent.Find(specialHack.Quickhack!.Value)!.Price * 4 / 5, 1));
                return new EventOccurrence(kind, [], merchant: offers);
            }
            var loot = kind == EventKind.Combat
                ? new[] { new LootEntry(ammo, Math.Max(1, startingWeapon.MagazineCapacity / 2)),
                    new LootEntry(healing, 1) }
                : new[] { new LootEntry(ammo, startingWeapon.MagazineCapacity),
                    new LootEntry(pistol, 1) };
            EnemyProfile? enemy = null;
            if (kind == EventKind.Combat)
            {
                var level = random.Next(zone.MinLevel, zone.MaxLevel + 1);
                fights++;
                enemy = zone.Code switch
                {
                    "japantown" => new("Tirador", zone.Affiliation, 45 + 2 * level,
                        new AttackParameters(7 + level / 3, 0.80m), level, shooter),
                    "dogtown" => new(fights switch { 2 => "Netrunner", 3 => "Centinela", _ => "Patrullero" },
                        zone.Affiliation, 50 + 2 * level, new AttackParameters(9 + level / 3, 0.85m),
                        level, Netrunner: fights == 2, IsBot: fights == 3, ControlQuickhacks: fights == 2),
                    _ => DevelopmentContent.MaelstromTest with { Level = level }
                };
            }
            var implantCode = enemy is { IsBot: true } ? "tetratronic-rippler-t1"
                : enemy is { Netrunner: true } ? "kiroshi-t1" : null;
            if (implantCode is not null && catalog.Definitions.OfType<CybergearStashDefinition>()
                .FirstOrDefault(gear => gear.Code == implantCode) is { } implant)
                return new EventOccurrence(kind, loot.Append(new LootEntry(implant, 1)), enemy);
            return new EventOccurrence(kind, loot, enemy);
        }).ToArray();
        return new Expedition(zone, player, events);
    }
}
