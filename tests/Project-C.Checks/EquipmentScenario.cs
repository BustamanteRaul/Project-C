using MySharpVenture;
using MySharpVenture.Pantallas;

internal static class EquipmentScenario
{
    public static int Run(ItemCatalog catalog, bool discard = false)
    {
        var state = new GameState();
        state.CreateCharacter(OriginPath.Corpo, catalog);
        var player = state.Player!;
        var factory = new ItemFactory(catalog);
        player.Inventory.Add(factory.CreateWeapon("pistola-t1"));
        player.Inventory.Add(factory.CreateWeapon("pistola-t1", loaded: true));
        player.Inventory.Add(factory.CreateWeapon("pistola-t1"));

        var items = player.Inventory.Items.ToArray();
        var loaded = items.OfType<Weapon>().ToDictionary(weapon => weapon, weapon => weapon.LoadedAmmo);
        var quantities = items.OfType<Consumable>().Select(item => item.Quantity).ToArray();
        var health = player.CurrentHealth;

        new GameEngine(new Dictionary<PantallaId, IPantalla>
        {
            [PantallaId.MenuPrincipal] = new MenuPrincipal(state),
            [PantallaId.Equipamento] = new Equipamento(state),
            [PantallaId.Progresion] = new ProgressionPantalla(state)
        }, PantallaId.MenuPrincipal).Run();

        var expectedItems = discard ? items.Where(item => item != items[4]).ToArray() : items;
        var expectedWeapons = expectedItems.OfType<Weapon>().ToArray();
        if (!player.Inventory.Items.SequenceEqual(expectedItems)
            || expectedWeapons.Any(weapon => weapon.LoadedAmmo != loaded[weapon])
            || (discard && (player.Inventory.Contains(items[4])
                || !ReferenceEquals(player.Equipment.Weapons.SingleOrDefault(), items[0])))
            || !items.OfType<Consumable>().Select(item => item.Quantity).SequenceEqual(quantities)
            || player.CurrentHealth != health
            || player.Equipment.Weapons.Count > 3
            || player.Equipment.Weapons.Distinct().Count() != player.Equipment.Weapons.Count
            || player.Equipment.Weapons.Any(weapon => !player.Inventory.Contains(weapon)))
        {
            Console.Error.WriteLine("ERROR: el recorrido alteró pertenencia, recursos o integridad del equipo.");
            return 1;
        }

        Console.WriteLine(discard
            ? "OK: descarte interactivo de una instancia sin alterar equipo ni recursos restantes."
            : "OK: escenario interactivo sin pérdidas, duplicación ni cambios de recursos.");
        return 0;
    }
}
