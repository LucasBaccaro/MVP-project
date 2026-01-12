using UnityEngine;
using Game.Combat;
using Game.UI;

namespace Game.Player
{
    /// <summary>
    /// Conecta el TargetingSystem con el TargetFrameUI
    /// </summary>
    [RequireComponent(typeof(TargetingSystem))]
    public class TargetingUIConnector : MonoBehaviour
    {
        private TargetingSystem targetingSystem;
        private TargetFrameUI targetFrameUI;

        private void Start()
        {
            targetingSystem = GetComponent<TargetingSystem>();
            
            // Buscar el TargetFrameUI en la escena (incluso si está desactivado)
            targetFrameUI = FindFirstObjectByType<TargetFrameUI>(FindObjectsInactive.Include);

            if (targetFrameUI == null)
            {
                Debug.LogWarning("[TargetingUIConnector] No se encontró TargetFrameUI en la escena");
                return;
            }
            else
            {
                Debug.Log($"[TargetingUIConnector] Conectado a {targetFrameUI.gameObject.name}");
            }

            // Suscribirse al evento de cambio de objetivo
            targetingSystem.OnTargetChanged += OnTargetChanged;

            // Sincronizar estado inicial (si ya hay un target seleccionado)
            if (targetingSystem.currentTarget != null)
            {
                Debug.Log($"[TargetingUIConnector] Sincronizando target inicial: {targetingSystem.currentTarget.name}");
                OnTargetChanged(targetingSystem.currentTarget);
            }
        }

        private void OnTargetChanged(Mirror.NetworkIdentity newTarget)
        {
            if (targetFrameUI != null)
            {
                targetFrameUI.SetTarget(newTarget);
            }
        }

        private void OnDestroy()
        {
            if (targetingSystem != null)
            {
                targetingSystem.OnTargetChanged -= OnTargetChanged;
            }
        }
    }
}
