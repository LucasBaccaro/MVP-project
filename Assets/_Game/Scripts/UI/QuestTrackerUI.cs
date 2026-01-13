using UnityEngine;
using UnityEngine.UIElements;
using Game.Quests;
using System.Text;

namespace Game.UI
{
    /// <summary>
    /// Quest Tracker UI - Muestra las quests activas en tiempo real
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class QuestTrackerUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private Label trackerLabel;

        private void Start()
        {
            // Obtener el documento UI y buscar el elemento
            uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError("[QuestTrackerUI] UIDocument component not found!");
                return;
            }

            var root = uiDocument.rootVisualElement;

            // Query el label por nombre
            trackerLabel = root.Q<Label>("tracker-text");

            if (trackerLabel == null)
            {
                Debug.LogError("[QuestTrackerUI] No se pudo encontrar 'tracker-text' en el UXML!");
            }
        }

        /// <summary>
        /// Actualiza el tracker con las quests activas
        /// Llamado desde PlayerQuests cuando cambia la lista
        /// </summary>
        public void UpdateTracker(QuestStatus[] activeQuests)
        {
            if (trackerLabel == null) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b>QUESTS</b>");

            foreach (var q in activeQuests)
            {
                QuestData questData = q.GetQuestData();
                if (questData == null) continue;

                sb.AppendLine($"<color=orange>{questData.questTitle}</color>");
                foreach(var obj in questData.objectives)
                {
                    // MVP: Single objective tracking logic
                    sb.AppendLine($"- {obj.targetName}: {q.currentAmount}/{obj.requiredAmount}");
                }
            }

            trackerLabel.text = sb.ToString();
        }
    }
}
