using UnityEngine;
using UnityEngine.UIElements;
using Game.Quests;
using Game.Player;

namespace Game.UI
{
    /// <summary>
    /// Quest Giver UI - Diálogo de NPC con 5 estados (Nueva, EnProgreso, Completa, Bloqueada, SinQuests)
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class QuestGiverUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement panel;
        private Label titleLabel;
        private Label descriptionLabel;
        private Label rewardsLabel;
        private Label statusLabel;
        private Label blockedReasonLabel;

        // Botones
        private Button acceptButton;
        private Button declineButton;
        private Button completeButton;
        private Button closeButton;

        // Estado actual
        private QuestGiver currentNpc;
        private QuestData currentQuest;
        private PlayerQuests playerQuests;
        private bool isBlocked;

        private void Start()
        {
            // Obtener el documento UI y buscar los elementos
            uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError("[QuestGiverUI] UIDocument component not found!");
                return;
            }

            var root = uiDocument.rootVisualElement;

            // Query elementos
            panel = root.Q<VisualElement>("quest-giver-panel");
            titleLabel = root.Q<Label>("quest-title");
            descriptionLabel = root.Q<Label>("quest-description");
            rewardsLabel = root.Q<Label>("quest-rewards");
            statusLabel = root.Q<Label>("quest-status");
            blockedReasonLabel = root.Q<Label>("quest-blocked-reason");

            // Query botones
            acceptButton = root.Q<Button>("btn-accept");
            declineButton = root.Q<Button>("btn-decline");
            completeButton = root.Q<Button>("btn-complete");
            closeButton = root.Q<Button>("btn-close");

            // Verificar que todos los elementos se encontraron
            if (panel == null || titleLabel == null || descriptionLabel == null ||
                rewardsLabel == null || statusLabel == null)
            {
                Debug.LogError("[QuestGiverUI] No se pudieron encontrar todos los elementos UI en el UXML!");
            }

            // Suscribirse a eventos de botones
            if (acceptButton != null) acceptButton.clicked += OnAcceptButton;
            if (declineButton != null) declineButton.clicked += OnDeclineButton;
            if (completeButton != null) completeButton.clicked += OnCompleteButton;
            if (closeButton != null) closeButton.clicked += OnCloseButton;

            // Ocultar panel al inicio
            if (panel != null) panel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Abre el panel y decide qué mostrar según el estado de la quest
        /// </summary>
        /// <param name="blocked">True si la quest está bloqueada por nivel</param>
        public void Open(QuestGiver npc, QuestData quest, GameObject player, bool blocked)
        {
            currentNpc = npc;
            currentQuest = quest;
            isBlocked = blocked;

            // Obtener componente de quests del jugador
            playerQuests = player.GetComponent<PlayerQuests>();
            if (playerQuests == null)
            {
                Debug.LogError("[QuestGiverUI] Player doesn't have PlayerQuests component!");
                return;
            }

            // Datos básicos de la quest
            titleLabel.text = quest.questTitle;
            descriptionLabel.text = quest.questDescription;
            rewardsLabel.text = $"<color=yellow>Recompensas:</color>\n{quest.xpReward} XP\n{quest.goldReward} Oro";

            // Determinar estado y botones
            if (blocked)
            {
                ShowBlockedState(quest);
            }
            else
            {
                ShowNormalState(quest);
            }

            panel.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Abre el panel cuando no hay más quests disponibles (fin de cadena)
        /// </summary>
        public void OpenNoQuests(QuestGiver npc, GameObject player)
        {
            currentNpc = npc;
            currentQuest = null;
            isBlocked = false;

            playerQuests = player.GetComponent<PlayerQuests>();

            // Mostrar mensaje de "no más quests"
            titleLabel.text = npc.npcName;
            descriptionLabel.text = "Has completado todas las quests que tengo para ti por ahora. ¡Sigue entrenando y vuelve pronto, aventurero!";
            rewardsLabel.text = "";
            statusLabel.text = "<color=gray>Sin quests disponibles</color>";

            // Ocultar mensaje de bloqueo si existe
            if (blockedReasonLabel != null)
            {
                blockedReasonLabel.style.display = DisplayStyle.None;
            }

            // Solo botón cerrar
            acceptButton.style.display = DisplayStyle.None;
            declineButton.style.display = DisplayStyle.None;
            completeButton.style.display = DisplayStyle.None;
            closeButton.style.display = DisplayStyle.Flex;

            panel.style.display = DisplayStyle.Flex;

            Debug.Log("[QuestGiverUI] Showing 'No quests available' message.");
        }

        /// <summary>
        /// Muestra el estado de quest bloqueada por nivel
        /// </summary>
        private void ShowBlockedState(QuestData quest)
        {
            statusLabel.text = $"<color=red>Bloqueada - Requiere nivel {quest.requiredLevel}</color>";

            if (blockedReasonLabel != null)
            {
                blockedReasonLabel.text = $"Vuelve cuando seas nivel {quest.requiredLevel}";
                blockedReasonLabel.style.display = DisplayStyle.Flex;
            }

            // Solo botón cerrar
            acceptButton.style.display = DisplayStyle.None;
            declineButton.style.display = DisplayStyle.None;
            completeButton.style.display = DisplayStyle.None;
            closeButton.style.display = DisplayStyle.Flex;

            Debug.Log($"[QuestGiverUI] Quest '{quest.questTitle}' está bloqueada (requiere nivel {quest.requiredLevel})");
        }

        /// <summary>
        /// Muestra el estado normal de quest (Nueva, En Progreso, Completa)
        /// </summary>
        private void ShowNormalState(QuestData quest)
        {
            // Ocultar mensaje de bloqueo si existe
            if (blockedReasonLabel != null)
            {
                blockedReasonLabel.style.display = DisplayStyle.None;
            }

            string questID = quest.name;

            // IMPORTANTE: Acceder a la SyncList LOCAL del jugador
            int questIndex = GetLocalQuestIndex(questID);
            bool hasQuest = questIndex != -1;
            bool isComplete = hasQuest && IsLocalQuestComplete(questIndex);

            Debug.Log($"[QuestGiverUI] Quest '{quest.questTitle}' - hasQuest: {hasQuest}, isComplete: {isComplete}, index: {questIndex}");

            if (isComplete)
            {
                // CASO 1: Quest completa - Mostrar botón de entregar
                statusLabel.text = "<color=green>¡Completa!</color>";
                acceptButton.style.display = DisplayStyle.None;
                declineButton.style.display = DisplayStyle.None;
                completeButton.style.display = DisplayStyle.Flex;
                closeButton.style.display = DisplayStyle.Flex;
            }
            else if (hasQuest)
            {
                // CASO 2: Quest en progreso - Mostrar recordatorio
                QuestStatus qs = playerQuests.activeQuests[questIndex];
                QuestData questData = qs.GetQuestData();
                int current = qs.currentAmount;
                int required = (questData != null && questData.objectives.Count > 0) ? questData.objectives[0].requiredAmount : 0;

                statusLabel.text = $"<color=orange>En Progreso ({current}/{required})</color>";
                acceptButton.style.display = DisplayStyle.None;
                declineButton.style.display = DisplayStyle.None;
                completeButton.style.display = DisplayStyle.None;
                closeButton.style.display = DisplayStyle.Flex;
            }
            else
            {
                // CASO 3: Quest nueva - Mostrar botón de aceptar
                statusLabel.text = "<color=green>Nueva Quest</color>";
                acceptButton.style.display = DisplayStyle.Flex;
                declineButton.style.display = DisplayStyle.None; // No permitir declinar en cadena lineal
                completeButton.style.display = DisplayStyle.None;
                closeButton.style.display = DisplayStyle.Flex;
            }
        }

        // ===== MÉTODOS HELPER LOCALES =====

        /// <summary>
        /// Busca el índice de una quest en la SyncList LOCAL
        /// </summary>
        private int GetLocalQuestIndex(string questName)
        {
            if (playerQuests == null) return -1;

            for (int i = 0; i < playerQuests.activeQuests.Count; i++)
            {
                if (playerQuests.activeQuests[i].questName == questName)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Verifica si una quest local está completa
        /// </summary>
        private bool IsLocalQuestComplete(int questIndex)
        {
            if (playerQuests == null) return false;
            if (questIndex < 0 || questIndex >= playerQuests.activeQuests.Count) return false;

            QuestStatus qs = playerQuests.activeQuests[questIndex];
            QuestData questData = qs.GetQuestData();
            if (questData == null || questData.objectives.Count == 0) return false;

            return qs.currentAmount >= questData.objectives[0].requiredAmount;
        }

        // ===== CALLBACKS DE BOTONES =====

        private void OnAcceptButton()
        {
            if (currentQuest == null || playerQuests == null) return;

            playerQuests.CmdAcceptQuest(currentQuest.name);
            Debug.Log($"[QuestGiverUI] Accepted quest: {currentQuest.questTitle}");
            Close();
        }

        private void OnDeclineButton()
        {
            Debug.Log("[QuestGiverUI] Quest declined.");
            Close();
        }

        private void OnCompleteButton()
        {
            if (currentQuest == null || playerQuests == null) return;

            // Obtener el índice ACTUAL (puede haber cambiado)
            int questIndex = GetLocalQuestIndex(currentQuest.name);
            if (questIndex == -1)
            {
                Debug.LogError("[QuestGiverUI] Quest not found in player's active quests!");
                Close();
                return;
            }

            playerQuests.CmdCompleteQuest(questIndex);
            Debug.Log($"[QuestGiverUI] Completed quest: {currentQuest.questTitle}");
            Close();
        }

        private void OnCloseButton()
        {
            Debug.Log("[QuestGiverUI] Closed dialog.");
            Close();
        }

        private void Close()
        {
            panel.style.display = DisplayStyle.None;

            // Ocultar mensaje de bloqueo si está activo
            if (blockedReasonLabel != null)
            {
                blockedReasonLabel.style.display = DisplayStyle.None;
            }

            currentNpc = null;
            currentQuest = null;
            playerQuests = null;
            isBlocked = false;
        }

        private void OnDisable()
        {
            // Cleanup
            if (acceptButton != null) acceptButton.clicked -= OnAcceptButton;
            if (declineButton != null) declineButton.clicked -= OnDeclineButton;
            if (completeButton != null) completeButton.clicked -= OnCompleteButton;
            if (closeButton != null) closeButton.clicked -= OnCloseButton;
        }
    }
}
