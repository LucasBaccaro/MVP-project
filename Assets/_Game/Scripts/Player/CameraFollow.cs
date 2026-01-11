using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Sistema de cámara que sigue al jugador de forma suave
    /// Típico de Action RPGs (Diablo, Path of Exile, etc.)
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("El transform del jugador a seguir")]
        public Transform target;

        [Header("Camera Settings")]
        [Tooltip("Offset de la cámara respecto al jugador")]
        public Vector3 offset = new Vector3(0, 15, -10);

        [Tooltip("Velocidad de seguimiento (mayor = más rápido)")]
        [Range(1f, 20f)]
        public float followSpeed = 10f;

        [Tooltip("Velocidad de rotación de la cámara")]
        [Range(1f, 20f)]
        public float rotationSpeed = 5f;

        [Header("Look At")]
        [Tooltip("Si la cámara debe mirar siempre al jugador")]
        public bool lookAtTarget = true;

        [Tooltip("Offset vertical del punto donde mira la cámara")]
        public float lookAtYOffset = 1f;

        private void LateUpdate()
        {
            if (target == null) return;

            // Calcular posición deseada
            Vector3 desiredPosition = target.position + offset;

            // Interpolar suavemente hacia la posición deseada
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSpeed * Time.deltaTime
            );

            // Mirar hacia el target si está habilitado
            if (lookAtTarget)
            {
                Vector3 lookAtPosition = target.position + Vector3.up * lookAtYOffset;
                Quaternion desiredRotation = Quaternion.LookRotation(lookAtPosition - transform.position);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        /// <summary>
        /// Establece el target a seguir
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;

            // Posicionar inmediatamente la cámara sin interpolación
            if (target != null)
            {
                transform.position = target.position + offset;

                if (lookAtTarget)
                {
                    Vector3 lookAtPosition = target.position + Vector3.up * lookAtYOffset;
                    transform.LookAt(lookAtPosition);
                }
            }
        }
    }
}
