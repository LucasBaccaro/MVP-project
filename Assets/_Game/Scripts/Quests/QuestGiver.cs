using UnityEngine;
using Game.Core;
using System.Collections.Generic;
using Game.UI;

namespace Game.Quests
{
    public class QuestGiver : MonoBehaviour, IInteractable
    {
        [Header("Quests")]
        public List<QuestData> availableQuests;

        public string InteractionPrompt => "Talk";

        public void Interact(GameObject player)
        {
            Debug.Log($"[QuestGiver] Interacting with {player.name}");

            // Open Quest UI
            QuestGiverUI ui = FindFirstObjectByType<QuestGiverUI>(FindObjectsInactive.Include);
            if (ui == null)
            {
                Debug.LogWarning("[QuestGiver] QuestGiverUI not found in scene.");
                return;
            }

            if (availableQuests.Count == 0)
            {
                Debug.Log("[QuestGiver] No quests available.");
                return;
            }

            // MVP: Show first quest only
            // El UI decidirá qué botones mostrar basándose en el estado del jugador
            QuestData quest = availableQuests[0];
            ui.Open(this, quest, player);
        }
    }
}
