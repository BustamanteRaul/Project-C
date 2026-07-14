using MySharpVenture.Pantallas;
namespace MySharpVenture;

// Shared state passed into screens that need it, instead of using statics.
// Grows over time: player stats, inventory, flags, current location, etc.
public class GameState
{
    public Entidad Player { get; set; } = new(1, "Aventurero", 100, 100, 100, 100, new());
    public List<Entidad> Enemies { get; } = new();
    public List<string> Inventory { get; } = new();
    public PantallaId CurrentScreen { get; set; } = PantallaId.MenuPrincipal;
}
