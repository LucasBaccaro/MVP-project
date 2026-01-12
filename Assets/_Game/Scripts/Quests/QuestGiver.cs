using UnityEngine;
using Game.Core;
using System.Collections.Generic;
using Game.UI;
using System.Linq;

namespace Game.Quests
{
    public class QuestGiver : MonoBehaviour, IInteractable
    {
        [Header("Quest Chain")]
        [Tooltip("Lista de quests en orden. El NPC determinará cuál mostrar según progreso del jugador")]
        public List<QuestData> questChain;

        [Header("NPC Info")]
        public string npcName = "Guardián del Bosque";
        [TextArea]
        public string greetingText = "¡Aventurero! Tengo tareas para ti.";

        public string InteractionPrompt => "Talk";

        private void Start()
        {
            Debug.Log($"[QuestGiver] Initialized with {questChain.Count} quests in chain");
            for (int i = 0; i < questChain.Count; i++)
            {
                if (questChain[i] != null)
                {
                    Debug.Log($"  Quest {i}: {questChain[i].name} - {questChain[i].questTitle} (Order: {questChain[i].orderInChain})");
                }
                else
                {
                    Debug.LogWarning($"  Quest {i}: NULL!");
                }
            }
        }

        public void Interact(GameObject player)
        {
            Debug.Log($"[QuestGiver] Interacting with {player.name}");

            PlayerQuests playerQuests = player.GetComponent<PlayerQuests>();
            if (playerQuests == null)
            {
                Debug.LogWarning("[QuestGiver] Player doesn't have PlayerQuests component.");
                return;
            }

            // Abrir UI
            QuestGiverUI ui = FindFirstObjectByType<QuestGiverUI>(FindObjectsInactive.Include);
            if (ui == null)
            {
                Debug.LogWarning("[QuestGiver] QuestGiverUI not found in scene.");
                return;
            }

            // Determinar qué quest mostrar
            QuestData questToShow = DetermineQuestToShow(playerQuests);
            QuestData blockedQuest = null;

            if (questToShow == null)
            {
                // Si no hay quest disponible, buscar la siguiente bloqueada
                blockedQuest = playerQuests.GetNextBlockedQuest();
            }

            // SIEMPRE abrir el diálogo, con quest, bloqueada, o mensaje de fin
            if (questToShow != null)
            {
                ui.Open(this, questToShow, player, false); // false = no bloqueada
            }
            else if (blockedQuest != null)
            {
                ui.Open(this, blockedQuest, player, true); // true = bloqueada
            }
            else
            {
                // No hay más quests - mostrar mensaje de fin
                Debug.Log("[QuestGiver] No more quests available for this player.");
                ui.OpenNoQuests(this, player);
            }
        }

        /// <summary>
        /// Determina cuál quest debe mostrar el NPC según el estado del jugador.
        /// Prioridad: Quest activa > Quest nueva disponible > null
        /// </summary>
        private QuestData DetermineQuestToShow(PlayerQuests playerQuests)
        {
            Debug.Log($"[QuestGiver] DetermineQuestToShow - questChain count: {questChain.Count}");

            // Ordenar quests por orderInChain
            var sortedChain = questChain.OrderBy(q => q.orderInChain).ToList();

            foreach (var quest in sortedChain)
            {
                if (quest == null) continue;

                string questID = quest.name;
                Debug.Log($"[QuestGiver] Checking quest: {quest.questTitle} (Order: {quest.orderInChain}, ReqLevel: {quest.requiredLevel})");

                // PRIORIDAD 1: Quest activa (completa o en progreso)
                int activeIndex = GetActiveQuestIndex(playerQuests, questID);
                if (activeIndex != -1)
                {
                    Debug.Log($"[QuestGiver] Quest is ACTIVE (index {activeIndex})");
                    return quest; // Mostrar para entregar o ver progreso
                }

                // PRIORIDAD 2: Quest nueva que puede aceptar
                if (playerQuests.CanAcceptQuest(quest, out string reason))
                {
                    Debug.Log($"[QuestGiver] Quest CAN be accepted");
                    return quest;
                }
                else
                {
                    Debug.Log($"[QuestGiver] Quest CANNOT be accepted: {reason}");
                }
            }

            // No hay quest disponible
            Debug.Log("[QuestGiver] No quest found to show");
            return null;
        }

        /// <summary>
        /// Helper: Busca el índice de una quest activa por nombre
        /// </summary>
        private int GetActiveQuestIndex(PlayerQuests pq, string questName)
        {
            for (int i = 0; i < pq.activeQuests.Count; i++)
            {
                if (pq.activeQuests[i].questName == questName)
                    return i;
            }
            return -1;
        }
    }
}
