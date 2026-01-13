using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Player
{
    /// <summary>
    /// Maneja la UI que muestra si el jugador está en zona segura o insegura
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ZoneUIManager : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Texto mostrado en zona segura")]
        public string safeZoneText = "ZONA SEGURA";

        [Tooltip("Texto mostrado en zona insegura")]
        public string unsafeZoneText = "ZONA PELIGROSA";

        [Header("Animation")]
        [Tooltip("Duración del efecto de pulso cuando cambia de zona (milisegundos)")]
        public int pulseDurationMs = 500;

        private UIDocument uiDocument;
        private Label zoneStatusLabel;
        private bool isInSafeZone = false;
        private IVisualElementScheduledItem pulseSchedule;

        private void Start()
        {
            // Obtener el documento UI y buscar el elemento
            uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError("[ZoneUIManager] UIDocument component not found!");
                return;
            }

            var root = uiDocument.rootVisualElement;

            // Query el label por nombre
            zoneStatusLabel = root.Q<Label>("zone-status-text");

            if (zoneStatusLabel == null)
            {
                Debug.LogError("[ZoneUIManager] No se pudo encontrar 'zone-status-text' en el UXML!");
            }

            // Inicializar en zona insegura por defecto
            UpdateZoneStatus(false);
        }

        /// <summary>
        /// Actualiza el estado de la zona en el UI
        /// </summary>
        public void UpdateZoneStatus(bool isSafe)
        {
            if (zoneStatusLabel == null)
            {
                Debug.LogWarning("[ZoneUIManager] zoneStatusLabel no está inicializado!");
                return;
            }

            isInSafeZone = isSafe;

            // Actualizar texto
            zoneStatusLabel.text = isSafe ? safeZoneText : unsafeZoneText;

            // Actualizar clases CSS (cambiar color)
            if (isSafe)
            {
                zoneStatusLabel.RemoveFromClassList("zone__text--unsafe");
                zoneStatusLabel.AddToClassList("zone__text--safe");
            }
            else
            {
                zoneStatusLabel.RemoveFromClassList("zone__text--safe");
                zoneStatusLabel.AddToClassList("zone__text--unsafe");
            }

            // Iniciar efecto de pulso
            StartPulseEffect();

            Debug.Log($"[ZoneUIManager] UI actualizado: {(isSafe ? "SEGURA" : "PELIGROSA")}");
        }

        private void StartPulseEffect()
        {
            // Cancelar pulso anterior si existe
            if (pulseSchedule != null)
            {
                pulseSchedule.Pause();
            }

            // Añadir clase de pulso
            zoneStatusLabel.AddToClassList("zone__text--pulsing");

            // Remover clase después de la duración (CSS transitions manejan la animación)
            pulseSchedule = zoneStatusLabel.schedule.Execute(() =>
            {
                zoneStatusLabel.RemoveFromClassList("zone__text--pulsing");
            }).StartingIn(pulseDurationMs);
        }

        /// <summary>
        /// Obtiene el estado actual de la zona
        /// </summary>
        public bool IsInSafeZone()
        {
            return isInSafeZone;
        }

        private void OnDisable()
        {
            // Limpiar el schedule al desactivar
            if (pulseSchedule != null)
            {
                pulseSchedule.Pause();
            }
        }
    }
}
