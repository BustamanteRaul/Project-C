# M05 · Aceptación manual en Windows 11 (sin conexión)

**Estado:** guía de ejecución; PM08 sigue **pendiente** hasta registrar una
ejecución real en Windows 11. La publicación en Linux no la sustituye.
El recorrido sigue el [SDD §13.7](../MySharpVenture-SDD-v1.0/MySharpVenture-SDD-v1.0.md)
y sus criterios PM01–PM08. No requiere SDK .NET ni acceso a NuGet.

## Preparación segura

1. En Windows 11 x64, copiar `win-x64-m05.zip` **completo** a una carpeta local.
   Comparar su SHA-256 con el indicado en el `README.md` entregado junto al
   paquete, antes de extraerlo. No mezclar archivos de M04 ni ejecutar desde
   el ZIP. Cortar la conexión de red (Wi-Fi y Ethernet) y verificar que sigue
   desconectada durante **todo** el recorrido.
2. En PowerShell, adaptar `$zip` a la ruta real. Estos comandos crean carpetas
   **nuevas** para binarios y datos, sin borrar ninguna base existente:

   ```powershell
   $zip = 'C:\Ruta\win-x64-m05.zip'
   $expectedZip = 'REEMPLAZAR_CON_SHA256_DEL_README'
   if ((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash -ne $expectedZip) { throw 'ZIP distinto del publicado: detenerse' }
   $stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fffffff'
   $extract = Join-Path $env:TEMP "MySharpVenture-M05-$stamp"
   $data = Join-Path $env:LOCALAPPDATA "MySharpVenture-M05-Aceptacion-$stamp"
   if ((Test-Path -LiteralPath $extract) -or (Test-Path -LiteralPath $data)) { throw 'La ruta de prueba ya existe: detenerse' }
   New-Item -ItemType Directory -Path $extract, $data | Out-Null
   Expand-Archive -LiteralPath $zip -DestinationPath $extract
   $package = Join-Path $extract 'win-x64-m05'
   $exe = Join-Path $package 'Project-C.exe'
   $dll = Join-Path $package 'Project-C.dll'
   $sqlite = Join-Path $package 'e_sqlite3.dll'
   if ((Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash -ne 'd8c210d372d26af6a51c8eb0bdb8e83cadaad997390a70e2f1cc5dd2170a0b4f') { throw 'EXE distinto: detenerse' }
   if ((Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash -ne '5ffff9f31c5add8058a8d6437451fecad5ee4a564ffefcd38017a64c77b3db69') { throw 'DLL distinto: detenerse' }
   if ((Get-FileHash -LiteralPath $sqlite -Algorithm SHA256).Hash -ne 'b7385d722c83fb52142a00477a726723745916d22a555711ee89834c1111fb2e') { throw 'SQLite nativo distinto: detenerse' }
   $env:MYSHARPVENTURE_DATA_DIR = $data
   Write-Host "Binarios: $package"
   Write-Host "Base de PRUEBA: $(Join-Path $data 'game.db')"
   & $exe
   $LASTEXITCODE
   ```

   Si falta algún archivo o falla un hash, **no ejecutar**. Mantener esta
   ventana de PowerShell para las reaperturas; si se cierra, volver a establecer
   `$env:MYSHARPVENTURE_DATA_DIR = $data` antes de iniciar el EXE.
   La base habitual está en `%LOCALAPPDATA%\MySharpVenture\game.db`, **no**
   en `$data`. No renombrarla, copiarla, eliminarla ni apuntar la prueba a ella.
   No hay que cambiar ninguna base previa para este recorrido.

## Recorrido y resultados esperados

Usar las flechas y Enter; las pausas aceptan una tecla. El contenido de Watson
es aleatorio: anotar el tipo y resultado de cada evento; si se muere antes de
terminar cinco, repetir con un personaje nuevo, **sin borrar la base de prueba**.
No fingir un resultado que no se haya observado.

1. **PM01 / PM07:** pasar la bienvenida, elegir Corpo. Comprobar en preparación
   salud 100/100 y en inventario Pistola tier 1 equipada 12/12, reserva 12,
   curaciones 2. Más adelante repetir con Street (Subfusil 30/30 y reserva
   30) y Nomad (Escopeta de corredera 6/6 y reserva 6), siempre con 2
   curaciones. Al iniciar debe crearse `$data\game.db`; sin esa base o con un
   error de catálogo, registrar fallo, no sustituirla.
2. **PM02:** desequipar y volver a equipar el arma; comprobar que el cargador
   no cambia y que no aparece una segunda copia. Cancelar una selección y
   comprobar que tampoco cambia. Elegir un arma durante un combate no debe
   consumir turno. Si se prueba **Descartar**, hacerlo con una unidad que no
   necesites: descartar un arma pierde sus balas cargadas; no es depositar.
3. **PM03:** en combate de prueba o en Watson, comprobar la intención enemiga.
   Atacar o esperar debe permitir una respuesta enemiga mientras continúe el
   combate; recargar reduce la reserva y usa una acción; curarse consume una
   unidad y usa una acción. Intentar atacar con cargador vacío o recargar sin
   reserva debe rechazar la acción sin respuesta enemiga. Registrar daños,
   munición y salud observados, no exigir un número fijo de aciertos.
4. **PM04:** desde preparación iniciar Watson, resolver y anotar sus **cinco**
   eventos. Si hay botín, seleccionar objetos y cantidades (se admite recoger
   parcialmente); comprobar que consumibles iguales se apilan. Entre eventos,
   abrir equipo y regresar: la salud no debe curarse solo por visitar menús.
   Avanzar manualmente; tras el quinto evento, regresar a preparación con
   botín conservado y salud recuperada por completo.
5. **PM06:** desde preparación abrir Stash. Depositar el arma equipada:
   cancelar una vez y comprobar que no cambió nada; confirmar después.
   Debe desaparecer del equipo, aparecer en el stash como arma y sus balas
   cargadas como **munición separada**; la reserva transportada no aumenta.
   Depositar una cantidad parcial de curaciones o munición y confirmar.
   Retirar el arma: vuelve **sin equipar y con cargador vacío**; retirar parte
   de la munición debe aumentar la pila transportada sin duplicarla.
   Depositar al menos una curación y **dejarla en el stash** para PM05.
6. **PM05:** provocar una derrota en combate de prueba o Watson, sin retirar
   esa curación. Comprobar que se pierde el personaje y la carga. Crear Street
   o Nomad, confirmar kit nuevo y consultar el stash: debe quedar lo depositado
   y no aparecer como persistido el inventario que llevaba el personaje muerto.
   Retirar la curación, comprobar la cantidad y, si se quiere verificar la
   siguiente reapertura, volver a depositar una unidad.
7. **PM06 / PM08:** salir normalmente y ejecutar `& $exe` en **la misma**
   PowerShell, con `$env:MYSHARPVENTURE_DATA_DIR` aún apuntando a `$data`.
   Debe pedir un **personaje nuevo**, pero el stash debe mantener las entradas
   dejadas allí. Comprobar textos en español y funcionamiento sin red.

## Acta de aceptación (completar en Windows; no preaprobar)

Windows 11 versión/arquitectura: ______ · Terminal: ______ · Fecha: ______

ZIP SHA-256: ______ · EXE/DLL/SQLite verificados: ______ · Red desconectada: ______

Ruta `$package`: ______ · Ruta `$data\game.db`: ______

| Criterio | Resultado (aprobado / falló / no ejecutado) | Evidencia o incidencia |
|---|---|---|
| PM01: tres orígenes y kits | No ejecutado | |
| PM02: inventario, equipo, descarte y selección gratuita | No ejecutado | |
| PM03: intención, turnos, rechazo, recarga y curación | No ejecutado | |
| PM04: Watson, cinco eventos, botín, avance y regreso | No ejecutado | |
| PM05: muerte, nuevo personaje y stash conservado | No ejecutado | |
| PM06: transferencias y persistencia al reiniciar | No ejecutado | |
| PM07: catálogo SQLite y base válida | No ejecutado | |
| PM08: recorrido completo sin red en Windows 11 | No ejecutado | |

Para PM07, el rechazo de bases inválidas ya tiene prueba automatizada aislada
en Linux; **no corromper** `$data\game.db` ni la base habitual para demostrarlo
en Windows. Si aparece un error, guardar mensaje, pasos y ruta para diagnóstico.
Ante un fallo, detener el recorrido y conservar el ZIP y la base de prueba
para investigar. No limpiar ni sobrescribir bases como parte de la aceptación.
