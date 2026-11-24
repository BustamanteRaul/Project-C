namespace MySharpVenture;

/// <summary>Oferta de consumibles, armas o quickhacks con stock independiente.</summary>
public sealed class MerchantOffer(ItemDefinition definition, int unitPrice, int stock)
{
    public ItemDefinition Definition { get; } = definition switch
    {
        ConsumableDefinition or WeaponDefinition or CybergearStashDefinition { Quickhack: not null } => definition,
        null => throw new ArgumentNullException(nameof(definition)),
        _ => throw new ArgumentException("Objeto no admitido en el comercio común.", nameof(definition))
    };
    public int UnitPrice { get; } = unitPrice > 0 ? unitPrice : throw new ArgumentOutOfRangeException(nameof(unitPrice));
    public int Stock { get; private set; } = stock > 0 && (definition is ConsumableDefinition || stock == 1)
        ? stock : throw new ArgumentOutOfRangeException(nameof(stock));

    internal void Restock() => Stock = stock;

    internal bool TryPurchase(Person player, int quantity, out string error)
    {
        if (quantity <= 0 || quantity > Stock)
            error = "La cantidad supera el stock disponible o no es válida.";
        else if (Definition is CybergearStashDefinition { Quickhack: { } owned } && player.OwnsQuickhack(owned))
            error = $"Ya tenés {Definition.Name}.";
        else if ((long)UnitPrice * quantity > player.Money)
            error = "No tenés suficientes eurodólares.";
        else
        {
            switch (Definition)
            {
                case ConsumableDefinition consumable:
                    if (!player.Inventory.TryAddToStack(new Consumable(consumable, quantity), out error))
                        return false;
                    break;
                case WeaponDefinition weapon:
                    player.Inventory.Add(new Weapon(weapon, loadedAmmo: 0));
                    break;
                case CybergearStashDefinition { Quickhack: { } hack }:
                    player.ReceiveQuickhack(hack);
                    break;
            }
            player.SpendMoney(UnitPrice * quantity);
            Stock -= quantity;
            error = "";
            return true;
        }
        return false;
    }
}
