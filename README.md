# MySharpVenture

Las etapas pendientes del SDD y sus resultados de pruebas se registran en
[Etapas posteriores al MVP](docs/etapas-post-mvp.md).

En el menú principal, **Guía y práctica** reúne un manual por temas,
una ruta para la primera aventura y el combate de prueba. Leer la guía
no gasta recursos. El combate de prueba usa una copia del personaje y
no tiene consecuencias para la partida.

**Espacio** salta las barras de transición y revela de inmediato el
resumen de combate. Para quitar los efectos durante la sesión, elegí
**Guía y práctica → Desactivar animaciones**. También podés iniciar el
juego con `MYSHARPVENTURE_NO_ANIMATIONS=1` en el entorno.
Al abrir el juego, la bienvenida de Night City ofrece **Empezar** o
**Salir**. Si perdés un personaje, volvés directamente a elegir origen.

RPG de terminal en C# y Spectre.Console. Desarrollo por etapas.

## Estado del MVP (preparación M05)

Bienvenida, creación Corpo/Street/Nomad, preparación, consulta del kit inicial
y equipar/desequipar hasta tres armas o descartar objetos desde inventario.
Desde preparación se puede curar, practicar un combate o ir de aventura a
**Watson**: cinco eventos de combate/hallazgo, botín seleccionable con cantidades,
pausa y continuación manual. La salud persiste entre eventos; solo el regreso
exitoso la recupera por completo. Morir elimina personaje y carga, pero no el
stash. El catálogo reducido y el stash usan SQLite; **solo el stash persiste**.
El personaje y su inventario transportado existen en memoria y se pierden al cerrar.
M05 añade el paquete Windows x64 y el recorrido de aceptación. El usuario
confirmó funcionamiento en Windows 11, Watson, persistencia del stash y muerte;
**PM08 aún requiere confirmar la ejecución sin conexión y registrar el acta**.
La interfaz muestra un título grande **MySharp** amarillo / **Venture** azul
(texto compacto en terminales estrechas). Estado, inventario, combate, botín,
mensajes y pausas usan bloques con bordes de distintos colores, centrados y
adaptados al ancho disponible. Con menos de 80 columnas, el inventario se
muestra en fichas por objeto en lugar de una tabla comprimida. Los selectores
interactivos conservan el diseño nativo de Spectre.Console y navegación
circular (flecha arriba desde la primera opción vuelve a la última). Las
armas indican tier con número y color; el color no es el único indicador.

El SDD está en `MySharpVenture-SDD-v1.0/MySharpVenture-SDD-v1.0.md`.
Describe el diseño objetivo: no acredita funciones implementadas.

### Primera ampliación posterior al MVP (código fuente actual)

- El combate **no borra la pantalla entre turnos**: acciones del jugador,
  respuestas y resultado se acumulan en el desplazamiento de la terminal
  hasta terminar el encuentro. No se guarda el combate en SQLite.
- Corpo, Street y Nomad usan acentos distintos en bordes y selectores. El
  tier de cada objeto conserva su color y los datos siguen siendo legibles
  sin depender exclusivamente del color.
- Las acciones se eligen desde un único menú que indica daño, acierto,
  munición, curación y motivo de rechazo cuando corresponde; ya no hay una
  lista de efectos separada que duplique las opciones.
- **Recargar arma** está disponible en preparación y en la pausa de Watson.
  Solo acepta un arma propia equipada y reserva compatible; permite recarga
  parcial sin desperdiciar las balas del cargador. Fuera del combate no
  provoca respuesta enemiga ni avanza el evento.

Estas mejoras **no están en `artifacts/win-x64-m05.zip`**: ese paquete M05
se conserva intacto. Para probar la ampliación hay que compilar el código
actual; todavía no se publicó ni validó esta versión nueva en Windows.
Más implantes y quickhacks siguen siendo entregas posteriores.

### Tercera ampliación local: XP visible y primer cyberdeck (balance provisional)

Watson muestra **XP ganada por evento**, XP restante hasta el siguiente nivel
y, al subir, los puntos de atributo y habilidad concedidos. La progresión
distingue el **nivel 1–20 del personaje** de los **niveles 1/2/3 de cada
árbol**: las habilidades figuran como adquiridas, disponibles o bloqueadas
con el requisito pendiente. La ficha de preparación muestra nivel y
progreso de XP en filas separadas.

Los tres orígenes empiezan con **Militech Paraline** instalado en el sistema
operativo (capacidad **8/20**, RAM **6/6**) y **Cortocircuito**. En combate,
Cortocircuito inflige **12 de daño directo**, cuesta **3 RAM**, no utiliza
tirada de acierto físico y consume una acción; si falta RAM o deck, se rechaza
sin gastar turno. Paraline regenera **1 RAM al inicio de cada turno
propio**, después del daño persistente si el combatiente sobrevive.
Al avanzar a otro evento recupera hasta **3 RAM**; completar la aventura
la restaura por completo.

Desde «Atributos y habilidades → Cyberware y RAM» se consulta capacidad,
RAM e implantes instalados. Las operaciones se hacen en el ripperdoc.

En **Inventario y equipo → Quickhacks** podés configurar hasta cuatro
quickhacks distintos fuera de combate. Solo aparecen en combate los que
equipaste. **Sobrecalentamiento** se compra en la tienda de suministros por
**25 eurodólares** y no se equipa al comprarlo.

Con el balance provisional, Sobrecalentamiento cuesta **4 RAM** e inflige
**6 de daño inmediato** más **6 al inicio de cada uno de los dos próximos
turnos enemigos**. El primer pulso ocurre en el mismo turno del lanzamiento;
el segundo, después de tu siguiente acción válida. Los rechazos y las
selecciones gratuitas no consumen pulsos. El efecto no se acumula, un pulso
letal impide la respuesta enemiga y el daño pendiente se pierde al terminar
el combate. Los quickhacks no se revenden; solo persisten si los guardás
en el stash.

**Quemado sináptico** cuesta **35 eurodólares** en la tienda y **3 RAM** al
lanzarlo. Hace **8 de daño base + 2 por cada punto de RAM gastado antes en
ese combate**, hasta **24 de daño**. La regeneración no borra el gasto
previo, un intento rechazado no cuenta y cada combate empieza de cero.
El menú muestra el daño actualizado antes de lanzar el quickhack.

**Suicidio**, exclusivo del jugador, cuesta **75 eurodólares** en la tienda
y **8 RAM** al ejecutarlo. Con Paraline necesitás Ex-Disk para alcanzar
ese máximo. Si Self-ICE aún está listo, bloquea el lanzamiento sin devolver
RAM ni turno. Una vez gastado ese bloqueo, la reducción de daño de Self-ICE
no impide la ejecución instantánea. El enemigo cae sin responder.

En Dogtown, el **Centinela de Barghest** aparece junto al patrullero y al
netrunner como un encuentro separado. Su ficha lo identifica como **Bot**.
**Cortocircuito** inflige **18** de daño contra bots y **12** contra humanos
por las mismas **3 RAM**. **Suicidio** solo afecta a humanos: contra un bot
se rechaza sin gastar RAM ni turno.

**Fallo de arma** se compra por **30 eurodólares** y cuesta **3 RAM**.
No inflige daño: bloquea un disparo o una recarga del turno enemigo sin
alterar su munición. Se rechaza gratis si el objetivo no tiene un arma
equipada y no impide ataques cuerpo a cuerpo. Los pulsos pendientes se
resuelven antes del bloqueo. En Japantown, el tirador de Tyger Claws
empieza con dos disparos cargados y un cargador de reserva; al agotarlos
recurre al ataque básico. Watson no cambia.

**Self-ICE** cuesta **40 eurodólares** en el ripperdoc, ocupa **5** de
capacidad y comparte la corteza frontal con Ex-Disk. Bloquea el primer
quickhack recibido por combate sin devolver RAM al atacante. Después
reduce el daño de los quickhacks al **75 %**, redondeando hacia abajo.

**Fallo de cyberware** se compra por **45 eurodólares** y cuesta **4 RAM**.
Desactiva los implantes durante los **dos próximos turnos propios del
objetivo**, sin desinstalarlos. Self-ICE puede bloquear el primer intento.
Al reactivarse los implantes vuelve la RAM máxima, no la RAM gastada.
Dogtown incluye un netrunner de Barghest con Self-ICE que usa Fallo de
cyberware o Cortocircuito y recurre al ataque básico sin deck activo o RAM.
Precios y balance provisionales.

**Dynalar Sandevistan** cuesta **60 eurodólares** y ocupa **10** de
capacidad. Se instala en lugar del cyberdeck, por lo que no da acceso a
RAM ni quickhacks. Activarlo deja una acción en el mismo turno y elimina
la respuesta enemiga; el enemigo sigue procesando efectos y regeneración.
Si usás esa acción para un ataque físico válido, acierta incluso bajo
Reboot Optics. La activación no cambia los costes ni garantiza un crítico.
Sin cyberdeck instalado no se muestra RAM; si está temporalmente
desactivado, la interfaz indica ese estado.
La pantalla marca **SANDEVISTAN ACTIVO** y luego muestra el enfriamiento.
Si se activa en el turno 1, vuelve a estar listo en el 6. Fallo de
cyberware impide activarlo mientras dura la desactivación, pero no pausa
el enfriamiento. Los estados de combate no se guardan.

### Ripperdoc (balance provisional)

En preparación, elegí **Visitar al ripperdoc**. Podés comprar **Ex-Disk**
por **30 eurodólares** e instalarlo en la corteza frontal por **5**.
Ocupa **4** de capacidad y añade **2** a la RAM máxima si tenés un cyberdeck.
Hay una unidad en stock y se repone al regresar con éxito de una aventura.

Extraer un implante también cuesta **5** y lo devuelve a tu inventario de
cyberware. Las operaciones rechazadas no consumen dinero, stock ni equipo.
Instalar no rellena la RAM, y extraer limita la RAM actual al nuevo máximo.
Si extraés el deck, Cortocircuito deja de estar disponible; reinstalarlo
no restaura la RAM gastada.

El stock y los implantes que llevás siguen en memoria. No hay reventa
ni botín de implantes. Los implantes extraídos se pueden guardar en el
stash. El ZIP M05 no incluye estas ampliaciones.

### Segunda ampliación: progresión y habilidades (balance provisional)

Cada origen empieza en nivel 1 con **cinco atributos en 1**, cinco puntos
de atributo y uno de habilidad, más una habilidad gratuita: **Análisis**
(Corpo), **Sangre fría** (Street) o **Puños de acero** (Nomad). En preparación
y en la pausa de Watson, **Atributos y habilidades** permite gastar puntos,
ver requisitos y conservar los que no se usen. No hay redistribución.
Cada atributo llega a 10; el personaje llega a nivel 20.

Al resolver por primera vez un evento de Watson se conceden **30 XP por
combate** o **15 XP por hallazgo**. Análisis aumenta un 10 % esa ganancia
(truncando fracciones); el siguiente nivel requiere en total
`50 × (nivel − 1) × nivel / 2` XP. Cada subida concede un punto de cada tipo.
Los combates de prueba no otorgan XP. No se obtienen puntos directamente
del botín o de los menús. A nivel 20 se detiene la ganancia de XP en 9500.

Las **15 habilidades** se compran una vez; cada árbol avanza por requisitos
de atributo **1 → 3 → 6** y dependencias entre sus tres habilidades:

| Árbol | Habilidades y efectos provisionales, en orden |
|---|---|
| Inteligencia | Análisis: +10 % XP; Estrategia: +5 % acierto con arma; Medicina de campaña: +15 % curación. |
| Fuerza | Puños de acero: +20 % daño básico; Potencia: +10 % daño con arma; Recuperación: +20 % curación. |
| Agilidad | Pulso firme: +5 % acierto con arma; Reflejos: +5 % acierto básico; Impulso: +15 % daño básico. |
| Habilidad técnica | Mantenimiento: +10 % daño con arma; Calibración: +5 % acierto con arma; Primeros auxilios: +15 % curación. |
| Cool | Sangre fría: +5 % acierto con arma; Golpe certero: +5 % acierto básico; Oportunidad: +10 % XP. |

Los porcentajes de habilidades iguales se suman; el acierto se limita al
100 %, y daño/curación se redondean al entero más cercano. Son valores de
**prueba**, no balance aprobado. Personaje, XP, puntos y habilidades siguen
en memoria: morir o cerrar los pierde; **solo el stash persiste**. No cambió
el esquema SQLite ni se migró ninguna base existente.

## Entorno

- SDK .NET **10.0.115**, fijado en `global.json`.
- Destino **net10.0**.
- Spectre.Console **0.57.2**, declarado en `Project-C.csproj`.
- Microsoft.Data.Sqlite **10.0.15**, con proveedor nativo incluido en la publicación.
- Entorno de desarrollo consultado: Linux Mint 22.3 x64,
  runtime .NET 10.0.15 y MSBuild 18.0.37.

Desde la raíz del repositorio:

```bash
dotnet --info
dotnet restore Project-C.csproj
dotnet build Project-C.csproj --no-restore
dotnet run --project Project-C.csproj --no-build
```

Usá una terminal interactiva: flechas para elegir y Enter para confirmar.
Las pausas aceptan cualquier tecla.

## Comprobaciones de esta iteración

El ejecutable de comprobaciones no agrega dependencias de pruebas:

```bash
dotnet run --project tests/Project-C.Checks/Project-C.Checks.csproj
```

Comprueba kits, identidades, pertenencia, límite de armas, desequipado,
reequipado, descarte, turnos, recarga y curación en combate, conservación de
recursos y rechazos sin creación parcial; también verifica la aventura,
fusión de botín consumible, catálogo SQLite y transferencias persistentes.
No acredita el recorrido integral de aceptación en Windows.

Recorrido de interfaz, repetir iniciando un proceso nuevo por origen:

1. Pasar la bienvenida y seleccionar Corpo, Street o Nomad.
2. Verificar origen, salud completa y un arma equipada en preparación.
3. Entrar a **Inventario y equipo**. Debe contener:

   | Origen | Arma tier 1 | Cargador | Reserva | Curaciones |
   |---|---|---|---|---|
   | Corpo | Pistola | 12/12 | 12 | 2 |
   | Street | Subfusil | 30/30 | 30 | 2 |
   | Nomad | Escopeta de corredera | 6/6 | 6 | 2 |

4. Elegir **Desequipar arma**, seleccionar el arma inicial y confirmar.
   Debe mostrar 0/3; el arma permanece en inventario con el mismo cargador.
5. Intentar desequipar nuevamente: informa que no hay armas equipadas.
6. Elegir **Equipar arma** y luego **Cancelar**: conserva el estado.
7. Equipar el arma inicial: vuelve a 1/3. Abrir el selector de desequipado
   y cancelar también conserva el estado.
8. Volver a preparación y entrar nuevamente: mismas cantidades, salud y equipo.
9. Salir, abrir nuevamente y comprobar que vuelve a pedir origen.

**Descartar objeto:** elegir una instancia; para pilas, indicar una cantidad
entre 1 y el total (0 cancela). Confirmar o cancelar antes de aplicar el cambio.
Descartar un arma equipada elimina su referencia de equipo **y sus balas cargadas**,
sin añadirlas a la reserva; la interfaz muestra cuántas balas se perderán.
Se puede dejar el inventario vacío y volver a preparación. Un objeto descartado
no puede volver a entrar al inventario aunque quede una referencia en memoria.

El número de fila identifica cada opción en la pantalla actual. Dos armas
del mismo modelo tienen opciones distintas; la selección opera sobre la
instancia, no sobre el nombre. Equipar y desequipar conservan la munición cargada.

### Escenario interactivo con cuatro armas

El proyecto de comprobaciones puede abrir las pantallas reales con un personaje
de prueba y cuatro pistolas, alternando cargadores llenos y vacíos:

```bash
dotnet run --project tests/Project-C.Checks/Project-C.Checks.csproj -- --equipo-interactivo
```

Las armas aparecen en las filas #1, #4, #5 y #6; las filas #2 y #3 son
consumibles. Equipar hasta tres, intentar una cuarta, desequipar una y ocupar
la ranura liberada. Al salir, el escenario comprueba pertenencia, recursos y
ausencia de duplicaciones. Su carga adicional pertenece solo a esta prueba.
Para probar el descarte de una de esas armas sin alterar las demás:

```bash
dotnet run --project tests/Project-C.Checks/Project-C.Checks.csproj -- --descarte-interactivo
```

Este segundo escenario comprueba al salir que se retiró únicamente la pistola
cargada #5 y que permanecen el equipo y los recursos no descartados.

### Combate de prueba

Desde `dotnet run --project Project-C.csproj` podés elegir **Combate de prueba**
en **Guía y práctica**. La simulación copia el estado y el equipo actuales
sin modificar el personaje real. Ganar o perder devuelve a la guía, sin
premios ni pérdidas. En las aventuras reales, la derrota sí elimina al
personaje y sus pertenencias transportadas.

Se muestra la intención enemiga, una franja **aproximada** de daño si acierta
y su probabilidad de fallo; el daño exacto enemigo no se anticipa. Para el
ataque propio se muestra daño si acierta, probabilidades y coste reales de
la definición. Consultar el adelanto o seleccionar arma no avanza turnos
ni tira dados. Ataques, recarga, curación y espera consumen una acción efectiva
cuando se ejecutan. Un rechazo previo no causa respuesta enemiga.

Recargar toma únicamente la reserva compatible necesaria y disponible,
incluso entre varias pilas, sin perder las balas ya cargadas. Curarse requiere
salud faltante y un consumible; su magnitud se limita por la salud máxima.
**Stamina sigue fuera del MVP**, según el alcance confirmado.

El mismo componente puede probarse de manera independiente:

```bash
dotnet run --project tests/Project-C.Checks/Project-C.Checks.csproj -- --combate-interactivo
```

Elegir Corpo, Street o Nomad. El jugador actúa primero contra un enemigo
Maelstrom; se muestra su intención aproximada. Se puede atacar con arma,
usar ataque básico o esperar; elegir entre armas equipadas es gratuito.
Un fallo gasta munición y turno; falta de cargador o selección inválida
rechaza sin turno enemigo. Al vencer o morir termina este escenario.
Este combate de práctica no entrega botín ni cuenta como evento de aventura;
tampoco recupera salud automáticamente.

### Aventuras de cinco eventos

En preparación, elegí **Ir de aventura** y seleccioná una zona:

| Zona | Organización | Niveles enemigos |
|---|---|---|
| Watson | Maelstrom | 1–5 |
| Japantown | Tyger Claws | 6–12 |
| Dogtown | Barghest | 13–20 |

Las tres están abiertas desde el nivel 1. Los rangos no escalan con el
personaje. Cada aventura tiene cinco eventos y al menos tres combates.
Al crearla, se sortea una sola vez si incluye un comerciante (**1/3**);
si aparece, ocupa uno de los cinco lugares. La pausa muestra el tipo del
siguiente evento sin volver a sortearlo.

Tras un combate o hallazgo se puede recoger todo, parte o nada del botín; consultar y
cambiar equipo, curarse y continuar manualmente. Lo no recogido se pierde al
continuar. Navegar los menús no cura ni resuelve encuentros de nuevo.
La munición y las curaciones recogidas se suman a una pila ya transportada
del mismo objeto, sin cambiar su identidad; si no existe pila, se crea una.
Las armas recogidas conservan instancias independientes y cargadores vacíos.
Tras el quinto evento, continuar devuelve a preparación con botín, salud y
RAM completas. Morir elimina el personaje, su carga y su dinero; el stash
no se pierde.

**Retirarse de la aventura** está disponible en combate y en la pausa.
Cancelar no gasta un turno. Confirmar te devuelve a preparación sin
contraataque, sin recuperar salud ni RAM y sin reponer tiendas.
La retirada confirmada elimina todas tus armas, sus balas
cargadas y tu dinero. Conservás la progresión, el cyberware, los quickhacks,
las curaciones, la munición de reserva y el stash. El encuentro abandonado
no entrega XP ni botín.

Empezás sin eurodólares. Cada combate ganado da **15** y cada hallazgo **8**.
El comerciante vende hasta dos curaciones a **10** cada una y munición del
arma inicial a **1** por bala, hasta un cargador. Al generar la aventura
también se sortean un arma y un quickhack, con **una unidad de cada uno**.
El arma es Tier 2 en Watson (**25 eurodólares**), Tier 3 en Japantown
(**40**) y Tier 4 o 5 en Dogtown (**55** o **70**). Se entrega descargada
y sin equipar. El quickhack tiene **20 % de descuento** respecto de la
tienda común y queda sin asignar; no se puede comprar si ya lo tenés.
No cobra si faltan fondos, stock o espacio en la pila. No concede XP ni dinero al salir. Perfiles,
recompensas y precios son provisionales. Dinero y stock viven en memoria;
solo persiste el stash, sin cambios al esquema SQLite.

### Carga y sobrepeso

Preparación e inventario muestran la carga en kg. La capacidad es **20 kg
+ 2 kg por punto de Fuerza**, más los bonos de implantes activos como
Titanium Bones (**+4 kg** en Tier 1). Los pesos provisionales son 1,5 kg por
pistola, 3 kg por subfusil, 4,5 kg por escopeta, 10 g por bala, 250 g por
curación o accesorio, 50 g por componente y 750 g por implante suelto.
Los implantes instalados, los eurodólares y los quickhacks no pesan.
El inventario muestra el peso de cada arma con sus balas y accesorios
montados, el peso de cada pila y un desglose de los objetos sueltos.

Podés recoger botín por encima del límite y terminar o retirarte de la
aventura. Para iniciar otra, reducí la carga con el stash o descartando
objetos. Estar justo en el límite está permitido. En combate, el sobrepeso
añade **2 de stamina** al coste de los ataques físicos y reduce en **1** la
stamina recuperada por turno. No cambia el acierto, el daño, la RAM ni el
gasto de munición.

### Stamina

Cada combatiente tiene **12 de stamina** y recupera **3 al empezar su
turno**. Disparar cuesta **4** y atacar cuerpo a cuerpo cuesta **5**;
un ataque fallido también paga su coste. Sin stamina suficiente, se
rechaza la acción sin gastar turno ni munición. Recargar, curarse,
esperar y lanzar quickhacks no cuestan stamina. El enemigo espera si no
puede atacar, recargar ni usar un quickhack.

Activar Sandevistan no inicia otro turno del jugador; el enemigo sí
recupera recursos aunque pierda su acción. Avanzar al siguiente evento
recupera hasta **9 de stamina, 6 de escudo y 3 de RAM** (con Paraline),
una sola vez. El juego muestra cuánto recuperaste; no recupera salud.
Completar la aventura la
rellena; retirarse conserva la stamina actual. Los valores son
provisionales y el recurso no se guarda.

### Golpes críticos

Si un disparo o un ataque cuerpo a cuerpo acierta, tiene un **10 %** de
probabilidad de crítico (**5 % con sobrepeso**). El crítico multiplica
el daño por **1,5**, redondea hacia abajo y luego se aplica el escudo.
El menú separa acierto y probabilidad de crítico; el resumen marca el
impacto crítico. Los quickhacks no hacen críticos y el coste del ataque
no cambia.

### Escudo: Subdermal Armor

El ripperdoc vende **Subdermal Armor** por **50 eurodólares**. Instalado
ocupa **6 de capacidad** y aporta **12 de escudo máximo**. El escudo
evita hasta la mitad del daño con la carga llena, y proporcionalmente
menos al agotarse (redondeo hacia abajo). Pierde exactamente lo que
evitó; Suicidio pasa por alto esa protección.

El escudo recupera **2 al inicio del turno** y hasta **6 al avanzar de
evento**. Completar la aventura lo rellena; retirarse no. Instalarlo o
reactivarlo no lo rellena. Fallo de cyberware anula el escudo activo y
reduce su carga a cero. El implante extraído sí puede guardarse en el
stash, pero su carga no persiste. La interfaz usa celeste para el
escudo y verde para la RAM.

### Tienda de suministros

En preparación, elegí **Tienda de suministros**. Podés comprar o vender
munición de reserva y curaciones por cantidad; `0` cancela la operación.
Compras y ventas se separan en Munición, Curaciones y Quickhacks.
Las compras muestran stock y las unidades que podés pagar. En el
ripperdoc, las compras se agrupan por zona corporal y la instalación
indica si falta dinero o capacidad, o si ya hay un implante en esa zona.

| Suministro | Pagás por unidad | Recibís al vender | Stock inicial |
|---|---:|---:|---:|
| Munición (por tipo) | 2 | 1 | 24 |
| Curación | 12 | 5 | 4 |

Los precios son provisionales. Vender no repone el stock; completar una
aventura sí. La tienda también compra quickhacks sin asignar, pero no
armas, munición cargada ni implantes. El ripperdoc compra implantes
sueltos sin quickhacks asociados; el comerciante de aventura mantiene su
propio stock y sus precios.

### Taller

En preparación, elegí **Taller**. Desarmar un arma devuelve **4 a 7 componentes**
según el tier; una curación o un accesorio suelto devuelve **1**. Si el arma
estaba equipada, se libera su ranura.
Sus balas cargadas se pierden, pero la reserva queda intacta. El menú pide
confirmación antes de destruir un objeto.

Cada lote fabrica **1 curación por 2 componentes** o **6 unidades de
munición por 1 componente**. Las recetas están disponibles desde el
inicio. Cancelar o no poder añadir el resultado no consume componentes.
Los componentes transportados se conservan al retirarse de una aventura
y se pierden al morir o cerrar. En preparación podés depositarlos y
retirarlos por cantidad desde el **stash**, que muestra los saldos
transportado y guardado. Solo los depositados sobreviven a otra sesión.

**Mejorar arma** sube cualquier modelo un tier por
vez, hasta Tier 5. Cuesta **4, 6, 8 y 10 componentes** respectivamente y
añade **2 de daño base** por tier, sin cambiar precisión, capacidad,
munición cargada ni ranura de equipo. No cuesta dinero.

En **Fabricar** aparecen los nueve modelos en Tier 1. Se fabrican de a
uno, sin equipar ni montar accesorios. Las armas de fuego salen con
el cargador vacío; espada y bate no usan munición. La confirmación muestra
el peso y la carga resultante; el sobrepeso no impide fabricar, pero sí
iniciar otra aventura.

Los **quickhacks** se fabrican de a uno, sin gastar eurodólares
ni stock de la tienda: Cortocircuito cuesta **4** componentes;
Sobrecalentamiento, **6**; Fallo de arma y Quemado sináptico, **8**;
Fallo de cyberware, **10**; y Suicidio, **16**. No se fabrican copias
de quickhacks que ya poseés, aunque estén asignados a un deck extraído.
El resultado queda sin asignar y se puede depositar en el stash.

**Mejorar implante** sube Militech Paraline y Ex-Disk hasta Tier 5, estén
instalados o sueltos. Usa los mismos costes que las armas. Cada tier de
Paraline añade **1 RAM máxima** y requiere **2 de capacidad** adicional.
Cada tier de Ex-Disk añade **1 de bonificación de RAM** y requiere **1 de
capacidad** adicional. Si el implante instalado superaría el límite total,
la mejora se rechaza sin gastar componentes. La RAM actual y los quickhacks
equipados se conservan. Self-ICE y Sandevistan aún no pueden mejorarse.

El stash conserva el tier. Al abrir una base v1 a v11 válida, el juego
crea una copia SQLite `game.db.vN-….bak` junto al original (N es la versión
anterior) y actualiza el catálogo a v12 en una sola transacción. Conserva
los IDs y depósitos anteriores. El ZIP M05 no incluye esta actualización.

**Fabricar cargador ampliado** cuesta **3 componentes**. Se monta o quita
en **Inventario y equipo → Cargadores ampliados**, también en las pausas
de aventura. Solo es compatible con pistolas: añade **4 plazas** (12 a 16
con el balance inicial) sin rellenar el arma. Al quitarlo, las balas
sobrantes vuelven a la reserva; si no caben, el desmontaje se rechaza
sin cambios. Un cargador montado se pierde con el arma si te retirás de
una aventura; los sueltos se conservan.

La **mira réflex** también se fabrica por **3 componentes**. Se monta y
se quita en **Inventario y equipo → Miras réflex**, incluso entre eventos.
Solo admite pistolas y se puede combinar con el cargador ampliado. Suma
**5 puntos porcentuales de acierto** después de los bonos de habilidades,
con un máximo del **100 %**, sin cambiar daño ni capacidad. El menú de
combate muestra el acierto final.

Al depositar un arma, el stash guarda el arma, las balas cargadas y los
accesorios montados por separado en una sola transacción. Antes de
confirmar se muestra qué se separará. Si falla una parte, no se deposita
nada. Las armas retiradas vuelven vacías y sin accesorios. También podés
depositar y retirar cargadores y miras sueltos por cantidad, sin montarlos.

Para guardar un implante instalado, primero extraelo en el **ripperdoc**.
Al depositar un cyberdeck extraído, el stash guarda el deck y los
quickhacks que tiene asignados en una sola transacción. La confirmación
lista los que se separarán. El implante vuelve suelto, con su tier, sin
RAM guardada y, si es un deck, con sus ranuras vacías. Un quickhack sin
asignar también puede depositarse por separado. No podés retirar un
quickhack que ya poseas; esa entrada permanece en el stash.

Prueba interactiva de Watson con secuencia y aciertos controlados, solo en
el proyecto de comprobaciones:

```bash
dotnet run --project tests/Project-C.Checks/Project-C.Checks.csproj -- --aventura-prueba
dotnet run --project tests/Project-C.Checks/Project-C.Checks.csproj -- --aventura-muerte-prueba
dotnet run --project tests/Project-C.Checks/Project-C.Checks.csproj -- --comerciante-prueba
python3 tests/check_expedition_ui.py
```

Para probar selección entre dos armas, la segunda pistola se equipa **vacía**
solo en este escenario:

```bash
dotnet run --project tests/Project-C.Checks/Project-C.Checks.csproj -- --combate-multiarma
```

Para repetir recorridos automáticos en Linux con tiradas de acierto
controladas (solo en los escenarios de prueba):

```bash
python3 tests/check_combat_ui.py
python3 tests/check_session_ui.py
```

La salud de prueba (enemigo 40) y los ataques son balance **provisional**:
pistola 12/85 %, subfusil 10/85 %, escopeta 18/75 %, básico jugador
5/85 % y básico enemigo 6/75 %. Cada disparo gasta una bala. El daño,
precisión y coste de armas se cargan de `WEAPON_DEFINITION` en SQLite. Las
definiciones de `DevelopmentContent.cs` permanecen como datos de prueba para
los escenarios aislados; no se usan en la ejecución normal.
El ataque básico y el perfil enemigo siguen en la configuración del programa.
Se calibrarán comparando acciones, salud restante y munición por origen.
La curación provisional es **25 de salud por unidad** (limitada al máximo),
configurada en `ITEM_EFFECT` del catálogo SQLite. El estimado enemigo de 1–10 agrupa el
daño configurado: **no es un sorteo adicional ni revela el número exacto**.

### Recorridos automatizados de terminal en Linux

Requieren Python 3 y `pexpect` (utilizado: 4.9.0). Después de compilar el juego
y ejecutar las comprobaciones de dominio:

```bash
python3 tests/check_equipment_ui.py
```

Cubren los tres orígenes, cancelaciones, mensajes sin candidatas, reingreso,
selección de armas iguales, límite de equipo, descarte parcial/total, pérdida
de balas cargadas y navegación con el inventario vacío.

La salud 100 sigue provisional en `contenido/DevelopmentContent.cs`; las
capacidades 12/30/6 están en la base SQLite, tampoco son valores aprobados por el SDD.
El kit usa definiciones compartidas y cargadores por instancia.
El ataque básico y el enemigo de práctica siguen en código.

### Catálogo SQLite y stash compartido (M04)

Al iniciar, el juego crea `game.db` si no existe, con las cinco tablas del
catálogo reducido y sus siete objetos iniciales. La ubicación predeterminada
es la carpeta de datos local del usuario: en Windows, normalmente
`%LOCALAPPDATA%\MySharpVenture\game.db`; en Linux,
`~/.local/share/MySharpVenture/game.db`. La carpeta puede cambiarse con
`MYSHARPVENTURE_DATA_DIR` **antes de iniciar**:

```powershell
$env:MYSHARPVENTURE_DATA_DIR = 'D:\MisJuegos\MySharpVenture'
.\Project-C.exe
```

Desde preparación, elegir **Stash**. Se pueden depositar o retirar cantidades;
`0` cancela y se solicita confirmación antes de escribir. Los consumibles
iguales se suman a la pila existente. Cada arma guardada se desequipa y
descarga: sus balas cargadas pasan a la entrada de munición del stash, sin
modificar la reserva transportada. Al retirar armas, cada una tiene una
identidad nueva y cargador vacío. **Descartar** un arma, a diferencia de
**depositarla**, sigue perdiendo sus balas cargadas.

Cada confirmación modifica el stash en una transacción. Salir o morir elimina
el personaje y el inventario transportado, **no el stash**. No se copia la base
al publicar la siguiente versión: permanece en los datos del usuario.
Una base ya existente con esquema, relaciones o cantidades inválidos no se
reemplaza ni vacía; el programa muestra un error con la ruta. Antes de corregir
una base manualmente, guardá una copia y no borres `game.db` si contiene botín.

La comprobación interactiva usa un directorio temporal aislado y abre el
juego dos veces:

```bash
python3 tests/check_stash_ui.py
```

## M05: paquete y aceptación Windows 11

Publicación autocontenida para **Windows x64**:
`artifacts/win-x64-m05.zip` (carpeta extraída `win-x64-m05/`, 200 archivos).
Incluye el runtime, `e_sqlite3.dll` y la
[guía de aceptación M05](docs/M05-aceptacion-windows.md), también dentro del
ZIP. Copiar el ZIP **completo** y este `README.md` para disponer del hash
externo; seguir la guía para verificar hashes y usar una base de prueba
aislada. No sustituir `game.db` de `%LOCALAPPDATA%\MySharpVenture`.

SHA-256 comprobados tras la publicación en Linux:

| Archivo | SHA-256 |
|---|---|
| `artifacts/win-x64-m05.zip` | `fa8a9aee3eed6e32380bbd14df430fd8d4059cb5bb486ac2b43af3053d0dd434` |
| `Project-C.exe` | `d8c210d372d26af6a51c8eb0bdb8e83cadaad997390a70e2f1cc5dd2170a0b4f` |
| `Project-C.dll` | `5ffff9f31c5add8058a8d6437451fecad5ee4a564ffefcd38017a64c77b3db69` |
| `e_sqlite3.dll` | `b7385d722c83fb52142a00477a726723745916d22a555711ee89834c1111fb2e` |

La guía indica cómo rellenar el acta PM01–PM08. Empaquetar en Linux y
verificar que los binarios son PE x64 **no acredita** PM08.

## Publicación M04 anterior (referencia)

Para un equipo Windows **x64** (comprobar antes su arquitectura):

```bash
dotnet publish Project-C.csproj -c Release -r win-x64 --self-contained true -o artifacts/win-x64-m04
file artifacts/win-x64-m04/Project-C.exe
sha256sum artifacts/win-x64-m04/Project-C.exe
sha256sum artifacts/win-x64-m04/Project-C.dll
```

Copiar toda la carpeta `artifacts/win-x64-m04` al equipo Windows. Incluye
`e_sqlite3.dll` y demás dependencias del catálogo. **No copiar ni
renombrar el ejecutable `bin/Debug/net10.0/Project-C`**: ese archivo es un
ELF de Linux, que Windows rechaza como aplicación inválida. La publicación
requiere poder restaurar los paquetes de plataforma; el ejecutable resultante
incluye el runtime y debe funcionar sin instalar el SDK ni tener conexión.
Para Windows ARM64, usar `win-arm64` y una carpeta de salida correspondiente.

En Windows Terminal/PowerShell, ubicado en la carpeta copiada:

```powershell
Get-CimInstance Win32_OperatingSystem | Select-Object Caption,Version,OSArchitecture
$env:PROCESSOR_ARCHITECTURE
Get-Item .\Project-C.exe | Select-Object Name,Length,FullName
Get-FileHash .\Project-C.exe -Algorithm SHA256
Get-FileHash .\Project-C.dll -Algorithm SHA256
.\Project-C.exe
$LASTEXITCODE
```

Con la red desconectada, repetir el recorrido anterior y registrar:
versión/arquitectura de Windows 11, terminal, ruta completa del ejecutable,
hash, resultado y mensaje de error si lo hubiera. Publicar desde Linux no
verifica Windows.

**Diagnóstico del fallo anterior:** en Windows, `Project-C.exe` tenía el
SHA-256 `E3B0C442…B855`, correspondiente a un archivo vacío; el DLL sí
coincidía con el publicado. El paquete `win-x64` generado en Linux contenía
un ejecutable PE x64 no vacío. Tras corregir la copia, el usuario confirmó
que el juego **arranca en Windows 11 (AMD64)**. Esto acredita el arranque,
pero no equivale a ejecutar allí el recorrido integral de aceptación PM08.
Si se vuelve a publicar, comparar los hashes de **ambos** archivos: el
anfitrión `.exe` puede conservar su hash aunque cambie `Project-C.dll`.
En la publicación M04 generada en Linux, `Project-C.exe` es PE x64 con SHA-256
`d8c210d372d26af6a51c8eb0bdb8e83cadaad997390a70e2f1cc5dd2170a0b4f`
y `Project-C.dll` tiene SHA-256
`e96e537c20586e0137a5ff326e0013bd512c04d2f97ecc0f3925016a3e1b8fac`.
Son hashes de este paquete, **no** evidencia de ejecución M04 en Windows.

## Evidencia de esta iteración — 18/11/2026

- `dotnet run --project tests/Project-C.Checks/Project-C.Checks.csproj`:
  **47/47 comprobaciones correctas** en Linux, incluyendo validación de
  dependencias nulas y fusión por
  `CatalogItemId`, identidad de la pila, recogida parcial, pila agotada,
  desbordamiento y uso del botín apilado al recargar o curarse, catálogo SQLite,
  transferencias atómicas, otra sesión y rechazo de base inválida.
- `dotnet build tests/Project-C.Checks/Project-C.Checks.csproj --no-restore`:
  compilación correcta, cero advertencias y cero errores.
- `python3 tests/check_equipment_ui.py`:
  **ocho recorridos correctos** por pseudoterminal: equipamiento y descarte
  con Corpo, Street y Nomad; dos escenarios con cuatro armas iguales.
  Incluyen cancelaciones, rechazo de cantidades inválidas, descarte de pilas
  y equipo, conservación de recursos restantes y salida con código 0.
- `python3 tests/check_combat_ui.py`: **cinco recorridos correctos**:
  victoria con cada origen, selección/cancelación, rechazo por arma vacía
  sin respuesta enemiga, ataque básico, espera, recarga, curación y derrota controlada.
- `python3 tests/check_session_ui.py`: **recorrido de pantallas correcto**:
  creación, dos combates consecutivos conservando salud y carga, curaciones
  en combate y preparación, derrota, recreación y kit nuevo. Usó tiradas
  controladas únicamente en el escenario de comprobación, no en el juego.
- `python3 tests/check_expedition_ui.py`: **cinco recorridos correctos**,
   cinco eventos con botín parcial y reserva 12 + 2 = 14 en una sola pila,
   pausas/curación explícita, regreso con salud completa, muerte con
   recreación, navegación circular y terminales de 80 y 40 columnas.
   Tiradas controladas solo en el proyecto de comprobaciones.
- `python3 tests/check_stash_ui.py`: **dos procesos correctos** con la misma
  base temporal: depósito de arma y balas, cantidad parcial, cierre, creación
  nueva, retirada con cargador vacío y rechazo de otra base inválida sin borrarla.
- Se corrigió la comprobación del escenario de equipo: ahora captura el
  estado de los cargadores **antes** de la interacción.
- El recorrido manual reproducible figura arriba.
- Windows 11: **arranque confirmado por el usuario** tras corregir la copia
  del ejecutable vacío; PM08 (recorrido integral) aún pendiente.

Equipo, combate, Watson, catálogo SQLite y stash se comprobaron en Linux.
La aceptación global PM01–PM08 sigue pendiente, especialmente repetir el
recorrido completo en Windows 11 sin conexión con el paquete **M05**.
La stamina y los sistemas de progresión adicionales quedan para después
del MVP; la presentación visual actual puede ajustarse más adelante.

## Evidencia de preparación M05 — 18/11/2026

- `dotnet build tests/Project-C.Checks/Project-C.Checks.csproj --no-restore`:
  cero advertencias y cero errores.
- `dotnet run --project tests/Project-C.Checks/Project-C.Checks.csproj --no-build`:
  **47/47** comprobaciones.
- `python3 tests/check_equipment_ui.py`, `tests/check_combat_ui.py`,
  `tests/check_session_ui.py`, `tests/check_expedition_ui.py` y
  `tests/check_stash_ui.py`: todos los recorridos correctos. La prueba de
  descarte ahora reconoce el texto cuando el panel lo parte en dos líneas.
- `dotnet publish Project-C.csproj -c Release -r win-x64 --self-contained true
  -o artifacts/win-x64-m05`: publicación nueva; EXE y SQLite nativo PE x64.
  ZIP verificado mediante lectura de sus 200 entradas y prueba de integridad.
- En esta etapa aún no constaba una ejecución en Windows 11. No se cambió ni
  se eliminó ninguna base existente; la guía propone una base aislada.

## Seguimiento Windows 11 y M06 — 18/11/2026

- **Confirmado por el usuario:** funcionamiento del juego en Windows 11,
  recorrido de Watson, persistencia del stash y, posteriormente, muerte con
  conservación del stash. La muerte ya no figura como prueba manual pendiente.
- **Sin constancia suficiente para acreditar PM08:** el usuario confirmó
  que probó con la red conectada; tampoco se aportaron hashes/rutas ni un
  acta PM01–PM08 completada. La guía de
  `docs/M05-aceptacion-windows.md` permanece como plantilla, no como acta
  ejecutada. No se modificó el ZIP M05 ni su hash.
- **M06 (reserva de estabilización):** por ahora no hay defectos comunicados
  que corregir. Antes del cierre, completar el acta con la evidencia real y
  resolver solo las incidencias que aparezcan; no tocar el stash habitual.

## Verificación M06 — 18/11/2026

- Compilación de `tests/Project-C.Checks/Project-C.Checks.csproj --no-restore`:
  cero advertencias y cero errores; **47/47** comprobaciones de dominio.
- Pasaron los cinco scripts de interfaz: `check_equipment_ui.py`,
  `check_combat_ui.py`, `check_session_ui.py`, `check_expedition_ui.py` y
  `check_stash_ui.py`; usan escenarios controlados o bases temporales.
- Se volvió a verificar el ZIP M05: **200 archivos íntegros**, guía incluida,
  sin ninguna base SQLite empaquetada. El SHA-256 del ZIP sigue siendo
  `fa8a9aee3eed6e32380bbd14df430fd8d4059cb5bb486ac2b43af3053d0dd434`.
- No se detectaron defectos reproducibles en esta regresión. No hubo cambios
  de código, bases de datos ni paquete Windows. El usuario aclaró que las
  pruebas manuales en Windows 11 **se hicieron con conexión**: PM08 permanece
  **pendiente** del recorrido sin red y de su acta, sin presuponer su resultado.

## Primera ampliación: evidencia local — 18/11/2026

- Compilación del juego y las comprobaciones: cero errores y advertencias;
  **49/49** comprobaciones de dominio, incluidas recarga fuera de combate y
  acentos distintos para los orígenes.
- Pasaron `check_equipment_ui.py`, `check_combat_ui.py`,
  `check_session_ui.py`, `check_expedition_ui.py` y `check_stash_ui.py`.
  La prueba de combate comprueba que no se limpia la pantalla entre turnos
  y que se puede terminar una pelea en una terminal de 40 columnas;
  las de sesión y Watson cubren recarga fuera de combate, rechazo de
  cargador lleno y salud/evento sin avance adicional.
- No cambió el esquema SQLite ni se modificaron bases existentes.
  El paquete M05 y sus hashes corresponden **solo** a la versión anterior.

## Segunda ampliación: evidencia local — 18/11/2026

- `dotnet run --project tests/Project-C.Checks/Project-C.Checks.csproj`:
  **52/52** comprobaciones. Se añadieron pruebas de catálogo y dependencias,
  kits, XP con nivel máximo 20, puntos, requisitos, efectos en combate y
  curación, y premio único por evento; las anteriores siguen pasando.
- Pasaron los cinco scripts de interfaz. Se verificó la compra de habilidad
  y asignación de atributo en preparación, la consulta en pausa sin avanzar
  el evento y navegación de progresión en 40 columnas.
- La publicación Windows M05 no incluye estas ampliaciones. La aceptación
  PM08 de M05 aún requiere la ejecución completa sin red en Windows 11.

## Tercera ampliación: evidencia local — 18/11/2026

- `dotnet run --project tests/Project-C.Checks/Project-C.Checks.csproj`:
  **54/54** comprobaciones. Se verificaron aviso único de XP/subida,
  instalación por zona/capacidad, costes y rechazos de Cortocircuito,
  regeneración de RAM, victoria sin respuesta y muerte sin herencia de RAM.
- Pasaron los cinco recorridos de interfaz, incluido Cortocircuito y la
  extracción/reinstalación del deck en preparación.
- Sin publicación Windows nueva ni cambio de esquema SQLite. PM08 sin red
  sigue pendiente.
