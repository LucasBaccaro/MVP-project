using UnityEngine;
using UnityEngine.UIElements;
using Game.Quests;

namespace Game.UI
{
    /// <summary>
    /// Quest Log UI - Diario de quests (toggle con tecla J)
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class QuestLogUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement panel;
        private Label contentLabel;
        private Button closeButton;

        private void Start()
        {
            Debug.Log("[QuestLogUI] Start() called");

            // Obtener el documento UI y buscar los elementos
            uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError("[QuestLogUI] UIDocument component not found!");
                return;
            }

            var root = uiDocument.rootVisualElement;
            Debug.Log($"[QuestLogUI] Root element found: {root != null}");

            // Query elementos
            panel = root.Q<VisualElement>("quest-log-panel");
            contentLabel = root.Q<Label>("quest-log-content");
            closeButton = root.Q<Button>("quest-log-close-btn");

            Debug.Log($"[QuestLogUI] Panel found: {panel != null}, ContentLabel: {contentLabel != null}, CloseButton: {closeButton != null}");

            // Verificar que todos los elementos se encontraron
            if (panel == null || contentLabel == null)
            {
                Debug.LogError("[QuestLogUI] No se pudieron encontrar todos los elementos UI en el UXML!");
                Debug.LogError($"[QuestLogUI] Panel null: {panel == null}, ContentLabel null: {contentLabel == null}");
            }

            // Suscribirse al botón de cerrar
            if (closeButton != null)
            {
                closeButton.clicked += Close;
            }

            // Ocultar panel al inicio
            if (panel != null)
            {
                panel.style.display = DisplayStyle.None;
                Debug.Log("[QuestLogUI] Panel hidden at start");
            }
        }


        /// <summary>
        /// Toggle del panel (abrir/cerrar)
        /// </summary>
        public void Toggle()
        {
            if (panel == null)
            {
                Debug.LogError("[QuestLogUI] Toggle() called but panel is null!");
                return;
            }

            if (panel.style.display == DisplayStyle.None)
            {
                panel.style.display = DisplayStyle.Flex;
                Debug.Log("[QuestLogUI] Panel opened (display: flex)");
            }
            else
            {
                panel.style.display = DisplayStyle.None;
                Debug.Log("[QuestLogUI] Panel closed (display: none)");
            }
        }

        /// <summary>
        /// Cerrar el panel
        /// </summary>
        public void Close()
        {
            if (panel != null)
            {
                panel.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Actualiza el contenido del log con las quests activas
        /// </summary>
        public void UpdateLog(QuestStatus[] activeQuests)
        {
            if (contentLabel == null) return;

            // Simple text dump for MVP
            string t = "";
            foreach(var q in activeQuests)
            {
                QuestData questData = q.GetQuestData();
                if (questData == null) continue;

                t += $"<color=orange><b>{questData.questTitle}</b></color>\n";
                t += $"{questData.questDescription}\n";
                t += "<b>Objetivos:</b>\n";
                foreach(var obj in questData.objectives)
                {
                    // MVP assumes single objective tracking for now, but looping for future proof
                    t += $"- {obj.targetName}: {q.currentAmount}/{obj.requiredAmount}\n";
                }
                t += $"Rewards: {questData.xpReward} XP, {questData.goldReward} Gold\n\n";
            }
            contentLabel.text = t;
        }

        private void OnDisable()
        {
            // Cleanup
            if (closeButton != null)
            {
                closeButton.clicked -= Close;
            }
        }
    }
}
