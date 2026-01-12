using System.Collections.Generic;
using UnityEngine;

namespace Game.Quests
{
    [CreateAssetMenu(fileName = "NewQuest", menuName = "Game/Quest Data")]
    public class QuestData : ScriptableObject
    {
        [Header("Info")]
        public string questTitle;
        [TextArea] public string questDescription;

        [Header("Objectives")]
        public List<QuestObjective> objectives = new List<QuestObjective>();

        [Header("Rewards")]
        public int xpReward;
        public int goldReward;
        // public List<ItemData> itemRewards;
    }
}
