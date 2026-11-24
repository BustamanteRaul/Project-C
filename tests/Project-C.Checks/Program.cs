using MySharpVenture;
using Microsoft.Data.Sqlite;

// Comprobaciones de dominio sin consola interactiva ni paquetes de pruebas.
var catalog = DevelopmentContent.CreateCatalog();
var factory = new ItemFactory(catalog);
if (args is ["--equipo-interactivo"])
    return EquipmentScenario.Run(catalog);
if (args is ["--descarte-interactivo"])
    return EquipmentScenario.Run(catalog, discard: true);
if (args is ["--combate-interactivo"])
    return CombatScenario.Run(catalog);
if (args is ["--combate-multiarma"])
    return CombatScenario.Run(catalog, multipleWeapons: true);
if (args is ["--combate-prueba"])
    return CombatScenario.Run(catalog, multipleWeapons: true, guaranteedHits: true);
if (args is ["--combate-derrota-prueba"])
    return CombatScenario.Run(catalog, guaranteedHits: true);
if (args is ["--combate-pulso-prueba"])
    return CombatScenario.Run(catalog, guaranteedHits: true, quickhack: CombatAction.Overheat);
if (args is ["--combate-sinaptico-prueba"])
    return CombatScenario.Run(catalog, guaranteedHits: true, quickhack: CombatAction.SynapseBurnout);
if (args is ["--combate-bloqueo-prueba"])
    return CombatScenario.Run(catalog, guaranteedHits: true, quickhack: CombatAction.WeaponGlitch);
if (args is ["--combate-cyberware-prueba"])
    return CombatScenario.Run(catalog, guaranteedHits: true, quickhack: CombatAction.CyberwareMalfunction);
if (args is ["--combate-sandevistan-prueba"])
    return CombatScenario.Run(catalog, guaranteedHits: true, sandevistan: true);
if (args is ["--combate-ejecucion-prueba"])
    return CombatScenario.Run(catalog, guaranteedHits: true, quickhack: CombatAction.Suicide);
if (args is ["--combate-bot-prueba"])
    return CombatScenario.Run(catalog, guaranteedHits: true, quickhack: CombatAction.Suicide, bot: true);
if (args is ["--combate-escudo-prueba"])
    return CombatScenario.Run(catalog, guaranteedHits: true, shield: true);
if (args is ["--combate-critico-prueba"])
    return CombatScenario.Run(catalog, guaranteedHits: true, critical: true);
if (args is ["--combate-brazos-prueba"])
    return CombatScenario.Run(catalog, guaranteedHits: true, arms: CyberwareContent.GorillaArms);
if (args is ["--combate-lanzador-prueba"])
    return CombatScenario.Run(catalog, guaranteedHits: true, arms: CyberwareContent.ProjectileLaunchSystem);
if (args is ["--combate-control-prueba"])
    return CombatScenario.Run(catalog, guaranteedHits: true, control: true);
if (args is ["--combate-espada-prueba"])
    return CombatScenario.Run(catalog, guaranteedHits: true, melee: true);
if (args is ["--sesion-prueba"])
    return SessionScenario.Run(catalog);
if (args is ["--aventura-prueba"])
    return SessionScenario.Run(catalog, adventure: true);
if (args is ["--aventura-muerte-prueba"])
    return SessionScenario.Run(catalog, adventure: true, death: true);
if (args is ["--comerciante-prueba"])
    return SessionScenario.Run(catalog, adventure: true, merchant: true);
if (args.Length != 0)
{
    Console.Error.WriteLine("Opciones: --equipo-interactivo, --descarte-interactivo, --combate-interactivo o --combate-multiarma.");
    return 2;
}

var cases = new (string Name, Action Check)[]
{
    ("PM01 parcial: los tres kits y sus referencias", CheckKits),
    ("No reemplazar un personaje activo", CheckExistingCharacter),
    ("Nueva sesión sin personaje", () => Require(new GameState().Player is null, "Personaje recuperado.")),
    ("Identidades y cargadores independientes", CheckInstances),
    ("Pertenencia única y equipo sin copias", CheckOwnership),
    ("Límite de tres armas sin pérdida", CheckEquipmentLimit),
    ("Origen/catálogo inválidos no dejan un personaje parcial", CheckRejectedCreation),
    ("Cantidades y munición de catálogo válidas", CheckInvalidContent),
    ("Dependencias públicas nulas se rechazan al entrar", CheckNullDependencies),
    ("Desequipar y reequipar conserva instancias y recursos", CheckEquipmentCycles),
    ("Desequipados rechazados conservan ambos personajes", CheckRejectedUnequip),
    ("Liberar una ranura permite equipar la cuarta arma", CheckSlotReuse),
    ("Armas iguales se desequipan por instancia", CheckSameModelUnequip),
    ("Descartar arma equipada pierde cargador y conserva reserva", CheckDiscardEquipped),
    ("Descartar instancia no equipada conserva otras armas", CheckDiscardLoose),
    ("Descartar pilas parcialmente y por completo", CheckDiscardStacks),
    ("Descartes rechazados conservan todo el estado", CheckRejectedDiscard),
    ("Descartar última arma deja inventario utilizable", CheckDiscardAll),
    ("Combate: acierto y respuesta enemiga", CheckCombatHit),
    ("Combate: fallo paga costes y permite respuesta", CheckCombatMiss),
    ("Combate: sin munición rechaza sin respuesta", CheckCombatNoAmmo),
    ("Combate: arma ajena o no equipada no ejecuta", CheckCombatInvalidWeapon),
    ("Combate: selección gratuita de armas concretas", CheckCombatSelection),
    ("Combate: ataque básico sin arma y esperar", CheckCombatFallback),
    ("Combate: victoria y derrota impiden acciones posteriores", CheckCombatEnd),
    ("Enemigos de perfil comparten definición, no salud", CheckEnemyIndependence),
    ("Daño limitado y estadísticas inválidas rechazadas", CheckCombatValidation),
    ("Recarga parcial con reserva escasa no desperdicia balas", CheckPartialReload),
    ("Recargar varias pilas modifica solo el arma elegida", CheckReloadAcrossStacks),
    ("Recarga rechazada no gasta turno ni reserva", CheckRejectedReload),
    ("Recarga fuera de combate conserva reservas y rechazos", CheckPreparationReload),
    ("Los acentos de origen son distintos sin alterar el tier", CheckOriginColors),
    ("Progresión: catálogo, kits, experiencia y límite 20", CheckProgression),
    ("Progresión: requisitos, compra única y efectos reales", CheckSkills),
    ("Progresión: cada evento concede experiencia una sola vez", CheckExpeditionExperience),
    ("Cyberware: zona, capacidad, instalación y RAM por turno", CheckCyberware),
    ("Cortocircuito: rechazo gratis, ejecución y muerte", CheckShortCircuit),
    ("Sobrecalentamiento: compra, configuración y pulsos al inicio del turno enemigo", CheckOverheat),
    ("Quemado sináptico: daño según RAM previa, límite y reinicio", CheckSynapseBurnout),
    ("Fallo de arma: disparo, recarga, pulsos y rechazo sin objetivo", CheckWeaponGlitch),
    ("Self-ICE y Fallo de cyberware: bloqueo, reducción y reactivación", CheckCyberwareDefense),
    ("Sandevistan: instalación exclusiva, acción extra, enfriamiento y desactivación", CheckSandevistan),
    ("Suicidio: coste, cuatro ranuras, Self-ICE y ejecución sin respuesta", CheckSuicide),
    ("Bot: Cortocircuito reforzado y ejecución rechazada sin coste", CheckBot),
    ("Retirada: pérdidas selectivas, sin recompensas ni reposición", CheckWithdrawal),
    ("Curación en combate: costes, límite y rechazo", CheckCombatHealing),
    ("Curación fuera de combate y muerte no resucitable", CheckPreparationHealing),
    ("Previsiones no tiran dados ni alteran recursos", CheckPreview),
    ("Eliminar personaje muerto permite recrear sin conservar carga", CheckDeathRecreation),
    ("Zonas: rangos fijos, predominio de combates y sorteo de comerciante", CheckZoneGeneration),
    ("Comerciante: fondos, cantidades, stock y rechazo sin pérdidas", CheckMerchant),
    ("Tienda: compra, venta, reposición y aislamiento de la aventura", CheckShop),
    ("Taller: desarme, fabricación por lotes y rechazos sin pérdidas", CheckWorkshop),
    ("Taller: armas Tier 1 independientes y recuperación tras retirada", CheckWeaponCrafting),
    ("Armas de fuego: cuatro modelos, recetas, disparo, tiers y migración", CheckNewFirearms),
    ("Armas cuerpo a cuerpo: daño crítico, rechazo de accesorios y stash", CheckMeleeWeapons),
    ("Taller: quickhacks sin asignar, duplicados y transferencia al stash", CheckQuickhackCrafting),
    ("Taller: mejora hasta Tier 5 sin cambiar instancia, ranura o munición", CheckWeaponUpgrade),
    ("Taller: tiers de cyberware, capacidad y RAM conservada", CheckImplantUpgrade),
    ("Cargador ampliado: compatibilidad, recarga, excedentes y stash", CheckExtendedMagazine),
    ("Mira réflex: monta por instancia, combina precisión y protege el stash", CheckReflexSight),
    ("Aventura: avanzar exige resolución única y manual", CheckExpeditionProgress),
    ("Botín: cantidades, selección y abandono al avanzar", CheckExpeditionLoot),
    ("Botín: consumibles se apilan sin cambiar identidad", CheckLootStacking),
    ("Botín: pila agotada se reconstruye sin resucitar la anterior", CheckLootAfterDepletion),
    ("Botín: desbordamiento rechazado sin pérdidas", CheckLootStackOverflow),
    ("Botín apilado sirve para recargar y curarse", CheckLootStackUsage),
    ("Botín: dos armas iguales conservan identidad y cargadores vacíos", CheckExpeditionWeapons),
    ("Regreso cura solo al completar; morir descarta la aventura", CheckExpeditionOutcomes),
    ("SQLite: catálogo y stash sobreviven a otra sesión", CheckSqliteStartup),
    ("SQLite: actualización aditiva y reapertura del stash con arma mejorada", CheckWeaponTierMigration),
    ("SQLite: migración v2 y depósito atómico de accesorios de arma", CheckAccessoryStash),
    ("SQLite: migración v3 y transferencias parciales de componentes", CheckComponentStash),
    ("SQLite: migración v4 y separación atómica de un deck mejorado", CheckCybergearStash),
    ("Carga: peso sin duplicación y bloqueo de salida solo por sobrepeso", CheckCarryWeight),
    ("Stamina: costes, espera enemiga y recuperación al avanzar", CheckStamina),
    ("Práctica: equipo y recursos copiados sin modificar al personaje", CheckPracticeCopy),
    ("Escopetas: disparos, quickhacks, cargador vacío y recarga", CheckShotgunAmmo),
    ("Escudo: mitigación parcial, recuperación, desactivación y migración v5", CheckShield),
    ("Gorilla Arms: ataque, alternativa, desactivación y stash tras migrar v6", CheckGorillaArms),
    ("Brazos: tres variantes, exclusividad, extracción y migración v7", CheckOtherArmImplants),
    ("Netrunner: Fallo de arma, Reboot Optics y Self-ICE", CheckEnemyControl),
    ("Cyberware: catálogo por zonas, tiers, efectos y botín de Dogtown", CheckExpandedCyberware),
    ("Carga: Titanium Bones y penalización de recuperación sin afectar RAM", CheckTitaniumBones),
    ("Economía: reventa, desarme de accesorios y ciclos sin beneficio", CheckTradeBalance),
    ("Críticos: tirada tras acierto, armas, básicos y escudo", CheckCriticalHits),
    ("Stash: armas, pilas y balas se transfieren sin duplicación", CheckStashTransfers),
    ("Stash: rechazos y desbordamiento conservan ambos lados", CheckStashRejections),
    ("SQLite: base existente inválida no se reemplaza", CheckSqliteCorruption)
};

var failures = 0;
foreach (var test in cases)
{
    try
    {
        test.Check();
        Console.WriteLine($"OK: {test.Name}");
    }
    catch (Exception error)
    {
        failures++;
        Console.Error.WriteLine($"ERROR: {test.Name}: {error.Message}");
    }
}
Console.WriteLine($"{cases.Length - failures}/{cases.Length} comprobaciones correctas.");
return failures == 0 ? 0 : 1;

void CheckNullDependencies()
{
    var player = new Person("Prueba", 100);
    Throws<ArgumentNullException>(() => new ItemCatalog(null!));
    Throws<ArgumentException>(() => new ItemCatalog(new ItemDefinition[] { null! }));
    Throws<ArgumentNullException>(() => new ItemFactory(null!));
    Throws<ArgumentNullException>(() => new GameEngine(null!));
    Throws<ArgumentNullException>(() => ExpeditionContent.Create(ExpeditionContent.Watson, null!, catalog, player));
    Throws<ArgumentNullException>(() => ExpeditionContent.Create(ExpeditionContent.Watson, new Random(), null!, player));
    Throws<ArgumentNullException>(() => ExpeditionContent.Create(ExpeditionContent.Watson, new Random(), catalog, null!));
    Throws<ArgumentNullException>(() => new EventOccurrence(EventKind.Discovery, null!));
    Throws<ArgumentNullException>(() => new Expedition(null!, player, Array.Empty<EventOccurrence>()));
    Throws<ArgumentNullException>(() => new Expedition(ExpeditionContent.Watson, null!, Array.Empty<EventOccurrence>()));
    Throws<ArgumentNullException>(() => new Expedition(ExpeditionContent.Watson, player, null!));
}

void CheckKits()
{
    var expectations = new[]
    {
        (OriginPath.Corpo, WeaponVariant.Pistol, 12),
        (OriginPath.Street, WeaponVariant.SubmachineGun, 30),
        (OriginPath.Nomad, WeaponVariant.PumpShotgun, 6)
    };
    foreach (var (origin, variant, capacity) in expectations)
    {
        var state = new GameState();
        state.CreateCharacter(origin, catalog);
        var player = state.Player!;
        var weapon = player.Inventory.Items.OfType<Weapon>().Single();
        var ammo = player.Inventory.Items.OfType<Consumable>()
            .Single(item => item.Type == ConsumableType.Ammunition);
        var healing = player.Inventory.Items.OfType<Consumable>()
            .Single(item => item.Type == ConsumableType.Healing);
        Require(player.Origin == origin && player.CurrentHealth == player.MaxHealth, "Personaje inicial incorrecto.");
        Require(player.Inventory.Items.Count == 3, "Kit duplicado o incompleto.");
        Require(weapon.WeaponDefinition.Variant == variant && weapon.Definition.Tier == 1, "Arma incorrecta.");
        Require(weapon.LoadedAmmo == capacity && weapon.MagazineCapacity == capacity, "Carga inicial incorrecta.");
        Require(ammo.CatalogItemId == weapon.WeaponDefinition.AmmoCatalogItemId
            && ammo.Quantity == capacity && healing.Quantity == 2, "Reservas incorrectas.");
        Require(ReferenceEquals(player.Equipment.Weapons.Single(), weapon), "Equipo copiado.");
    }
}

void CheckExistingCharacter()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var original = state.Player;
    Throws<InvalidOperationException>(() => state.CreateCharacter(OriginPath.Nomad, catalog));
    Require(ReferenceEquals(original, state.Player) && original!.Inventory.Items.Count == 3,
        "Se modificó el personaje ante un rechazo.");
}

void CheckInstances()
{
    var loaded = factory.CreateWeapon("pistola-t1", loaded: true);
    var empty = factory.CreateWeapon("pistola-t1");
    Require(loaded.RuntimeId != empty.RuntimeId, "Identidad duplicada.");
    Require(ReferenceEquals(loaded.Definition, empty.Definition), "Definiciones no compartidas.");
    Require(loaded.LoadedAmmo == 12 && empty.LoadedAmmo == 0, "Estado de cargadores compartido.");
}

void CheckOwnership()
{
    var first = new Person("Primero", 100);
    var second = new Person("Segundo", 100);
    var weapon = factory.CreateWeapon("pistola-t1");
    first.Inventory.Add(weapon);
    Throws<InvalidOperationException>(() => first.Inventory.Add(weapon));
    Throws<InvalidOperationException>(() => second.Inventory.Add(weapon));
    Require(!second.Equipment.TryEquip(weapon, out _), "Se equipó un arma ajena.");
    Require(first.Equipment.TryEquip(weapon, out _), "No se equipó un arma propia.");
    Require(!first.Equipment.TryEquip(weapon, out _), "Se duplicó el equipo.");
    Require(first.Inventory.Items.Count == 1 && second.Inventory.Items.Count == 0
        && first.Equipment.Weapons.Count == 1, "Pertenencia alterada.");
}

void CheckEquipmentLimit()
{
    var person = new Person("Prueba", 100);
    for (var i = 0; i < 4; i++)
    {
        var weapon = factory.CreateWeapon("pistola-t1");
        person.Inventory.Add(weapon);
        Require(person.Equipment.TryEquip(weapon, out _) == (i < 3), "Límite incorrecto.");
    }
    Require(person.Inventory.Items.Count == 4 && person.Equipment.Weapons.Count == 3,
        "Arma perdida o duplicada.");
}

void CheckRejectedCreation()
{
    var state = new GameState();
    Throws<ArgumentOutOfRangeException>(() => state.CreateCharacter((OriginPath)99, catalog));
    var incomplete = new ItemCatalog(new[]
    {
        catalog.Get("pistola-t1"), catalog.Get("municion-pistola")
    });
    Throws<InvalidOperationException>(() => state.CreateCharacter(OriginPath.Corpo, incomplete));
    Require(state.Player is null, "Quedó un personaje parcial.");
    state.CreateCharacter(OriginPath.Corpo, catalog);
    Require(state.Player is not null, "El rechazo bloqueó una creación válida.");
}

void CheckEquipmentCycles()
{
    foreach (var origin in Enum.GetValues<OriginPath>())
    {
        var state = new GameState();
        state.CreateCharacter(origin, catalog);
        var player = state.Player!;
        var items = player.Inventory.Items.ToArray();
        var weapon = player.Equipment.Weapons.Single();
        var id = weapon.RuntimeId;
        var definition = weapon.Definition;
        var loaded = weapon.LoadedAmmo;
        var quantities = items.OfType<Consumable>().Select(item => item.Quantity).ToArray();
        var health = player.CurrentHealth;
        for (var i = 0; i < 5; i++)
        {
            Require(player.Equipment.TryUnequip(weapon, out var error) && error == "",
                "No se pudo desequipar el arma inicial.");
            Require(player.Equipment.Weapons.Count == 0 && player.Inventory.Contains(weapon)
                && weapon.LoadedAmmo == loaded, "Desequipar perdió el arma o descargó munición.");
            Require(player.Equipment.TryEquip(weapon, out error) && error == "",
                "No se pudo reequipar.");
            Require(ReferenceEquals(player.Equipment.Weapons.Single(), weapon),
                "Se equipó una copia.");
            Require(player.Inventory.Items.SequenceEqual(items)
                && weapon.RuntimeId == id && ReferenceEquals(weapon.Definition, definition)
                && weapon.LoadedAmmo == loaded && player.CurrentHealth == health
                && items.OfType<Consumable>().Select(item => item.Quantity).SequenceEqual(quantities),
                "El ciclo alteró pertenencia, identidad o recursos.");
        }
    }
}

void CheckRejectedUnequip()
{
    var first = new GameState();
    var second = new GameState();
    first.CreateCharacter(OriginPath.Corpo, catalog);
    second.CreateCharacter(OriginPath.Street, catalog);
    var player = first.Player!;
    var other = second.Player!;
    var own = player.Equipment.Weapons.Single();
    var foreign = other.Equipment.Weapons.Single();
    var ownItems = player.Inventory.Items.ToArray();
    var foreignItems = other.Inventory.Items.ToArray();
    Require(!player.Equipment.TryUnequip(foreign, out var error) && error.Length > 0,
        "Se aceptó desequipar un arma ajena.");
    Require(ReferenceEquals(player.Equipment.Weapons.Single(), own)
        && ReferenceEquals(other.Equipment.Weapons.Single(), foreign),
        "El rechazo alteró el equipo de un personaje.");
    Require(player.Equipment.TryUnequip(own, out _), "No se pudo preparar el caso.");
    Require(!player.Equipment.TryUnequip(own, out error) && error.Length > 0,
        "Se aceptó desequipar dos veces.");
    Require(player.Equipment.Weapons.Count == 0
        && ReferenceEquals(other.Equipment.Weapons.Single(), foreign)
        && player.Inventory.Items.SequenceEqual(ownItems)
        && other.Inventory.Items.SequenceEqual(foreignItems)
        && own.LoadedAmmo == own.MagazineCapacity
        && foreign.LoadedAmmo == foreign.MagazineCapacity,
        "Un rechazo modificó pertenencia, equipo o cargadores.");
}

void CheckSlotReuse()
{
    var player = new Person("Prueba", 100);
    var weapons = Enumerable.Range(0, 4).Select(_ => factory.CreateWeapon("pistola-t1")).ToArray();
    foreach (var weapon in weapons)
        player.Inventory.Add(weapon);
    foreach (var weapon in weapons.Take(3))
        Require(player.Equipment.TryEquip(weapon, out _), "No se pudo preparar el caso.");
    var before = player.Equipment.Weapons.ToArray();
    Require(!player.Equipment.TryEquip(weapons[3], out _)
        && player.Equipment.Weapons.SequenceEqual(before), "La cuarta arma alteró el equipo.");
    Require(player.Equipment.TryUnequip(weapons[1], out _)
        && player.Equipment.TryEquip(weapons[3], out _), "No se reutilizó la ranura libre.");
    Require(player.Equipment.Weapons.SequenceEqual(new[] { weapons[0], weapons[2], weapons[3] })
        && player.Inventory.Items.SequenceEqual(weapons), "Referencias incorrectas al cambiar equipo.");
}

void CheckSameModelUnequip()
{
    var player = new Person("Prueba", 100);
    var loaded = factory.CreateWeapon("pistola-t1", loaded: true);
    var empty = factory.CreateWeapon("pistola-t1");
    player.Inventory.Add(loaded);
    player.Inventory.Add(empty);
    Require(player.Equipment.TryEquip(loaded, out _) && player.Equipment.TryEquip(empty, out _),
        "No se pudo preparar el caso.");
    Require(player.Equipment.TryUnequip(empty, out _)
        && ReferenceEquals(player.Equipment.Weapons.Single(), loaded),
        "Se desequipó otra arma del mismo modelo.");
    Require(player.Inventory.Contains(empty) && player.Inventory.Contains(loaded)
        && empty.LoadedAmmo == 0 && loaded.LoadedAmmo == loaded.MagazineCapacity,
        "Se mezclaron cargadores o pertenencias.");
}

void CheckInvalidContent()
{
    Throws<ArgumentOutOfRangeException>(() => factory.CreateConsumable(104, 0));
    Throws<ArgumentOutOfRangeException>(() => factory.CreateConsumable(104, -1));
    Throws<InvalidOperationException>(() => factory.CreateConsumable(1, 1));
    Throws<ArgumentException>(() => new ItemCatalog(new ItemDefinition[]
    {
        new WeaponDefinition(1, "arma", "Arma", 1, WeaponVariant.Pistol, 12, 104, 12, 0.85m, 1),
        catalog.Get(104)
    }));
    Throws<ArgumentException>(() => new ItemCatalog(new ItemDefinition[]
    {
        new WeaponDefinition(1, "arma", "Arma", 6, WeaponVariant.Pistol, 12, 101, 12, 0.85m, 1),
        catalog.Get(101)
    }));
}

void CheckDiscardEquipped()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var weapon = player.Equipment.Weapons.Single();
    var reserve = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Ammunition);
    var healing = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Healing);
    var ammo = reserve.Quantity;
    var health = player.CurrentHealth;
    Require(player.TryDiscard(weapon, 1, out var error) && error == "",
        "No se descartó el arma equipada.");
    Require(player.Equipment.Weapons.Count == 0 && !player.Inventory.Contains(weapon)
        && player.Inventory.Items.Count == 2, "El arma descartada conservó referencias.");
    Require(reserve.Quantity == ammo && healing.Quantity == 2
        && player.CurrentHealth == health, "Se recuperó el cargador o se alteraron recursos.");
    var other = new Person("Nuevo", 100);
    Throws<InvalidOperationException>(() => other.Inventory.Add(weapon));
    Require(!other.Inventory.Contains(weapon) && !other.Equipment.Weapons.Contains(weapon),
        "Se recuperó un arma descartada.");
}

void CheckDiscardLoose()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var equipped = player.Equipment.Weapons.Single();
    var loose = factory.CreateWeapon("pistola-t1", loaded: true);
    player.Inventory.Add(loose);
    var original = player.Inventory.Items.ToArray();
    Require(loose.RuntimeId != equipped.RuntimeId, "Armas sin identidad propia.");
    Require(player.TryDiscard(loose, 1, out _), "No se pudo descartar el arma suelta.");
    Require(ReferenceEquals(player.Equipment.Weapons.Single(), equipped)
        && player.Inventory.Items.SequenceEqual(original.Where(item => item != loose))
        && equipped.LoadedAmmo == equipped.MagazineCapacity,
        "Se descartó otra arma o cambió el equipo.");
}

void CheckDiscardStacks()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Street, catalog);
    var player = state.Player!;
    var healing = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Healing);
    var ammo = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Ammunition);
    var weapon = player.Equipment.Weapons.Single();
    var healingId = healing.RuntimeId;
    Require(player.TryDiscard(healing, 1, out _), "No se redujo la pila.");
    Require(player.Inventory.Contains(healing) && healing.Quantity == 1
        && healing.RuntimeId == healingId, "La pila parcial cambió de instancia.");
    Require(player.TryDiscard(healing, 1, out _)
        && !player.Inventory.Contains(healing), "La pila agotada sigue en inventario.");
    Throws<InvalidOperationException>(() => player.Inventory.Add(healing));
    Require(player.TryDiscard(ammo, ammo.Quantity, out _)
        && !player.Inventory.Contains(ammo), "La reserva no se pudo descartar.");
    Require(ReferenceEquals(player.Equipment.Weapons.Single(), weapon)
        && weapon.LoadedAmmo == weapon.MagazineCapacity
        && player.Inventory.Items.SequenceEqual(new Item[] { weapon }),
        "El descarte de pilas alteró el arma.");
}

void CheckRejectedDiscard()
{
    var first = new GameState();
    var second = new GameState();
    first.CreateCharacter(OriginPath.Corpo, catalog);
    second.CreateCharacter(OriginPath.Nomad, catalog);
    var player = first.Player!;
    var other = second.Player!;
    var weapon = player.Equipment.Weapons.Single();
    var foreign = other.Equipment.Weapons.Single();
    var healing = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Healing);
    var items = player.Inventory.Items.ToArray();
    var equipped = player.Equipment.Weapons.ToArray();
    var otherItems = other.Inventory.Items.ToArray();
    var originalAmmo = player.Inventory.Items.OfType<Consumable>().Single(c => c.Type == ConsumableType.Ammunition).Quantity;
    var invalid = new (Item? Item, int Quantity)[]
    {
        (null, 1), (foreign, 1), (weapon, 0), (weapon, 2),
        (healing, 0), (healing, -1), (healing, 3)
    };
    foreach (var (item, quantity) in invalid)
    {
        Require(!player.TryDiscard(item, quantity, out var error) && error.Length > 0,
            "Se aceptó un descarte inválido.");
        Require(player.Inventory.Items.SequenceEqual(items)
            && player.Equipment.Weapons.SequenceEqual(equipped)
            && other.Inventory.Items.SequenceEqual(otherItems)
            && ReferenceEquals(other.Equipment.Weapons.Single(), foreign)
            && weapon.LoadedAmmo == weapon.MagazineCapacity
            && healing.Quantity == 2
            && player.Inventory.Items.OfType<Consumable>().Single(c => c.Type == ConsumableType.Ammunition).Quantity == originalAmmo,
            "Un rechazo modificó recursos, pertenencia o equipo.");
    }
    Require(player.TryDiscard(healing, 2, out _), "No se pudo preparar objeto retirado.");
    Require(!player.TryDiscard(healing, 1, out _)
        && player.Inventory.Items.SequenceEqual(items.Where(item => item != healing)),
        "Se pudo descartar de nuevo una pila retirada.");
}

void CheckDiscardAll()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Nomad, catalog);
    var player = state.Player!;
    foreach (var item in player.Inventory.Items.ToArray())
        Require(player.TryDiscard(item, item is Consumable stack ? stack.Quantity : 1, out _),
            "No se pudo vaciar el inventario.");
    Require(player.Inventory.Items.Count == 0 && player.Equipment.Weapons.Count == 0
        && player.CurrentHealth == player.MaxHealth, "Inventario vacío inconsistente.");
    var replacement = factory.CreateWeapon("pistola-t1");
    player.Inventory.Add(replacement);
    Require(player.Equipment.TryEquip(replacement, out _), "El inventario quedó bloqueado.");
}

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Se esperaba {typeof(T).Name}.");
}

CombatController Combat(GameState state, double roll = 0) =>
    new(state.Player!, DevelopmentContent.MaelstromTest,
        DevelopmentContent.BasicAttack, new FixedRandom(roll));

void CheckCombatHit()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var weapon = state.Player!.Equipment.Weapons.Single();
    var combat = Combat(state);
    Require(combat.EnemyIntention.Contains("ataque") && combat.SelectedWeapon == weapon,
        "Intención o selección inicial incorrecta.");
    var turn = combat.TakeTurn(CombatAction.WeaponAttack);
    Require(turn.PlayerAction is { Valid: true, ConsumedAction: true, AmmoSpent: 1, Hit: true }
        && turn.PlayerAction.Damage?.Applied == 12
        && combat.Enemy.CurrentHealth == 28 && weapon.LoadedAmmo == 11,
        "Ataque de arma incorrecto.");
    Require(turn.EnemyAction is { ConsumedAction: true, Hit: true }
        && turn.EnemyAction.Damage?.Applied == 6 && state.Player.CurrentHealth == 94
        && turn.Status == CombatStatus.InProgress, "Respuesta enemiga incorrecta.");
}

void CheckCombatMiss()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Nomad, catalog);
    var player = state.Player!;
    var weapon = player.Equipment.Weapons.Single();
    var reserve = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Ammunition);
    var combat = Combat(state, 0.99);
    var turn = combat.TakeTurn(CombatAction.WeaponAttack);
    Require(turn.PlayerAction is { Valid: true, Hit: false, AmmoSpent: 1, Damage: null }
        && turn.EnemyAction is { Valid: true, ConsumedAction: true, Hit: false }
        && weapon.LoadedAmmo == 5 && reserve.Quantity == 6
        && combat.Enemy.CurrentHealth == 40 && player.CurrentHealth == 100,
        "Fallo no consumió costes o turno correcto.");
}

void CheckCombatNoAmmo()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var empty = factory.CreateWeapon("pistola-t1");
    player.Inventory.Add(empty);
    player.Equipment.TryEquip(empty, out _);
    var combat = Combat(state);
    Require(combat.TrySelectWeapon(empty, out _), "No se pudo elegir arma vacía.");
    var before = player.CurrentHealth;
    var turn = combat.TakeTurn(CombatAction.WeaponAttack);
    Require(!turn.PlayerAction.Valid && !turn.PlayerAction.ConsumedAction
        && turn.EnemyAction is null && combat.Enemy.CurrentHealth == 40
        && empty.LoadedAmmo == 0 && player.CurrentHealth == before,
        "Rechazo por munición alteró turno o recursos.");
    var fallback = combat.TakeTurn(CombatAction.BasicAttack);
    Require(fallback.PlayerAction.Valid && fallback.EnemyAction is not null,
        "El combate quedó bloqueado sin munición.");
}

void CheckCombatInvalidWeapon()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var loose = factory.CreateWeapon("pistola-t1", loaded: true);
    var foreign = factory.CreateWeapon("pistola-t1", loaded: true);
    player.Inventory.Add(loose);
    var combat = Combat(state);
    var initial = combat.SelectedWeapon;
    Require(!combat.TrySelectWeapon(loose, out _) && !combat.TrySelectWeapon(foreign, out _)
        && !combat.TrySelectWeapon(null, out _)
        && combat.SelectedWeapon == initial, "Selección inválida cambió arma.");
    Require(player.Equipment.TryUnequip(initial!, out _), "No se pudo preparar rechazo.");
    var turn = combat.TakeTurn(CombatAction.WeaponAttack);
    Require(!turn.PlayerAction.Valid && turn.EnemyAction is null
        && initial!.LoadedAmmo == initial.MagazineCapacity
        && player.CurrentHealth == 100 && combat.Enemy.CurrentHealth == 40,
        "Arma no equipada ejecutó un ataque.");
}

void CheckCombatSelection()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var first = player.Equipment.Weapons.Single();
    var second = factory.CreateWeapon("pistola-t1", loaded: true);
    player.Inventory.Add(second);
    player.Equipment.TryEquip(second, out _);
    var random = new CountingRandom(0);
    var combat = new CombatController(player, DevelopmentContent.MaelstromTest,
        DevelopmentContent.BasicAttack, random);
    for (var i = 0; i < 4; i++)
    {
        Require(combat.TrySelectWeapon(second, out _)
            && combat.SelectedWeapon == second && combat.TrySelectWeapon(first, out _)
            && combat.EnemyIntention.Contains("ataque"), "La selección alteró la intención.");
    }
    Require(random.Calls == 0 && first.LoadedAmmo == 12 && second.LoadedAmmo == 12
        && player.CurrentHealth == 100 && combat.Enemy.CurrentHealth == 40,
        "Seleccionar gastó turnos, recursos o aleatoriedad.");
    combat.TrySelectWeapon(second, out _);
    combat.TakeTurn(CombatAction.WeaponAttack);
    Require(second.LoadedAmmo == 11 && first.LoadedAmmo == 12 && random.Calls == 2,
        "Disparar una instancia alteró otra o duplicó turnos.");
}

void CheckCombatFallback()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var weapon = player.Equipment.Weapons.Single();
    player.TryDiscard(weapon, 1, out _);
    var combat = Combat(state);
    Require(combat.SelectedWeapon is null, "Arma descartada quedó seleccionada.");
    var invalid = combat.TakeTurn(CombatAction.WeaponAttack);
    Require(!invalid.PlayerAction.Valid && invalid.EnemyAction is null, "Ataque sin arma consumió acción.");
    var basic = combat.TakeTurn(CombatAction.BasicAttack);
    Require(basic.PlayerAction is { Valid: true, AmmoSpent: 0, Hit: true }
        && basic.PlayerAction.Damage?.Applied == 5 && basic.EnemyAction is not null,
        "Ataque básico no permitió continuar.");
    var wait = combat.TakeTurn(CombatAction.Wait);
    Require(wait.PlayerAction is { Hit: null, ConsumedAction: true, AmmoSpent: 0 }
        && wait.EnemyAction is { ConsumedAction: true }
        && combat.Enemy.CurrentHealth == 35 && player.CurrentHealth == 88,
        "Esperar no ejecutó exactamente una respuesta.");
}

void CheckCombatEnd()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Nomad, catalog);
    var combat = Combat(state);
    combat.Enemy.ReceiveDamage(39);
    var victory = combat.TakeTurn(CombatAction.WeaponAttack);
    Require(victory.Status == CombatStatus.Victory && victory.EnemyAction is null
        && victory.PlayerAction.Damage is { Applied: 1, RemainingHealth: 0, Killed: true }
        && state.Player!.CurrentHealth == 100, "Victoria no cortó el turno enemigo.");
    var after = combat.TakeTurn(CombatAction.Wait);
    Require(!after.PlayerAction.Valid && after.EnemyAction is null && after.Status == CombatStatus.Victory,
        "El combate continuó después de la victoria.");
    Require(!combat.TrySelectWeapon(state.Player!.Equipment.Weapons.Single(), out _),
        "Se cambió arma tras terminar el combate.");

    var dying = new GameState();
    dying.CreateCharacter(OriginPath.Corpo, catalog);
    dying.Player!.ReceiveDamage(99);
    var defeat = Combat(dying).TakeTurn(CombatAction.Wait);
    Require(defeat.Status == CombatStatus.Defeat
        && defeat.EnemyAction?.Damage is { Applied: 1, RemainingHealth: 0, Killed: true },
        "Derrota no terminó el combate.");
}

void CheckEnemyIndependence()
{
    var first = DevelopmentContent.MaelstromTest.CreateEnemy();
    var second = DevelopmentContent.MaelstromTest.CreateEnemy();
    first.ReceiveDamage(35);
    Require(first.Id != second.Id && first.CurrentHealth == 5
        && second.CurrentHealth == second.MaxHealth, "Perfil compartió salud entre enemigos.");
}

void CheckCombatValidation()
{
    var player = new Person("Prueba", 10);
    Throws<ArgumentOutOfRangeException>(() => player.ReceiveDamage(-1));
    var result = player.ReceiveDamage(25);
    Require(result is { Incoming: 25, Applied: 10, RemainingHealth: 0, Killed: true },
        "Daño no se limitó a salud disponible.");
    Throws<ArgumentOutOfRangeException>(() => new AttackParameters(0, 0.5m).Validate());
    Throws<ArgumentOutOfRangeException>(() => new AttackParameters(5, 1.1m).Validate());
    Throws<ArgumentException>(() => new ItemCatalog(new ItemDefinition[]
    {
        new WeaponDefinition(1, "arma", "Arma", 1, WeaponVariant.Pistol, 12, 101, 1, 2m, 1),
        catalog.Get(101)
    }));
    Throws<ArgumentException>(() => new ItemCatalog(new ItemDefinition[]
    {
        new WeaponDefinition(1, "arma", "Arma", 1, WeaponVariant.Pistol, 12, 101, 1, 0.8m, 13),
        catalog.Get(101)
    }));
}

void CheckPartialReload()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Nomad, catalog);
    var player = state.Player!;
    var weapon = player.Equipment.Weapons.Single();
    var reserve = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Ammunition);
    var combat = Combat(state, 0.99);
    for (var i = 0; i < 4; i++)
        Require(combat.TakeTurn(CombatAction.WeaponAttack).PlayerAction.AmmoSpent == 1,
            "No se gastó munición para preparar recarga.");
    Require(player.TryDiscard(reserve, 5, out _) && weapon.LoadedAmmo == 2
        && reserve.Quantity == 1, "No se preparó la reserva escasa.");
    var beforeHealth = player.CurrentHealth;
    Require(combat.Preview(CombatAction.Reload).ReloadAmount == 1,
        "La previsión de recarga parcial es incorrecta.");
    var turn = combat.TakeTurn(CombatAction.Reload);
    Require(turn.PlayerAction is { Valid: true, ConsumedAction: true, Reloaded: 1, AmmoSpent: 0 }
        && turn.EnemyAction is { ConsumedAction: true }
        && weapon.LoadedAmmo == 3 && !player.Inventory.Contains(reserve)
        && player.CurrentHealth == beforeHealth, "La recarga parcial gastó munición ajena o descartó balas.");
}

void CheckReloadAcrossStacks()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var original = player.Equipment.Weapons.Single();
    var empty = factory.CreateWeapon("pistola-t1");
    player.Inventory.Add(empty);
    Require(player.Equipment.TryEquip(empty, out _), "No se pudo equipar segundo cargador.");
    var firstStack = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Ammunition);
    Require(player.TryDiscard(firstStack, 10, out _), "No se pudo preparar pila parcial.");
    var secondStack = factory.CreateConsumable(firstStack.CatalogItemId, 3);
    var incompatible = factory.CreateConsumable(102, 8);
    player.Inventory.Add(secondStack);
    player.Inventory.Add(incompatible);
    var combat = Combat(state, 0.99);
    Require(combat.TrySelectWeapon(empty, out _), "No se eligió el arma vacía.");
    Require(combat.Preview(CombatAction.Reload).ReloadAmount == 5, "No se sumaron pilas compatibles.");
    var turn = combat.TakeTurn(CombatAction.Reload);
    Require(turn.PlayerAction is { Reloaded: 5, ConsumedAction: true }
        && turn.EnemyAction is not null && empty.LoadedAmmo == 5
        && original.LoadedAmmo == 12 && !player.Inventory.Contains(firstStack)
        && !player.Inventory.Contains(secondStack)
        && incompatible.Quantity == 8, "La recarga mezcló armas o municiones.");
}

void CheckRejectedReload()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var weapon = player.Equipment.Weapons.Single();
    var reserve = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Ammunition);
    var random = new CountingRandom(0);
    var combat = new CombatController(player, DevelopmentContent.MaelstromTest,
        DevelopmentContent.BasicAttack, random);
    Require(!combat.TakeTurn(CombatAction.Reload).PlayerAction.Valid
        && random.Calls == 0 && player.CurrentHealth == 100, "Recarga llena consumió acción.");
    Require(player.TryDiscard(reserve, reserve.Quantity, out _), "No se pudo agotar reserva.");
    combat.TakeTurn(CombatAction.WeaponAttack);
    var health = player.CurrentHealth;
    var ammo = weapon.LoadedAmmo;
    var rolls = random.Calls;
    var rejected = combat.TakeTurn(CombatAction.Reload);
    Require(!rejected.PlayerAction.Valid && rejected.EnemyAction is null
        && random.Calls == rolls && player.CurrentHealth == health && weapon.LoadedAmmo == ammo,
        "Recarga sin reserva ejecutó un turno.");
}

void CheckPreparationReload()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var weapon = player.Equipment.Weapons.Single();
    var reserve = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Ammunition);
    Require(!player.TryReload(weapon, out _, out _)
        && reserve.Quantity == 12, "Recargar un arma llena gastó reserva.");
    var combat = Combat(state, 0.99);
    combat.TakeTurn(CombatAction.WeaponAttack);
    var health = player.CurrentHealth;
    Require(player.PreviewReload(weapon, out _) == 1
        && player.TryReload(weapon, out var amount, out _)
        && amount == 1 && weapon.LoadedAmmo == 12 && reserve.Quantity == 11
        && player.CurrentHealth == health,
        "Recargar fuera del combate cambió salud o consumió munición incorrecta.");
    Require(!player.TryReload(factory.CreateWeapon("pistola-t1"), out _, out _)
        && !player.TryReload(weapon, out _, out _) && reserve.Quantity == 11,
        "Se recargó un arma ajena o un cargador lleno.");
    Require(player.Equipment.TryUnequip(weapon, out _)
        && !player.TryReload(weapon, out _, out _),
        "La recarga debe exigir un arma equipada.");
}

void CheckOriginColors()
{
    Require(UiHelpers.ColorOrigen(OriginPath.Corpo) != UiHelpers.ColorOrigen(OriginPath.Street)
        && UiHelpers.ColorOrigen(OriginPath.Street) != UiHelpers.ColorOrigen(OriginPath.Nomad)
        && UiHelpers.ColorOrigen(OriginPath.Nomad) != UiHelpers.ColorOrigen(OriginPath.Corpo)
        && UiHelpers.Tier(1) == "[grey]Tier 1[/]",
        "Los acentos no distinguen orígenes o cambiaron el color del tier.");
}

void CheckProgression()
{
    var skills = ProgressionContent.Skills;
    Throws<ArgumentOutOfRangeException>(() => new Person("Nivel inválido", 100, level: 21));
    Throws<ArgumentOutOfRangeException>(() => ProgressionContent.ExperienceForLevel(21));
    Require(skills.Count == 15 && skills.Select(skill => skill.Code).Distinct().Count() == 15
        && Enum.GetValues<AttributeType>().All(attribute =>
            skills.Count(skill => skill.Attribute == attribute) == 3)
        && skills.All(skill => skill.Percent > 0 && skill.RequiredAttribute is >= 1 and <= 10
            && skill.Name.Length > 0)
        && ProgressionContent.ExperienceForLevel(1) == 0
        && ProgressionContent.ExperienceForLevel(20) == 9500,
        "El catálogo o los umbrales provisionales son incoherentes.");
    foreach (var skill in skills)
    {
        var visited = new HashSet<string> { skill.Code };
        var prerequisite = skill.Prerequisite;
        while (prerequisite is not null)
        {
            Require(visited.Add(prerequisite), "Hay un ciclo en las habilidades.");
            var previous = ProgressionContent.Get(prerequisite);
            Require(previous.Attribute == skill.Attribute
                && previous.RequiredAttribute < skill.RequiredAttribute,
                "La dependencia no es de un nivel inferior del mismo árbol.");
            prerequisite = previous.Prerequisite;
        }
    }
    foreach (var (origin, free) in new[]
    {
        (OriginPath.Corpo, "analisis"), (OriginPath.Street, "sangre-fria"),
        (OriginPath.Nomad, "punos")
    })
    {
        var state = new GameState();
        state.CreateCharacter(origin, catalog);
        var player = state.Player!;
        Require(player.Level == 1 && player.Experience == 0
            && player.AttributePoints == 5 && player.SkillPoints == 1
            && Enum.GetValues<AttributeType>().All(attribute => player.Attribute(attribute) == 1)
            && player.Skills.SequenceEqual([free]), "Kit de progresión por origen incorrecto.");
    }
    var progression = new GameState();
    progression.CreateCharacter(OriginPath.Corpo, catalog);
    var character = progression.Player!;
    Require(character.GainExperience(30) == 0 && character.Experience == 33
        && character.GainExperience(15) == 0 && character.Experience == 49
        && character.GainExperience(30) == 1 && character.Level == 2
        && character.AttributePoints == 6 && character.SkillPoints == 2,
        "La experiencia o los puntos no avanzaron en el umbral.");
    Require(character.GainExperience(20000) == 18 && character.Level == 20
        && character.Experience == 9500 && character.AttributePoints == 24
        && character.SkillPoints == 20 && character.ExperienceToNextLevel == 0
        && character.GainExperience(10) == 0,
        "El nivel máximo o los puntos concedidos son incorrectos.");
    for (var i = 0; i < 9; i++)
        Require(character.TryIncreaseAttribute(AttributeType.Intelligence, out _),
            "No se pudo aumentar el atributo hasta diez.");
    Require(!character.TryIncreaseAttribute(AttributeType.Intelligence, out _)
        && character.Attribute(AttributeType.Intelligence) == 10
        && character.AttributePoints == 15, "El límite de atributo gastó puntos.");
    character.ReceiveDamage(character.MaxHealth);
    Throws<InvalidOperationException>(() => character.GainExperience(10));
    progression.RemoveDeadCharacter(character);
    progression.CreateCharacter(OriginPath.Nomad, catalog);
    Require(progression.Player is { Level: 1, Experience: 0, AttributePoints: 5, SkillPoints: 1 }
        && progression.Player.HasSkill("punos") && !progression.Player.HasSkill("analisis"),
        "El personaje nuevo conservó la progresión del muerto.");
}

void CheckSkills()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    Require(!player.TryUnlockSkill("analisis", out _)
        && !player.TryUnlockSkill("desconocida", out _)
        && !player.TryUnlockSkill("reflejos", out _)
        && !player.TryUnlockSkill("potencia", out _)
        && player.SkillPoints == 1,
        "Un requisito o recompra gastó puntos.");
    Require(!player.TryIncreaseAttribute((AttributeType)99, out _)
        && player.TryIncreaseAttribute(AttributeType.Agility, out _)
        && player.TryIncreaseAttribute(AttributeType.Agility, out _)
        && player.TryUnlockSkill("pulso", out _)
        && !player.TryUnlockSkill("pulso", out _),
        "No se respetaron atributos, compra única o puntos.");
    var combat = Combat(state, 0.99);
    Require(combat.Preview(CombatAction.WeaponAttack).Accuracy == 0.8925m,
        "La habilidad no modificó la precisión real del arma.");
    player.GainExperience(150);
    Require(player.TryIncreaseAttribute(AttributeType.TechnicalAbility, out _)
        && player.TryIncreaseAttribute(AttributeType.TechnicalAbility, out _)
        && player.TryUnlockSkill("mantenimiento", out _)
        && combat.Preview(CombatAction.WeaponAttack).Damage == 13,
        "La habilidad no modificó el daño real.");
    player.GainExperience(600);
    Require(player.TryUnlockSkill("calibracion", out _)
        && !player.TryUnlockSkill("primeros-auxilios", out _),
        "Se ignoró el requisito de atributo.");
    for (var i = 0; i < 3; i++)
        Require(player.TryIncreaseAttribute(AttributeType.TechnicalAbility, out _),
            "No se pudieron asignar puntos al atributo.");
    Require(player.TryUnlockSkill("primeros-auxilios", out _),
        "La habilidad con dependencia válida fue rechazada.");
    player.ReceiveDamage(40);
    var healing = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Healing);
    Require(player.TryUseHealing(healing, out var healed, out _)
        && healed == 29 && player.CurrentHealth == 89,
        "La bonificación de curación no se aplicó fuera del combate.");
    var preview = combat.Preview(CombatAction.Heal);
    Require(preview.HealAmount == 11, "La curación en combate no usa el mismo efecto.");
}

void CheckExpeditionExperience()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var expedition = ExpeditionFor(state, Enumerable.Range(0, 5)
        .Select(_ => Discovery(catalog.Get(104))).ToArray());
    var player = state.Player!;
    expedition.ResolveCurrent();
    Require(player.Experience == 16 && player.Level == 1
        && expedition.LastExperienceReward == new ExperienceReward(16, 0),
        "Un hallazgo no otorgó la experiencia provisional.");
    Throws<InvalidOperationException>(() => expedition.ResolveCurrent());
    Require(player.Experience == 16 && expedition.Current.Resolved
        && expedition.LastExperienceReward == new ExperienceReward(16, 0),
        "Consultar el evento otorgó experiencia duplicada.");
    expedition.Advance();
    expedition.ResolveCurrent();
    Require(expedition.LastExperienceReward == new ExperienceReward(16, 0),
        "Se duplicó el aviso de XP entre eventos.");
    expedition.Advance();
    expedition.ResolveCurrent();
    Require(expedition.LastExperienceReward == new ExperienceReward(16, 0),
        "El premio del nuevo evento fue incorrecto.");
    expedition.Advance();
    expedition.ResolveCurrent();
    Require(expedition.LastExperienceReward == new ExperienceReward(16, 1),
        "No se informó la subida de nivel ni los puntos.");
    expedition.Advance();
    expedition.ResolveCurrent();
    expedition.Advance();
    Require(player.Experience == 80 && player.Level == 2
        && player.AttributePoints == 6 && player.SkillPoints == 2
        && expedition.LastExperienceReward == new ExperienceReward(16, 0),
        "Los cinco eventos no actualizaron correctamente la progresión.");
}

void CheckCyberware()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Street, catalog);
    var player = state.Player!;
    Require(player.Cyberdeck == CyberwareContent.StartingDeck
        && player.MaxRam == 6 && player.Ram == 6 && player.UsedCyberwareCapacity == 8,
        "El kit inicial no instaló un deck con RAM.");
    Require(!state.TryBuyExDisk(out _) && !state.TryExtractImplant(CyberwareZone.OperatingSystem, out _)
        && player.Money == 0 && state.ExDiskInStock && player.SpareCyberware.Count == 0,
        "El ripperdoc operó sin fondos.");
    Require(player.CyberwareFitError(new CyberwareDefinition("grande", "Grande", CyberwareZone.Eyes, 13))
        == "Capacidad de cyberware insuficiente.", "No detectó un implante que excede la capacidad.");

    void EarnExpeditionMoney(Action? during = null)
    {
        var expedition = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
            new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
        state.StartExpedition(expedition);
        do
        {
            expedition.ResolveCurrent();
        } while (expedition.Advance());
        during?.Invoke();
        state.CompleteExpedition(expedition);
    }

    EarnExpeditionMoney(() => Require(!state.TryBuyExDisk(out _) && player.Money == 75,
        "El ripperdoc cobró durante una aventura."));
    Require(state.TryBuyExDisk(out _) && player.Money == 45 && !state.ExDiskInStock && player.MaxRam == 6,
        "La compra no respetó el saldo, stock o separación de la instalación.");
    var first = player.SpareCyberware.Single();
    Require(!state.TryBuyExDisk(out _) && !state.TryInstallImplant(first with { }, out _)
        && player.Money == 45 && player.SpareCyberware.Count == 1,
        "Se compró sin stock o se instaló un implante ajeno.");
    player.SpendRam(3);
    Require(state.TryInstallImplant(first, out _) && player is { Money: 40, Ram: 3, MaxRam: 8, UsedCyberwareCapacity: 12 }
        && player.SpareCyberware.Count == 0, "Instalar Ex-Disk recargó RAM o aplicó mal el coste.");
    EarnExpeditionMoney(() => Require(!state.TryExtractImplant(CyberwareZone.FrontalCortex, out _)
        && !state.ExDiskInStock, "Se extrajo un implante o se repuso el stock antes de regresar."));
    Require(player.Ram == 8 && state.ExDiskInStock && state.TryBuyExDisk(out _),
        "Regresar no recuperó RAM o no repuso el stock.");
    var second = player.SpareCyberware.Single();
    Require(!state.TryInstallImplant(second, out _) && player.Money == 85 && player.Ram == 8,
        "Instaló en una zona ocupada o cobró por el rechazo.");
    Require(state.TryExtractImplant(CyberwareZone.FrontalCortex, out _)
        && player is { Money: 80, Ram: 6, MaxRam: 6 }
        && ReferenceEquals(player.SpareCyberware.Last(), first)
        && state.TryInstallImplant(first, out _) && player.Ram == 6 && player.MaxRam == 8
        && ReferenceEquals(player.SpareCyberware.Single(), second),
        "Extracción/reinstalación no conservó pertenencia, saldo o límite de RAM.");
    Require(state.TryExtractImplant(CyberwareZone.OperatingSystem, out _)
        && player is { Ram: 0, MaxRam: 0 }
        && !Combat(state, 0).Preview(CombatAction.ShortCircuit).Available,
        "El bonus de Ex-Disk funcionó sin un deck.");
    var deck = player.SpareCyberware.Single(implant => implant.Zone == CyberwareZone.OperatingSystem);
    Require(state.TryInstallImplant(deck, out _) && player is { Ram: 0, MaxRam: 8 },
        "Reinstalar el deck recargó RAM.");
    player.RegenerateRam();
    Require(player.Ram == 1, "El deck reinstalado no regenera.");
    player.ReceiveDamage(100);
    Require(!state.TryInstallImplant(second, out _)
        && !state.TryExtractImplant(CyberwareZone.FrontalCortex, out _),
        "Personaje muerto instaló o extrajo cyberware.");
    state.RemoveDeadCharacter(player);
    state.CreateCharacter(OriginPath.Nomad, catalog);
    Require(state.Player is { Ram: 6, UsedCyberwareCapacity: 8 }
        && state.Player.InstalledCyberware.Count == 1 && state.Player.SpareCyberware.Count == 0,
        "El personaje nuevo heredó instalaciones.");
}

void CheckShortCircuit()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var combat = Combat(state, 0.99);
    var preview = combat.Preview(CombatAction.ShortCircuit);
    Require(preview is { Available: true, Damage: 12, RamCost: 3, Accuracy: null }
        && player.Ram == 6, "Consultar Cortocircuito gastó recursos o dados.");
    var first = combat.TakeTurn(CombatAction.ShortCircuit);
    Require(first.PlayerAction is { Valid: true, RamSpent: 3, Hit: true }
        && first.EnemyAction is not null && player.Ram == 4
        && first.PlayerAction.Damage?.Applied == 12,
        "El quickhack no gastó RAM, consumió turno o no regeneró.");
    combat.TakeTurn(CombatAction.ShortCircuit);
    Require(player.Ram == 2 && !combat.Preview(CombatAction.ShortCircuit).Available,
        "RAM insuficiente dejó ejecutar el hack.");
    var health = player.CurrentHealth;
    var enemyHealth = combat.Enemy.CurrentHealth;
    var rejected = combat.TakeTurn(CombatAction.ShortCircuit);
    Require(!rejected.PlayerAction.Valid && rejected.EnemyAction is null
        && player.Ram == 2 && player.CurrentHealth == health
        && combat.Enemy.CurrentHealth == enemyHealth,
        "El rechazo consumió turno o recursos.");
    combat.TakeTurn(CombatAction.Wait);
    Require(player.Ram == 3, "Esperar no regeneró RAM.");
    var lethal = new CombatController(player, new EnemyProfile("Débil", "Test", 10,
        new AttackParameters(1, 0)), DevelopmentContent.BasicAttack, new Random(1));
    Require(player.Ram == 4, "El primer turno del nuevo combate no recuperó RAM.");
    var victory = lethal.TakeTurn(CombatAction.ShortCircuit);
    Require(victory.Status == CombatStatus.Victory && victory.EnemyAction is null
        && player.Ram == 1, "El hack letal respondió o devolvió RAM.");
    player.ReceiveDamage(player.CurrentHealth);
    Throws<InvalidOperationException>(() => player.SpendRam(1));
    state.RemoveDeadCharacter(player);
    state.CreateCharacter(OriginPath.Street, catalog);
    Require(state.Player is { Ram: 6, MaxRam: 6 },
        "La muerte conservó RAM en el siguiente personaje.");
}

void CheckOverheat()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    const CombatAction overheat = CombatAction.Overheat;
    Require(player.EquippedQuickhacks.Single().Action == CombatAction.ShortCircuit
        && !state.TryBuyQuickhack(overheat, out _) && !player.TryConfigureQuickhack(overheat, true, out _),
        "Se equipó o compró un quickhack sin cumplir requisitos.");
    var exp = ExpeditionFor(state, Enumerable.Range(0, 5)
        .Select(_ => Discovery(catalog.Get("curacion"))).ToArray());
    state.StartExpedition(exp);
    do
    {
        exp.ResolveCurrent();
    } while (exp.Advance());
    Require(!state.TryBuyQuickhack(overheat, out _) && player.Money == 40, "La tienda operó en aventura.");
    state.CompleteExpedition(exp);
    Require(state.TryBuyQuickhack(overheat, out _) && player.Money == 15
        && player.OwnsQuickhack(overheat) && !player.IsQuickhackEquipped(overheat)
        && !state.TryBuyQuickhack(overheat, out _), "La compra no respetó precio o configuración.");

    CombatController MakeCombat(int health, int enemyDamage = 1) =>
        new(player, new EnemyProfile("Objetivo", "Test", health, new AttackParameters(enemyDamage, 1)),
            DevelopmentContent.BasicAttack, new CountingRandom(0));

    var combat = MakeCombat(18);
    Require(!combat.TakeTurn(overheat).PlayerAction.Valid && player.Ram == 6
        && player.TryConfigureQuickhack(overheat, true, out _)
        && !player.TryConfigureQuickhack(overheat, true, out _),
        "Ejecutó un quickhack no equipado o lo equipó dos veces.");
    var first = combat.TakeTurn(overheat);
    Require(first.PlayerAction is { RamSpent: 4, Damage.Applied: 6 }
        && first.OverheatPulse is { Applied: 6, RemainingHealth: 6 }
        && first.EnemyAction is not null && combat.OverheatPulses == 1 && player.Ram == 3,
        "El lanzamiento no resolvió daño inmediato y primer pulso antes de la respuesta.");
    player.RestoreRam();
    Require(!combat.Preview(overheat).Available && !combat.TakeTurn(overheat).PlayerAction.Valid
        && combat.OverheatPulses == 1 && player.Ram == 6 && combat.Enemy.CurrentHealth == 6,
        "Repetir el quickhack acumuló el efecto, gastó RAM o avanzó el pulso.");
    var last = combat.TakeTurn(CombatAction.Wait);
    Require(last is { Status: CombatStatus.Victory, EnemyAction: null, OverheatPulse.Killed: true }
        && combat.OverheatPulses == 0 && player.CurrentHealth == 99,
        "El segundo pulso letal permitió respuesta o dejó el estado activo.");

    var next = MakeCombat(40, enemyDamage: 100);
    Require(next.TakeTurn(CombatAction.Wait).OverheatPulse is null && next.OverheatPulses == 0,
        "Se arrastró daño persistente al siguiente combate.");
    state.RemoveDeadCharacter(player);
    state.CreateCharacter(OriginPath.Nomad, catalog);
    Require(!state.Player!.OwnsQuickhack(overheat), "El personaje nuevo heredó el quickhack.");
}

void CheckSynapseBurnout()
{
    const CombatAction action = CombatAction.SynapseBurnout;
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var exp = ExpeditionFor(state, Enumerable.Range(0, 5)
        .Select(_ => Discovery(catalog.Get("curacion"))).ToArray());
    state.StartExpedition(exp);
    do
    {
        exp.ResolveCurrent();
    } while (exp.Advance());
    state.CompleteExpedition(exp);
    Require(!state.TryBuyQuickhack(CombatAction.Wait, out _)
        && state.TryBuyQuickhack(action, out _) && player.Money == 5
        && !player.IsQuickhackEquipped(action) && player.TryConfigureQuickhack(action, true, out _),
        "La tienda cobró por una acción inválida o no vendió el quickhack correcto.");

    var enemy = DevelopmentContent.MaelstromTest with { MaxHealth = 100 };
    CombatController Start() => new(player, enemy, DevelopmentContent.BasicAttack, new CountingRandom(0.99));
    var combat = Start();
    Require(combat.Preview(action) is { Damage: 8, RamCost: 3 }, "Vista previa inicial incorrecta.");
    combat.TakeTurn(CombatAction.ShortCircuit);
    Require(combat.Preview(action).Damage == 14 && combat.TakeTurn(action).PlayerAction.Damage?.Applied == 14
        && !combat.TakeTurn(action).PlayerAction.Valid, "No se usó la RAM previa o se ejecutó sin RAM.");
    combat.TakeTurn(CombatAction.Wait);
    Require(combat.Preview(action).Damage == 20 && combat.TakeTurn(action).PlayerAction.Damage?.Applied == 20,
        "Recuperar RAM borró gasto previo o un rechazo contó como gasto.");
    combat.TakeTurn(CombatAction.Wait);
    combat.TakeTurn(CombatAction.Wait);
    Require(combat.Preview(action).Damage == 24 && combat.TakeTurn(action).PlayerAction.Damage?.Applied == 24,
        "El daño no respetó el tope.");
    player.RestoreRam();
    Require(Start().Preview(action).Damage == 8, "El gasto sobrevivió al combate.");
}

void CheckWeaponGlitch()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var expedition = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
        new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
    state.StartExpedition(expedition);
    do
    {
        expedition.ResolveCurrent();
    } while (expedition.Advance());
    state.CompleteExpedition(expedition);
    const CombatAction glitch = CombatAction.WeaponGlitch;
    Require(state.TryBuyQuickhack(glitch, out _) && player.Money == 45
        && player.TryConfigureQuickhack(glitch, true, out _)
        && state.TryBuyQuickhack(CombatAction.Overheat, out _)
        && player.TryConfigureQuickhack(CombatAction.Overheat, true, out _), "Falló el kit de prueba.");
    var noWeapon = Combat(state, 0);
    Require(!noWeapon.Preview(glitch).Available
        && noWeapon.TakeTurn(glitch) is { PlayerAction.ConsumedAction: false, EnemyAction: null }
        && player.Ram == 6, "Un enemigo desarmado permitió gastar turno o RAM.");

    var zone = ExpeditionContent.Create(ExpeditionContent.Japantown, new Random(1), catalog, player);
    var profile = zone.Events.First(entry => entry.Kind == EventKind.Combat).Enemy!;
    Require(profile.Loadout is not null, "Japantown no generó un tirador armado.");
    var combat = new CombatController(player, profile with
    {
        Loadout = profile.Loadout! with { Loaded = 1, Reserve = 1 }
    }, DevelopmentContent.BasicAttack, new CountingRandom(0.99));
    var weapon = combat.Enemy.Equipment.Weapons.Single();
    Require(combat.PlannedEnemyAction == CombatAction.WeaponAttack
        && combat.Preview(glitch) is { Available: true, Damage: null, RamCost: 3 }, "Previsión inicial inválida.");
    Require(combat.TakeTurn(glitch) is { BlockedEnemyAction: CombatAction.WeaponAttack,
            EnemyAction: null, PlayerAction: { RamSpent: 3, Damage: null } }
        && player.Ram == 4 && weapon.LoadedAmmo == 1, "El bloqueo de disparo no conservó la munición.");
    Require(combat.TakeTurn(CombatAction.Overheat).EnemyAction?.Action == CombatAction.WeaponAttack
        && weapon.LoadedAmmo == 0 && combat.PlannedEnemyAction == CombatAction.Reload,
        "El disparo no se reanudó o no dio paso a la recarga.");
    player.RestoreRam();
    Require(combat.TakeTurn(glitch) is { BlockedEnemyAction: CombatAction.Reload,
            OverheatPulse.Applied: 6, EnemyAction: null }
        && weapon.LoadedAmmo == 0 && combat.PlannedEnemyAction == CombatAction.Reload,
        "Bloquear la recarga omitió el pulso o cambió el cargador.");
    Require(combat.TakeTurn(CombatAction.Wait).EnemyAction is { Action: CombatAction.Reload, Reloaded: 1 }
        && combat.TakeTurn(CombatAction.Wait).EnemyAction?.Action == CombatAction.WeaponAttack
        && combat.PlannedEnemyAction == CombatAction.BasicAttack
        && combat.TakeTurn(glitch) is { BlockedEnemyAction: null, EnemyAction.Action: CombatAction.BasicAttack },
        "No se reanudó la recarga o el bloqueo detuvo un ataque básico.");

    player.RestoreRam();
    var lethal = new CombatController(player, profile with { MaxHealth = 18 },
        DevelopmentContent.BasicAttack, new CountingRandom(0.99));
    lethal.TakeTurn(CombatAction.Overheat);
    player.RestoreRam();
    Require(lethal.TakeTurn(glitch) is { Status: CombatStatus.Victory, OverheatPulse.Killed: true,
        BlockedEnemyAction: null, EnemyAction: null }, "El pulso letal dejó resolver una acción enemiga.");
}

void CheckCyberwareDefense()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var zone = ExpeditionContent.Create(ExpeditionContent.Dogtown, new Random(1), catalog, player);
    var netrunner = zone.Events.Single(entry => entry.Enemy?.Netrunner == true).Enemy! with { ControlQuickhacks = false };
    for (var run = 0; run < 2; run++)
    {
        var expedition = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
            new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
        state.StartExpedition(expedition);
        do { expedition.ResolveCurrent(); } while (expedition.Advance());
        state.CompleteExpedition(expedition);
    }
    const CombatAction malfunction = CombatAction.CyberwareMalfunction;
    Require(state.TryBuyImplant(CyberwareContent.SelfIce, out _)
        && !state.HasImplantStock(CyberwareContent.SelfIce)
        && state.TryInstallImplant(player.SpareCyberware.Single(), out _)
        && state.TryBuyQuickhack(malfunction, out _)
        && player.TryConfigureQuickhack(malfunction, true, out _),
        "No se pudo obtener el kit de Self-ICE y Fallo de cyberware.");
    CombatController Start() => new(player, netrunner, DevelopmentContent.BasicAttack, new CountingRandom(0.99));
    var fight = Start();
    Require(fight.PlayerSelfIceReady && fight.EnemySelfIceReady && fight.PlannedEnemyAction == malfunction,
        "No se configuraron las dos defensas o el netrunner.");
    var first = fight.TakeTurn(malfunction);
    Require(first is { PlayerAction: { BlockedBySelfIce: true, RamSpent: 4 },
            EnemyAction: { BlockedBySelfIce: true, RamSpent: 4 } }
        && !fight.PlayerSelfIceReady && !fight.EnemySelfIceReady
        && player.Ram == 3 && fight.Enemy.Ram == 2,
        "El primer quickhack no fue bloqueado o no pagó su coste.");
    Require(fight.Preview(CombatAction.ShortCircuit).Damage == 9
        && fight.PlannedEnemyAction == CombatAction.ShortCircuit && fight.Enemy.Ram == 2,
        "La intención no anticipó la regeneración sin aplicarla.");
    var second = fight.TakeTurn(CombatAction.ShortCircuit);
    Require(second is { PlayerAction.Damage.Applied: 9, EnemyAction.Damage.Applied: 9 },
        "La reducción posterior no se aplicó a ambos combatientes.");
    player.RestoreRam();
    var third = fight.TakeTurn(malfunction);
    Require(third.EnemyAction?.Action == CombatAction.BasicAttack && fight.Enemy.CyberwareDisabledTurns == 1
        && fight.Enemy.MaxRam == 0 && fight.Enemy.Ram == 0
        && fight.Enemy.InstalledCyberware.Count == 2 && !fight.Preview(malfunction).Available,
        "Desactivar modificó instalaciones o el enemigo conservó acceso al deck.");
    var fourth = fight.TakeTurn(CombatAction.Wait);
    Require(fourth.EnemyCyberwareRestored && fourth.EnemyAction?.Action == CombatAction.BasicAttack
        && fight.Enemy.MaxRam == 6 && fight.Enemy.Ram == 0 && !fight.EnemySelfIceReady,
        "El segundo turno restauró RAM o rearmó Self-ICE.");

    // Sin la defensa frontal, el primer intento enemigo sí reduce el máximo del jugador.
    Require(state.TryExtractImplant(CyberwareZone.FrontalCortex, out _)
        && state.TryBuyExDisk(out _) && state.TryInstallImplant(player.SpareCyberware.Single(x => x == CyberwareContent.ExDisk), out _),
        "No se pudo cambiar Self-ICE por Ex-Disk.");
    player.RestoreRam();
    var next = Start();
    Require(next.EnemySelfIceReady, "Self-ICE no volvió a estar disponible en otro combate.");
    next.TakeTurn(CombatAction.Wait);
    Require(player.MaxRam == 0 && player.Ram == 0 && player.CyberwareDisabledTurns == 2
        && !next.TakeTurn(CombatAction.ShortCircuit).PlayerAction.Valid
        && player.CyberwareDisabledTurns == 2, "Un rechazo consumió un turno de desactivación.");
    next.TakeTurn(CombatAction.Wait);
    var last = next.TakeTurn(CombatAction.Wait);
    Require(last.PlayerCyberwareRestored && player.MaxRam == 8 && player.Ram == 1,
        "El inicio del turno reactivado no aplicó exactamente una recuperación de RAM.");

    var medicine = player.Inventory.Items.OfType<Consumable>().Single(item => item.Type == ConsumableType.Healing);
    Require(state.TrySellToShop(medicine, 2, out _) && state.TryBuyQuickhack(CombatAction.Overheat, out _)
        && player.TryConfigureQuickhack(CombatAction.Overheat, true, out _), "No se pudo preparar el pulso.");
    var lethalPulse = new CombatController(player, netrunner with { MaxHealth = 12 },
        DevelopmentContent.BasicAttack, new CountingRandom(0.99));
    lethalPulse.Enemy.SpendRam(6);
    player.RestoreRam();
    lethalPulse.TakeTurn(CombatAction.ShortCircuit);
    lethalPulse.TakeTurn(CombatAction.Overheat);
    Require(lethalPulse.Enemy.Ram == 2
        && lethalPulse.TakeTurn(CombatAction.Wait) is { OverheatPulse.Killed: true, EnemyAction: null }
        && lethalPulse.Enemy.Ram == 2, "Un pulso letal dejó regenerar al netrunner.");
}

void CheckSuicide()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    const CombatAction execution = CombatAction.Suicide;
    Require(!state.TryBuyQuickhack(execution, out _), "La tienda vendió sin fondos.");
    for (var run = 0; run < 3; run++)
    {
        var expedition = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
            new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
        state.StartExpedition(expedition);
        do { expedition.ResolveCurrent(); } while (expedition.Advance());
        state.CompleteExpedition(expedition);
    }
    Require(state.TryBuyQuickhack(execution, out _) && player.Money == 150
        && !player.IsQuickhackEquipped(execution) && !state.TryBuyQuickhack(execution, out _),
        "La compra equipó la ejecución, cobró de más o se duplicó.");
    foreach (var action in new[] { CombatAction.Overheat, CombatAction.SynapseBurnout, CombatAction.WeaponGlitch })
        Require(state.TryBuyQuickhack(action, out _) && player.TryConfigureQuickhack(action, true, out _),
            "No se pudo llenar el deck.");
    Require(!player.TryConfigureQuickhack(execution, true, out _)
        && player.TryConfigureQuickhack(CombatAction.WeaponGlitch, false, out _)
        && player.TryConfigureQuickhack(execution, true, out _)
        && !Combat(state, 0).TakeTurn(execution).PlayerAction.ConsumedAction && player.Ram == 6,
        "Se superaron cuatro ranuras o se ejecutó con RAM insuficiente.");
    Require(state.TryBuyExDisk(out _) && state.TryInstallImplant(player.SpareCyberware.Single(), out _)
        && player.Ram == 6 && player.MaxRam == 8, "Ex-Disk no habilitó el máximo sin rellenar RAM.");
    player.RestoreRam();
    var profile = DevelopmentContent.MaelstromTest with { Netrunner = true, MaxHealth = 120 };
    var fight = new CombatController(player, profile, DevelopmentContent.BasicAttack, new CountingRandom(0.99));
    fight.Enemy.SpendRam(fight.Enemy.Ram);
    Require(fight.TakeTurn(execution) is { PlayerAction: { RamSpent: 8, BlockedBySelfIce: true },
        Status: CombatStatus.InProgress, EnemyAction: not null } && player.Ram == 1
        && fight.Enemy.CurrentHealth == 120 && !fight.EnemySelfIceReady,
        "Self-ICE no bloqueó la ejecución o el lanzamiento no pagó su coste.");
    player.RestoreRam();
    Require(fight.Preview(execution) is { Available: true, Damage: 120, RamCost: 8 }
        && fight.TakeTurn(execution) is { Status: CombatStatus.Victory, EnemyAction: null,
            PlayerAction: { Damage.Killed: true, RamSpent: 8 } } && player.Ram == 0,
        "La reducción de Self-ICE impidió la ejecución o el enemigo respondió.");
}

void CheckBot()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    for (var run = 0; run < 2; run++)
    {
        var expedition = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
            new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
        state.StartExpedition(expedition);
        do { expedition.ResolveCurrent(); } while (expedition.Advance());
        state.CompleteExpedition(expedition);
    }
    Require(state.TryBuyQuickhack(CombatAction.Suicide, out _)
        && player.TryConfigureQuickhack(CombatAction.Suicide, true, out _)
        && state.TryBuyExDisk(out _) && state.TryInstallImplant(player.SpareCyberware.Single(), out _),
        "No se pudo preparar el personaje de prueba.");
    player.RestoreRam();
    var dogtown = ExpeditionContent.Create(ExpeditionContent.Dogtown, new Random(1), catalog, player);
    var profile = dogtown.Events.Single(entry => entry.Enemy?.IsBot == true).Enemy!;
    var fight = new CombatController(player, profile, DevelopmentContent.BasicAttack, new CountingRandom(0));
    Require(fight.Enemy.IsBot && !fight.Preview(CombatAction.Suicide).Available
        && fight.TakeTurn(CombatAction.Suicide) is { PlayerAction.ConsumedAction: false, EnemyAction: null }
        && player.Ram == 8 && player.CurrentHealth == 100 && fight.Enemy.CurrentHealth == profile.MaxHealth,
        "La ejecución contra el bot alteró recursos o salud.");
    Require(fight.Preview(CombatAction.ShortCircuit) is { Damage: 18, RamCost: 3 }
        && fight.TakeTurn(CombatAction.ShortCircuit).PlayerAction is { Damage.Applied: 18, RamSpent: 3 }
        && Combat(state, 0).Preview(CombatAction.ShortCircuit) is { Damage: 12, RamCost: 3 },
        "La diferencia de Cortocircuito no coincide con el tipo de objetivo.");
}

void CheckWithdrawal()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    Expedition NewExpedition() => ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
        new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
    var expedition = NewExpedition();
    state.StartExpedition(expedition);
    expedition.ResolveCurrent();
    expedition.Advance();
    var fight = Combat(state, 0);
    fight.TakeTurn(CombatAction.ShortCircuit);
    var carried = player.Inventory.Items.OfType<Consumable>().ToArray();
    var weapons = player.Inventory.Items.OfType<Weapon>().Append(factory.CreateWeapon("pistola-t1", loaded: true)).ToArray();
    player.Inventory.Add(weapons.Last());
    var health = player.CurrentHealth;
    var ram = player.Ram;
    Throws<InvalidOperationException>(() => state.WithdrawExpedition(NewExpedition()));
    fight.Withdraw();
    Require(fight.Status == CombatStatus.Withdrawn && !fight.TakeTurn(CombatAction.Wait).PlayerAction.Valid,
        "Un combate abandonado aceptó otro turno.");
    state.WithdrawExpedition(expedition);
    Require(expedition.Status == ExpeditionStatus.Withdrawn && !expedition.Current.Resolved
        && state.ActiveExpedition is null && ReferenceEquals(state.Player, player)
        && player.Experience == 33 && player.Money == 0
        && player.Ram == ram && player.CurrentHealth == health
        && player.IsQuickhackEquipped(CombatAction.ShortCircuit)
        && player.Inventory.Items.SequenceEqual(carried) && player.Equipment.Weapons.Count == 0,
        "La retirada alteró recursos conservados o no aplicó todas las pérdidas.");
    foreach (var weapon in weapons)
        Throws<InvalidOperationException>(() => player.Inventory.Add(weapon));
    Throws<InvalidOperationException>(() => state.CompleteExpedition(expedition));
    Throws<InvalidOperationException>(() => expedition.ResolveCurrent());

    // Comprobar una retirada posterior a una compra sin que la tienda reponga esa unidad.
    var second = NewExpedition();
    state.StartExpedition(second);
    do { second.ResolveCurrent(); } while (second.Advance());
    state.CompleteExpedition(second);
    var offer = state.ShopOffers.First();
    Require(state.TryBuyFromShop(offer, 1, out _), "No se pudo preparar el stock de prueba.");
    var stock = offer.Stock;
    var third = NewExpedition();
    state.StartExpedition(third);
    state.WithdrawExpedition(third);
    Require(offer.Stock == stock && player.Money == 0, "Retirarse repuso la tienda o conservó dinero.");
}

void CheckSandevistan()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    const CombatAction activate = CombatAction.ActivateSandevistan;
    var expedition = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
        new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
    state.StartExpedition(expedition);
    do { expedition.ResolveCurrent(); } while (expedition.Advance());
    state.CompleteExpedition(expedition);
    Require(!Combat(state, 0).Preview(activate).Available
        && state.TryBuyImplant(CyberwareContent.Sandevistan, out _)
        && !state.HasImplantStock(CyberwareContent.Sandevistan),
        "Se activó sin implante o falló la compra.");
    var sandevistan = player.SpareCyberware.Single();
    Require(!state.TryInstallImplant(sandevistan, out _) && player.Money == 15
        && state.TryExtractImplant(CyberwareZone.OperatingSystem, out _)
        && state.TryInstallImplant(sandevistan, out _) && player.Money == 5
        && player.MaxRam == 0 && !player.EquippedQuickhacks.Any(),
        "La instalación no respetó exclusividad, coste o pérdida del deck.");

    var fight = Combat(state, 0.99);
    Require(fight.Preview(CombatAction.WeaponAttack).Accuracy < 1m
        && fight.TakeTurn(activate) is { PlayerAction.ConsumedAction: true, EnemyAction: null }
        && fight.SandevistanActive && fight.SandevistanCooldown == 4 && player.CurrentHealth == 100,
        "Activar no dejó una acción libre en el mismo turno.");
    Require(!fight.TakeTurn(activate).PlayerAction.Valid && !fight.TakeTurn(CombatAction.Reload).PlayerAction.Valid
        && fight.TrySelectWeapon(player.Equipment.Weapons.Single(), out _)
        && fight.SandevistanActive && fight.SandevistanCooldown == 4,
        "Un rechazo o selección gratuita consumió el turno.");
    Require(fight.Preview(CombatAction.WeaponAttack).Accuracy == 1m
        && fight.Preview(CombatAction.BasicAttack).Accuracy == 1m
        && fight.TakeTurn(CombatAction.WeaponAttack) is
            { PlayerAction: { Hit: true, Critical: false, AmmoSpent: 1 }, EnemySkippedBySandevistan: true, EnemyAction: null }
        && player.CurrentHealth == 100 && !fight.SandevistanActive && fight.SandevistanCooldown == 4,
        "La acción adicional no se ejecutó o el enemigo respondió.");
    for (var remaining = 3; remaining >= 0; remaining--)
    {
        Require(!fight.Preview(activate).Available, "Terminó el enfriamiento antes de tiempo.");
        Require(fight.TakeTurn(CombatAction.Wait).EnemyAction is not null
            && fight.SandevistanCooldown == remaining, "El enfriamiento no avanzó un turno propio.");
    }
    Require(fight.Preview(activate).Available, "No estuvo listo en el turno 6.");

    var netrunner = DevelopmentContent.MaelstromTest with { Netrunner = true };
    var next = new CombatController(player, netrunner, DevelopmentContent.BasicAttack, new CountingRandom(0.99));
    next.Enemy.SpendRam(1);
    Require(next.TakeTurn(activate).PlayerAction.Valid && next.Enemy.Ram == 5
        && next.TakeTurn(CombatAction.Wait).EnemySkippedBySandevistan && next.Enemy.Ram == 6,
        "Un turno omitido no procesó la regeneración enemiga o heredó el enfriamiento.");
    next.TakeTurn(CombatAction.Wait);
    Require(player.CyberwareDisabledTurns == 2 && !next.Preview(activate).Available
        && !next.TakeTurn(activate).PlayerAction.Valid && next.SandevistanCooldown == 3,
        "Fallo de cyberware no impidió activar o el rechazo avanzó el enfriamiento.");
    Require(next.TakeTurn(CombatAction.Wait).PlayerAction.Valid && next.SandevistanCooldown == 2,
        "El enfriamiento se pausó al desactivar los implantes.");

    var lethal = new CombatController(state.Player!, DevelopmentContent.MaelstromTest with { MaxHealth = 1 },
        DevelopmentContent.BasicAttack, new CountingRandom(0.99));
    lethal.TakeTurn(CombatAction.Wait); // Termina la desactivación del personaje, sin restaurar recursos.
    Require(lethal.TakeTurn(activate).PlayerAction.Valid
        && lethal.TakeTurn(CombatAction.BasicAttack).Status == CombatStatus.Victory
        && lethal.SandevistanCooldown == 0 && !lethal.SandevistanActive,
        "La victoria no limpió el estado temporal.");

    var tempo = Combat(state, 0.99);
    tempo.TakeTurn(CombatAction.BasicAttack);
    var beforeActivation = player.Stamina;
    var enemyStamina = tempo.Enemy.Stamina;
    Require(tempo.TakeTurn(activate).PlayerAction.Valid && player.Stamina == beforeActivation
        && tempo.Enemy.Stamina == enemyStamina
        && tempo.TakeTurn(CombatAction.Wait).EnemySkippedBySandevistan
        && player.Stamina == beforeActivation + 3 && tempo.Enemy.Stamina == enemyStamina + 3,
        "Sandevistan regeneró antes de tiempo u omitió la recuperación enemiga.");
    var optics = new CombatController(player, netrunner with { ControlQuickhacks = true },
        DevelopmentContent.BasicAttack, new CountingRandom(0.99));
    optics.TakeTurn(CombatAction.Wait);
    optics.TakeTurn(CombatAction.Wait);
    Require(optics.PlayerOpticsDisrupted && optics.Preview(CombatAction.BasicAttack).Accuracy == 0.05m
        && optics.TakeTurn(activate).PlayerAction.Valid && optics.Preview(CombatAction.BasicAttack).Accuracy == 1m
        && optics.TakeTurn(CombatAction.BasicAttack).PlayerAction is { Hit: true, Critical: false },
        "Reboot Optics anuló el acierto garantizado o el golpe se convirtió en crítico.");
}

void CheckShotgunAmmo()
{
    foreach (var code in new[] { "escopeta-corredera-t1", "escopeta-doble-t1" })
    {
        var state = new GameState();
        state.CreateCharacter(OriginPath.Nomad, catalog);
        var player = state.Player!;
        var weapon = code == "escopeta-corredera-t1" ? player.Equipment.Weapons.Single()
            : factory.CreateWeapon(code, loaded: true);
        if (!player.Inventory.Contains(weapon))
        {
            player.Inventory.Add(weapon);
            Require(player.Equipment.TryEquip(weapon, out _), "No se pudo equipar la escopeta.");
        }
        var reserve = player.Inventory.Items.OfType<Consumable>().Single(item => item.Type == ConsumableType.Ammunition);
        var combat = Combat(state, 0.99);
        Require(combat.TrySelectWeapon(weapon, out _), $"No se pudo seleccionar {code}.");
        var capacity = weapon.MagazineCapacity;
        for (var remaining = capacity; remaining > 0; remaining--)
        {
            if (remaining == capacity || remaining == 1)
                Require(combat.TakeTurn(CombatAction.ShortCircuit).PlayerAction is { AmmoSpent: 0, RamSpent: 3 }
                    && weapon.LoadedAmmo == remaining && reserve.Quantity == 6, $"El quickhack modificó los cartuchos de {code}.");
            Require(combat.Preview(CombatAction.WeaponAttack) is { Available: true, AmmoCost: 1 }
                && combat.TakeTurn(CombatAction.WeaponAttack).PlayerAction is { Hit: false, AmmoSpent: 1 }
                && weapon.LoadedAmmo == remaining - 1 && reserve.Quantity == 6, $"El disparo no descontó un solo cartucho de {code}.");
        }
        Require(!combat.Preview(CombatAction.WeaponAttack).Available
            && !combat.TakeTurn(CombatAction.WeaponAttack).PlayerAction.ConsumedAction
            && combat.TakeTurn(CombatAction.Reload).PlayerAction.Reloaded == capacity
            && weapon.LoadedAmmo == capacity
            && player.Inventory.Items.OfType<Consumable>().Where(item => item.CatalogItemId == reserve.CatalogItemId)
                .Sum(item => item.Quantity) == 6 - capacity, $"Cargador vacío o recarga incorrectos: {code}.");
    }
}

void CheckCombatHealing()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Street, catalog);
    var player = state.Player!;
    var healing = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Healing);
    var random = new CountingRandom(0);
    var combat = new CombatController(player, DevelopmentContent.MaelstromTest,
        DevelopmentContent.BasicAttack, random);
    var rejected = combat.TakeTurn(CombatAction.Heal);
    Require(!rejected.PlayerAction.Valid && rejected.EnemyAction is null
        && healing.Quantity == 2 && random.Calls == 0, "Curación a salud completa gastó recursos.");
    combat.TakeTurn(CombatAction.Wait);
    Require(player.CurrentHealth == 94 && combat.Preview(CombatAction.Heal).HealAmount == 6,
        "Previsión de curación no respeta máximo.");
    var turn = combat.TakeTurn(CombatAction.Heal);
    Require(turn.PlayerAction is { Valid: true, Healed: 6, ConsumedAction: true }
        && turn.EnemyAction is { ConsumedAction: true }
        && healing.Quantity == 1 && player.CurrentHealth == 94
        && random.Calls == 2, "Curar gastó tirada extra o aplicó salud incorrecta.");
    combat.TakeTurn(CombatAction.Heal);
    Require(!player.Inventory.Contains(healing) && player.CurrentHealth == 94,
        "No se agotó la pila de curación.");
    var final = combat.TakeTurn(CombatAction.Heal);
    Require(!final.PlayerAction.Valid && final.EnemyAction is null,
        "Curación agotada gastó turno.");
}

void CheckPreparationHealing()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var healing = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Healing);
    Require(!player.TryUseHealing(healing, out _, out _) && healing.Quantity == 2,
        "Curación sin herida consumió pila.");
    player.ReceiveDamage(5);
    Require(player.TryUseHealing(healing, out var healed, out _)
        && healed == 5 && player.CurrentHealth == 100 && healing.Quantity == 1,
        "Curación de preparación no respetó máximo.");
    Require(!player.TryUseHealing(healing, out _, out _) && healing.Quantity == 1,
        "Curación a salud completa consumió segunda unidad.");
    player.ReceiveDamage(100);
    Require(!player.TryUseHealing(healing, out _, out _) && healing.Quantity == 1,
        "Se curó a personaje muerto.");
    Throws<InvalidOperationException>(() => player.Heal(1));
}

void CheckPreview()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Nomad, catalog);
    var player = state.Player!;
    var weapon = player.Equipment.Weapons.Single();
    var random = new CountingRandom(0.99);
    var combat = new CombatController(player, DevelopmentContent.MaelstromTest,
        DevelopmentContent.BasicAttack, random);
    var inventory = player.Inventory.Items.ToArray();
    var enemyHealth = combat.Enemy.CurrentHealth;
    for (var i = 0; i < 5; i++)
    {
        var preview = combat.Preview(CombatAction.WeaponAttack);
        Require(preview is { Available: true, Damage: 18, Accuracy: 0.75m, AmmoCost: 1 },
            "Previsión no usa definición del arma.");
        Require(combat.EnemyDamageEstimate == "1–10"
            && combat.EnemyMissChance == 0.25m
            && combat.Preview(CombatAction.BasicAttack).Damage == 6,
            "La previsión básica o enemiga es incorrecta.");
    }
    Require(random.Calls == 0 && combat.Enemy.CurrentHealth == enemyHealth
        && player.CurrentHealth == 100 && weapon.LoadedAmmo == 6
        && player.Inventory.Items.SequenceEqual(inventory),
        "Consultar previsiones gastó tiradas o recursos.");
    var result = combat.TakeTurn(CombatAction.WeaponAttack);
    Require(result.PlayerAction.Hit == false && random.Calls == 2,
        "La ejecución después de consultar modificó la precisión.");
}

void CheckDeathRecreation()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Nomad, catalog);
    var old = state.Player!;
    var oldWeapon = old.Equipment.Weapons.Single();
    Throws<InvalidOperationException>(() => state.RemoveDeadCharacter(old));
    old.ReceiveDamage(100);
    state.RemoveDeadCharacter(old);
    Require(state.Player is null, "La sesión conserva personaje muerto.");
    state.CreateCharacter(OriginPath.Street, catalog);
    Require(state.Player is { Origin: OriginPath.Street }
        && state.Player.Equipment.Weapons.Single().RuntimeId != oldWeapon.RuntimeId
        && state.Player.Inventory.Items.Count == 3
        && state.Player.CurrentHealth == 100, "Recreación conservó pertenencias o salud.");
    Throws<InvalidOperationException>(() => state.RemoveDeadCharacter(old));
}

Expedition ExpeditionFor(GameState state, params EventOccurrence[] events) =>
    new(ExpeditionContent.Watson, state.Player!, events);

EventOccurrence Discovery(ItemDefinition definition, int quantity = 2) =>
    new(EventKind.Discovery, new[] { new LootEntry(definition, quantity) });

void CheckZoneGeneration()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Nomad, catalog);
    foreach (var zone in ExpeditionContent.Zones)
    {
        var merchants = 0;
        for (var seed = 0; seed < 300; seed++)
        {
            var adventure = ExpeditionContent.Create(zone, new Random(seed), catalog, state.Player!);
            var count = adventure.Events.Count(e => e.Kind == EventKind.Merchant);
            var expected = new Random(seed).Next(3) == 0 ? 1 : 0;
            merchants += count;
            Require(count == expected && adventure.Events.Count == 5
                && adventure.Events.Count(e => e.Kind == EventKind.Combat) >= 3
                && adventure.Events.All(e => e.Kind == EventKind.Merchant
                    ? e.Offers.Count > 0 && e.Loot.Count == 0 : e.Loot.Count > 0)
                && adventure.Events.Where(e => e.Kind == EventKind.Combat).All(e =>
                    e.Enemy!.Affiliation == zone.Affiliation
                    && e.Enemy.Level >= zone.MinLevel && e.Enemy.Level <= zone.MaxLevel),
                $"Pool inválido: {zone.Name}.");
            if (count == 1)
            {
                var offers = adventure.Events.Single(e => e.Kind == EventKind.Merchant).Offers;
                var weapon = offers.Single(o => o.Definition is WeaponDefinition);
                var hack = offers.Single(o => o.Definition is CybergearStashDefinition);
                var tier = weapon.Definition.Tier;
                var action = ((CybergearStashDefinition)hack.Definition).Quickhack!.Value;
                var repeat = ExpeditionContent.Create(zone, new Random(seed), catalog, state.Player!)
                    .Events.Single(e => e.Kind == EventKind.Merchant).Offers;
                Require((zone == ExpeditionContent.Dogtown ? tier is 4 or 5
                        : tier == (zone == ExpeditionContent.Japantown ? 3 : 2))
                    && weapon is { Stock: 1 } && weapon.UnitPrice == 15 * tier - 5
                    && hack.Stock == 1 && hack.UnitPrice == QuickhackContent.Find(action)!.Price * 4 / 5
                    && offers.Select(o => o.Definition).SequenceEqual(repeat.Select(o => o.Definition)),
                    $"Ofertas especiales inválidas: {zone.Name}.");
            }
            var dogtown = zone == ExpeditionContent.Dogtown ? 1 : 0;
            Require(adventure.Events.Count(e => e.Enemy is { IsBot: true, Netrunner: false }) == dogtown
                && adventure.Events.Count(e => e.Enemy is { IsBot: false, Netrunner: true }) == dogtown,
                "Dogtown perdió al bot o al netrunner, o aparecieron fuera de la zona.");
            Require(adventure.Events.SelectMany(e => e.Loot).Distinct().Count() ==
                adventure.Events.Sum(e => e.Loot.Count), "Eventos comparten botín mutable.");
        }
        Require(merchants is > 75 and < 125, "Frecuencia de comerciante alejada de 1/3.");
        var expedition = ExpeditionContent.Create(zone, new Random(0), catalog, state.Player!);
        state.StartExpedition(expedition);
        while (expedition.Status == ExpeditionStatus.Active)
        {
            expedition.ResolveCurrent();
            expedition.Advance();
        }
        state.CompleteExpedition(expedition);
    }
}

void CheckMerchant()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Street, catalog);
    var player = state.Player!;
    var healing = (ConsumableDefinition)catalog.Get("curacion");
    var offer = new MerchantOffer(healing, 5, 2);
    var exp = ExpeditionFor(state,
        new EventOccurrence(EventKind.Merchant, [], merchant: [offer]),
        Discovery(healing), Discovery(healing),
        new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest),
        Discovery(healing));
    var stack = player.Inventory.Items.OfType<Consumable>().Single(item => item.Type == ConsumableType.Healing);
    Require(!exp.TryBuy(offer, 1, out _) && player.Money == 0
        && offer.Stock == 2 && stack.Quantity == 2, "Compró sin fondos.");
    exp.ResolveCurrent();
    Require(exp.LastExperienceReward is null, "El comerciante concedió XP.");
    exp.Advance();
    exp.ResolveCurrent();
    Throws<InvalidOperationException>(() => exp.ResolveCurrent());
    Require(player.Money == 8 && !exp.TryBuy(offer, 1, out _), "Duplicó dinero o aceptó oferta ajena.");

    offer = new MerchantOffer(healing, 5, 2);
    var shop = ExpeditionFor(state, new EventOccurrence(EventKind.Merchant, [], merchant: [offer]),
        Discovery(healing), Discovery(healing), Discovery(healing), Discovery(healing));
    Require(!shop.TryBuy(offer, 0, out _) && !shop.TryBuy(offer, 3, out _)
        && !shop.TryBuy(offer, 2, out _) && offer.Stock == 2 && player.Money == 8
        && stack.Quantity == 2, "Un rechazo alteró recursos.");
    Require(shop.TryBuy(offer, 1, out _) && player.Money == 3 && offer.Stock == 1
        && stack.Quantity == 3, "No aplicó la compra.");
    shop.ResolveCurrent();
    Require(!shop.TryBuy(offer, 1, out _), "Aceptó una compra tras cerrar el evento.");

    var reserve = player.Inventory.Items.OfType<Consumable>().Single(item => item.Type == ConsumableType.Ammunition);
    Require(player.TryDiscard(reserve, reserve.Quantity, out _), "No se pudo preparar la pila llena.");
    player.Inventory.Add(factory.CreateConsumable(reserve.CatalogItemId, int.MaxValue));
    var fullStackOffer = new MerchantOffer(reserve.ConsumableDefinition, 1, 1);
    var fullStackShop = ExpeditionFor(state,
        new EventOccurrence(EventKind.Merchant, [], merchant: [fullStackOffer]),
        Discovery(healing), Discovery(healing), Discovery(healing), Discovery(healing));
    Require(!fullStackShop.TryBuy(fullStackOffer, 1, out _)
        && player.Money == 3 && fullStackOffer.Stock == 1, "Cobró al desbordar la pila.");
    var premiumWeapon = new MerchantOffer(catalog.Get("pistola-t2"), 25, 1);
    var overheat = catalog.Get("sobrecalentamiento");
    var specialHack = new MerchantOffer(overheat, 20, 1);
    var premiumShop = ExpeditionFor(state, new EventOccurrence(EventKind.Merchant, [],
        merchant: [premiumWeapon, specialHack]), Discovery(healing), Discovery(healing), Discovery(healing), Discovery(healing));
    Require(!premiumShop.TryBuy(premiumWeapon, 1, out _) && premiumWeapon.Stock == 1 && player.Money == 3,
        "La compra sin fondos agotó el arma especial.");
    var earnings = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
        new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
    do { earnings.ResolveCurrent(); } while (earnings.Advance());
    Require(premiumShop.TryBuy(premiumWeapon, 1, out _) && premiumWeapon.Stock == 0
        && player.Inventory.Items.OfType<Weapon>().Any(weapon => weapon.Definition == premiumWeapon.Definition && weapon.LoadedAmmo == 0)
        && player.Equipment.Weapons.Count == 1 && !premiumShop.TryBuy(premiumWeapon, 1, out _)
        && player.Money == 53, "La compra del arma especial no respetó el stock, precio o cargador.");
    Require(premiumShop.TryBuy(specialHack, 1, out _) && specialHack.Stock == 0 && player.Money == 33
        && player.OwnsQuickhack(CombatAction.Overheat) && !player.IsQuickhackAssigned(CombatAction.Overheat)
        && player.TryConfigureQuickhack(CombatAction.Overheat, true, out _)
        && Combat(state, 0.99).TakeTurn(CombatAction.Overheat).PlayerAction is { Valid: true, RamSpent: 4 },
        "El quickhack comprado no quedó disponible para configurar y usar.");
    var duplicate = new MerchantOffer(overheat, 20, 1);
    var nextShop = ExpeditionFor(state, new EventOccurrence(EventKind.Merchant, [], merchant: [duplicate]),
        Discovery(healing), Discovery(healing), Discovery(healing), Discovery(healing));
    Require(!nextShop.TryBuy(duplicate, 1, out _) && duplicate.Stock == 1 && player.Money == 33,
        "Una compra duplicada gastó fondos o stock.");
    player.ReceiveDamage(player.CurrentHealth);
    state.RemoveDeadCharacter(player);
    state.CreateCharacter(OriginPath.Street, catalog);
    Require(state.Player!.Money == 0, "El personaje nuevo heredó dinero.");
}

void CheckWorkshop()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var weapon = player.Equipment.Weapons.Single();
    var ammo = player.Inventory.Items.OfType<Consumable>().Single(item => item.Type == ConsumableType.Ammunition);
    var healing = player.Inventory.Items.OfType<Consumable>().Single(item => item.Type == ConsumableType.Healing);
    var ammoRecipe = state.CraftingRecipes.Single(recipe => recipe.Output == ammo.Definition);
    var healingRecipe = state.CraftingRecipes.Single(recipe => recipe.Output == healing.Definition);
    Require(!state.TryCraft(ammoRecipe, 1, out _) && !state.TryDisassemble(ammo, 1, out _)
        && !state.TryDisassemble(factory.CreateWeapon("pistola-t1"), 1, out _)
        && !state.TryDisassemble(weapon, 2, out _) && player.Components == 0
        && ammo.Quantity == 12 && player.Equipment.Weapons.Contains(weapon),
        "Una operación inválida alteró el personaje.");
    Require(state.TryDisassemble(weapon, 1, out _) && state.TryDisassemble(healing, 1, out _)
        && player.Components == 5 && !player.Inventory.Contains(weapon)
        && player.Equipment.Weapons.Count == 0 && ammo.Quantity == 12 && healing.Quantity == 1,
        "El desarme no respetó pila, equipo o reserva.");
    Require(!state.TryCraft(ammoRecipe with { ComponentCost = 0 }, 1, out _)
        && state.TryCraft(ammoRecipe, 2, out _) && state.TryCraft(healingRecipe, 1, out _)
        && ammo.Quantity == 24 && healing.Quantity == 2 && player.Components == 1 && player.Money == 0,
        "La fabricación no respetó las recetas, lotes o componentes.");
    Require(player.TryDiscard(ammo, ammo.Quantity, out _), "No se pudo preparar la pila llena.");
    var full = factory.CreateConsumable(ammo.CatalogItemId, int.MaxValue);
    player.Inventory.Add(full);
    Require(!state.TryCraft(ammoRecipe, 1, out _) && full.Quantity == int.MaxValue && player.Components == 1,
        "La fabricación cobró por un desbordamiento.");
    Require(player.TryDiscard(full, full.Quantity, out _), "No se pudo aligerar la carga de prueba.");
    var expedition = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ => Discovery(healing.Definition)).ToArray());
    state.StartExpedition(expedition);
    Require(!state.TryDisassemble(healing, 1, out _) && !state.TryCraft(ammoRecipe, 1, out _)
        && healing.Quantity == 2 && player.Components == 1, "El taller operó durante una aventura.");
    state.WithdrawExpedition(expedition);
    Require(player.Components == 1, "La retirada eliminó componentes.");
    player.ReceiveDamage(player.CurrentHealth);
    state.RemoveDeadCharacter(player);
    state.CreateCharacter(OriginPath.Corpo, catalog);
    Require(state.Player!.Components == 0, "El nuevo personaje heredó componentes.");
}

void CheckWeaponUpgrade()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var weapon = player.Equipment.Weapons.Single();
    var id = weapon.RuntimeId;
    Combat(state, 0).TakeTurn(CombatAction.WeaponAttack);
    var loaded = weapon.LoadedAmmo;
    Require(!state.TryUpgradeWeapon(weapon, out _) && weapon.Definition.Tier == 1,
        "La mejora sin componentes alteró el arma.");
    var factory = new ItemFactory(catalog);
    for (var i = 0; i < 7; i++)
    {
        var spare = factory.CreateWeapon("pistola-t1");
        player.Inventory.Add(spare);
        Require(state.TryDisassemble(spare, 1, out _), "No se pudieron preparar los componentes.");
    }
    var expedition = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
        Discovery(catalog.Get("curacion"))).ToArray());
    state.StartExpedition(expedition);
    Require(!state.TryUpgradeWeapon(weapon, out _) && player.Components == 28, "El taller operó durante una aventura.");
    do { expedition.ResolveCurrent(); } while (expedition.Advance());
    state.CompleteExpedition(expedition);
    var balance = player.Money;
    for (var tier = 2; tier <= 5; tier++)
    {
        Require(state.PreviewWeaponUpgrade(weapon) is { } preview && preview.Next.Tier == tier
            && preview.Cost == tier * 2, "Siguiente tier o coste incorrecto.");
        Require(state.TryUpgradeWeapon(weapon, out _) && weapon.Definition.Tier == tier
            && weapon.RuntimeId == id && ReferenceEquals(player.Equipment.Weapons.Single(), weapon)
            && weapon.LoadedAmmo == loaded && weapon.WeaponDefinition.Damage == 12 + 2 * (tier - 1)
            && weapon.MagazineCapacity == 12 && weapon.WeaponDefinition.Accuracy == 0.85m,
            "Cambió la instancia, munición o las estadísticas incorrectas.");
    }
    Require(!state.TryUpgradeWeapon(weapon, out _) && player.Components == 0 && player.Money == balance,
        "El Tier 5 avanzó o se cobró dinero.");
}

void CheckWeaponCrafting()
{
    WithDatabase((store, catalog, path) =>
    {
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, catalog);
        var player = state.Player!;
        var recipes = state.CraftingRecipes.Where(recipe => recipe.Output is WeaponDefinition).ToArray();
        Require(recipes.Take(3).Select(recipe => recipe.ComponentCost).SequenceEqual([8, 10, 12])
            && recipes.All(recipe => recipe.Output.Tier == 1 && recipe.Units == 1),
            "Las recetas de armas no coinciden con los costes y tiers del Taller.");
        var pistolRecipe = recipes[0];
        Require(!state.TryCraft(pistolRecipe, 1, out _) && player.Equipment.Weapons.Count == 1,
            "Se fabricó sin materiales.");
        var id = ComponentDefinition.Workshop.CatalogItemId;
        store.Deposit(id, 16);
        var stash = new StashService(store, catalog);
        Require(stash.TryWithdraw(player, id, 16, out _), "No se prepararon los componentes.");
        var expedition = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ => Discovery(catalog.Get("curacion"))).ToArray());
        state.StartExpedition(expedition);
        Require(!state.TryCraft(pistolRecipe, 1, out _) && player.Components == 16,
            "El taller fabricó armas en plena aventura.");
        state.WithdrawExpedition(expedition);
        Require(!state.TryCraft(pistolRecipe, 0, out _) && !state.TryCraft(pistolRecipe, 2, out _)
            && !state.TryCraft(pistolRecipe with { ComponentCost = 1 }, 1, out _)
            && player.Components == 16 && state.TryCraft(pistolRecipe, 1, out _)
            && state.TryCraft(pistolRecipe, 1, out _) && player.Components == 0 && player.Money == 0,
            "Una receta o cantidad inválida gastó componentes, o no se fabricaron las dos armas.");
        var weapons = player.Inventory.Items.OfType<Weapon>().ToArray();
        Require(weapons.Length == 2 && weapons[0].RuntimeId != weapons[1].RuntimeId
            && weapons.All(weapon => weapon is { LoadedAmmo: 0, HasAccessories: false } && weapon.Definition == pistolRecipe.Output)
            && player.Equipment.Weapons.Count == 0,
            "Las armas no se crearon como instancias vacías y sin equipar.");
        Require(player.Equipment.TryEquip(weapons[0], out _) && player.TryReload(weapons[0], out var loaded, out _)
            && loaded == 12 && weapons[1].LoadedAmmo == 0 && stash.TryDeposit(player, weapons[1], 1, out _),
            "El arma fabricada no se pudo usar o guardar, o compartió el cargador.");
    });
}

void CheckNewFirearms()
{
    WithDatabase((store, original, path) =>
    {
        store.Deposit(104, 2);
        store.Deposit(303, 100);
        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            RemoveNewFirearms(connection);
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 8";
            command.ExecuteNonQuery();
        }
        var notices = new List<string>();
        var upgraded = store.LoadCatalog(notices.Add);
        Require(notices.Single().Contains(".v8-") && store.ReadStash(upgraded)[104] == 2, "Migración sin respaldo o sin depósitos.");
        var stash = new StashService(store, upgraded);
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, upgraded);
        var player = state.Player!;
        Require(stash.TryWithdraw(player, 303, 100, out _), "No se pudieron preparar componentes.");
        foreach (var (code, technology, sight, magazine) in new[]
        {
            ("revolver-t1", "Power", false, false), ("escopeta-doble-t1", "Power", false, false),
            ("rifle-asalto-t1", "Smart", true, true), ("rifle-precision-t1", "Tech", true, false)
        })
        {
            var definition = (WeaponDefinition)upgraded.Get(code);
            var recipe = state.CraftingRecipes.Single(r => r.Output == definition);
            Require(definition.Technology == technology && state.TryCraft(recipe, 1, out _), $"Etiqueta o receta inválida: {code}.");
            var weapon = player.Inventory.Items.OfType<Weapon>().Single(w => w.Definition == definition);
            var ammoRecipe = state.CraftingRecipes.Single(r => r.Output.CatalogItemId == definition.AmmoCatalogItemId);
            Require(state.TryCraft(ammoRecipe, 2, out _) && player.Equipment.TryEquip(weapon, out _)
                && player.TryReload(weapon, out _, out _), $"El arma no se pudo equipar o recargar: {code}.");
            var fight = Combat(state, 0);
            Require(fight.TrySelectWeapon(weapon, out _) && fight.TakeTurn(CombatAction.WeaponAttack).PlayerAction is
                { Hit: true } hit && hit.AmmoSpent == definition.AmmoPerAttack, $"Consumo de munición incorrecto: {code}.");
            Require(state.TryCraftExtendedMagazine(out _) && state.TryCraftReflexSight(out _)
                && state.TryChangeExtendedMagazine(weapon, true, out _) == magazine
                && state.TryChangeReflexSight(weapon, true, out _) == sight, $"Compatibilidad incorrecta: {code}.");
            Require(state.TryUpgradeWeapon(weapon, out _) && weapon.Definition.Tier == 2
                && stash.TryDeposit(player, weapon, 1, out _) && store.ReadStash(upgraded)[weapon.CatalogItemId] == 1,
                $"El modelo no se pudo mejorar y guardar: {code}.");
        }
    });
}

void CheckMeleeWeapons()
{
    WithDatabase((store, original, path) =>
    {
        store.Deposit(105, 5);
        store.Deposit(303, 50);
        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            RemoveMeleeWeapons(connection);
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 9";
            command.ExecuteNonQuery();
        }
        var notices = new List<string>();
        var upgraded = store.LoadCatalog(notices.Add);
        var stash = new StashService(store, upgraded);
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, upgraded);
        var player = state.Player!;
        Require(notices.Single().Contains(".v9-") && store.ReadStash(upgraded)[105] == 5
            && stash.TryWithdraw(player, 303, 50, out _), "La migración no conservó el stash.");
        foreach (var code in new[] { "espada-t1", "bate-t1" })
        {
            var definition = (WeaponDefinition)upgraded.Get(code);
            Require(state.TryCraft(state.CraftingRecipes.Single(r => r.Output == definition), 1, out _), "Falló la receta.");
            var weapon = player.Inventory.Items.OfType<Weapon>().Single(w => w.Definition == definition);
            Require(player.Equipment.TryEquip(weapon, out _) && !player.TryReload(weapon, out _, out _)
                && !state.TryChangeExtendedMagazine(weapon, true, out _) && !state.TryChangeReflexSight(weapon, true, out _),
                $"Se permitió recargar o montar accesorios en {code}.");
            var fight = new CombatController(player, DevelopmentContent.MaelstromTest,
                DevelopmentContent.BasicAttack, new CriticalRandom(0, 0));
            Require(fight.TrySelectWeapon(weapon, out _) && fight.Preview(CombatAction.BasicAttack).Damage == 5
                && fight.TakeTurn(CombatAction.WeaponAttack).PlayerAction is { Critical: true, AmmoSpent: 0, StaminaSpent: 5 } hit
                && hit.Damage?.Incoming == definition.Damage * 3 / 2 && weapon.LoadedAmmo == 0,
                $"El ataque de {code} no respetó la stamina, el crítico o el golpe básico.");
            Require(state.TryUpgradeWeapon(weapon, out _) && weapon.Definition.Tier == 2
                && stash.TryDeposit(player, weapon, 1, out _) && stash.TryWithdraw(player, weapon.CatalogItemId, 1, out _),
                $"No se pudo mejorar y transferir {code}.");
        }
    });
}

void CheckQuickhackCrafting()
{
    WithDatabase((store, catalog, path) =>
    {
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, catalog);
        var player = state.Player!;
        var recipes = state.CraftingRecipes.Where(recipe => recipe.Output is CybergearStashDefinition).ToArray();
        Require(recipes.Select(r => r.ComponentCost).SequenceEqual([4, 6, 8, 8, 10, 16])
            && recipes.All(recipe => recipe.Units == 1), "La tabla de recetas no coincide con los costes acordados.");
        var overheat = recipes.Single(r => r.Output is CybergearStashDefinition { Quickhack: CombatAction.Overheat });
        var shortCircuit = recipes.Single(r => r.Output is CybergearStashDefinition { Quickhack: CombatAction.ShortCircuit });
        Require(!state.TryCraft(overheat, 1, out _) && !player.OwnsQuickhack(CombatAction.Overheat),
            "Fabricó sin suficientes componentes.");
        var stash = new StashService(store, catalog);
        var materials = ComponentDefinition.Workshop.CatalogItemId;
        store.Deposit(materials, 20);
        Require(stash.TryWithdraw(player, materials, 20, out _), "No se pudieron preparar materiales.");
        var trip = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ => Discovery(catalog.Get("curacion"))).ToArray());
        state.StartExpedition(trip);
        Require(!state.TryCraft(overheat, 1, out _) && player.Components == 20, "El taller fabricó durante la aventura.");
        do { trip.ResolveCurrent(); } while (trip.Advance());
        state.CompleteExpedition(trip);
        var money = player.Money;
        Require(!state.TryCraft(overheat, 2, out _) && !state.TryCraft(shortCircuit, 1, out _)
            && state.TryExtractImplant(CyberwareZone.OperatingSystem, out _)
            && !state.TryCraft(shortCircuit, 1, out _) && player.Components == 20
            && state.TryCraft(overheat, 1, out _) && player.Money == money - CyberwareContent.ProcedureFee
            && player.Components == 14 && player.OwnsQuickhack(CombatAction.Overheat)
            && !player.IsQuickhackAssigned(CombatAction.Overheat)
            && !state.TryCraft(overheat, 1, out _), "La fabricación duplicó, equipó o cobró indebidamente un quickhack.");
        Require(stash.TryDepositQuickhack(player, CombatAction.Overheat, out _)
            && !player.OwnsQuickhack(CombatAction.Overheat)
            && state.TryBuyQuickhack(CombatAction.Overheat, out _), "Fabricar bloqueó comprar después de depositar.");
        var reopened = new SqliteGameStore(path);
        var restored = reopened.LoadCatalog();
        var newState = new GameState();
        newState.CreateCharacter(OriginPath.Nomad, restored);
        var recipient = newState.Player!;
        Require(new StashService(reopened, restored).TryWithdraw(recipient, overheat.Output.CatalogItemId, 1, out _)
            && recipient.OwnsQuickhack(CombatAction.Overheat) && !recipient.IsQuickhackAssigned(CombatAction.Overheat),
            "El quickhack fabricado no sobrevivió al cierre como objeto sin asignar.");
    });
}

void CheckImplantUpgrade()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var factory = new ItemFactory(catalog);
    for (var i = 0; i < 8; i++)
    {
        var spare = factory.CreateWeapon("pistola-t1");
        player.Inventory.Add(spare);
        Require(state.TryDisassemble(spare, 1, out _), "No se pudieron preparar componentes.");
    }
    var expedition = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
        new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
    var originalDeck = player.Cyberdeck!;
    state.StartExpedition(expedition);
    Require(!state.TryUpgradeImplant(originalDeck, out _) && player.Components == 32,
        "El taller mejoró un implante durante la aventura.");
    do { expedition.ResolveCurrent(); } while (expedition.Advance());
    state.CompleteExpedition(expedition);
    Require(state.TryBuyExDisk(out _) && state.TryInstallImplant(player.SpareCyberware.Single(), out _),
        "No se pudo preparar Ex-Disk.");
    var money = player.Money;
    player.SpendRam(3);
    var ramBeforeUpgrade = player.Ram;
    Require(!state.TryUpgradeImplant(originalDeck with { }, out _), "El taller aceptó un implante ajeno.");
    for (var tier = 2; tier <= 5; tier++)
    {
        Require(state.PreviewImplantUpgrade(player.Cyberdeck!) is { } upgrade
            && upgrade.Next.Tier == tier && upgrade.Cost == 2 * tier && upgrade.BlockedReason is null
            && state.TryUpgradeImplant(player.Cyberdeck!, out _)
            && player.Cyberdeck!.Tier == tier && player.MaxRam == 8 + tier - 1
            && player.UsedCyberwareCapacity == 12 + 2 * (tier - 1) && player.Ram == ramBeforeUpgrade
            && player.IsQuickhackEquipped(CombatAction.ShortCircuit),
            "La mejora alteró la RAM actual, el quickhack, el tier o la capacidad.");
    }
    var exDisk = player.InstalledCyberware[CyberwareZone.FrontalCortex];
    Require(!state.TryUpgradeImplant(player.Cyberdeck!, out _)
        && state.PreviewImplantUpgrade(exDisk) is { BlockedReason: not null }
        && !state.TryUpgradeImplant(exDisk, out _) && player.Components == 4 && player.Money == money,
        "Tier 5, capacidad o dinero no se respetaron.");
    Require(state.TryExtractImplant(CyberwareZone.FrontalCortex, out _)
        && state.TryUpgradeImplant(exDisk, out _) && player.Components == 0, "No se pudo mejorar Ex-Disk suelto.");
    var upgraded = player.SpareCyberware.Single();
    Require(upgraded is { Tier: 2, BonusRam: 3, CapacityCost: 5 }
        && !state.TryInstallImplant(upgraded, out _)
        && state.TryExtractImplant(CyberwareZone.OperatingSystem, out _)
        && state.TryInstallImplant(upgraded, out _)
        && ReferenceEquals(player.InstalledCyberware[CyberwareZone.FrontalCortex], upgraded)
        && player.Ram == 0 && player.MaxRam == 0, "La instalación perdió el tier o ignoró la capacidad.");
}

void CheckExtendedMagazine()
{
    WithDatabase((store, persisted, path) =>
    {
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, persisted);
        var player = state.Player!;
        var weapon = player.Equipment.Weapons.Single();
        var factory = new ItemFactory(persisted);
        var another = factory.CreateWeapon("pistola-t1");
        var incompatible = factory.CreateWeapon("subfusil-t1");
        player.Inventory.Add(another);
        player.Inventory.Add(incompatible);
        Require(!state.TryCraftExtendedMagazine(out _)
            && state.TryDisassemble(incompatible, 1, out _)
            && state.TryCraftExtendedMagazine(out _) && player.Components == 1
            && player.SpareExtendedMagazines == 1, "Fabricación sin coste o sin materiales incorrecta.");
        incompatible = factory.CreateWeapon("subfusil-t1");
        player.Inventory.Add(incompatible);
        Require(!state.TryChangeExtendedMagazine(incompatible, true, out _)
            && !state.TryChangeExtendedMagazine(factory.CreateWeapon("pistola-t1"), true, out _),
            "Se modificó un arma ajena o incompatible.");

        var expedition = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ => Discovery(persisted.Get("curacion"))).ToArray());
        state.StartExpedition(expedition);
        Require(state.TryChangeExtendedMagazine(weapon, true, out _) && weapon.MagazineCapacity == 16
            && weapon.LoadedAmmo == 12 && another.MagazineCapacity == 12
            && !state.TryChangeExtendedMagazine(weapon, true, out _) && player.SpareExtendedMagazines == 0,
            "El montaje en aventura rellenó munición o cambió otra arma.");
        var combat = Combat(state, 0.99);
        Require(combat.TakeTurn(CombatAction.Reload).PlayerAction.Reloaded == 4 && weapon.LoadedAmmo == 16,
            "La recarga no usó la capacidad ampliada.");
        var stash = new StashService(store, persisted);
        var ammo = player.Inventory.Items.OfType<Consumable>().Single(stack => stack.Type == ConsumableType.Ammunition);
        Require(ammo.Quantity == 8 && state.TryChangeExtendedMagazine(weapon, false, out _)
            && weapon.LoadedAmmo == 12 && ammo.Quantity == 12 && player.SpareExtendedMagazines == 1,
            "El desmontaje perdió o duplicó balas.");
        Require(state.TryChangeExtendedMagazine(weapon, true, out _)
            && combat.TakeTurn(CombatAction.Reload).PlayerAction.Reloaded == 4
            && player.TryDiscard(ammo, ammo.Quantity, out _), "No se pudo preparar la prueba de exceso.");
        var full = factory.CreateConsumable(ammo.CatalogItemId, int.MaxValue);
        player.Inventory.Add(full);
        Require(!state.TryChangeExtendedMagazine(weapon, false, out _) && full.Quantity == int.MaxValue
            && weapon.LoadedAmmo == 16 && weapon.HasExtendedMagazine && player.SpareExtendedMagazines == 0,
            "Un rechazo por reserva llena alteró la carga o el accesorio.");
    });
}

void CheckReflexSight()
{
    WithDatabase((store, persisted, path) =>
    {
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, persisted);
        var player = state.Player!;
        var pistol = player.Equipment.Weapons.Single();
        var factory = new ItemFactory(persisted);
        var second = factory.CreateWeapon("pistola-t1", loaded: true);
        var smg = factory.CreateWeapon("subfusil-t1", loaded: true);
        player.Inventory.Add(second);
        player.Inventory.Add(smg);
        Require(!state.TryCraftReflexSight(out _)
            && state.TryDisassemble(factory.CreateWeapon("pistola-t1"), 1, out _) == false,
            "El taller cobró sin materiales o desarmó un arma ajena.");
        for (var i = 0; i < 2; i++)
        {
            var spare = factory.CreateWeapon("pistola-t1");
            player.Inventory.Add(spare);
            Require(state.TryDisassemble(spare, 1, out _), "No se pudo preparar materiales.");
        }
        Require(state.TryCraftReflexSight(out _) && state.TryCraftExtendedMagazine(out _)
            && player.Components == 2 && !state.TryChangeReflexSight(smg, true, out _)
            && !state.TryChangeReflexSight(factory.CreateWeapon("pistola-t1"), true, out _)
            && state.TryChangeExtendedMagazine(pistol, true, out _)
            && state.TryChangeReflexSight(pistol, true, out _)
            && !state.TryChangeReflexSight(pistol, true, out _)
            && pistol.MagazineCapacity == 16 && pistol.LoadedAmmo == 12 && !second.HasAccessories,
            "El montaje no respetó compatibilidad, coste u otras instancias.");
        Require(player.TryUnlockSkill("pulso", out _), "No se pudo preparar el bono de habilidad.");
        var combat = Combat(state, 0.92);
        Require(combat.Preview(CombatAction.WeaponAttack) is { Accuracy: 0.9425m, Damage: 12 }
            && combat.TakeTurn(CombatAction.WeaponAttack).PlayerAction.Hit == true,
            "La vista previa y el disparo no aplicaron los mismos cinco puntos de acierto.");
        Require(state.TryChangeExtendedMagazine(pistol, false, out _), "No se pudo quitar el cargador.");
        Require(state.TryChangeReflexSight(pistol, false, out _) && player.SpareReflexSights == 1
            && pistol.LoadedAmmo == 11 && combat.Preview(CombatAction.WeaponAttack).Accuracy == 0.8925m,
            "Quitar la mira alteró la munición o dejó la bonificación activa.");
    });
}

void CheckShop()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var offer = state.ShopOffers.Single(entry => entry.Definition is ConsumableDefinition { Type: ConsumableType.Healing });
    var stack = player.Inventory.Items.OfType<Consumable>().Single(item => item.Type == ConsumableType.Healing);
    Require(!state.TryBuyFromShop(offer, 1, out _) && offer.Stock == 4 && stack.Quantity == 2,
        "Una compra sin fondos cambió la tienda o el inventario.");
    var foreign = factory.CreateConsumable(stack.CatalogItemId, 1);
    Require(!state.TrySellToShop(foreign, 1, out _) && !state.TrySellToShop(stack, 3, out _)
        && state.TrySellToShop(stack, 1, out _) && player.Money == 5 && stack.Quantity == 1
        && offer.Stock == 4, "La venta no respetó pertenencia, cantidad o precio.");

    void FinishExpedition(Action? beforeReturning = null)
    {
        var exp = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
            new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
        state.StartExpedition(exp);
        do
        {
            exp.ResolveCurrent();
        } while (exp.Advance());
        beforeReturning?.Invoke();
        state.CompleteExpedition(exp);
    }

    FinishExpedition();
    var unrelatedOffer = new MerchantOffer(offer.Definition, 1, 9);
    Require(!state.TryBuyFromShop(unrelatedOffer, 1, out _)
        && state.TryBuyFromShop(offer, 4, out _) && player.Money == 32
        && offer.Stock == 0 && stack.Quantity == 5 && !state.TryBuyFromShop(offer, 1, out _),
        "La compra no respetó pertenencia de la oferta, precio, pila o stock.");
    Require(state.TrySellToShop(stack, 5, out _) && player.Money == 57
        && !player.Inventory.Contains(stack) && offer.Stock == 0,
        "La venta total no retiró la pila o repuso stock antes de tiempo.");
    FinishExpedition(() =>
    {
        var ammoOffer = state.ShopOffers.First(entry => entry.Definition is ConsumableDefinition { Type: ConsumableType.Ammunition });
        var ammo = player.Inventory.Items.OfType<Consumable>().Single();
        Require(!state.TryBuyFromShop(ammoOffer, 1, out _) && !state.TrySellToShop(ammo, 1, out _)
            && player.Money == 132 && ammo.Quantity == 12 && offer.Stock == 0,
            "La tienda operó o repuso existencias antes de regresar.");
    });
    Require(offer.Stock == 4 && state.TryBuyFromShop(offer, 1, out _), "No se repuso la tienda al regresar.");
    player.ReceiveDamage(player.CurrentHealth);
    state.RemoveDeadCharacter(player);
    state.CreateCharacter(OriginPath.Corpo, catalog);
    Require(state.Player!.Money == 0 && !state.ShopOffers.Contains(offer),
        "El nuevo personaje heredó dinero u ofertas anteriores.");
}

void CheckExpeditionProgress()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var exp = ExpeditionFor(state, Enumerable.Range(0, 5)
        .Select(_ => Discovery(catalog.Get(104))).ToArray());
    state.StartExpedition(exp);
    Throws<InvalidOperationException>(() => exp.Advance());
    Require(exp.Position == 0 && exp.Events.All(e => !e.Resolved), "Avanzó evento sin resolver.");
    for (var index = 0; index < 5; index++)
    {
        var occurrence = exp.Current;
        exp.ResolveCurrent();
        Throws<InvalidOperationException>(() => exp.ResolveCurrent());
        Require(exp.Position == index && ReferenceEquals(exp.Current, occurrence),
            "Consultar evento volvió a sortearlo.");
        Require(exp.Advance() == (index < 4), "Avance o regreso anticipado.");
        if (index < 4)
            Throws<InvalidOperationException>(() => exp.Advance());
    }
    Require(exp.Status == ExpeditionStatus.Success && exp.Position == 4,
        "La aventura no terminó exactamente al quinto evento.");
    Throws<InvalidOperationException>(() => exp.Advance());
    Throws<InvalidOperationException>(() => exp.ResolveCurrent());
    state.CompleteExpedition(exp);
}

void CheckExpeditionLoot()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Street, catalog);
    var player = state.Player!;
    var events = Enumerable.Range(0, 5).Select(_ => Discovery(catalog.Get(102), 5)).ToArray();
    var exp = ExpeditionFor(state, events);
    state.StartExpedition(exp);
    var entry = events[0].Loot.Single();
    var before = player.Inventory.Items.ToArray();
    var reserve = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.CatalogItemId == 102);
    var originalId = reserve.RuntimeId;
    var originalQuantity = reserve.Quantity;
    var factory = new ItemFactory(catalog);
    Require(!exp.TryCollect(entry, 1, factory, out _) && player.Inventory.Items.SequenceEqual(before),
        "Se recogió botín antes de resolver el evento.");
    exp.ResolveCurrent();
    Require(!exp.TryCollect(entry, 0, factory, out _)
        && !exp.TryCollect(entry, 6, factory, out _)
        && !exp.TryCollect(events[1].Loot.Single(), 1, factory, out _)
        && player.Inventory.Items.SequenceEqual(before)
        && reserve.Quantity == originalQuantity, "Botín inválido modificó inventario.");
    Require(exp.TryCollect(entry, 2, factory, out _) && entry.Remaining == 3,
        "No se recogió parte de la pila.");
    var collected = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.CatalogItemId == 102);
    Require(ReferenceEquals(collected, reserve) && collected.RuntimeId == originalId
        && collected.Quantity == originalQuantity + 2
        && player.Inventory.Items.SequenceEqual(before)
        && collected.Type == ConsumableType.Ammunition,
        "El botín creó una segunda pila o alteró la existente.");
    exp.Advance();
    Require(!exp.TryCollect(entry, 1, factory, out _)
        && player.Inventory.Contains(collected) && entry.Remaining == 3
        && collected.Quantity == originalQuantity + 2,
        "Se pudo recuperar botín abandonado.");
}

void CheckLootStacking()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var healing = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Healing);
    var ammunition = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Ammunition);
    var originalItems = player.Inventory.Items.ToArray();
    var originalHealth = player.CurrentHealth;
    var events = Enumerable.Range(0, 5).Select(_ =>
        new EventOccurrence(EventKind.Discovery, new[]
        {
            new LootEntry(catalog.Get(104), 3),
            new LootEntry(catalog.Get(101), 6)
        })).ToArray();
    var exp = ExpeditionFor(state, events);
    state.StartExpedition(exp);
    var factory = new ItemFactory(catalog);
    for (var index = 0; index < 2; index++)
    {
        exp.ResolveCurrent();
        var originalHealingId = healing.RuntimeId;
        var originalAmmoId = ammunition.RuntimeId;
        Require(exp.TryCollect(exp.Current.Loot[0], index + 1, factory, out _)
            && exp.TryCollect(exp.Current.Loot[1], 2, factory, out _),
            "No se recogió el botín parcial.");
        Require(ReferenceEquals(player.Inventory.Items.OfType<Consumable>()
                .Single(item => item.Type == ConsumableType.Healing), healing)
            && ReferenceEquals(player.Inventory.Items.OfType<Consumable>()
                .Single(item => item.Type == ConsumableType.Ammunition), ammunition)
            && healing.RuntimeId == originalHealingId && ammunition.RuntimeId == originalAmmoId
            && player.Inventory.Items.SequenceEqual(originalItems)
            && player.CurrentHealth == originalHealth
            && healing.Quantity == (index == 0 ? 3 : 5)
            && ammunition.Quantity == 12 + (index + 1) * 2,
            "El botín duplicó pilas o cambió identidad, recursos o equipo.");
        exp.Advance();
    }
}

void CheckLootAfterDepletion()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var healing = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Healing);
    Require(player.TryDiscard(healing, healing.Quantity, out _)
        && !player.Inventory.Contains(healing), "No se vació la pila.");
    var exp = ExpeditionFor(state, Enumerable.Range(0, 5)
        .Select(_ => Discovery(catalog.Get(104), 2)).ToArray());
    exp.ResolveCurrent();
    var reward = exp.Current.Loot.Single();
    Require(exp.TryCollect(reward, 2, new ItemFactory(catalog), out _),
        "No se creó la pila nueva.");
    var replacement = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Healing);
    Require(replacement.RuntimeId != healing.RuntimeId && replacement.Quantity == 2
        && !player.Inventory.Contains(healing), "Revivió una pila descartada.");
    Throws<InvalidOperationException>(() => player.Inventory.Add(healing));
}

void CheckLootStackOverflow()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var old = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Ammunition);
    Require(player.TryDiscard(old, old.Quantity, out _), "No se pudo preparar el límite.");
    var full = factory.CreateConsumable(101, int.MaxValue - 1);
    player.Inventory.Add(full);
    var oldItems = player.Inventory.Items.ToArray();
    var exp = ExpeditionFor(state, Enumerable.Range(0, 5)
        .Select(_ => Discovery(catalog.Get(101), 2)).ToArray());
    exp.ResolveCurrent();
    var entry = exp.Current.Loot.Single();
    Require(!exp.TryCollect(entry, 2, factory, out var error)
        && !string.IsNullOrWhiteSpace(error)
        && full.Quantity == int.MaxValue - 1
        && entry.Remaining == 2
        && player.Inventory.Items.SequenceEqual(oldItems),
        "Una pila desbordada modificó inventario o botín.");
    Require(exp.TryCollect(entry, 1, factory, out _)
        && full.Quantity == int.MaxValue && entry.Remaining == 1
        && player.Inventory.Items.SequenceEqual(oldItems), "No se recogió el último espacio válido.");
    Require(!exp.TryCollect(entry, 1, factory, out _)
        && entry.Remaining == 1 && full.Quantity == int.MaxValue,
        "El exceso posterior modificó una pila llena.");
}

void CheckLootStackUsage()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var ammo = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Ammunition);
    var healing = player.Inventory.Items.OfType<Consumable>()
        .Single(item => item.Type == ConsumableType.Healing);
    var originalWeapon = player.Equipment.Weapons.Single();
    var exp = ExpeditionFor(state, Enumerable.Range(0, 5)
        .Select(_ => new EventOccurrence(EventKind.Discovery, new[]
        {
            new LootEntry(catalog.Get(101), 2),
            new LootEntry(catalog.Get(104), 2)
        })).ToArray());
    exp.ResolveCurrent();
    var factory = new ItemFactory(catalog);
    Require(exp.TryCollect(exp.Current.Loot[0], 2, factory, out _)
        && exp.TryCollect(exp.Current.Loot[1], 2, factory, out _)
        && ammo.Quantity == 14 && healing.Quantity == 4,
        "No se apiló el botín para el uso posterior.");
    var empty = factory.CreateWeapon("pistola-t1");
    player.Inventory.Add(empty);
    Require(player.Equipment.TryEquip(empty, out _), "No se pudo preparar arma vacía.");
    var combat = Combat(state, 0.99);
    Require(combat.TrySelectWeapon(empty, out _), "No se eligió arma vacía.");
    var reload = combat.TakeTurn(CombatAction.Reload);
    Require(reload.PlayerAction is { Reloaded: 12, ConsumedAction: true }
        && ammo.Quantity == 2 && player.Inventory.Contains(ammo)
        && empty.LoadedAmmo == 12 && originalWeapon.LoadedAmmo == 12,
        "Recarga no consumió la pila apilada.");
    player.ReceiveDamage(20);
    Require(player.TryUseHealing(healing, out var healed, out _)
        && healed == 20 && healing.Quantity == 3
        && player.Inventory.Items.OfType<Consumable>()
            .Count(item => item.Type == ConsumableType.Healing) == 1,
        "Curación no consumió de la pila apilada.");
}

void CheckExpeditionWeapons()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var exp = ExpeditionFor(state, Enumerable.Range(0, 5)
        .Select(_ => Discovery(catalog.Get(1), 2)).ToArray());
    var current = exp.Current.Loot.Single();
    exp.ResolveCurrent();
    Require(exp.TryCollect(current, 2, new ItemFactory(catalog), out _)
        && current.Remaining == 0, "No se recogieron dos armas.");
    var weapons = player.Inventory.Items.OfType<Weapon>().ToArray();
    Require(weapons.Length == 3 && weapons.DistinctBy(w => w.RuntimeId).Count() == 3
        && weapons.Skip(1).All(w => w.LoadedAmmo == 0)
        && player.Equipment.Weapons.Count == 1
        && player.Equipment.Weapons.Single() == weapons[0],
        "Botín duplicó equipo o cargadores.");
    Require(!exp.TryCollect(current, 1, new ItemFactory(catalog), out _),
        "Se recogió el mismo botín dos veces.");
}

void CheckExpeditionOutcomes()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var exp = ExpeditionFor(state, Enumerable.Range(0, 5)
        .Select(_ => Discovery(catalog.Get(104))).ToArray());
    state.StartExpedition(exp);
    player.ReceiveDamage(41);
    var health = player.CurrentHealth;
    for (var i = 0; i < 5; i++)
    {
        Require(player.CurrentHealth == health, "Menú/evento curó pasivamente.");
        exp.ResolveCurrent();
        exp.Advance();
    }
    Require(player.CurrentHealth == health, "Quinto evento curó antes del regreso.");
    state.CompleteExpedition(exp);
    Require(player.CurrentHealth == player.MaxHealth && state.ActiveExpedition is null,
        "No recuperó salud al regresar.");
    Throws<InvalidOperationException>(() => state.CompleteExpedition(exp));

    var doomed = ExpeditionFor(state, Enumerable.Range(0, 5)
        .Select(_ => Discovery(catalog.Get(104))).ToArray());
    state.StartExpedition(doomed);
    player.ReceiveDamage(100);
    Throws<InvalidOperationException>(() => doomed.ResolveCurrent());
    Require(!doomed.TryCollect(doomed.Current.Loot.Single(), 1,
        new ItemFactory(catalog), out _), "Se recogió botín después de morir.");
    state.RemoveDeadCharacter(player);
    Require(state.Player is null && state.ActiveExpedition is null,
        "La muerte conservó sesión o aventura.");
    state.CreateCharacter(OriginPath.Nomad, catalog);
    Require(state.Player is { Origin: OriginPath.Nomad }
        && state.Player.Inventory.Items.Count == 3, "La recreación conservó carga.");
}

void WithDatabase(Action<SqliteGameStore, ItemCatalog, string> check)
{
    var root = OperatingSystem.IsLinux() ? "/tmp/opencode" : Path.GetTempPath();
    Directory.CreateDirectory(root);
    var path = Path.Combine(root, $"mysharpventure-check-{Guid.NewGuid():N}.db");
    try
    {
        var store = new SqliteGameStore(path);
        check(store, store.LoadCatalog(), path);
    }
    finally
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(path))
            File.Delete(path);
        foreach (var backup in Directory.GetFiles(root, $"{Path.GetFileName(path)}.v*-*.bak"))
            File.Delete(backup);
    }
}

void RemoveNewFirearms(SqliteConnection connection)
{
    RemoveMeleeWeapons(connection);
    using var command = connection.CreateCommand();
    command.CommandText = """
        DELETE FROM WEAPON_DEFINITION WHERE variant IN ('Revolver', 'DoubleBarrel', 'AssaultRifle', 'PrecisionRifle');
        DELETE FROM ITEM_CATALOG WHERE code GLOB 'revolver-t?' OR code GLOB 'escopeta-doble-t?'
            OR code GLOB 'rifle-asalto-t?' OR code GLOB 'rifle-precision-t?';
        DELETE FROM CONSUMABLE_DEFINITION WHERE catalog_item_id = 105;
        DELETE FROM ITEM_CATALOG WHERE catalog_item_id = 105;
        """;
    command.ExecuteNonQuery();
}

void RemoveMeleeWeapons(SqliteConnection connection)
{
    using var command = connection.CreateCommand();
    command.CommandText = """
        DELETE FROM WEAPON_DEFINITION WHERE variant IN ('Sword', 'Bat');
        DELETE FROM ITEM_CATALOG WHERE code GLOB 'espada-t?' OR code GLOB 'bate-t?';
        DELETE FROM ITEM_CATALOG WHERE catalog_item_id >= 470 OR catalog_item_id BETWEEN 432 AND 435
            OR catalog_item_id BETWEEN 442 AND 445;
        """;
    command.ExecuteNonQuery();
}

void CheckAccessoryStash()
{
    WithDatabase((store, original, path) =>
    {
        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var setup = connection.CreateCommand();
            setup.CommandText = """
                DELETE FROM ITEM_CATALOG WHERE kind IN ('accessory', 'component', 'implant', 'quickhack');
                INSERT INTO STASH_ENTRY VALUES (202, 1);
                PRAGMA user_version = 2;
                """;
            setup.ExecuteNonQuery();
            RemoveNewFirearms(connection);
        }
        var notices = new List<string>();
        var catalog = store.LoadCatalog(notices.Add);
        var backup = Directory.GetFiles(Path.GetDirectoryName(path)!, $"{Path.GetFileName(path)}.v2-*.bak").Single();
        Require(notices.Single().Contains(backup) && store.ReadStash(catalog)[202] == 1,
            "No se respaldó v2 o se perdió un arma mejorada del stash.");
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, catalog);
        var player = state.Player!;
        var pistol = player.Equipment.Weapons.Single();
        var factory = new ItemFactory(catalog);
        for (var i = 0; i < 3; i++)
        {
            var spare = factory.CreateWeapon("pistola-t1");
            player.Inventory.Add(spare);
            Require(state.TryDisassemble(spare, 1, out _), "Faltan componentes para la prueba.");
        }
        var stash = new StashService(store, catalog);
        var sight = AccessoryDefinition.Sight;
        var magazine = AccessoryDefinition.Magazine;
        Require(state.TryCraftExtendedMagazine(out _) && state.TryCraftReflexSight(out _)
            && state.TryCraftReflexSight(out _) && state.TryCraftReflexSight(out _)
            && state.TryChangeExtendedMagazine(pistol, true, out _)
            && state.TryChangeReflexSight(pistol, true, out _) && player.TryReload(pistol, out _, out _),
            "No se pudo configurar el arma a depositar.");
        store.Deposit(sight.CatalogItemId, int.MaxValue);
        Require(!stash.TryDeposit(player, pistol, 1, out _) && !stash.TryDepositSupply(player, sight, 2, out _)
            && player.SpareReflexSights == 2 && pistol.HasReflexSight && pistol.HasExtendedMagazine
            && pistol.LoadedAmmo == 16 && player.Equipment.Weapons.Contains(pistol)
            && stash.Entries().Count == 2, "El rechazo no revirtió el depósito completo.");
        store.Withdraw(sight.CatalogItemId, int.MaxValue);
        Require(stash.TryDeposit(player, pistol, 1, out _) && stash.TryDepositSupply(player, sight, 2, out _)
            && !player.Inventory.Contains(pistol) && !pistol.HasAccessories && pistol.LoadedAmmo == 0,
            "No se depositaron el arma y los accesorios.");

        var reopened = new SqliteGameStore(path);
        var restored = reopened.LoadCatalog(notices.Add);
        var restoredStash = new StashService(reopened, restored);
        var entries = reopened.ReadStash(restored);
        Require(notices.Count == 1 && entries[1] == 1 && entries[101] == 16 && entries[202] == 1
            && entries[sight.CatalogItemId] == 3 && entries[magazine.CatalogItemId] == 1,
            "La reapertura perdió o duplicó parte del depósito.");
        var next = new GameState();
        next.CreateCharacter(OriginPath.Nomad, restored);
        var recipient = next.Player!;
        Require(restoredStash.TryWithdraw(recipient, 1, 1, out _)
            && restoredStash.TryWithdraw(recipient, magazine.CatalogItemId, 1, out _)
            && restoredStash.TryWithdraw(recipient, sight.CatalogItemId, 2, out _)
            && recipient.SpareExtendedMagazines == 1 && recipient.SpareReflexSights == 2
            && recipient.Inventory.Items.OfType<Weapon>().Single(weapon => weapon.CatalogItemId == 1)
                is { LoadedAmmo: 0, HasAccessories: false }
            && reopened.ReadStash(restored)[sight.CatalogItemId] == 1,
            "El arma no volvió vacía o los accesorios no volvieron sueltos.");
    });
}

void CheckComponentStash()
{
    WithDatabase((store, original, path) =>
    {
        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var setup = connection.CreateCommand();
            setup.CommandText = """
                DELETE FROM ITEM_CATALOG WHERE kind IN ('component', 'implant', 'quickhack');
                INSERT INTO STASH_ENTRY VALUES (202, 1), (301, 2), (104, 3);
                PRAGMA user_version = 3;
                """;
            setup.ExecuteNonQuery();
            RemoveNewFirearms(connection);
        }
        var notices = new List<string>();
        var catalog = store.LoadCatalog(notices.Add);
        var backup = Directory.GetFiles(Path.GetDirectoryName(path)!, $"{Path.GetFileName(path)}.v3-*.bak").Single();
        Require(notices.Single().Contains(backup) && store.ReadStash(catalog).Count == 3,
            "No se respaldó v3 o se perdieron depósitos.");
        using (var old = new SqliteConnection($"Data Source={backup};Mode=ReadOnly;Pooling=False"))
        {
            old.Open();
            using var query = old.CreateCommand();
            query.CommandText = "PRAGMA user_version";
            Require(Convert.ToInt32(query.ExecuteScalar()) == 3, "La copia no conserva la versión 3.");
        }

        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, catalog);
        var player = state.Player!;
        var supply = ComponentDefinition.Workshop;
        var id = supply.CatalogItemId;
        var stash = new StashService(store, catalog);
        Require(state.TryDisassemble(player.Equipment.Weapons.Single(), 1, out _) && player.Components == 4
            && !stash.TryDepositSupply(player, supply, 0, out _) && !stash.TryDepositSupply(player, supply, 5, out _),
            "Se admitió una cantidad inválida.");
        store.Deposit(id, int.MaxValue);
        Require(!stash.TryDepositSupply(player, supply, 2, out _) && !stash.TryWithdraw(player, id, int.MaxValue, out _)
            && player.Components == 4 && store.ReadStash(catalog)[id] == int.MaxValue,
            "Un rechazo por desbordamiento alteró uno de los saldos.");
        store.Withdraw(id, int.MaxValue);
        Require(stash.TryDepositSupply(player, supply, 3, out _) && player.Components == 1,
            "El depósito parcial no descontó lo correcto.");
        player.ReceiveDamage(player.CurrentHealth);
        state.RemoveDeadCharacter(player);
        var reopened = new SqliteGameStore(path);
        var restored = reopened.LoadCatalog(notices.Add);
        var entries = reopened.ReadStash(restored);
        Require(notices.Count == 1 && entries[id] == 3 && entries[202] == 1 && entries[301] == 2 && entries[104] == 3,
            "La reapertura migró de nuevo o alteró los depósitos.");
        state.CreateCharacter(OriginPath.Nomad, restored);
        Require(state.Player!.Components == 0
            && new StashService(reopened, restored).TryWithdraw(state.Player, id, 2, out _)
            && state.Player.Components == 2 && reopened.ReadStash(restored)[id] == 1,
            "El personaje heredó los componentes transportados o la retirada parcial falló.");
        var recipe = state.CraftingRecipes.Single(entry => entry.Output is ConsumableDefinition { Type: ConsumableType.Healing });
        Require(state.TryCraft(recipe, 1, out _) && state.Player.Components == 0,
            "Los componentes retirados no sirvieron en el Taller.");
    });
}

void CheckCarryWeight()
{
    WithDatabase((store, catalog, path) =>
    {
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, catalog);
        var player = state.Player!;
        var weapon = player.Equipment.Weapons.Single();
        var factory = new ItemFactory(catalog);
        var ammoId = weapon.WeaponDefinition.AmmoCatalogItemId;
        Require(player.CarryWeightGrams == 2240 && player.CarryLimitGrams == 22000,
            "El kit inicial duplicó el peso del arma o sumó el implante instalado.");
        Combat(state, 0).TakeTurn(CombatAction.WeaponAttack);
        Require(player.CarryWeightGrams == 2230 && player.TryReload(weapon, out _, out _)
            && player.CarryWeightGrams == 2230, "Recargar no conservó el peso de las balas.");
        var spare = factory.CreateWeapon("pistola-t1");
        player.Inventory.Add(spare);
        Require(state.TryDisassemble(spare, 1, out _) && state.TryCraftExtendedMagazine(out _),
            "No se preparó el accesorio.");
        var withAccessory = player.CarryWeightGrams;
        Require(state.TryChangeExtendedMagazine(weapon, true, out _)
            && player.CarryWeightGrams == withAccessory
            && state.TryChangeExtendedMagazine(weapon, false, out _)
            && player.CarryWeightGrams == withAccessory, "Mover un accesorio duplicó su peso.");

        var medicine = player.Inventory.Items.OfType<Consumable>().Single(item => item.Type == ConsumableType.Healing);
        Require(state.TrySellToShop(medicine, 2, out _), "No se pudo financiar el ripperdoc.");
        var beforeExtraction = player.CarryWeightGrams;
        Require(state.TryExtractImplant(CyberwareZone.OperatingSystem, out _)
            && player.CarryWeightGrams == beforeExtraction + 750
            && state.TryInstallImplant(player.SpareCyberware.Single(), out _)
            && player.CarryWeightGrams == beforeExtraction, "Instalar o extraer no recalculó el peso.");
        Require(player.TryIncreaseAttribute(AttributeType.Strength, out _) && player.CarryLimitGrams == 24000,
            "Fuerza no aumentó la capacidad.");

        var needed = (player.CarryLimitGrams - player.CarryWeightGrams) / 10;
        var stash = new StashService(store, catalog);
        store.Deposit(ammoId, (int)needed);
        Require(stash.TryWithdraw(player, ammoId, (int)needed, out _)
            && player.CarryWeightGrams == player.CarryLimitGrams && !player.IsOverweight,
            "Retirar munición no alcanzó el límite exacto.");
        Expedition NewTrip() => ExpeditionFor(state, Enumerable.Range(0, 5)
            .Select(_ => Discovery(catalog.Get(ammoId), 2)).ToArray());
        var first = NewTrip();
        state.StartExpedition(first);
        player.RestoreRam();
        var combat = Combat(state, 0.95);
        Require(combat.Preview(CombatAction.WeaponAttack) is { Accuracy: 0.85m, StaminaCost: 4 }
            && combat.Preview(CombatAction.BasicAttack) is { Accuracy: 0.85m, StaminaCost: 5 },
            "Estar justo en el límite aplicó una penalización.");
        first.ResolveCurrent();
        Require(first.TryCollect(first.Current.Loot.Single(), 2, factory, out _)
            && player.IsOverweight && first.Advance(), "El sobrepeso impidió recoger o avanzar.");
        Require(combat.Preview(CombatAction.WeaponAttack) is { Accuracy: 0.85m, Damage: 12, AmmoCost: 1, StaminaCost: 6 }
            && combat.Preview(CombatAction.BasicAttack) is { Accuracy: 0.85m, StaminaCost: 7 }
            && combat.Preview(CombatAction.ShortCircuit) is { RamCost: 3, Damage: 12, StaminaCost: 0 },
            "El sobrepeso cambió daño, acierto, RAM o no añadió dos de stamina.");
        Require(combat.TakeTurn(CombatAction.WeaponAttack).PlayerAction is { Hit: false, StaminaSpent: 6 }
            && combat.TakeTurn(CombatAction.BasicAttack).PlayerAction is { Hit: false, StaminaSpent: 7 }
            && combat.TakeTurn(CombatAction.ShortCircuit).PlayerAction is { RamSpent: 3, StaminaSpent: 0 } && player.Ram == 4,
            "El fallo no gastó stamina o el sobrepeso alteró la RAM.");
        Require(player.Stamina == 5, "El sobrepeso no redujo la recuperación de stamina.");
        var remaining = player.Stamina;
        state.WithdrawExpedition(first);
        Require(player.Stamina == remaining, "Retirarse recuperó stamina.");
        player.Inventory.Add(factory.CreateWeapon("pistola-t1"));
        var excess = factory.CreateConsumable(ammoId, 2000);
        player.Inventory.Add(excess);
        var next = NewTrip();
        Throws<InvalidOperationException>(() => state.StartExpedition(next));
        Require(state.ActiveExpedition is null && stash.TryDeposit(player, excess, 2000, out _)
            && !player.IsOverweight, "El rechazo dejó una expedición activa o depositar no alivió la carga.");
        Require(Combat(state, 0).Preview(CombatAction.BasicAttack).StaminaCost == 5,
            "La penalización siguió activa al aligerar la carga.");
        state.StartExpedition(next);
    });
}

void CheckPracticeCopy()
{
    WithDatabase((store, catalog, path) =>
    {
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, catalog);
        var player = state.Player!;
        var trip = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
            new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
        state.StartExpedition(trip);
        do { trip.ResolveCurrent(); } while (trip.Advance());
        state.CompleteExpedition(trip);
        var gun = player.Equipment.Weapons.Single();
        var ammo = player.Inventory.Items.OfType<Consumable>().First(stack => stack.Type == ConsumableType.Ammunition);
        var medicine = player.Inventory.Items.OfType<Consumable>().First(stack => stack.Type == ConsumableType.Healing);
        store.Deposit(303, 12);
        Require(new StashService(store, catalog).TryWithdraw(player, 303, 12, out _)
            && state.TryCraftExtendedMagazine(out _) && state.TryCraftReflexSight(out _)
            && state.TryChangeExtendedMagazine(gun, true, out _) && state.TryChangeReflexSight(gun, true, out _)
            && player.TryReload(gun, out _, out _)
            && state.TryBuyImplant(CyberwareContent.SubdermalArmor, out _)
            && state.TryInstallImplant(player.SpareCyberware.Single(), out _), "No se pudo preparar el equipo.");
        var setup = Combat(state, 0.99);
        setup.TakeTurn(CombatAction.BasicAttack);
        player.SpendRam(2);
        player.ReceiveDamage(30);
        var before = (player.CurrentHealth, player.Shield, player.Stamina, player.Ram, player.Money, player.Experience,
            gun.LoadedAmmo, ammo.Quantity, medicine.Quantity);
        var copy = player.CreatePracticeCopy();
        var copyGun = copy.Equipment.Weapons.Single();
        Require(copy != player && copyGun != gun && copyGun.HasExtendedMagazine && copyGun.HasReflexSight
            && copyGun.LoadedAmmo == gun.LoadedAmmo && gun.LoadedAmmo > gun.WeaponDefinition.MagazineCapacity
            && copy.CarryWeightGrams == player.CarryWeightGrams
            && (copy.CurrentHealth, copy.Shield, copy.Stamina, copy.Ram) ==
                (player.CurrentHealth, player.Shield, player.Stamina, player.Ram)
            && copy.IsQuickhackEquipped(CombatAction.ShortCircuit), "La copia no conservó el estado actual.");
        var fight = new CombatController(copy, DevelopmentContent.MaelstromTest, DevelopmentContent.BasicAttack, new FixedRandom(0.99));
        Require(fight.TakeTurn(CombatAction.Heal).PlayerAction.Healed > 0
            && fight.TakeTurn(CombatAction.WeaponAttack).PlayerAction.AmmoSpent == 1
            && fight.TakeTurn(CombatAction.Reload).PlayerAction.Reloaded == 1
            && fight.TakeTurn(CombatAction.ShortCircuit).PlayerAction.RamSpent == 3,
            "La copia no permitió usar los recursos reales de la configuración.");
        copy.ReceiveDamage(copy.CurrentHealth, bypassShield: true);
        Require(state.Player == player && before == (player.CurrentHealth, player.Shield, player.Stamina, player.Ram,
            player.Money, player.Experience, gun.LoadedAmmo, ammo.Quantity, medicine.Quantity)
            && player.Equipment.Weapons.Single() == gun && gun.HasAccessories,
            "La práctica modificó al personaje real.");
    });
}

void CheckStamina()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var expedition = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ => Discovery(catalog.Get("curacion"))).ToArray());
    state.StartExpedition(expedition);
    var fight = Combat(state, 0.99);
    for (var i = 0; i < 4; i++)
        Require(fight.TakeTurn(CombatAction.BasicAttack) is
            { PlayerAction.StaminaSpent: 5, EnemyAction.Action: CombatAction.BasicAttack }, "No se aplicó el coste de ataque.");
    var ammo = fight.SelectedWeapon!.LoadedAmmo;
    Require(player.Stamina == 4 && fight.PlannedEnemyAction == CombatAction.Wait
        && !fight.Preview(CombatAction.BasicAttack).Available
        && !fight.TakeTurn(CombatAction.BasicAttack).PlayerAction.ConsumedAction
        && fight.TrySelectWeapon(fight.SelectedWeapon, out _)
        && player.Stamina == 4 && fight.SelectedWeapon.LoadedAmmo == ammo,
        "El rechazo o un menú gratis recuperó stamina o cambió el turno.");
    Require(fight.TakeTurn(CombatAction.WeaponAttack) is
        { PlayerAction.StaminaSpent: 4, EnemyAction.Action: CombatAction.Wait }
        && player.Stamina == 3 && fight.Enemy.Stamina == 4,
        "El enemigo no esperó o la regeneración no se aplicó una sola vez.");
    player.SpendRam(5);
    expedition.ResolveCurrent();
    Require(player.Stamina == 3 && player.Ram == 1 && expedition.Advance()
        && expedition.LastRecovery == (9, 0, 3) && player.Stamina == 12 && player.Ram == 4,
        "No se recuperaron los tres turnos justo al pasar al siguiente evento.");
    Throws<InvalidOperationException>(() => expedition.Advance());
    Require(player.Ram == 4, "Un avance rechazado repitió la recuperación.");
    var nextFight = Combat(state, 0.99);
    nextFight.TakeTurn(CombatAction.BasicAttack);
    Require(player.Stamina == 10, "No se conservó la stamina antes de terminar la aventura.");
    do { expedition.ResolveCurrent(); } while (expedition.Advance());
    state.CompleteExpedition(expedition);
    Require(player.Stamina == 12, "El regreso exitoso no restauró la stamina.");
}

void CheckCriticalHits()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var weapon = player.Equipment.Weapons.Single();
    var shielded = new GameState();
    shielded.CreateCharacter(OriginPath.Nomad, catalog);
    var target = shielded.Player!;
    void EarnAndRefill()
    {
        var exp = ExpeditionFor(shielded, Enumerable.Range(0, 5).Select(_ =>
            new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
        shielded.StartExpedition(exp);
        do { exp.ResolveCurrent(); } while (exp.Advance());
        shielded.CompleteExpedition(exp);
    }
    EarnAndRefill();
    Require(shielded.TryBuyImplant(CyberwareContent.SubdermalArmor, out _)
        && shielded.TryInstallImplant(target.SpareCyberware.Single(), out _), "No se pudo preparar el escudo.");
    EarnAndRefill();

    var miss = new CriticalRandom(0.99, 0);
    var missed = new ActionResolver(new DamageCalculator(miss)).Resolve(player, target,
        CombatAction.WeaponAttack, weapon, DevelopmentContent.BasicAttack);
    Require(missed is { Hit: false, Critical: false, AmmoSpent: 1, StaminaSpent: 4 }
        && miss.CriticalRolls == 0 && target.Shield == 12, "Un fallo hizo tirada crítica o cambió los costes.");
    var random = new CriticalRandom(0, 7);
    var resolver = new ActionResolver(new DamageCalculator(random));
    Require(resolver.Preview(player, target, CombatAction.WeaponAttack, weapon, DevelopmentContent.BasicAttack)
        is { CriticalPercent: 10, Accuracy: 0.85m } && random.CriticalRolls == 0,
        "La vista previa confundió acierto y crítico o consumió una tirada.");
    var hit = resolver.Resolve(player, target, CombatAction.WeaponAttack, weapon, DevelopmentContent.BasicAttack);
    Require(hit is { Hit: true, Critical: true, Damage: { Incoming: 18, ShieldPrevented: 9, Applied: 9 } }
        && random.CriticalRolls == 1 && target.Shield == 3,
        "El crítico de arma no se aplicó antes del escudo.");
    var fight = new CombatController(player, DevelopmentContent.MaelstromTest, DevelopmentContent.BasicAttack,
        new CriticalRandom(0, 7));
    Require(fight.TakeTurn(CombatAction.BasicAttack).PlayerAction is { Critical: true, Damage.Incoming: 7 },
        "El crítico cuerpo a cuerpo no redondeó el daño hacia abajo.");
    player.Inventory.Add(factory.CreateConsumable(101, 3000));
    fight.TakeTurn(CombatAction.Wait);
    Require(fight.Preview(CombatAction.WeaponAttack) is { CriticalPercent: 5, Accuracy: 0.85m, StaminaCost: 6 }
        && fight.TakeTurn(CombatAction.WeaponAttack).PlayerAction is { Hit: true, Critical: false, Damage.Incoming: 12 },
        "La misma tirada debía dar un impacto normal con sobrepeso.");
    Require(fight.TakeTurn(CombatAction.ShortCircuit).PlayerAction is { Critical: false, RamSpent: 3, Damage.Incoming: 12 },
        "Un quickhack fue afectado por la tirada crítica.");
}

void CheckGorillaArms()
{
    WithDatabase((store, original, path) =>
    {
        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var setup = connection.CreateCommand();
            setup.CommandText = """
                DELETE FROM ITEM_CATALOG WHERE catalog_item_id BETWEEN 462 AND 465;
                INSERT INTO STASH_ENTRY VALUES (461, 1);
                PRAGMA user_version = 6;
                """;
            setup.ExecuteNonQuery();
            RemoveNewFirearms(connection);
        }
        var notices = new List<string>();
        var upgraded = store.LoadCatalog(notices.Add);
        Require(notices.Single().Contains(".v6-") && store.ReadStash(upgraded)[461] == 1,
            "La migración no respaldó el catálogo o perdió un depósito.");
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, upgraded);
        var player = state.Player!;
        var trip = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
            new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
        state.StartExpedition(trip);
        do { trip.ResolveCurrent(); } while (trip.Advance());
        state.CompleteExpedition(trip);
        var implant = CyberwareContent.GorillaArms;
        Require(state.TryBuyImplant(implant, out _) && !state.HasImplantStock(implant)
            && state.TryInstallImplant(player.SpareCyberware.Single(), out _) && player.Money == 25
            && player.UsedCyberwareCapacity == 14 && player.Equipment.Weapons.Count == 1,
            "La compra o instalación cambió mal el saldo, capacidad o equipamiento.");
        var fight = Combat(state, 0.99);
        Require(fight.Preview(CombatAction.BasicAttack) is { ArmAttack: "Gorilla Arms", Damage: 9, StaminaCost: 7 }
            && fight.TakeTurn(CombatAction.BasicAttack).PlayerAction is
                { ArmAttack: "Gorilla Arms", Hit: false, StaminaSpent: 7, AmmoSpent: 0 }
            && fight.TakeTurn(CombatAction.BasicAttack).PlayerAction.ArmAttack == "Gorilla Arms"
            && fight.Preview(CombatAction.BasicAttack) is { Available: false, ArmAttack: null, StaminaCost: 5 }
            && !fight.TakeTurn(CombatAction.BasicAttack).PlayerAction.ConsumedAction
            && fight.TakeTurn(CombatAction.WeaponAttack).PlayerAction.Valid
            && fight.TakeTurn(CombatAction.Wait).PlayerAction.Valid
            && fight.Preview(CombatAction.BasicAttack) is { Available: true, ArmAttack: null, Damage: 5, StaminaCost: 5 },
            "No coincide el ataque, el coste al fallar o la alternativa sin stamina.");
        var netrunner = new CombatController(player, DevelopmentContent.MaelstromTest with { Netrunner = true },
            DevelopmentContent.BasicAttack, new FixedRandom(0.99));
        netrunner.TakeTurn(CombatAction.Wait);
        Require(player.CyberwareDisabledTurns > 0
            && netrunner.Preview(CombatAction.BasicAttack) is { ArmAttack: null, Damage: 5, StaminaCost: 5 },
            "Un implante desactivado mantuvo su ataque.");
        netrunner.Withdraw();
        var stash = new StashService(store, upgraded);
        Require(state.TryExtractImplant(CyberwareZone.Arms, out _)
            && stash.TryDepositImplant(player, player.SpareCyberware.Single(), out _),
            "No se pudo extraer y guardar el implante.");
        var reopened = new SqliteGameStore(path);
        Require(new StashService(reopened, reopened.LoadCatalog()).TryWithdraw(player, 462, 1, out _)
            && player.SpareCyberware.Single() == implant && !player.InstalledCyberware.ContainsKey(CyberwareZone.Arms),
            "El implante no volvió suelto tras reabrir el stash.");
    });
}

void CheckOtherArmImplants()
{
    WithDatabase((store, original, path) =>
    {
        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var setup = connection.CreateCommand();
            setup.CommandText = """
                DELETE FROM ITEM_CATALOG WHERE catalog_item_id BETWEEN 463 AND 465;
                INSERT INTO STASH_ENTRY VALUES (462, 1);
                PRAGMA user_version = 7;
                """;
            setup.ExecuteNonQuery();
            RemoveNewFirearms(connection);
        }
        var notices = new List<string>();
        var upgraded = store.LoadCatalog(notices.Add);
        Require(notices.Single().Contains(".v7-") && store.ReadStash(upgraded)[462] == 1,
            "La migración v7 no respaldó el catálogo o perdió un implante guardado.");
        var stash = new StashService(store, upgraded);
        foreach (var (implant, price, damage, stamina, accuracy) in new[]
        {
            (CyberwareContent.MantisBlades, 55, 11, 8, 0.90m),
            (CyberwareContent.Monowire, 60, 8, 6, 0.85m),
            (CyberwareContent.ProjectileLaunchSystem, 65, 15, 10, 0.75m)
        })
        {
            var state = new GameState();
            state.CreateCharacter(OriginPath.Corpo, upgraded);
            var player = state.Player!;
            var trip = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
                new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
            state.StartExpedition(trip);
            do { trip.ResolveCurrent(); } while (trip.Advance());
            state.CompleteExpedition(trip);
            Require(state.TryBuyImplant(implant, out _) && state.TryInstallImplant(player.SpareCyberware.Single(), out _)
                && player.Money == 70 - price && stash.TryWithdraw(player, 462, 1, out _)
                && !state.TryInstallImplant(player.SpareCyberware.Single(), out _)
                && stash.TryDepositImplant(player, player.SpareCyberware.Single(), out _),
                $"{implant.Name}: coste, instalación o exclusividad incorrectos.");
            var fight = Combat(state, 0);
            var preview = fight.Preview(CombatAction.BasicAttack);
            var attack = fight.TakeTurn(CombatAction.BasicAttack).PlayerAction;
            Require(preview.ArmAttack == implant.Name && preview.Damage == damage && preview.Accuracy == accuracy
                && preview.StaminaCost == stamina && attack.Damage?.Incoming == damage
                && attack.StaminaSpent == stamina && attack.AmmoSpent == 0 && attack.RamSpent == 0,
                $"{implant.Name}: la previsión no coincide con el ataque.");
            var netrunner = new CombatController(player, DevelopmentContent.MaelstromTest with { Netrunner = true },
                DevelopmentContent.BasicAttack, new FixedRandom(0.99));
            netrunner.TakeTurn(CombatAction.Wait);
            Require(netrunner.Preview(CombatAction.BasicAttack) is { ArmAttack: null, Damage: 5 },
                $"{implant.Name}: no volvió el ataque básico tras desactivar.");
            netrunner.Withdraw();
            Require(state.TryExtractImplant(CyberwareZone.Arms, out _)
                && Combat(state, 0).Preview(CombatAction.BasicAttack) is { ArmAttack: null, Damage: 5 }
                && stash.TryDepositImplant(player, player.SpareCyberware.Single(), out _),
                $"{implant.Name}: el ataque siguió activo tras la extracción o no se pudo guardar.");
        }
    });
}

void CheckEnemyControl()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var profile = ExpeditionContent.Create(ExpeditionContent.Dogtown, new Random(1), catalog, player)
        .Events.Single(entry => entry.Enemy?.Netrunner == true).Enemy!;
    var gun = player.Equipment.Weapons.Single();
    var second = factory.CreateWeapon("pistola-t1", loaded: true);
    player.Inventory.Add(second);
    Require(player.Equipment.TryEquip(second, out _), "No se pudo preparar el arma alternativa.");
    var fight = new CombatController(player, profile, DevelopmentContent.BasicAttack, new FixedRandom(0.1));
    Require(fight.PlannedEnemyAction == CombatAction.WeaponGlitch
        && fight.TakeTurn(CombatAction.ShortCircuit) is
            { PlayerAction.BlockedBySelfIce: true, EnemyAction: { Action: CombatAction.WeaponGlitch, RamSpent: 3 } }
        && fight.GlitchedPlayerWeapon == gun && !fight.Preview(CombatAction.Reload).Available
        && !fight.TakeTurn(CombatAction.WeaponAttack).PlayerAction.ConsumedAction && gun.LoadedAmmo == 12
        && fight.TrySelectWeapon(second, out _) && fight.Preview(CombatAction.WeaponAttack).Available,
        "Fallo de arma no bloqueó solo el arma afectada o gastó recursos al rechazar.");
    Require(fight.PlannedEnemyAction == CombatAction.RebootOptics
        && fight.TakeTurn(CombatAction.Wait).EnemyAction is { Action: CombatAction.RebootOptics, RamSpent: 2 }
        && fight.GlitchedPlayerWeapon is null && fight.PlayerOpticsDisrupted
        && fight.Preview(CombatAction.WeaponAttack).Accuracy == 0.05m
        && fight.Preview(CombatAction.BasicAttack).Accuracy == 0.05m
        && fight.TakeTurn(CombatAction.ShortCircuit).PlayerAction is { Hit: true, RamSpent: 3 }
        && !fight.PlayerOpticsDisrupted,
        "Reboot Optics no limitó ambos ataques físicos o afectó un quickhack.");

    var trip = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
        new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
    state.StartExpedition(trip);
    do { trip.ResolveCurrent(); } while (trip.Advance());
    state.CompleteExpedition(trip);
    Require(state.TryBuyImplant(CyberwareContent.SelfIce, out _)
        && state.TryInstallImplant(player.SpareCyberware.Single(), out _), "No se pudo preparar Self-ICE.");
    var defended = new CombatController(player, profile, DevelopmentContent.BasicAttack, new FixedRandom(0.1));
    Require(defended.TakeTurn(CombatAction.Wait).EnemyAction is
            { Action: CombatAction.WeaponGlitch, BlockedBySelfIce: true }
        && defended.GlitchedPlayerWeapon is null && !defended.PlayerSelfIceReady,
        "Self-ICE no bloqueó el primer hackeo de control.");
}

void CheckExpandedCyberware()
{
    WithDatabase((store, catalog, path) =>
    {
        store.Deposit(303, 60);
        var stash = new StashService(store, catalog);
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, catalog);
        var player = state.Player!;
        var dogtown = ExpeditionContent.Create(ExpeditionContent.Dogtown, new Random(5), catalog, player);
        state.StartExpedition(dogtown);
        do
        {
            dogtown.ResolveCurrent();
            if (dogtown.Current.Enemy is { IsBot: true })
            {
                var loot = dogtown.Current.Loot.Single(entry => entry.Definition is CybergearStashDefinition);
                Require(dogtown.TryCollect(loot, 1, new ItemFactory(catalog), out _)
                    && !dogtown.TryCollect(loot, 1, new ItemFactory(catalog), out _), "El botín se duplicó o no se recogió.");
            }
        } while (dogtown.Advance());
        state.CompleteExpedition(dogtown);
        var rippler = player.SpareCyberware.Single();
        Require(rippler.Code == "tetratronic-rippler" && state.TryExtractImplant(CyberwareZone.OperatingSystem, out _)
            && state.TryInstallImplant(rippler, out _) && player.MaxRam == 9 && player.Ram == 0
            && stash.TryWithdraw(player, 303, 60, out _), "El deck no se instaló con su máximo sin rellenar.");
        Require(Combat(state, 0).Player.Ram == 2, "Rippler no recuperó 2 RAM.");
        for (var i = 0; i < 4; i++)
            Require(state.TryUpgradeImplant(rippler = player.InstalledCyberware[CyberwareZone.OperatingSystem], out _),
                "La cadena de Rippler no llega a Tier 5.");
        var maxed = player.InstalledCyberware[CyberwareZone.OperatingSystem];
        Require(maxed.Tier == 5 && player.MaxRam == 13 && player.Ram == 2, "La mejora rellenó RAM o no elevó el máximo.");
        var armor = (CybergearStashDefinition)catalog.Get("subdermal-armor-t1");
        store.Deposit(armor.CatalogItemId, 1);
        Require(stash.TryWithdraw(player, armor.CatalogItemId, 1, out _)
            && !state.TryInstallImplant(player.SpareCyberware.Single(implant => implant == armor.Implant), out _),
            "Se superó la capacidad de cyberware.");
        Require(state.TryExtractImplant(CyberwareZone.OperatingSystem, out _)
            && stash.TryDepositImplant(player, maxed, out _), "No se pudo depositar Rippler Tier 5.");
        foreach (var code in new[] { "self-ice", "dynalar-sandevistan", "subdermal-armor", "kiroshi",
            "kiroshi-cockatrice", "blood-pump", "heal-on-kill", "neofiber", "reflex-tuner", "shock-n-awe",
            "dense-marrow", "microgenerator", "immovable-force", "jenkins-tendons", "leeroy-ligament-system" })
            Require(catalog.Get($"{code}-t5") is CybergearStashDefinition { Implant.Tier: 5 }, $"Falta Tier 5: {code}.");
        var eyes = (CybergearStashDefinition)catalog.Get("kiroshi-t1");
        store.Deposit(eyes.CatalogItemId, 1);
        Require(stash.TryWithdraw(player, eyes.CatalogItemId, 1, out _)
            && state.TryInstallImplant(player.SpareCyberware.Single(implant => implant == eyes.Implant), out _)
            && Combat(state, 0).Preview(CombatAction.WeaponAttack).Accuracy == 0.88m,
            "El implante de ojos no cambió el acierto físico.");

        (GameState State, CyberwareDefinition Implant) Fit(string code)
        {
            var next = new GameState();
            next.CreateCharacter(OriginPath.Corpo, catalog);
            var owner = next.Player!;
            var trip = ExpeditionFor(next, Enumerable.Range(0, 5).Select(_ => Discovery(catalog.Get("curacion"))).ToArray());
            do { trip.ResolveCurrent(); } while (trip.Advance());
            var entry = (CybergearStashDefinition)catalog.Get($"{code}-t1");
            store.Deposit(entry.CatalogItemId, 1);
            if (entry.Implant!.Zone == CyberwareZone.OperatingSystem)
                Require(next.TryExtractImplant(CyberwareZone.OperatingSystem, out _), "No se pudo liberar el sistema operativo.");
            Require(stash.TryWithdraw(owner, entry.CatalogItemId, 1, out _)
                && next.TryInstallImplant(owner.SpareCyberware.Single(item => item == entry.Implant), out _),
                $"No se pudo instalar {code}.");
            return (next, owner.InstalledCyberware[entry.Implant.Zone]);
        }
        foreach (var (code, damage, accuracy, critical, shield, stamina, healing) in new[]
        {
            ("kiroshi-cockatrice", 5, 0.85m, 15, 0, 10, 25),
            ("blood-pump", 5, 0.85m, 10, 0, 10, 30),
            ("neofiber", 5, 0.85m, 10, 4, 10, 25),
            ("reflex-tuner", 5, 0.90m, 10, 0, 10, 25),
            ("shock-n-awe", 5, 0.85m, 10, 8, 10, 25),
            ("dense-marrow", 6, 0.85m, 10, 0, 10, 25),
            ("microgenerator", 5, 0.85m, 14, 0, 10, 25),
            ("immovable-force", 5, 0.89m, 10, 0, 10, 25),
            ("jenkins-tendons", 5, 0.85m, 10, 0, 11, 25),
            ("leeroy-ligament-system", 6, 0.85m, 10, 0, 10, 25)
        })
        {
            var (next, implant) = Fit(code);
            var owner = next.Player!;
            owner.ReceiveDamage(50);
            var fight = Combat(next, 0.99);
            var preview = fight.Preview(CombatAction.BasicAttack);
            Require(preview.Damage == damage && preview.Accuracy == accuracy && preview.CriticalPercent == critical
                && owner.MaxShield == shield && fight.Preview(CombatAction.Heal).HealAmount == healing
                && (code != "shock-n-awe" || owner.Shield == 3)
                && fight.TakeTurn(CombatAction.BasicAttack).PlayerAction.Valid && owner.Stamina == stamina,
                $"El efecto de {code} no coincide con su definición.");
            Require(next.TryExtractImplant(implant.Zone, out _) && stash.TryDepositImplant(owner, implant, out _),
                $"No se pudo extraer y guardar {code}.");
        }
        var (vampire, _) = Fit("heal-on-kill");
        vampire.Player!.ReceiveDamage(20);
        var lethal = new CombatController(vampire.Player, DevelopmentContent.MaelstromTest with { MaxHealth = 5 },
            DevelopmentContent.BasicAttack, new FixedRandom(0));
        Require(lethal.TakeTurn(CombatAction.BasicAttack).Status == CombatStatus.Victory
            && vampire.Player.CurrentHealth == 85, "Heal-On-Kill no recuperó salud al vencer.");
        foreach (var code in new[] { "self-ice", "dynalar-sandevistan", "subdermal-armor" })
        {
            var (next, implant) = Fit(code);
            store.Deposit(303, 4);
            Require(stash.TryWithdraw(next.Player!, 303, 4, out _) && next.TryUpgradeImplant(implant, out _),
                $"No se pudo mejorar {code}.");
            var owner = next.Player!;
            var fight = Combat(next, 0);
            Require(code switch
            {
                "self-ice" => fight.PlayerSelfIceReady,
                "dynalar-sandevistan" => fight.Preview(CombatAction.ActivateSandevistan).Available && owner.UsedCyberwareCapacity == 9,
                _ => owner.MaxShield == 13
            }, $"La mejora de {code} perdió su efecto.");
        }
    });
}

void CheckTitaniumBones()
{
    var state = new GameState();
    state.CreateCharacter(OriginPath.Corpo, catalog);
    var player = state.Player!;
    var trip = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
        new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
    state.StartExpedition(trip);
    do { trip.ResolveCurrent(); } while (trip.Advance());
    state.CompleteExpedition(trip);
    Require(state.TryBuyImplant(CyberwareContent.TitaniumBones, out _)
        && state.TryInstallImplant(player.SpareCyberware.Single(), out _), "No se pudo instalar Titanium Bones.");
    player.Inventory.Add(factory.CreateConsumable(101, 2200));
    Require(!player.IsOverweight && player.CarryLimitGrams == 26000, "El implante no aumentó la carga.");
    var netrunner = new CombatController(player, DevelopmentContent.MaelstromTest with { Netrunner = true },
        DevelopmentContent.BasicAttack, new FixedRandom(0.99));
    Require(netrunner.TakeTurn(CombatAction.Wait).EnemyAction?.Action == CombatAction.CyberwareMalfunction
        && player.IsOverweight && player.CarryLimitGrams == 22000
        && netrunner.Preview(CombatAction.WeaponAttack) is { Accuracy: 0.85m, AmmoCost: 1, StaminaCost: 6 }
        && netrunner.TakeTurn(CombatAction.WeaponAttack).PlayerAction.StaminaSpent == 6
        && player.Stamina == 8, "El sobrepeso temporal no afectó solo los parámetros previstos.");
    netrunner.Withdraw();
    Require(!player.IsOverweight && player.CarryLimitGrams == 26000,
        "El límite no volvió al reactivarse Titanium Bones.");
}

void CheckTradeBalance()
{
    WithDatabase((store, catalog, path) =>
    {
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, catalog);
        var player = state.Player!;
        var trip = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
            new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
        state.StartExpedition(trip);
        do { trip.ResolveCurrent(); } while (trip.Advance());
        state.CompleteExpedition(trip);
        var startingMoney = player.Money;
        var armor = CyberwareContent.SubdermalArmor;
        Require(state.TryBuyImplant(armor, out _) && state.TryInstallImplant(player.SpareCyberware.Single(), out _)
            && !state.TrySellImplant(player.InstalledCyberware[armor.Zone], out _)
            && state.TryExtractImplant(armor.Zone, out _)
            && state.TrySellImplant(player.SpareCyberware.Single(), out _)
            && player.Money == startingMoney - 50 - 10 + GameState.ImplantSalePrice(armor)
            && !state.HasImplantStock(armor), "Reventa de implante incorrecta o reposición no prevista.");
        Require(!state.TrySellQuickhack(CombatAction.ShortCircuit, out _)
            && state.TryExtractImplant(CyberwareZone.OperatingSystem, out _)
            && !state.TrySellImplant(player.SpareCyberware.Single(), out _)
            && !state.TrySellQuickhack(CombatAction.ShortCircuit, out _),
            "Se vendió un deck o un quickhack con configuración asignada.");
        store.Deposit(303, 200);
        var stash = new StashService(store, catalog);
        Require(stash.TryWithdraw(player, 303, 200, out _), "No se pudieron preparar componentes.");
        foreach (var recipe in state.CraftingRecipes.Where(recipe => recipe.Output is WeaponDefinition))
        {
            var before = player.Components;
            Require(state.TryCraft(recipe, 1, out _), "No se pudo fabricar para probar el desarme.");
            var weapon = player.Inventory.Items.OfType<Weapon>().Last();
            Require(state.TryDisassemble(weapon, 1, out _) && player.Components < before,
                $"El ciclo de {recipe.Output.Name} generó materiales.");
        }
        var quickhack = QuickhackContent.Overheat;
        var hackRecipe = state.CraftingRecipes.Single(recipe => recipe.Output is CybergearStashDefinition
            { Quickhack: CombatAction.Overheat });
        var cash = player.Money;
        Require(state.TryCraft(hackRecipe, 1, out _) && state.TrySellQuickhack(quickhack.Action, out _)
            && player.Money == cash + GameState.QuickhackSalePrice(quickhack)
            && !state.TrySellQuickhack(quickhack.Action, out _)
            && GameState.QuickhackSalePrice(quickhack) < quickhack.Price * 4 / 5,
            "La venta del quickhack no fue única o supera el precio de aventura.");
        var parts = player.Components;
        Require(state.TryCraftExtendedMagazine(out _) && state.TryDisassembleAccessory(AccessoryDefinition.Magazine, out _)
            && player.Components < parts && !state.TryDisassembleAccessory(AccessoryDefinition.Magazine, out _),
            "El accesorio se duplicó o devolvió los materiales íntegros.");
    });
}

void CheckShield()
{
    WithDatabase((store, original, path) =>
    {
        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var setup = connection.CreateCommand();
            setup.CommandText = """
                DELETE FROM ITEM_CATALOG WHERE catalog_item_id BETWEEN 461 AND 465;
                INSERT INTO STASH_ENTRY VALUES (411, 2);
                PRAGMA user_version = 5;
                """;
            setup.ExecuteNonQuery();
            RemoveNewFirearms(connection);
        }
        var notices = new List<string>();
        var catalog = store.LoadCatalog(notices.Add);
        var backup = Directory.GetFiles(Path.GetDirectoryName(path)!, $"{Path.GetFileName(path)}.v5-*.bak").Single();
        Require(notices.Single().Contains(backup) && store.ReadStash(catalog)[411] == 2,
            "La migración v5 no respaldó el catálogo o preservó el stash.");
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, catalog);
        var player = state.Player!;
        Expedition NewTrip() => ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ =>
            new EventOccurrence(EventKind.Combat, [], DevelopmentContent.MaelstromTest)).ToArray());
        void Finish(Expedition exp)
        {
            do { exp.ResolveCurrent(); } while (exp.Advance());
            state.CompleteExpedition(exp);
        }
        var first = NewTrip();
        state.StartExpedition(first);
        Finish(first);
        Require(state.TryBuyImplant(CyberwareContent.SubdermalArmor, out _)
            && state.TryInstallImplant(player.SpareCyberware.Single(), out _)
            && player.Money == 20 && player.MaxShield == 12 && player.Shield == 0 && player.UsedCyberwareCapacity == 14,
            "No se aplicaron el precio, la capacidad o el máximo del implante.");
        var second = NewTrip();
        state.StartExpedition(second);
        second.ResolveCurrent();
        Require(player.Shield == 0 && second.Advance() && player.Shield == 6, "La pausa regeneró antes de avanzar.");
        Finish(second);
        Require(player.Shield == 12
            && player.ReceiveDamage(10) is { Applied: 5, ShieldPrevented: 5 } && player.Shield == 7
            && player.ReceiveDamage(10) is { Applied: 8, ShieldPrevented: 2 } && player.Shield == 5
            && player.ReceiveDamage(30) is { Applied: 25, ShieldPrevented: 5 } && player.Shield == 0
            && player.ReceiveDamage(1) is { Applied: 1, ShieldPrevented: 0 },
            "El escudo no redujo parcialmente el impacto según la carga.");
        var netrunner = new CombatController(player, DevelopmentContent.MaelstromTest with { Netrunner = true },
            DevelopmentContent.BasicAttack, new CountingRandom(0.99));
        Require(player.Shield == 2, "El primer turno no recuperó escudo.");
        netrunner.TakeTurn(CombatAction.Wait);
        Require(player.Shield == 0 && player.MaxShield == 0, "Fallo de cyberware no anuló el escudo.");
        netrunner.TakeTurn(CombatAction.Wait);
        netrunner.Withdraw();
        Require(player.Shield == 0 && player.MaxShield == 12, "Reactivar por retirada rellenó el escudo.");

        Require(state.TryExtractImplant(CyberwareZone.Integumentary, out _), "No se pudo extraer el implante.");
        var stash = new StashService(store, catalog);
        Require(stash.TryDepositImplant(player, player.SpareCyberware.Single(), out _), "No se guardó el implante extraído.");
        var reopened = new SqliteGameStore(path);
        var restored = reopened.LoadCatalog(notices.Add);
        Require(notices.Count == 1 && new StashService(reopened, restored).TryWithdraw(player, 461, 1, out _)
            && player.SpareCyberware.Single() == CyberwareContent.SubdermalArmor && player.MaxShield == 0,
            "El implante no volvió suelto al reabrir.");
        Require(state.TryInstallImplant(player.SpareCyberware.Single(), out _), "No se pudo instalar de nuevo.");
        var third = NewTrip();
        state.StartExpedition(third);
        Finish(third);
        Require(state.TryBuyImplant(CyberwareContent.Sandevistan, out _)
            && state.TryExtractImplant(CyberwareZone.OperatingSystem, out _)
            && state.TryInstallImplant(player.SpareCyberware.Single(implant => implant == CyberwareContent.Sandevistan), out _),
            "No se pudieron combinar Sandevistan y el escudo.");
        player.ReceiveDamage(10);
        var fight = Combat(state, 0);
        Require(player.Shield == 9 && fight.TakeTurn(CombatAction.ActivateSandevistan).PlayerAction.Valid
            && player.Shield == 9 && fight.TakeTurn(CombatAction.Wait).EnemySkippedBySandevistan
            && player.Shield == 11, "Sandevistan no respetó el inicio de turno del escudo.");
        Require(player.ReceiveDamage(player.CurrentHealth, bypassShield: true) is { Killed: true, ShieldPrevented: 0 },
            "El escudo impidió una ejecución.");
    });
}

void CheckCybergearStash()
{
    WithDatabase((store, original, path) =>
    {
        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var setup = connection.CreateCommand();
            setup.CommandText = """
                DELETE FROM ITEM_CATALOG WHERE kind IN ('implant', 'quickhack');
                INSERT INTO STASH_ENTRY VALUES (303, 2);
                PRAGMA user_version = 4;
                """;
            setup.ExecuteNonQuery();
            RemoveNewFirearms(connection);
        }
        var notices = new List<string>();
        var catalog = store.LoadCatalog(notices.Add);
        var backup = Directory.GetFiles(Path.GetDirectoryName(path)!, $"{Path.GetFileName(path)}.v4-*.bak").Single();
        Require(notices.Single().Contains(backup), "No se anunció la copia de v4.");
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, catalog);
        var player = state.Player!;
        var stash = new StashService(store, catalog);
        var expedition = ExpeditionFor(state, Enumerable.Range(0, 5).Select(_ => Discovery(catalog.Get("curacion"))).ToArray());
        state.StartExpedition(expedition);
        do { expedition.ResolveCurrent(); } while (expedition.Advance());
        state.CompleteExpedition(expedition);
        Require(state.TryBuyQuickhack(CombatAction.Overheat, out _)
            && player.TryConfigureQuickhack(CombatAction.Overheat, true, out _)
            && state.TryDisassemble(player.Equipment.Weapons.Single(), 1, out _)
            && state.TryUpgradeImplant(player.Cyberdeck!, out _), "No se preparó el deck mejorado.");
        var deck = player.Cyberdeck!;
        Require(deck.Tier == 2 && player.QuickhacksOn(deck).Count() == 2
            && !stash.TryDepositImplant(player, deck, out _)
            && !stash.TryDepositQuickhack(player, CombatAction.Overheat, out _)
            && state.TryExtractImplant(CyberwareZone.OperatingSystem, out _)
            && player.QuickhacksOn(deck).Count() == 2, "La extracción o una operación inválida perdió la configuración.");
        var overheat = catalog.Get("sobrecalentamiento").CatalogItemId;
        var shortCircuit = catalog.Get("cortocircuito").CatalogItemId;
        var deckId = catalog.Get("militech-paraline-t2").CatalogItemId;
        store.Deposit(overheat, int.MaxValue);
        Require(!stash.TryDepositImplant(player, deck, out _) && player.SpareCyberware.Contains(deck)
            && player.QuickhacksOn(deck).Count() == 2 && !store.ReadStash(catalog).ContainsKey(deckId)
            && !store.ReadStash(catalog).ContainsKey(shortCircuit), "El depósito parcial no se revirtió.");
        store.Withdraw(overheat, int.MaxValue);
        Require(stash.TryDepositImplant(player, deck, out _) && player.SpareCyberware.Count == 0
            && !player.OwnedQuickhacks.Any(), "La separación no retiró equipo y quickhacks.");

        var reopen = new SqliteGameStore(path);
        var restored = reopen.LoadCatalog(notices.Add);
        var restoredStash = new StashService(reopen, restored);
        var entries = reopen.ReadStash(restored);
        Require(notices.Count == 1 && entries[303] == 2 && entries[deckId] == 1
            && entries[overheat] == 1 && entries[shortCircuit] == 1, "Se perdió un depósito al reabrir.");
        var next = new GameState();
        next.CreateCharacter(OriginPath.Nomad, restored);
        var recipient = next.Player!;
        Require(!restoredStash.TryWithdraw(recipient, shortCircuit, 1, out _)
            && reopen.ReadStash(restored)[shortCircuit] == 1
            && restoredStash.TryWithdraw(recipient, deckId, 1, out _)
            && restoredStash.TryWithdraw(recipient, overheat, 1, out _)
            && !recipient.IsQuickhackAssigned(CombatAction.Overheat),
            "Una retirada duplicó quickhacks o los asignó automáticamente.");
        var received = recipient.SpareCyberware.Single();
        Require(received.Tier == 2 && recipient.Cyberdeck?.Tier == 1 && !recipient.QuickhacksOn(received).Any()
            && restoredStash.TryDepositQuickhack(recipient, CombatAction.Overheat, out _)
            && !recipient.OwnsQuickhack(CombatAction.Overheat), "El deck volvió configurado o se perdió el quickhack suelto.");
        var startingDeck = recipient.Cyberdeck!;
        var medicine = recipient.Inventory.Items.OfType<Consumable>().Single(item => item.Type == ConsumableType.Healing);
        Require(next.TrySellToShop(medicine, 2, out _)
            && next.TryExtractImplant(CyberwareZone.OperatingSystem, out _)
            && next.TryInstallImplant(received, out _)
            && !recipient.EquippedQuickhacks.Any() && recipient.QuickhacksOn(startingDeck).Single().Action == CombatAction.ShortCircuit
            && !recipient.TryConfigureQuickhack(CombatAction.ShortCircuit, true, out _),
            "El deck retirado heredó la configuración de otra instancia.");
    });
}

void CheckWeaponTierMigration()
{
    WithDatabase((store, original, path) =>
    {
        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var setup = connection.CreateCommand();
            setup.CommandText = """
                DELETE FROM WEAPON_DEFINITION WHERE catalog_item_id IN (SELECT catalog_item_id FROM ITEM_CATALOG WHERE tier > 1);
                DELETE FROM ITEM_CATALOG WHERE tier > 1;
                DELETE FROM ITEM_CATALOG WHERE kind IN ('accessory', 'component', 'implant', 'quickhack');
                INSERT INTO STASH_ENTRY VALUES (104, 2);
                PRAGMA user_version = 1;
                """;
            setup.ExecuteNonQuery();
            RemoveNewFirearms(connection);
        }
        var notices = new List<string>();
        var migrated = store.LoadCatalog(notices.Add);
        var backupPath = Directory.GetFiles(Path.GetDirectoryName(path)!, $"{Path.GetFileName(path)}.v1-*.bak").Single();
        Require(notices.Single().Contains(backupPath) && store.ReadStash(migrated)[104] == 2,
            "No se anunció la copia o no sobrevivió el stash original.");
        using (var backup = new SqliteConnection($"Data Source={backupPath};Mode=ReadOnly;Pooling=False"))
        {
            backup.Open();
            using var query = backup.CreateCommand();
            query.CommandText = "PRAGMA user_version";
            Require(Convert.ToInt32(query.ExecuteScalar()) == 1, "La copia no conserva la versión anterior.");
            query.CommandText = "SELECT quantity FROM STASH_ENTRY WHERE catalog_item_id = 104";
            Require(Convert.ToInt32(query.ExecuteScalar()) == 2, "La copia no contiene los depósitos previos.");
        }
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, migrated);
        var player = state.Player!;
        var extra = new ItemFactory(migrated).CreateWeapon("subfusil-t1");
        player.Inventory.Add(extra);
        var weapon = player.Equipment.Weapons.Single();
        var stash = new StashService(store, migrated);
        Require(state.TryDisassemble(extra, 1, out _) && state.TryUpgradeWeapon(weapon, out _)
            && stash.TryDeposit(player, weapon, 1, out _), "No se pudo depositar el arma mejorada.");
        var reopened = new SqliteGameStore(path);
        var restored = reopened.LoadCatalog(notices.Add);
        var next = new GameState();
        next.CreateCharacter(OriginPath.Nomad, restored);
        Require(notices.Count == 1 && reopened.ReadStash(restored)[104] == 2
            && new StashService(reopened, restored).TryWithdraw(next.Player!, weapon.CatalogItemId, 1, out _)
            && next.Player!.Inventory.Items.OfType<Weapon>().Any(item => item is
                { WeaponDefinition: { Code: "pistola-t2", Tier: 2, Damage: 14 }, LoadedAmmo: 0 }),
            "La reapertura migró de nuevo o perdió el arma mejorada o su tier.");
    });
}

void CheckSqliteStartup()
{
    WithDatabase((store, fromDb, path) =>
    {
        var first = new GameState();
        first.CreateCharacter(OriginPath.Street, fromDb);
        var character = first.Player!;
        var weapon = character.Equipment.Weapons.Single();
        Require(weapon.MagazineCapacity == 30 && weapon.WeaponDefinition.Damage == 10,
            "La primera sesión no recibió las estadísticas de SQLite.");
        var stash = new StashService(store, fromDb);
        var healing = character.Inventory.Items.OfType<Consumable>()
            .Single(item => item.Type == ConsumableType.Healing);
        Require(stash.TryDeposit(character, healing, 1, out _) && healing.Quantity == 1,
            "No se depositó una curación.");
        character.ReceiveDamage(character.MaxHealth);
        first.RemoveDeadCharacter(character);
        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var change = connection.CreateCommand();
            change.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table'";
            Require(Convert.ToInt32(change.ExecuteScalar()) == 5,
                "El modelo reducido no contiene las cinco tablas esperadas.");
            change.CommandText = """
                UPDATE WEAPON_DEFINITION SET base_damage = 15 WHERE catalog_item_id = 3;
                UPDATE ITEM_EFFECT SET magnitude = 30 WHERE catalog_item_id = 104;
                """;
            change.ExecuteNonQuery();
        }
        var reopened = new SqliteGameStore(path);
        var restoredCatalog = reopened.LoadCatalog();
        Require(new GameState().Player is null
            && ((ConsumableDefinition)restoredCatalog.Get("curacion")).HealAmount == 30
            && ((WeaponDefinition)restoredCatalog.Get("escopeta-corredera-t1")).Damage == 15
            && new StashService(reopened, restoredCatalog).Entries()
                .Single().Quantity == 1, "Se guardó el personaje o se perdió el stash.");
        var second = new GameState();
        second.CreateCharacter(OriginPath.Nomad, restoredCatalog);
        var secondHealing = second.Player!.Inventory.Items.OfType<Consumable>()
            .Single(item => item.Type == ConsumableType.Healing);
        var withdrawn = new StashService(reopened, restoredCatalog)
            .TryWithdraw(second.Player, 104, 1, out var error);
        Require(withdrawn
            && secondHealing.Quantity == 3
            && second.Player.Inventory.Items.OfType<Consumable>()
                .Count(item => item.Type == ConsumableType.Healing) == 1
            && !new StashService(reopened, restoredCatalog).Entries().Any(),
            $"La retirada no se sumó a la pila del nuevo personaje: {error}");
    });
}

void CheckStashTransfers()
{
    WithDatabase((store, fromDb, _) =>
    {
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, fromDb);
        var player = state.Player!;
        var stash = new StashService(store, fromDb);
        var weapon = player.Equipment.Weapons.Single();
        var oldId = weapon.RuntimeId;
        var reserve = player.Inventory.Items.OfType<Consumable>()
            .Single(item => item.Type == ConsumableType.Ammunition);
        Require(stash.TryDeposit(player, weapon, 1, out _)
            && !player.Inventory.Contains(weapon) && player.Equipment.Weapons.Count == 0
            && weapon.LoadedAmmo == 0
            && reserve.Quantity == 12, "El depósito no retiró arma/equipo o modificó reserva.");
        var entries = stash.Entries();
        Require(entries.Count == 2
            && entries.Single(e => e.Definition.CatalogItemId == 1).Quantity == 1
            && entries.Single(e => e.Definition.CatalogItemId == 101).Quantity == 12,
            "El arma y sus balas no quedaron separadas en el stash.");
        Require(stash.TryDeposit(player, reserve, 5, out _)
            && reserve.Quantity == 7 && stash.Entries()
                .Single(e => e.Definition.CatalogItemId == 101).Quantity == 17,
            "Depósito parcial de reserva incorrecto.");
        var removedWeapon = stash.TryWithdraw(player, 1, 1, out var weaponError);
        var removedAmmo = stash.TryWithdraw(player, 101, 2, out var ammoError);
        Require(removedWeapon && removedAmmo
            && reserve.Quantity == 9 && player.Inventory.Items.OfType<Consumable>()
                .Count(item => item.CatalogItemId == 101) == 1,
            $"Retirada duplicó pila de reserva: {weaponError} {ammoError}");
        var replacement = player.Inventory.Items.OfType<Weapon>().Single();
        Require(replacement.RuntimeId != oldId && replacement.LoadedAmmo == 0
            && player.Equipment.Weapons.Count == 0, "El arma retirada recuperó identidad, carga o equipo.");
        store.Deposit(1, 2);
        Require(stash.TryWithdraw(player, 1, 2, out _)
            && player.Inventory.Items.OfType<Weapon>().DistinctBy(item => item.RuntimeId).Count() == 3
            && player.Inventory.Items.OfType<Weapon>().All(item => item.LoadedAmmo == 0),
            "Varias armas recuperadas no mantuvieron identidades independientes.");
    });
}

void CheckStashRejections()
{
    WithDatabase((store, fromDb, _) =>
    {
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, fromDb);
        var player = state.Player!;
        var stash = new StashService(store, fromDb);
        var weapon = player.Equipment.Weapons.Single();
        var reserve = player.Inventory.Items.OfType<Consumable>()
            .Single(item => item.Type == ConsumableType.Ammunition);
        Require(!stash.TryDeposit(player, weapon, 2, out _)
            && !stash.TryDeposit(player, reserve, 0, out _)
            && !stash.TryDeposit(player, reserve, 13, out _)
            && !stash.TryWithdraw(player, 101, 1, out _)
            && stash.Entries().Count == 0
            && player.Inventory.Contains(weapon) && reserve.Quantity == 12,
            "Un rechazo alteró inventario o stash.");
        store.Deposit(101, int.MaxValue - 11);
        var before = player.Inventory.Items.ToArray();
        Require(!stash.TryDeposit(player, weapon, 1, out _)
            && stash.Entries().Single().Quantity == int.MaxValue - 11
            && player.Inventory.Items.SequenceEqual(before)
            && player.Equipment.Weapons.Single() == weapon,
            "El depósito de arma excedió el límite y perdió equipo.");
        Throws<InvalidOperationException>(() => store.Deposit(1, 1, 101, 12));
        Require(!stash.Entries().Any(entry => entry.Definition.CatalogItemId == 1),
            "La transacción dejó el arma sin sus balas.");
        var full = new ItemFactory(fromDb).CreateConsumable(101, int.MaxValue - 12);
        Require(player.TryDiscard(reserve, reserve.Quantity, out _), "No se pudo preparar la pila límite.");
        player.Inventory.Add(full);
        Require(!stash.TryWithdraw(player, 101, 13, out _)
            && full.Quantity == int.MaxValue - 12
            && stash.Entries().Single().Quantity == int.MaxValue - 11,
            "Retirada con desbordamiento cambió el stash.");
        Require(stash.TryWithdraw(player, 101, 12, out _)
            && full.Quantity == int.MaxValue
            && stash.Entries().Single().Quantity == int.MaxValue - 23,
            "Retirada al límite no conservó la pila.");
    });
}

void CheckSqliteCorruption()
{
    WithDatabase((store, fromDb, path) =>
    {
        store.Deposit(104, 2);
        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM CONSUMABLE_DEFINITION WHERE catalog_item_id = 101";
            command.ExecuteNonQuery();
        }
        var size = new FileInfo(path).Length;
        Throws<InvalidDataException>(() => new SqliteGameStore(path).LoadCatalog());
        Require(new FileInfo(path).Length == size
            && store.ReadStash(fromDb).Single().Value == 2,
            "Se reemplazó la base inválida.");
    });
    var root = OperatingSystem.IsLinux() ? "/tmp/opencode" : Path.GetTempPath();
    var path = Path.Combine(root, $"mysharpventure-broken-{Guid.NewGuid():N}.db");
    try
    {
        File.WriteAllText(path, "base ajena: conservar");
        Throws<InvalidDataException>(() => new SqliteGameStore(path).LoadCatalog());
        Require(File.ReadAllText(path) == "base ajena: conservar",
            "Se sobrescribió un archivo de base existente.");
    }
    finally
    {
        SqliteConnection.ClearAllPools();
        File.Delete(path);
    }
}

sealed class FixedRandom(double value) : Random
{
    public override double NextDouble() => value;
    public override int Next(int maxValue) => maxValue - 1;
}

sealed class CriticalRandom(double accuracyRoll, int criticalRoll) : Random
{
    public int CriticalRolls { get; private set; }
    public override double NextDouble() => accuracyRoll;
    public override int Next(int maxValue)
    {
        CriticalRolls++;
        return criticalRoll;
    }
}

sealed class CountingRandom(double value) : Random
{
    public override int Next(int maxValue) => maxValue - 1;
    public int Calls { get; private set; }
    public override double NextDouble()
    {
        Calls++;
        return value;
    }
}
