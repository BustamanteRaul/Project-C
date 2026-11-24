using System.Collections.ObjectModel;

namespace MySharpVenture;

public sealed class Equipment
{
    private readonly Inventory _inventory;
    private readonly List<Weapon> _weapons = new();
    public ReadOnlyCollection<Weapon> Weapons { get; }

    public Equipment(Inventory inventory)
    {
        _inventory = inventory;
        Weapons = _weapons.AsReadOnly();
    }

    public bool TryEquip(Weapon weapon, out string error)
    {
        if (!_inventory.Contains(weapon))
            error = "El arma no pertenece a este inventario.";
        else if (_weapons.Contains(weapon))
            error = "El arma ya está equipada.";
        else if (_weapons.Count >= 3)
            error = "Solo podés equipar hasta tres armas.";
        else
        {
            _weapons.Add(weapon);
            error = string.Empty;
            return true;
        }

        return false;
    }

    public bool TryUnequip(Weapon weapon, out string error)
    {
        if (!_inventory.Contains(weapon))
            error = "El arma no pertenece a este inventario.";
        else if (!_weapons.Contains(weapon))
            error = "El arma no está equipada.";
        else
        {
            _weapons.Remove(weapon);
            error = string.Empty;
            return true;
        }

        return false;
    }
}
