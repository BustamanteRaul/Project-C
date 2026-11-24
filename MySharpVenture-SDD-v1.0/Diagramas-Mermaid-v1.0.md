# MySharpVenture · fuentes de diagramas · SDD 1.0

Los PNG del documento principal permiten consultar los gráficos sin un complemento de diagramación. Cada fuente se entrega también como archivo `.mmd`. Los casos de uso utilizan la sintaxis `usecase-beta` de Mermaid 12; su imagen es la alternativa para lectores que no la admiten.

## 01-estados

```mermaid
stateDiagram-v2
    direction TB
    [*] --> Bienvenida
    Bienvenida --> Creacion: Iniciar
    Creacion --> Preparacion: Elegir origen
    Preparacion --> SeleccionZona: Iniciar sin sobrepeso
    SeleccionZona --> Aventura: Elegir zona
    state "Gestion de la aventura" as Aventura {
        [*] --> Evento
        Evento --> Combate: Encuentro hostil
        Evento --> Encuentro: Hallazgo o comerciante
        Combate --> Gestion: Victoria
        Encuentro --> Gestion: Encuentro resuelto
        Gestion --> Evento: Continuar y quedan eventos
        Gestion --> [*]: Continuar tras el quinto evento
    }
    Aventura --> Preparacion: Completar y recuperar recursos
    Combate --> Creacion: Muerte y perdida de carga
    Combate --> Preparacion: Retirada
    Encuentro --> Preparacion: Retirada
    Gestion --> Preparacion: Retirada
    Preparacion --> [*]: Salir
```

## 02-turnos

```mermaid
flowchart TB
    A["Iniciar turno del participante"] --> B["Aplicar daño persistente"]
    B --> C{"¿Sigue vivo?"}
    C -->|No| Z["Finalizar combate y aplicar resultado"]
    C -->|Sí| D["Regenerar y actualizar contadores"]
    D --> E{"¿Quedan acciones?"}
    E -->|No| N["Cambiar al otro participante"]
    N --> A
    E -->|Sí| V["Elegir y validar acción"]
    V --> R{"¿Puede ejecutarse?"}
    R -->|No, corregir selección| V
    R -->|Sí| X["Consumir acción y recursos; resolver efecto"]
    X --> K{"¿Terminó el combate?"}
    K -->|Sí| Z
    K -->|No| E
```

## 03-arquitectura

```mermaid
flowchart TB
    UI["Terminal con Spectre.Console"] --> APP["Coordinación de sesión"]
    APP --> COM["Resolución de combate"]
    APP --> PRE["Servicios de preparación"]
    APP --> CAT["Catálogos y configuración"]
    COM --> DOM["Dominio y reglas"]
    PRE --> DOM
    PRE --> REP["Repositorio del stash"]
    CAT --> DB["SQLite: catálogo y stash"]
    REP --> DB
```

## 04-clases-entidades

```mermaid
classDiagram
direction TB
class Entity {
    <<abstract>>
    +Guid Id
    +string Name
    +int Level
    +decimal CurrentHealth
    +decimal MaxHealth
    +decimal CurrentShield
    +decimal MaxShield
    +decimal CurrentStamina
    +decimal MaxStamina
    +ReceiveDamage(DamageResult result)
    +Regenerate()
}
class Person {
    +OriginPath Origin
    +int Experience
    +int AttributePoints
    +int SkillPoints
    +int CyberwareCapacity
    +int MaxEquippedWeapons = 3
    +IncreaseAttribute(AttributeType type)
    +UnlockPerk(Perk perk)
}
class Bot {
    +string Model
    +int MaxEquippedWeapons = 1
}
class AttributeStat {
    +AttributeType Type
    +int Value
    +int Maximum = 10
}
class Perk {
    +int Id
    +string Name
    +AttributeType Branch
    +int TreeLevel
    +int RequiredAttributeValue
    +int Cost = 1
    +CanUnlockFor(Person person)
}
class EnemyProfile {
    +CombatRole Role
    +string Affiliation
    +ChooseIntention(Entity enemy)
}
Entity <|-- Person
Entity <|-- Bot
Person "1" *-- "5" AttributeStat : atributos
Person "0..*" --> "0..*" Perk : desbloqueadas
Perk "0..*" --> "0..*" Perk : requiere
Entity "0..*" --> "0..1" EnemyProfile : comportamiento enemigo
```

## 05-clases-objetos

```mermaid
classDiagram
direction TB
class Entity
class Inventory {
    +List~Item~ Items
    +decimal CurrentWeight
    +decimal Capacity
    +bool IsOverweight
    +Add(Item item)
    +Remove(Guid runtimeId)
    +WeightOf(Item item)
}
class Item {
    <<abstract>>
    +Guid RuntimeId
    +int CatalogItemId
    +ItemDefinition Definition
}
class Weapon {
    +WeaponVariant Variant
    +int LoadedAmmo
    +int MagazineCapacity
    +Reload()
}
class Accessory {
    +AccessorySlot Slot
    +IsCompatibleWith(Weapon weapon)
}
class Consumable {
    +ConsumableType Type
    +int Quantity
}
class Cyberware {
    +BodyZone Slot
    +int CapacityCost
}
class Quickhack {
    +int RamCost
}
Entity "1" *-- "1" Inventory : pertenencia
Inventory "0..1" --> "0..*" Item : objetos
Item <|-- Weapon
Item <|-- Accessory
Item <|-- Consumable
Item <|-- Cyberware
Item <|-- Quickhack
Weapon "0..1" --> "0..2" Accessory : mira y cargador
```

## 06-clases-equipo

```mermaid
classDiagram
direction TB
class Entity
class Weapon
class Equipment {
    +List~Guid~ EquippedWeaponIds
    +List~Guid~ InstalledCyberwareIds
    +int UsedCyberwareCapacity
    +ValidateCapacity()
}
class Cyberware
class OperatingSystem {
    <<abstract>>
}
class Cyberdeck {
    +decimal CurrentRam
    +decimal MaxRam
    +int MaxQuickhacks = 4
    +List~Guid~ EquippedQuickhackIds
    +SpendRam(int amount)
}
class Sandevistan {
    +Activate(CombatContext context)
}
class Quickhack
class CombatEffect {
    +string EffectCode
    +int RemainingTurns
    +bool IsTemporary
    +Apply(Entity target)
}
Entity "1" *-- "1" Equipment : equipo
Equipment "0..1" --> "0..3" Weapon : armas equipadas
Equipment "0..1" --> "0..10" Cyberware : implantes instalados
Cyberware <|-- OperatingSystem
OperatingSystem <|-- Cyberdeck
OperatingSystem <|-- Sandevistan
Cyberdeck "0..1" --> "0..4" Quickhack : configurados
Quickhack ..> CombatEffect : aplica
Cyberware ..> CombatEffect : aporta o activa
```

## 07-clases-sesion

```mermaid
classDiagram
direction TB
class Person
class ItemDefinition
class GameSession {
    +Person? CurrentCharacter
    +GameState State
}
class Expedition {
    +int EventCount = 5
    +int EventsResolved
    +bool HasMerchant
    +Complete()
    +Retreat()
}
class Zone {
    +string Name
    +string Affiliation
    +int MinEnemyLevel
    +int MaxEnemyLevel
}
class EventOccurrence {
    +int Position
    +EventType Type
    +bool IsResolved
    +Preview()
}
class Stash {
    +List~StashEntry~ Entries
}
class StashEntry {
    +int CatalogItemId
    +int Quantity
}
GameSession "1" *-- "0..1" Person : personaje activo
GameSession "1" *-- "0..1" Expedition : aventura activa
GameSession "1" --> "1" Stash : unico compartido
Expedition "0..*" --> "1" Zone : zona seleccionada
Expedition "1" *-- "5" EventOccurrence : eventos
Stash "1" *-- "0..*" StashEntry : cantidades guardadas
StashEntry "0..*" --> "1" ItemDefinition : objeto del catalogo
```

## 08-clases-persistencia

```mermaid
classDiagram
direction TB
class StashService {
    +Deposit(Guid itemId, int quantity)
    +Withdraw(int catalogItemId, int quantity)
}
class IStashRepository {
    <<interface>>
    +ReadEntries()
    +CommitTransfer(TransferPlan plan)
}
class SqliteStashRepository {
    +ReadEntries()
    +CommitTransfer(TransferPlan plan)
}
class ItemFactory {
    +Create(int catalogItemId)
}
class ItemCatalog {
    +Find(int catalogItemId)
}
class ItemDefinition {
    +int CatalogItemId
    +string Code
    +ItemKind Kind
    +int? Tier
    +decimal UnitWeight
}
class TransferPlan {
    +Validate()
    +PrepareRuntimeChanges()
}
StashService --> IStashRepository : usa
StashService --> ItemFactory : reconstruye
StashService ..> TransferPlan : prepara
SqliteStashRepository ..|> IStashRepository : implementa
IStashRepository ..> TransferPlan : confirma
ItemFactory --> ItemCatalog : consulta
ItemCatalog "1" --> "0..*" ItemDefinition : definiciones
```

## 09-bd-nucleo

```mermaid
erDiagram
    direction TB
    ITEM_CATALOG {
        int catalog_item_id PK
        string code UK
        string name
        string kind
        int tier "Nulo si no corresponde"
        int unit_weight_g
        int buy_price
        int sell_price
        boolean can_stash
        boolean can_craft
        boolean can_dismantle
        int next_tier_id FK, UK "Opcional"
    }
    STASH_ENTRY {
        int catalog_item_id PK, FK
        int quantity "Mayor que cero"
    }
    ITEM_EFFECT {
        int catalog_item_id PK, FK
        int effect_order PK
        string effect_code
        decimal magnitude
        int duration_turns "Opcional"
    }
    ITEM_CATALOG ||--o| STASH_ENTRY : cantidad_guardada
    ITEM_CATALOG ||--o{ ITEM_EFFECT : efectos_definidos
    ITEM_CATALOG o|..o| ITEM_CATALOG : siguiente_tier
```

## 10-bd-armas

```mermaid
erDiagram
    direction TB
    ITEM_CATALOG {
        int catalog_item_id PK
        string kind
    }
    WEAPON_DEFINITION {
        int catalog_item_id PK, FK
        string category
        string variant
        string technology "Opcional"
        decimal base_damage
        decimal stamina_cost
        decimal base_hit_chance
        decimal critical_chance
        decimal critical_multiplier
        int magazine_capacity
        int ammo_catalog_id FK "Nulo en melee"
    }
    ACCESSORY_DEFINITION {
        int catalog_item_id PK, FK
        string slot "Sight o Magazine"
    }
    ACCESSORY_COMPATIBILITY {
        int accessory_catalog_id PK, FK
        string weapon_variant PK
    }
    CONSUMABLE_DEFINITION {
        int catalog_item_id PK, FK
        string subtype
    }
    ITEM_CATALOG ||--o| WEAPON_DEFINITION : detalle_arma
    ITEM_CATALOG ||--o| ACCESSORY_DEFINITION : detalle_accesorio
    ITEM_CATALOG ||--o| CONSUMABLE_DEFINITION : detalle_consumible
    ACCESSORY_DEFINITION ||--|{ ACCESSORY_COMPATIBILITY : compatible_con
    ITEM_CATALOG o|..o{ WEAPON_DEFINITION : es_municion_de
```

## 11-bd-cyberware

```mermaid
erDiagram
    direction TB
    ITEM_CATALOG {
        int catalog_item_id PK
        string kind
    }
    CYBERWARE_DEFINITION {
        int catalog_item_id PK, FK
        string body_zone
        int capacity_cost
        string operating_system_kind "Opcional"
    }
    CYBERDECK_DEFINITION {
        int catalog_item_id PK, FK
        decimal max_ram
        decimal ram_regeneration
    }
    QUICKHACK_DEFINITION {
        int catalog_item_id PK, FK
        decimal ram_cost
    }
    ITEM_CATALOG ||--o| CYBERWARE_DEFINITION : detalle_implante
    CYBERWARE_DEFINITION ||--o| CYBERDECK_DEFINITION : detalle_cyberdeck
    ITEM_CATALOG ||--o| QUICKHACK_DEFINITION : detalle_quickhack
```

## 12-bd-mvp

```mermaid
erDiagram
    direction TB
    ITEM_CATALOG {
        int catalog_item_id PK
        string code UK
        string name
        string kind
        int tier "Armas iniciales de tier 1"
        boolean can_stash
    }
    STASH_ENTRY {
        int catalog_item_id PK, FK
        int quantity "Entero positivo"
    }
    WEAPON_DEFINITION {
        int catalog_item_id PK, FK
        string variant
        decimal base_damage
        decimal base_hit_chance
        int magazine_capacity
        int ammo_catalog_id FK
    }
    CONSUMABLE_DEFINITION {
        int catalog_item_id PK, FK
        string subtype "Municion o curacion"
    }
    ITEM_EFFECT {
        int catalog_item_id PK, FK
        int effect_order PK
        string effect_code
        decimal magnitude
    }
    ITEM_CATALOG ||--o| STASH_ENTRY : cantidad_guardada
    ITEM_CATALOG ||--o| WEAPON_DEFINITION : detalle_arma
    ITEM_CATALOG ||--o| CONSUMABLE_DEFINITION : detalle_consumible
    ITEM_CATALOG ||--o{ ITEM_EFFECT : efecto_del_objeto
    ITEM_CATALOG ||..o{ WEAPON_DEFINITION : es_municion_de
```

## 13-casos-uso-mvp

```text
usecase-beta
direction LR
actor Jugador("Jugador")
systemBoundary sistema["MySharpVenture - MVP"]
    CU05("CU05 Crear personaje")
    CU07("CU07 Gestionar inventario y equipo")
    CU03("CU03 Gestionar stash")
    CU01("CU01 Completar aventura")
    CU02("CU02 Resolver combate")
    CU15("CU15 Recoger botín")
    CU08("CU08 Utilizar curación")
    CU14("CU14 Salir del juego")
end
Jugador -- CU05
Jugador -- CU07
Jugador -- CU03
Jugador -- CU01
Jugador -- CU02
Jugador -- CU15
Jugador -- CU08
Jugador -- CU14
CU02 ..> : extend CU01
CU15 ..> : extend CU01
```

## 14-casos-uso-aventura

```text
usecase-beta
direction LR
actor Jugador("Jugador")
systemBoundary sistema["MySharpVenture - sesión y aventura"]
    CU05("CU05 Crear personaje")
    CU01("CU01 Completar aventura")
    CU02("CU02 Resolver combate")
    CU15("CU15 Recoger botín")
    CU13("CU13 Retirarse")
    CU14("CU14 Salir del juego")
end
Jugador -- CU05
Jugador -- CU01
Jugador -- CU02
Jugador -- CU15
Jugador -- CU13
Jugador -- CU14
CU02 ..> : extend CU01
CU15 ..> : extend CU01
CU13 ..> : extend CU01
```

## 15-casos-uso-preparacion

```text
usecase-beta
direction LR
actor Jugador("Jugador")
systemBoundary sistema["MySharpVenture - preparación y equipo"]
    CU03("CU03 Gestionar stash")
    CU04("CU04 Mejorar objeto")
    CU06("CU06 Asignar puntos")
    CU07("CU07 Gestionar inventario y equipo")
    CU08("CU08 Utilizar curación")
    CU09("CU09 Comprar o vender")
    CU10("CU10 Visitar ripperdoc")
    CU11("CU11 Fabricar objeto")
    CU12("CU12 Desarmar objeto")
end
Jugador -- CU03
Jugador -- CU04
Jugador -- CU06
Jugador -- CU07
Jugador -- CU08
Jugador -- CU09
Jugador -- CU10
Jugador -- CU11
Jugador -- CU12
```

## 16-gantt

```mermaid
gantt
    title MySharpVenture - MVP 2 de diciembre de 2026
    dateFormat YYYY-MM-DD
    axisFormat %d/%m
    excludes sunday
    todayMarker off
    section Trabajo previo
    SDD disponible :done, milestone, doc, 2026-11-14, 0d
    section Implementacion del MVP
    M01 Entorno y navegacion 4 h :m01, 2026-11-16, 2026-11-17
    M02 Combate e inventario 20 h :m02, 2026-11-17, 2026-11-22
    M03 Watson y resultados 8 h :m03, 2026-11-23, 2026-11-25
    M04 SQLite y stash 8 h :m04, 2026-11-25, 2026-11-27
    section Validacion y margen
    M05 Pruebas y empaquetado 8 h :m05, 2026-11-27, 2026-11-29
    M06 Reserva de correcciones 8 h :m06, 2026-11-30, 2026-12-02
    section Hitos
    Candidato completo previsto :milestone, candidato, 2026-11-28, 0d
    Entrega escolar :milestone, entrega, 2026-12-02, 0d
```

