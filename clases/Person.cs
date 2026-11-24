namespace MySharpVenture;

public enum OriginPath
{
    Corpo,
    Street,
    Nomad
}

public sealed class Person : Entity
{
    private readonly Dictionary<AttributeType, int> _attributes = Enum.GetValues<AttributeType>()
        .ToDictionary(attribute => attribute, _ => 1);
    private readonly HashSet<string> _skills = new(StringComparer.Ordinal);
    private readonly Dictionary<CyberwareZone, CyberwareDefinition> _installedCyberware = new();
    private readonly List<CyberwareDefinition> _spareCyberware = new();
    private readonly HashSet<CombatAction> _ownedQuickhacks = new();
    private readonly Dictionary<CombatAction, CyberwareDefinition> _configuredQuickhacks = new();

    // Un enemigo humano no necesita tener uno de los orígenes del jugador.
    public OriginPath? Origin { get; }
    /// <summary>Identifica a un combatiente mecánico creado a partir de un perfil enemigo.</summary>
    public bool IsBot { get; internal init; }
    public int Level { get; private set; }
    public int Experience { get; private set; }
    /// <summary>Eurodólares de este personaje; no se guardan en el stash.</summary>
    public int Money { get; private set; }
    /// <summary>Componentes de fabricación transportados durante esta sesión.</summary>
    public int Components { get; internal set; }
    /// <summary>Peso transportado, en gramos, incluidas las balas cargadas y los accesorios.</summary>
    public long CarryWeightGrams => CarryWeight.Grams(this);
    /// <summary>Capacidad de carga en gramos según Fuerza y los implantes activos.</summary>
    public int CarryLimitGrams => 20_000 + 2_000 * Attribute(AttributeType.Strength)
        + ImplantBonus(implant => implant.CarryBonusGrams);
    public bool IsOverweight => CarryWeightGrams > CarryLimitGrams;
    /// <summary>Cargadores ampliados fabricados que no están montados en un arma.</summary>
    public int SpareExtendedMagazines { get; internal set; }
    public int SpareReflexSights { get; internal set; }

    internal void EarnMoney(int amount) => Money = (int)Math.Min(int.MaxValue, Money + (long)amount);

    internal void SpendMoney(int amount) => Money -= amount;
    public int AttributePoints { get; private set; }
    public int SkillPoints { get; private set; }
    public int ExperienceToNextLevel => Level == 20 ? 0 : ProgressionContent.ExperienceForLevel(Level + 1);
    public IReadOnlyCollection<string> Skills => _skills.ToArray();
    public IReadOnlyDictionary<CyberwareZone, CyberwareDefinition> InstalledCyberware =>
        new System.Collections.ObjectModel.ReadOnlyDictionary<CyberwareZone, CyberwareDefinition>(_installedCyberware);
    /// <summary>Implantes propios que aún no están instalados.</summary>
    public IReadOnlyList<CyberwareDefinition> SpareCyberware => _spareCyberware.AsReadOnly();
    public int CyberwareCapacity => CyberwareContent.StartingCapacity;
    public int UsedCyberwareCapacity => _installedCyberware.Values.Sum(implant => implant.CapacityCost);
    internal CyberwareDefinition? InstalledCyberdeck =>
        _installedCyberware.GetValueOrDefault(CyberwareZone.OperatingSystem) is { MaxRam: > 0 } deck ? deck : null;
    public CyberwareDefinition? Cyberdeck => CyberwareDisabledTurns == 0 ? InstalledCyberdeck : null;
    /// <summary>Turnos propios restantes sin efectos de implantes.</summary>
    public int CyberwareDisabledTurns { get; private set; }
    internal int SelfIceTier => CyberwareDisabledTurns == 0
        && _installedCyberware.GetValueOrDefault(CyberwareZone.FrontalCortex) is { } ice
        && ice.Code == CyberwareContent.SelfIce.Code ? ice.Tier : 0;
    public bool HasActiveSelfIce => SelfIceTier > 0;
    internal int ImplantBonus(Func<CyberwareDefinition, int> value) =>
        CyberwareDisabledTurns == 0 ? _installedCyberware.Values.Sum(value) : 0;
    public int MaxRam => Cyberdeck is { } deck
        ? deck.MaxRam + _installedCyberware.Values.Sum(implant => implant.BonusRam) : 0;
    public override int MaxShield => CyberwareDisabledTurns == 0
        ? _installedCyberware.Values.Sum(implant => implant.MaxShield) : 0;
    public int Ram { get; private set; }
    /// <summary>Quickhacks del inventario, en orden de catálogo.</summary>
    public IEnumerable<QuickhackDefinition> OwnedQuickhacks =>
        QuickhackContent.All.Where(hack => _ownedQuickhacks.Contains(hack.Action));

    /// <summary>Quickhacks disponibles a través del deck instalado.</summary>
    public IEnumerable<QuickhackDefinition> EquippedQuickhacks =>
        _installedCyberware.GetValueOrDefault(CyberwareZone.OperatingSystem) is { MaxRam: > 0 } deck
            ? QuickhacksOn(deck) : [];

    /// <summary>Quickhacks asociados a esta instancia, incluso cuando está extraída.</summary>
    public IEnumerable<QuickhackDefinition> QuickhacksOn(CyberwareDefinition deck) =>
        OwnedQuickhacks.Where(hack => ReferenceEquals(_configuredQuickhacks.GetValueOrDefault(hack.Action), deck));

    public Person(string name, int maxHealth, OriginPath? origin = null, int level = 1)
        : base(name, maxHealth)
    {
        if (origin is not null && !Enum.IsDefined(origin.Value))
            throw new ArgumentOutOfRangeException(nameof(origin));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(level);
        if (level > 20)
            throw new ArgumentOutOfRangeException(nameof(level));
        Origin = origin;
        Level = level;
        if (origin is not null)
        {
            AddStartingDeck();
            AttributePoints = StartingKit.AttributePoints;
            SkillPoints = StartingKit.SkillPoints;
            _skills.Add(StartingKit.SkillCode(origin.Value));
        }
    }

    /// <summary>Crea un personaje aislado para practicar con su estado y equipo actuales.</summary>
    /// <returns>Una copia que no modifica al personaje original.</returns>
    public Person CreatePracticeCopy() => new(this);

    private Person(Person source) : base(source)
    {
        Origin = source.Origin;
        IsBot = source.IsBot;
        Level = source.Level;
        Experience = source.Experience;
        Money = source.Money;
        AttributePoints = source.AttributePoints;
        SkillPoints = source.SkillPoints;
        Components = source.Components;
        SpareExtendedMagazines = source.SpareExtendedMagazines;
        SpareReflexSights = source.SpareReflexSights;
        CyberwareDisabledTurns = source.CyberwareDisabledTurns;
        Ram = source.Ram;
        _attributes = new(source._attributes);
        _skills = new(source._skills, StringComparer.Ordinal);
        _spareCyberware = new(source._spareCyberware);
        _installedCyberware = new(source._installedCyberware);
        _ownedQuickhacks = new(source._ownedQuickhacks);
        _configuredQuickhacks = new(source._configuredQuickhacks);
    }

    internal void AddStartingDeck()
    {
        _installedCyberware.Add(CyberwareZone.OperatingSystem, CyberwareContent.StartingDeck with { });
        Ram = MaxRam;
        _ownedQuickhacks.Add(StartingKit.Quickhack);
        _configuredQuickhacks.Add(StartingKit.Quickhack, Cyberdeck!);
    }

    internal void AddNetrunnerCyberware(bool controlQuickhacks)
    {
        AddStartingDeck();
        _installedCyberware.Add(CyberwareZone.FrontalCortex, CyberwareContent.SelfIce);
        _ownedQuickhacks.Add(CombatAction.CyberwareMalfunction);
        _configuredQuickhacks.Add(CombatAction.CyberwareMalfunction, Cyberdeck!);
        if (controlQuickhacks)
        {
            foreach (var action in new[] { CombatAction.WeaponGlitch, CombatAction.RebootOptics })
            {
                _ownedQuickhacks.Add(action);
                _configuredQuickhacks.Add(action, Cyberdeck!);
            }
        }
    }

    internal void DisableCyberware(int turns)
    {
        CyberwareDisabledTurns = turns;
        Ram = Math.Min(Ram, MaxRam);
        ClampShield();
    }

    internal bool TickCyberware() => CyberwareDisabledTurns > 0 && --CyberwareDisabledTurns == 0;

    public bool OwnsQuickhack(CombatAction action) => _ownedQuickhacks.Contains(action);

    public bool IsQuickhackEquipped(CombatAction action) => Cyberdeck is { } deck
        && ReferenceEquals(_configuredQuickhacks.GetValueOrDefault(action), deck);

    public bool IsQuickhackAssigned(CombatAction action) => _configuredQuickhacks.ContainsKey(action);

    internal void ReceiveQuickhack(CombatAction action) => _ownedQuickhacks.Add(action);
    internal void RemoveQuickhack(CombatAction action)
    {
        _configuredQuickhacks.Remove(action);
        _ownedQuickhacks.Remove(action);
    }

    /// <summary>Configura un quickhack propio desde el inventario, fuera del combate.</summary>
    public bool TryConfigureQuickhack(CombatAction action, bool equip, out string error)
    {
        if (CurrentHealth == 0 || Cyberdeck is not { } deck)
            error = "Necesitás un cyberdeck instalado.";
        else if (!OwnsQuickhack(action))
            error = "No tenés ese quickhack en el inventario.";
        else if (equip && IsQuickhackAssigned(action))
            error = "El quickhack ya está configurado en un cyberdeck.";
        else if (!equip && !IsQuickhackEquipped(action))
            error = "El quickhack no está equipado en este cyberdeck.";
        else if (equip && EquippedQuickhacks.Count() >= QuickhackContent.DeckSlots)
            error = "El cyberdeck ya tiene cuatro quickhacks equipados.";
        else
        {
            if (equip)
                _configuredQuickhacks.Add(action, deck);
            else
                _configuredQuickhacks.Remove(action);
            error = string.Empty;
            return true;
        }
        return false;
    }

    /// <summary>Devuelve el motivo por el que un implante no cabe, o null si cabe.</summary>
    public string? CyberwareFitError(CyberwareDefinition implant)
    {
        ArgumentNullException.ThrowIfNull(implant);
        if (_installedCyberware.ContainsKey(implant.Zone))
            return "La zona corporal ya tiene un implante.";
        return implant.CapacityCost > CyberwareCapacity - UsedCyberwareCapacity
            ? "Capacidad de cyberware insuficiente." : null;
    }

    internal void ReceiveCyberware(CyberwareDefinition implant) => _spareCyberware.Add(implant);
    internal void RemoveSpareImplant(CyberwareDefinition implant) =>
        _spareCyberware.RemoveAt(_spareCyberware.FindIndex(candidate => ReferenceEquals(candidate, implant)));

    internal void UpgradeOwnedImplant(CyberwareDefinition current, CyberwareDefinition next)
    {
        foreach (var hack in QuickhacksOn(current).ToArray())
            _configuredQuickhacks[hack.Action] = next;
        if (ReferenceEquals(_installedCyberware.GetValueOrDefault(current.Zone), current))
            _installedCyberware[current.Zone] = next;
        else
            _spareCyberware[_spareCyberware.FindIndex(candidate => ReferenceEquals(candidate, current))] = next;
    }

    internal void InstallOwnedCyberware(CyberwareDefinition implant)
    {
        _installedCyberware.Add(implant.Zone, implant);
        RemoveSpareImplant(implant);
    }

    internal void ExtractCyberware(CyberwareZone zone)
    {
        var implant = _installedCyberware[zone];
        _spareCyberware.Add(implant);
        _installedCyberware.Remove(zone);
        Ram = Math.Min(Ram, MaxRam);
        ClampShield();
    }

    /// <summary>Gasta RAM al ejecutar un quickhack validado.</summary>
    public void SpendRam(int cost)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cost);
        if (Cyberdeck is null || CurrentHealth == 0 || Ram < cost)
            throw new InvalidOperationException("RAM insuficiente o cyberdeck no instalado.");
        Ram -= cost;
    }

    /// <summary>Recupera RAM por turno, sin exceder el máximo del deck instalado.</summary>
    public void RegenerateRam() => Ram = RamAfterRecovery();

    internal int RamAfterRecovery(int turns = 1) =>
        (int)Math.Min(MaxRam, Ram + (long)(Cyberdeck?.RamPerTurn ?? 0) * turns);

    internal (int Stamina, int Shield, int Ram) RecoverTurnResources(int turns = 1)
    {
        var before = (Stamina, Shield, Ram);
        RecoverStamina(turns, ImplantBonus(implant => implant.StaminaRecovery)
            - (Origin is not null && IsOverweight ? StaminaContent.OverweightRecoveryPenalty : 0));
        RecoverShield(turns, ImplantBonus(implant => implant.ShieldRecovery));
        Ram = RamAfterRecovery(turns);
        return (Stamina - before.Stamina, Shield - before.Shield, Ram - before.Ram);
    }

    /// <summary>Al terminar la aventura, recupera la RAM del deck instalado.</summary>
    public void RestoreRam() => Ram = MaxRam;

    /// <summary>Consulta el valor actual de un atributo, inicialmente uno.</summary>
    public int Attribute(AttributeType attribute) =>
        _attributes.TryGetValue(attribute, out var value)
            ? value : throw new ArgumentOutOfRangeException(nameof(attribute));

    /// <summary>Comprueba si la habilidad ya pertenece a este personaje.</summary>
    public bool HasSkill(string code) => _skills.Contains(code);

    /// <summary>Calcula la suma de bonificaciones porcentuales desbloqueadas.</summary>
    public int Bonus(SkillEffect effect) => Origin is null ? 0
        : ProgressionContent.Skills.Where(skill => skill.Effect == effect && HasSkill(skill.Code))
            .Sum(skill => skill.Percent);

    /// <summary>Aplica experiencia de un evento una sola vez y concede puntos por nivel, hasta 20.</summary>
    public int GainExperience(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (Origin is null || CurrentHealth == 0)
            throw new InvalidOperationException("Solo un personaje vivo puede ganar experiencia.");
        var adjusted = amount + amount * (long)Bonus(SkillEffect.Experience) / 100;
        Experience = (int)Math.Min(ProgressionContent.ExperienceForLevel(20), Experience + adjusted);
        var levels = 0;
        while (Level < 20 && Experience >= ExperienceToNextLevel)
        {
            Level++;
            AttributePoints++;
            SkillPoints++;
            levels++;
        }
        return levels;
    }

    /// <summary>Gasta un punto permanente en un atributo sin superar diez.</summary>
    public bool TryIncreaseAttribute(AttributeType attribute, out string error)
    {
        if (!_attributes.TryGetValue(attribute, out var current))
            error = "Atributo desconocido.";
        else if (CurrentHealth == 0 || AttributePoints == 0)
            error = "No tenés puntos de atributo disponibles.";
        else if (current == 10)
            error = "El atributo ya está en su máximo.";
        else
        {
            _attributes[attribute] = current + 1;
            AttributePoints--;
            error = string.Empty;
            return true;
        }
        return false;
    }

    /// <summary>Desbloquea una habilidad una vez, si se cumplen puntos, atributo y dependencia.</summary>
    public bool TryUnlockSkill(string code, out string error)
    {
        var skill = ProgressionContent.Skills.FirstOrDefault(entry => entry.Code == code);
        if (skill is null)
            error = "Habilidad desconocida.";
        else if (CurrentHealth == 0 || SkillPoints == 0)
            error = "No tenés puntos de habilidad disponibles.";
        else if (HasSkill(code))
            error = "Ya tenés esta habilidad.";
        else if (Attribute(skill.Attribute) < skill.RequiredAttribute)
            error = "El atributo no alcanza el requisito.";
        else if (skill.Prerequisite is not null && !HasSkill(skill.Prerequisite))
            error = "Falta la habilidad previa.";
        else
        {
            _skills.Add(code);
            SkillPoints--;
            error = string.Empty;
            return true;
        }
        return false;
    }

    /// <summary>Aplica al valor una bonificación porcentual, redondeando y limitando la curación.</summary>
    public int ModifiedAmount(int amount, SkillEffect effect) =>
        (int)Math.Min(int.MaxValue, Math.Round(amount * (1m + (Bonus(effect) + (effect switch
        {
            SkillEffect.Healing => ImplantBonus(implant => implant.HealingPercent),
            SkillEffect.BasicDamage => ImplantBonus(implant => implant.BasicDamagePercent),
            _ => 0
        })) / 100m),
            MidpointRounding.AwayFromZero));

    public bool TryDiscard(Item? item, int quantity, out string error)
    {
        if (item is null || !Inventory.Contains(item))
            error = "El objeto no pertenece a este inventario.";
        else if (quantity <= 0)
            error = "La cantidad debe ser positiva.";
        else if (item is Weapon && quantity != 1)
            error = "Solo podés descartar un arma por vez.";
        else if (item is Consumable stack && quantity > stack.Quantity)
            error = "La cantidad supera lo disponible.";
        else if (item is not Weapon && item is not Consumable)
            error = "Este tipo de objeto no se puede descartar.";
        else
        {
            if (item is Weapon weapon && Equipment.Weapons.Contains(weapon))
            {
                if (!Equipment.TryUnequip(weapon, out error))
                    return false;
            }

            if (item is Consumable consumable && quantity < consumable.Quantity)
                consumable.Reduce(quantity);
            else
            {
                Inventory.Remove(item);
                item.MarkDiscarded();
            }

            error = string.Empty;
            return true;
        }

        return false;
    }

    public bool TryUseHealing(Consumable? item, out int healed, out string error)
    {
        healed = 0;
        if (item is null || !Inventory.Contains(item) || item.Type != ConsumableType.Healing)
            error = "No tenés esa curación disponible.";
        else if (CurrentHealth == 0)
            error = "No se puede curar a un personaje muerto.";
        else if (CurrentHealth == MaxHealth)
            error = "La salud ya está completa.";
        else
        {
            Consume(item, 1);
            healed = Heal(ModifiedAmount(item.ConsumableDefinition.HealAmount, SkillEffect.Healing));
            error = string.Empty;
            return true;
        }

        return false;
    }
}
