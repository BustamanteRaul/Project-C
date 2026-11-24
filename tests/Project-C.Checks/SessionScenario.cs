using MySharpVenture;
using MySharpVenture.Pantallas;

internal static class SessionScenario
{
    public static int Run(ItemCatalog catalog, bool adventure = false, bool death = false, bool merchant = false)
    {
        var state = new GameState();
        new GameEngine(new Dictionary<PantallaId, IPantalla>
        {
            [PantallaId.Bienvenida] = new Bienvenida(),
            [PantallaId.Creacion] = new Creacion(state, catalog),
            [PantallaId.MenuPrincipal] = new MenuPrincipal(state),
            [PantallaId.Equipamento] = new Equipamento(state),
            [PantallaId.Curacion] = new CuracionPantalla(state),
            [PantallaId.Recarga] = new RecargaPantalla(state),
            [PantallaId.Progresion] = new ProgressionPantalla(state),
            [PantallaId.Ripperdoc] = new RipperdocPantalla(state),
            [PantallaId.Tienda] = new TiendaPantalla(state),
            [PantallaId.Taller] = new TallerPantalla(state),
            [PantallaId.CombatePrueba] = new CombateEncuentro(state, () => new AlwaysHits()),
            [PantallaId.Guia] = new GuiaPantalla(state),
            [PantallaId.Aventura] = new Aventura(state, catalog, () => new WatsonRandom(merchant))
        }).Run();
        if (adventure && (state.ActiveExpedition is not null
            || state.Player is null || state.Player.CurrentHealth != state.Player.MaxHealth
            || (death && state.Player.Origin != OriginPath.Nomad)))
        {
            Console.Error.WriteLine("ERROR: la aventura dejó un estado de sesión inválido.");
            return 1;
        }
        Console.WriteLine(adventure
            ? "OK: aventura interactiva terminada."
            : "OK: sesión interactiva terminada.");
        return 0;
    }

    private sealed class AlwaysHits : Random
    {
        public override double NextDouble() => 0;
        public override int Next(int maxValue) => maxValue - 1;
    }

    // Tres combates seguidos y dos hallazgos; nivel fijo 1 y aciertos previsibles.
    private sealed class WatsonRandom(bool merchant) : Random
    {
        private bool _first = true;

        public override int Next(int maxValue)
        {
            var result = _first && merchant ? 0 : maxValue == 2 ? 0 : maxValue - 1;
            _first = false;
            return result;
        }
        public override int Next(int minValue, int maxValue) => minValue;
        public override double NextDouble() => 0;
    }
}
