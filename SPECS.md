Aquí tienes el **Plan Maestro Detallado** para tu MMO MVP. He integrado todo el feedback, ajustado la lógica de la cámara (hija del prefab), agregado el sistema de Login, el NavMesh y los detalles técnicos que faltaban.

Antes de empezar, respondo tu duda sobre el tiempo:

> **¿Por qué 30-42 horas? ¿En cuánto lo hago yo?**
>
> *   **Un experto (Senior/Specialist):** Si yo tuviera que hacer esto desde cero, con experiencia previa en Mirror y Unity, me tomaría unas **10 a 15 horas** de trabajo concentrado. Tengo los patrones en la cabeza y sé cómo solucionar los bugs de sincronización rápido.
> *   **Tu estimación (30-42h):** Es realista y saludable para alguien que está *aprendiendo* o consolidando conocimientos. La mayor parte del tiempo no se va en escribir código, se va en: **Debugear por qué una variable no se sincroniza, configurar el UI pixel-perfect, pelear con el Drag&Drop del inventario y hacer builds para probar cliente/servidor.**
>
> **Consejo:** No te presiones con el reloj. Mejor que funcione bien a que esté rápido.

---

# 🗺️ GUÍA PASO A PASO: MMO MVP con Mirror (Unity 6)

## 🛠️ FASE 0: Configuración del Entorno (1 Hora)

**Objetivo:** Tener el proyecto listo, limpio y con las capas físicas configuradas.

1.  **Limpieza:**
    *   Borra `HolaMundo.cs` y cualquier script de prueba anterior.
2.  **Estructura de Carpetas:** Crea estas carpetas en `Assets/_Game/`:
    *   `Scripts/Core` (Managers)
    *   `Scripts/Player` (Controlador, Stats)
    *   `Scripts/Network` (Lógica específica de Mirror)
    *   `Scripts/UI` (Menús, HUD)
    *   `Scripts/Items` (Datos de items)
    *   `Scripts/Combat` (Daño, Skills)
    *   `Scripts/NPCs` (Enemigos, Vendedores)
    *   `Prefabs/Player`
    *   `Prefabs/UI`
    *   `Prefabs/NPCs`
    *   `Prefabs/Items`
    *   `ScriptableObjects` (Resources)
    *   `Scenes`
3.  **Layers y Tags (CRÍTICO para el Raycast y Combate):**
    *   Ve a *Edit > Project Settings > Tags and Layers*.
    *   **Layers:**
        *   Layer 3: `Ground` (Suelo navegable)
        *   Layer 6: `Player` (Jugadores)
        *   Layer 7: `Enemy` (NPCs)
        *   Layer 8: `ZoneTrigger` (Detectores de zona)
    *   **Tags:** Añade `SafeZone`, `UnsafeZone`, `Enemy`, `Player`.
4.  **Instalar Paquetes:**
    *   Asegúrate de tener **Mirror**, **AI Navigation** e **Input System**.

---

## 🔐 FASE 0.5: Login y Lobby (1-2 Horas)

**Objetivo:** Que el jugador pueda poner su nombre y entrar al mundo.

1.  **Escena: `MenuPrincipal`**
    *   Crea un Canvas con un `Input Field (TMP)` para el nombre y un Botón "Entrar".
    *   Crea un objeto vacío `NetworkManager` y añádele el componente `NetworkManager` de Mirror y `NetworkManagerHUD` (para testing rápido).
    *   En el componente `NetworkManager`, asigna:
        *   **Offline Scene:** `MenuPrincipal`
        *   **Online Scene:** `GameWorld` (Créala vacía por ahora).
    *   **Transport:** Usa `KcpTransport` (viene por defecto con Mirror actual, es el mejor para UDP).
2.  **Script `NetworkManagerMMO`:**
    *   Crea este script heredando de `NetworkManager`.
    *   Crea una clase/struct `CharacterMessage` que herede de `NetworkMessage` para enviar el nombre al servidor.
    *   Reemplaza el `NetworkManager` del inspector con tu nuevo script.

---

## 📡 FASE 1: Player Setup & Cámara (2-3 Horas)

**Objetivo:** Moverse, verse y que la cámara funcione bien (Hija del Prefab).

1.  **Prefab del Jugador (`PlayerPrefab`):**
    *   Crea una Cápsula. Ponle Layer: `Player`.
    *   Añade componente `NetworkIdentity`. **Importante:** Marca "Server Only" NO, deja todo por defecto (necesitamos Local Player Authority).
    *   Añade componente `NetworkTransform` (Client Authority: ✅).
    *   Añade componente `NavMeshAgent` (para movimiento click-to-move estilo Albion) O `CharacterController` (para WASD). *Recomiendo NavMeshAgent para estilo MMO clásico, pero CharacterController es más directo para WASD.* Usemos **NavMeshAgent** para asegurar compatibilidad fácil con AI luego, o **Rigidbody** kinemático.
        *   *Simplificación:* Usemos movimiento simple con `CharacterController` para WASD.
2.  **La Cámara (Tu petición):**
    *   Dentro del `PlayerPrefab`, crea una **Camera** como hija.
    *   Posiciónala arriba y atrás (ej: 0, 15, -10), rotada 60° en X mirando abajo.
    *   Añade un `AudioListener` a la cámara.
3.  **Script `PlayerController.cs`:**

```csharp
using Mirror;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Referencias")]
    public Camera playerCamera;
    public AudioListener playerListener;
    public CharacterController characterController;

    [Header("Settings")]
    public float speed = 5f;

    public override void OnStartLocalPlayer()
    {
        // SI somos el dueño de este objeto: Activamos cámara y audio
        playerCamera.gameObject.SetActive(true);
        playerListener.enabled = true;
        
        // Color para distinguirnos (opcional)
        GetComponent<MeshRenderer>().material.color = Color.blue;
    }

    void Start()
    {
        // SI NO somos el dueño: Desactivamos cámara para no ver desde los ojos de otro
        if (!isLocalPlayer)
        {
            playerCamera.gameObject.SetActive(false);
            playerListener.enabled = false;
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return; // Solo procesar input local

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, 0, v);
        characterController.SimpleMove(move * speed);
        
        // Rotar el personaje hacia donde camina
        if (move.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(move);
        }
    }
}
```
4.  **Testing:** Build & Run (Server). Editor (Client). Verifica que cada uno ve su propia cámara y ve al otro moverse.

---

## 🌍 FASE 2: Mundo, Zonas y NavMesh (2 Horas)

**Objetivo:** Crear el terreno y definir reglas de zona.

1.  **Escena `GameWorld`:**
    *   Crea un Plano grande (100x100). Layer: `Ground`.
    *   **NavMesh Baking:**
        *   Window > AI > Navigation.
        *   Selecciona el suelo. Pestaña "Object" > Marca "Navigation Static".
        *   Pestaña "Bake" > Click "Bake". (Esto crea el mapa azul de navegación).
2.  **Zonas:**
    *   Crea un Cubo (Trigger) invisible o semitransparente verde en el centro. Tag: `SafeZone`. Layer: `ZoneTrigger`.
    *   El resto del mundo es, por defecto, inseguro. O crea otro Trigger gigante rojo para `UnsafeZone`.
3.  **Script `ZoneHandler` (en el Player):**
    *   Usa `OnTriggerEnter` y `OnTriggerExit` para detectar los tags.
    *   `[SyncVar] public bool isSafeZone;`
    *   Si entra en SafeZone -> `CmdSetSafeZone(true)`.
    *   UI: Muestra un texto "ZONA SEGURA" si `isSafeZone` es true.

---

## 📊 FASE 3: Stats, Clases y Recursos (3 Horas)

**Objetivo:** Vida, Maná, Oro y Regeneración.

1.  **ScriptableObjects de Clase:**
    *   Crea `ClassData.cs` (ScriptableObject) con: `className`, `baseHP`, `baseMana`, `baseDamage`.
    *   Crea assets para: Mago, Paladin, Clerigo, Cazador.
2.  **Script `PlayerStats.cs`:**
    *   `[SyncVar]` para `currentHealth`, `maxHealth`, `currentMana`, `maxMana`, `Gold`, `Level`, `XP`.
    *   **Regeneración:**
        *   En `Update()` (solo en `isServer`), cada 1 segundo: `currentMana += manaRegenRate`.
3.  **Selección de Clase:**
    *   Al hacer login, o con un NPC inicial, envía un `Command` al server `CmdSelectClass(int classIndex)`.
    *   El server busca el ScriptableObject y aplica los stats base al `PlayerStats`.

---

## 🎒 FASE 4: Inventario (El paso difícil) (5-6 Horas)

**Objetivo:** Items, UI y mover cosas.

1.  **Base de Datos:**
    *   `ItemData.cs` (ScriptableObject): ID, Nombre, Icono (Sprite), EsApilable.
    *   `ItemDatabase.cs`: Un script con una lista estática o Singleton que tiene TODOS los items del juego. Método `GetItem(int id)`.
2.  **Lógica Backend (`PlayerInventory.cs`):**
    *   Struct `InventorySlot` (necesita `[Serializable]`): `int itemID`, `int amount`.
    *   `readonly SyncList<InventorySlot> inventory = new SyncList<InventorySlot>();`
    *   En `OnStartServer`, inicializa la lista con slots vacíos (ID -1).
3.  **UI (`InventoryUI.cs`):**
    *   Crea un Panel con 20 botones/slots.
    *   Loop en `Update` (o mejor, suscríbete al callback `inventory.Callback`): Dibuja los iconos según los IDs de la SyncList.
4.  **Drag & Drop:**
    *   Implementa `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler` en los slots de UI.
    *   Al soltar item A sobre slot B -> Llama `CmdSwapItems(indexA, indexB)`.
    *   **Server:** Valida índices y hace el swap en la SyncList. Mirror sincroniza el cambio automáticamente.

---

## ⚔️ FASE 5: Combate y Habilidades (4-5 Horas)

**Objetivo:** Pegar, cooldowns y Line of Sight.

1.  **Targeting:**
    *   Click en entidad (Layer Enemy/Player) -> Guarda referencia `NetworkIdentity currentTarget`.
    *   UI: Muestra barra de vida del target.
2.  **Script `PlayerCombat.cs`:**
    *   `[Command] CmdUseAbility(int abilityIndex)`.
    *   **Validaciones Servidor:**
        1.  ¿Tiene maná?
        2.  ¿Cooldown listo?
        3.  ¿Distancia correcta? (`Vector3.Distance`).
        4.  **Line of Sight:**
            ```csharp
            bool CanSeeTarget(Transform target) {
                Vector3 dir = target.position - transform.position;
                // Raycast que choca solo con Ground (Layer 3) o Obstacles
                if (Physics.Raycast(transform.position, dir, out RaycastHit hit, dir.magnitude, layerMaskObstaculos)) {
                    return false; // Pared en medio
                }
                return true;
            }
            ```
3.  **Ejecución:**
    *   Resta maná.
    *   Calcula daño.
    *   Aplica daño al `PlayerStats` del target (`TakeDamage(amount)`).
    *   `[ClientRpc]` para reproducir sonido/partícula en todos los clientes.

---

## 💀 FASE 6: Muerte y Loot (3 Horas)

1.  **Muerte:**
    *   En `TakeDamage`, si HP <= 0 -> `Die()`.
    *   `Die()` en Servidor:
        *   Instancia prefab `LootBag` en la posición.
        *   Copia el inventario del jugador al `LootBag`.
        *   Limpia inventario jugador.
        *   Mueve al jugador al `SpawnPoint` de la ciudad.
        *   Resetea HP/Mana.
2.  **Loot Bag:**
    *   Prefab con Trigger.
    *   Al hacer click/entrar -> Abre UI de loot.
    *   Botón "Recoger todo" -> `CmdLootAll()`.

---

## 🤖 FASE 7: NPCs e IA (3 Horas)

1.  **NavMesh Agent:**
    *   El enemigo usa `NavMeshAgent` para perseguir.
    *   `EnemyController.cs` (Server Only logic):
        *   Si `Distance(player) < aggroRange` -> `agent.SetDestination(player.position)`.
        *   Si `Distance < attackRange` -> Atacar.
2.  **Spawner:**
    *   Script simple que instancia un enemigo si hay menos de X en la zona.
    *   `NetworkServer.Spawn(enemyInstance)`.

---

## 📜 FASE 8 & 9: Quests y Persistencia (RAM) (3-4 Horas)

1.  **Quests:** Estructura simple de IDs (`List<int> completedQuests`).
    *   NPC verifica si tienes quest ID 1 completada para darte la 2.
2.  **Persistencia en Sesión:**
    *   Clase estática `ServerDataStorage` en el Servidor.
    *   Dictionary `Dictionary<string, PlayerSaveData>`.
    *   **OnServerDisconnect:** Guarda los datos del `Player` al Diccionario usando el nombre como Key.
    *   **OnServerAddPlayer:** Antes de spawnear, mira si el nombre existe en el diccionario. Si existe, carga los datos (XP, Inventario) en el prefab recién creado.

---

## 💅 FASE 10: Polish y Build Final (2 Horas)

1.  **Builds:**
    *   File > Build Settings.
    *   Crea una build para Windows/Mac.
    *   Ejecuta 1 instancia como "Server Only" (o Host).
    *   Ejecuta 2 instancias como Clientes.
2.  **Verificación:**
    *   ¿Se ve fluido?
    *   ¿El oro sube al matar?
    *   ¿Si tiro un item, el otro lo ve?

---

### 📝 Resumen de Tareas Técnicas Críticas

1.  **NetworkIdentity:** Todo objeto interactivo DEBE tenerlo.
2.  **Server Authority:** NUNCA restes vida en el cliente. El cliente pide (`Command`), el server ejecuta y avisa (`SyncVar`/`ClientRpc`).
3.  **Layers:** Configúralas al principio o el Raycast de combate te va a fallar (le pegarás a tu propio collider).
4.  **Cámara:** Al usar cámara hija, recuerda el código de `OnStartLocalPlayer` para apagar las cámaras de los demás.

¡Listo! Este markdown es tu hoja de ruta. Empieza por la **Fase 0 y 0.5** hoy mismo.