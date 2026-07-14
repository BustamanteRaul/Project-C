using MySharpVenture;
using MySharpVenture.Pantallas;

var state = new GameState();

var engine = new GameEngine(new Dictionary<PantallaId, IPantalla>
{
    [PantallaId.MenuPrincipal] = new MenuPrincipal(state),
    [PantallaId.Equipamento] = new Equipamento(state),
    [PantallaId.Aventura] = new Aventura(state),
});

engine.Run();
