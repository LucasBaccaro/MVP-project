using Mirror;
using UnityEngine;
using System;
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
        public event Action<NetworkIdentity> OnTargetChanged;

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

        private void OnDrawGizmos()
        {
            // Dibujar línea al objetivo en el editor
            if (currentTarget != null && Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position + Vector3.up, currentTarget.transform.position + Vector3.up);
            }
        }
    }
}
