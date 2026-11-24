"""Recorridos Linux por pseudoterminal. Requiere Python 3 y pexpect.

Antes de ejecutar: compilar el juego y ejecutar Project-C.Checks una vez.
"""
import io
import os
from pathlib import Path
import re
import tempfile
from contextlib import contextmanager

import pexpect
from main_menu_ui import confirm_origin, open_main, start_game


ROOT = Path(__file__).resolve().parents[1]
ANSI = re.compile(r"\x1b\[[0-?]*[ -/]*[@-~]")


@contextmanager
def session(fixture=False, discard=False, width=140):
    project = "tests/Project-C.Checks/Project-C.Checks.csproj" if fixture else "Project-C.csproj"
    args = ["run", "--project", project, "--no-build"]
    if fixture:
        args += ["--", "--descarte-interactivo" if discard else "--equipo-interactivo"]
    with tempfile.TemporaryDirectory(prefix="mysharp-ui-",
                                     dir="/tmp/opencode" if Path("/tmp/opencode").is_dir() else None) as data_dir:
        child = pexpect.spawn(
            "dotnet", args, cwd=str(ROOT), encoding="utf-8", timeout=20,
            dimensions=(40, width),
            env={**os.environ, "TERM": "xterm-256color", "MYSHARPVENTURE_DATA_DIR": data_dir},
        )
        log = io.StringIO()
        child.logfile_read = log
        try:
            yield child
        except Exception:
            print(log.getvalue())
            raise
        finally:
            if child.isalive():
                child.close(force=True)


def choose(child, index=0):
    for _ in range(index):
        child.send("\x1b[B")
    child.send("\r")


def main_menu(child):
    child.expect_exact("¿Qué querés hacer?")
    text = ANSI.sub("", child.before)
    child.expect_exact("Salir")
    assert "100/100" in text, text


def inventory(child):
    child.expect_exact("¿Qué querés hacer con tu equipo?")
    text = ANSI.sub("", child.before)
    child.expect_exact("Descartar objeto")
    child.expect_exact("Volver a preparación")
    rows = {}
    for line in text.splitlines():
        cells = [cell.strip() for cell in line.split("│")[1:-1]]
        if cells and cells[0].isdigit():
            rows[int(cells[0])] = cells[1:]
    equipped = {number for number, cells in rows.items() if cells[-1] == "Equipada"}
    assert f"Armas equipadas: {len(equipped)}/3" in text, text
    return rows, equipped


def acknowledge(child, message):
    child.expect_exact(message)
    child.expect_exact("Presione una tecla")
    child.send("\r")
    return inventory(child)


def select_weapon(child, equip, index):
    choose(child, 0 if equip else 1)
    child.expect_exact("Elegí el arma que querés equipar:" if equip
                       else "Elegí el arma que querés desequipar:")
    child.expect_exact("Cancelar")
    choose(child, index)


def finish(child, fixture=False, discard=False):
    choose(child, 4)
    main_menu(child)
    child.send("\x1b[A\r")  # Salir
    if fixture:
        child.expect_exact("OK: descarte interactivo de una instancia sin alterar equipo ni recursos restantes."
                           if discard else
                           "OK: escenario interactivo sin pérdidas, duplicación ni cambios de recursos.")
    child.expect(pexpect.EOF)
    child.close()
    assert child.exitstatus == 0, child.exitstatus


def check_origin(index, origin, name, capacity):
    with session() as child:
        start_game(child)
        child.expect_exact("Nomad — Escopeta de corredera")
        choose(child, index)
        child.expect_exact(f"Kit inicial · {origin}")
        child.expect(rf"Arma equipada:{ANSI.pattern} {re.escape(name)}")
        child.expect_exact(f"Cargador: {capacity}/{capacity} · Reserva: {capacity}")
        child.expect_exact(("Análisis", "Sangre fría", "Puños de acero")[index])
        child.expect_exact("2 curaciones")
        child.expect_exact("Militech Paraline con Cortocircuito")
        child.expect_exact("Puntos de atributo: 5 · Puntos de habilidad: 1")
        confirm_origin(child, origin)
        main_menu(child)
        open_main(child, "inventario")
        original, equipped = inventory(child)
        assert equipped == {1} and original[1][0] == name, original
        assert original[1][1] == "Tier 1", original
        assert original[1][2:4] == ["1", f"{capacity}/{capacity}"], original
        assert original[2][2] == str(capacity) and original[3][2] == "2", original
        assert original[1][4] == ("1.62 kg", "3.3 kg", "4.56 kg")[index], original
        assert original[2][4] == f"{capacity * 10} g" and original[3][4] == "500 g", original

        choose(child)
        assert acknowledge(child, "No tenés armas sin equipar en el inventario.") == (original, {1})
        select_weapon(child, equip=False, index=0)
        unequipped, equipped = acknowledge(child, f"{name}: desequipada.")
        assert equipped == set()
        expected = {key: cells.copy() for key, cells in original.items()}
        expected[1][-1] = "—"
        assert unequipped == expected, unequipped

        choose(child, 1)
        assert acknowledge(child, "No tenés armas equipadas.") == (expected, set())
        select_weapon(child, equip=True, index=1)  # Cancelar
        assert inventory(child) == (expected, set())
        select_weapon(child, equip=True, index=0)
        assert acknowledge(child, f"{name}: equipada.") == (original, {1})
        select_weapon(child, equip=False, index=1)  # Cancelar
        assert inventory(child) == (original, {1})

        choose(child, 4)
        main_menu(child)
        open_main(child, "inventario")
        assert inventory(child) == (original, {1})
        finish(child)
    print(f"OK: {origin}: equipar, desequipar, cancelar, mensajes sin candidatas y reingreso.")


def check_multiple_weapons():
    with session(fixture=True) as child:
        main_menu(child)
        open_main(child, "inventario")
        original, equipped = inventory(child)
        assert equipped == {1} and set(original) == {1, 2, 3, 4, 5, 6}, original

        def verify(expected):
            rows, equipped = inventory(child)
            assert equipped == expected, (rows, expected)
            assert {key: cells[:-1] for key, cells in rows.items()} == {
                key: cells[:-1] for key, cells in original.items()
            }, rows
            return rows, equipped

        def operation(equip, index, expected):
            select_weapon(child, equip, index)
            child.expect_exact("Pistola: equipada." if equip else "Pistola: desequipada.")
            child.expect_exact("Presione una tecla")
            child.send("\r")
            return verify(expected)

        operation(True, 0, {1, 4})  # Pistola vacía #4
        operation(True, 1, {1, 4, 6})  # Elegir #6, saltando #5
        choose(child)
        child.expect_exact("Solo podés equipar hasta tres armas.")
        child.expect_exact("Presione una tecla")
        child.send("\r")
        verify({1, 4, 6})
        operation(False, 1, {1, 6})  # Desequipar exactamente #4
        operation(True, 1, {1, 5, 6})  # Elegir #5, saltando #4
        choose(child, 4)
        main_menu(child)
        open_main(child, "inventario")
        verify({1, 5, 6})
        finish(child, fixture=True)
    print("OK: cuatro armas iguales: selección por instancia, rechazo de cuarta y reutilización de ranura.")


def discard_pick(child, index):
    choose(child, 2)
    child.expect_exact("¿Qué objeto querés descartar?")
    child.expect_exact("Cancelar")
    choose(child, index)


def discard_confirm(child, confirm):
    child.expect_exact("¿Confirmás el descarte?")
    child.expect_exact("Confirmar descarte")
    choose(child, 1 if confirm else 0)


def check_discard_origin(index, origin, name, capacity):
    with session() as child:
        start_game(child)
        child.expect_exact("Nomad — Escopeta de corredera")
        choose(child, index)
        confirm_origin(child, origin)
        main_menu(child)
        open_main(child, "inventario")
        original, equipped = inventory(child)
        assert equipped == {1}, original

        discard_pick(child, 3)  # Cancelar selección
        assert inventory(child) == (original, {1})

        discard_pick(child, 2)  # Curación
        child.expect_exact("0 cancela")
        child.sendline("3")  # Cantidad fuera de rango: debe repreguntar
        child.expect_exact("Ingresá una cantidad entera dentro del rango.")
        child.expect_exact("0 cancela")
        child.sendline("0")
        assert inventory(child) == (original, {1})

        discard_pick(child, 2)
        child.expect_exact("0 cancela")
        child.sendline("1")
        child.expect_exact("Vas a descartar 1 × Curación")
        discard_confirm(child, confirm=False)
        assert inventory(child) == (original, {1})

        discard_pick(child, 2)
        child.expect_exact("0 cancela")
        child.sendline("1")
        discard_confirm(child, confirm=True)
        rows, equipped = acknowledge(child, "Descartaste 1 × Curación.")
        assert equipped == {1} and rows[3][2] == "1" and rows[2] == original[2], rows

        discard_pick(child, 0)  # Arma equipada cargada
        child.expect_exact(f"se perderán también {capacity} balas cargadas")
        child.expect_exact("cambia")
        discard_confirm(child, confirm=False)
        assert inventory(child) == (rows, {1})

        discard_pick(child, 0)
        child.expect_exact(f"se perderán también {capacity} balas cargadas")
        child.expect_exact("cambia")
        discard_confirm(child, confirm=True)
        child.expect_exact(f"Descartaste 1 × {name}.")
        child.expect_exact("Presione una tecla")
        child.send("\r")
        remaining, equipped = inventory(child)
        assert equipped == set() and len(remaining) == 2
        assert remaining[1][0] == original[2][0] and remaining[1][2] == str(capacity), remaining
        assert remaining[2][0] == "Curación" and remaining[2][2] == "1", remaining

        discard_pick(child, 0)  # Reserva entera
        child.expect_exact("0 cancela")
        child.sendline(str(capacity))
        discard_confirm(child, confirm=True)
        child.expect_exact(f"Descartaste {capacity} × {original[2][0]}.")
        child.expect_exact("Presione una tecla")
        child.send("\r")
        remaining, equipped = inventory(child)
        assert equipped == set() and len(remaining) == 1 and remaining[1][2] == "1"

        discard_pick(child, 0)  # Última curación: inventario vacío
        child.expect_exact("0 cancela")
        child.sendline("1")
        discard_confirm(child, confirm=True)
        child.expect_exact("Descartaste 1 × Curación.")
        child.expect_exact("Presione una tecla")
        child.send("\r")
        assert inventory(child) == ({}, set())
        choose(child, 2)
        assert acknowledge(child, "No tenés objetos para descartar.") == ({}, set())
        choose(child, 4)
        main_menu(child)
        open_main(child, "inventario")
        assert inventory(child) == ({}, set())
        finish(child)
    print(f"OK: {origin}: descarte parcial/total, cancelaciones, cargador perdido y vacío navegable.")


def check_discard_equal_weapons():
    with session(fixture=True, discard=True) as child:
        main_menu(child)
        open_main(child, "inventario")
        original, equipped = inventory(child)
        assert equipped == {1} and len(original) == 6
        discard_pick(child, 4)  # Pistola #5 cargada, no la #1 equipada ni la #4 vacía
        child.expect_exact("se perderán también 12 balas cargadas")
        child.expect_exact("cambia")
        discard_confirm(child, confirm=True)
        child.expect_exact("Descartaste 1 × Pistola.")
        child.expect_exact("Presione una tecla")
        child.send("\r")
        rows, equipped = inventory(child)
        assert equipped == {1} and len(rows) == 5
        assert rows[4][3] == "0/12" and rows[5][3] == "0/12", rows
        assert rows[2] == original[2] and rows[3] == original[3], rows
        finish(child, fixture=True, discard=True)
    print("OK: armas iguales: descarte de una instancia sin alterar la equipada ni los recursos.")


def check_narrow_weights():
    with session(width=40) as child:
        start_game(child)
        child.expect_exact("Escopeta de corredera")
        choose(child)
        confirm_origin(child, "Corpo")
        main_menu(child)
        open_main(child, "inventario")
        for weight in ("1.62 kg", "120 g", "500 g"):
            child.expect_exact(f"Peso: {weight}")
        child.expect_exact("Volver a preparación")
        finish(child)
        print("OK: peso de cada objeto en terminal de 40 columnas.")


check_narrow_weights()
for index, (origin, name, capacity) in enumerate([
    ("Corpo", "Pistola", 12),
    ("Street", "Subfusil", 30),
    ("Nomad", "Escopeta de corredera", 6),
]):
    check_origin(index, origin, name, capacity)
check_multiple_weapons()
for index, (origin, name, capacity) in enumerate([
    ("Corpo", "Pistola", 12),
    ("Street", "Subfusil", 30),
    ("Nomad", "Escopeta de corredera", 6),
]):
    check_discard_origin(index, origin, name, capacity)
check_discard_equal_weapons()
