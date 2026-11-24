namespace MySharpVenture;

public abstract class Item
{
    public Guid RuntimeId { get; } = Guid.NewGuid();
    public ItemDefinition Definition { get; private protected set; }
    public int CatalogItemId => Definition.CatalogItemId;
    internal Inventory? Owner { get; set; }
    internal bool IsDiscarded { get; private set; }

    internal void MarkDiscarded() => IsDiscarded = true;

    protected Item(ItemDefinition definition)
    {
        Definition = definition;
    }
}
