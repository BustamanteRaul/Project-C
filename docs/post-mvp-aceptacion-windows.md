# Prueba sin conexión de la versión posterior al MVP

Usar Windows 11 x64. Esta prueba no sustituye el acta PM08 del paquete histórico M05; para ese paquete seguir `M05-aceptacion-windows.md`.

1. Copiar el ZIP completo y comprobarlo en PowerShell con `Get-FileHash .\win-x64-post-mvp.zip -Algorithm SHA256`. Comparar el valor con el archivo `.sha256` publicado junto al ZIP. Descomprimirlo y desconectar la red.
2. Usar un directorio de prueba, nunca la base habitual:

   ```powershell
   $data = Join-Path $env:LOCALAPPDATA ("MySharpVenture-Prueba-" + (Get-Date -Format yyyyMMdd-HHmmss))
   New-Item -ItemType Directory -Path $data
   $env:MYSHARPVENTURE_DATA_DIR = $data
   ```

3. Con el juego anterior cerrado, copiar al directorio de prueba una **copia de respaldo** de una base anterior con depósitos conocidos, como `game.db`. Anotar sus artículos y cantidades. Si no hay copia anterior, probar el arranque limpio y dejar la migración como no comprobada.
4. Desde la carpeta extraída, ejecutar `.\Project-C.exe`. Comprobar que informa de la copia de seguridad, que el stash conserva los artículos y cantidades anotados y que no sustituye la base habitual. Depositar y retirar un objeto; cerrar, volver a abrir sin red y comprobar que se conserva el stash pero no el personaje.
5. Jugar una expedición. Comprobar combate, curación, regreso al menú y recuperación de recursos. En preparación, probar una receta de arma nueva, equiparla y usarla; si es espada o bate, no debe aparecer «Recargar» con ese arma seleccionada. Probar también cancelar una venta antes de confirmarla.
6. Registrar fecha, versión de Windows, hash, estado de la red, resultados y fallos. No marcar como ejecutada una comprobación que no se haya realizado.
