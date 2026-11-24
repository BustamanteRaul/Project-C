namespace MySharpVenture.Pantallas;

// Combate de práctica independiente de los eventos de Watson.
public sealed class CombateEncuentro(GameState state, Func<Random>? randomFactory = null) : IPantalla
{
    public PantallaId Show()
    {
        if (state.Player is not { } player)
            return PantallaId.Creacion;

        var combat = new CombatController(player.CreatePracticeCopy(), DevelopmentContent.MaelstromTest,
            DevelopmentContent.BasicAttack, randomFactory?.Invoke() ?? new Random());
        new CombatePantalla(combat).Show();
        return PantallaId.Guia;
    }
}
