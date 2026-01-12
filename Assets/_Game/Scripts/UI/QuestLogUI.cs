using UnityEngine;
using TMPro;
using Game.Quests;

namespace Game.UI
{
    public class QuestLogUI : MonoBehaviour
    {
        public GameObject panel;
        public TextMeshProUGUI contentText;

        public void Toggle()
        {
            panel.SetActive(!panel.activeSelf);
        }

        public void UpdateLog(QuestStatus[] activeQuests)
        {
            // FIXED: Actualizar el contenido siempre, no solo cuando está visible
            // De esta forma, cuando el jugador abra el log con "J", verá info actualizada

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
            contentText.text = t;
        }
    }
}
