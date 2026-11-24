"""Sesión real de pantallas con tiradas controladas. Requiere pexpect."""
import io
import os
from pathlib import Path
import pexpect
from main_menu_ui import confirm_origin, open_main, start_game

ROOT = Path(__file__).resolve().parents[1]
ANSI = r"(?:\x1b\[[0-?]*[ -/]*[@-~])*"
child = pexpect.spawn("dotnet", [
    "run", "--project", "tests/Project-C.Checks/Project-C.Checks.csproj",
    "--no-build", "--", "--sesion-prueba"
], cwd=str(ROOT), encoding="utf-8", timeout=20,
    dimensions=(40, 140), env={**os.environ, "TERM": "xterm-256color"})
child.logfile_read = io.StringIO()


def choose(index):
    for _ in range(index):
        child.send("\x1b[B")
    child.send("\r")


def pause():
    child.expect_exact("Presione una tecla")
    child.send("\r")


def preparation(health):
    child.expect(rf"Salud\s+{ANSI}│{ANSI} {health}/100")
    child.expect(rf"Nivel\s+{ANSI}│{ANSI} 1")
    child.expect(rf"XP\s+{ANSI}│{ANSI} 0/50")
    child.expect(rf"Capacidad de cyberware\s+{ANSI}│{ANSI} 8/20")
    child.expect(rf"RAM\s+{ANSI}│{ANSI} {ANSI}6/6")
    child.expect_exact("¿Qué querés hacer?")
    child.expect_exact("Salir")


def combat(health, fresh_enemy=False):
    child.expect_exact(f"Corpo: {health}/100")
    if fresh_enemy:
        child.expect_exact("Merodeador (Maelstrom): 40/40")
    child.expect_exact("Intención:")
    child.expect_exact("Elegí tu acción:")
    child.expect_exact("Esperar")

def leave_guide():
    child.expect_exact("Guía y práctica · Elegí un tema")
    child.expect_exact("Volver al menú principal")
    child.send("\x1b[A\r")


try:
    start_game(child)
    child.expect_exact("Nomad — Escopeta de corredera")
    choose(0)  # Corpo
    confirm_origin(child, "Corpo")
    preparation(100)

    open_main(child, "progresion")
    child.expect_exact("Nivel 1/20")
    child.expect_exact("¿Qué querés mejorar?")
    child.expect_exact("Volver a preparación")
    choose(2)  # Consulta de cyberware
    child.expect_exact("Capacidad: 8/20")
    child.expect_exact("Instalación y extracción: ripperdoc en preparación.")
    pause()
    child.expect_exact("¿Qué querés mejorar?")
    child.expect_exact("Volver a preparación")
    choose(0)
    child.expect_exact("¿Qué atributo querés aumentar?")
    child.expect_exact("Cancelar")
    choose(4)  # Cool
    child.expect_exact("Cool aumentó a 2.")
    pause()
    child.expect_exact("¿Qué querés mejorar?")
    child.expect_exact("Volver a preparación")
    choose(1)
    child.expect_exact("Elegí un atributo · Puntos de habilidad: 1")
    child.expect_exact("Inteligencia · 1/10 · 1/3 adquiridas")
    child.expect_exact("Cool · 2/10 · 0/3 adquiridas")
    child.expect_exact("Volver a progresión")
    choose(4)
    child.expect_exact("Cool · Puntos de habilidad: 1")
    child.expect_exact("Volver a los atributos")
    branch = child.before
    assert "Sangre fría · +5% acierto con arma · disponible" in branch
    assert "requiere Cool 3, requiere Sangre fría" in branch
    assert "Análisis" not in branch
    choose(0)  # Sangre fría: también está disponible para Corpo.
    child.expect_exact("Aprendiste Sangre fría.")
    pause()
    child.expect_exact("Cool · Puntos de habilidad: 0")
    child.expect_exact("Sangre fría · +5% acierto con arma · adquirida")
    child.expect_exact("Volver a los atributos")
    choose(0)
    child.expect_exact("No tenés puntos de habilidad disponibles.")
    pause()
    child.expect_exact("Cool · Puntos de habilidad: 0")
    child.expect_exact("Volver a los atributos")
    choose(3)
    child.expect_exact("Elegí un atributo · Puntos de habilidad: 0")
    child.expect_exact("Cool · 2/10 · 1/3 adquiridas")
    child.expect_exact("Volver a progresión")
    choose(5)
    child.expect_exact("¿Qué querés mejorar?")
    child.expect_exact("Volver a preparación")
    choose(3)
    preparation(100)

    open_main(child, "recarga")  # Cargador lleno: rechazo sin consumir reserva ni tiempo
    child.expect_exact("Elegí un arma para recargar:")
    child.expect_exact("Cancelar")
    choose(0)
    child.expect_exact("El cargador ya está lleno.")
    pause()
    preparation(100)

    open_main(child, "practica")
    combat(100, fresh_enemy=True)
    choose(6)
    child.expect_exact("Merodeador acierta e inflige 6 de daño.")
    pause()
    combat(94)
    choose(4)
    child.expect_exact("Corpo recupera 6 de salud.")
    pause()
    for shot, health in enumerate((94, 88, 82, 82)):
        combat(health)
        choose(0)
        child.expect_exact("Corpo acierta e inflige")
        if shot < 2:
            child.expect_exact("Merodeador acierta e inflige 6 de daño.")
        elif shot == 2:
            child.expect_exact("Merodeador espera.")
        else:
            child.expect_exact("Victoria en el combate.")
        pause()
    leave_guide()
    preparation(100)

    open_main(child, "practica")  # Perder en la simulación no borra al personaje.
    health = 100
    for turn in range(26):
        combat(health)
        choose(6)
        child.expect_exact("Corpo espera.")
        if turn in (4, 7, 9, 12, 14, 17, 19, 22, 24):
            child.expect_exact("Merodeador espera.")
        else:
            child.expect_exact("Merodeador acierta e inflige")
            health -= 6
        if turn == 25:
            child.expect_exact("Derrota en el combate.")
        pause()
    leave_guide()
    child.expect(rf"Origen\s+{ANSI}│{ANSI} Corpo")
    preparation(100)
    open_main(child, "inventario")
    child.expect_exact("Pistola")
    child.expect_exact("12/12")
    child.expect_exact("Munición de pistola")
    child.expect(rf"Curación.*│{ANSI}\s*2\s*{ANSI}│")
    child.expect_exact("Armas equipadas:")
    child.expect_exact("Volver a preparación")
    choose(4)
    preparation(100)
    open_main(child, "taller")
    child.expect_exact("Componentes: 0")
    child.expect_exact("¿Qué querés hacer en el taller?")
    choose(1)
    child.expect_exact("¿Qué querés desarmar?")
    child.expect_exact("se pierden 12 bala(s) cargadas")
    choose(0)
    child.expect_exact("¿Confirmás el desarme?")
    choose(0)
    child.expect_exact("Componentes: 0")
    child.expect_exact("¿Qué querés hacer en el taller?")
    choose(1)
    child.expect_exact("¿Qué querés desarmar?")
    choose(0)
    child.expect_exact("¿Confirmás el desarme?")
    choose(1)
    child.expect_exact("Obtuviste 4 componente(s).")
    pause()
    child.expect_exact("Componentes: 4")
    child.expect_exact("¿Qué querés hacer en el taller?")
    choose(0)
    child.expect_exact("¿Qué querés fabricar?")
    child.expect_exact("Munición de pistola × 6")
    choose(0)
    child.expect_exact("Cantidad de lotes")
    child.sendline("2")
    child.expect_exact("Fabricaste 12 × Munición de pistola.")
    pause()
    child.expect_exact("Componentes: 2")
    child.expect_exact("¿Qué querés hacer en el taller?")
    choose(1)
    child.expect_exact("¿Qué querés desarmar?")
    child.expect_exact("Curación")
    choose(0)
    child.expect_exact("Cantidad a desarmar")
    child.sendline("2")
    child.expect_exact("¿Confirmás el desarme?")
    choose(1)
    child.expect_exact("Obtuviste 2 componente(s).")
    pause()
    for confirmation in (0, 1):
        child.expect_exact("Componentes: 4")
        child.expect_exact("¿Qué querés hacer en el taller?")
        choose(3)
        child.expect_exact("Militech Paraline · Tier 1 · instalado")
        choose(0)
        child.expect_exact("RAM del deck: 6 → 7.")
        child.expect_exact("Capacidad requerida: 8 → 10.")
        child.expect_exact("¿Confirmás la mejora del implante?")
        choose(confirmation)
    child.expect_exact("Militech Paraline mejorado a Tier 2.")
    pause()
    child.expect_exact("Componentes: 0")
    child.expect_exact("¿Qué querés hacer en el taller?")
    choose(6)
    child.expect(rf"Capacidad de cyberware\s+{ANSI}│{ANSI} 10/20")
    child.expect(rf"RAM\s+{ANSI}│{ANSI} {ANSI}6/7")
    child.expect_exact("Salir")
    child.send("\x1b[A\r")
    child.expect_exact("OK: sesión interactiva terminada.")
    child.expect(pexpect.EOF)
    child.close()
    assert child.exitstatus == 0
    print("OK: victoria y derrota de práctica conservan personaje, salud, munición y curaciones.")
except Exception:
    print(child.logfile_read.getvalue())
    raise
finally:
    if child.isalive():
        child.close(force=True)
