using Mirror;
using UnityEngine;
using System.Collections.Generic;
using Game.NPCs;
using Game.Player; // Required for PlayerStats
using System.Linq;

namespace Game.Quests
{
    [System.Serializable]
    public struct QuestStatus
    {
        // CRÍTICO: NO usar ScriptableObject directamente (Mirror no lo serializa bien)
        // Usamos el nombre del asset y lo cargamos desde Resources cuando se necesite
        public string questName;  // Nombre del asset (quest.name)
        public int currentAmount;
        public bool isCompleted;

        public QuestStatus(QuestData questData)
        {
            questName = questData.name;  // Guardamos el nombre del asset
            currentAmount = 0;
            isCompleted = false;
        }

        /// <summary>
        /// Obtiene el QuestData desde Resources. Usar esto en lugar de acceder a 'data' directamente.
        /// </summary>
        public QuestData GetQuestData()
        {
            if (string.IsNullOrEmpty(questName)) return null;

            // IMPORTANTE: Copiar a variable local antes de usar en lambda (requisito de structs)
            string localQuestName = questName;

            // Cargar desde Resources/Quests
            QuestData[] allQuests = Resources.LoadAll<QuestData>("Quests");
            return System.Array.Find(allQuests, q => q.name == localQuestName);
        }
    }

    public class PlayerQuests : NetworkBehaviour
    {
        [Header("State")]
        public readonly SyncList<QuestStatus> activeQuests = new SyncList<QuestStatus>();

        private PlayerStats playerStats;
        
        // UI References (Client Only)
        private Game.UI.QuestTrackerUI trackerUI;
        private Game.UI.QuestLogUI logUI;

        private void Awake()
        {
            playerStats = GetComponent<PlayerStats>();

            // CRÍTICO: Suscribirse al callback de SyncList
            // Esto asegura que el UI se actualice AUTOMÁTICAMENTE cuando la SyncList cambie
            activeQuests.Callback += OnQuestListChanged;
        }

        public override void OnStartLocalPlayer()
        {
            // Find UI
            trackerUI = FindFirstObjectByType<Game.UI.QuestTrackerUI>();
            logUI = FindFirstObjectByType<Game.UI.QuestLogUI>();

            Debug.Log($"[PlayerQuests] OnStartLocalPlayer. TrackerUI: {(trackerUI != null ? "Found" : "NOT Found")}");
            UpdateUI();
        }

        /// <summary>
        /// Callback automático cuando la SyncList cambia (insert, update, remove)
        /// Se ejecuta en el cliente cuando Mirror sincroniza los cambios
        /// </summary>
        private void OnQuestListChanged(SyncList<QuestStatus>.Operation op, int index, QuestStatus oldItem, QuestStatus newItem)
        {
            // Solo actualizar UI en el jugador local
            if (!isLocalPlayer) return;

            Debug.Log($"[PlayerQuests] SyncList changed! Operation: {op}, Index: {index}");
            UpdateUI();
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            // Toggle Log
            if (Input.GetKeyDown(KeyCode.J))
            {
                if (logUI != null) logUI.Toggle();
            }
        }

        private void UpdateUI()
        {
            // Convert SyncList to Array for UI
            QuestStatus[] quests = new QuestStatus[activeQuests.Count];
            activeQuests.CopyTo(quests, 0);

            Debug.Log($"[PlayerQuests] UpdateUI called. SyncList count: {activeQuests.Count}. TrackerUI exists: {trackerUI != null}");

            if (trackerUI != null) trackerUI.UpdateTracker(quests);
            if (logUI != null) logUI.UpdateLog(quests);
        }

        /// <summary>
        /// Called when an NPC dies and this player was the killer
        /// </summary>
        [Server]
        public void ServerOnEnemyKilled(string npcName)
        {
            Debug.Log($"[PlayerQuests] ServerOnEnemyKilled called for: {npcName}. Active quests: {activeQuests.Count}");

            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestStatus qs = activeQuests[i];
                if (qs.isCompleted) continue;

                QuestData questData = qs.GetQuestData();
                if (questData == null) continue;

                foreach(var obj in questData.objectives)
                {
                    Debug.Log($"[PlayerQuests] Checking objective: '{obj.targetName}' vs '{npcName}'");

                    // Case-insensitive comparison and trimming to avoid easy setup errors
                    bool isMatch = string.Equals(obj.targetName?.Trim(), npcName?.Trim(), System.StringComparison.InvariantCultureIgnoreCase);

                    if (obj.type == ObjectiveType.Kill && isMatch)
                    {
                        if (qs.currentAmount < obj.requiredAmount)
                        {
                            qs.currentAmount++;

                            if (qs.currentAmount >= obj.requiredAmount)
                            {
                                Debug.Log($"[PlayerQuests] Quest '{questData.questTitle}' COMPLETED (Ready to turn in)!");
                            }
                            else
                            {
                                Debug.Log($"[PlayerQuests] Quest '{questData.questTitle}' progress updated: {qs.currentAmount}/{obj.requiredAmount}");
                            }

                            // CRÍTICO: Actualizar el elemento de la SyncList
                            // Mirror detectará el cambio y llamará al callback automáticamente
                            activeQuests[i] = qs;
                        }
                    }
                }
            }
        }

        [Command]
        public void CmdAcceptQuest(string questName) 
        {
            // Load by Filename which acts as the ID
            QuestData[] allQuests = Resources.LoadAll<QuestData>("Quests");
            QuestData quest = System.Array.Find(allQuests, q => q.name == questName);

            if (quest != null)
            {
                ServerAcceptQuest(quest);
            }
            else
            {
                Debug.LogError($"[PlayerQuests] Could not find quest in Resources/Quests with Filename: {questName}");
            }
        }

        [Server]
        public void ServerAcceptQuest(QuestData quest)
        {
            if (quest == null) return;

            // Check duplicates (by Name/ID)
            if (activeQuests.Any(q => q.questName == quest.name)) return;

            QuestStatus newQuest = new QuestStatus(quest);
            activeQuests.Add(newQuest);

            Debug.Log($"[PlayerQuests] Accepted quest: {quest.questTitle}");
            // El callback de SyncList actualizará el UI automáticamente
        }

        [Command]
        public void CmdCompleteQuest(int questIndex)
        {
            if (questIndex < 0 || questIndex >= activeQuests.Count) return;

            QuestStatus qs = activeQuests[questIndex];
            QuestData questData = qs.GetQuestData();
            if (questData == null) return;

            if (questData.objectives.Count > 0)
            {
               if (qs.currentAmount >= questData.objectives[0].requiredAmount)
               {
                   playerStats.AddXP(questData.xpReward);
                   playerStats.AddGold(questData.goldReward);

                   Debug.Log($"[PlayerQuests] Completed {questData.questTitle}. Rewards: {questData.xpReward} XP, {questData.goldReward} Gold");

                   activeQuests.RemoveAt(questIndex);
                   // El callback de SyncList actualizará el UI automáticamente
               }
            }
        }

    }
}
