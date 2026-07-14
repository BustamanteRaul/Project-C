using Spectre.Console;
namespace MySharpVenture;

public class Item(int Id, string Nombre, string Descripcion, int Cantidad)
{
    public int Id { get; set; } = Id;
    public string Nombre { get; set; } = Nombre;
    public string Descripcion { get; set; } = Descripcion;
    public int Cantidad { get; set; } = Cantidad;


    public void GetInfo()
    {
        var table = new Table()
            .AddColumn("Campo")
            .AddColumn("Contenido")
            .AddRow("ID", Id.ToString())
            .AddRow($"Nombre:", Nombre)
            .AddRow($"Descripcion:", Descripcion)
            .AddRow($"Cantidad:", Cantidad.ToString());
        AnsiConsole.Write(table);
    }
}
