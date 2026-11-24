"""Recorridos de Watson con encuentros controlados. Requiere Python y pexpect."""
import io
import os
from pathlib import Path
import pexpect
from main_menu_ui import confirm_origin, leave_services, open_main, start_game

ROOT = Path(__file__).resolve().parents[1]
ANSI = r"(?:\x1b\[[0-?]*[ -/]*[@-~])*"
WRAPPED_SPACE = r"(?:\s|\x1b\[[0-?]*[ -/]*[@-~])+"


def start(mode, width=140, color=True):
    env = {**os.environ, "TERM": "xterm-256color"}
    if not color:
        env["NO_COLOR"] = "1"
    child = pexpect.spawn("dotnet", [
        "run", "--project", "tests/Project-C.Checks/Project-C.Checks.csproj",
        "--no-build", "--", mode
    ], cwd=str(ROOT), encoding="utf-8", timeout=20,
        dimensions=(40, width), env=env)
    child.logfile_read = io.StringIO()
    return child


def choose(child, index):
    for _ in range(index):
        child.send("\x1b[B")
    child.send("\r")


def pause(child):
    child.expect_exact("Presione una tecla")
    child.send("\r")


def preparation(child, health):
    child.expect(rf"Salud\s+{ANSI}│{ANSI} {health}/100")
    child.expect_exact("¿Qué querés hacer?")
    child.expect_exact("Salir")


def select_zone(child, index=0):
    open_main(child, "aventura")
    child.expect_exact("Elegí una zona:")
    for label in ("Watson · Maelstrom", "Japantown · Tyger Claws", "Dogtown · Barghest"):
        child.expect_exact(label)
    child.expect_exact("Volver a preparación")
    choose(child, index)


def adventure_menu(child, event, health, next_event=None):
    child.expect_exact(f"Watson · Evento {event}/5 resuelto")
    child.expect_exact(f"Salud: {health}/100")
    if next_event:
        child.expect_exact(f"Siguiente evento: {next_event}")
    child.expect_exact("Pausa: elegí qué hacer antes de continuar.")
    child.expect_exact("Continuar")


def encounter(child, index, health):
    child.expect_exact(f"Watson · Evento {index}/5")
    if index > 1:
        child.expect_exact("Recuperación entre eventos")
        child.expect_exact("+0 RAM")
    child.expect_exact("Aparece Merodeador de Maelstrom (nivel 1).")
    pause(child)
    for shot in range(4):
        child.expect_exact(f"Corpo: {health - 6 * shot}/100")
        child.expect_exact("Elegí tu acción:")
        child.expect_exact("Esperar")
        choose(child, 0)
        child.expect_exact("Corpo acierta e inflige")
        if shot < 3:
            child.expect_exact("Merodeador acierta e inflige 6 de daño.")
        else:
            child.expect_exact("Victoria en el combate.")
        pause(child)
    return health - 18


def collect_healing(child, index, health):
    choose(child, 0)
    child.expect_exact("¿Qué querés recoger?")
    child.expect_exact("Cancelar")
    choose(child, 1)
    child.expect_exact("Cantidad a recoger (1–1; 0 cancela):")
    child.sendline("1")
    child.expect_exact("Recogiste 1 × Curación.")
    pause(child)
    adventure_menu(child, index, health)


def success():
    child = start("--aventura-prueba")
    try:
        child.expect_exact("Iniciando MySharpVenture…")
        child.expect_exact("Espacio: saltar")
        frame = child.before
        assert "0%" in frame and frame.count("█") >= 80, frame
        child.send(" ")
        start_game(child)
        child.expect_exact("Nomad — Escopeta de corredera")
        choose(child, 0)
        confirm_origin(child, "Corpo")
        preparation(child, 100)
        select_zone(child)
        child.expect_exact("Rumbo a Watson…")
        health = encounter(child, 1, 100)
        adventure_menu(child, 1, health)
        choose(child, 4)  # Consultar progresión en pausa sin curar ni avanzar
        child.expect_exact("Nivel 1/20")
        child.expect_exact("¿Qué querés mejorar?")
        child.expect_exact("Volver a aventura")
        choose(child, 2)
        child.expect_exact("Instalación y extracción: ripperdoc en preparación.")
        pause(child)
        child.expect_exact("Volver a aventura")
        choose(child, 3)
        adventure_menu(child, 1, health)
        choose(child, 0)  # Tomar solo dos balas del botín
        child.expect_exact("¿Qué querés recoger?")
        child.expect_exact("Cancelar")
        choose(child, 0)
        child.expect_exact("Cantidad a recoger (1–6; 0 cancela):")
        child.sendline("2")
        child.expect_exact("Recogiste 2 × Munición de pistola.")
        pause(child)
        adventure_menu(child, 1, 82)
        choose(child, 1)  # Consultar inventario en pausa
        child.expect_exact("Munición de pistola")
        child.expect_exact("14")  # 12 de reserva inicial + 2 del botín, en una pila
        child.expect_exact("Volver a aventura")
        choose(child, 4)
        adventure_menu(child, 1, 82)  # Consultar no cura
        choose(child, 3)  # Recargar en la pausa, sin avanzar el evento
        child.expect_exact("Elegí un arma para recargar:")
        child.expect_exact("Cancelar")
        choose(child, 0)
        child.expect_exact("Pistola: recargaste 4 bala(s).")
        pause(child)
        adventure_menu(child, 1, 82)
        choose(child, 2)  # Curarse explícitamente
        child.expect_exact("Salud: 82/100. Elegí una curación:")
        child.expect_exact("Cancelar")
        choose(child, 0)
        child.expect_exact("Recuperaste 18 de salud.")
        pause(child)
        adventure_menu(child, 1, 100)
        choose(child, 5)  # Cuatro balas restantes abandonadas

        health = encounter(child, 2, 100)
        adventure_menu(child, 2, health)
        collect_healing(child, 2, health)
        choose(child, 5)
        health = encounter(child, 3, 82)
        adventure_menu(child, 3, health)
        collect_healing(child, 3, health)
        choose(child, 5)

        child.expect_exact("Watson · Evento 4/5")
        child.expect_exact("Encontraste un escondite con objetos.")
        pause(child)
        adventure_menu(child, 4, 64)
        choose(child, 0)
        child.expect_exact("¿Qué querés recoger?")
        child.expect_exact("Cancelar")
        choose(child, 1)  # Pistola del hallazgo
        child.expect_exact("Cantidad a recoger (1–1; 0 cancela):")
        child.sendline("1")
        child.expect_exact("Recogiste 1 × Pistola.")
        pause(child)
        adventure_menu(child, 4, 64)
        choose(child, 1)
        child.expect_exact("0/12")
        child.expect_exact("Volver a aventura")
        choose(child, 4)
        adventure_menu(child, 4, 64)
        choose(child, 5)

        child.expect_exact("Watson · Evento 5/5")
        child.expect_exact("Encontraste un escondite con objetos.")
        pause(child)
        adventure_menu(child, 5, 64)
        choose(child, 0)
        child.expect_exact("¿Qué querés recoger?")
        child.expect_exact("Cancelar")
        choose(child, 1)
        child.expect_exact("Cantidad a recoger (1–1; 0 cancela):")
        child.sendline("1")
        child.expect_exact("Recogiste 1 × Pistola.")
        pause(child)
        adventure_menu(child, 5, 64)
        choose(child, 5)
        child.expect_exact("Completaste los cinco eventos de Watson.")
        pause(child)
        preparation(child, 100)
        open_main(child, "inventario")
        child.expect_exact("Pistola")
        child.expect_exact("0/12")
        child.expect_exact("Volver a preparación")
        choose(child, 4)
        preparation(child, 100)
        check_ripperdoc(child)
        check_shop(child)
        check_weapon_upgrade(child)
        check_magazine(child)
        child.send("\x1b[A\r")  # Salir
        child.expect_exact("OK: aventura interactiva terminada.")
        child.expect(pexpect.EOF)
        child.close()
        assert child.exitstatus == 0
        print("OK: Watson 5 eventos, botín parcial, pausas sin curación, regreso con salud completa.")
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)


def defeat(zone=0):
    child = start("--aventura-muerte-prueba")
    try:
        start_game(child)
        child.expect_exact("Nomad — Escopeta de corredera")
        choose(child, 0)
        confirm_origin(child, "Corpo")
        preparation(child, 100)
        select_zone(child, zone)
        child.expect_exact(("Dogtown" if zone else "Watson") + " · Evento 1/5")
        if zone:
            child.expect_exact("Aparece Patrullero de Barghest (nivel 13).")
        pause(child)
        turns = 11 if zone else 26
        for turn in range(turns):
            child.expect_exact("Elegí tu acción:")
            child.expect_exact("Esperar")
            choose(child, 6)
            child.expect_exact("Corpo espera.")
            reply = " espera." if turn in (4, 7, 9, 12, 14, 17, 19, 22, 24) else " acierta e inflige"
            child.expect_exact(("Patrullero" if zone else "Merodeador") + reply)
            if turn == turns - 1:
                child.expect_exact("Derrota en el combate.")
            pause(child)
        child.expect_exact("Nomad — Escopeta de corredera")
        assert "Bienvenido a Night City" not in child.before
        choose(child, 2)
        confirm_origin(child, "Nomad")
        preparation(child, 100)
        child.send("\x1b[A\r")  # Salir
        child.expect_exact("OK: aventura interactiva terminada.")
        child.expect(pexpect.EOF)
        child.close()
        assert child.exitstatus == 0
        print("OK: muerte y recreación en " + ("Dogtown." if zone else "Watson."))
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)


def circular_navigation():
    child = start("--aventura-prueba")
    try:
        start_game(child)
        child.expect_exact("Nomad — Escopeta de corredera")
        choose(child, 0)
        child.expect_exact("¿Confirmás el origen Corpo?")
        child.expect_exact("Volver a elegir")
        child.send("\r")  # La opción inicial no crea personaje.
        child.expect_exact("Nomad — Escopeta de corredera")
        child.send("\x1b[A\r")  # Desde Corpo, subir selecciona la última opción: Nomad
        confirm_origin(child, "Nomad")
        child.expect(rf"Origen\s+{ANSI}│{ANSI} Nomad")
        preparation(child, 100)
        child.send("\x1b[A\r")  # Desde la primera opción, subir selecciona Salir.
        child.expect_exact("OK: aventura interactiva terminada.")
        child.expect(pexpect.EOF)
        child.close()
        assert child.exitstatus == 0
        print("OK: selección circular en origen y menú principal.")
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)


def narrow_terminal(width):
    child = start("--aventura-prueba", width=width, color=width >= 80)
    try:
        child.expect_exact("MySharpVenture")
        start_game(child)
        child.expect_exact("Nomad — Escopeta de corredera")
        choose(child, 0)
        confirm_origin(child, "Corpo")
        child.expect_exact("100/100")
        child.expect_exact("Salir")
        open_main(child, "inventario")
        child.expect_exact("Inventario de Corpo")
        if width < 80:
            child.expect_exact("Objeto #1")
            child.expect_exact("Cargador 12/12")
        child.expect_exact("¿Qué querés hacer con tu equipo?")
        child.expect_exact("Volver a preparación")
        choose(child, 4)
        child.expect_exact("¿Qué querés hacer?")
        child.expect_exact("Salir")
        open_main(child, "progresion")
        child.expect_exact("¿Qué querés mejorar?")
        child.expect_exact("Volver a preparación")
        choose(child, 3)
        child.expect_exact("¿Qué querés hacer?")
        child.expect_exact("Salir")
        choose(child, 2)
        child.expect_exact("Preparación · Elegí una opción")
        child.expect_exact("Volver al menú principal")
        child.send("\x1b[A\r")
        preparation(child, 100)
        choose(child, 4)
        child.expect_exact("Guía de Night City")
        for index, markers in enumerate((
            ("1 · PREPARATE", "2 · SALÍ A WATSON", "4 · VOLVÉ Y GUARDÁ"),
            ("LEÉ LA INTENCIÓN", "CUIDÁ LA STAMINA"),
            ("ATRIBUTOS", "HABILIDADES"),
            ("RIPPERDOC", "QUICKHACKS"),
            ("Solo el stash persiste", "RETIRADA", "DERROTA"),
        )):
            child.expect_exact("Guía y práctica · Elegí un tema")
            child.expect_exact("Volver al menú principal")
            choose(child, index)
            for marker in markers:
                child.expect_exact(marker)
            pause(child)
        child.expect_exact("Guía y práctica · Elegí un tema")
        child.expect_exact("Desactivar animaciones")
        choose(child, 6)
        child.expect_exact("Guía y práctica · Elegí un tema")
        child.expect_exact("Activar animaciones")
        child.expect_exact("Volver al menú principal")
        child.send("\x1b[A\r")
        preparation(child, 100)
        open_main(child, "tienda")
        child.expect_exact("¿Qué querés hacer en la tienda?")
        choose(child, 0)
        child.expect_exact("Comprar · Elegí una categoría")
        choose(child, 1)
        child.expect_exact("Comprar Curación")
        child.expect(rf"Te{WRAPPED_SPACE}faltan{WRAPPED_SPACE}12")
        child.send("\x1b[A\r")
        leave_services(child, "Comprar · Elegí una categoría", "¿Qué querés hacer en la tienda?")
        preparation(child, 100)
        open_main(child, "ripperdoc")
        child.expect_exact("¿Qué querés hacer en el ripperdoc?")
        choose(child, 0)
        child.expect_exact("Comprar implantes · Elegí una zona")
        choose(child, 1)
        child.expect_exact("Comprar Ex-Disk (30)")
        child.expect(rf"Te{WRAPPED_SPACE}faltan{WRAPPED_SPACE}30")
        child.send("\x1b[A\r")
        leave_services(child, "Comprar implantes · Elegí una zona", "¿Qué querés hacer en el ripperdoc?")
        preparation(child, 100)
        child.send("\x1b[A\r")  # Salir
        child.expect_exact("OK: aventura interactiva terminada.")
        child.expect(pexpect.EOF)
        child.close()
        assert child.exitstatus == 0
        print(f"OK: cabecera y paneles navegables en terminal de {width} columnas.")
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)


def merchant():
    child = start("--comerciante-prueba")
    try:
        start_game(child)
        child.expect_exact("Nomad — Escopeta de corredera")
        choose(child, 0)
        confirm_origin(child, "Corpo")
        preparation(child, 100)
        select_zone(child, 3)  # Cancelar no crea una aventura
        preparation(child, 100)
        select_zone(child)
        health = 100
        for index in range(1, 4):
            health = encounter(child, index, health)
            adventure_menu(child, index, health, "comerciante" if index == 3 else None)
            choose(child, 5)
        child.expect_exact("Comerciante de aventura")
        child.expect_exact("Eurodólares: 45")
        child.expect_exact("stock 2")
        child.expect_exact("Tier 2 · cargador vacío · 25 eurodólares/u · stock 1")
        child.expect_exact("Suicidio · 60 eurodólares/u · stock 1")
        child.expect_exact("Dejar al comerciante")
        choose(child, 0)
        child.expect_exact("Cantidad a comprar")
        child.sendline("2")
        child.expect_exact("Compraste 2 × Curación.")
        pause(child)
        child.expect_exact("Eurodólares: 25")
        child.expect_exact("Dejar al comerciante")
        assert "Curación" not in child.before, "La oferta agotada sigue visible."
        choose(child, 3)
        adventure_menu(child, 4, health)
        assert "Tomar botín" not in child.before
        choose(child, 4)
        child.expect_exact("Encontraste un escondite con objetos.")
        pause(child)
        adventure_menu(child, 5, health)
        choose(child, 5)
        child.expect_exact("Completaste los cinco eventos de Watson.")
        pause(child)
        preparation(child, 100)
        check_overheat(child)
        child.send("\x1b[A\r")  # Salir
        child.expect_exact("OK: aventura interactiva terminada.")
        child.expect(pexpect.EOF)
        child.close()
        assert child.exitstatus == 0
        print("OK: selección/cancelación, compra, stock agotado y regreso.")
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)


def check_ripperdoc(child):
    open_main(child, "ripperdoc")
    child.expect_exact("Eurodólares: 61")
    child.expect_exact("¿Qué querés hacer en el ripperdoc?")
    choose(child, 0)
    child.expect_exact("Comprar implantes · Elegí una zona")
    choose(child, 1)
    child.expect_exact("Comprar · Corteza frontal")
    child.expect_exact("Comprar Ex-Disk (30)")
    child.expect_exact("Disponible")
    choose(child, 0)
    child.expect_exact("Compraste Ex-Disk. Está suelto, aún no instalado.")
    pause(child)
    child.expect_exact("Capacidad: 8/20")
    child.expect_exact("Comprar Ex-Disk (30) · stock 0")
    child.expect_exact("Agotado")
    child.send("\x1b[A\r")
    leave_services(child, "Comprar implantes · Elegí una zona")
    child.expect_exact("¿Qué querés hacer en el ripperdoc?")
    choose(child, 1)
    child.expect_exact("Instalar Ex-Disk (5)")
    choose(child, 0)
    child.expect_exact("¿Confirmás instalar Ex-Disk?")
    choose(child, 1)
    child.expect_exact("Instalando Ex-Disk…")
    child.send(" \r")  # Espacio salta la animación; el Enter anticipado no pasa la pausa.
    child.expect_exact("Instalación completada")
    child.expect_exact("Instalaste Ex-Disk.")
    child.expect_exact("Presione una tecla")
    assert child.expect_exact(["Instalar implantes", pexpect.TIMEOUT], timeout=0.2) == 1
    child.send("\r")
    child.expect_exact("Eurodólares: 26")
    child.expect_exact("Capacidad: 12/20")
    child.expect_exact("RAM: 6/8")
    leave_services(child, "Instalar implantes")
    child.expect_exact("¿Qué querés hacer en el ripperdoc?")
    choose(child, 2)
    child.expect_exact("Extraer Ex-Disk (5)")
    choose(child, 1)
    child.expect_exact("¿Confirmás extraer Ex-Disk?")
    choose(child, 1)
    child.expect_exact("Extrajiste Ex-Disk.")
    pause(child)
    child.expect_exact("Eurodólares: 21")
    child.expect_exact("Capacidad: 8/20")
    child.expect_exact("RAM: 6/6")
    leave_services(child, "Extraer implantes")
    child.expect_exact("¿Qué querés hacer en el ripperdoc?")
    choose(child, 3)
    child.expect_exact("Vender Ex-Disk (12)")
    choose(child, 0)
    child.expect_exact("¿Confirmás la venta?")
    choose(child, 0)
    child.expect_exact("Eurodólares: 21")
    leave_services(child, "Vender implantes", "¿Qué querés hacer en el ripperdoc?")
    preparation(child, 100)


def check_shop(child):
    open_main(child, "tienda")
    child.expect_exact("Eurodólares: 21")
    child.expect_exact("¿Qué querés hacer en la tienda?")
    choose(child, 0)
    child.expect_exact("Comprar · Elegí una categoría")
    choose(child, 1)
    child.expect_exact("Comprar Curación")
    child.expect_exact("podés pagar 1")
    choose(child, 0)
    child.expect_exact("Cantidad a comprar (1–1; 0 cancela):")
    child.sendline("1")
    child.expect_exact("Compraste 1 × Curación.")
    pause(child)
    child.expect_exact("Eurodólares: 9")
    child.expect_exact("Te faltan 3 eurodólares")
    choose(child, 0)
    pause(child)
    leave_services(child, "Comprar · Curaciones", "Comprar · Elegí una categoría")
    child.expect_exact("¿Qué querés hacer en la tienda?")
    choose(child, 1)
    child.expect_exact("Vender · Elegí una categoría")
    choose(child, 1)
    child.expect_exact("Vender Curación")
    choose(child, 0)
    child.expect_exact("Cantidad a vender")
    child.sendline("1")
    child.expect_exact("Vendiste 1 × Curación.")
    pause(child)
    child.expect_exact("Eurodólares: 14")
    leave_services(child, "Vender · Curaciones", "Vender · Elegí una categoría", "¿Qué querés hacer en la tienda?")
    preparation(child, 100)


def check_weapon_upgrade(child):
    open_main(child, "taller")
    child.expect_exact("¿Qué querés hacer en el taller?")
    choose(child, 1)
    child.expect_exact("¿Qué querés desarmar?")
    child.expect_exact("Cancelar")
    choose(child, 2)  # Pistola suelta del hallazgo, después del arma inicial y la curación.
    child.expect_exact("¿Confirmás el desarme?")
    choose(child, 1)
    child.expect_exact("Obtuviste 4 componente(s).")
    pause(child)
    for confirmation in (0, 1):
        child.expect_exact("Componentes: 4")
        child.expect_exact("¿Qué querés hacer en el taller?")
        choose(child, 2)
        child.expect_exact("daño 12 → 14")
        choose(child, 0)
        child.expect_exact("Coste: 4 componentes. Las balas cargadas se conservan.")
        child.expect_exact("¿Confirmás la mejora?")
        choose(child, confirmation)
    child.expect_exact("Pistola mejorada a Tier 2.")
    pause(child)
    child.expect_exact("Componentes: 0")
    child.expect_exact("¿Qué querés hacer en el taller?")
    choose(child, 6)
    preparation(child, 100)


def check_magazine(child):
    open_main(child, "taller")
    child.expect_exact("¿Qué querés hacer en el taller?")
    choose(child, 1)
    child.expect_exact("¿Qué querés desarmar?")
    child.expect_exact("Curación")
    choose(child, 1)  # La pistola Tier 2 también se puede desarmar.
    child.expect_exact("Cantidad a desarmar")
    child.sendline("3")
    child.expect_exact("¿Confirmás el desarme?")
    choose(child, 1)
    child.expect_exact("Obtuviste 3 componente(s).")
    pause(child)
    child.expect_exact("¿Qué querés hacer en el taller?")
    choose(child, 1)
    child.expect_exact("¿Qué querés desarmar?")
    child.expect_exact("Cancelar")
    choose(child, 1)  # Pistola Tier 1; la equipada ya es Tier 2.
    child.expect_exact("¿Confirmás el desarme?")
    choose(child, 1)
    child.expect_exact("Obtuviste 4 componente(s).")
    pause(child)
    for confirmation in (0, 1):
        child.expect_exact("Componentes: 7")
        child.expect_exact("Fabricar cargador ampliado")
        choose(child, 4)
        child.expect_exact("Confirmar fabricación")
        choose(child, confirmation)
    child.expect_exact("Fabricaste un cargador ampliado.")
    pause(child)
    child.expect_exact("Componentes: 4")
    child.expect_exact("Fabricar mira réflex")
    choose(child, 5)
    child.expect_exact("Confirmar fabricación")
    choose(child, 1)
    child.expect_exact("Fabricaste una mira réflex.")
    pause(child)
    child.expect_exact("Cargadores ampliados sueltos: 1")
    child.expect_exact("Miras réflex sueltas: 1")
    child.expect_exact("Volver a preparación")
    choose(child, 6)
    preparation(child, 100)
    open_main(child, "inventario")
    child.expect_exact("Cargadores ampliados")
    choose(child, 4)
    child.expect_exact("Montar cargador ampliado")
    choose(child, 0)
    child.expect_exact("Montaste el cargador ampliado.")
    pause(child)
    child.expect_exact("4/16 · Ampliado")
    child.expect_exact("Volver a preparación")
    choose(child, 5)
    child.expect_exact("Montar mira réflex")
    choose(child, 0)
    child.expect_exact("Montaste la mira réflex.")
    pause(child)
    child.expect_exact("Pistola · Mira réflex")
    child.expect_exact("4/16 · Ampliado")
    child.expect_exact("Volver a preparación")
    choose(child, 4)
    child.expect_exact("Quitar cargador ampliado")
    child.expect_exact("Cancelar")
    choose(child, 1)
    child.expect_exact("4/16 · Ampliado")
    child.expect_exact("Volver a preparación")
    choose(child, 4)
    child.expect_exact("Quitar cargador ampliado")
    choose(child, 0)
    child.expect_exact("Quitaste el cargador ampliado.")
    pause(child)
    child.expect_exact("4/12")
    child.expect_exact("Volver a preparación")
    choose(child, 5)
    child.expect_exact("Quitar mira réflex")
    choose(child, 0)
    child.expect_exact("Quitaste la mira réflex.")
    pause(child)
    child.expect_exact("Volver a preparación")
    choose(child, 6)
    preparation(child, 100)


def check_overheat(child):
    open_main(child, "tienda")
    child.expect_exact("Eurodólares: 33")
    child.expect_exact("¿Qué querés hacer en la tienda?")
    choose(child, 0)
    child.expect_exact("Comprar · Elegí una categoría")
    choose(child, 2)
    child.expect_exact("Comprar Sobrecalentamiento")
    choose(child, 0)
    child.expect_exact("Compraste Sobrecalentamiento. Equipalo desde el inventario.")
    pause(child)
    child.expect_exact("Ya lo tenés")
    child.send("\x1b[A\r")
    leave_services(child, "Comprar · Elegí una categoría", "¿Qué querés hacer en la tienda?")
    preparation(child, 100)
    open_main(child, "inventario")
    child.expect_exact("Volver a preparación")
    choose(child, 3)
    child.expect_exact("Ranuras: 1/4")
    child.expect_exact("Sobrecalentamiento · 4 RAM · Equipar")
    choose(child, 1)
    child.expect_exact("Sobrecalentamiento: equipado.")
    pause(child)
    child.expect_exact("Ranuras: 2/4")
    child.expect_exact("Volver al inventario")
    choose(child, 2)
    child.expect_exact("Volver a preparación")
    choose(child, 4)
    preparation(child, 100)
    open_main(child, "recarga")
    child.expect_exact("Cancelar")
    choose(child, 0)
    child.expect_exact("Pistola: recargaste 12 bala(s).")
    pause(child)
    preparation(child, 100)

    open_main(child, "practica")
    child.expect_exact("Elegí tu acción:")
    child.expect_exact("Sobrecalentamiento")
    choose(child, 3)
    child.expect_exact("ejecuta Sobrecalentamiento: 6 de daño (gasta 4 RAM).")
    child.expect_exact("Sobrecalentamiento inflige 6 de daño.")
    child.expect_exact("Merodeador acierta e inflige")
    pause(child)
    child.expect_exact("1 pulso pendiente")
    child.expect_exact("Elegí tu acción:")
    child.expect_exact("Esperar")
    choose(child, 7)
    child.expect_exact("Sobrecalentamiento inflige 6 de daño.")
    pause(child)
    for _ in range(2):
        child.expect_exact("Elegí tu acción:")
        choose(child, 0)
        child.expect_exact("Corpo acierta e inflige")
        pause(child)
    child.expect_exact("Guía y práctica · Elegí un tema")
    child.expect_exact("Volver al menú principal")
    child.send("\x1b[A\r")
    preparation(child, 100)


def withdraw(in_combat):
    child = start("--sesion-prueba")
    try:
        start_game(child)
        child.expect_exact("Nomad — Escopeta de corredera")
        choose(child, 0)
        confirm_origin(child, "Corpo")
        preparation(child, 100)
        select_zone(child)
        if in_combat:
            child.expect_exact("Aparece Merodeador")
            pause(child)
            child.expect_exact("Esperar")
            choose(child, 2)
            child.expect_exact("gasta 3 RAM")
            child.expect_exact("Respuesta enemiga:")
            pause(child)
            child.expect_exact("Turno 2 · Combatientes")
            child.expect_exact("RAM: 4/6")
        else:
            health = encounter(child, 1, 100)
            adventure_menu(child, 1, health)
        child.expect_exact("Retirarse de la aventura")
        child.send("\x1b[A\r")  # Última opción del menú.
        child.expect_exact("¿Confirmás la retirada?")
        child.expect_exact("Cancelar")
        choose(child, 0)
        if in_combat:
            child.expect_exact("Turno 2 · Combatientes")
            child.expect_exact("RAM: 4/6")
        else:
            adventure_menu(child, 1, 82)
        child.expect_exact("Retirarse de la aventura")
        child.send("\x1b[A\r")
        child.expect_exact("¿Confirmás la retirada?")
        child.expect_exact("Confirmar retirada")
        choose(child, 1)
        child.expect_exact("Retirada de la aventura")
        child.expect_exact("Perdiste 1 arma(s), " + ("12" if in_combat else "8") + " bala(s) cargadas")
        child.expect_exact("Volvés a preparación sin recuperación.")
        assert "Experiencia ganada" not in child.before
        pause(child)
        preparation(child, 94 if in_combat else 82)
        child.send("\x1b[A\r")  # Salir
        child.expect_exact("OK: sesión interactiva terminada.")
        child.expect(pexpect.EOF)
        child.close()
        assert child.exitstatus == 0
        print("OK: cancelar y confirmar retirada en " + ("combate." if in_combat else "pausa."))
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)


success()
defeat()
defeat(2)
merchant()
circular_navigation()
narrow_terminal(80)
narrow_terminal(40)
withdraw(in_combat=True)
withdraw(in_combat=False)
