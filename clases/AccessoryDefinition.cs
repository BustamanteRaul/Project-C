namespace MySharpVenture;

public sealed record AccessoryDefinition(int CatalogItemId, string Code, string Name)
    : StashSupplyDefinition(CatalogItemId, Code, Name)
{
    public static AccessoryDefinition Magazine { get; } = new(301, "cargador-ampliado", "Cargador ampliado");
    public static AccessoryDefinition Sight { get; } = new(302, "mira-reflex", "Mira réflex");

    internal override int SpareCount(Person player) => this == Magazine ? player.SpareExtendedMagazines : player.SpareReflexSights;
    internal override void AdjustSpare(Person player, int delta)
    {
        if (this == Magazine)
            player.SpareExtendedMagazines += delta;
        else
            player.SpareReflexSights += delta;
    }
}
