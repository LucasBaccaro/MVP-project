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
    /// </summary>
    public class PlayerCombat : NetworkBehaviour
    {
        [Header("Abilities")]
        [Tooltip("Habilidades disponibles para este jugador")]
        public readonly SyncList<AbilityData> abilities = new SyncList<AbilityData>();

        [Header("References")]
        private TargetingSystem targetingSystem;
        private PlayerStats playerStats;
        private ZoneHandler zoneHandler; // Referencia al ZoneHandler

        // Cooldowns activos (abilityIndex -> tiempo cuando estará listo)
        private Dictionary<int, float> cooldowns = new Dictionary<int, float>();

        // Eventos para UI
        public event System.Action<int, float> OnCooldownStarted;
        public event System.Action<int> OnCooldownReady;

        private void Awake()
        {
            targetingSystem = GetComponent<TargetingSystem>();
            playerStats = GetComponent<PlayerStats>();
            zoneHandler = GetComponent<ZoneHandler>(); // Obtener referencia
            
            // Suscribirse a cambios en la SyncList
            abilities.Callback += OnAbilitiesChanged;
        }

        private void Update()
        {
            // Solo el jugador local procesa input
            if (!isLocalPlayer) return;

            // Atajos de teclado para habilidades (1, 2, 3, 4)
            if (Input.GetKeyDown(KeyCode.Alpha1)) TryUseAbility(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TryUseAbility(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TryUseAbility(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) TryUseAbility(3);

            // Actualizar cooldowns
            UpdateCooldowns();
        }

        /// <summary>
        /// Intenta usar una habilidad (llamado desde cliente)
        /// </summary>
        public void TryUseAbility(int abilityIndex)
        {
            // Validaciones locales (feedback rápido)
            if (!ValidateAbilityLocal(abilityIndex))
            {
                return;
            }

            // Obtener el objetivo actual
            NetworkIdentity target = targetingSystem.currentTarget;
            
            // Enviar comando al servidor con el objetivo
            CmdUseAbility(abilityIndex, target);
        }

        /// <summary>
        /// Validaciones locales antes de enviar al servidor
        /// </summary>
        private bool ValidateAbilityLocal(int abilityIndex)
        {
            // Verificar índice válido
            if (abilityIndex < 0 || abilityIndex >= abilities.Count)
            {
                Debug.LogWarning($"[PlayerCombat] Índice de habilidad inválido: {abilityIndex}");
                return false;
            }

            // Verificar que existe la habilidad
            AbilityData ability = abilities[abilityIndex];
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
            if (targetingSystem.currentTarget == null)
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
                ZoneHandler targetZone = targetingSystem.currentTarget.GetComponent<ZoneHandler>();
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
            if (abilityIndex < 0 || abilityIndex >= abilities.Count)
            {
                Debug.LogWarning($"[PlayerCombat][Server] Índice inválido: {abilityIndex}");
                return false;
            }

            ability = abilities[abilityIndex];
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
                    // TODO: Implementar buffs en el futuro
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
            PlayerStats targetStats = target.GetComponent<PlayerStats>();
            if (targetStats != null)
            {
                int totalDamage = ability.baseDamage + playerStats.damage;
                targetStats.TakeDamage(totalDamage);
                
                Debug.Log($"[PlayerCombat][Server] {gameObject.name} hizo {totalDamage} de daño a {target.gameObject.name}");
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
                int healAmount = ability.baseDamage; // Reutilizamos baseDamage para heal
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
            if (abilityIndex < 0 || abilityIndex >= abilities.Count) return;
            
            AbilityData ability = abilities[abilityIndex];
            if (ability == null) return;

            // TODO: Instanciar partículas, reproducir sonidos, etc.
            Debug.Log($"[PlayerCombat][Client] Efecto de {ability.abilityName} reproducido");
        }

        #region Cooldown Management

        /// <summary>
        /// Inicia un cooldown
        /// </summary>
        /// <summary>
        /// Inicia un cooldown (Server Side)
        /// </summary>
        [Server]
        private void StartCooldown(int abilityIndex, float cooldownTime)
        {
            // 1. Actualizar diccionario del servidor para validación
            float readyTime = Time.time + cooldownTime;
            
            if (cooldowns.ContainsKey(abilityIndex))
                cooldowns[abilityIndex] = readyTime;
            else
                cooldowns.Add(abilityIndex, readyTime);
            
            // 2. Avisar a los clientes para la UI
            RpcStartCooldown(abilityIndex, cooldownTime);
        }

        /// <summary>
        /// ClientRpc: Inicia el cooldown visual en los clientes
        /// </summary>
        [ClientRpc]
        private void RpcStartCooldown(int abilityIndex, float cooldownTime)
        {
            // 1. Actualizar diccionario local (para predicción/UI)
            float readyTime = Time.time + cooldownTime;
            
            if (cooldowns.ContainsKey(abilityIndex))
                cooldowns[abilityIndex] = readyTime;
            else
                cooldowns.Add(abilityIndex, readyTime);

            // 2. Disparar evento para la UI
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

            // Notificar habilidades listas
            foreach (int abilityIndex in readyAbilities)
            {
                cooldowns.Remove(abilityIndex);
                OnCooldownReady?.Invoke(abilityIndex);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Callback cuando la SyncList de habilidades cambia
        /// </summary>
        private void OnAbilitiesChanged(SyncList<AbilityData>.Operation op, int index, AbilityData oldItem, AbilityData newItem)
        {
            Debug.Log($"[PlayerCombat] Habilidades actualizadas. Total: {abilities.Count}");
        }

        /// <summary>
        /// Asigna habilidades al jugador (llamado desde NetworkManager al inicializar)
        /// </summary>
        [Server]
        public void SetAbilities(AbilityData[] newAbilities)
        {
            if (newAbilities == null || newAbilities.Length == 0)
            {
                Debug.LogWarning("[PlayerCombat][Server] Intentando asignar habilidades null o vacías");
                return;
            }

            // Limpiar y añadir a la SyncList
            abilities.Clear();
            foreach (var ability in newAbilities)
            {
                if (ability != null)
                {
                    abilities.Add(ability);
                }
            }
            
            Debug.Log($"[PlayerCombat][Server] {abilities.Count} habilidades asignadas");
        }

        #endregion
    }
}
