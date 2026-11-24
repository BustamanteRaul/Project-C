namespace MySharpVenture;

/// <summary>Un suministro que se lleva como cantidad, no como instancia de inventario.</summary>
public abstract record StashSupplyDefinition(int CatalogItemId, string Code, string Name)
    : ItemDefinition(CatalogItemId, Code, Name, null)
{
    internal abstract int SpareCount(Person player);
    internal abstract void AdjustSpare(Person player, int delta);
}

/// <summary>Identidad de catálogo de los componentes del Taller.</summary>
public sealed record ComponentDefinition(int CatalogItemId, string Code, string Name)
    : StashSupplyDefinition(CatalogItemId, Code, Name)
{
    public static ComponentDefinition Workshop { get; } = new(303, "componentes", "Componentes");

    internal override int SpareCount(Person player) => player.Components;
    internal override void AdjustSpare(Person player, int delta) => player.Components += delta;
}
