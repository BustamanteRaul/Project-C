"""Select an action from the grouped main menu in a terminal test."""

def start_game(child):
    child.expect_exact("Bienvenido a Night City")
    child.expect_exact("Solo el stash se guarda al salir.")
    child.expect_exact("Empezar")
    child.send("\r")


def confirm_origin(child, origin):
    child.expect_exact(f"¿Confirmás el origen {origin}?")
    child.expect_exact("Crear personaje")
    child.send("\x1b[B\r")


def open_main(child, action):
    routes = {
        "aventura": (0,),
        "inventario": (1, 0),
        "progresion": (1, 1),
        "curacion": (2, 0),
        "recarga": (2, 1),
        "stash": (2, 2),
        "tienda": (3, 0),
        "taller": (3, 1),
        "ripperdoc": (3, 2),
        "practica": (4, 5),
    }
    prompts = ("", "Personaje", "Preparación", "Servicios", "Guía y práctica")
    route = routes[action]
    for depth, index in enumerate(route):
        if depth:
            child.expect_exact(f"{prompts[route[0]]} · Elegí " + ("un tema" if route[0] == 4 else "una opción"))
        for _ in range(index):
            child.send("\x1b[B")
        child.send("\r")


def leave_services(child, *prompts):
    for prompt in prompts:
        child.expect_exact(prompt)
        child.send("\x1b[A\r")
