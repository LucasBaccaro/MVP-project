using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using Game.Player;

namespace Game.UI
{
    /// <summary>
    /// UI que muestra información del objetivo seleccionado
    /// </summary>
    public class TargetFrameUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Panel principal (se oculta cuando no hay objetivo)")]
        public GameObject targetPanel;
        
        [Tooltip("Texto del nombre del objetivo")]
        public TextMeshProUGUI targetNameText;
        
        [Tooltip("Texto de la clase del objetivo")]
        public TextMeshProUGUI targetClassText;
        
        [Tooltip("Barra de vida del objetivo")]
        public Image healthBar;
        
        [Tooltip("Texto de HP (100/150)")]
        public TextMeshProUGUI healthText;

        private NetworkIdentity currentTarget;
        private PlayerStats currentTargetStats;

        private void Start()
        {
            // Ocultar panel al inicio
            if (targetPanel != null)
            {
                targetPanel.SetActive(false);
            }
        }

        private void Update()
        {
            // Actualizar UI del objetivo cada frame
            if (currentTarget != null && currentTargetStats != null)
            {
                UpdateTargetUI();
            }
        }

        /// <summary>
        /// Establece el objetivo a mostrar
        /// </summary>
        /// <summary>
        /// Establece el objetivo a mostrar
        /// </summary>
        public void SetTarget(NetworkIdentity target)
        {
            if (target == null)
            {
                ClearTarget();
                return;
            }

            currentTarget = target;
            currentTargetStats = target.GetComponent<PlayerStats>();

            if (currentTargetStats == null)
            {
                Debug.LogWarning("[TargetFrameUI] El objetivo no tiene PlayerStats");
                ClearTarget();
                return;
            }

            // Mostrar panel
            if (targetPanel != null)
            {
                targetPanel.SetActive(true);
                Debug.Log($"[TargetFrameUI] Panel activado para: {target.gameObject.name}");
            }
            else
            {
                Debug.LogError("[TargetFrameUI] targetPanel reference is NULL!");
            }

            // Actualizar nombre y clase
            if (targetNameText != null)
            {
                targetNameText.text = target.gameObject.name;
            }

            if (targetClassText != null)
            {
                targetClassText.text = currentTargetStats.className;
            }

            UpdateTargetUI();
        }

        /// <summary>
        /// Limpia el objetivo actual
        /// </summary>
        public void ClearTarget()
        {
            currentTarget = null;
            currentTargetStats = null;

            if (targetPanel != null)
            {
                targetPanel.SetActive(false);
                Debug.Log("[TargetFrameUI] Panel desactivado (ClearTarget)");
            }
        }

        /// <summary>
        /// Actualiza la barra de vida y texto
        /// </summary>
        private void UpdateTargetUI()
        {
            if (currentTargetStats == null) return;

            // Actualizar barra de vida
            if (healthBar != null)
            {
                float healthPercent = (float)currentTargetStats.currentHealth / currentTargetStats.maxHealth;
                healthBar.fillAmount = healthPercent;
            }

            // Actualizar texto de HP
            if (healthText != null)
            {
                healthText.text = $"{currentTargetStats.currentHealth}/{currentTargetStats.maxHealth}";
            }
        }
    }
}
