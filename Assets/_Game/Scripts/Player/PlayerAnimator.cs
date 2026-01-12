using Mirror;
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NetworkAnimator))]
    public class PlayerAnimator : NetworkBehaviour
    {
        [Header("Referencias")]
        public Animator animator;
        public CharacterController characterController;
        // Referencia al componente de red para asegurar el trigger
        private NetworkAnimator networkAnimator;

        [Header("Nombres de Parámetros")]
        public string speedParameterName = "Speed";
        public string isMovingParameterName = "IsMoving";
        public string attackTriggerName = "Attack"; // Nuevo parámetro

        [Header("Configuración")]
        public float movementThreshold = 0.1f;
        public float dampTime = 0.1f;

        // Hashes para optimización
        private int speedHash;
        private int isMovingHash;
        private int attackHash;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
            networkAnimator = GetComponent<NetworkAnimator>();

            // Convertimos nombres a IDs numéricos para que Unity sea más rápido
            speedHash = Animator.StringToHash(speedParameterName);
            isMovingHash = Animator.StringToHash(isMovingParameterName);
            attackHash = Animator.StringToHash(attackTriggerName);
        }

        private void Update()
        {
            // Solo procesamos input y lógica si somos el jugador local
            if (!isLocalPlayer) return;

            UpdateLocomotion();
            HandleInput();
        }

        private void HandleInput()
        {
            // Si aprieta el 1
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                // Disparamos el trigger.
                // Al tener NetworkAnimator con ClientAuthority, esto se enviará automáticamente.
                // NOTA: Para triggers, a veces es más seguro usar networkAnimator.SetTrigger(attackHash)
                // si notas que a veces falla, pero animator.SetTrigger suele bastar.
                // animator.SetTrigger(attackHash);
                
                // Opción ultra-segura (descomentar si la de arriba falla en red):
                networkAnimator.SetTrigger(attackHash); 
            }
        }

        private void UpdateLocomotion()
        {
            Vector3 velocity = characterController.velocity;
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            float speed = horizontalVelocity.magnitude;

            bool isMoving = speed > movementThreshold;

            animator.SetFloat(speedHash, speed, dampTime, Time.deltaTime);
            animator.SetBool(isMovingHash, isMoving);
        }
    }
}