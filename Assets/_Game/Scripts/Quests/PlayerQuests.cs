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

        // NUEVO: Persistencia de quests completadas (separadas por comas)
        [SyncVar]
        public string completedQuestsCSV = "";

        // NUEVO: Índice de progreso en la cadena principal
        [SyncVar]
        public int currentChainIndex = 0;

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

            // DEBUG: Tecla D para imprimir estado de quests
            if (Input.GetKeyDown(KeyCode.D))
            {
                DebugPrintQuestState();
            }
        }

        /// <summary>
        /// DEBUG: Imprime el estado actual de quests del jugador
        /// </summary>
        private void DebugPrintQuestState()
        {
            Debug.Log("========== QUEST STATE DEBUG ==========");
            Debug.Log($"Player Level: {playerStats.level}");
            Debug.Log($"Completed Quests CSV: '{completedQuestsCSV}'");
            Debug.Log($"Current Chain Index: {currentChainIndex}");
            Debug.Log($"Active Quests Count: {activeQuests.Count}");

            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestStatus qs = activeQuests[i];
                Debug.Log($"  Active Quest {i}: {qs.questName} - Progress: {qs.currentAmount}");
            }

            // Listar todas las quests disponibles en Resources
            QuestData[] allQuests = Resources.LoadAll<QuestData>("Quests");
            Debug.Log($"Total Quests in Resources/Quests: {allQuests.Length}");
            foreach (var q in allQuests.OrderBy(q => q.orderInChain))
            {
                Debug.Log($"  Quest: {q.name} - Title: {q.questTitle} - Order: {q.orderInChain} - ReqLevel: {q.requiredLevel}");
            }

            Debug.Log("======================================");
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

        /// <summary>
        /// Verifica si el jugador puede aceptar una quest específica (validación central)
        /// NOTA: No es [Server] porque necesita ejecutarse en clientes para UI.
        /// Solo lee SyncVars, no modifica estado.
        /// </summary>
        public bool CanAcceptQuest(QuestData quest, out string reason)
        {
            Debug.Log($"[PlayerQuests] CanAcceptQuest - Quest: {quest?.name}, PlayerLevel: {playerStats?.level}, CompletedQuests: '{completedQuestsCSV}'");

            if (quest == null)
            {
                reason = "Quest inválida";
                Debug.Log($"[PlayerQuests] Validation FAILED: Quest is null");
                return false;
            }

            // 1. No duplicados
            if (activeQuests.Any(q => q.questName == quest.name))
            {
                reason = "Ya tienes esta quest activa";
                Debug.Log($"[PlayerQuests] Validation FAILED: Quest already active");
                return false;
            }

            // 2. No completadas anteriormente
            if (IsQuestCompleted(quest.name))
            {
                reason = "Ya completaste esta quest";
                Debug.Log($"[PlayerQuests] Validation FAILED: Quest already completed");
                return false;
            }

            // 3. Verificar nivel
            if (playerStats.level < quest.requiredLevel)
            {
                reason = $"Requiere nivel {quest.requiredLevel}";
                Debug.Log($"[PlayerQuests] Validation FAILED: Level too low ({playerStats.level} < {quest.requiredLevel})");
                return false;
            }

            // 4. Verificar orden en cadena (quest previa completada)
            if (quest.orderInChain > 0)
            {
                QuestData[] allQuests = Resources.LoadAll<QuestData>("Quests");
                QuestData previousQuest = System.Array.Find(allQuests,
                    q => q.orderInChain == quest.orderInChain - 1);

                if (previousQuest != null && !IsQuestCompleted(previousQuest.name))
                {
                    reason = $"Primero debes completar: {previousQuest.questTitle}";
                    Debug.Log($"[PlayerQuests] Validation FAILED: Previous quest not completed ({previousQuest.name})");
                    return false;
                }
            }

            reason = "";
            Debug.Log($"[PlayerQuests] Validation PASSED - Quest can be accepted");
            return true;
        }

        /// <summary>
        /// Verifica si una quest fue completada (consulta el historial CSV)
        /// </summary>
        public bool IsQuestCompleted(string questName)
        {
            if (string.IsNullOrEmpty(completedQuestsCSV)) return false;
            string[] completed = completedQuestsCSV.Split(',');
            return System.Array.Exists(completed, q => q == questName);
        }

        /// <summary>
        /// Marca una quest como completada (agrega al historial CSV)
        /// </summary>
        [Server]
        private void MarkQuestCompleted(string questName)
        {
            if (IsQuestCompleted(questName)) return;

            if (string.IsNullOrEmpty(completedQuestsCSV))
            {
                completedQuestsCSV = questName;
            }
            else
            {
                completedQuestsCSV += "," + questName;
            }

            Debug.Log($"[PlayerQuests] Quest completada agregada al historial: {questName}");
        }

        /// <summary>
        /// Obtiene la próxima quest disponible en la cadena (que puede aceptar)
        /// NOTA: No es [Server] porque necesita ejecutarse en clientes para UI.
        /// </summary>
        public QuestData GetNextAvailableQuest()
        {
            QuestData[] allQuests = Resources.LoadAll<QuestData>("Quests");

            // Ordenar por orderInChain
            var sortedQuests = System.Array.FindAll(allQuests, q => q != null)
                .OrderBy(q => q.orderInChain)
                .ToArray();

            foreach (var quest in sortedQuests)
            {
                // Retornar la primera quest que pueda aceptar
                if (CanAcceptQuest(quest, out _))
                {
                    return quest;
                }
            }

            return null; // No hay más quests disponibles
        }

        /// <summary>
        /// Obtiene la siguiente quest bloqueada por nivel (para mostrar en UI)
        /// NOTA: No es [Server] porque necesita ejecutarse en clientes para UI.
        /// </summary>
        public QuestData GetNextBlockedQuest()
        {
            QuestData[] allQuests = Resources.LoadAll<QuestData>("Quests");

            var sortedQuests = System.Array.FindAll(allQuests, q => q != null)
                .OrderBy(q => q.orderInChain)
                .ToArray();

            foreach (var quest in sortedQuests)
            {
                // Buscar la primera quest que esté bloqueada solo por nivel
                if (!IsQuestCompleted(quest.name) &&
                    !activeQuests.Any(q => q.questName == quest.name) &&
                    playerStats.level < quest.requiredLevel)
                {
                    // Verificar que la quest previa esté completa
                    if (quest.orderInChain == 0 || IsQuestCompleted(GetPreviousQuestName(quest)))
                    {
                        return quest;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Helper: Obtiene el nombre de la quest anterior en la cadena
        /// </summary>
        private string GetPreviousQuestName(QuestData quest)
        {
            if (quest.orderInChain == 0) return null;

            QuestData[] allQuests = Resources.LoadAll<QuestData>("Quests");
            QuestData prev = System.Array.Find(allQuests,
                q => q.orderInChain == quest.orderInChain - 1);

            return prev?.name;
        }

        [Command]
        public void CmdAcceptQuest(string questName)
        {
            // Load by Filename which acts as the ID
            QuestData[] allQuests = Resources.LoadAll<QuestData>("Quests");
            QuestData quest = System.Array.Find(allQuests, q => q.name == questName);

            if (quest != null)
            {
                // NUEVO: Usar validación antes de aceptar
                if (CanAcceptQuest(quest, out string reason))
                {
                    ServerAcceptQuest(quest);
                }
                else
                {
                    Debug.LogWarning($"[PlayerQuests] No se puede aceptar quest '{quest.questTitle}': {reason}");
                }
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
                   // Recompensas
                   playerStats.AddXP(questData.xpReward);
                   playerStats.AddGold(questData.goldReward);

                   Debug.Log($"[PlayerQuests] Completed {questData.questTitle}. Rewards: {questData.xpReward} XP, {questData.goldReward} Gold");

                   // NUEVO: Marcar como completada (persistencia)
                   MarkQuestCompleted(questData.name);

                   // NUEVO: Actualizar progreso de cadena
                   if (questData.orderInChain >= currentChainIndex)
                   {
                       currentChainIndex = questData.orderInChain + 1;
                   }

                   activeQuests.RemoveAt(questIndex);
                   // El callback de SyncList actualizará el UI automáticamente
               }
            }
        }

    }
}
