using System.Collections.ObjectModel;

namespace MySharpVenture;

public enum EventKind { Combat, Discovery, Merchant }
public enum ExpeditionStatus { Active, Success, Withdrawn }

public sealed record Zone(string Code, string Name, string Affiliation, int MinLevel, int MaxLevel);

public sealed class LootEntry(ItemDefinition definition, int quantity)
{
    public ItemDefinition Definition { get; } = definition;
    public int Remaining { get; private set; } = quantity > 0
        ? quantity : throw new ArgumentOutOfRangeException(nameof(quantity));

    internal void Take(int quantity) => Remaining -= quantity;
}

public sealed class EventOccurrence
{
    public EventKind Kind { get; }
    public EnemyProfile? Enemy { get; }
    private readonly List<LootEntry> _loot;
    public ReadOnlyCollection<LootEntry> Loot { get; }
    public IReadOnlyList<MerchantOffer> Offers { get; }
    public bool Resolved { get; private set; }

    public EventOccurrence(EventKind kind, IEnumerable<LootEntry> loot, EnemyProfile? enemy = null,
        IEnumerable<MerchantOffer>? merchant = null)
    {
        ArgumentNullException.ThrowIfNull(loot);
        if (!Enum.IsDefined(kind) || (kind == EventKind.Combat) != (enemy is not null)
            || (kind == EventKind.Merchant) != (merchant is not null))
            throw new ArgumentException("Evento o enemigo incompatible.");
        Kind = kind;
        Enemy = enemy;
        _loot = loot.ToList();
        Loot = _loot.AsReadOnly();
        Offers = Array.AsReadOnly(merchant?.ToArray() ?? []);
    }

    internal void Resolve()
    {
        if (Resolved)
            throw new InvalidOperationException("El evento ya fue resuelto.");
        Resolved = true;
    }
}

public sealed class Expedition
{
    private readonly List<EventOccurrence> _events;
    public Zone Zone { get; }
    public Person Player { get; }
    public ReadOnlyCollection<EventOccurrence> Events { get; }
    public int Position { get; private set; }
    public EventOccurrence Current => _events[Position];
    public ExpeditionStatus Status { get; private set; } = ExpeditionStatus.Active;
    public ExperienceReward? LastExperienceReward { get; private set; }
    /// <summary>Recursos recuperados al avanzar al evento actual.</summary>
    public (int Stamina, int Shield, int Ram) LastRecovery { get; private set; }

    public Expedition(Zone zone, Person player, IEnumerable<EventOccurrence> events)
    {
        Zone = zone ?? throw new ArgumentNullException(nameof(zone));
        Player = player ?? throw new ArgumentNullException(nameof(player));
        ArgumentNullException.ThrowIfNull(events);
        _events = events.ToList();
        if (_events.Count != 5 || _events.Distinct().Count() != 5)
            throw new ArgumentException("La aventura requiere cinco eventos independientes.", nameof(events));
        if (_events.Count(entry => entry.Kind == EventKind.Merchant) > 1)
            throw new ArgumentException("Solo puede haber un comerciante por aventura.", nameof(events));
        Events = _events.AsReadOnly();
    }

    public void ResolveCurrent()
    {
        if (Status != ExpeditionStatus.Active || Player.CurrentHealth == 0)
            throw new InvalidOperationException("La aventura no puede resolver más eventos.");
        Current.Resolve();
        LastExperienceReward = null;
        if (Player.Origin is not null && Current.Kind != EventKind.Merchant)
        {
            Player.EarnMoney(Current.Kind == EventKind.Combat ? 15 : 8);
            var before = Player.Experience;
            var levels = Player.GainExperience(Current.Kind == EventKind.Combat ? 30 : 15);
            LastExperienceReward = new ExperienceReward(Player.Experience - before, levels);
        }
    }

    /// <summary>Compra al comerciante actual sin gastar fondos ni stock ante un rechazo.</summary>
    public bool TryBuy(MerchantOffer offer, int quantity, out string error)
    {
        if (Status != ExpeditionStatus.Active || Player.CurrentHealth == 0 || Current.Resolved
            || !Current.Offers.Contains(offer))
        {
            error = "La oferta no está disponible en este evento.";
            return false;
        }
        return offer.TryPurchase(Player, quantity, out error);
    }

    public bool TryCollect(LootEntry entry, int quantity, ItemFactory factory, out string error)
    {
        if (Status != ExpeditionStatus.Active || Player.CurrentHealth == 0 || !Current.Resolved
            || !Current.Loot.Contains(entry) || entry.Remaining == 0)
            error = "El botín no está disponible en este evento.";
        else if (quantity <= 0 || quantity > entry.Remaining)
            error = "La cantidad elegida no está disponible.";
        else
        {
            if (entry.Definition is ConsumableDefinition consumable)
            {
                var stack = factory.CreateConsumable(consumable.CatalogItemId, quantity);
                if (!Player.Inventory.TryAddToStack(stack, out error))
                    return false;
            }
            else if (entry.Definition is WeaponDefinition weapon)
            {
                var items = Enumerable.Range(0, quantity)
                    .Select(_ => factory.CreateWeapon(weapon.Code)).ToArray();
                foreach (var item in items)
                    Player.Inventory.Add(item);
            }
            else if (entry.Definition is CybergearStashDefinition { Implant: { } implant })
            {
                for (var i = 0; i < quantity; i++)
                    Player.ReceiveCyberware(implant with { });
            }
            else
                throw new InvalidOperationException("Botín desconocido.");

            entry.Take(quantity);
            error = string.Empty;
            return true;
        }
        return false;
    }

    public bool Advance()
    {
        if (Status != ExpeditionStatus.Active || Player.CurrentHealth == 0 || !Current.Resolved)
            throw new InvalidOperationException("No se puede avanzar sin resolver el evento.");
        if (Position == _events.Count - 1)
        {
            Status = ExpeditionStatus.Success;
            return false;
        }
        Position++;
        LastRecovery = Player.RecoverTurnResources(StaminaContent.RecoveryTurns);
        return true;
    }

    internal void Withdraw()
    {
        if (Status != ExpeditionStatus.Active || Player.CurrentHealth == 0)
            throw new InvalidOperationException("Solo podés retirarte de una aventura activa si seguís con vida.");
        Status = ExpeditionStatus.Withdrawn;
    }
}
