using Mirror;
using UnityEngine;
using System.Collections.Generic;
using Game.Core;
using Game.Player;

namespace Game.Combat
{
    /// <summary>
    /// Sistema de combate del jugador
    /// Maneja uso de habilidades, validaciones y cooldowns
    /// Sistema de targeting estilo Argentum: seleccionar habilidad -> click en objetivo
    /// </summary>
    public class PlayerCombat : NetworkBehaviour
    {
        [Header("Abilities")]
        [Tooltip("IDs de las habilidades disponibles para este jugador (sincronizado)")]
        public readonly SyncList<int> abilityIDs = new SyncList<int>();

        [Header("References")]
        private TargetingSystem targetingSystem;
        private PlayerStats playerStats;
        private ZoneHandler zoneHandler;

        // Cooldowns activos (abilityIndex -> tiempo cuando estará listo)
        private Dictionary<int, float> cooldowns = new Dictionary<int, float>();

        // Sistema de selección de habilidad (estilo Argentum)
        private int selectedAbilityIndex = -1; // -1 = ninguna seleccionada
        public int SelectedAbilityIndex => selectedAbilityIndex;
        public bool HasSelectedAbility => selectedAbilityIndex >= 0;

        // Eventos para UI
        public event System.Action<int, float> OnCooldownStarted;
        public event System.Action<int> OnCooldownReady;
        public event System.Action OnAbilitiesUpdated;
        public event System.Action<int> OnAbilitySelected; // Notifica qué habilidad se seleccionó (-1 = ninguna)

        /// <summary>
        /// Propiedad para obtener las AbilityData desde los IDs sincronizados
        /// </summary>
        public List<AbilityData> abilities
        {
            get
            {
                List<AbilityData> result = new List<AbilityData>();
                if (AbilityDatabase.Instance == null) return result;

                foreach (int id in abilityIDs)
                {
                    AbilityData ability = AbilityDatabase.Instance.GetAbility(id);
                    result.Add(ability); // Puede ser null si no existe
                }
                return result;
            }
        }

        private void Awake()
        {
            targetingSystem = GetComponent<TargetingSystem>();
            playerStats = GetComponent<PlayerStats>();
            zoneHandler = GetComponent<ZoneHandler>();

            // Suscribirse a cambios en la SyncList
            abilityIDs.Callback += OnAbilityIDsChanged;
        }

        // Rango mínimo para considerar una habilidad como "de rango" (usa sistema de cruz)
        private const float RANGED_ABILITY_THRESHOLD = 3f;

        private void Update()
        {
            // Solo el jugador local procesa input
            if (!isLocalPlayer) return;

            // Atajos de teclado para habilidades (1, 2, 3, 4)
            if (Input.GetKeyDown(KeyCode.Alpha1)) HandleAbilityInput(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) HandleAbilityInput(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) HandleAbilityInput(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) HandleAbilityInput(3);

            // ESC o click derecho cancela la selección
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelAbilitySelection();
            }

            // Actualizar cooldowns
            UpdateCooldowns();
        }

        /// <summary>
        /// Maneja el input de habilidad - decide si usar sistema de cruz o ejecutar directamente
        /// </summary>
        private void HandleAbilityInput(int abilityIndex)
        {
            AbilityData ability = GetAbility(abilityIndex);
            if (ability == null)
            {
                Debug.LogWarning($"[PlayerCombat] No hay habilidad en slot {abilityIndex}");
                return;
            }

            // Si es habilidad de rango (range > threshold), usar sistema de selección con cruz
            if (ability.range >= RANGED_ABILITY_THRESHOLD)
            {
                Debug.Log($"[PlayerCombat] Habilidad de RANGO detectada: {ability.abilityName} (range={ability.range}m) - Usando sistema de cruz");
                SelectAbility(abilityIndex);
            }
            else
            {
                // Habilidad melee - usar sistema antiguo (ejecutar directamente si hay target)
                Debug.Log($"[PlayerCombat] Habilidad MELEE detectada: {ability.abilityName} (range={ability.range}m) - Ejecutando directamente");
                TryUseAbility(abilityIndex);
            }
        }

        /// <summary>
        /// Obtiene el AbilityData para un índice específico
        /// </summary>
        public AbilityData GetAbility(int index)
        {
            if (index < 0 || index >= abilityIDs.Count) return null;
            if (AbilityDatabase.Instance == null) return null;

            return AbilityDatabase.Instance.GetAbility(abilityIDs[index]);
        }

        /// <summary>
        /// Obtiene la habilidad actualmente seleccionada (null si ninguna)
        /// </summary>
        public AbilityData GetSelectedAbility()
        {
            if (!HasSelectedAbility) return null;
            return GetAbility(selectedAbilityIndex);
        }

        /// <summary>
        /// Selecciona una habilidad para usar (estilo Argentum)
        /// No la ejecuta, solo la prepara para el siguiente click
        /// </summary>
        public void SelectAbility(int abilityIndex)
        {
            Debug.Log($"[PlayerCombat] SelectAbility llamado - index={abilityIndex}, isLocalPlayer={isLocalPlayer}, isServer={isServer}");

            // Verificar índice válido
            if (abilityIndex < 0 || abilityIndex >= abilityIDs.Count)
            {
                Debug.LogWarning($"[PlayerCombat] Índice de habilidad inválido: {abilityIndex} (total={abilityIDs.Count})");
                return;
            }

            // Verificar que existe la habilidad
            AbilityData ability = GetAbility(abilityIndex);
            if (ability == null)
            {
                Debug.LogWarning($"[PlayerCombat] No hay habilidad en slot {abilityIndex}");
                return;
            }

            // Verificar cooldown
            if (IsOnCooldown(abilityIndex))
            {
                float remainingTime = GetCooldownRemaining(abilityIndex);
                Debug.Log($"[PlayerCombat] {ability.abilityName} en cooldown ({remainingTime:F1}s restantes)");
                return;
            }

            // Verificar maná
            if (playerStats.currentMana < ability.manaCost)
            {
                Debug.LogWarning($"[PlayerCombat] Maná insuficiente ({playerStats.currentMana}/{ability.manaCost})");
                return;
            }

            // Si ya está seleccionada, deseleccionar (toggle)
            if (selectedAbilityIndex == abilityIndex)
            {
                Debug.Log($"[PlayerCombat] Toggle: deseleccionando habilidad {abilityIndex}");
                CancelAbilitySelection();
                return;
            }

            // Seleccionar la habilidad
            selectedAbilityIndex = abilityIndex;
            Debug.Log($"[PlayerCombat] >>> HABILIDAD SELECCIONADA: {ability.abilityName} (index={abilityIndex}) - HasSelectedAbility={HasSelectedAbility}");
            OnAbilitySelected?.Invoke(selectedAbilityIndex);
        }

        /// <summary>
        /// Cancela la selección de habilidad actual
        /// </summary>
        public void CancelAbilitySelection()
        {
            Debug.Log($"[PlayerCombat] CancelAbilitySelection llamado - selectedAbilityIndex={selectedAbilityIndex}");
            if (selectedAbilityIndex >= 0)
            {
                Debug.Log($"[PlayerCombat] >>> CANCELANDO selección de habilidad {selectedAbilityIndex}");
                selectedAbilityIndex = -1;
                OnAbilitySelected?.Invoke(-1);
            }
        }

        /// <summary>
        /// Ejecuta la habilidad seleccionada en el objetivo dado (llamado desde TargetingSystem)
        /// </summary>
        public void ExecuteSelectedAbilityOnTarget(NetworkIdentity target)
        {
            Debug.Log($"[PlayerCombat] ExecuteSelectedAbilityOnTarget llamado - HasSelectedAbility={HasSelectedAbility}, target={target?.gameObject.name ?? "NULL"}");

            if (!HasSelectedAbility)
            {
                Debug.LogWarning("[PlayerCombat] No hay habilidad seleccionada");
                return;
            }

            if (target == null)
            {
                Debug.LogWarning("[PlayerCombat] No hay objetivo válido");
                return;
            }

            AbilityData ability = GetSelectedAbility();
            Debug.Log($"[PlayerCombat] >>> EJECUTANDO habilidad {ability?.abilityName ?? "NULL"} en {target.gameObject.name}");

            // Usar la habilidad seleccionada
            TryUseAbility(selectedAbilityIndex, target);

            // Cancelar selección después de usar (comportamiento Argentum)
            CancelAbilitySelection();
        }

        /// <summary>
        /// Intenta usar una habilidad en un objetivo específico
        /// </summary>
        public void TryUseAbility(int abilityIndex, NetworkIdentity target)
        {
            // Validaciones locales (feedback rápido)
            if (!ValidateAbilityLocal(abilityIndex, target))
            {
                return;
            }

            // Enviar comando al servidor con el objetivo
            CmdUseAbility(abilityIndex, target);
        }

        /// <summary>
        /// Intenta usar una habilidad (llamado desde cliente) - usa el objetivo actual del targeting
        /// </summary>
        public void TryUseAbility(int abilityIndex)
        {
            // Obtener el objetivo actual
            NetworkIdentity target = targetingSystem.currentTarget;
            TryUseAbility(abilityIndex, target);
        }

        /// <summary>
        /// Validaciones locales antes de enviar al servidor
        /// </summary>
        private bool ValidateAbilityLocal(int abilityIndex, NetworkIdentity target)
        {
            // Verificar índice válido
            if (abilityIndex < 0 || abilityIndex >= abilityIDs.Count)
            {
                Debug.LogWarning($"[PlayerCombat] Índice de habilidad inválido: {abilityIndex}");
                return false;
            }

            // Verificar que existe la habilidad
            AbilityData ability = GetAbility(abilityIndex);
            if (ability == null)
            {
                Debug.LogWarning($"[PlayerCombat] No hay habilidad en slot {abilityIndex}");
                return false;
            }

            // Verificar cooldown
            if (IsOnCooldown(abilityIndex))
            {
                float remainingTime = GetCooldownRemaining(abilityIndex);
                Debug.Log($"[PlayerCombat] {ability.abilityName} en cooldown ({remainingTime:F1}s restantes)");
                return false;
            }

            // Verificar que hay objetivo
            if (target == null)
            {
                Debug.LogWarning($"[PlayerCombat] No hay objetivo seleccionado");
                return false;
            }

            // Validar Zona Segura (si es habilidad de daño)
            if (ability.abilityType == AbilityType.Damage)
            {
                // Chequear si YO estoy en zona segura
                if (zoneHandler != null && zoneHandler.IsInSafeZone())
                {
                    Debug.LogWarning("[PlayerCombat] No puedes atacar desde una zona segura");
                    return false;
                }

                // Chequear si el OBJETIVO está en zona segura
                ZoneHandler targetZone = target.GetComponent<ZoneHandler>();
                if (targetZone != null && targetZone.IsInSafeZone())
                {
                    Debug.LogWarning("[PlayerCombat] El objetivo está en zona segura");
                    return false;
                }
            }

            // Verificar maná
            if (playerStats.currentMana < ability.manaCost)
            {
                Debug.LogWarning($"[PlayerCombat] Maná insuficiente ({playerStats.currentMana}/{ability.manaCost})");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Command: Cliente solicita usar habilidad
        /// </summary>
        [Command]
        private void CmdUseAbility(int abilityIndex, NetworkIdentity clientTarget)
        {
            // Validaciones del servidor (autoridad)
            if (!ValidateAbilityServer(abilityIndex, clientTarget, out AbilityData ability, out NetworkIdentity target))
            {
                return;
            }

            // Ejecutar habilidad
            ExecuteAbility(ability, target);

            // Iniciar cooldown en el servidor
            StartCooldown(abilityIndex, ability.cooldownTime);

            // Notificar a todos los clientes para efectos visuales
            RpcPlayAbilityEffect(abilityIndex, target.transform.position);
        }

        /// <summary>
        /// Validaciones del servidor (definitivas)
        /// </summary>
        private bool ValidateAbilityServer(int abilityIndex, NetworkIdentity clientTarget, out AbilityData ability, out NetworkIdentity target)
        {
            ability = null;
            target = null;

            // 1. Verificar índice válido
            if (abilityIndex < 0 || abilityIndex >= abilityIDs.Count)
            {
                Debug.LogWarning($"[PlayerCombat][Server] Índice inválido: {abilityIndex}");
                return false;
            }

            ability = GetAbility(abilityIndex);
            if (ability == null)
            {
                Debug.LogWarning($"[PlayerCombat][Server] Habilidad null en slot {abilityIndex}");
                return false;
            }

            // 2. Verificar maná
            if (playerStats.currentMana < ability.manaCost)
            {
                Debug.LogWarning($"[PlayerCombat][Server] Maná insuficiente");
                return false;
            }

            // 3. Verificar cooldown
            if (IsOnCooldown(abilityIndex))
            {
                Debug.LogWarning($"[PlayerCombat][Server] Habilidad en cooldown");
                return false;
            }

            // 4. Verificar objetivo válido (usar el objetivo enviado por el cliente)
            target = clientTarget;
            if (target == null || target.gameObject == null)
            {
                Debug.LogWarning($"[PlayerCombat][Server] Objetivo inválido");
                return false;
            }

            // 5. Verificar distancia
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > ability.range)
            {
                Debug.LogWarning($"[PlayerCombat][Server] Objetivo fuera de rango ({distance:F1}m > {ability.range}m)");
                return false;
            }

            // 6. Verificar Line of Sight
            if (!targetingSystem.CanSeeTarget(target.transform))
            {
                Debug.LogWarning($"[PlayerCombat][Server] Sin Line of Sight al objetivo");
                return false;
            }

            // 7. Verificar Zonas Seguras (Server Side)
            if (ability.abilityType == AbilityType.Damage)
            {
                // Chequear si el atacante está en zona segura
                if (zoneHandler != null && zoneHandler.IsInSafeZone())
                {
                    Debug.LogWarning("[PlayerCombat][Server] Atacante en zona segura");
                    return false;
                }

                // Chequear si el objetivo está en zona segura
                ZoneHandler targetZone = target.GetComponent<ZoneHandler>();
                if (targetZone != null && targetZone.IsInSafeZone())
                {
                    Debug.LogWarning("[PlayerCombat][Server] Objetivo en zona segura");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Ejecuta la habilidad en el servidor
        /// </summary>
        [Server]
        private void ExecuteAbility(AbilityData ability, NetworkIdentity target)
        {
            // Gastar maná
            playerStats.SpendMana(ability.manaCost);

            // Aplicar efecto según tipo
            switch (ability.abilityType)
            {
                case AbilityType.Damage:
                    ApplyDamage(ability, target);
                    break;

                case AbilityType.Heal:
                    ApplyHeal(ability, target);
                    break;

                case AbilityType.Buff:
                    Debug.Log($"[PlayerCombat][Server] Buffs no implementados aún");
                    break;
            }

            Debug.Log($"[PlayerCombat][Server] {ability.abilityName} ejecutado en {target.gameObject.name}");
        }

        /// <summary>
        /// Aplica daño al objetivo
        /// </summary>
        [Server]
        private void ApplyDamage(AbilityData ability, NetworkIdentity target)
        {
            IEntityStats targetStats = target.GetComponent<IEntityStats>();
            if (targetStats != null)
            {
                int totalDamage = ability.baseDamage + playerStats.damage;

                // Pasar 'playerStats' como atacante
                targetStats.TakeDamage(totalDamage, playerStats);

                Debug.Log($"[PlayerCombat][Server] {gameObject.name} hizo {totalDamage} de daño a {targetStats.EntityName}");
            }
        }

        /// <summary>
        /// Aplica curación al objetivo
        /// </summary>
        [Server]
        private void ApplyHeal(AbilityData ability, NetworkIdentity target)
        {
            PlayerStats targetStats = target.GetComponent<PlayerStats>();
            if (targetStats != null)
            {
                int healAmount = ability.baseDamage;
                targetStats.Heal(healAmount);

                Debug.Log($"[PlayerCombat][Server] {gameObject.name} curó {healAmount} HP a {target.gameObject.name}");
            }
        }

        /// <summary>
        /// ClientRpc: Reproducir efectos visuales/sonoros
        /// </summary>
        [ClientRpc]
        private void RpcPlayAbilityEffect(int abilityIndex, Vector3 targetPosition)
        {
            AbilityData ability = GetAbility(abilityIndex);
            if (ability == null) return;

            Debug.Log($"[PlayerCombat][Client] Efecto de {ability.abilityName} reproducido");
        }

        #region Cooldown Management

        /// <summary>
        /// Inicia un cooldown (Server Side)
        /// </summary>
        [Server]
        private void StartCooldown(int abilityIndex, float cooldownTime)
        {
            float readyTime = Time.time + cooldownTime;

            if (cooldowns.ContainsKey(abilityIndex))
                cooldowns[abilityIndex] = readyTime;
            else
                cooldowns.Add(abilityIndex, readyTime);

            RpcStartCooldown(abilityIndex, cooldownTime);
        }

        /// <summary>
        /// ClientRpc: Inicia el cooldown visual en los clientes
        /// </summary>
        [ClientRpc]
        private void RpcStartCooldown(int abilityIndex, float cooldownTime)
        {
            float readyTime = Time.time + cooldownTime;

            if (cooldowns.ContainsKey(abilityIndex))
                cooldowns[abilityIndex] = readyTime;
            else
                cooldowns.Add(abilityIndex, readyTime);

            OnCooldownStarted?.Invoke(abilityIndex, cooldownTime);

            Debug.Log($"[PlayerCombat] Cooldown iniciado para slot {abilityIndex}: {cooldownTime}s");
        }

        /// <summary>
        /// Verifica si una habilidad está en cooldown
        /// </summary>
        public bool IsOnCooldown(int abilityIndex)
        {
            if (!cooldowns.ContainsKey(abilityIndex)) return false;

            return Time.time < cooldowns[abilityIndex];
        }

        /// <summary>
        /// Obtiene el tiempo restante de cooldown
        /// </summary>
        public float GetCooldownRemaining(int abilityIndex)
        {
            if (!IsOnCooldown(abilityIndex)) return 0f;

            return cooldowns[abilityIndex] - Time.time;
        }

        /// <summary>
        /// Actualiza cooldowns cada frame
        /// </summary>
        private void UpdateCooldowns()
        {
            List<int> readyAbilities = new List<int>();

            foreach (var kvp in cooldowns)
            {
                if (Time.time >= kvp.Value)
                {
                    readyAbilities.Add(kvp.Key);
                }
            }

            foreach (int abilityIndex in readyAbilities)
            {
                cooldowns.Remove(abilityIndex);
                OnCooldownReady?.Invoke(abilityIndex);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Callback cuando la SyncList de IDs cambia
        /// </summary>
        private void OnAbilityIDsChanged(SyncList<int>.Operation op, int index, int oldItem, int newItem)
        {
            Debug.Log($"[PlayerCombat] Habilidades actualizadas. Total: {abilityIDs.Count}");
            OnAbilitiesUpdated?.Invoke();
        }

        /// <summary>
        /// Asigna habilidades al jugador por IDs (llamado desde NetworkManager al inicializar)
        /// </summary>
        [Server]
        public void SetAbilities(AbilityData[] newAbilities)
        {
            if (newAbilities == null || newAbilities.Length == 0)
            {
                Debug.LogWarning("[PlayerCombat][Server] Intentando asignar habilidades null o vacías");
                return;
            }

            abilityIDs.Clear();
            foreach (var ability in newAbilities)
            {
                if (ability != null)
                {
                    abilityIDs.Add(ability.abilityID);
                }
            }

            Debug.Log($"[PlayerCombat][Server] {abilityIDs.Count} habilidades asignadas (IDs)");
        }

        /// <summary>
        /// Asigna habilidades por IDs directamente
        /// </summary>
        [Server]
        public void SetAbilityIDs(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                Debug.LogWarning("[PlayerCombat][Server] Intentando asignar IDs null o vacíos");
                return;
            }

            abilityIDs.Clear();
            foreach (var id in ids)
            {
                abilityIDs.Add(id);
            }

            Debug.Log($"[PlayerCombat][Server] {abilityIDs.Count} habilidades asignadas (IDs)");
        }

        #endregion
    }
}
