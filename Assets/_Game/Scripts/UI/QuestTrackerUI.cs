using UnityEngine;
using TMPro;
using Game.Quests;
using System.Text;

namespace Game.UI
{
    public class QuestTrackerUI : MonoBehaviour
    {
        public TextMeshProUGUI trackerText;
        
        // This would listen to PlayerQuests events ideally.
        // For MVP, we can have PlayerQuests reference this directly if it finds it.

        public void UpdateTracker(QuestStatus[] activeQuests)
        {
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

            trackerText.text = sb.ToString();
        }
    }
}
