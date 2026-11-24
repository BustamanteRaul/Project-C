namespace MySharpVenture;

public sealed class ItemCatalog
{
    private readonly Dictionary<int, ItemDefinition> _byId = new();
    private readonly Dictionary<string, ItemDefinition> _byCode = new(StringComparer.Ordinal);

    public ItemCatalog(IEnumerable<ItemDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        foreach (var definition in definitions)
        {
            if (definition is null
                || definition.CatalogItemId <= 0
                || string.IsNullOrWhiteSpace(definition.Code)
                || string.IsNullOrWhiteSpace(definition.Name)
                || !_byId.TryAdd(definition.CatalogItemId, definition)
                || !_byCode.TryAdd(definition.Code, definition))
                throw new ArgumentException("El catálogo contiene identificadores o datos inválidos.");
        }

        foreach (var definition in _byId.Values)
        {
            switch (definition)
            {
                case WeaponDefinition weapon:
                    if (weapon.Tier is null or < 1 or > 5
                        || !Enum.IsDefined(weapon.Variant)
                        || weapon.Damage <= 0
                        || weapon.Accuracy is < 0 or > 1
                        || (weapon.IsMelee
                            ? weapon.MagazineCapacity != 0 || weapon.AmmoCatalogItemId != 0 || weapon.AmmoPerAttack != 0
                            : weapon.AmmoPerAttack <= 0 || weapon.AmmoPerAttack > weapon.MagazineCapacity
                                || !_byId.TryGetValue(weapon.AmmoCatalogItemId, out var ammo)
                                || ammo is not ConsumableDefinition { Type: ConsumableType.Ammunition }))
                        throw new ArgumentException($"Definición de arma inválida: {weapon.Code}.");
                    break;
                case ConsumableDefinition consumable:
                    if (!Enum.IsDefined(consumable.Type) || consumable.Tier is not null
                        || (consumable.Type == ConsumableType.Healing && consumable.HealAmount <= 0)
                        || (consumable.Type != ConsumableType.Healing && consumable.HealAmount != 0))
                        throw new ArgumentException($"Definición de consumible inválida: {consumable.Code}.");
                    break;
                case AccessoryDefinition accessory when accessory == AccessoryDefinition.Magazine
                    || accessory == AccessoryDefinition.Sight:
                case ComponentDefinition component when component == ComponentDefinition.Workshop:
                case CybergearStashDefinition gear when CybergearStashContent.All.Contains(gear):
                    break;
                default:
                    throw new ArgumentException($"Tipo de objeto no admitido: {definition.Code}.");
            }
        }
    }

    public ItemDefinition Get(int id) => _byId.TryGetValue(id, out var definition)
        ? definition
        : throw new InvalidOperationException($"No existe el objeto de catálogo {id}.");

    public ItemDefinition Get(string code) => _byCode.TryGetValue(code, out var definition)
        ? definition
        : throw new InvalidOperationException($"No existe el objeto de catálogo «{code}».");

    internal IEnumerable<ConsumableDefinition> Consumables => _byId.Values.OfType<ConsumableDefinition>();
    internal IEnumerable<ItemDefinition> Definitions => _byId.Values;

    internal WeaponDefinition? NextWeaponTier(WeaponDefinition current)
    {
        if (current.Tier is not (>= 1 and < 5)
            || !current.Code.EndsWith($"-t{current.Tier}", StringComparison.Ordinal)
            || _byCode.GetValueOrDefault(current.Code) != current)
            return null;
        return _byCode.GetValueOrDefault(WeaponUpgradeContent.CodeForTier(current.Code, current.Tier.Value + 1))
            is WeaponDefinition next && next.Tier == current.Tier + 1 && next.Variant == current.Variant
            && next.AmmoCatalogItemId == current.AmmoCatalogItemId
            && next.MagazineCapacity == current.MagazineCapacity ? next : null;
    }
}
