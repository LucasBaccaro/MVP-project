using Mirror;
using UnityEngine;
using UnityEngine.AI; // Added for NavMesh.SamplePosition
using System;
using System.Collections.Generic; // Added for List
using Game.Player; // Necesario para TargetingUIConnector

namespace Game.Combat
{
    /// <summary>
    /// Sistema de selección de objetivos mediante raycast
    /// Maneja targeting de jugadores y enemigos
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
        // Eventos
        public event Action<NetworkIdentity> OnTargetChanged;

        [Header("Visuals")]
        [Tooltip("Prefab del indicador (círculo rojo)")]
        public GameObject targetIndicatorPrefab;
        [Tooltip("Offset vertical del indicador desde el suelo")]
        public float indicatorYOffset = 0.1f;

        private GameObject activeIndicator;
        private Camera playerCamera;

        private void Start()
        {
            // Solo el jugador local puede hacer targeting
            if (!isLocalPlayer) return;

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
                // No lo hacemos hijo para evitar problemas de escala/rotación con el target
                DontDestroyOnLoad(activeIndicator); // O mejor, manejar limpieza manual
            }
        }

        private void OnDestroy()
        {
            if (activeIndicator != null) Destroy(activeIndicator);
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

            // Click izquierdo para seleccionar objetivo
            if (Input.GetMouseButtonDown(0))
            {
                TrySelectTarget();
            }

            // ESC para limpiar objetivo
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClearTarget();
            }

            // Validar que el objetivo sigue siendo válido
            if (currentTarget != null && !IsTargetValid())
            {
                ClearTarget();
            }

            // Tab Targeting
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                CycleTargets();
            }

            // Actualizar posición del indicador
            UpdateIndicator();
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
                    // No podemos targetearnos a nosotros mismos
                    if (targetIdentity == netIdentity)
                    {
                        Debug.Log("[TargetingSystem] Click en self (ignorado)");
                        return;
                    }

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
            if (currentTarget == null) return;

            Debug.Log("[TargetingSystem] Objetivo limpiado");
            currentTarget = null;
            OnTargetChanged?.Invoke(null);
        }

        /// <summary>
        /// Verifica si el objetivo actual sigue siendo válido
        /// </summary>
        public bool IsTargetValid()
        {
            if (currentTarget == null) return false;
            if (currentTarget.gameObject == null) return false;
            if (!currentTarget.gameObject.activeInHierarchy) return false;
            
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
