using System;
using UnityEngine;

namespace Game.Quests
{
    public enum ObjectiveType
    {
        Kill,
        // Gather, TalkTo, etc.
    }

    [Serializable]
    public class QuestObjective
    {
        public ObjectiveType type;
        
        [Tooltip("The exact NpcName from NpcData to match on kill")]
        public string targetName;
        
        [Tooltip("Amount required (e.g., Kill 3 Crocodiles)")]
        public int requiredAmount;
    }
}
