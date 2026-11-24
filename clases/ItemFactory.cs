namespace MySharpVenture;

public sealed class ItemFactory(ItemCatalog catalog)
{
    private readonly ItemCatalog _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

    public Weapon CreateWeapon(string code, bool loaded = false)
    {
        if (_catalog.Get(code) is not WeaponDefinition definition)
            throw new InvalidOperationException("La definición seleccionada no es un arma.");

        return new Weapon(definition, loaded ? definition.MagazineCapacity : 0);
    }

    public Consumable CreateConsumable(int catalogItemId, int quantity)
    {
        if (_catalog.Get(catalogItemId) is not ConsumableDefinition definition)
            throw new InvalidOperationException("La definición seleccionada no es un consumible.");

        return new Consumable(definition, quantity);
    }
}
