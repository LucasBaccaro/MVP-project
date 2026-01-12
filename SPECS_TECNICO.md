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
    Consumable, Weapon, Armor, Quest, Material, Misc
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
| `CmdAddItem` | itemID, amount | Añade item (apila si es posible) |
| `CmdRemoveItem` | slotIndex, amount | Remueve cantidad de un slot |
| `CmdUseItem` | slotIndex | Usa consumible (restaura HP/Mana) |

**IMPORTANTE:** Los Commands de Mirror **NO pueden tener parámetros opcionales**. Todos los parámetros deben ser explícitos.

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
| 1 | Poción de Salud | Consumable | ✅ | 20 | +50 HP |
| 2 | Poción de Maná | Consumable | ✅ | 20 | +30 Mana |
| 3 | Espada de Hierro | Weapon | ❌ | 1 | +10 Damage |
| 4 | Escudo de Madera | Armor | ❌ | 1 | +5 Armor |
| 5 | Moneda de Oro | Misc | ✅ | 999 | 1 Gold |

**Creación:** Usar menú `MMO > Create Default Items` en Unity.

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

**Flujo de añadir item:**

1. **Cliente:** Usuario presiona botón "Add Health Potion"
2. **Cliente:** `ItemTester.AddHealthPotion()` llama `playerInventory.CmdAddItem(1, 1)`
3. **Servidor:** Command ejecuta:
   - Valida itemID y cantidad
   - Busca ItemData en ItemDatabase
   - Si es apilable, busca stack existente con espacio
   - Si no, busca slot vacío
   - Modifica `SyncList<InventorySlot>`
4. **Mirror:** Detecta cambio en SyncList y sincroniza a TODOS los clientes
5. **Clientes:** Callback `OnInventoryUpdated` dispara evento `OnInventoryChanged`
6. **UI:** `InventoryUI.RefreshUI()` actualiza todos los slots visuales

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
- **Botones UI:** Añadir items específicos (testing)

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

---

## 📝 NOTAS PARA PRÓXIMA SESIÓN

### Completado ✅
- ✅ FASE 0: Configuración del proyecto
- ✅ FASE 0.5: Login y selección de clase
- ✅ FASE 1: Player Setup & Cámara
- ✅ FASE 2: Mundo, Zonas y NavMesh
- ✅ FASE 3: Stats y Clases
- ✅ FASE 4: Inventario (Drag & Drop, SyncList, Commands)

### Pendiente ⏳
- ⏳ FASE 5: Combate y Habilidades
- ⏳ FASE 6: Muerte y Loot
- ⏳ FASE 7: NPCs e IA
- ⏳ FASE 8-9: Quests y Persistencia
- ⏳ FASE 10: Polish y Build

### Issues Conocidos 🐛
- Ninguno en las fases completadas

### Mejoras Futuras 💡
1. Sistema de persistencia (guardar en archivo o DB)
2. Animaciones para personajes
3. Efectos visuales de habilidades
4. Sistema de chat
5. Mini-mapa
6. Barras de progreso animadas para HP/Mana
7. Efectos de partículas al cambiar de zona

---

## 🔗 REFERENCIAS Y DOCUMENTACIÓN

- **Mirror Networking:** https://mirror-networking.gitbook.io/
- **Unity AI Navigation:** https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/
- **Input System:** https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/
- **URP:** https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/

---

**Última actualización:** Enero 2026
**Autor:** Sesión de desarrollo con Claude Code
**Versión:** 1.0
