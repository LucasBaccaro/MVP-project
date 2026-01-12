using UnityEngine;
using System.Collections.Generic;
using Game.Items;

namespace Game.NPCs
{
    [System.Serializable]
    public struct DropItem
    {
        public ItemData item;
        [Range(0f, 1f)] public float dropChance;
        public int minAmount;
        public int maxAmount;
    }

    [CreateAssetMenu(fileName = "NewNpcData", menuName = "MMO/NPC Data")]
    public class NpcData : ScriptableObject
    {
        [Header("General")]
        public string npcName = "Enemy";
        public float moveSpeed = 3.5f;

        [Header("Combat Stats")]
        public int maxHealth = 100;
        public int damage = 10;
        public float attackRange = 2f;
        public float attackSpeed = 1.5f;
        public float aggroRange = 10f;
        [Tooltip("Distancia máxima desde el punto de spawn antes de volver")]
        public float maxChaseDistance = 20f;

        [Header("Rewards")]
        public int minXP = 10;
        public int maxXP = 20;

        public int minGold = 1;
        public int maxGold = 5;
        
        // Referencia al item de oro para instanciar en la bolsa
        public ItemData goldItem; 

        [Header("Item Drops")]
        public List<DropItem> table = new List<DropItem>();
    }
}
