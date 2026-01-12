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

        // Estado actual
        private QuestGiver currentNpc;
        private QuestData currentQuest;
        private PlayerQuests playerQuests;

        private void Start()
        {
            if(panel != null) panel.SetActive(false);
        }

        /// <summary>
        /// Abre el panel y decide qué mostrar según el estado de la quest
        /// </summary>
        public void Open(QuestGiver npc, QuestData quest, GameObject player)
        {
            currentNpc = npc;
            currentQuest = quest;

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

            // Decidir qué botones mostrar según el estado
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
                declineButton.SetActive(true);
                completeButton.SetActive(false);
                closeButton.SetActive(false);
            }

            panel.SetActive(true);
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
            currentNpc = null;
            currentQuest = null;
            playerQuests = null;
        }
    }
}
