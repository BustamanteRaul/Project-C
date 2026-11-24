using MySharpVenture.Pantallas;

namespace MySharpVenture;

public class GameEngine(Dictionary<PantallaId, IPantalla> screens, PantallaId startScreen = PantallaId.Bienvenida)
{
    private readonly Dictionary<PantallaId, IPantalla> _screens =
        screens ?? throw new ArgumentNullException(nameof(screens));
    private readonly PantallaId _startScreen = startScreen;

    public void Run()
    {
        var current = _startScreen;

        while (current != PantallaId.Salir)
        {
            if (!_screens.TryGetValue(current, out var screen))
            {
                throw new InvalidOperationException($"No hay pantalla registrada para {current}");
            }

            current = screen.Show();
        }
    }
}
