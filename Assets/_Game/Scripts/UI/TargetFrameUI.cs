using UnityEngine;
using UnityEngine.UIElements;
using Mirror;
using Game.Player;
using Game.Core;

namespace Game.UI
{
    /// <summary>
    /// UI de Objetivo - Versión para UI TOOLKIT (TargetFrame_Toolkit)
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TargetFrameUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement targetPanel;
        private Label targetNameLabel;
        private Label targetClassLabel;
        private ProgressBar healthBar;
        private Label healthTextLabel;

        private NetworkIdentity currentTarget;
        private IEntityStats currentTargetStats;
        private bool isInitialized = false;

        private void Awake()
        {
            // Intentamos inicializar lo antes posible
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (isInitialized) return;

            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            // IMPORTANTE: Estos nombres deben coincidir con el UXML de TargetFrame_Toolkit
            targetPanel = root.Q<VisualElement>("target-panel");
            targetNameLabel = root.Q<Label>("target-name-text");
            targetClassLabel = root.Q<Label>("target-class-text");
            healthBar = root.Q<ProgressBar>("target-health-bar");
            healthTextLabel = root.Q<Label>("target-health-text");

            if (targetPanel != null)
            {
                // Empezamos con el panel oculto mediante estilos, no desactivando el objeto
                targetPanel.style.display = DisplayStyle.None;
                isInitialized = true;
                Debug.Log("[TargetFrame_Toolkit] UI Toolkit inicializado correctamente.");
            }
        }

        public void SetTarget(NetworkIdentity target)
        {
            if (!isInitialized) EnsureInitialized();

            if (target == null)
            {
                ClearTarget();
                return;
            }

            currentTarget = target;
            currentTargetStats = target.GetComponent<IEntityStats>();

            if (currentTargetStats == null)
            {
                ClearTarget();
                return;
            }

            // Hacemos visible el panel
            if (targetPanel != null)
                targetPanel.style.display = DisplayStyle.Flex;

            // Actualizar textos básicos
            if (targetNameLabel != null) targetNameLabel.text = currentTargetStats.EntityName;
            if (targetClassLabel != null) targetClassLabel.text = currentTargetStats.ClassName;

            UpdateTargetUI();
        }

        public void ClearTarget()
        {
            currentTarget = null;
            currentTargetStats = null;
            if (targetPanel != null)
                targetPanel.style.display = DisplayStyle.None;
        }

        private void Update()
        {
            // Verificar si el target fue destruido (Unity overload del operador ==)
            if (!currentTarget)
            {
                if (targetPanel != null && targetPanel.style.display == DisplayStyle.Flex)
                    ClearTarget();
                return;
            }

            if (currentTargetStats != null)
            {
                if (currentTargetStats.CurrentHealth <= 0)
                    ClearTarget();
                else
                    UpdateTargetUI();
            }
        }

        private void UpdateTargetUI()
        {
            if (currentTargetStats == null) return;

            if (healthBar != null)
            {
                healthBar.highValue = currentTargetStats.MaxHealth;
                healthBar.value = currentTargetStats.CurrentHealth;
            }

            if (healthTextLabel != null)
            {
                healthTextLabel.text = $"{currentTargetStats.CurrentHealth} / {currentTargetStats.MaxHealth}";
            }
        }
    }
}