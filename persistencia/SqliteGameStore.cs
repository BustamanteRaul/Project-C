using Microsoft.Data.Sqlite;

namespace MySharpVenture;

/// <summary>Catálogo de objetos y cantidades del stash en la base local.</summary>
public sealed class SqliteGameStore(string path)
{
    private const int CatalogVersion = 12;
    private readonly string _path = Path.GetFullPath(path);

    /// <summary>Inicializa una base nueva o valida y carga una existente sin reemplazarla.</summary>
    public ItemCatalog LoadCatalog(Action<string>? onCatalogUpgrade = null)
    {
        var isNew = !File.Exists(_path);
        if (isNew)
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        try
        {
            using var connection = Connect(isNew);
            if (isNew)
            {
                using var transaction = connection.BeginTransaction();
                using var create = connection.CreateCommand();
                create.Transaction = transaction;
                create.CommandText = """
                    CREATE TABLE ITEM_CATALOG (
                        catalog_item_id INTEGER PRIMARY KEY,
                        code TEXT NOT NULL UNIQUE,
                        name TEXT NOT NULL,
                        kind TEXT NOT NULL CHECK (kind IN ('weapon', 'consumable')),
                        tier INTEGER CHECK (tier BETWEEN 1 AND 5),
                        can_stash INTEGER NOT NULL CHECK (can_stash IN (0, 1))
                    );
                    CREATE TABLE CONSUMABLE_DEFINITION (
                        catalog_item_id INTEGER PRIMARY KEY REFERENCES ITEM_CATALOG(catalog_item_id),
                        subtype TEXT NOT NULL CHECK (subtype IN ('Ammunition', 'Healing'))
                    );
                    CREATE TABLE WEAPON_DEFINITION (
                        catalog_item_id INTEGER PRIMARY KEY REFERENCES ITEM_CATALOG(catalog_item_id),
                        variant TEXT NOT NULL,
                        base_damage INTEGER NOT NULL CHECK (base_damage > 0),
                        base_hit_chance REAL NOT NULL CHECK (base_hit_chance BETWEEN 0 AND 1),
                        magazine_capacity INTEGER NOT NULL CHECK (magazine_capacity > 0),
                        ammo_catalog_id INTEGER NOT NULL REFERENCES ITEM_CATALOG(catalog_item_id),
                        ammo_per_attack INTEGER NOT NULL CHECK (ammo_per_attack > 0)
                    );
                    CREATE TABLE ITEM_EFFECT (
                        catalog_item_id INTEGER NOT NULL REFERENCES ITEM_CATALOG(catalog_item_id),
                        effect_order INTEGER NOT NULL,
                        effect_code TEXT NOT NULL,
                        magnitude INTEGER NOT NULL,
                        PRIMARY KEY (catalog_item_id, effect_order)
                    );
                    CREATE TABLE STASH_ENTRY (
                        catalog_item_id INTEGER PRIMARY KEY REFERENCES ITEM_CATALOG(catalog_item_id),
                        quantity INTEGER NOT NULL CHECK (quantity > 0 AND quantity <= 2147483647)
                    );
                    INSERT INTO ITEM_CATALOG VALUES
                        (1, 'pistola-t1', 'Pistola', 'weapon', 1, 1),
                        (2, 'subfusil-t1', 'Subfusil', 'weapon', 1, 1),
                        (3, 'escopeta-corredera-t1', 'Escopeta de corredera', 'weapon', 1, 1),
                        (101, 'municion-pistola', 'Munición de pistola', 'consumable', NULL, 1),
                        (102, 'municion-subfusil', 'Munición de subfusil', 'consumable', NULL, 1),
                        (103, 'cartuchos-escopeta', 'Cartuchos de escopeta', 'consumable', NULL, 1),
                        (104, 'curacion', 'Curación', 'consumable', NULL, 1);
                    INSERT INTO WEAPON_DEFINITION VALUES
                        (1, 'Pistol', 12, 0.85, 12, 101, 1),
                        (2, 'SubmachineGun', 10, 0.85, 30, 102, 1),
                        (3, 'PumpShotgun', 18, 0.75, 6, 103, 1);
                    INSERT INTO CONSUMABLE_DEFINITION VALUES
                        (101, 'Ammunition'), (102, 'Ammunition'),
                        (103, 'Ammunition'), (104, 'Healing');
                    INSERT INTO ITEM_EFFECT VALUES (104, 1, 'heal', 25);
                    PRAGMA user_version = 1;
                    """;
                create.ExecuteNonQuery();
                transaction.Commit();
            }

            var version = ValidateDatabase(connection);
            var catalog = ReadCatalog(connection);
            ReadStash(catalog);
            if (version >= 2)
                ValidateWeaponTiers(catalog, version);
            if (version >= 3)
                ValidateAccessories(catalog);
            if (version >= 4)
                ValidateComponents(catalog);
            if (version >= 5)
                ValidateCybergear(catalog, version);
            if (version < CatalogVersion)
            {
                var additions = version == 1 ? WeaponUpgradeContent.StartingCodes
                    .SelectMany(code => WeaponUpgradeContent.HigherTiers((WeaponDefinition)catalog.Get(code)))
                    .ToArray() : [];
                if (version < 9)
                    additions = additions.Concat(WeaponUpgradeContent.ExtraFirearms.SelectMany(WeaponUpgradeContent.AllTiers)).ToArray();
                if (version < 10)
                    additions = additions.Concat(WeaponUpgradeContent.MeleeWeapons.SelectMany(WeaponUpgradeContent.AllTiers)).ToArray();
                ItemDefinition[] supplies =
                [
                    .. version < 3 ? new[] { AccessoryDefinition.Magazine, AccessoryDefinition.Sight } : [],
                    .. version < 4 ? new[] { ComponentDefinition.Workshop } : [],
                    .. CybergearStashContent.All.Where(gear => version < CybergearVersion(gear)),
                    .. version < 9 ? new[] { WeaponUpgradeContent.RifleAmmo } : []
                ];
                _ = new ItemCatalog(catalog.Definitions.Concat(additions).Concat(supplies));
                if (!isNew)
                {
                    var backupPath = $"{_path}.v{version}-{Guid.NewGuid():N}.bak";
                    using (File.Open(backupPath, FileMode.CreateNew)) { }
                    using (var backup = new SqliteConnection(new SqliteConnectionStringBuilder
                    {
                        DataSource = backupPath, Pooling = false
                    }.ToString()))
                    {
                        backup.Open();
                        connection.BackupDatabase(backup);
                    }
                    onCatalogUpgrade?.Invoke($"Se actualizará el catálogo. Copia de seguridad: {backupPath}");
                }
                UpgradeCatalog(connection, version, additions, supplies);
                catalog = ReadCatalog(connection);
            }
            ValidateAccessories(catalog);
            ValidateComponents(catalog);
            ValidateCybergear(catalog, CatalogVersion);
            ValidateWeaponTiers(catalog, CatalogVersion);
            return catalog;
        }
        catch (Exception error) when (error is SqliteException or ArgumentException
            or InvalidOperationException or OverflowException or InvalidCastException)
        {
            throw new InvalidDataException(
                $"No se pudo validar la base de datos «{_path}». No se borró ni reemplazó. {error.Message}",
                error);
        }
    }

    private static int CybergearVersion(CybergearStashDefinition gear) => gear.CatalogItemId switch
    {
        461 => 6,
        462 => 7,
        463 or 464 or 465 => 8,
        >= 631 and <= 635 => 12,
        >= 470 or (>= 432 and <= 435) or (>= 442 and <= 445) => 11,
        _ => 5
    };

    private static void ValidateCybergear(ItemCatalog catalog, int version)
    {
        foreach (var gear in CybergearStashContent.All)
            if (version >= CybergearVersion(gear) && catalog.Get(gear.CatalogItemId) != gear)
                throw new InvalidDataException($"Falta {gear.Code} en el catálogo.");
    }

    private static void ValidateComponents(ItemCatalog catalog)
    {
        if (catalog.Get(ComponentDefinition.Workshop.CatalogItemId) != ComponentDefinition.Workshop)
            throw new InvalidDataException("Faltan los componentes del catálogo.");
    }

    private static void ValidateAccessories(ItemCatalog catalog)
    {
        if (catalog.Get(AccessoryDefinition.Magazine.CatalogItemId) != AccessoryDefinition.Magazine
            || catalog.Get(AccessoryDefinition.Sight.CatalogItemId) != AccessoryDefinition.Sight)
            throw new InvalidDataException("Faltan los accesorios del catálogo.");
    }

    private static void ValidateWeaponTiers(ItemCatalog catalog, int version)
    {
        var codes = WeaponUpgradeContent.StartingCodes.AsEnumerable();
        if (version >= 9)
            codes = codes.Concat(WeaponUpgradeContent.ExtraFirearms.Select(weapon => weapon.Code));
        if (version >= 10)
            codes = codes.Concat(WeaponUpgradeContent.MeleeWeapons.Select(weapon => weapon.Code));
        foreach (var code in codes)
        {
            var tier = (WeaponDefinition)catalog.Get(code);
            for (var level = 2; level <= 5; level++)
                tier = catalog.NextWeaponTier(tier)
                    ?? throw new InvalidDataException($"Falta un tier válido para {code} (Tier {level}).");
        }
    }

    private static void UpgradeCatalog(SqliteConnection connection, int version,
        IEnumerable<WeaponDefinition> additions, IEnumerable<ItemDefinition> supplies)
    {
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = OFF";
        pragma.ExecuteNonQuery();
        try
        {
            Upgrade();
        }
        finally
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON";
            pragma.ExecuteNonQuery();
        }

        void Upgrade()
        {
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "PRAGMA user_version";
            if (Convert.ToInt32(command.ExecuteScalar()) != version)
                return; // Otra sesión ya actualizó el catálogo.

            if (version < 5)
            {
                command.CommandText = """
                    CREATE TABLE ITEM_CATALOG_NEW (
                        catalog_item_id INTEGER PRIMARY KEY,
                        code TEXT NOT NULL UNIQUE,
                        name TEXT NOT NULL,
                        kind TEXT NOT NULL CHECK (kind IN ('weapon', 'consumable', 'accessory', 'component', 'implant', 'quickhack')),
                        tier INTEGER CHECK (tier BETWEEN 1 AND 5),
                        can_stash INTEGER NOT NULL CHECK (can_stash IN (0, 1))
                    );
                    INSERT INTO ITEM_CATALOG_NEW SELECT catalog_item_id, code, name, kind, tier, can_stash FROM ITEM_CATALOG;
                    DROP TABLE ITEM_CATALOG;
                    ALTER TABLE ITEM_CATALOG_NEW RENAME TO ITEM_CATALOG;
                    """;
                command.ExecuteNonQuery();
            }
            if (version < 10)
            {
                command.CommandText = """
                    CREATE TABLE WEAPON_DEFINITION_NEW (
                        catalog_item_id INTEGER PRIMARY KEY REFERENCES ITEM_CATALOG(catalog_item_id),
                        variant TEXT NOT NULL,
                        base_damage INTEGER NOT NULL CHECK (base_damage > 0),
                        base_hit_chance REAL NOT NULL CHECK (base_hit_chance BETWEEN 0 AND 1),
                        magazine_capacity INTEGER NOT NULL,
                        ammo_catalog_id INTEGER REFERENCES ITEM_CATALOG(catalog_item_id),
                        ammo_per_attack INTEGER NOT NULL,
                        CHECK ((variant IN ('Sword', 'Bat') AND magazine_capacity = 0
                                AND ammo_catalog_id IS NULL AND ammo_per_attack = 0)
                            OR (variant NOT IN ('Sword', 'Bat') AND magazine_capacity > 0
                                AND ammo_catalog_id IS NOT NULL AND ammo_per_attack BETWEEN 1 AND magazine_capacity))
                    );
                    INSERT INTO WEAPON_DEFINITION_NEW SELECT * FROM WEAPON_DEFINITION;
                    DROP TABLE WEAPON_DEFINITION;
                    ALTER TABLE WEAPON_DEFINITION_NEW RENAME TO WEAPON_DEFINITION;
                    """;
                command.ExecuteNonQuery();
            }
            foreach (var supply in supplies)
            {
                command.Parameters.Clear();
                command.CommandText = "INSERT INTO ITEM_CATALOG VALUES ($id, $code, $name, $kind, $tier, 1)";
                command.Parameters.AddWithValue("$id", supply.CatalogItemId);
                command.Parameters.AddWithValue("$code", supply.Code);
                command.Parameters.AddWithValue("$name", supply.Name);
                command.Parameters.AddWithValue("$kind", supply switch
                {
                    ComponentDefinition => "component",
                    ConsumableDefinition => "consumable",
                    CybergearStashDefinition { Implant: not null } => "implant",
                    CybergearStashDefinition => "quickhack",
                    _ => "accessory"
                });
                command.Parameters.AddWithValue("$tier", (object?)supply.Tier ?? DBNull.Value);
                command.ExecuteNonQuery();
                if (supply is ConsumableDefinition consumable)
                {
                    command.CommandText = "INSERT INTO CONSUMABLE_DEFINITION VALUES ($id, $subtype)";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("$id", consumable.CatalogItemId);
                    command.Parameters.AddWithValue("$subtype", consumable.Type.ToString());
                    command.ExecuteNonQuery();
                }
            }
            InsertWeaponTiers(command, additions);
            command.Parameters.Clear();
            command.CommandText = "PRAGMA foreign_key_check";
            using (var reader = command.ExecuteReader())
                if (reader.Read())
                    throw new InvalidDataException("La actualización dejaría relaciones inválidas.");
            command.CommandText = $"PRAGMA user_version = {CatalogVersion}";
            command.ExecuteNonQuery();
            transaction.Commit();
        }
    }

    private static void InsertWeaponTiers(SqliteCommand command, IEnumerable<WeaponDefinition> additions)
    {
        foreach (var weapon in additions)
        {
            command.CommandText = """
                INSERT INTO ITEM_CATALOG VALUES ($id, $code, $name, 'weapon', $tier, 1);
                INSERT INTO WEAPON_DEFINITION VALUES ($id, $variant, $damage, $accuracy, $magazine, $ammo, $cost);
                """;
            command.Parameters.Clear();
            command.Parameters.AddWithValue("$id", weapon.CatalogItemId);
            command.Parameters.AddWithValue("$code", weapon.Code);
            command.Parameters.AddWithValue("$name", weapon.Name);
            command.Parameters.AddWithValue("$tier", weapon.Tier!.Value);
            command.Parameters.AddWithValue("$variant", weapon.Variant.ToString());
            command.Parameters.AddWithValue("$damage", weapon.Damage);
            command.Parameters.AddWithValue("$accuracy", (double)weapon.Accuracy);
            command.Parameters.AddWithValue("$magazine", weapon.MagazineCapacity);
            command.Parameters.AddWithValue("$ammo", weapon.IsMelee ? DBNull.Value : (object)weapon.AmmoCatalogItemId);
            command.Parameters.AddWithValue("$cost", weapon.AmmoPerAttack);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>Lee las cantidades persistidas; las filas inválidas producen un error.</summary>
    public IReadOnlyDictionary<int, int> ReadStash(ItemCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        using var connection = Connect(create: false);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT catalog_item_id, quantity FROM STASH_ENTRY";
        using var reader = command.ExecuteReader();
        var entries = new Dictionary<int, int>();
        while (reader.Read())
        {
            var id = checked((int)reader.GetInt64(0));
            var quantity = checked((int)reader.GetInt64(1));
            if (quantity <= 0)
                throw new InvalidDataException("El stash contiene una cantidad inválida.");
            catalog.Get(id);
            entries.Add(id, quantity);
        }
        return entries;
    }

    /// <summary>Guarda un depósito y, opcionalmente, las balas cargadas en una transacción.</summary>
    public void Deposit(int itemId, int quantity, int? ammoId = null, int ammoQuantity = 0,
        bool magazine = false, bool sight = false, IReadOnlyList<int>? separatedQuickhacks = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(ammoQuantity);
        if (ammoQuantity > 0 && ammoId is null)
            throw new ArgumentException("Falta la definición de la munición.", nameof(ammoId));
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction();
        Increase(itemId, quantity);
        if (ammoId is not null && ammoQuantity > 0)
            Increase(ammoId.Value, ammoQuantity);
        if (magazine)
            Increase(AccessoryDefinition.Magazine.CatalogItemId, 1);
        if (sight)
            Increase(AccessoryDefinition.Sight.CatalogItemId, 1);
        if (separatedQuickhacks is not null)
            foreach (var id in separatedQuickhacks)
                Increase(id, 1);
        transaction.Commit();

        void Increase(int id, int amount)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO STASH_ENTRY (catalog_item_id, quantity) VALUES ($id, $amount)
                ON CONFLICT(catalog_item_id) DO UPDATE SET quantity = quantity + excluded.quantity
                WHERE quantity <= 2147483647 - excluded.quantity
                """;
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue("$amount", amount);
            if (command.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("La cantidad del stash supera el máximo permitido.");
        }
    }

    /// <summary>Retira una cantidad de manera atómica, sin dejar filas vacías.</summary>
    public void Withdraw(int itemId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction();
        using var delete = connection.CreateCommand();
        delete.Transaction = transaction;
        delete.CommandText = """
            DELETE FROM STASH_ENTRY WHERE catalog_item_id = $id AND quantity = $amount
            """;
        delete.Parameters.AddWithValue("$id", itemId);
        delete.Parameters.AddWithValue("$amount", quantity);
        if (delete.ExecuteNonQuery() == 0)
        {
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE STASH_ENTRY SET quantity = quantity - $amount
                WHERE catalog_item_id = $id AND quantity > $amount
                """;
            update.Parameters.AddWithValue("$id", itemId);
            update.Parameters.AddWithValue("$amount", quantity);
            if (update.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("El stash ya no tiene esa cantidad.");
        }
        transaction.Commit();
    }

    private SqliteConnection Connect(bool create)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _path,
            Mode = create ? SqliteOpenMode.ReadWriteCreate : SqliteOpenMode.ReadWrite
        }.ToString());
        try
        {
            connection.Open();
            using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys = ON";
            pragma.ExecuteNonQuery();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static int ValidateDatabase(SqliteConnection connection)
    {
        using var check = connection.CreateCommand();
        check.CommandText = "PRAGMA user_version";
        var version = Convert.ToInt32(check.ExecuteScalar());
        if (version is < 1 or > CatalogVersion)
            throw new InvalidDataException("La versión del esquema es desconocida.");
        check.CommandText = "PRAGMA integrity_check";
        if ((string?)check.ExecuteScalar() != "ok")
            throw new InvalidDataException("La base de datos no superó la verificación de integridad.");
        check.CommandText = "PRAGMA foreign_key_check";
        using var reader = check.ExecuteReader();
        if (reader.Read())
            throw new InvalidDataException("La base de datos contiene relaciones inválidas.");
        return version;
    }

    private static ItemCatalog ReadCatalog(SqliteConnection connection)
    {
        var heals = new Dictionary<int, int>();
        using (var effects = connection.CreateCommand())
        {
            effects.CommandText = "SELECT catalog_item_id, effect_order, effect_code, magnitude FROM ITEM_EFFECT";
            using var reader = effects.ExecuteReader();
            while (reader.Read())
            {
                var id = checked((int)reader.GetInt64(0));
                if (reader.GetInt64(1) != 1 || reader.GetString(2) != "heal"
                    || !heals.TryAdd(id, checked((int)reader.GetInt64(3)))
                    || heals[id] <= 0)
                    throw new InvalidDataException("El catálogo contiene efectos desconocidos o inválidos.");
            }
        }

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT i.catalog_item_id, i.code, i.name, i.kind, i.tier, i.can_stash,
                   w.catalog_item_id, w.variant, w.base_damage, w.base_hit_chance,
                   w.magazine_capacity, w.ammo_catalog_id, w.ammo_per_attack,
                   c.catalog_item_id, c.subtype
            FROM ITEM_CATALOG i
            LEFT JOIN WEAPON_DEFINITION w ON w.catalog_item_id = i.catalog_item_id
            LEFT JOIN CONSUMABLE_DEFINITION c ON c.catalog_item_id = i.catalog_item_id
            """;
        using var rows = command.ExecuteReader();
        var definitions = new List<ItemDefinition>();
        while (rows.Read())
        {
            var id = checked((int)rows.GetInt64(0));
            var code = rows.GetString(1);
            var name = rows.GetString(2);
            var tier = rows.IsDBNull(4) ? (int?)null : checked((int)rows.GetInt64(4));
            if (rows.GetInt64(5) != 1)
                throw new InvalidDataException("El catálogo reducido solo admite objetos que pueden guardarse.");
            ItemDefinition definition;
            if (rows.GetString(3) == "weapon" && !rows.IsDBNull(6) && rows.IsDBNull(13)
                && Enum.TryParse<WeaponVariant>(rows.GetString(7), out var variant)
                && Enum.IsDefined(variant) && !heals.ContainsKey(id))
                definition = new WeaponDefinition(id, code, name, tier, variant,
                     checked((int)rows.GetInt64(10)), rows.IsDBNull(11) ? 0 : checked((int)rows.GetInt64(11)),
                    checked((int)rows.GetInt64(8)), Convert.ToDecimal(rows.GetDouble(9)),
                    checked((int)rows.GetInt64(12)));
            else if (rows.GetString(3) == "consumable" && rows.IsDBNull(6) && !rows.IsDBNull(13)
                && Enum.TryParse<ConsumableType>(rows.GetString(14), out var type)
                && Enum.IsDefined(type) && (type == ConsumableType.Healing) == heals.ContainsKey(id))
                definition = new ConsumableDefinition(id, code, name, type,
                    heals.GetValueOrDefault(id));
            else if (rows.GetString(3) == "accessory" && rows.IsDBNull(6) && rows.IsDBNull(13)
                && tier is null && !heals.ContainsKey(id))
                definition = new AccessoryDefinition(id, code, name);
            else if (rows.GetString(3) == "component" && rows.IsDBNull(6) && rows.IsDBNull(13)
                && tier is null && !heals.ContainsKey(id))
                definition = new ComponentDefinition(id, code, name);
            else if (rows.GetString(3) is "implant" or "quickhack" && rows.IsDBNull(6) && rows.IsDBNull(13)
                && !heals.ContainsKey(id) && CybergearStashContent.All.FirstOrDefault(gear => gear.CatalogItemId == id
                    && gear.Code == code && gear.Name == name && gear.Tier == tier
                    && (gear.Implant is not null) == (rows.GetString(3) == "implant")) is { } gear)
                definition = gear;
            else
                throw new InvalidDataException($"Definición de objeto inválida: {id}.");
            definitions.Add(definition);
        }
        if (heals.Count != definitions.OfType<ConsumableDefinition>()
            .Count(item => item.Type == ConsumableType.Healing))
            throw new InvalidDataException("Hay efectos sin definición de curación.");
        var catalog = new ItemCatalog(definitions);
        foreach (var code in WeaponUpgradeContent.StartingCodes)
            if (catalog.Get(code) is not WeaponDefinition { Tier: 1 })
                throw new InvalidDataException($"Falta el arma inicial {code}.");
        if (catalog.Get("curacion") is not ConsumableDefinition { Type: ConsumableType.Healing })
            throw new InvalidDataException("Falta la curación inicial.");
        return catalog;
    }
}
