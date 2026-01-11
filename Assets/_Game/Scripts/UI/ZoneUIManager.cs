using UnityEngine;
using TMPro;

namespace Game.Player
{
    /// <summary>
    /// Maneja la UI que muestra si el jugador está en zona segura o insegura
    /// </summary>
    public class ZoneUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Texto que muestra el estado de la zona")]
        public TMP_Text zoneStatusText;

        [Header("Settings")]
        [Tooltip("Texto mostrado en zona segura")]
        public string safeZoneText = "ZONA SEGURA";

        [Tooltip("Texto mostrado en zona insegura")]
        public string unsafeZoneText = "ZONA PELIGROSA";

        [Tooltip("Color del texto en zona segura")]
        public Color safeZoneColor = Color.green;

        [Tooltip("Color del texto en zona insegura")]
        public Color unsafeZoneColor = Color.red;

        [Header("Animation")]
        [Tooltip("Duración del efecto de fade cuando cambia de zona (segundos)")]
        public float fadeDuration = 0.5f;

        private bool isInSafeZone = false;
        private float fadeTimer = 0f;
        private bool isFading = false;

        private void Start()
        {
            // Inicializar en zona insegura por defecto
            UpdateZoneStatus(false);
        }

        /// <summary>
        /// Actualiza el estado de la zona en el UI
        /// </summary>
        public void UpdateZoneStatus(bool isSafe)
        {
            if (zoneStatusText == null)
            {
                Debug.LogWarning("[ZoneUIManager] zoneStatusText no está asignado!");
                return;
            }

            isInSafeZone = isSafe;

            // Actualizar texto y color
            zoneStatusText.text = isSafe ? safeZoneText : unsafeZoneText;
            zoneStatusText.color = isSafe ? safeZoneColor : unsafeZoneColor;

            // Iniciar efecto de fade/pulse
            StartFadeEffect();

            Debug.Log($"[ZoneUIManager] UI actualizado: {(isSafe ? "SEGURA" : "PELIGROSA")}");
        }

        private void StartFadeEffect()
        {
            isFading = true;
            fadeTimer = fadeDuration;
        }

        private void Update()
        {
            // Efecto de fade al cambiar de zona
            if (isFading && zoneStatusText != null)
            {
                fadeTimer -= Time.deltaTime;

                // Pulsar la escala del texto
                float scale = 1f + Mathf.Sin(fadeTimer / fadeDuration * Mathf.PI) * 0.2f;
                zoneStatusText.transform.localScale = Vector3.one * scale;

                if (fadeTimer <= 0f)
                {
                    isFading = false;
                    zoneStatusText.transform.localScale = Vector3.one;
                }
            }
        }

        /// <summary>
        /// Obtiene el estado actual de la zona
        /// </summary>
        public bool IsInSafeZone()
        {
            return isInSafeZone;
        }
    }
}
