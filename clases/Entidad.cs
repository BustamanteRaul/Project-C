using Spectre.Console;
namespace MySharpVenture;

public class Entidad(int id, string nombre, int salud, int stamina, int mana, int fuerza, List<Item> inventario)
{
    public int Id { get; set; } = id;
    public string Nombre { get; set; } = nombre;
    public int Salud { get; set; } = salud;
    public int Stamina { get; set; } = stamina;
    public int Mana { get; set; } = mana;
    public int Fuerza { get; set; } = fuerza;
    public List<Item> Inventario { get; set; } = inventario;

    public void GetInfo()
    {
        var table = new Table()
            .AddColumn("Campo")
            .AddColumn("Contenido")
            .AddRow("ID", Id.ToString())
            .AddRow($"Nombre:", Nombre)
            .AddEmptyRow()
            .AddRow($"Salud:", $"[red]{Salud}[/]")
            .AddRow($"Stamina:", $"[teal]{Stamina}[/]")
            .AddRow($"Mana:", $"[purple]{Mana}[/]")
            .AddRow($"Fuerza:", $"[darkorange]{Fuerza}[/]")
            .AddEmptyRow()
            .AddRow($"Inventario:", Inventario.Count.ToString());
        AnsiConsole.Write(table);
    }

    public void AgregarItem(Item item)
    {
        Inventario.Add(item);
    }
}
