namespace MySharpVenture;

public sealed class Consumable : Item
{
    public int Quantity { get; private set; }
    public ConsumableType Type => ((ConsumableDefinition)Definition).Type;
    public ConsumableDefinition ConsumableDefinition => (ConsumableDefinition)Definition;

    internal Consumable(ConsumableDefinition definition, int quantity) : base(definition)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        Quantity = quantity;
    }

    internal void Reduce(int quantity)
    {
        if (quantity <= 0 || quantity >= Quantity)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Quantity -= quantity;
    }

    internal void Increase(int quantity)
    {
        if (quantity <= 0 || quantity > int.MaxValue - Quantity)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Quantity += quantity;
    }
}
