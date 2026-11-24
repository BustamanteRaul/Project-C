using Spectre.Console;

namespace MySharpVenture.Pantallas;

/// <summary>Manual por temas y acceso al combate de simulación.</summary>
public sealed class GuiaPantalla(GameState state) : IPantalla
{
    private static readonly (string Title, string Body, Color Color)[] Chapters =
    [
        ("01 · Tu primera aventura", """
            [bold aqua]1 · PREPARATE[/]  Equipá un arma. Revisá su cargador y llevá curaciones.

            [bold aqua]2 · SALÍ A WATSON[/]  Elegí «Ir de aventura». Watson es la zona inicial recomendada.

            [bold aqua]3 · AVANZÁ A TU RITMO[/]  Tras cada encuentro, recogé lo que quieras conservar. En la pausa podés curarte, recargar o revisar tu equipo antes de seguir.

            [bold aqua]4 · VOLVÉ Y GUARDÁ[/]  Completar los cinco eventos restaura tus recursos. Depositá en el stash lo que quieras conservar al cerrar.
            """, Color.Aqua),
        ("02 · Combate", """
            [bold yellow]LEÉ LA INTENCIÓN[/]  El enemigo anuncia qué prepara. Elegir otra arma equipada es gratis; recargar, curarte o esperar gasta una acción.

            [bold yellow]CUIDÁ LA STAMINA[/]  Los ataques físicos gastan stamina incluso si fallan. «Esperar» permite recuperarla al iniciar tu próximo turno.

            [bold yellow]MIRÁ TUS RECURSOS[/]  Las armas de fuego consumen munición, los quickhacks usan RAM y el escudo reduce parte del daño recibido.

            [bold yellow]DISTINGUÍ LAS BALAS[/]  «Gasta» es el coste del próximo ataque. «Cargador» muestra lo listo para disparar y «Reserva» lo que llevás para recargar. Usar un quickhack no gasta munición.

            [bold yellow]GOLPES CRÍTICOS[/]  La probabilidad de crítico solo se aplica si acertás. El daño crítico mostrado ya está redondeado y se calcula antes de que el escudo reduzca el golpe.
            """, Color.Yellow),
        ("03 · Progresión", """
            [bold lime]ATRIBUTOS[/]  En Personaje → Atributos y habilidades podés asignar sus puntos por separado.

            [bold lime]HABILIDADES[/]  Elegí un atributo para ver su rama. Las habilidades bloqueadas indican qué requisito falta.
            """, Color.Lime),
        ("04 · Cyberware", """
            [bold springgreen3]RIPPERDOC[/]  Comprá, instalá o extraé implantes en Servicios. Cada uno ocupa una zona del cuerpo y consume capacidad.

            [bold springgreen3]QUICKHACKS[/]  En Inventario y equipo podés asignar hasta cuatro a un cyberdeck. Consultá su coste de RAM antes de usarlos.

            [bold aqua]SANDEVISTAN[/]  Si usás la acción adicional para atacar físicamente, el golpe acierta. La munición y la stamina siguen siendo necesarias, y los críticos se calculan por separado.
            """, Color.SpringGreen3),
        ("05 · Stash y pérdidas", """
            [bold aqua]STASH[/]  Solo el stash persiste al cerrar. Tu personaje, el dinero y el inventario activo no se guardan.

            [bold yellow]RETIRADA[/]  Perdés las armas, sus balas cargadas, sus accesorios montados y el dinero. No recuperás recursos por retirarte.

            [bold red]DERROTA[/]  Si morís, perdés al personaje y lo que llevaba. El stash queda intacto.
            """, Color.Teal)
    ];

    /// <summary>Muestra la guía y devuelve el destino seleccionado.</summary>
    public PantallaId Show()
    {
        if (state.Player is not { } player)
            return PantallaId.Creacion;
        const string practice = "Combate de prueba";
        const string back = "Volver al menú principal";
        while (true)
        {
            var toggle = Transiciones.Enabled ? "Desactivar animaciones" : "Activar animaciones";
            UiHelpers.MostrarTitulo();
            UiHelpers.MostrarPanel("[bold aqua]Guía de Night City[/]",
                "[bold]Manual de campo[/]\nElegí un tema para leerlo sin gastar recursos."
                + "\n[aqua]Simulación:[/] no consume recursos de tu personaje.", Color.Teal);
            var choice = AnsiConsole.Prompt(UiHelpers.Menu<string>("Guía y práctica · Elegí un tema", player.Origin)
                .AddChoices(Chapters.Select(chapter => chapter.Title).Append(practice).Append(toggle).Append(back)));
            if (choice == practice)
                return PantallaId.CombatePrueba;
            if (choice == back)
                return PantallaId.MenuPrincipal;
            if (choice == toggle)
            {
                Transiciones.Enabled = !Transiciones.Enabled;
                continue;
            }
            var chapter = Chapters.Single(chapter => chapter.Title == choice);
            UiHelpers.MostrarPanel($"[bold]{chapter.Title}[/]", chapter.Body, chapter.Color);
            UiHelpers.Pausar();
        }
    }
}
