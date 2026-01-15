using Mirror;
using UnityEngine;
using UnityEngine.AI; // Added for NavMesh.SamplePosition
using UnityEngine.EventSystems; // Para verificar si el click está sobre UI
using System;
using System.Collections.Generic; // Added for List
using Game.Player; // Necesario para TargetingUIConnector

namespace Game.Combat
{
    /// <summary>
    /// Sistema de selección de objetivos mediante raycast
    /// Maneja targeting de jugadores y enemigos
    /// Sistema estilo Argentum: cursor cruz cuando hay habilidad seleccionada
    /// </summary>
    public class TargetingSystem : NetworkBehaviour
    {
        [Header("Settings")]
        [Tooltip("Layers que pueden ser targetados (Player, Enemy)")]
        public LayerMask targetableLayers;

        [Tooltip("Layers que bloquean Line of Sight (Ground, Obstacles)")]
        public LayerMask losBlockingLayers;

        [Tooltip("Distancia máxima del raycast de targeting")]
        public float maxTargetingDistance = 100f;

        [Header("Current Target")]
        [Tooltip("Objetivo actual (puede ser null)")]
        public NetworkIdentity currentTarget;

        // Eventos
        public event Action<NetworkIdentity> OnTargetChanged;

        [Header("Visuals")]
        [Tooltip("Prefab del indicador (círculo rojo) para objetivo seleccionado")]
        public GameObject targetIndicatorPrefab;

        [Tooltip("Prefab del cursor cruz para cuando hay habilidad seleccionada")]
        public GameObject crosshairCursorPrefab;

        [Tooltip("Offset vertical del indicador desde el suelo")]
        public float indicatorYOffset = 0.1f;

        private GameObject activeIndicator;
        private GameObject activeCrosshair;
        private Camera playerCamera;
        private PlayerCombat playerCombat;

        private void Start()
        {
            Debug.Log($"[TargetingSystem] Start - isLocalPlayer={isLocalPlayer}, isServer={isServer}, isClient={isClient}, gameObject={gameObject.name}");

            // Solo el jugador local puede hacer targeting
            if (!isLocalPlayer)
            {
                Debug.Log("[TargetingSystem] No es local player, saliendo de Start");
                return;
            }

            // Obtener referencia a PlayerCombat
            playerCombat = GetComponent<PlayerCombat>();
            Debug.Log($"[TargetingSystem] PlayerCombat reference: {(playerCombat != null ? "OK" : "NULL")}");

            // Asegurar que existe el conector de UI
            if (GetComponent<TargetingUIConnector>() == null)
            {
                gameObject.AddComponent<TargetingUIConnector>();
                Debug.Log("[TargetingSystem] TargetingUIConnector añadido dinámicamente");
            }

            // La cámara se asignará desde PlayerController
            Debug.Log("[TargetingSystem] Esperando asignación de cámara desde PlayerController");

            // Instanciar indicador visual si hay prefab
            if (targetIndicatorPrefab != null)
            {
                activeIndicator = Instantiate(targetIndicatorPrefab);
                activeIndicator.SetActive(false);
                DontDestroyOnLoad(activeIndicator);
                Debug.Log("[TargetingSystem] Target indicator creado desde prefab");
            }

            // Instanciar cursor cruz si hay prefab
            if (crosshairCursorPrefab != null)
            {
                activeCrosshair = Instantiate(crosshairCursorPrefab);
                activeCrosshair.SetActive(false);
                DontDestroyOnLoad(activeCrosshair);
                Debug.Log("[TargetingSystem] Crosshair creado desde prefab");
            }
            else
            {
                // Crear cursor cruz programáticamente si no hay prefab
                CreateDefaultCrosshair();
                Debug.Log("[TargetingSystem] Crosshair creado programáticamente");
            }
        }

        private void OnDestroy()
        {
            if (activeIndicator != null) Destroy(activeIndicator);
            if (activeCrosshair != null) Destroy(activeCrosshair);
        }

        /// <summary>
        /// Crea un cursor cruz por defecto si no hay prefab asignado
        /// </summary>
        private void CreateDefaultCrosshair()
        {
            Debug.Log("[TargetingSystem] CreateDefaultCrosshair - Creando cursor cruz programáticamente");

            activeCrosshair = new GameObject("CrosshairCursor");
            DontDestroyOnLoad(activeCrosshair);

            // Crear la cruz con 2 líneas usando LineRenderer (forma de +)
            // Línea horizontal (eje X)
            GameObject horizontalLine = CreateCrosshairLine("HorizontalLine", new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0));
            horizontalLine.transform.SetParent(activeCrosshair.transform);

            // Línea vertical (eje Z para vista top-down)
            GameObject verticalLine = CreateCrosshairLine("VerticalLine", new Vector3(0, 0, -0.5f), new Vector3(0, 0, 0.5f));
            verticalLine.transform.SetParent(activeCrosshair.transform);

            activeCrosshair.SetActive(false);

            Debug.Log($"[TargetingSystem] Crosshair creado: {activeCrosshair.name}, children={activeCrosshair.transform.childCount}");
        }

        private GameObject CreateCrosshairLine(string name, Vector3 start, Vector3 end)
        {
            GameObject lineObj = new GameObject(name);
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();

            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            lr.startWidth = 0.1f;
            lr.endWidth = 0.1f;
            lr.useWorldSpace = false;

            // Buscar un shader que funcione en URP
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            if (shader != null)
            {
                lr.material = new Material(shader);
                lr.material.color = Color.red;
            }
            else
            {
                Debug.LogWarning("[TargetingSystem] No se pudo encontrar shader para crosshair");
            }

            lr.startColor = Color.red;
            lr.endColor = Color.red;

            Debug.Log($"[TargetingSystem] Línea creada: {name}, shader={(shader != null ? shader.name : "NULL")}");

            return lineObj;
        }

        /// <summary>
        /// Asigna la cámara del jugador (llamado desde PlayerController)
        /// </summary>
        public void SetCamera(Camera camera)
        {
            playerCamera = camera;
            Debug.Log("[TargetingSystem] Cámara asignada correctamente");
        }

        private void Update()
        {
            // Solo el jugador local procesa input
            if (!isLocalPlayer) return;

            bool hasAbilitySelected = playerCombat != null && playerCombat.HasSelectedAbility;

            // Log cada segundo para no saturar
            if (Time.frameCount % 60 == 0 && hasAbilitySelected)
            {
                Debug.Log($"[TargetingSystem] Update - hasAbilitySelected={hasAbilitySelected}, playerCombat={playerCombat != null}, selectedIndex={playerCombat?.SelectedAbilityIndex ?? -1}");
            }

            // Click izquierdo (ignorar si está sobre UI)
            if (Input.GetMouseButtonDown(0))
            {
                // Verificar si el click está sobre un elemento de UI
                bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                Debug.Log($"[TargetingSystem] Click izquierdo detectado - hasAbilitySelected={hasAbilitySelected}, isOverUI={isOverUI}");

                if (!isOverUI)
                {
                    if (hasAbilitySelected)
                    {
                        // Modo Argentum: ejecutar habilidad en el objetivo clickeado
                        TryExecuteAbilityOnClick();
                    }
                    else
                    {
                        // Modo normal: seleccionar objetivo
                        TrySelectTarget();
                    }
                }
            }

            // ESC para limpiar objetivo (la cancelación de habilidad se maneja en PlayerCombat)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClearTarget();
            }

            // Validar que el objetivo sigue siendo válido (usar operador implícito de Unity)
            if (currentTarget && !IsTargetValid())
            {
                ClearTarget();
            }

            // Tab Targeting (solo si no hay habilidad seleccionada)
            if (Input.GetKeyDown(KeyCode.Tab) && !hasAbilitySelected)
            {
                CycleTargets();
            }

            // Actualizar posición del indicador
            UpdateIndicator();

            // Actualizar cursor cruz
            UpdateCrosshair(hasAbilitySelected);
        }

        /// <summary>
        /// Intenta ejecutar la habilidad seleccionada en el objetivo clickeado (estilo Argentum)
        /// </summary>
        private void TryExecuteAbilityOnClick()
        {
            Debug.Log($"[TargetingSystem] TryExecuteAbilityOnClick - playerCamera={playerCamera != null}, playerCombat={playerCombat != null}");

            if (playerCamera == null)
            {
                Debug.LogWarning("[TargetingSystem] No hay cámara asignada");
                return;
            }

            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Usar una máscara combinada para detectar objetivos Y el suelo/obstáculos
            LayerMask combinedMask = targetableLayers | losBlockingLayers;
            if (Physics.Raycast(ray, out hit, maxTargetingDistance, combinedMask))
            {
                Debug.Log($"[TargetingSystem] Raycast HIT: {hit.collider.gameObject.name}");
                NetworkIdentity targetIdentity = hit.collider.GetComponentInParent<NetworkIdentity>();

                if (targetIdentity != null)
                {
                    // Verificar si está vivo
                    var targetStats = targetIdentity.GetComponent<Game.Core.IEntityStats>();
                    if (targetStats != null && targetStats.CurrentHealth <= 0)
                    {
                        Debug.Log("[TargetingSystem] El objetivo está muerto");
                        return;
                    }
 
                    // Ejecutar la habilidad en el objetivo
                    Debug.Log($"[TargetingSystem] >>> EJECUTANDO habilidad en {targetIdentity.gameObject.name}");
                    playerCombat.ExecuteSelectedAbilityOnTarget(targetIdentity);
 
                    // También actualizar el target actual
                    SelectTarget(targetIdentity);
                }
                else
                {
                    Debug.Log($"[TargetingSystem] Click en objeto sin NetworkIdentity - Verificando AoE");
                    
                    // Si es AoE, permitir ejecutar en posición
                    if (playerCombat.HasSelectedAbility && playerCombat.SelectedAbility.aoeRadius > 0.1f)
                    {
                        Debug.Log("[TargetingSystem] Ejecutando AoE en posición del click");
                        playerCombat.ExecuteSelectedAbilityAtPosition(hit.point);
                    }
                    else
                    {
                        // Cancelar la habilidad si clickea en lugar inválido
                        playerCombat.CancelAbilitySelection();
                    }
                }
            }
            else
            {
                Debug.Log("[TargetingSystem] Raycast no detectó nada (vacío) - cancelando habilidad");
                playerCombat.CancelAbilitySelection();
            }
        }

        /// <summary>
        /// Intenta seleccionar un objetivo con raycast desde el mouse
        /// </summary>
        private void TrySelectTarget()
        {
            if (playerCamera == null)
            {
                Debug.LogWarning("[TargetingSystem] No hay cámara asignada");
                return;
            }

            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            Debug.Log($"[TargetingSystem] Raycast desde {ray.origin} en dirección {ray.direction}");
            Debug.Log($"[TargetingSystem] TargetableLayers: {targetableLayers.value}");

            if (Physics.Raycast(ray, out hit, maxTargetingDistance, targetableLayers))
            {
                Debug.Log($"[TargetingSystem] Raycast hit: {hit.collider.gameObject.name} (Layer: {hit.collider.gameObject.layer})");
                
                // Verificar que tiene NetworkIdentity (buscar en padres también por si el collider está en un hijo)
                NetworkIdentity targetIdentity = hit.collider.GetComponentInParent<NetworkIdentity>();
                
                if (targetIdentity != null)
                {
                    SelectTarget(targetIdentity);
                }
                else
                {
                    Debug.LogWarning($"[TargetingSystem] Objeto {hit.collider.gameObject.name} no tiene NetworkIdentity ni en padres");
                }
            }
            else
            {
                Debug.Log("[TargetingSystem] Raycast no detectó nada");
            }
        }

        /// <summary>
        /// Selecciona un objetivo
        /// </summary>
        public void SelectTarget(NetworkIdentity target)
        {
            Debug.Log($"[TargetingSystem] SelectTarget solicitado para: {target.gameObject.name}");

            if (target == currentTarget)
            {
                Debug.Log("[TargetingSystem] El objetivo ya está seleccionado (sin cambios)");
                // Aun así notificamos al UI por si se perdió la referencia
                OnTargetChanged?.Invoke(currentTarget);
                return;
            }

            currentTarget = target;
            Debug.Log($"[TargetingSystem] NUEVO Objetivo seleccionado: {target.gameObject.name}");
            
            OnTargetChanged?.Invoke(currentTarget);
        }

        /// <summary>
        /// Limpia el objetivo actual
        /// </summary>
        public void ClearTarget()
        {
            // Siempre notificar aunque el target ya esté destruido
            Debug.Log("[TargetingSystem] Objetivo limpiado");
            currentTarget = null;
            OnTargetChanged?.Invoke(null);
        }

        /// <summary>
        /// Verifica si el objetivo actual sigue siendo válido
        /// </summary>
        public bool IsTargetValid()
        {
            // Usar operador implícito de Unity para detectar objetos destruidos
            if (!currentTarget) return false;
            if (!currentTarget.gameObject.activeInHierarchy) return false;

            // Verificar si el target está muerto
            var targetStats = currentTarget.GetComponent<Game.Core.IEntityStats>();
            if (targetStats != null && targetStats.CurrentHealth <= 0) return false;

            return true;
        }

        /// <summary>
        /// Verifica si hay Line of Sight al objetivo
        /// </summary>
        public bool CanSeeTarget(Transform target)
        {
            if (target == null) return false;

            Vector3 directionToTarget = target.position - transform.position;
            float distanceToTarget = directionToTarget.magnitude;

            // Raycast desde el jugador hacia el objetivo
            // Si choca con algo antes de llegar al objetivo, no hay LoS
            if (Physics.Raycast(transform.position + Vector3.up, directionToTarget.normalized, 
                out RaycastHit hit, distanceToTarget, losBlockingLayers))
            {
                Debug.Log($"[TargetingSystem] Line of Sight bloqueado por: {hit.collider.gameObject.name}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifica si el objetivo está en rango
        /// </summary>
        public bool IsTargetInRange(float range)
        {
            if (!IsTargetValid()) return false;

            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
            return distance <= range;
        }

        /// <summary>
        /// Obtiene la distancia al objetivo actual
        /// </summary>
        public float GetDistanceToTarget()
        {
            if (!IsTargetValid()) return float.MaxValue;
            
            return Vector3.Distance(transform.position, currentTarget.transform.position);
        }

        private void CycleTargets()
        {
            // 1. Encontrar candidatos en rango
            Collider[] hits = Physics.OverlapSphere(transform.position, maxTargetingDistance, targetableLayers);
            List<NetworkIdentity> candidates = new List<NetworkIdentity>();

            foreach (var hit in hits)
            {
                NetworkIdentity identity = hit.GetComponentInParent<NetworkIdentity>();
                
                // Validaciones básicas
                if (identity == null || identity == netIdentity) continue;
                if (!identity.gameObject.activeInHierarchy) continue;

                // Validación de LoS (Requerimiento de usuario)
                if (!CanSeeTarget(identity.transform)) continue;

                // Validación de Frustum (Requerimiento de usuario: dentro de cámara)
                if (!IsTargetOnScreen(identity.transform)) continue;

                if (!candidates.Contains(identity))
                {
                    candidates.Add(identity);
                }
            }

            if (candidates.Count == 0) return;

            // 2. Ordenar por distancia
            candidates.Sort((a, b) => 
                Vector3.Distance(transform.position, a.transform.position)
                .CompareTo(Vector3.Distance(transform.position, b.transform.position)));

            // 3. Seleccionar siguiente
            int currentIndex = -1;
            if (currentTarget != null)
            {
                currentIndex = candidates.IndexOf(currentTarget);
            }

            // Si no tenemos target, o el target actual no está en la lista (se alejó o algo), empezamos por el 0
            // Si tenemos target, vamos al siguiente (cycling)
            int nextIndex = (currentIndex + 1) % candidates.Count;
            SelectTarget(candidates[nextIndex]);
        }

        private void UpdateIndicator()
        {
            if (activeIndicator == null) return;

            if (currentTarget != null && IsTargetValid())
            {
                if (!activeIndicator.activeSelf) activeIndicator.SetActive(true);

                // Seguir posición pegado al NavMesh (suelo)
                Vector3 targetPos = currentTarget.transform.position;

                // Buscar punto más cercano en NavMesh en radio de 2m
                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                {
                    activeIndicator.transform.position = hit.position + Vector3.up * indicatorYOffset;
                }
                else
                {
                    // Fallback si no encuentra NavMesh (ej: aire)
                    activeIndicator.transform.position = targetPos + Vector3.up * indicatorYOffset;
                }
            }
            else
            {
                if (activeIndicator.activeSelf) activeIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// Actualiza la posición y visibilidad del cursor cruz (estilo Argentum)
        /// </summary>
        private void UpdateCrosshair(bool hasAbilitySelected)
        {
            if (activeCrosshair == null)
            {
                // Log solo una vez
                if (Time.frameCount % 300 == 0)
                    Debug.LogWarning("[TargetingSystem] activeCrosshair es NULL!");
                return;
            }

            if (hasAbilitySelected && playerCamera != null)
            {
                // Mostrar el cursor cruz
                if (!activeCrosshair.activeSelf)
                {
                    Debug.Log("[TargetingSystem] >>> MOSTRANDO cursor cruz");
                    activeCrosshair.SetActive(true);
                }

                // Raycast para posicionar la cruz donde apunta el mouse
                Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // Primero intentar hit en objetivos válidos
                if (Physics.Raycast(ray, out hit, maxTargetingDistance, targetableLayers))
                {
                    // Posicionar sobre el objetivo
                    Vector3 targetPos = hit.collider.GetComponentInParent<Transform>().position;

                    if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
                    {
                        activeCrosshair.transform.position = navHit.position + Vector3.up * (indicatorYOffset + 0.05f);
                    }
                    else
                    {
                        activeCrosshair.transform.position = targetPos + Vector3.up * (indicatorYOffset + 0.05f);
                    }

                    // Cambiar color a verde si el objetivo es válido
                    SetCrosshairColor(Color.green);
                }
                else
                {
                    // Hit en el suelo o cualquier otra superficie
                    LayerMask groundLayer = LayerMask.GetMask("Ground", "Default");
                    if (Physics.Raycast(ray, out hit, maxTargetingDistance, groundLayer))
                    {
                        activeCrosshair.transform.position = hit.point + Vector3.up * indicatorYOffset;
                    }
                    else
                    {
                        // Fallback: posicionar a cierta distancia del jugador
                        activeCrosshair.transform.position = transform.position + transform.forward * 3f + Vector3.up * indicatorYOffset;
                    }

                    // Color rojo cuando no hay objetivo válido
                    SetCrosshairColor(Color.red);
                }

                // Rotar para que siempre mire hacia arriba (plano XZ)
                activeCrosshair.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                // Ocultar el cursor cruz
                if (activeCrosshair.activeSelf)
                {
                    Debug.Log($"[TargetingSystem] >>> OCULTANDO cursor cruz - hasAbilitySelected={hasAbilitySelected}, playerCamera={playerCamera != null}");
                    activeCrosshair.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Cambia el color del cursor cruz
        /// </summary>
        private void SetCrosshairColor(Color color)
        {
            if (activeCrosshair == null) return;

            LineRenderer[] lines = activeCrosshair.GetComponentsInChildren<LineRenderer>();
            foreach (var lr in lines)
            {
                lr.startColor = color;
                lr.endColor = color;
            }
        }

        private void OnDrawGizmos()
        {
            // Dibujar línea al objetivo en el editor
            if (currentTarget != null && Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position + Vector3.up, currentTarget.transform.position + Vector3.up);
            }
        }

        /// <summary>
        /// Verifica si el objetivo está dentro de la pantalla (Frustum Culling manual)
        /// </summary>
        private bool IsTargetOnScreen(Transform target)
        {
            if (playerCamera == null) return false;

            Vector3 screenPoint = playerCamera.WorldToViewportPoint(target.position);
            
            // z > 0: está frente a la cámara
            // x, y entre 0 y 1: está dentro del viewport
            bool onScreen = screenPoint.z > 0 && 
                            screenPoint.x > 0 && screenPoint.x < 1 && 
                            screenPoint.y > 0 && screenPoint.y < 1;
                            
            return onScreen;
        }
    }
}
