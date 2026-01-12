using Mirror;
using UnityEngine;
using Game.Items;
using Game.Player;
using Game.Core;
using System.Collections.Generic;

namespace Game.NPCs
{
    public class NpcStats : NetworkBehaviour, IEntityStats
    {
        [Header("Data")]
        public NpcData data;
        
        [Header("Runtime Stats")]
        [SyncVar] public int currentHealth;

        // Referencia al prefab de la LootBag (asignar en inspector)
        public GameObject lootBagPrefab;

        // IEntityStats Implementation
        public string EntityName => data != null ? data.npcName : "Enemy";
        public string ClassName => "NPC";
        public int CurrentHealth => currentHealth;
        public int MaxHealth => data != null ? data.maxHealth : 100;

        // Rastreo del último atacante para dar XP
        private PlayerStats lastAttacker;

        public override void OnStartServer()
        {
            if (data != null)
            {
                currentHealth = data.maxHealth;
            }
        }

        [Server]
        public void TakeDamage(int damage, PlayerStats attacker)
        {
            if (currentHealth <= 0) return;

            currentHealth -= damage;
            lastAttacker = attacker;

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        [Server]
        private void Die()
        {
            // 1. Dar XP al asesino
            if (lastAttacker != null && data != null)
            {
                int xpReward = Random.Range(data.minXP, data.maxXP + 1);
                lastAttacker.AddXP(xpReward);
                Debug.Log($"[NpcStats] {data.npcName} murió. XP dada: {xpReward} a {lastAttacker.name}");
            }

            // 2. Generar Loot
            if (data != null && lootBagPrefab != null)
            {
                List<InventorySlot> drops = new List<InventorySlot>();

                // A. Oro (Drop garantizado si min > 0)
                int goldAmount = Random.Range(data.minGold, data.maxGold + 1);
                if (goldAmount > 0 && data.goldItem != null)
                {
                    drops.Add(new InventorySlot(data.goldItem.itemID, goldAmount));
                }

                // B. Items (Drop Chance)
                foreach (var drop in data.table)
                {
                    if (Random.value <= drop.dropChance && drop.item != null)
                    {
                        int amount = Random.Range(drop.minAmount, drop.maxAmount + 1);
                        drops.Add(new InventorySlot(drop.item.itemID, amount));
                    }
                }

                // C. Instanciar LootBag si hay algo
                if (drops.Count > 0)
                {
                    GameObject bagObj = Instantiate(lootBagPrefab, transform.position, Quaternion.identity);
                    LootBag bagScript = bagObj.GetComponent<LootBag>();
                    if (bagScript != null)
                    {
                        bagScript.Initialize(drops);
                    }
                    NetworkServer.Spawn(bagObj);
                }
            }

            // 3. Destruir NPC
            NetworkServer.Destroy(gameObject);
        }
    }
}
