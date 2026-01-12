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

        [Header("Chain Progression")]
        [Tooltip("Nivel mínimo requerido para aceptar esta quest")]
        public int requiredLevel = 1;

        [Tooltip("Orden en la cadena (0 = primera quest, 1 = segunda, etc.)")]
        public int orderInChain = 0;

        [Header("Objectives")]
        public List<QuestObjective> objectives = new List<QuestObjective>();

        [Header("Rewards")]
        public int xpReward;
        public int goldReward;
        // public List<ItemData> itemRewards;

        [Header("Auto-Balance")]
        [Tooltip("Si está activo, calcula XP automáticamente basado en requiredLevel")]
        public bool autoCalculateXP = true;

        [Tooltip("XP base por nivel. Fórmula: baseXP * requiredLevel * multiplier")]
        public int baseXPPerLevel = 80;

        /// <summary>
        /// Calcula la recompensa XP recomendada para esta quest.
        /// Fórmula: baseXP * requiredLevel * (1 + (requiredLevel - 1) * 0.1)
        /// Nivel 1: 80 XP, Nivel 3: 288 XP, Nivel 5: 560 XP, Nivel 8: 1088 XP
        /// </summary>
        public int CalculateRecommendedXP()
        {
            // Escalado: aumenta 10% por cada nivel
            float multiplier = 1f + (requiredLevel - 1) * 0.1f;
            return Mathf.RoundToInt(baseXPPerLevel * requiredLevel * multiplier);
        }

        /// <summary>
        /// Valida automáticamente el XP en el editor (se ejecuta cuando cambias valores en el Inspector)
        /// </summary>
        private void OnValidate()
        {
            if (autoCalculateXP)
            {
                xpReward = CalculateRecommendedXP();
            }
        }
    }
}
