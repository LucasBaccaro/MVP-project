# 📘 ESPECIFICACIONES TÉCNICAS - MMO MVP

**Proyecto:** MMO Multiplayer con Mirror
**Engine:** Unity 6
**Networking:** Mirror (última versión)
**Render Pipeline:** URP (Universal Render Pipeline)
**Fecha de creación:** Enero 2026
**Estado:** Fases 0-4 completadas (Login, Player, Zonas, Clases, Inventario)

---

## 📁 ESTRUCTURA DEL PROYECTO

```
MVP-project/
├── Assets/
│   ├── _Game/                          # Carpeta principal del proyecto
│   │   ├── Prefabs/
│   │   │   ├── Player/
│   │   │   │   ├── Player.prefab       # Prefab del jugador con NetworkIdentity
│   │   │   │   └── PlayerCamera.prefab # Cámara con CameraFollow
│   │   │   ├── UI/
│   │   │   │   └── InventorySlot.prefab # Slot del inventario con Drag & Drop
│   │   │   ├── NPCs/
│   │   │   └── Items/
│   │   ├── Scenes/
│   │   │   ├── MenuPrincipal.unity     # Escena de login y selección de clase
│   │   │   └── GameWorld.unity         # Escena del mundo del juego
│   │   ├── Scripts/
│   │   │   ├── Core/
│   │   │   │   └── ClassData.cs        # ScriptableObject de clases
│   │   │   ├── Player/
│   │   │   │   ├── PlayerController.cs  # Movimiento WASD y cámara
│   │   │   │   ├── PlayerStats.cs       # HP, Mana, Oro, Level, XP
│   │   │   │   ├── ZoneHandler.cs       # Detección de zonas seguras/inseguras
│   │   │   │   └── CameraFollow.cs      # Sistema de cámara smooth follow
│   │   │   ├── Network/
│   │   │   │   └── NetworkManagerMMO.cs # NetworkManager personalizado
│   │   │   ├── UI/
│   │   │   │   ├── LoginUI.cs           # UI de login y selección de clase
│   │   │   │   ├── ZoneUIManager.cs     # UI de indicador de zona
│   │   │   │   ├── PlayerHUD.cs         # HUD de stats del jugador
│   │   │   │   ├── InventoryUI.cs       # Manager del UI del inventario
│   │   │   │   ├── InventorySlotUI.cs   # Slot individual con Drag & Drop
│   │   │   │   └── ItemTester.cs        # Script de testing para items
│   │   │   ├── Items/
│   │   │   │   ├── ItemData.cs          # ScriptableObject de items
│   │   │   │   ├── ItemDatabase.cs      # Base de datos de items (Singleton)
│   │   │   │   └── PlayerInventory.cs   # Sistema de inventario (SyncList)
│   │   │   ├── Editor/
│   │   │   │   └── ItemCreator.cs       # Editor window para crear items
│   │   │   ├── Combat/
│   │   │   └── NPCs/
│   │   └── ScriptableObjects/
│   │       ├── Guerrero.asset          # Clase Guerrero (marrón)
│   │       ├── Mago.asset              # Clase Mago (azul)
│   │       ├── Cazador.asset           # Clase Cazador (verde)
│   │       ├── Sacerdote.asset         # Clase Sacerdote (amarillo)
│   │       └── Items/                  # Items del juego
│   │           ├── HealthPotion.asset  # Poción de Salud
│   │           ├── ManaPotion.asset    # Poción de Maná
│   │           ├── IronSword.asset     # Espada de Hierro
│   │           ├── WoodenShield.asset  # Escudo de Madera
│   │           └── GoldCoin.asset      # Moneda de Oro
│   ├── Mirror/                         # Framework de networking
│   └── Settings/                       # Configuraciones de Unity
├── ProjectSettings/
│   ├── TagManager.asset                # Tags y Layers configurados
│   ├── EditorBuildSettings.asset       # Escenas en Build
│   └── ProjectSettings.asset           # Input System configurado
└── Packages/
    └── manifest.json                   # Paquetes instalados
```

---

## 🔧 CONFIGURACIÓN DEL PROYECTO

### Paquetes Instalados

```json
{
  "com.unity.ai.navigation": "2.0.9",           // NavMesh moderno
  "com.unity.inputsystem": "1.17.0",           // Nuevo Input System
  "com.unity.render-pipelines.universal": "17.3.0",  // URP
  "com.unity.textmeshpro": "incluido en UGI",  // UI de texto
  "Mirror": "latest"                            // Networking
}
```

### Layers Configurados

| Layer | Nombre | Uso |
|-------|--------|-----|
| 0 | Default | Objetos por defecto |
| 3 | **Ground** | Suelo navegable (NavMesh) |
| 5 | UI | Interfaz de usuario |
| 6 | **Player** | Jugadores |
| 7 | **Enemy** | NPCs enemigos |
| 8 | **ZoneTrigger** | Detectores de zona |

### Tags Configurados

- `SafeZone` - Zonas seguras (sin PvP)
- `UnsafeZone` - Zonas peligrosas (con PvP)
- `Enemy` - NPCs enemigos
- `Player` - Jugadores

### Build Settings

**Escenas en Build (en orden):**
1. `Assets/_Game/Scenes/MenuPrincipal.unity` (índice 0)
2. `Assets/_Game/Scenes/GameWorld.unity` (índice 1)

### Input System

**Configuración:** Both (Legacy + New Input System)
- `activeInputHandler: 2` en ProjectSettings.asset
- Permite usar `Input.GetAxis()` con compatibilidad futura

---

## 🌐 ARQUITECTURA DE RED (Mirror)

### Componentes de Mirror Utilizados

#### 1. **NetworkManager**
- Script base: `Mirror.NetworkManager`
- Custom: `NetworkManagerMMO` (hereda de NetworkManager)
- Ubicación: Objeto `NetworkManager` en escena `MenuPrincipal`

**Configuración:**
```
NetworkManagerMMO:
├── Player Prefab: Player.prefab
├── Offline Scene: MenuPrincipal
├── Online Scene: GameWorld
├── Transport: KcpTransport (UDP)
└── Available Classes: [Guerrero, Mago, Cazador, Sacerdote]
```

#### 2. **Transport: KcpTransport**
- Protocolo: UDP
- Puerto: 7777 (por defecto)
- Auto-detectado por Mirror (mismo GameObject que NetworkManager)

#### 3. **NetworkIdentity**
- En: `Player.prefab`
- Server Authority: Activado
- Sincroniza la existencia del objeto en red

#### 4. **NetworkTransform**
- En: `Player.prefab`
- Sync Direction: **Client To Server**
- Interpolate: ✅ Position & Rotation
- Sincroniza movimiento del jugador

---

## 🎮 SISTEMA DE JUGADOR

### Prefab: Player.prefab

**Jerarquía:**
```
Player (GameObject)
├── Components:
│   ├── CharacterController (movimiento)
│   ├── NetworkIdentity (red)
│   ├── NetworkTransform (sincronización de posición)
│   ├── PlayerController (script - movimiento WASD)
│   ├── PlayerStats (script - stats y clase)
│   └── ZoneHandler (script - detección de zonas)
├── Capsule (hijo - visual con MeshRenderer)
└── (NO tiene cámara - se instancia en runtime)
```

### PlayerController.cs

**Responsabilidades:**
- Movimiento WASD con CharacterController
- Instanciación de cámara para jugador local
- Rotación del personaje hacia dirección de movimiento

**Propiedades:**
```csharp
public GameObject cameraPrefab;           // Prefab de cámara (asignar en Inspector)
public CharacterController characterController;
public float speed = 5f;
public float rotationSpeed = 10f;
private Camera playerCamera;              // Instancia en runtime
private CameraFollow cameraFollow;        // Instancia en runtime
```

**Flujo:**
1. `OnStartLocalPlayer()`: Solo se ejecuta en el jugador local
   - Instancia `PlayerCamera.prefab`
   - Configura `CameraFollow` apuntando al transform del jugador
2. `Update()`: Solo procesa input si `isLocalPlayer == true`
3. `OnDestroy()`: Limpia la cámara instanciada

### PlayerStats.cs

**Responsabilidades:**
- Gestión de stats del jugador (HP, Mana, Oro, Level, XP)
- Aplicación de clase (ScriptableObject)
- Sincronización de stats en red (SyncVars)
- Regeneración de recursos
- Aplicación de color de clase al material

**SyncVars:**
```csharp
// Clase
[SyncVar]
public string className = "Unknown";  // Nombre de la clase sincronizado

// Health
[SyncVar(hook = nameof(OnHealthChanged))]
public int currentHealth;

[SyncVar]
public int maxHealth;  // IMPORTANTE: maxHealth debe ser SyncVar

// Mana
[SyncVar(hook = nameof(OnManaChanged))]
public int currentMana;

[SyncVar]
public int maxMana;  // IMPORTANTE: maxMana debe ser SyncVar

// Recursos
[SyncVar(hook = nameof(OnGoldChanged))]
public int gold;

// Experience
[SyncVar(hook = nameof(OnLevelChanged))]
public int level;

[SyncVar(hook = nameof(OnXPChanged))]
public int currentXP;

[SyncVar]
public int xpToNextLevel;  // IMPORTANTE: xpToNextLevel debe ser SyncVar

// Combat
[SyncVar]
public int damage;

// Regeneración
[SyncVar]
public float hpRegenRate;

[SyncVar]
public float manaRegenRate;

// Visual
[SyncVar(hook = nameof(OnClassColorChanged))]
private Color classColor;  // Color de la clase
```

**IMPORTANTE:**
- Los SyncVars se sincronizan automáticamente del servidor a todos los clientes
- **Todos los stats deben ser SyncVars**, no solo los "current"
- Si solo sincronizas `currentHealth` pero no `maxHealth`, los clientes verán valores incorrectos
- El `ClassData` (ScriptableObject) **NO se sincroniza**, solo existe en el servidor
- Por eso sincronizamos `className` (string) en lugar del objeto completo

**Método crítico:**
```csharp
[Server]
public void InitializeStats(ClassData data)
{
    // Se llama desde NetworkManagerMMO.OnServerAddPlayer()
    // Aplica stats base de la clase al jugador

    classData = data;  // Solo en servidor (NO se sincroniza)
    className = data.className;  // SyncVar - se sincroniza a clientes

    // Todos estos son SyncVars - se sincronizan automáticamente
    maxHealth = data.baseHP;
    currentHealth = maxHealth;
    maxMana = data.baseMana;
    currentMana = maxMana;
    damage = data.baseDamage;
    gold = data.startingGold;
    level = data.startingLevel;
    hpRegenRate = data.hpRegenRate;
    manaRegenRate = data.manaRegenRate;
    classColor = data.classColor;
}
```

**Aplicación de Color (URP):**
```csharp
private void OnClassColorChanged(Color oldColor, Color newColor)
{
    // Hook del SyncVar - se ejecuta en todos los clientes
    Material instanceMaterial = new Material(characterRenderer.material);

    // Compatible con URP
    if (instanceMaterial.HasProperty("_BaseColor"))
        instanceMaterial.SetColor("_BaseColor", newColor);

    characterRenderer.material = instanceMaterial;
}
```

**CRÍTICO:** Cada jugador necesita su propia **instancia del material** para tener colores diferentes.

### ZoneHandler.cs

**Responsabilidades:**
- Detectar entrada/salida de zonas (Triggers)
- Sincronizar estado de zona en red (SyncVar)
- Notificar al UI cuando cambia de zona

**SyncVar:**
```csharp
[SyncVar(hook = nameof(OnSafeZoneChanged))]
public bool isSafeZone = false;
```

**Flujo:**
1. Cliente detecta trigger (`OnTriggerEnter`)
2. Cliente envía Command al servidor (`CmdSetSafeZone(true)`)
3. Servidor actualiza SyncVar
4. SyncVar se sincroniza a todos los clientes
5. Hook `OnSafeZoneChanged` actualiza UI

---

## 🎨 SISTEMA DE CLASES

### ScriptableObject: ClassData.cs

**Propiedades:**
```csharp
public string className;        // "Guerrero", "Mago", etc.
public string description;      // Descripción de la clase
public Color classColor;        // Color del material

// Stats base
public int baseHP;              // Vida base
public int baseMana;            // Maná base
public int baseDamage;          // Daño base

// Regeneración
public float hpRegenRate;       // HP/segundo
public float manaRegenRate;     // Mana/segundo

// Recursos iniciales
public int startingGold;        // Oro inicial
public int startingLevel;       // Nivel inicial (1)
```

### Assets Creados

| Clase | Color | HP | Mana | Daño | HP Regen | Mana Regen |
|-------|-------|----|----|------|----------|------------|
| **Guerrero** | Marrón (0.6, 0.4, 0.2) | 150 | 30 | 15 | 2 | 1 |
| **Mago** | Azul (0.2, 0.4, 1.0) | 80 | 150 | 8 | 1 | 5 |
| **Cazador** | Verde (0.2, 0.8, 0.2) | 100 | 80 | 12 | 1.5 | 2 |
| **Sacerdote** | Amarillo (1.0, 0.9, 0.2) | 110 | 120 | 7 | 3 | 4 |

---

## 🔐 SISTEMA DE LOGIN Y NETWORKING

### Flujo de Conexión

#### A. Host (Servidor + Cliente Local)

**Escena MenuPrincipal:**
1. Usuario ingresa nombre: "Player1"
2. Usuario selecciona clase: Mago (índice 1)
3. Click en "Crear Servidor (Host)"
4. `LoginUI` asigna:
   ```csharp
   networkManager.playerName = "Player1";
   networkManager.selectedClassIndex = 1;
   ```
5. `NetworkManagerMMO.StartHost()` se ejecuta
6. Cambia a escena `GameWorld`

**Escena GameWorld:**
7. `OnServerAddPlayer()` se ejecuta
8. Detecta que es `LocalConnectionToClient` (Host)
9. Usa variables locales: `playerName` y `selectedClassIndex`
10. Aplica clase Mago al jugador
11. Spawns el jugador en la red

#### B. Cliente (Solo Cliente)

**Escena MenuPrincipal:**
1. Usuario ingresa nombre: "Player2"
2. Usuario selecciona clase: Guerrero (índice 0)
3. Click en "Conectar como Cliente"
4. `LoginUI` asigna:
   ```csharp
   networkManager.playerName = "Player2";
   networkManager.selectedClassIndex = 0;
   ```
5. `NetworkManagerMMO.ConnectAsClient()` se ejecuta
6. `OnClientConnect()` se ejecuta en el cliente
7. Cliente envía mensaje al servidor:
   ```csharp
   CharacterMessage msg = new CharacterMessage
   {
       characterName = "Player2",
       classIndex = 0
   };
   NetworkClient.Send(msg);
   ```

**En el Servidor:**
8. `OnCharacterMessageReceived()` recibe el mensaje
9. Guarda datos en Dictionary:
   ```csharp
   pendingPlayers[conn.connectionId] = new PlayerData
   {
       name = "Player2",
       classIndex = 0
   };
   ```
10. `OnServerAddPlayer()` se ejecuta
11. Detecta que NO es LocalConnection
12. Busca datos en Dictionary usando `conn.connectionId`
13. Aplica clase Guerrero al jugador
14. Spawns el jugador en la red

### NetworkManagerMMO.cs - Componentes Clave

```csharp
// Datos locales (solo para Host)
public string playerName = "";
public int selectedClassIndex = 0;
public ClassData[] availableClasses = new ClassData[4];

// Datos remotos (clientes)
private Dictionary<int, PlayerData> pendingPlayers;

// Mensaje de red
public struct CharacterMessage : NetworkMessage
{
    public string characterName;
    public int classIndex;
}

// Handler del mensaje (solo servidor)
private void OnCharacterMessageReceived(NetworkConnectionToClient conn, CharacterMessage msg)
{
    pendingPlayers[conn.connectionId] = new PlayerData
    {
        name = msg.characterName,
        classIndex = msg.classIndex
    };
}
```

---

## 📷 SISTEMA DE CÁMARA

### Prefab: PlayerCamera.prefab

**Componentes:**
```
PlayerCamera (GameObject)
├── Camera (componente)
└── CameraFollow (script)
```

### CameraFollow.cs

**Propiedades:**
```csharp
public Transform target;              // Se asigna en runtime
public Vector3 offset = (0, 15, -10); // Posición relativa al jugador
public float followSpeed = 10f;       // Velocidad de seguimiento
public float rotationSpeed = 5f;
public bool lookAtTarget = true;      // Mira al jugador
public float lookAtYOffset = 1f;      // Offset vertical de la mirada
```

**Flujo:**
1. `PlayerController.OnStartLocalPlayer()` instancia el prefab
2. `CameraFollow.SetTarget(transform)` se llama
3. `LateUpdate()` actualiza posición y rotación cada frame

**Importante:** La cámara NO es hija del prefab Player, se instancia independientemente para evitar rotaciones no deseadas.

---

## 🗺️ SISTEMA DE ZONAS

### GameWorld - Configuración

**Objetos en escena:**
```
GameWorld
├── Ground (Plane)
│   ├── Layer: Ground
│   ├── Scale: (10, 1, 10) = 100x100 unidades
│   └── NavMeshSurface (componente)
├── SafeZone (Cube - Trigger)
│   ├── Tag: SafeZone
│   ├── Layer: ZoneTrigger
│   ├── Is Trigger: ✅
│   └── Scale: (20, 5, 20)
└── GameWorldCanvas (UI)
    ├── ZoneUIManager (componente)
    ├── PlayerHUDPanel
    │   └── [ClassText, HPText, ManaText, GoldText, LevelText, XPText]
    └── ZoneStatusText
```

### NavMesh (Unity 6)

**Método:** NavMeshSurface (nuevo sistema)
- Componente en objeto `Ground`
- Configuración: Agent Type = Humanoid, Collect Objects = All
- Click "Bake" para generar NavMesh azul

**Nota:** NO usar la ventana Navigation antigua (no existe en Unity 6 con AI Navigation 2.0+)

---

## 🎨 UI DEL PROYECTO

### MenuPrincipal - UI de Login

**Jerarquía:**
```
Canvas (Screen Space - Overlay)
├── LoginUI (componente - en el Canvas)
└── LoginPanel
    ├── ClassDropdown (TMP_Dropdown)
    ├── NameInputField (TMP_InputField)
    ├── HostButton (Button)
    ├── ClientButton (Button)
    └── StatusText (TMP_Text)
```

**LoginUI.cs - Referencias:**
- Dropdown se llena automáticamente desde `NetworkManager.availableClasses`
- Guarda selección en PlayerPrefs para persistencia

### GameWorld - UI del Juego

**Jerarquía:**
```
GameWorldCanvas (Screen Space - Overlay)
├── ZoneUIManager (componente)
├── PlayerHUD (componente)
├── PlayerHUDPanel (Top-Left)
│   ├── ClassText: "Clase: Guerrero"
│   ├── HPText: "HP: 150/150" (rojo)
│   ├── ManaText: "Mana: 30/30" (azul)
│   ├── GoldText: "Oro: 100" (amarillo)
│   ├── LevelText: "Nivel: 1" (verde)
│   └── XPText: "XP: 0/100" (violeta)
└── ZoneStatusText (Top-Center)
    └── "ZONA SEGURA" (verde) / "ZONA PELIGROSA" (rojo)
```

**PlayerHUD.cs:**
- Busca automáticamente el `PlayerStats` del jugador local
- Se actualiza cada frame en `Update()`

---

## ⚙️ DETALLES TÉCNICOS IMPORTANTES

### 1. Mirror - SyncVar vs ClientRpc vs Command

| Tipo | Dirección | Cuándo usar |
|------|-----------|-------------|
| **SyncVar** | Server → Clients | Sincronizar variables automáticamente |
| **Command** | Client → Server | Cliente pide acción al servidor |
| **ClientRpc** | Server → Clients | Servidor ejecuta función en todos los clientes |

**Ejemplo - Sistema de Color:**
```csharp
// ❌ MAL: ClientRpc antes del spawn
[Server]
public void InitializeStats(ClassData data)
{
    RpcApplyClassColor(data.classColor);  // Error: "un-spawned object"
}

// ✅ BIEN: SyncVar con hook
[SyncVar(hook = nameof(OnClassColorChanged))]
private Color classColor;

[Server]
public void InitializeStats(ClassData data)
{
    classColor = data.classColor;  // Se sincroniza automáticamente después del spawn
}

private void OnClassColorChanged(Color oldColor, Color newColor)
{
    // Se ejecuta automáticamente cuando se sincroniza
    ApplyColor(newColor);
}
```

### 2. URP - Aplicación de Colores

**Problema:** `material.color` no funciona con shaders URP.

**Solución:**
```csharp
// URP usa "_BaseColor" en lugar de "_Color"
if (material.HasProperty("_BaseColor"))
    material.SetColor("_BaseColor", color);
else if (material.HasProperty("_Color"))
    material.SetColor("_Color", color);  // Fallback Legacy
else
    material.color = color;  // Último recurso
```

### 3. Materiales Únicos por Jugador

**Problema:** Todos los jugadores comparten el mismo material, cambian al mismo color.

**Solución:** Crear instancia del material
```csharp
Material instanceMaterial = new Material(characterRenderer.material);
instanceMaterial.SetColor("_BaseColor", color);
characterRenderer.material = instanceMaterial;
```

### 4. isLocalPlayer vs isServer vs isClient

```csharp
if (isLocalPlayer)   // TRUE solo en TU jugador controlado
if (isServer)        // TRUE en el servidor (incluye Host)
if (isClient)        // TRUE en el cliente (incluye Host)
if (hasAuthority)    // TRUE si tienes control del objeto
```

**Uso común:**
- `isLocalPlayer`: Input, cámara, UI del jugador
- `isServer`: Lógica de juego, validaciones, spawning
- `isClient`: Efectos visuales, sonidos

### 5. LocalConnectionToClient vs NetworkConnectionToClient

```csharp
if (conn is LocalConnectionToClient)
{
    // Es el HOST (servidor + cliente local)
    // Usar variables locales del NetworkManager
}
else
{
    // Es un CLIENTE REMOTO
    // Usar datos recibidos por mensajes de red
}
```

---

## 🐛 PROBLEMAS COMUNES Y SOLUCIONES

### Problema 1: "Display 1 No Cameras Rendering"
**Causa:** La cámara no se está activando para el jugador local
**Solución:** Verificar que:
1. `PlayerController.cameraPrefab` esté asignado
2. `OnStartLocalPlayer()` se ejecute correctamente
3. La cámara se instancie y active

### Problema 2: Todos los jugadores tienen el mismo color
**Causa:** No se está creando instancia del material
**Solución:** `new Material(original)` antes de aplicar color

### Problema 3: "Input.GetAxis not available with Input System"
**Causa:** Input System configurado solo para nuevo sistema
**Solución:** Cambiar `activeInputHandler: 2` (Both) en ProjectSettings

### Problema 4: Cliente aparece como "Unknown" clase y stats incorrectos
**Causa:**
1. El cliente no envía su clase al servidor (resuelto con `CharacterMessage`)
2. Los stats (maxHealth, maxMana, etc.) NO son SyncVars

**Solución:**
1. Implementar `CharacterMessage` con handler en servidor
2. **CRÍTICO:** Hacer que TODOS los stats sean SyncVars:
   ```csharp
   [SyncVar] public int maxHealth;     // No solo currentHealth
   [SyncVar] public int maxMana;       // No solo currentMana
   [SyncVar] public int xpToNextLevel;
   [SyncVar] public int damage;
   [SyncVar] public float hpRegenRate;
   [SyncVar] public float manaRegenRate;
   [SyncVar] public string className;  // Sincronizar nombre, no el ScriptableObject
   ```
3. En el UI, usar `playerStats.className` en lugar de `playerStats.classData.className`

**Por qué:** El ScriptableObject `classData` solo existe en el servidor. Los clientes nunca lo reciben. Si solo sincronizas `currentHealth` pero no `maxHealth`, el cliente ve valores incorrectos (ej: 110/100 en lugar de 110/150).

### Problema 5: "ClientRpc called on un-spawned object"
**Causa:** Llamar RPC antes de `NetworkServer.Spawn()`
**Solución:** Usar SyncVar con hook en lugar de ClientRpc

### Problema 6: NavMesh no se genera (Unity 6)
**Causa:** Intentar usar ventana Navigation antigua
**Solución:** Usar componente `NavMeshSurface` en el suelo y hacer Bake ahí

---

## 📦 PREFABS Y ASSETS CLAVE

### Prefabs
1. **Player.prefab** (`Assets/_Game/Prefabs/Player/`)
   - Asignar en: NetworkManager > Player Prefab
   - Debe tener: NetworkIdentity, NetworkTransform

2. **PlayerCamera.prefab** (`Assets/_Game/Prefabs/Player/`)
   - Asignar en: Player.prefab > PlayerController > Camera Prefab
   - Debe tener: Camera, CameraFollow

### ScriptableObjects
1. **Guerrero.asset, Mago.asset, Cazador.asset, Sacerdote.asset**
   - Ubicación: `Assets/_Game/ScriptableObjects/`
   - Asignar en: NetworkManager > Available Classes (array de 4)

---

## 🔄 FLUJO COMPLETO DEL JUEGO

```
INICIO
  │
  ├─ MenuPrincipal (Escena)
  │   ├─ Usuario ingresa nombre
  │   ├─ Usuario selecciona clase (Dropdown)
  │   └─ Click Host/Client
  │       │
  │       ├─ HOST: StartHost()
  │       │   └─ Usa variables locales
  │       │
  │       └─ CLIENT: ConnectAsClient()
  │           └─ Envía CharacterMessage al servidor
  │
  ├─ SERVIDOR: OnServerAddPlayer()
  │   ├─ Instancia Player.prefab
  │   ├─ Obtiene datos (local o Dictionary)
  │   ├─ Aplica ClassData (InitializeStats)
  │   │   ├─ Asigna HP, Mana, Damage, etc.
  │   │   └─ Asigna classColor (SyncVar)
  │   └─ NetworkServer.Spawn(player)
  │
  ├─ CLIENTE: OnStartLocalPlayer()
  │   ├─ Instancia PlayerCamera
  │   ├─ Configura CameraFollow
  │   └─ Activa cámara
  │
  ├─ SINCRONIZACIÓN AUTOMÁTICA
  │   ├─ SyncVar classColor → OnClassColorChanged()
  │   │   └─ Aplica color al material (URP compatible)
  │   │
  │   └─ SyncVars de stats → Hooks actualizan UI
  │
  └─ JUEGO ACTIVO
      ├─ PlayerController: Movimiento WASD
      ├─ CameraFollow: Sigue al jugador
      ├─ PlayerStats: Regenera HP/Mana
      ├─ ZoneHandler: Detecta zonas
      └─ PlayerHUD: Muestra stats
```

---

## 🎒 SISTEMA DE INVENTARIO (FASE 4)

### Arquitectura del Inventario

El inventario utiliza **SyncList** de Mirror para sincronización automática entre servidor y clientes.

**Flujo de datos:**
```
Cliente: Arrastra Item A → Slot B
    ↓
Cliente: Llama CmdSwapItems(indexA, indexB)
    ↓
Servidor: Valida y ejecuta swap en SyncList
    ↓
Mirror: Sincroniza cambios a TODOS los clientes
    ↓
Clientes: Hook actualiza UI automáticamente
```

### Scripts del Sistema

#### 1. ItemData.cs (ScriptableObject)

**Ubicación:** `Assets/_Game/Scripts/Items/ItemData.cs`

Define las propiedades de un item:

```csharp
[CreateAssetMenu(fileName = "New Item", menuName = "MMO/Item Data")]
public class ItemData : ScriptableObject
{
    public int itemID;              // ID único
    public string itemName;
    public string description;
    public Sprite icon;

    public ItemType itemType;       // Consumable, Weapon, Armor, etc.
    public bool isStackable;
    public int maxStackSize;

    public int goldValue;
    public int healthRestore;       // Para consumibles
    public int manaRestore;
    public int damageBonus;         // Para armas
    public int armorBonus;          // Para armaduras
}

public enum ItemType
{
    Consumable,     // Pociones, comida
    Weapon,         // Espadas, hachas, arcos
    Armor,          // Armaduras, escudos
    Quest,          // Items de quest
    Material,       // Materiales de crafteo
    Currency,       // Monedas, oro (se suma directo a PlayerStats.gold, NO va al inventario)
    Misc            // Otros
}
```

#### 2. ItemDatabase.cs (Singleton)

**Ubicación:** `Assets/_Game/Scripts/Items/ItemDatabase.cs`

Base de datos centralizada con acceso rápido por ID:

```csharp
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }
    public List<ItemData> allItems;
    private Dictionary<int, ItemData> itemDictionary;

    public ItemData GetItem(int itemID);
    public bool ItemExists(int itemID);
    public List<ItemData> GetItemsByType(ItemType type);
}
```

**Configuración en escena:**
- GameObject `ItemDatabase` en escena `GameWorld`
- Lista `allItems` con todos los ScriptableObjects de items

#### 3. PlayerInventory.cs (NetworkBehaviour)

**Ubicación:** `Assets/_Game/Scripts/Items/PlayerInventory.cs`

**Struct InventorySlot:**
```csharp
[Serializable]
public struct InventorySlot : IEquatable<InventorySlot>
{
    public int itemID;      // -1 = vacío
    public int amount;
}
```

**Componente principal:**
```csharp
public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] private int inventorySize = 20;

    // CRÍTICO: SyncList sincroniza automáticamente
    public readonly SyncList<InventorySlot> inventory = new SyncList<InventorySlot>();

    public event Action OnInventoryChanged;

    // Callback cuando SyncList cambia
    void OnInventoryUpdated(SyncList<InventorySlot>.Operation op, int index,
                           InventorySlot oldItem, InventorySlot newItem)
    {
        OnInventoryChanged?.Invoke();  // Notificar al UI
    }
}
```

**Commands disponibles:**

| Command | Parámetros | Descripción |
|---------|-----------|-------------|
| `CmdSwapItems` | indexA, indexB | Intercambia dos slots |
| `CmdAddItem` | itemID, amount | Añade item (apila si es posible). **Si es Currency, suma directo a gold** |
| `CmdRemoveItem` | slotIndex, amount | Remueve cantidad de un slot |
| `CmdUseItem` | slotIndex | Usa consumible (restaura HP/Mana) |

**IMPORTANTE:** Los Commands de Mirror **NO pueden tener parámetros opcionales**. Todos los parámetros deben ser explícitos.

**Lógica especial de Currency:**

`CmdAddItem()` detecta automáticamente items de tipo `Currency` y los maneja diferente:

```csharp
[Command]
public void CmdAddItem(int itemID, int amount)
{
    ItemData itemData = ItemDatabase.Instance?.GetItem(itemID);

    // CASO ESPECIAL: Currency (oro, monedas) se suma directo a PlayerStats
    if (itemData.itemType == ItemType.Currency)
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            int goldToAdd = itemData.goldValue * amount;
            stats.gold += goldToAdd;  // Suma directo al oro del jugador
            RpcShowGoldPickup(goldToAdd);  // Feedback visual
        }
        return; // NO añadir al inventario
    }

    // Items normales: añadir al inventario...
}
```

**ClientRpc para feedback:**
```csharp
[ClientRpc]
void RpcShowGoldPickup(int goldAmount)
{
    Debug.Log($"+{goldAmount} oro recogido");
    // Aquí se puede mostrar texto flotante, sonido, partículas doradas, etc.
}
```

#### 4. InventoryUI.cs

**Ubicación:** `Assets/_Game/Scripts/UI/InventoryUI.cs`

Manager del UI que se sincroniza con `PlayerInventory`:

```csharp
public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel;
    public GameObject slotPrefab;
    public Transform slotsContainer;
    public Canvas mainCanvas;
    public KeyCode toggleKey = KeyCode.I;

    private List<InventorySlotUI> slotUIList;
    private PlayerInventory playerInventory;

    void TryInitialize()
    {
        // Buscar jugador local y suscribirse a cambios
        playerInventory.OnInventoryChanged += RefreshUI;
    }

    void RefreshUI()
    {
        for (int i = 0; i < slotUIList.Count; i++)
        {
            slotUIList[i].UpdateSlot(playerInventory.inventory[i]);
        }
    }
}
```

**Requiere namespace:** `using Game.Player;`

#### 5. InventorySlotUI.cs

**Ubicación:** `Assets/_Game/Scripts/UI/InventorySlotUI.cs`

Slot individual con Drag & Drop:

```csharp
public class InventorySlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public Image iconImage;
    public TextMeshProUGUI amountText;
    public Image backgroundImage;
    public int slotIndex;

    private GameObject draggedIcon;     // Copia visual durante drag
    private CanvasGroup canvasGroup;    // Para transparencia

    public void UpdateSlot(InventorySlot slot)
    {
        // Actualizar icono, cantidad, visibilidad
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Detectar slot destino y solicitar swap
        InventorySlotUI targetSlot = ...;
        inventoryUI.SwapSlots(slotIndex, targetSlot.slotIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Click derecho para usar item
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            inventoryUI.UseItem(slotIndex);
        }
    }
}
```

#### 6. ItemTester.cs (Testing)

**Ubicación:** `Assets/_Game/Scripts/UI/ItemTester.cs`

Script de utilidad para añadir items durante testing:

```csharp
public class ItemTester : MonoBehaviour
{
    public int testItemID = 1;
    public int testAmount = 1;
    public KeyCode addItemKey = KeyCode.T;

    public void AddHealthPotion() => AddItemByID(1);
    public void AddManaPotion() => AddItemByID(2);
    public void AddIronSword() => AddItemByID(3);
    // etc...
}
```

**Requiere namespace:** `using Game.Player;`

#### 7. ItemCreator.cs (Editor Script)

**Ubicación:** `Assets/_Game/Scripts/Editor/ItemCreator.cs`

Editor window para crear items desde Unity:

```csharp
public class ItemCreator : EditorWindow
{
    [MenuItem("MMO/Create Default Items")]
    public static void CreateDefaultItems()
    {
        // Crea 5 items de ejemplo programáticamente
    }
}
```

### Items Creados (ScriptableObjects)

**Ubicación:** `Assets/_Game/ScriptableObjects/Items/`

| ID | Nombre | Tipo | Stackable | Max Stack | Efecto |
|----|--------|------|-----------|-----------|--------|
| 1 | Poción de Salud | Consumable | ✅ | 20 | +50 HP (va al inventario) |
| 2 | Poción de Maná | Consumable | ✅ | 20 | +30 Mana (va al inventario) |
| 3 | Espada de Hierro | Weapon | ❌ | 1 | +10 Damage (va al inventario) |
| 4 | Escudo de Madera | Armor | ❌ | 1 | +5 Armor (va al inventario) |
| 5 | Moneda de Oro | **Currency** | ✅ | 999 | **+1 Gold (NO va al inventario, suma directo a PlayerStats.gold)** |

**Creación:** Usar menú `MMO > Create Default Items` en Unity.

**IMPORTANTE - Sistema de Currency:**
- Los items de tipo **Currency** NO ocupan espacio en el inventario
- Se suman automáticamente al stat `gold` del jugador (PlayerStats)
- Fórmula: `gold += itemData.goldValue * amount`
- Ejemplo: Recoger 50 monedas de oro → `gold += 1 * 50 = +50 oro`

### Configuración del UI

#### Prefab: InventorySlot.prefab

**Ubicación:** `Assets/_Game/Prefabs/UI/InventorySlot.prefab`

```
InventorySlot (80x80)
├── Components:
│   ├── Image (background, gris oscuro)
│   ├── Canvas Group (para drag transparencia)
│   └── Inventory Slot UI (script)
├── Icon (hijo)
│   └── Image (sprite del item, desactivado por defecto)
└── AmountText (hijo)
    └── TextMeshPro (cantidad del stack)
```

#### UI en GameWorld

**Jerarquía:**
```
GameWorldCanvas
├── InventoryPanel (600x400, desactivado por defecto)
│   └── SlotsContainer (Grid Layout Group)
│       └── (slots se crean dinámicamente)
├── TestingPanel (panel de testing, top-right)
│   ├── Title ("TESTING")
│   ├── BtnHealthPotion
│   ├── BtnManaPotion
│   ├── BtnIronSword
│   ├── BtnWoodenShield
│   └── BtnGoldCoin
└── Components:
    ├── Inventory UI (manager)
    └── Item Tester (testing script)
```

**Referencias en InventoryUI:**
- Inventory Panel: `InventoryPanel`
- Slot Prefab: `InventorySlot.prefab`
- Slots Container: `SlotsContainer`
- Main Canvas: `GameWorldCanvas`

### Sincronización en Red

**Flujo de añadir item (normal):**

1. **Cliente:** Usuario presiona botón "Add Health Potion"
2. **Cliente:** `ItemTester.AddHealthPotion()` llama `playerInventory.CmdAddItem(1, 1)`
3. **Servidor:** Command ejecuta:
   - Valida itemID y cantidad
   - Busca ItemData en ItemDatabase
   - **Verifica si es Currency** → Si NO, continúa
   - Si es apilable, busca stack existente con espacio
   - Si no, busca slot vacío
   - Modifica `SyncList<InventorySlot>`
4. **Mirror:** Detecta cambio en SyncList y sincroniza a TODOS los clientes
5. **Clientes:** Callback `OnInventoryUpdated` dispara evento `OnInventoryChanged`
6. **UI:** `InventoryUI.RefreshUI()` actualiza todos los slots visuales

**Flujo de añadir Currency (oro):**

1. **Cliente:** Usuario presiona botón "Add Gold Coin" o recoge oro del suelo
2. **Cliente:** `ItemTester.AddGoldCoin()` llama `playerInventory.CmdAddItem(5, 50)` (50 monedas)
3. **Servidor:** Command ejecuta:
   - Valida itemID y cantidad
   - Busca ItemData en ItemDatabase
   - **Detecta que es ItemType.Currency**
   - Calcula: `goldToAdd = itemData.goldValue * amount` → `1 * 50 = 50`
   - Suma directo a PlayerStats: `stats.gold += 50`
   - Llama `RpcShowGoldPickup(50)` para feedback visual
   - **RETURN** (NO añade al inventario)
4. **Mirror:** Sincroniza el SyncVar `gold` de PlayerStats a todos los clientes
5. **Clientes:** Hook `OnGoldChanged` actualiza el HUD
6. **UI:** `PlayerHUD` muestra el nuevo valor de oro, inventario NO cambia
7. **ClientRpc:** Todos los clientes ejecutan `RpcShowGoldPickup(50)` mostrando "+50 oro recogido"

**Flujo de Drag & Drop:**

1. **Cliente:** Usuario arrastra Item A sobre Slot B
2. **Cliente:** `InventorySlotUI.OnEndDrag()` llama `inventoryUI.SwapSlots(indexA, indexB)`
3. **Cliente:** `InventoryUI.SwapSlots()` llama `playerInventory.CmdSwapItems(indexA, indexB)`
4. **Servidor:** Valida índices y ejecuta swap en SyncList
5. **Mirror:** Sincroniza cambios
6. **UI:** Se actualiza automáticamente

**Flujo de usar item:**

1. **Cliente:** Click derecho en poción
2. **Cliente:** `InventorySlotUI.OnPointerClick()` llama `inventoryUI.UseItem(slotIndex)`
3. **Cliente:** `InventoryUI.UseItem()` llama `playerInventory.CmdUseItem(slotIndex)`
4. **Servidor:**
   - Valida que sea consumible
   - Obtiene stats del jugador (`PlayerStats`)
   - Restaura HP/Mana
   - Reduce cantidad del item (llama `CmdRemoveItem`)
5. **ClientRpc:** Reproduce efecto visual/sonido
6. **Mirror:** Sincroniza cambios de HP/Mana y cantidad de item

### Controles

- **Tecla I:** Abrir/cerrar inventario
- **Tecla T:** Añadir item de prueba (configurado en ItemTester)
- **Drag & Drop:** Arrastrar items entre slots
- **Click Derecho:** Usar consumible (pociones)
- **Botones UI Testing:**
  - "Add Health Potion" → Añade poción al inventario
  - "Add Mana Potion" → Añade poción al inventario
  - "Add Iron Sword" → Añade espada al inventario
  - "Add Wooden Shield" → Añade escudo al inventario
  - **"Add Gold Coin"** → **Suma oro directo a PlayerStats.gold (NO va al inventario)**

### Problemas Comunes y Soluciones

#### Error: "CmdAddItem cannot have optional parameters"

**Causa:** Mirror no permite parámetros opcionales en Commands

**Solución:**
```csharp
// ❌ MAL
[Command]
public void CmdAddItem(int itemID, int amount = 1)

// ✅ BIEN
[Command]
public void CmdAddItem(int itemID, int amount)
```

#### Error: "PlayerController could not be found"

**Causa:** Falta importar namespace en scripts de UI

**Solución:** Añadir al inicio del script:
```csharp
using Game.Player;
```

#### Error: "ItemDatabase.Instance es null"

**Causa:** No existe GameObject ItemDatabase en GameWorld

**Solución:**
1. Crear GameObject vacío llamado `ItemDatabase`
2. Añadir componente `ItemDatabase`
3. Asignar los 5 items a la lista `allItems`

#### Problema: Los slots no se crean

**Causa:** Referencias no asignadas en InventoryUI

**Solución:** Verificar en Inspector que:
- `slotPrefab` apunte a `InventorySlot.prefab`
- `slotsContainer` apunte al objeto con GridLayoutGroup
- `mainCanvas` apunte a GameWorldCanvas

#### Problema: Drag & Drop no funciona

**Causa:** Falta componente Canvas Group en el prefab

**Solución:** Añadir `Canvas Group` al root de `InventorySlot.prefab`

#### Problema: Items no tienen iconos

**Causa:** Los ScriptableObjects no tienen sprites asignados

**Solución:** Es normal en esta fase. Los iconos se pueden añadir después en el Inspector de cada ItemData.

#### Problema: El oro (GoldCoin) aparece en el inventario en lugar de sumarse al stat

**Causa:** El ScriptableObject GoldCoin tiene `itemType: 5` (Misc) en lugar de `itemType: 5` (Currency)

**Solución:**
1. Abre `Assets/_Game/ScriptableObjects/Items/GoldCoin.asset` en el Inspector
2. Cambia **Item Type** de `Misc` a `Currency`
3. O elimina todos los items y vuelve a ejecutar `MMO > Create Default Items`

**Nota:** El valor numérico de `Currency` en el enum es 5, igual que Misc si no actualizaste el código. Verifica que el enum `ItemType` en `ItemData.cs` tenga `Currency` antes de `Misc`.

#### Problema: Añadir oro no suma al stat gold

**Causa:** Falta referencia al componente PlayerStats en PlayerInventory

**Solución:** Verificar que el prefab Player tenga ambos componentes:
- `PlayerStats` (debe existir)
- `PlayerInventory` (debe existir)

Ambos deben estar en el mismo GameObject para que `GetComponent<PlayerStats>()` funcione.

---

---

## ⚔️ SISTEMA DE COMBATE (FASE 5)

### Arquitectura de Combate

El sistema de combate es **Server-Authoritative** (autoridad del servidor) con feedback inmediato en el cliente para la UI.

**Componentes Clave:**
1. **PlayerCombat.cs**: Lógica central de habilidades, cooldowns y validación.
2. **TargetingSystem.cs**: Selección de objetivos (Raycast).
3. **AbilityData.cs**: Define las habilidades (ScriptableObject).
4. **TargetFrameUI**: Muestra la vida y datos del objetivo seleccionado.
5. **AbilityBarUI**: Barra de habilidades con cooldowns visuales.

### Scripts del Sistema

#### 1. AbilityData.cs (ScriptableObject)
Define las propiedades estáticas de una habilidad:
- `abilityName`: Nombre
- `manaCost`: Coste de maná
- `cooldownTime`: Tiempo de recarga
- `range`: Rango máximo
- `damage`: Daño base (o curación)
- `abilityType`: Damage, Heal, Buff
- `icon`: Sprite para la UI

#### 2. PlayerCombat.cs (NetworkBehaviour)
Maneja la ejecución de habilidades.

**SyncVars:**
- `abilities`: `SyncList<AbilityData>` que sincroniza qué habilidades tiene el jugador.

**Flujo de Uso de Habilidad:**
1. **Cliente:** Presiona tecla (1-4) o click en botón
2. **Cliente:** `TryUseAbility(index)` valida localmente:
   - Cooldown (diccionario local)
   - Maná suficiente
   - Objetivo seleccionado (`TargetingSystem`)
   - **Validación de Zona Segura** (no atacar desde/hacia zona segura)
3. **Cliente:** Envía Command `CmdUseAbility(index, targetNetIdentity)`
4. **Servidor:** `ValidateAbilityServer()` verifica todo nuevamente (Anti-Cheat):
   - Cooldown, Maná, Distancia, Line of Sight (Raycast)
   - **Validación de Zona Segura** (autoridad final)
5. **Servidor:** Ejecuta la habilidad:
   - Resta maná
   - Aplica daño/curación al objetivo (`target.GetComponent<PlayerStats>().TakeDamage()`)
   - Inicia Cooldown (`StartCooldown`)
6. **Servidor:** Llama `RpcStartCooldown` y `RpcPlayAbilityEffect`
7. **Clientes:**
   - `RpcStartCooldown`: Inicia animación gris en UI
   - `RpcPlayAbilityEffect`: Muestra partículas/sonidos

#### 3. TargetingSystem.cs
Maneja la selección de objetivos con el mouse.

**Lógica de Selección:**
- Usa `Camera.main` (o referencia pasada por `PlayerController`) para lanzar Raycast.
- Filtra por LayerMask (Players, Enemies).
- `GetComponentInParent<NetworkIdentity>()` para encontrar al jugador raíz.
- Evita seleccionarse a sí mismo.
- Evento `OnTargetChanged` notifica a la UI.

**Corrección de Cámara:**
El `PlayerController` pasa la referencia de la cámara inmeditamente después de instanciarla (ya que no es hija del jugador) mediante `targetingSystem.SetCamera()`.

### UI de Combate

#### TargetFrameUI
Panel que aparece al seleccionar un objetivo.
- Muestra Nombre, Clase y Barra de Vida.
- Se conecta automáticamente mediante `TargetingUIConnector` (añadido dinámicamente si falta).

#### AbilityBarUI
Genera botones dinámicamente según las habilidades del jugador.
- **Auto-Configuración:** Busca referencias (`Icon`, `CooldownText`) aunque no estén asignadas en prefab.
- **Sincronización:** Escucha cambios en `PlayerCombat.abilities` (SyncList).

### Validaciones de Seguridad (Safe Zones)
El combate está prohibido en zonas seguras.
- **Cliente:** Bloquea el intento de lanzar habilidad y muestra advertencia.
- **Servidor:** Rechaza el comando si el atacante O el objetivo están en zona segura (`ZoneHandler.isSafeZone`).

---

## 💀 SISTEMA DE MUERTE Y LOOT (FASE 6)

### Arquitectura de Muerte

El sistema maneja la muerte del jugador, el dropeo de items y el respawn con sincronización de red.

**Flujo de Muerte (Server-Side en `PlayerStats.cs`):**
1. `TakeDamage` reduce la vida a <= 0.
2. Se llama a `Die()` (Server).
3. **Inventory Drop:**
    - Se llama `PlayerInventory.ClearInventory()` para vaciar inventario y obtener items.
    - Se instancia prefab `LootBag` en la posición de muerte.
    - Se inicializa `LootBag` con los items dropeados.
    - `NetworkServer.Spawn(lootBag)` para sincronizar en red.
4. **Respawn:**
    - Se busca `NetworkManager.singleton.GetStartPosition()`.
    - Se mueve el transform del jugador al spawn.
    - **Corrección Client Authority:** Se llama `TargetRespawn` (TargetRpc) para ordenar al cliente cambiar su posición inmediata (bypass de predicción).
5. **Reset Stats:**
    - HP y Mana se restauran al máximo.

### Sistema de Loot

**Componentes:**
1. **LootBag.cs (NetworkBehaviour):**
    - Contiene `SyncList<InventorySlot> items`.
    - `CmdTakeItem(index)`: Permite a un jugador reclamar un item específico. Valida distancia.
    - Auto-destrucción cuando se vacía (`NetworkServer.Destroy`).

2. **LootUI.cs (Manager):**
    - Muestra el contenido de la bolsa actual.
    - Se suscribe a `LootBag.items.Callback` para actualizaciones en tiempo real.
    - Gestiona el click derecho en items para lootear.

**Interacción (PlayerController.cs):**
- Detección de **Click Derecho** del mouse.
- Raycast busca objetos con componente `LootBag`.
- Si encuentra bolsa:
    - Busca `LootUI` en escena (incluso si está inactivo con `FindFirstObjectByType`).
    - Llama `LootUI.Open(lootBag)`.

**UI de Loot (LootUI):**
- Reutiliza `InventorySlotUI` para mostrar items.
- Configura `OnRightClickAction` en los slots para llamar `CmdTakeItem` en lugar de usar el item.
- Ventana se cierra automáticamente si la bolsa se destruye o el jugador se aleja.

---

## 🤖 SISTEMA DE NPCs E IA (FASE 7)

### Arquitectura de IA

El sistema de IA es **Server-Authoritative**. Los clientes solo visualizan la posición sincronizada.

**Componentes Clave:**
1.  **NpcData.cs:** ScriptableObject para configurar stats y loot.
2.  **EnemyController.cs:** Mente de la IA (Server).
3.  **NpcStats.cs:** Vida, daño y generación de loot.
4.  **IEntityStats:** Interface común para Players y NPCs.

### Scripts del Sistema

#### 1. NpcData.cs (ScriptableObject)
Define propiedades del enemigo:
- `npcName`: Nombre visual.
- `maxHP`, `damage`, `moveSpeed`.
- `aggroRange`: Distancia de detección.
- `attackRange`: Distancia de ataque.
- `rewards`: XP y Oro (min/max).
- `lootTable`: Lista de items con probablidad de drop.

#### 2. EnemyController.cs (NetworkBehaviour)
Maneja el comportamiento:
- **NavMeshAgent:** Calcula rutas en el Servidor.
- **Estados:** Idle, Chase, Attack.
- **Optimización Cliente:** Desactiva `NavMeshAgent` en clientes para evitar predicciones locales erróneas (empujones).
- **Física:** Fuerza `Physics.IgnoreLayerCollision(Player, Enemy)` para movimiento fluido "estilo WoW".

#### 3. NpcStats.cs
Implementa `IEntityStats`.
- **TakeDamage:** Recibe daño y registra al atacante (`lastAttacker`).
- **Die:**
  - Otorga XP directa al `lastAttacker`.
  - Genera `LootBag` con Oro y Items según `NpcData`.
  - Spawnea la bolsa en red (`NetworkServer.Spawn`).
  - Destruye al NPC.

#### 4. IEntityStats.cs (Interface)
Permite al sistema de combate atacar genéricamente:
```csharp
public interface IEntityStats
{
    string EntityName { get; }
    int CurrentHealth { get; }
    int MaxHealth { get; }
    void TakeDamage(int damage, PlayerStats attacker);
}
```
`PlayerStats` y `NpcStats` implementan esta interface.

### Configuración de Física y Movimiento

**Problema:** NPCs empujando jugadores.
**Solución:** Matriz de Colisiones + Ajuste de IA.

1.  **Layers:** `Player` (6) y `Enemy` (7).
2.  **Matriz:** Desactivar colisión entre Layer 6 y 7.
3.  **NavMeshAgent:**
    - `StoppingDistance = AttackRange` (frenar ANTES de chocar).
    - `isStopped = true` durante el ataque.
    - Desactivado en clientes (`OnStartClient`).

---

## 🚀 MEJORAS DE IA Y TARGETING (FASE 7.5)

Se han implementado mejoras significativas en la inteligencia artificial y experiencia de usuario.

### 1. Sistema de Leash (Correa)
Para evitar que los enemigos persigan infinitamente:
- **Max Chase Distance:** Configurable en `NpcData`.
- **Comportamiento:** Si el NPC se aleja más de X metros de su punto de spawn, abandona la persecución.
- **Retorno:** Vuelve a su posición original, invulnerable, y se cura al llegar.
- **Anti-Griefing:** Si el objetivo entra en una **Zona Segura**, el NPC suelta el aggro inmediatamente.

### 2. Sistema de Spawning Automático
Nuevo script `NpcSpawner.cs`:
- Hereda de `NetworkBehaviour` (Server Only).
- **Pooling Básico:** Instancia un NPC al inicio.
- **Auto-Respawn:** Detecta cuando el NPC muere o es destruido. Espera `respawnTime` y crea uno nuevo.
- **Radio:** Spawnea en una posición aleatoria dentro de un radio configurado, ajustado al NavMesh.

### 3. Tab Targeting & Visuales
Mejoras en `TargetingSystem.cs`:
- **Tab Cycling:** Tecla `TAB` alterna entre enemigos cercanos.
- **Criterios de Selección:**
    1.  Distancia (más cercanos primero).
    2.  **Line of Sight:** Raycast para asegurar visibilidad.
    3.  **Frustum Culling:** Solo selecciona enemigos visibles en la pantalla (cámara).
- **Indicador Visual:** Prefab (círculo rojo) que aparece en los pies del objetivo seleccionado.
    - Usa `NavMesh.SamplePosition` para pegarse perfectamente al terreno irregular.

---

## 📜 SISTEMA DE QUESTS - CADENA LINEAL (FASE 8)

### Arquitectura del Sistema de Quests

El sistema de quests es **Server-Authoritative** con sincronización automática vía `SyncList`. Implementa una **cadena lineal story-driven** donde las quests se desbloquean progresivamente según el nivel del jugador.

**Características Principales:**
- 🔗 **Cadena Lineal**: Un solo NPC ofrece quests en secuencia obligatoria
- 🔒 **Bloqueo por Nivel**: Quests bloqueadas se muestran con mensaje motivacional
- ⚖️ **Auto-Balanceo XP**: Recompensas calculadas automáticamente según nivel requerido
- 💾 **Persistencia**: Historial de quests completadas (CSV en SyncVar)
- 🔄 **Validación Inteligente**: Sistema de 4 capas que previene saltos y duplicados

**Componentes Clave:**
1. **QuestData.cs:** ScriptableObject con progresión (requiredLevel, orderInChain)
2. **QuestObjective.cs:** Struct que define los objetivos (Kill, Collect, etc.)
3. **PlayerQuests.cs:** Maneja progreso, validación y persistencia
4. **QuestGiver.cs:** Lógica inteligente que determina qué quest mostrar
5. **QuestGiverUI.cs:** Panel con 4 estados (Nueva/En Progreso/Completa/Bloqueada)
6. **QuestTrackerUI.cs:** HUD flotante con progreso en tiempo real
7. **QuestLogUI.cs:** Diario detallado (tecla J)

---

### Scripts del Sistema

#### 1. QuestObjective.cs (Struct)

Define los tipos de objetivos:

```csharp
[System.Serializable]
public struct QuestObjective
{
    public ObjectiveType type;      // Kill, Collect, Talk, etc.
    public string targetName;       // Nombre del NPC/Item
    public int requiredAmount;      // Cantidad necesaria
}

public enum ObjectiveType
{
    Kill,       // Matar enemigos
    Collect,    // Recolectar items (futuro)
    Talk,       // Hablar con NPCs (futuro)
    Explore     // Descubrir zonas (futuro)
}
```

**Nota:** MVP solo implementa `ObjectiveType.Kill`.

#### 2. QuestData.cs (ScriptableObject)

Define una quest completa con progresión lineal:

```csharp
[CreateAssetMenu(fileName = "NewQuest", menuName = "Game/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Info")]
    public string questTitle;
    [TextArea] public string questDescription;

    [Header("Chain Progression")]
    [Tooltip("Nivel mínimo requerido para aceptar esta quest")]
    public int requiredLevel = 1;

    [Tooltip("Orden en la cadena (0 = primera quest, 1 = segunda, etc.)")]
    public int orderInChain = 0;

    [Header("Objectives")]
    public List<QuestObjective> objectives;

    [Header("Rewards")]
    public int xpReward;
    public int goldReward;

    [Header("Auto-Balance")]
    [Tooltip("Si está activo, calcula XP automáticamente basado en requiredLevel")]
    public bool autoCalculateXP = true;

    [Tooltip("XP base por nivel para la fórmula automática")]
    public int baseXPPerLevel = 80;

    /// <summary>
    /// Calcula XP recomendada según nivel requerido
    /// Fórmula: baseXP * requiredLevel * (1 + (requiredLevel-1) * 0.1)
    /// </summary>
    public int CalculateRecommendedXP()
    {
        float multiplier = 1f + (requiredLevel - 1) * 0.1f;
        return Mathf.RoundToInt(baseXPPerLevel * requiredLevel * multiplier);
    }

    private void OnValidate()
    {
        if (autoCalculateXP)
        {
            xpReward = CalculateRecommendedXP();
        }
    }
}
```

**Ubicación:** `Assets/Resources/Quests/` (CRÍTICO para el sistema de serialización)

**Campos Nuevos Explicados:**

| Campo | Tipo | Propósito |
|-------|------|-----------|
| `requiredLevel` | int | Nivel mínimo para aceptar la quest |
| `orderInChain` | int | Posición en la secuencia (0, 1, 2, 3...) |
| `autoCalculateXP` | bool | Activa cálculo automático de XP |
| `baseXPPerLevel` | int | Base para la fórmula (default: 80) |

**Fórmula de Balanceo XP:**
```
XP = baseXPPerLevel * requiredLevel * (1 + (requiredLevel - 1) * 0.1)

Ejemplos:
- Nivel 1: 80 * 1 * 1.0 = 80 XP
- Nivel 3: 80 * 3 * 1.2 = 288 XP
- Nivel 5: 80 * 5 * 1.4 = 560 XP
- Nivel 8: 80 * 8 * 1.7 = 1088 XP
```

**OnValidate:** Se ejecuta automáticamente en el editor cuando cambias valores. Si `autoCalculateXP` está activo, recalcula `xpReward` cada vez que modificas `requiredLevel`.

#### 3. QuestStatus (Struct en PlayerQuests.cs)

**CRÍTICO:** Este struct tiene un diseño especial para networking.

```csharp
[System.Serializable]
public struct QuestStatus
{
    // NO guardamos el ScriptableObject directamente (Mirror no lo serializa)
    public string questName;      // Nombre del asset (ID)
    public int currentAmount;     // Progreso actual
    public bool isCompleted;      // Flag de completitud (futuro)

    public QuestStatus(QuestData questData)
    {
        questName = questData.name;
        currentAmount = 0;
        isCompleted = false;
    }

    // Método helper para obtener el SO desde Resources
    public QuestData GetQuestData()
    {
        string localQuestName = questName;  // Copia local (requisito de structs)
        QuestData[] allQuests = Resources.LoadAll<QuestData>("Quests");
        return System.Array.Find(allQuests, q => q.name == localQuestName);
    }
}
```

**Por Qué Este Diseño:**
- Mirror **NO puede serializar** ScriptableObjects en SyncLists.
- Solución: Guardar el **nombre del asset** (string) y cargarlo desde Resources cuando se necesite.
- Patrón estándar en MMOs: "Serializar ID, Cargar Asset".

#### 4. PlayerQuests.cs (NetworkBehaviour)

Maneja la lista de quests activas, validación de cadena y persistencia.

**SyncVars y SyncList:**
```csharp
public readonly SyncList<QuestStatus> activeQuests = new SyncList<QuestStatus>();

// NUEVO: Persistencia de quests completadas (separadas por comas)
[SyncVar]
public string completedQuestsCSV = "";  // "Quest1_Tutorial,Quest2_VillageInDanger"

// NUEVO: Índice de progreso en la cadena principal
[SyncVar]
public int currentChainIndex = 0;
```

**Callback de Sincronización:**
```csharp
private void Awake()
{
    // CRÍTICO: Suscribirse al callback para actualizaciones automáticas
    activeQuests.Callback += OnQuestListChanged;
}

private void OnQuestListChanged(SyncList<QuestStatus>.Operation op, int index,
                                QuestStatus oldItem, QuestStatus newItem)
{
    if (!isLocalPlayer) return;
    UpdateUI();  // Actualiza Tracker y Log automáticamente
}
```

**Métodos Clave:**

| Método | Tipo | Descripción |
|--------|------|-------------|
| `CanAcceptQuest(quest, out reason)` | **NO [Server]** | **Validación de 4 capas** (nivel, orden, duplicados, historial). Debe ejecutarse en clientes para UI. |
| `IsQuestCompleted(questName)` | Local | Consulta el historial CSV. |
| `MarkQuestCompleted(questName)` | `[Server]` | Añade quest al historial CSV. |
| `GetNextAvailableQuest()` | Local | Obtiene la siguiente quest que el jugador puede aceptar. |
| `GetNextBlockedQuest()` | Local | Obtiene la siguiente quest bloqueada por nivel. |
| `ServerOnEnemyKilled(npcName)` | `[Server]` | Llamado por `NpcStats` al morir. Incrementa progreso. |
| `CmdAcceptQuest(questName)` | `[Command]` | Cliente pide aceptar una quest. **Usa validación**. |
| `ServerAcceptQuest(quest)` | `[Server]` | Añade quest a la SyncList. |
| `CmdCompleteQuest(index)` | `[Command]` | Cliente pide entregar una quest. **Marca en historial**. |
| `UpdateUI()` | Local | Actualiza Tracker y QuestLog. |

**CRÍTICO - Validación Central (CanAcceptQuest):**

Sistema de **4 capas de validación** que previene exploits y mantiene coherencia:

```csharp
public bool CanAcceptQuest(QuestData quest, out string reason)
{
    // CAPA 1: Validar que la quest existe
    if (quest == null)
    {
        reason = "Quest inválida";
        return false;
    }

    // CAPA 2: No duplicados (quest ya activa)
    if (activeQuests.Any(q => q.questName == quest.name))
    {
        reason = "Ya tienes esta quest activa";
        return false;
    }

    // CAPA 3: No repetir quests completadas
    if (IsQuestCompleted(quest.name))
    {
        reason = "Ya completaste esta quest";
        return false;
    }

    // CAPA 4: Verificar nivel requerido
    if (playerStats.level < quest.requiredLevel)
    {
        reason = $"Requiere nivel {quest.requiredLevel}";
        return false;
    }

    // CAPA 5: Verificar orden en cadena (quest previa completada)
    if (quest.orderInChain > 0)
    {
        QuestData[] allQuests = Resources.LoadAll<QuestData>("Quests");
        QuestData previousQuest = System.Array.Find(allQuests,
            q => q.orderInChain == quest.orderInChain - 1);

        if (previousQuest != null && !IsQuestCompleted(previousQuest.name))
        {
            reason = $"Primero debes completar: {previousQuest.questTitle}";
            return false;
        }
    }

    reason = "";
    return true;
}
```

**POR QUÉ NO TIENE [Server]:** Este método se ejecuta en CLIENTES para determinar qué mostrar en el UI (botones, mensajes, etc.). Solo LEE SyncVars (que ya están sincronizadas), no modifica estado del servidor. El servidor valida nuevamente en `CmdAcceptQuest` como capa de seguridad.

**Flujo de Progreso:**

```
1. NPC muere → NpcStats.Die() llama lastAttacker.GetComponent<PlayerQuests>().ServerOnEnemyKilled()
2. Servidor: Loop sobre activeQuests, busca match con npcName
3. Servidor: Incrementa currentAmount, actualiza SyncList[index]
4. Mirror: Detecta cambio en SyncList, sincroniza a todos los clientes
5. Cliente: Callback OnQuestListChanged() se ejecuta automáticamente
6. Cliente: UpdateUI() actualiza Tracker y QuestLog
```

**Validaciones:**
- Comparación de nombres **case-insensitive** con `Trim` (tolerante a errores de setup).
- Prevención de duplicados por nombre de quest.
- **Validación de cadena lineal**: No puedes saltar quests ni repetir completadas.
- **Anti-Cheat**: Cliente valida para UX, servidor valida para seguridad.

#### 5. QuestGiver.cs (MonoBehaviour + IInteractable)

Componente inteligente que determina **automáticamente** qué quest mostrar según el progreso del jugador.

```csharp
public class QuestGiver : MonoBehaviour, IInteractable
{
    [Header("Quest Chain")]
    [Tooltip("Lista de quests en orden. El NPC determinará cuál mostrar según progreso del jugador")]
    public List<QuestData> questChain;

    [Header("NPC Info")]
    public string npcName = "Guardián del Bosque";
    [TextArea]
    public string greetingText = "¡Aventurero! Tengo tareas para ti.";

    public string InteractionPrompt => "Talk";

    public void Interact(GameObject player)
    {
        PlayerQuests playerQuests = player.GetComponent<PlayerQuests>();
        if (playerQuests == null) return;

        // Determinar qué quest mostrar
        QuestData questToShow = DetermineQuestToShow(playerQuests);
        QuestData blockedQuest = null;

        if (questToShow == null)
        {
            // Si no hay quest disponible, buscar la siguiente bloqueada
            blockedQuest = playerQuests.GetNextBlockedQuest();
        }

        // Abrir UI
        QuestGiverUI ui = FindFirstObjectByType<QuestGiverUI>(FindObjectsInactive.Include);
        if (ui == null) return;

        // SIEMPRE abrir el diálogo, con quest, bloqueada, o mensaje de fin
        if (questToShow != null)
        {
            ui.Open(this, questToShow, player, false); // false = no bloqueada
        }
        else if (blockedQuest != null)
        {
            ui.Open(this, blockedQuest, player, true); // true = bloqueada
        }
        else
        {
            // No hay más quests - mostrar mensaje de fin
            ui.OpenNoQuests(this, player);
        }
    }

    /// <summary>
    /// Determina cuál quest debe mostrar el NPC según el estado del jugador.
    /// Prioridad: Quest activa > Quest nueva disponible > null
    /// </summary>
    private QuestData DetermineQuestToShow(PlayerQuests playerQuests)
    {
        // Ordenar quests por orderInChain
        var sortedChain = questChain.OrderBy(q => q.orderInChain).ToList();

        foreach (var quest in sortedChain)
        {
            if (quest == null) continue;

            string questID = quest.name;

            // PRIORIDAD 1: Quest activa (completa o en progreso)
            int activeIndex = GetActiveQuestIndex(playerQuests, questID);
            if (activeIndex != -1)
            {
                return quest; // Mostrar para entregar o ver progreso
            }

            // PRIORIDAD 2: Quest nueva que puede aceptar
            if (playerQuests.CanAcceptQuest(quest, out string reason))
            {
                return quest;
            }
        }

        // No hay quest disponible
        return null;
    }

    /// <summary>
    /// Helper: Busca el índice de una quest activa por nombre
    /// </summary>
    private int GetActiveQuestIndex(PlayerQuests pq, string questName)
    {
        for (int i = 0; i < pq.activeQuests.Count; i++)
        {
            if (pq.activeQuests[i].questName == questName)
                return i;
        }
        return -1;
    }
}
```

**Lógica de Inteligencia:**

El QuestGiver **NO ofrece ciegamente quests**, sino que analiza el estado del jugador:

1. **Prioridad 1 (Activa)**: Si el jugador ya tiene una quest de esta cadena activa, mostrarla (para recordatorio o entrega).
2. **Prioridad 2 (Nueva)**: Si el jugador puede aceptar una nueva quest (nivel correcto, quest previa completa), ofrecerla.
3. **Prioridad 3 (Bloqueada)**: Si no hay quest disponible pero hay una bloqueada por nivel, mostrarla con mensaje motivacional.
4. **Prioridad 4 (Fin)**: Si no hay más quests, mostrar mensaje de "No hay más quests disponibles".

**Ventaja:** Un solo NPC puede manejar toda la cadena de quests. No necesitas crear múltiples NPCs o scripts complejos.

**Configuración:**
- Añadir componente a un GameObject NPC.
- Asignar **todas las quests de la cadena** en `questChain` (en cualquier orden, el script las ordena).
- Requiere Collider para detección de clicks.
- El NPC automáticamente decide qué mostrar según el jugador.

---

### UI del Sistema de Quests

#### QuestGiverUI.cs

Panel de interacción con **4 modos dinámicos** según el estado de la quest:

**Modo 1: Ofrecer Quest Nueva**
- Estado: Jugador NO tiene la quest y cumple requisitos.
- UI: Título, Descripción, Recompensas, Status: "Nueva Quest" (verde).
- Botones: **Aceptar** / **Cerrar**.

**Modo 2: Recordatorio (En Progreso)**
- Estado: Jugador tiene la quest pero NO está completa.
- UI: Título, Descripción, Status: "En Progreso (2/3)" (naranja).
- Botones: **Cerrar**.

**Modo 3: Entregar Quest**
- Estado: Jugador tiene la quest completa (3/3).
- UI: Título, Descripción, Status: "¡Completa!" (verde).
- Botones: **Completar** / **Cerrar**.

**Modo 4: Quest Bloqueada (NUEVO)**
- Estado: Jugador NO cumple nivel requerido.
- UI: Título, Descripción, Recompensas, Status: "Bloqueada - Requiere nivel X" (rojo).
- Mensaje adicional: "Vuelve cuando seas nivel X" (si blockedReasonText está asignado).
- Botones: **Cerrar** (solo).

**Modo 5: Sin Quests (NUEVO)**
- Estado: Jugador completó toda la cadena.
- UI: Nombre del NPC, Mensaje: "Has completado todas las quests..."
- Botones: **Cerrar**.

**Lógica de Detección de Estado:**

```csharp
/// <summary>
/// Abre el panel y decide qué mostrar según el estado de la quest
/// </summary>
/// <param name="blocked">True si la quest está bloqueada por nivel</param>
public void Open(QuestGiver npc, QuestData quest, GameObject player, bool blocked)
{
    currentNpc = npc;
    currentQuest = quest;
    isBlocked = blocked;

    playerQuests = player.GetComponent<PlayerQuests>();
    if (playerQuests == null) return;

    // Datos básicos de la quest
    titleText.text = quest.questTitle;
    descriptionText.text = quest.questDescription;
    rewardsText.text = $"<color=yellow>Recompensas:</color>\n{quest.xpReward} XP\n{quest.goldReward} Oro";

    // Determinar estado y botones
    if (blocked)
    {
        ShowBlockedState(quest);
    }
    else
    {
        ShowNormalState(quest);
    }

    panel.SetActive(true);
}

/// <summary>
/// Muestra el estado de quest bloqueada por nivel
/// </summary>
private void ShowBlockedState(QuestData quest)
{
    statusText.text = $"<color=red>Bloqueada - Requiere nivel {quest.requiredLevel}</color>";

    if (blockedReasonText != null)
    {
        blockedReasonText.text = $"Vuelve cuando seas nivel {quest.requiredLevel}";
        blockedReasonText.gameObject.SetActive(true);
    }

    // Solo botón cerrar
    acceptButton.SetActive(false);
    declineButton.SetActive(false);
    completeButton.SetActive(false);
    closeButton.SetActive(true);
}

/// <summary>
/// Muestra el estado normal de quest (Nueva, En Progreso, Completa)
/// </summary>
private void ShowNormalState(QuestData quest)
{
    // Ocultar mensaje de bloqueo si existe
    if (blockedReasonText != null)
    {
        blockedReasonText.gameObject.SetActive(false);
    }

    // Buscar quest en SyncList LOCAL
    int questIndex = GetLocalQuestIndex(quest.name);
    bool hasQuest = questIndex != -1;
    bool isComplete = hasQuest && IsLocalQuestComplete(questIndex);

    if (isComplete)
    {
        // CASO 1: Quest completa - Mostrar botón de entregar
        statusText.text = "<color=green>¡Completa!</color>";
        acceptButton.SetActive(false);
        declineButton.SetActive(false);
        completeButton.SetActive(true);
        closeButton.SetActive(true);
    }
    else if (hasQuest)
    {
        // CASO 2: Quest en progreso - Mostrar recordatorio
        QuestStatus qs = playerQuests.activeQuests[questIndex];
        QuestData questData = qs.GetQuestData();
        int current = qs.currentAmount;
        int required = (questData != null && questData.objectives.Count > 0)
            ? questData.objectives[0].requiredAmount : 0;

        statusText.text = $"<color=orange>En Progreso ({current}/{required})</color>";
        acceptButton.SetActive(false);
        declineButton.SetActive(false);
        completeButton.SetActive(false);
        closeButton.SetActive(true);
    }
    else
    {
        // CASO 3: Quest nueva - Mostrar botón de aceptar
        statusText.text = "<color=green>Nueva Quest</color>";
        acceptButton.SetActive(true);
        declineButton.SetActive(false); // No permitir declinar en cadena lineal
        completeButton.SetActive(false);
        closeButton.SetActive(true);
    }
}

/// <summary>
/// Abre el panel cuando no hay más quests disponibles (fin de cadena)
/// </summary>
public void OpenNoQuests(QuestGiver npc, GameObject player)
{
    currentNpc = npc;
    currentQuest = null;
    isBlocked = false;

    playerQuests = player.GetComponent<PlayerQuests>();

    // Mostrar mensaje de "no más quests"
    titleText.text = npc.npcName;
    descriptionText.text = "Has completado todas las quests que tengo para ti por ahora. ¡Sigue entrenando y vuelve pronto, aventurero!";
    rewardsText.text = "";
    statusText.text = "<color=gray>Sin quests disponibles</color>";

    // Ocultar mensaje de bloqueo si existe
    if (blockedReasonText != null)
    {
        blockedReasonText.gameObject.SetActive(false);
    }

    // Solo botón cerrar
    acceptButton.SetActive(false);
    declineButton.SetActive(false);
    completeButton.SetActive(false);
    closeButton.SetActive(true);

    panel.SetActive(true);
}
```

**IMPORTANTE:** La lógica está en el UI (lado cliente), NO en el QuestGiver. Esto permite que funcione idénticamente en Host y Cliente. El parámetro `blocked` es pasado por QuestGiver después de evaluar el estado.

#### QuestTrackerUI.cs

HUD flotante (derecha de la pantalla) que muestra progreso en tiempo real.

```csharp
public void UpdateTracker(QuestStatus[] activeQuests)
{
    StringBuilder sb = new StringBuilder();
    sb.AppendLine("<b>QUESTS</b>");

    foreach (var q in activeQuests)
    {
        QuestData questData = q.GetQuestData();
        sb.AppendLine($"<color=orange>{questData.questTitle}</color>");
        foreach(var obj in questData.objectives)
        {
            sb.AppendLine($"- {obj.targetName}: {q.currentAmount}/{obj.requiredAmount}");
        }
    }

    trackerText.text = sb.ToString();
}
```

**Actualización:** Se ejecuta automáticamente vía `OnQuestListChanged` callback.

#### QuestLogUI.cs

Diario detallado (tecla J) que muestra todas las quests activas con descripción completa.

**Controles:**
- Tecla **J**: Abrir/cerrar diario.

**Contenido:**
- Título (naranja, negrita)
- Descripción completa
- Objetivos con progreso
- Recompensas (XP, Oro)

---

### Flujo Completo de Quest

#### A. Aceptar Quest

```
1. Cliente: Click derecho en NPC (validación de distancia: 5m)
2. Cliente: PlayerController.HandleInteraction() detecta IInteractable
3. Cliente: QuestGiver.Interact() abre QuestGiverUI
4. Cliente: QuestGiverUI.Open() lee SyncList LOCAL, decide mostrar "Nueva Quest"
5. Usuario: Click en "Aceptar"
6. Cliente: QuestGiverUI.OnAcceptButton() llama playerQuests.CmdAcceptQuest(questName)
7. Servidor: CmdAcceptQuest() carga QuestData desde Resources
8. Servidor: ServerAcceptQuest() añade QuestStatus a SyncList
9. Mirror: Sincroniza SyncList a todos los clientes
10. Clientes: Callback OnQuestListChanged() actualiza UI automáticamente
```

#### B. Progreso de Quest (Matar Enemigos)

```
1. Cliente: Ataca enemigo con habilidad
2. Servidor: PlayerCombat valida y aplica daño
3. Servidor: NpcStats.TakeDamage() registra lastAttacker
4. Servidor: NpcStats.Die() llama lastAttacker.PlayerQuests.ServerOnEnemyKilled(npcName)
5. Servidor: ServerOnEnemyKilled() loop sobre activeQuests
6. Servidor: Encuentra match con npcName (case-insensitive)
7. Servidor: Incrementa qs.currentAmount, actualiza activeQuests[i]
8. Mirror: Sincroniza cambio en SyncList
9. Cliente: Callback OnQuestListChanged() se ejecuta
10. Cliente: UpdateUI() actualiza Tracker ("2/3") y QuestLog
```

#### C. Entregar Quest

```
1. Cliente: Quest en 3/3, click derecho en NPC
2. Cliente: QuestGiverUI.Open() detecta isComplete = true
3. Cliente: UI muestra "¡Completa!" con botón "Completar"
4. Usuario: Click en "Completar"
5. Cliente: QuestGiverUI.OnCompleteButton() llama playerQuests.CmdCompleteQuest(questIndex)
6. Servidor: CmdCompleteQuest() valida progreso
7. Servidor: Otorga recompensas (playerStats.AddXP(), AddGold())
8. Servidor: Elimina quest de SyncList (RemoveAt)
9. Mirror: Sincroniza eliminación
10. Clientes: Callback actualiza UI, quest desaparece del Tracker
```

---

### Sincronización en Red

#### Problema: ScriptableObjects NO se Serializan

**❌ Intento Inicial (ROTO):**
```csharp
public struct QuestStatus
{
    public QuestData data;  // ScriptableObject
    public int currentAmount;
}
```

**Resultado:**
- Host: `data` tiene referencia local → Funciona.
- Cliente: `data` llega como `null` → ROTO.

**Por Qué:** Mirror serializa structs campo por campo. Los ScriptableObjects son **referencias a archivos**, no datos primitivos. Mirror no puede enviar referencias a archivos por la red.

#### ✅ Solución: Patrón "Serializar ID, Cargar Asset"

```csharp
public struct QuestStatus
{
    public string questName;  // ✅ String se serializa perfectamente

    public QuestData GetQuestData()
    {
        // Cargar desde Resources cuando se necesite
        string localQuestName = questName;
        QuestData[] allQuests = Resources.LoadAll<QuestData>("Quests");
        return System.Array.Find(allQuests, q => q.name == localQuestName);
    }
}
```

**Flujo:**
1. Servidor: Guarda `questName = "MisionCocodrilos"`.
2. Servidor: Envía string por la red.
3. Cliente: Recibe `"MisionCocodrilos"`.
4. Cliente: Llama `GetQuestData()` cuando necesita los datos completos.
5. Cliente: Carga el SO desde su carpeta local `Resources/Quests/`.
6. Cliente: Ambos tienen el mismo asset (mismo build) → Funciona ✅.

**Patrón Usado en MMOs Comerciales:**
- WoW, FFXIV, etc. usan este mismo enfoque.
- Servidor envía IDs (números o strings).
- Cliente busca en su "base de datos local" de quests/items.

#### SyncList Callback vs TargetRpc

**❌ Enfoque Inicial (PROBLEMÁTICO):**
```csharp
[Server]
void ServerOnEnemyKilled()
{
    activeQuests[i] = qs;  // Actualiza SyncList
    TargetQuestProgressUpdated();  // Llama RPC inmediatamente
}

[TargetRpc]
void TargetQuestProgressUpdated()
{
    UpdateUI();  // Lee SyncList... ¡pero aún tiene valor viejo!
}
```

**Problema:** Mirror sincroniza SyncLists en el **siguiente frame de red**, no inmediatamente. El RPC llegaba antes de la sincronización.

**✅ Solución: Callback Automático**
```csharp
private void Awake()
{
    activeQuests.Callback += OnQuestListChanged;
}

private void OnQuestListChanged(...)
{
    if (!isLocalPlayer) return;
    UpdateUI();  // Se ejecuta DESPUÉS de que Mirror sincroniza
}
```

**Ventaja:** Mirror garantiza que el callback se ejecuta después de actualizar la SyncList.

---

### Configuración del Sistema

#### Estructura de Carpetas

```
Assets/
├── Resources/
│   └── Quests/                    # CRÍTICO: Quests deben estar aquí
│       └── MisionCocodrilos.asset
├── _Game/
    ├── Scripts/
    │   ├── Core/
    │   │   └── IInteractable.cs
    │   ├── Quests/
    │   │   ├── QuestData.cs
    │   │   ├── QuestObjective.cs
    │   │   ├── PlayerQuests.cs
    │   │   └── QuestGiver.cs
    │   └── UI/
    │       ├── QuestGiverUI.cs
    │       ├── QuestTrackerUI.cs
    │       └── QuestLogUI.cs
```

**IMPORTANTE:** Las quests deben estar en `Assets/Resources/Quests/` para que `Resources.LoadAll()` funcione.

#### Componentes del Prefab Player

```
Player.prefab
├── PlayerController
├── PlayerStats
├── PlayerInventory
├── PlayerCombat
├── TargetingSystem
├── ZoneHandler
└── PlayerQuests  ← NUEVO (Fase 8)
```

#### UI en GameWorld

```
GameWorldCanvas
├── PlayerHUDPanel
├── ZoneStatusText
├── InventoryPanel
├── LootPanel
├── TargetFrame
├── AbilityBar
├── QuestTrackerUI           ← NUEVO (derecha)
│   └── TrackerText (TMP)
├── QuestLogPanel            ← NUEVO (centro, oculto por defecto)
│   ├── Title
│   ├── ContentText (TMP)
│   └── CloseButton
└── QuestGiverPanel          ← NUEVO (centro)
    ├── TitleText (TMP)
    ├── DescriptionText (TMP)
    ├── RewardsText (TMP)
    ├── StatusText (TMP)
    ├── AcceptButton
    ├── DeclineButton
    ├── CompleteButton
    └── CloseButton
```

**Referencias en Inspector:**

**QuestTrackerUI:**
- `trackerText` → TrackerText (TMP)

**QuestLogUI:**
- `panel` → QuestLogPanel
- `contentText` → ContentText (TMP)

**QuestGiverUI:**
- `panel` → QuestGiverPanel
- `titleText` → TitleText
- `descriptionText` → DescriptionText
- `rewardsText` → RewardsText
- `statusText` → StatusText
- `acceptButton` → AcceptButton (GameObject)
- `declineButton` → DeclineButton (GameObject)
- `completeButton` → CompleteButton (GameObject)
- `closeButton` → CloseButton (GameObject)

**Callbacks de Botones:**
- AcceptButton.OnClick() → `QuestGiverUI.OnAcceptButton()`
- DeclineButton.OnClick() → `QuestGiverUI.OnDeclineButton()`
- CompleteButton.OnClick() → `QuestGiverUI.OnCompleteButton()`
- CloseButton.OnClick() → `QuestGiverUI.OnCloseButton()`

#### Ejemplo: Cadena de Quests del Proyecto

**Ubicación:** `Assets/Resources/Quests/`

El proyecto incluye 4 quests como ejemplo de cadena lineal:

**Quest 1: "El Despertar del Héroe"**
- **Archivo:** `Quest1_Tutorial.asset`
- **Required Level:** 1
- **Order in Chain:** 0
- **Auto Calculate XP:** ✅ → XP: 80
- **Gold Reward:** 10
- **Objetivo:** Matar 3 Lobos
- **Descripción:** "Bienvenido, aventurero. Los lobos salvajes están amenazando a los aldeanos. Elimina 3 lobos para demostrar tu valía."

**Quest 2: "Aldea en Peligro"**
- **Archivo:** `Quest2_VillageInDanger.asset`
- **Required Level:** 3
- **Order in Chain:** 1
- **Auto Calculate XP:** ✅ → XP: 288
- **Gold Reward:** 25
- **Objetivo:** Matar 5 Cocodrilos
- **Descripción:** "Los cocodrilos del pantano están atacando la aldea. Elimínalos antes de que causen más daño."

**Quest 3: "El Pantano Oscuro"**
- **Archivo:** `Quest3_CrocodileSwamp.asset`
- **Required Level:** 5
- **Order in Chain:** 2
- **Auto Calculate XP:** ✅ → XP: 560
- **Gold Reward:** 50
- **Objetivo:** Matar 7 Cocodrilos
- **Descripción:** "Los cocodrilos provienen de lo profundo del pantano. Adéntrate en su territorio y elimina 7 cocodrilos para reducir su población. Ten cuidado, son más peligrosos en su hábitat natural."

**Quest 4: "Las Ruinas Antiguas"**
- **Archivo:** `Quest4_AncientRuins.asset`
- **Required Level:** 8
- **Order in Chain:** 3
- **Auto Calculate XP:** ✅ → XP: 1088
- **Gold Reward:** 100
- **Objetivo:** Matar 10 Cocodrilos
- **Descripción:** "Las leyendas hablan de unas ruinas antiguas custodiadas por cocodrilos ancestrales. Solo los héroes más valientes se atreven a desafiarlos. Elimina 10 cocodrilos en las ruinas y conviértete en leyenda."

#### Crear una Nueva Quest

1. Click derecho en `Assets/Resources/Quests/`
2. Create > Game > Quest Data
3. Configurar campos básicos:
   - **Quest Title:** Nombre visible
   - **Quest Description:** Historia y contexto
4. Configurar progresión:
   - **Required Level:** Nivel mínimo (ej: 1, 3, 5, 8...)
   - **Order in Chain:** Posición secuencial (0, 1, 2, 3...)
   - **Auto Calculate XP:** ✅ (recomendado)
   - **Base XP Per Level:** 80 (default, ajustar si necesario)
   - **Gold Reward:** Configurar manualmente
5. Configurar objetivos:
   - **Objectives:** Lista con 1 elemento:
     - Type: Kill
     - Target Name: "Cocodrilo" (debe coincidir **exactamente** con `NpcData.npcName`)
     - Required Amount: 3
6. Guardar asset

7. **IMPORTANTE:** Verificar que `orderInChain` sea único y secuencial. No puede haber dos quests con el mismo orden.

8. Configurar NPC QuestGiver:
   - Crear GameObject con modelo
   - Añadir `QuestGiver` component
   - Añadir Collider (para clicks)
   - Asignar **TODAS** las quests de la cadena en `questChain` (el script las ordena automáticamente)

---

### Controles del Sistema

| Tecla/Acción | Función |
|--------------|---------|
| **J** | Abrir/cerrar QuestLog (diario) |
| **D** | **DEBUG: Imprimir estado de quests** (nivel, historial, activas, todas las disponibles) |
| **Click Derecho en NPC** | Interactuar (máx 5m de distancia) |
| **Matar Enemigo** | Progreso automático si hay quest activa |

### Debug y Herramientas de Desarrollo

#### Comando Debug (Tecla D)

Implementado en `PlayerQuests.cs`, imprime información completa del estado de quests:

```csharp
private void Update()
{
    if (!isLocalPlayer) return;

    // DEBUG: Tecla D para imprimir estado de quests
    if (Input.GetKeyDown(KeyCode.D))
    {
        DebugPrintQuestState();
    }
}

private void DebugPrintQuestState()
{
    Debug.Log("========== QUEST STATE DEBUG ==========");
    Debug.Log($"Player Level: {playerStats.level}");
    Debug.Log($"Completed Quests CSV: '{completedQuestsCSV}'");
    Debug.Log($"Current Chain Index: {currentChainIndex}");
    Debug.Log($"Active Quests Count: {activeQuests.Count}");

    for (int i = 0; i < activeQuests.Count; i++)
    {
        QuestStatus qs = activeQuests[i];
        Debug.Log($"  Active Quest {i}: {qs.questName} - Progress: {qs.currentAmount}");
    }

    // Listar todas las quests disponibles en Resources
    QuestData[] allQuests = Resources.LoadAll<QuestData>("Quests");
    Debug.Log($"Total Quests in Resources/Quests: {allQuests.Length}");
    foreach (var q in allQuests.OrderBy(q => q.orderInChain))
    {
        Debug.Log($"  Quest: {q.name} - Title: {q.questTitle} - Order: {q.orderInChain} - ReqLevel: {q.requiredLevel}");
    }

    Debug.Log("======================================");
}
```

**Cuándo Usar:**
- Para verificar que las quests se están cargando correctamente
- Para detectar duplicados de `orderInChain`
- Para ver el historial de quests completadas
- Para debugging de validaciones

**Ejemplo de Output:**
```
========== QUEST STATE DEBUG ==========
Player Level: 3
Completed Quests CSV: 'Quest1_Tutorial'
Current Chain Index: 1
Active Quests Count: 0
Total Quests in Resources/Quests: 4
  Quest: Quest1_Tutorial - Title: El Despertar del Héroe - Order: 0 - ReqLevel: 1
  Quest: Quest2_VillageInDanger - Title: Aldea en Peligro - Order: 1 - ReqLevel: 3
  Quest: Quest3_CrocodileSwamp - Title: El Pantano Oscuro - Order: 2 - ReqLevel: 5
  Quest: Quest4_AncientRuins - Title: Las Ruinas Antiguas - Order: 3 - ReqLevel: 8
======================================
```

---

### Integración con Otros Sistemas

#### Con Sistema de NPCs (NpcStats.cs)

```csharp
[Server]
private void Die()
{
    // 1. Dar XP
    lastAttacker.AddXP(xpReward);

    // 2. NUEVO: Notificar sistema de quests
    PlayerQuests quests = lastAttacker.GetComponent<PlayerQuests>();
    if (quests != null)
    {
        quests.ServerOnEnemyKilled(data.npcName);
    }

    // 3. Generar loot
    // ...
}
```

#### Con Sistema de Interacción (PlayerController.cs)

```csharp
private void HandleInteraction()
{
    if (Input.GetMouseButtonDown(1))
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            float distance = Vector3.Distance(transform.position, hit.point);

            // PRIORIDAD 1: IInteractable (QuestGiver, etc.)
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                if (distance > interactionRange)
                {
                    Debug.Log("Demasiado lejos para interactuar");
                    return;
                }
                interactable.Interact(gameObject);
                return;
            }

            // PRIORIDAD 2: LootBag
            // ...
        }
    }
}
```

---

### Problemas Comunes y Soluciones

#### 🚨 CRÍTICO: "Previous Quest Not Completed" con Quest Incorrecta

**Síntoma:** Usuario completa Quest 1, sube a nivel 3, pero el NPC dice "Primero debes completar: [nombre de quest diferente]" en lugar de mostrar Quest 2.

**Ejemplo Real (Bug Reportado):**
```
Player Level: 3
Completed Quests CSV: 'Quest1_Tutorial'
Total Quests in Resources/Quests: 5
  Quest: MisionCocodrilos - Title: Caza de Cocodrilos - Order: 0 - ReqLevel: 1
  Quest: Quest1_Tutorial - Title: El Despertar del Héroe - Order: 0 - ReqLevel: 1  ← DUPLICADO
  Quest: Quest2_VillageInDanger - Title: Aldea en Peligro - Order: 1 - ReqLevel: 3
```

**Causa:** Existe un **asset de quest antiguo** con el mismo `orderInChain` que una quest nueva. Cuando el sistema valida Quest2 (order 1), busca la quest previa (order 0) y encuentra `MisionCocodrilos` en lugar de `Quest1_Tutorial`.

**Diagnóstico:**
1. Presionar **Tecla D** para debug
2. Ver la lista de quests en Resources
3. Buscar quests con el mismo `Order` (duplicados)

**Solución:**
1. Eliminar el archivo de quest antiguo: `Assets/Resources/Quests/MisionCocodrilos.asset`
2. O cambiar su `orderInChain` a un valor único no usado
3. Verificar con Tecla D que no haya duplicados

**Prevención:**
- Mantener `orderInChain` único y secuencial (0, 1, 2, 3...)
- Usar el comando debug (D) regularmente durante desarrollo
- Eliminar quests antiguas cuando creas nuevas versiones

---

#### Error: "Quest not found in Resources/Quests"

**Causa:** La quest no está en la carpeta correcta.

**Solución:**
1. Verifica que la quest esté en `Assets/Resources/Quests/`
2. El nombre del archivo se usa como ID (ej: `Quest1_Tutorial.asset`)

#### Problema: Cliente ve "Nueva Quest" cuando debería ver "¡Completa!"

**Causa:** ScriptableObject no se sincronizó (bug ya resuelto).

**Solución:** Verificar que `QuestStatus` use `string questName` en lugar de `QuestData data`.

#### Problema: Progreso no se actualiza al matar enemigos

**Causa:** Nombre del NPC no coincide exactamente.

**Solución:**
1. Verificar que `NpcData.npcName` coincida con `QuestObjective.targetName`
2. La comparación es case-insensitive y trimmed, pero debe ser el mismo texto base
3. Ejemplo: "Cocodrilo" en quest y "Cocodrilo" en NPC (no "cocodrilo salvaje")

#### Problema: No se puede interactuar con NPC desde lejos

**Causa:** Validación de distancia funcionando correctamente.

**Solución:** Acercarse al NPC (máx 5m). Configurable en `PlayerController.interactionRange`.

#### Problema: QuestLog no se actualiza

**Causa:** Bug ya resuelto (había un `if (!panel.activeSelf) return;`).

**Solución:** Verificar que `QuestLogUI.UpdateLog()` NO tenga check de visibilidad del panel.

#### Problema: Quest bloqueada no muestra mensaje adicional

**Causa:** Campo opcional `blockedReasonText` no asignado.

**Solución:**
1. En el Inspector de `QuestGiverUI`, el campo `blockedReasonText` es opcional
2. Si no está asignado, solo se usa `statusText` (suficiente para MVP)
3. Para mensaje adicional, crear un TextMeshProUGUI y asignarlo

#### Problema: NPC no abre diálogo

**Causa 1:** Distancia mayor a 5m.
**Causa 2:** Collider no configurado en NPC.
**Causa 3:** QuestGiver component no asignado.

**Solución:**
1. Acercarse al NPC
2. Verificar que el GameObject tenga Collider
3. Verificar que tenga componente `QuestGiver` con quests asignadas

#### Problema: XP de quest no coincide con lo esperado

**Causa:** `autoCalculateXP` desactivado o fórmula manual incorrecta.

**Solución:**
1. Abrir QuestData en Inspector
2. Activar `autoCalculateXP`
3. El campo `xpReward` se actualizará automáticamente al cambiar `requiredLevel`

---

### Performance y Optimización

#### GetQuestData() y Resources.LoadAll

**Situación Actual (MVP):**
```csharp
public QuestData GetQuestData()
{
    QuestData[] allQuests = Resources.LoadAll<QuestData>("Quests");
    return System.Array.Find(allQuests, q => q.name == localQuestName);
}
```

**Impacto:**
- **Para 1-10 quests:** Negligible (< 0.1ms)
- **Para 100+ quests:** Podría ser lento (se carga el array cada vez)

**Optimización Futura (Opcional):**
```csharp
private static QuestData[] cachedQuests;

public QuestData GetQuestData()
{
    if (cachedQuests == null)
    {
        cachedQuests = Resources.LoadAll<QuestData>("Quests");
    }

    string localQuestName = questName;
    return System.Array.Find(cachedQuests, q => q.name == localQuestName);
}
```

**Recomendación:** No optimizar hasta tener 50+ quests.

---

### Mejoras Futuras

#### Sistema de Quests

**✅ Implementado en Fase 8:**
- ✅ Cadena lineal con `orderInChain`
- ✅ Bloqueo por nivel con UI motivacional
- ✅ Auto-balanceo de XP
- ✅ Persistencia de quests completadas (CSV)
- ✅ Validación de 4 capas
- ✅ Debug command (Tecla D)

**🔮 Pendiente para Futuras Fases:**

1. **Múltiples Objetivos por Quest:**
   - Actualmente: Solo se rastrea el primer objetivo.
   - Futuro: Loop sobre todos los objetivos en paralelo.
   - Ejemplo: "Matar 3 lobos Y recolectar 5 hierbas".

2. **Otros Tipos de Objetivos:**
   - `ObjectiveType.Collect` - Recoger items del inventario.
   - `ObjectiveType.Talk` - Hablar con NPCs específicos.
   - `ObjectiveType.Explore` - Entrar en una zona.
   - `ObjectiveType.Escort` - Proteger NPC hasta un punto.

3. **Cadenas Paralelas:**
   - Actualmente: Solo una cadena lineal principal.
   - Futuro: Múltiples cadenas independientes (ej: quest principal + side quests).
   - Requiere: Sistema de categorías de quests.

4. **Items de Recompensa:**
   - `QuestData.itemRewards` - Lista de ItemData.
   - Al completar, añadir items al inventario automáticamente.
   - Validar espacio en inventario antes de entregar.

5. **Indicadores Visuales (World Space):**
   - Signo de exclamación (!) sobre NPC con quest nueva (amarillo).
   - Signo de interrogación (?) sobre NPC con quest completa (dorado).
   - Signo de exclamación gris si quest bloqueada.
   - Billboard que mira siempre a la cámara.

6. **Persistencia en Disco:**
   - Actualmente: CSV en memoria (se pierde al cerrar).
   - Futuro: Guardar en archivo JSON o base de datos.
   - Cargar progreso al iniciar sesión.

7. **Abandono de Quests (Opcional):**
   - NO recomendado para cadena lineal.
   - Si se implementa: Botón "Abandonar" en QuestLog.
   - `CmdAbandonQuest(index)` - Elimina de la lista sin recompensas.
   - Permitir volver a aceptar después.

8. **Diálogos con Ramas:**
   - Sistema de diálogo con opciones múltiples.
   - Diferentes respuestas del NPC según elecciones.
   - Integración con sistema de quest.

---

## 📝 NOTAS PARA PRÓXIMA SESIÓN

### Completado ✅
- ✅ FASE 0: Configuración del proyecto
- ✅ FASE 0.5: Login y selección de clase
- ✅ FASE 1: Player Setup & Cámara
- ✅ FASE 2: Mundo, Zonas y NavMesh
- ✅ FASE 3: Stats y Clases
- ✅ FASE 4: Inventario
- ✅ FASE 5: Combate y Habilidades
- ✅ FASE 6: Muerte y Loot
- ✅ FASE 7: NPCs e IA (Spawning, Aggro, Loot Tables, XP System, Physics Fixes)
- ✅ FASE 7.5: IA Avanzada (Leashing, Spawners, Tab Targeting)
- ✅ **FASE 8: Sistema de Quests - Cadena Lineal Completo**
  - ✅ Cadena lineal story-driven con progresión por niveles
  - ✅ Validación de 4 capas (nivel, orden, duplicados, historial)
  - ✅ Auto-balanceo de XP (fórmula automática)
  - ✅ Persistencia de quests completadas (CSV en SyncVar)
  - ✅ UI con 4 estados (Nueva/En Progreso/Completa/Bloqueada)
  - ✅ NPC inteligente que determina qué mostrar automáticamente
  - ✅ Debug command (Tecla D)
  - ✅ 4 quests de ejemplo configuradas

### Pendiente ⏳
- ⏳ FASE 8.5: Persistencia en Disco (Guardar progreso de quests y stats)
- ⏳ FASE 9: Polish y Build

### Issues Conocidos 🐛
- Ninguno crítico.
- ⚠️ **Advertencia:** Asegurar que no existan quests con `orderInChain` duplicados en Resources/Quests/. Usar comando debug (D) para verificar.

### Mejoras Futuras 💡
1. Sistema de persistencia (guardar en archivo o DB)
2. Animaciones para personajes (ataque, cast)
3. Efectos visuales de habilidades (partículas reales)
4. Sistema de chat
5. Mini-mapa
6. Barras de progreso animadas para HP/Mana

---

## 🔗 REFERENCIAS Y DOCUMENTACIÓN
- **Mirror Networking:** https://mirror-networking.gitbook.io/
- **Unity AI Navigation:** https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/
- **Input System:** https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/
- **URP:** https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/

---

**Última actualización:** 12 de Enero 2026
**Autor:** Sesión de desarrollo con Claude Code
**Versión:** 1.6 (Fase 8: Sistema de Quests - Cadena Lineal con Progresión Completa)
