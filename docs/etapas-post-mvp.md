# Etapas pendientes del SDD v1.0

Implementar en orden. Para cerrar una etapa, comprobar sus criterios, ejecutar las pruebas de dominio y de interfaz afectadas y registrar el resultado aquí. Si alguna comprobación no se ejecutó, indicar cuál y por qué. El balance nuevo es provisional; el personaje sigue en memoria y solo el stash persiste.

| Etapa | Trabajo | Criterio de cierre | Estado |
|---|---|---|---|
| 1. Gorilla Arms | Vender e instalar el implante desde el ripperdoc. Sustituir el golpe básico por un ataque físico con stamina. Integrar el implante extraído con el stash. | Con el implante activo y suficiente stamina, menú y resolución usan su ataque sin ocupar una ranura. Si no se puede usar, vuelve el golpe básico; si tampoco alcanza, el rechazo no consume turno y se puede esperar. Un fallo paga la stamina. | Verificada en Linux |
| 2. Otros brazos | Añadir Mantis Blades, Monowire y Projectile Launch System. Definir costes, daño y restricciones por implante. | Verificar coste y efecto de cada ataque, exclusividad de la zona y vuelta al ataque básico tras desactivar o extraer. | Verificada en Linux |
| 3. Quickhacks enemigos | Añadir Fallo de arma y Reboot Optics al netrunner, con prioridad e intención visibles. | Fallo de arma bloquea disparo y recarga del arma afectada; Reboot Optics baja el acierto físico a menos del 10 % sin afectar quickhacks. Self-ICE bloquea el primer quickhack recibido. | Verificada en Linux |
| 4. Armas de fuego | Añadir revólver, escopeta de doble cañón y rifles de asalto y precisión. Incluir munición, compatibilidades, recetas Tier 1, tiers y etiquetas Power, Tech o Smart. | Se pueden fabricar, equipar, disparar, recargar, mejorar y guardar. El comerciante respeta los tiers por zona. Las etiquetas no añaden mecánicas. | Verificada en Linux |
| 5. Armas cuerpo a cuerpo | Añadir espada y bate con stamina, críticos y tiers, sin munición ni accesorios. | Se pueden fabricar, equipar, usar, mejorar y guardar. No ofrecen recarga ni accesorios, y no sustituyen el golpe básico. | Verificada en Linux |
| 6. Cyberware restante | Añadir Tetratronic Rippler, completar tiers de Self-ICE, Sandevistan y Subdermal Armor, y ampliar las demás zonas por tandas. Añadir implantes al botín. | Cada implante tiene un efecto probado y admite instalación, extracción y stash. Las mejoras que exceden la capacidad no gastan componentes. | Catálogo base verificado en Linux |
| 7. Carga y sobrepeso | Añadir un implante de carga como Titanium Bones. Reducir la recuperación de stamina, sin penalizar RAM. | Desactivar el implante puede causar sobrepeso temporal; la interfaz lo explica y no cambian RAM, acierto ni gasto de munición. | Verificada en Linux |
| 8. Reventa y balance | Añadir venta de implantes sueltos al ripperdoc y de quickhacks no asignados a la tienda común. Revisar precios, recompensas y desarme. | Cancelar no cambia recursos; se rechaza vender equipo instalado o asignado. No hay ciclos que generen dinero o componentes gratis. | Verificada en Linux |

**Windows 11:** el paquete actualizado `artifacts/win-x64-post-mvp.zip` está publicado y su integridad se comprobó en Linux. La [guía de aceptación](post-mvp-aceptacion-windows.md) incluye la prueba offline con una copia del stash. Falta ejecutarla en Windows y registrar por separado el PM08 del paquete histórico M05.

## Registro de verificación

### 1. Gorilla Arms

Balance provisional Tier 1: **45 eurodólares**, **6 de capacidad**, **9 de daño base** y **7 de stamina** antes del sobrepeso. Conserva precisión base, bonos del ataque básico, críticos y escudo. La caída al golpe básico no cambia su coste normal.

- **Resultado:** compilación limpia, **80/80 comprobaciones** y cinco recorridos de interfaz. El recorrido de combate confirmó el coste, el nombre del ataque en el resumen y la alternativa sin stamina. La prueba de dominio verificó también la desactivación y la migración v6 → v7 con respaldo y depósitos intactos.

### 2. Otros brazos

| Implante Tier 1 | Precio | Capacidad | Daño base | Stamina | Acierto frente al básico |
|---|---:|---:|---:|---:|---:|
| Mantis Blades | 55 | 7 | 11 | 8 | +5 puntos porcentuales |
| Monowire | 60 | 7 | 8 | 6 | Sin cambio |
| Projectile Launch System | 65 | 9 | 15 | 10 | −10 puntos porcentuales |

Los tres cuentan como ataques físicos; usan stamina, no gastan RAM ni consumen munición del arma equipada. El lanzador no añade un tipo nuevo de munición. Solo se puede instalar uno por zona.

- **Resultado:** compilación limpia, **81/81 comprobaciones** y cinco recorridos de interfaz. Las pruebas de dominio verificaron las tres variantes, su exclusividad, desactivación, extracción y depósito. El recorrido de combate confirmó que, tras usar Projectile Launch System, se ofrece y ejecuta el golpe básico cuando falta stamina. La migración v7 → v8 respaldó la base y conservó los depósitos.

### 3. Quickhacks enemigos

El netrunner de Dogtown prioriza Fallo de arma (**3 RAM**) y Reboot Optics (**2 RAM**) si puede usarlos; si no, considera sus acciones existentes. Cada efecto dura el siguiente turno del jugador. Fallo de arma afecta solo al arma seleccionada en el momento del hackeo; Reboot Optics limita el acierto físico al **5 %** sin mejorar ataques que ya tenían menos. Ambos respetan Self-ICE. Reboot Optics es exclusivo del enemigo y no entra en tiendas, recetas ni stash.

- **Resultado:** compilación limpia, **82/82 comprobaciones** y cinco recorridos de interfaz. Se verificaron intención y estados visibles, cambio a un arma no afectada, rechazo sin gasto, límite de acierto en ambos ataques físicos, quickhack bajo Reboot Optics y bloqueo inicial con Self-ICE. Sin cambios de SQLite en esta etapa.

### 4. Armas de fuego

| Modelo Tier 1 | Componentes | Munición | Tecnología | Accesorios compatibles |
|---|---:|---|---|---|
| Revólver | 10 | Pistola | Power | Ninguno |
| Escopeta de doble cañón | 12 | Cartuchos de escopeta | Power | Ninguno |
| Rifle de asalto | 14 | Rifle | Smart | Mira réflex y cargador ampliado |
| Rifle de precisión | 16 | Rifle | Tech | Mira réflex |

Cada modelo tiene cinco tiers y participa en el sorteo del comerciante. La munición de rifle se vende en la tienda común y se fabrica en lotes de seis.

- **Resultado:** las comprobaciones de dominio verifican el ciclo de los cuatro modelos, el surtido por zona y la migración desde v8 con respaldo de los depósitos.

### 5. Armas cuerpo a cuerpo

Espada: 12 componentes y 16 de daño base. Bate: 7 componentes y 11 de daño base. Ambos gastan 5 de stamina, aceptan críticos y conservan el golpe básico como alternativa independiente. No hay munición ni accesorios.

- **Resultado:** las comprobaciones de dominio cubren ambos modelos, críticos, mejoras, stash y migración desde v9. El recorrido de combate verifica que con espada no aparece «Recargar».

### 6. Cyberware

Se incorporó el catálogo base por zonas del SDD, con precios y efectos provisionales y cinco tiers por implante. Rippler ofrece 9 RAM y recupera 2 por turno en Tier 1. Self-ICE mejora la reducción posterior al bloqueo inicial; Sandevistan reduce su coste de capacidad; Subdermal Armor aumenta su escudo máximo. El netrunner y el centinela de Dogtown entregan Kiroshi básico y Rippler, respectivamente, como botín suelto.

- **Resultado:** las comprobaciones registran recogida sin duplicación, efectos, mejoras, capacidad, extracción y stash. Las alternativas adicionales más allá del catálogo base siguen siendo ampliaciones de contenido.

### 7. Carga y sobrepeso

Titanium Bones añade 4 kg en Tier 1 y 1 kg adicional por mejora. El sobrepeso reduce en 1 la stamina recuperada por turno, también al calcular la recuperación entre eventos. No modifica la RAM, el escudo, el acierto ni la munición.

- **Resultado:** las comprobaciones verifican sobrepeso temporal al desactivar el implante, recuperación del límite al reactivarlo y conservación del coste de RAM.

### 8. Reventa y balance

El ripperdoc compra implantes propios sueltos sin quickhacks asignados. La tienda compra quickhacks sin asignar. Cada venta requiere confirmación y no repone el stock. El Taller también desmonta armas de tier superior y accesorios sueltos; su rendimiento es inferior al coste de fabricación y de sus mejoras. Se mantienen las recompensas por evento tras revisar su relación con precios y rendimientos.

- **Resultado:** las comprobaciones verifican reventa, rechazos y pérdida neta de materiales en los ciclos de fabricación y desarme. Los recorridos de interfaz prueban cancelación y confirmación.

### Regresión conjunta

- Compilación sin advertencias; **87/87 comprobaciones** de dominio.
- Pasaron los cinco recorridos de interfaz: combate, expedición, equipamiento, sesión y stash.
- Las pruebas de migración conservan IDs y depósitos, generan respaldo y actualizan el catálogo a **SQLite v12**.
