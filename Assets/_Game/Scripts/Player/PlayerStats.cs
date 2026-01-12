using Mirror;
using UnityEngine;
using Game.Core;

namespace Game.Player
{
    /// <summary>
    /// Maneja todos los stats del jugador (HP, Mana, Oro, Level, XP)
    /// Sincronizado en red con SyncVars
    /// </summary>
    public class PlayerStats : NetworkBehaviour, IEntityStats
    {
        [Header("Class Data")]
        [Tooltip("Datos de la clase del jugador (solo en servidor)")]
        public ClassData classData;

        [SyncVar]
        public string className = "Unknown";

        [Header("Health")]
        [SyncVar(hook = nameof(OnHealthChanged))]
        public int currentHealth = 100;

        [SyncVar]
        public int maxHealth = 100;

        [Header("Mana")]
        [SyncVar(hook = nameof(OnManaChanged))]
        public int currentMana = 50;

        [SyncVar]
        public int maxMana = 50;

        [Header("Resources")]
        [SyncVar(hook = nameof(OnGoldChanged))]
        public int gold = 0;

        [Header("Experience")]
        [SyncVar(hook = nameof(OnLevelChanged))]
        public int level = 1;

        [SyncVar(hook = nameof(OnXPChanged))]
        public int currentXP = 0;

        [SyncVar]
        public int xpToNextLevel = 100;

        // IEntityStats Implementation
        public string EntityName => gameObject.name;
        public string ClassName => className;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        [Header("Combat Stats")]
        [SyncVar]
        public int damage = 10;

        [Header("Regeneration")]
        [SyncVar]
        public float hpRegenRate = 1f;

        [SyncVar]
        public float manaRegenRate = 2f;

        private float regenTimer = 0f;

        [Header("Visual")]
        [Tooltip("Renderer del personaje para aplicar color de clase")]
        public MeshRenderer characterRenderer;

        [SyncVar(hook = nameof(OnClassColorChanged))]
        private Color classColor = Color.white;

        private void Awake()
        {
            if (characterRenderer == null)
            {
                characterRenderer = GetComponentInChildren<MeshRenderer>();
            }
        }

        /// <summary>
        /// Inicializa los stats del jugador basado en ClassData
        /// SOLO llamar desde el servidor
        /// </summary>
        [Server]
        public void InitializeStats(ClassData data)
        {
            if (data == null)
            {
                Debug.LogError("[PlayerStats] ClassData es null!");
                return;
            }

            classData = data;

            // Sincronizar nombre de clase (SyncVar)
            className = data.className;

            // Aplicar stats base (todos son SyncVars, se sincronizan automáticamente)
            maxHealth = data.baseHP;
            currentHealth = maxHealth;

            maxMana = data.baseMana;
            currentMana = maxMana;

            damage = data.baseDamage;
            gold = data.startingGold;
            level = data.startingLevel;
            currentXP = 0;

            hpRegenRate = data.hpRegenRate;
            manaRegenRate = data.manaRegenRate;

            // Calcular XP necesaria para nivel 2
            CalculateXPToNextLevel();

            Debug.Log($"[PlayerStats][Server] Stats inicializados para clase: {data.className}");

            // Aplicar color de clase (se sincroniza automáticamente vía SyncVar)
            classColor = data.classColor;
        }

        /// <summary>
        /// Hook del SyncVar classColor - se llama cuando el color se sincroniza
        /// Se ejecuta automáticamente en todos los clientes cuando el servidor asigna el color
        /// </summary>
        private void OnClassColorChanged(Color oldColor, Color newColor)
        {
            if (characterRenderer != null)
            {
                // Crear una instancia del material para este jugador
                // Esto asegura que cada jugador tenga su propio material único
                Material instanceMaterial = new Material(characterRenderer.material);

                // Aplicar color compatible con URP (Universal Render Pipeline)
                // Intentar ambas propiedades para máxima compatibilidad
                if (instanceMaterial.HasProperty("_BaseColor"))
                {
                    instanceMaterial.SetColor("_BaseColor", newColor);
                    Debug.Log($"[PlayerStats][Client] Color URP (_BaseColor) aplicado: {newColor}");
                }
                else if (instanceMaterial.HasProperty("_Color"))
                {
                    instanceMaterial.SetColor("_Color", newColor);
                    Debug.Log($"[PlayerStats][Client] Color Legacy (_Color) aplicado: {newColor}");
                }
                else
                {
                    // Fallback para shaders sin estas propiedades
                    instanceMaterial.color = newColor;
                    Debug.Log($"[PlayerStats][Client] Color genérico aplicado: {newColor}");
                }

                characterRenderer.material = instanceMaterial;
                Debug.Log($"[PlayerStats][Client] Material instanciado para {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[PlayerStats][Client] characterRenderer es NULL para {gameObject.name}");
            }
        }

        private void Update()
        {
            // Solo el servidor ejecuta la regeneración
            if (!isServer) return;

            RegenerateResources();
        }

        [Server]
        private void RegenerateResources()
        {
            regenTimer += Time.deltaTime;

            if (regenTimer >= 1f)
            {
                regenTimer = 0f;

                // Regenerar HP
                if (currentHealth < maxHealth)
                {
                    currentHealth = Mathf.Min(currentHealth + Mathf.RoundToInt(hpRegenRate), maxHealth);
                }

                // Regenerar Mana
                if (currentMana < maxMana)
                {
                    currentMana = Mathf.Min(currentMana + Mathf.RoundToInt(manaRegenRate), maxMana);
                }
            }
        }

        #region Hooks (SyncVar callbacks)

        private void OnHealthChanged(int oldValue, int newValue)
        {
            if (isLocalPlayer)
            {
                Debug.Log($"[PlayerStats] HP: {newValue}/{maxHealth}");
            }
        }

        private void OnManaChanged(int oldValue, int newValue)
        {
            if (isLocalPlayer)
            {
                Debug.Log($"[PlayerStats] Mana: {newValue}/{maxMana}");
            }
        }

        private void OnGoldChanged(int oldValue, int newValue)
        {
            if (isLocalPlayer)
            {
                Debug.Log($"[PlayerStats] Gold: {newValue}");
            }
        }

        private void OnLevelChanged(int oldValue, int newValue)
        {
            if (isLocalPlayer)
            {
                Debug.Log($"[PlayerStats] Level UP! Nivel: {newValue}");
            }
        }

        private void OnXPChanged(int oldValue, int newValue)
        {
            if (isLocalPlayer)
            {
                Debug.Log($"[PlayerStats] XP: {newValue}/{xpToNextLevel}");
            }

            // Verificar si sube de nivel
            if (isServer && newValue >= xpToNextLevel)
            {
                LevelUp();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Añade oro al jugador
        /// </summary>
        [Server]
        public void AddGold(int amount)
        {
            gold += amount;
            Debug.Log($"[PlayerStats] +{amount} Gold. Total: {gold}");
        }

        /// <summary>
        /// Añade XP al jugador
        /// </summary>
        [Server]
        public void AddXP(int amount)
        {
            currentXP += amount;
            Debug.Log($"[PlayerStats] +{amount} XP. Total: {currentXP}/{xpToNextLevel}");
        }

        /// <summary>
        /// Aplica daño al jugador
        /// </summary>
        [Server]
        public void TakeDamage(int damageAmount, PlayerStats attacker = null)
        {
            currentHealth = Mathf.Max(0, currentHealth - damageAmount);
            Debug.Log($"[PlayerStats] Daño recibido: {damageAmount} de {attacker?.name ?? "Desconocido"}. HP restante: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Restaura HP
        /// </summary>
        [Server]
        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }

        /// <summary>
        /// Gasta maná
        /// </summary>
        [Server]
        public bool SpendMana(int amount)
        {
            if (currentMana >= amount)
            {
                currentMana -= amount;
                return true;
            }
            return false;
        }

        #endregion

        #region Private Methods

        [Server]
        private void LevelUp()
        {
            level++;
            currentXP = 0;

            // Aumentar stats al subir de nivel
            maxHealth += 10;
            currentHealth = maxHealth;

            maxMana += 5;
            currentMana = maxMana;

            damage += 2;

            CalculateXPToNextLevel();

            Debug.Log($"[PlayerStats] ¡LEVEL UP! Nuevo nivel: {level}");
        }

        private void CalculateXPToNextLevel()
        {
            // Fórmula simple: nivel * 100
            xpToNextLevel = level * 100;
        }

        [Header("Death & Loot")]
        [Tooltip("Prefab de la bolsa de loot")]
        public GameObject lootBagPrefab;

        [Server]
        private void Die()
        {
            Debug.Log($"[PlayerStats] Jugador {name} ha muerto!");

            // 1. Obtener inventario y vaciarlo
            PlayerInventory inventory = GetComponent<PlayerInventory>();
            if (inventory != null && lootBagPrefab != null)
            {
                var droppedItems = inventory.ClearInventory();

                if (droppedItems.Count > 0)
                {
                    // 2. Instanciar LootBag
                    GameObject lootBag = Instantiate(lootBagPrefab, transform.position, Quaternion.identity);
                    
                    // 3. Inicializar LootBag con items
                    Game.Items.LootBag lootBagScript = lootBag.GetComponent<Game.Items.LootBag>();
                    if (lootBagScript != null)
                    {
                        lootBagScript.Initialize(droppedItems);
                    }

                    // 4. Spawnear en red
                    NetworkServer.Spawn(lootBag);
                }
            }

            // 5. Mover al punto de spawn
            Transform spawnPoint = NetworkManager.singleton.GetStartPosition();
            Vector3 respawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;

            // Mover en el servidor (para que otros lo vean)
            if (TryGetComponent<CharacterController>(out CharacterController cc))
            {
                cc.enabled = false;
                transform.position = respawnPos;
                cc.enabled = true;
            }
            else
            {
                transform.position = respawnPos;
            }

            // 6. Restaurar stats
            currentHealth = maxHealth;
            currentMana = maxMana;

            Debug.Log($"[PlayerStats] Jugador respawneado en {respawnPos}");

            // 7. Forzar posición en el cliente dueño (CRÍTICO para Client Authority)
            TargetRespawn(connectionToClient, respawnPos);
        }

        [TargetRpc]
        private void TargetRespawn(NetworkConnection target, Vector3 position)
        {
            Debug.Log($"[PlayerStats] Recibido respawn en: {position}");
            
            // Necesario desactivar el CC para moverlo instantáneamente
            if (TryGetComponent<CharacterController>(out CharacterController cc))
            {
                cc.enabled = false;
                transform.position = position;
                cc.enabled = true;
            }
            else
            {
                transform.position = position;
            }
        }

        #endregion
    }
}
