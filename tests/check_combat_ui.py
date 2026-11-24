"""Recorridos de combate con tiradas controladas. Requiere Python 3 y pexpect."""
import io
import os
from pathlib import Path
import pexpect

ROOT = Path(__file__).resolve().parents[1]


def start(mode, origin_index=0, width=140, color=True):
    env = {**os.environ, "TERM": "xterm-256color"}
    env.pop("NO_COLOR", None)
    if not color:
        env["NO_COLOR"] = "1"
    child = pexpect.spawn("dotnet", [
        "run", "--project", "tests/Project-C.Checks/Project-C.Checks.csproj",
        "--no-build", "--", mode
    ], cwd=str(ROOT), encoding="utf-8", timeout=20,
        dimensions=(40, width), env=env)
    child.logfile_read = io.StringIO()
    child.expect_exact("Elegí el origen del combate de prueba:")
    child.expect_exact("Nomad")
    choose(child, origin_index)
    return child


def choose(child, index):
    for _ in range(index):
        child.send("\x1b[B")
    child.send("\r")


def menu(child, narrow=False):
    child.expect_exact("Intención:")
    before = child.before
    child.expect_exact("Elegí tu acción:")
    overview = before + child.before
    child.expect_exact("Esperar")
    assert "Daño" in child.before and "Recargar" in child.before \
        and "Cortocircuito" in child.before, child.before
    return overview


def pause(child):
    child.expect_exact("Presione una tecla")
    child.send("\r")


def end(child, expected):
    child.expect_exact(f"Combate de prueba finalizado: {expected}.")
    child.expect(pexpect.EOF)
    child.close()
    assert child.exitstatus == 0, child.exitstatus


def victory():
    child = start("--combate-prueba")
    try:
        menu(child)
        choose(child, 5)  # Elegir arma, cancelar sin coste
        child.expect_exact("Elegí un arma equipada:")
        child.expect_exact("Cancelar")
        choose(child, 2)
        menu(child)
        choose(child, 5)
        child.expect_exact("Elegí un arma equipada:")
        child.expect_exact("Cancelar")
        choose(child, 1)  # Pistola sin munición
        child.expect_exact("Arma seleccionada: Pistola.")
        pause(child)
        menu(child)
        choose(child, 0)
        child.expect_exact("El arma no tiene suficiente munición cargada.")
        pause(child)
        menu(child)
        choose(child, 1)
        child.expect_exact("Corpo acierta e inflige 5 de daño.")
        if os.environ.get("MYSHARPVENTURE_NO_ANIMATIONS") != "1":
            child.send(" ")
        child.expect_exact("Merodeador acierta e inflige 6 de daño.")
        pause(child)
        overview = menu(child)
        assert "\x1b[2J" not in overview and "\x1b[3J" not in overview, "Se borró el historial de combate."
        choose(child, 6)
        child.expect_exact("Corpo espera.")
        child.expect_exact("Merodeador acierta e inflige 6 de daño.")
        pause(child)
        menu(child)
        choose(child, 5)
        child.expect_exact("Elegí un arma equipada:")
        child.expect_exact("Cancelar")
        choose(child, 0)  # Arma original llena
        child.expect_exact("Arma seleccionada: Pistola.")
        pause(child)
        for index in range(3):
            menu(child)
            choose(child, 0)
            child.expect_exact("Corpo acierta e inflige")
            if index < 2:
                child.expect_exact("Merodeador acierta e inflige 6 de daño.")
            else:
                child.expect_exact("Victoria en el combate.")
            pause(child)
        end(child, "victoria")
        print("OK: combate interactivo: selección/cancelación gratis, rechazo sin turno, básico, espera y victoria sin respuesta final.")
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)


def defeat():
    child = start("--combate-derrota-prueba")
    try:
        for index in range(26):
            menu(child)
            choose(child, 6)
            child.expect_exact("Corpo espera.")
            child.expect_exact("Merodeador espera." if index in (4, 7, 9, 12, 14, 17, 19, 22, 24)
                               else "Merodeador acierta e inflige")
            if index == 25:
                child.expect_exact("Derrota en el combate.")
            pause(child)
        end(child, "derrota")
        print("OK: combate interactivo: una respuesta por espera y derrota final.")
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)


def other_origins():
    for origin_index, (origin, damage, shots) in enumerate([
        ("Street", 10, 4), ("Nomad", 18, 3)
    ], start=1):
        child = start("--combate-prueba", origin_index)
        try:
            for index in range(shots):
                overview = menu(child)
                if origin == "Nomad":
                    assert f"Cargador {6 - index}/6 · Reserva: 6" in overview
                    assert "Gasta: 1 cartucho" in child.before
                choose(child, 0)
                child.expect_exact(f"{origin} acierta e inflige")
                if index < shots - 1:
                    child.expect_exact("Merodeador acierta e inflige 6 de daño.")
                else:
                    child.expect_exact("Victoria en el combate.")
                pause(child)
            end(child, "victoria")
            print(f"OK: {origin}: combate interactivo de {shots} ataques y victoria.")
        except Exception:
            print(child.logfile_read.getvalue())
            raise
        finally:
            if child.isalive():
                child.close(force=True)


def reload_and_heal():
    child = start("--combate-prueba")
    try:
        overview = menu(child)
        assert "100/100" in overview and "40/40" in overview, overview
        choose(child, 3)  # Recargar cargador lleno
        child.expect_exact("El cargador ya está lleno.")
        pause(child)
        menu(child)
        choose(child, 4)  # Curar salud completa
        child.expect_exact("La salud ya está completa.")
        pause(child)
        menu(child)
        choose(child, 5)
        child.expect_exact("Elegí un arma equipada:")
        child.expect_exact("Cancelar")
        choose(child, 1)  # Segunda pistola vacía
        pause(child)
        overview = menu(child)
        assert "Cargador 0/12" in overview and "Atacar con arma" not in overview, overview
        choose(child, 3)
        child.expect_exact("Corpo recarga 12 bala(s).")
        child.expect_exact("Merodeador acierta e inflige 6 de daño.")
        pause(child)
        overview = menu(child)
        assert "Cargador 12/12" in overview and "94/100" in overview
        choose(child, 4)
        child.expect_exact("Corpo recupera 6 de salud.")
        child.expect_exact("Merodeador acierta e inflige 6 de daño.")
        pause(child)
        for shot in range(4):
            menu(child)
            choose(child, 0)
            child.expect_exact("Corpo acierta e inflige")
            if shot == 2:
                child.expect_exact("Merodeador espera.")
            elif shot < 3:
                child.expect_exact("Merodeador acierta e inflige 6 de daño.")
            else:
                child.expect_exact("Victoria en el combate.")
            pause(child)
        end(child, "victoria")
        print("OK: interfaz de recarga/curación: rechazos gratis, reserva consumida y respuesta única.")
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)


def narrow_combat():
    child = start("--combate-prueba", width=40)
    try:
        for shot in range(4):
            menu(child, narrow=True)
            choose(child, 0)
            child.expect_exact("Corpo acierta e inflige")
            if shot < 3:
                child.expect_exact("Respuesta enemiga: Merodeador")
                child.expect_exact("acierta e inflige 6 de daño.")
            else:
                child.expect_exact("Victoria en el combate.")
            pause(child)
        end(child, "victoria")
        print("OK: combate navegable e historial visible en terminal de 40 columnas.")
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)

def quickhack():
    child = start("--combate-prueba")
    try:
        for _ in (6, 4):
            menu(child)
            choose(child, 2)
            child.expect_exact("Corpo ejecuta Cortocircuito: 12 de daño (gasta 3 RAM).")
            child.expect_exact("Merodeador acierta e inflige")
            pause(child)
        menu(child)
        choose(child, 2)
        child.expect_exact("RAM insuficiente para Cortocircuito.")
        pause(child)
        menu(child)
        choose(child, 6)
        child.expect_exact("Corpo espera.")
        child.expect_exact("Merodeador acierta e inflige")
        pause(child)
        menu(child)
        choose(child, 2)
        child.expect_exact("Corpo ejecuta Cortocircuito: 12 de daño (gasta 3 RAM).")
        child.expect_exact("Merodeador acierta e inflige")
        pause(child)
        menu(child)
        choose(child, 0)
        child.expect_exact("Victoria en el combate.")
        pause(child)
        end(child, "victoria")
        print("OK: Cortocircuito, RAM insuficiente, regeneración y victoria en interfaz.")
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)


def overheat_summary(width, color):
    child = start("--combate-pulso-prueba", width=width, color=color)
    try:
        child.expect_exact("Corpo: 100/100")
        child.expect_exact("RAM: 6/6")
        child.expect_exact("Merodeador (Maelstrom): 18/18")
        child.expect_exact("Elegí tu acción:")
        child.expect_exact("Sobrecalentamiento")
        child.expect_exact("4 RAM")
        child.expect_exact("6 ahora")
        child.expect_exact("2 pulsos de 6")
        choose(child, 3)
        child.expect_exact("Resumen del turno 1")
        child.expect_exact("Tu acción: Corpo ejecuta")
        child.expect_exact("Pulso: Sobrecalentamiento inflige")
        child.expect_exact("Respuesta enemiga:")
        pause(child)
        child.expect_exact("RAM: 3/6")
        child.expect_exact("Merodeador (Maelstrom): 6/18")
        child.expect_exact("Sobrecalentado · 1 pulso pendiente")
        child.expect_exact("Sobrecalentamiento —")
        child.expect_exact("No disponible:")
        child.expect_exact("Sobrecalentamiento ya está activo.")
        choose(child, 3)
        child.expect_exact("Sobrecalentamiento ya está activo.")
        pause(child)
        child.expect_exact("Turno 2 · Combatientes")
        child.expect_exact("Sobrecalentado · 1 pulso pendiente")
        child.expect_exact("Esperar")
        choose(child, 7)
        child.expect_exact("Resumen del turno 2")
        child.expect_exact("Tu acción: Corpo espera.")
        child.expect_exact("Pulso: Sobrecalentamiento inflige")
        child.expect_exact("El enemigo cae antes de responder")
        assert "Respuesta enemiga:" not in child.before
        child.expect_exact("Victoria en el combate.")
        assert "Respuesta enemiga:" not in child.before
        pause(child)
        end(child, "victoria")
        print(f"OK: resumen, relanzamiento y pulso letal en {width} columnas, color={color}.")
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)


def synapse_preview():
    child = start("--combate-sinaptico-prueba", width=80)
    try:
        child.expect_exact("Elegí tu acción:")
        child.expect_exact("Quemado sináptico")
        child.expect_exact("3 RAM")
        child.expect_exact("8 de daño")
        choose(child, 2)
        child.expect_exact("ejecuta Cortocircuito: 12 de daño")
        pause(child)
        child.expect_exact("Elegí tu acción:")
        child.expect_exact("Quemado sináptico")
        child.expect_exact("3 RAM")
        child.expect_exact("14 de daño")
        choose(child, 3)
        child.expect_exact("ejecuta Quemado sináptico: 14 de daño")
        child.expect_exact("Victoria en el combate.")
        assert "Respuesta enemiga:" not in child.before
        pause(child)
        end(child, "victoria")
        print("OK: Quemado sináptico actualiza el daño mostrado tras gastar RAM.")
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)


def weapon_glitch():
    child = start("--combate-bloqueo-prueba", width=80)
    try:
        child.expect_exact("Intención: Prepara un disparo.")
        child.expect_exact("Fallo de arma")
        child.expect_exact("Bloquea disparo o recarga")
        choose(child, 3)
        child.expect_exact("Arma bloqueada: el enemigo no dispara.")
        pause(child)
        child.expect_exact("Intención: Prepara un disparo.")
        child.expect_exact("Esperar")
        choose(child, 7)
        child.expect_exact("Respuesta enemiga: Merodeador acierta")
        pause(child)
        child.expect_exact("Intención: Prepara una recarga.")
        child.expect_exact("La recarga no inflige daño.")
        child.expect_exact("Esperar")
        choose(child, 3)
        child.expect_exact("Arma bloqueada: el enemigo no recarga.")
        pause(child)
        child.expect_exact("Intención: Prepara una recarga.")
        child.expect_exact("Esperar")
        choose(child, 7)
        child.expect_exact("Respuesta enemiga: Merodeador recarga 1 bala(s).")
        pause(child)
        for _ in range(3):
            child.expect_exact("Esperar")
            choose(child, 0)
            child.expect_exact("Corpo acierta e inflige")
            pause(child)
        end(child, "victoria")
        print("OK: Fallo de arma distingue disparo y recarga bloqueados.")
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)


def cyberware_defense():
    child = start("--combate-cyberware-prueba", width=80)
    try:
        child.expect_exact("Self-ICE listo")
        child.expect_exact("Intención: Prepara Fallo de cyberware.")
        child.expect_exact("Self-ICE bloqueará el efecto")
        choose(child, 3)
        child.expect_exact("Self-ICE bloqueó el quickhack.")
        child.expect_exact("Self-ICE bloqueó el quickhack.")
        pause(child)
        child.expect_exact("Intención: Prepara Cortocircuito.")
        child.expect_exact("Esperar")
        choose(child, 7)
        child.expect_exact("ejecuta Cortocircuito: 9 de daño")
        pause(child)
        child.expect_exact("Esperar")
        choose(child, 3)
        child.expect_exact("Implantes desactivados.")
        child.expect_exact("Respuesta enemiga: Netrunner acierta")
        pause(child)
        child.expect_exact("Cyberware desactivado · turnos: 1")
        child.expect_exact("Esperar")
        choose(child, 7)
        child.expect_exact("Cyberware enemigo reactivado.")
        pause(child)
        for _ in range(3):
            child.expect_exact("Esperar")
            choose(child, 0)
            child.expect_exact("Corpo acierta e inflige")
            pause(child)
        end(child, "victoria")
        print("OK: Self-ICE, desactivación y reactivación visibles en combate.")
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)


def sandevistan_status(width, color):
    child = start("--combate-sandevistan-prueba", width=width, color=color)
    try:
        child.expect_exact("Sandevistan listo")
        assert "RAM:" not in child.before
        child.expect_exact("Activar Sandevistan")
        choose(child, 2)
        child.expect_exact("SANDEVISTAN ACTIVO")
        child.expect_exact("Te queda 1 acción en este turno.")
        child.expect_exact("Acierto garantizado")
        child.expect_exact("El enemigo no responderá.")
        child.expect_exact("Turno 1 · Combatientes")
        child.expect_exact("Esperar")
        choose(child, 0)
        child.expect_exact("Resumen del turno 1")
        child.expect_exact("Sandevistan: el enemigo pierde")
        assert "Respuesta enemiga:" not in child.before
        pause(child)
        child.expect_exact("Turno 2 · Combatientes")
        child.expect_exact("enfriamiento: 4/4")
        assert "SANDEVISTAN ACTIVO" not in child.before
        child.expect_exact("Esperar")
        choose(child, 0)
        child.expect_exact("Victoria en el combate.")
        pause(child)
        end(child, "victoria")
        print(f"OK: Sandevistan activo y enfriamiento en {width} columnas, color={color}.")
    except Exception:
        print(child.logfile_read.getvalue())
        raise
    finally:
        if child.isalive():
            child.close(force=True)


def instant_execution():
    child = start("--combate-ejecucion-prueba", width=80)
    try:
        child.expect_exact("RAM: 8/8")
        child.expect_exact("Suicidio")
        child.expect_exact("8 RAM")
        child.expect_exact("Ejecución instantánea")
        choose(child, 3)
        child.expect_exact("ejecuta Suicidio (gasta 8 RAM).")
        child.expect_exact("El enemigo cae sin responder.")
        child.expect_exact("Victoria en el combate.")
        assert "Respuesta enemiga:" not in child.before
        pause(child)
        end(child, "victoria")
    finally:
        if child.isalive():
            child.close(force=True)


def robot_target():
    child = start("--combate-bot-prueba", width=80)
    try:
        child.expect_exact("Tipo de enemigo: Bot")
        child.expect_exact("Cortocircuito")
        child.expect_exact("18 de daño")
        child.expect_exact("Suicidio solo afecta a objetivos humanos.")
        choose(child, 3)
        child.expect_exact("Suicidio solo afecta a objetivos humanos.")
        pause(child)
        child.expect_exact("Turno 1 · Combatientes")
        child.expect_exact("RAM: 8/8")
        child.expect_exact("Esperar")
        choose(child, 2)
        child.expect_exact("ejecuta Cortocircuito: 18 de daño")
        child.expect_exact("Victoria en el combate.")
        pause(child)
        end(child, "victoria")
    finally:
        if child.isalive():
            child.close(force=True)


def stamina():
    child = start("--combate-prueba")
    try:
        for current in (12, 10, 8, 6):
            child.expect_exact(f"Stamina: {current}/12")
            child.expect_exact("5 stamina")
            choose(child, 1)
            child.expect_exact("Corpo acierta e inflige 5 de daño.")
            pause(child)
        child.expect_exact("Stamina: 4/12")
        child.expect_exact("Intención: Espera para recuperar stamina.")
        child.expect_exact("Stamina insuficiente: necesitás 5.")
        choose(child, 1)
        child.expect_exact("Stamina insuficiente: necesitás 5.")
        pause(child)
        child.expect_exact("Turno 5 · Combatientes")
        child.expect_exact("Stamina: 4/12")
        child.expect_exact("Esperar")
        choose(child, 6)
        child.expect_exact("Merodeador espera.")
        pause(child)
        child.expect_exact("Stamina: 7/12")
        for damage in (12, 8):
            child.expect_exact("Esperar")
            choose(child, 0)
            child.expect_exact(f"Corpo acierta e inflige {damage} de daño")
            pause(child)
        end(child, "victoria")
    finally:
        if child.isalive():
            child.close(force=True)


def shield():
    child = start("--combate-escudo-prueba", width=80)
    try:
        child.expect_exact("Escudo: 12/12")
        child.expect_exact("RAM: 6/6")
        child.expect_exact("Esperar")
        choose(child, 6)
        child.expect_exact("Merodeador acierta e inflige 3 de daño.")
        child.expect_exact("Escudo del jugador: evitó 3 de daño.")
        pause(child)
        child.expect_exact("Corpo: 97/100")
        child.expect_exact("Escudo: 11/12")
        for _ in range(2):
            child.expect_exact("Esperar")
            choose(child, 0)
            pause(child)
        end(child, "victoria")
    finally:
        if child.isalive():
            child.close(force=True)


def critical_hits():
    child = start("--combate-critico-prueba", width=140)
    try:
        child.expect_exact("Acierto")
        child.expect_exact("Prob. de crítico si acertás:")
        child.expect_exact("10 %")
        child.expect_exact("Daño crítico:")
        child.expect_exact("18")
        child.expect_exact("(×1,5)")
        child.expect_exact("Esperar")
        choose(child, 0)
        child.expect_exact("¡Crítico!")
        child.expect_exact("Corpo acierta e inflige 18 de daño")
        pause(child)
        child.expect_exact("Esperar")
        choose(child, 1)
        child.expect_exact("¡Crítico!")
        child.expect_exact("Corpo acierta e inflige 7 de daño.")
        pause(child)
        child.expect_exact("Esperar")
        choose(child, 0)
        child.expect_exact("¡Crítico!")
        child.expect_exact("Corpo acierta e inflige 15 de daño")
        child.expect_exact("Victoria en el combate.")
        pause(child)
        end(child, "victoria")
    finally:
        if child.isalive():
            child.close(force=True)


def gorilla_arms():
    child = start("--combate-brazos-prueba")
    try:
        for _ in range(2):
            child.expect_exact("Gorilla Arms —")
            child.expect_exact("7 stamina")
            child.expect_exact("Esperar")
            choose(child, 1)
            child.expect_exact("Corpo usa Gorilla Arms: 9 de daño")
            pause(child)
        child.expect_exact("Ataque básico —")
        child.expect_exact("Stamina insuficiente: necesitás 5.")
        child.expect_exact("Esperar")
        choose(child, 0)
        child.expect_exact("Victoria en el combate.")
        pause(child)
        end(child, "victoria")
    finally:
        if child.isalive():
            child.close(force=True)


def projectile_launcher():
    child = start("--combate-lanzador-prueba")
    try:
        child.expect_exact("Projectile Launch System —")
        child.expect_exact("10 stamina")
        child.expect_exact("Esperar")
        choose(child, 1)
        child.expect_exact("Corpo usa Projectile Launch System: 15 de daño")
        pause(child)
        child.expect_exact("Ataque básico —")
        child.expect_exact("5 stamina")
        child.expect_exact("Esperar")
        choose(child, 1)
        child.expect_exact("Corpo acierta e inflige 5 de daño.")
        pause(child)
        child.expect_exact("Esperar")
        choose(child, 6)
        child.expect_exact("Corpo espera.")
        pause(child)
        child.expect_exact("Esperar")
        choose(child, 0)
        child.expect_exact("Victoria en el combate.")
        pause(child)
        end(child, "victoria")
    finally:
        if child.isalive():
            child.close(force=True)


def enemy_control():
    child = start("--combate-control-prueba")
    try:
        child.expect_exact("Intención: Prepara Fallo de arma.")
        child.expect_exact("Esperar")
        choose(child, 6)
        child.expect_exact("Netrunner ejecuta Fallo de arma")
        pause(child)
        child.expect_exact("Fallo de arma: Pistola no puede disparar ni recargar.")
        child.expect_exact("Intención: Prepara Reboot Optics.")
        child.expect_exact("Esperar")
        choose(child, 6)
        child.expect_exact("Netrunner ejecuta Reboot Optics")
        pause(child)
        child.expect_exact("Reboot Optics: acierto físico limitado al 5 %.")
        child.expect_exact("Esperar")
        choose(child, 0)
        child.expect_exact("Victoria en el combate.")
        pause(child)
        end(child, "victoria")
    finally:
        if child.isalive():
            child.close(force=True)


def melee_weapon():
    child = start("--combate-espada-prueba")
    try:
        child.expect_exact("Arma seleccionada: Espada")
        child.expect_exact("cuerpo a cuerpo")
        child.expect_exact("Elegí tu acción:")
        child.expect_exact("Esperar")
        assert "Recargar" not in child.before
        assert "Ataque básico" in child.before
        choose(child, 0)
        child.expect_exact("Corpo acierta e inflige 16 de daño.")
        child.expect_exact("Victoria en el combate.")
        pause(child)
        end(child, "victoria")
    finally:
        if child.isalive():
            child.close(force=True)


melee_weapon()
gorilla_arms()
projectile_launcher()
enemy_control()
critical_hits()
shield()
stamina()
victory()
defeat()
other_origins()
reload_and_heal()
narrow_combat()
quickhack()
for columns in (40, 80):
    for use_color in (False, True):
        overheat_summary(columns, use_color)
synapse_preview()
weapon_glitch()
cyberware_defense()
sandevistan_status(40, False)
sandevistan_status(80, True)
instant_execution()
robot_target()
