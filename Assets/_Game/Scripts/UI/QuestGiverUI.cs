using UnityEngine;
using TMPro;
using Game.Quests;
using Game.Player;
using UnityEngine.UI;

namespace Game.UI
{
    public class QuestGiverUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject panel;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI rewardsText;
        public TextMeshProUGUI statusText;

        [Header("Buttons")]
        public GameObject acceptButton;
        public GameObject declineButton;
        public GameObject completeButton;
        public GameObject closeButton;

        [Header("Blocked Quest UI (Opcional)")]
        [Tooltip("Mensaje adicional para quests bloqueadas. Si no se asigna, solo se usa statusText.")]
        public TextMeshProUGUI blockedReasonText;

        // Estado actual
        private QuestGiver currentNpc;
        private QuestData currentQuest;
        private PlayerQuests playerQuests;
        private bool isBlocked; // NUEVO: Indica si la quest está bloqueada

        private void Start()
        {
            if(panel != null) panel.SetActive(false);
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
            titleText.text = quest.questTitle;
            descriptionText.text = quest.questDescription;
            rewardsText.text = $"<color=yellow>Recompensas:</color>\n{quest.xpReward} XP\n{quest.goldReward} Oro";

            // Determinar estado y botones
            if (blocked)
            {
                ShowBlockedState(quest);
            }
            else
            {
                ShowNormalState(quest);
            }

            panel.SetActive(true);
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
            titleText.text = npc.npcName;
            descriptionText.text = "Has completado todas las quests que tengo para ti por ahora. ¡Sigue entrenando y vuelve pronto, aventurero!";
            rewardsText.text = "";
            statusText.text = "<color=gray>Sin quests disponibles</color>";

            // Ocultar mensaje de bloqueo si existe
            if (blockedReasonText != null)
            {
                blockedReasonText.gameObject.SetActive(false);
            }

            // Solo botón cerrar
            acceptButton.SetActive(false);
            declineButton.SetActive(false);
            completeButton.SetActive(false);
            closeButton.SetActive(true);

            panel.SetActive(true);

            Debug.Log("[QuestGiverUI] Showing 'No quests available' message.");
        }

        /// <summary>
        /// Muestra el estado de quest bloqueada por nivel
        /// </summary>
        private void ShowBlockedState(QuestData quest)
        {
            statusText.text = $"<color=red>Bloqueada - Requiere nivel {quest.requiredLevel}</color>";

            if (blockedReasonText != null)
            {
                blockedReasonText.text = $"Vuelve cuando seas nivel {quest.requiredLevel}";
                blockedReasonText.gameObject.SetActive(true);
            }

            // Solo botón cerrar
            acceptButton.SetActive(false);
            declineButton.SetActive(false);
            completeButton.SetActive(false);
            closeButton.SetActive(true);

            Debug.Log($"[QuestGiverUI] Quest '{quest.questTitle}' está bloqueada (requiere nivel {quest.requiredLevel})");
        }

        /// <summary>
        /// Muestra el estado normal de quest (Nueva, En Progreso, Completa)
        /// </summary>
        private void ShowNormalState(QuestData quest)
        {
            // Ocultar mensaje de bloqueo si existe
            if (blockedReasonText != null)
            {
                blockedReasonText.gameObject.SetActive(false);
            }

            string questID = quest.name;

            // IMPORTANTE: Acceder a la SyncList LOCAL del jugador
            // En host y cliente, esta SyncList debe estar sincronizada
            int questIndex = GetLocalQuestIndex(questID);
            bool hasQuest = questIndex != -1;
            bool isComplete = hasQuest && IsLocalQuestComplete(questIndex);

            Debug.Log($"[QuestGiverUI] Quest '{quest.questTitle}' - hasQuest: {hasQuest}, isComplete: {isComplete}, index: {questIndex}");

            if (isComplete)
            {
                // CASO 1: Quest completa - Mostrar botón de entregar
                statusText.text = "<color=green>¡Completa!</color>";
                acceptButton.SetActive(false);
                declineButton.SetActive(false);
                completeButton.SetActive(true);
                closeButton.SetActive(true);
            }
            else if (hasQuest)
            {
                // CASO 2: Quest en progreso - Mostrar recordatorio
                QuestStatus qs = playerQuests.activeQuests[questIndex];
                QuestData questData = qs.GetQuestData();
                int current = qs.currentAmount;
                int required = (questData != null && questData.objectives.Count > 0) ? questData.objectives[0].requiredAmount : 0;

                statusText.text = $"<color=orange>En Progreso ({current}/{required})</color>";
                acceptButton.SetActive(false);
                declineButton.SetActive(false);
                completeButton.SetActive(false);
                closeButton.SetActive(true);
            }
            else
            {
                // CASO 3: Quest nueva - Mostrar botón de aceptar
                statusText.text = "<color=green>Nueva Quest</color>";
                acceptButton.SetActive(true);
                declineButton.SetActive(false); // No permitir declinar en cadena lineal
                completeButton.SetActive(false);
                closeButton.SetActive(true);
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

        public void OnAcceptButton()
        {
            if (currentQuest == null || playerQuests == null) return;

            playerQuests.CmdAcceptQuest(currentQuest.name);
            Debug.Log($"[QuestGiverUI] Accepted quest: {currentQuest.questTitle}");
            Close();
        }

        public void OnDeclineButton()
        {
            Debug.Log("[QuestGiverUI] Quest declined.");
            Close();
        }

        public void OnCompleteButton()
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

        public void OnCloseButton()
        {
            Debug.Log("[QuestGiverUI] Closed dialog.");
            Close();
        }

        private void Close()
        {
            panel.SetActive(false);

            // Ocultar mensaje de bloqueo si está activo
            if (blockedReasonText != null)
            {
                blockedReasonText.gameObject.SetActive(false);
            }

            currentNpc = null;
            currentQuest = null;
            playerQuests = null;
            isBlocked = false; // NUEVO: Resetear flag
        }
    }
}
