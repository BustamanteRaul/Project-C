namespace MySharpVenture;

/// <summary>Resultado y coste en componentes de un lote de fabricación.</summary>
public sealed record CraftingRecipe(ItemDefinition Output, int Units, int ComponentCost);
