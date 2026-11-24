using System.Collections.ObjectModel;

namespace MySharpVenture;

public sealed class Inventory
{
    private readonly List<Item> _items = new();
    public ReadOnlyCollection<Item> Items { get; }

    public Inventory()
    {
        Items = _items.AsReadOnly();
    }

    public void Add(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.IsDiscarded)
            throw new InvalidOperationException("Un objeto descartado no se puede recuperar.");
        if (item.Owner is not null)
            throw new InvalidOperationException("El objeto ya pertenece a un inventario.");

        _items.Add(item);
        item.Owner = this;
    }

    public bool Contains(Item item) => _items.Contains(item);

    // Une botín y futuras transferencias con la pila ya transportada.
    // Add sigue permitiendo pilas separadas para casos que las requieran.
    internal bool TryAddToStack(Consumable incoming, out string error)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        if (incoming.Owner is not null || incoming.IsDiscarded)
        {
            error = "El consumible ya pertenece a otro lugar o fue descartado.";
            return false;
        }

        var stack = _items.OfType<Consumable>()
            .FirstOrDefault(item => item.CatalogItemId == incoming.CatalogItemId);
        if (stack is not null)
        {
            if (incoming.Quantity > int.MaxValue - stack.Quantity)
            {
                error = "La cantidad excede el máximo permitido para una pila.";
                return false;
            }
            stack.Increase(incoming.Quantity);
            incoming.MarkDiscarded();
        }
        else
            Add(incoming);

        error = string.Empty;
        return true;
    }

    // Solo la operación coordinada de Person debe retirar objetos del inventario.
    internal void Remove(Item item)
    {
        if (!_items.Remove(item))
            throw new InvalidOperationException("El objeto no pertenece a este inventario.");

        item.Owner = null;
    }
}
