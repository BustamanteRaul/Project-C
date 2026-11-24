using MySharpVenture;
using MySharpVenture.Pantallas;
using Microsoft.Data.Sqlite;

var state = new GameState();
var dataDirectory = Environment.GetEnvironmentVariable("MYSHARPVENTURE_DATA_DIR")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MySharpVenture");
var store = new SqliteGameStore(Path.Combine(dataDirectory, "game.db"));
ItemCatalog catalog;
try
{
    catalog = store.LoadCatalog(Console.WriteLine);
}
catch (Exception error) when (error is InvalidDataException or IOException
    or UnauthorizedAccessException or SqliteException)
{
    Console.Error.WriteLine($"No se pudo abrir la base de datos en {dataDirectory}: {error.Message}");
    Environment.ExitCode = 1;
    return;
}
var stash = new StashService(store, catalog);

var engine = new GameEngine(new Dictionary<PantallaId, IPantalla>
{
    [PantallaId.Bienvenida] = new Bienvenida(),
    [PantallaId.Creacion] = new Creacion(state, catalog),
    [PantallaId.MenuPrincipal] = new MenuPrincipal(state, stash),
    [PantallaId.Equipamento] = new Equipamento(state),
    [PantallaId.Curacion] = new CuracionPantalla(state),
    [PantallaId.Recarga] = new RecargaPantalla(state),
    [PantallaId.Progresion] = new ProgressionPantalla(state),
    [PantallaId.Ripperdoc] = new RipperdocPantalla(state),
    [PantallaId.Tienda] = new TiendaPantalla(state),
    [PantallaId.Taller] = new TallerPantalla(state),
    [PantallaId.CombatePrueba] = new CombateEncuentro(state),
    [PantallaId.Guia] = new GuiaPantalla(state),
    [PantallaId.Aventura] = new Aventura(state, catalog),
    [PantallaId.Stash] = new StashPantalla(state, stash),
});

engine.Run();
