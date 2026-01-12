using Mirror;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Controlador principal del jugador
    /// Maneja el movimiento WASD y la cámara del jugador local
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Prefab de la cámara (debe tener componente Camera y CameraFollow)")]
        public GameObject cameraPrefab;

        public CharacterController characterController;

        [Header("Runtime References (No asignar manualmente)")]
        [Tooltip("Referencia a la cámara instanciada en runtime")]
        private Camera playerCamera;
        private CameraFollow cameraFollow;

        [Header("Settings")]
        [Tooltip("Velocidad de movimiento del jugador")]
        public float speed = 5f;

        [Tooltip("Velocidad de rotación del jugador")]
        public float rotationSpeed = 10f;

        private void Awake()
        {
            // Obtener referencias si no están asignadas
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
        }

        public override void OnStartLocalPlayer()
        {
            Debug.Log($"[PlayerController] OnStartLocalPlayer llamado para: {gameObject.name}");

            // Instanciar la cámara para el jugador local
            if (cameraPrefab != null)
            {
                GameObject cameraObj = Instantiate(cameraPrefab);
                cameraObj.name = "PlayerCamera";

                playerCamera = cameraObj.GetComponent<Camera>();
                cameraFollow = cameraObj.GetComponent<CameraFollow>();

                if (cameraFollow != null)
                {
                    cameraFollow.SetTarget(transform);
                    Debug.Log("[PlayerController] Cámara instanciada y configurada con CameraFollow");
                }
                else
                {
                    Debug.LogError("[PlayerController] El prefab de cámara no tiene componente CameraFollow!");
                }

                // Pasar la cámara al TargetingSystem
                var targetingSystem = GetComponent<Game.Combat.TargetingSystem>();
                if (targetingSystem != null && playerCamera != null)
                {
                    targetingSystem.SetCamera(playerCamera);
                    Debug.Log("[PlayerController] Cámara asignada a TargetingSystem");
                }
            }
            else
            {
                Debug.LogError("[PlayerController] cameraPrefab es NULL! Asigna el prefab de cámara en el Inspector.");
            }

            Debug.Log($"[PlayerController] Jugador local iniciado: {gameObject.name}");
        }

        private void Start()
        {
            // El color de clase se aplica desde PlayerStats
        }



        private void Update()
        {
            if (!isLocalPlayer) return; // Solo procesar input local

            HandleMovement();
            HandleInteraction();
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical   = Input.GetAxis("Vertical");

            // 1) Dirección SOLO para rotación (plano XZ)
            Vector3 inputDir = new Vector3(horizontal, 0f, vertical);

            // 2) Movimiento: SimpleMove ya aplica gravedad, NO le metas Y
            characterController.SimpleMove(inputDir * speed);

            // 3) Rotar solo si hay input y SOLO en Y
            if (inputDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(inputDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        /// <summary>
        /// Maneja la interacción con objetos en el mundo (Loot)
        /// </summary>
        private void HandleInteraction()
        {
            // Click derecho para interactuar
            if (Input.GetMouseButtonDown(1))
            {
                if (playerCamera == null) return;

                Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
                // Usamos un layer mask para detectar objetos interactuables o default
                // Se asume que LootBag tiene collider
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    // Verificar si es un LootBag
                    // Usamos GetComponentInParent por si clickeamos en una parte visual hija
                    Game.Items.LootBag lootBag = hit.collider.GetComponentInParent<Game.Items.LootBag>();
                    
                    if (lootBag != null)
                    {
                        Debug.Log($"[PlayerController] LootBag clickeado. Buscando LootUI...");
                        
                         // Buscar LootUI en la escena (incluso inactivo)
                        var lootUI = FindFirstObjectByType<Game.UI.LootUI>(FindObjectsInactive.Include);
                        if (lootUI != null)
                        {
                            Debug.Log($"[PlayerController] LootUI encontrado: {lootUI.name}. Abriendo...");
                            lootUI.Open(lootBag);
                        }
                        else
                        {
                            Debug.LogError("[PlayerController] ¡LootUI no encontrado en la escena! No se puede lootear.");
                            // lootBag.CmdClaimLoot(); // REMOVIDO: Solo permitir loot vía UI
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            // Limpiar la cámara cuando el jugador se destruye
            if (isLocalPlayer && playerCamera != null)
            {
                Destroy(playerCamera.gameObject);
                Debug.Log("[PlayerController] Cámara destruida con el jugador");
            }
        }
    }
}
