using Mirror;
using System.Collections.Generic;
using UnityEngine;
using Game.Player;

namespace Game.Items
{
    public class LootBag : NetworkBehaviour
    {
        [Header("Loot Settings")]
        [Tooltip("Lista de items en la bolsa")]
        public readonly SyncList<InventorySlot> items = new SyncList<InventorySlot>();

        [Tooltip("Radio de interacción")]
        public float interactionRange = 3f;

        // Referencia al prefab para spawnear (si fuera necesario, pero aquí es la instancia)

        /// <summary>
        /// Inicializa la bolsa con una lista de items
        /// </summary>
        [Server]
        public void Initialize(List<InventorySlot> newItems)
        {
            foreach (var item in newItems)
            {
                if (item.itemID >= 0 && item.amount > 0)
                {
                    items.Add(item);
                }
            }

            if (items.Count == 0)
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        /// <summary>
        /// Intenta reclamar el loot por un jugador
        /// </summary>
        [Command(requiresAuthority = false)] // Cualquiera puede llamar esto
        public void CmdClaimLoot(NetworkConnectionToClient sender = null)
        {
            if (sender == null || sender.identity == null) return;

            GameObject player = sender.identity.gameObject;
            
            // Validar distancia
            if (Vector3.Distance(transform.position, player.transform.position) > interactionRange)
            {
                Debug.LogWarning($"[LootBag] Jugador {player.name} demasiado lejos para recoger loot.");
                return;
            }

            PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();
            if (playerInventory != null)
            {
                // Transferir items
                List<InventorySlot> itemsToGive = new List<InventorySlot>();
                foreach (var item in items)
                {
                    itemsToGive.Add(item);
                }

                // Añadir al inventario del jugador
                playerInventory.AddItems(itemsToGive);

                // Vaciar y destruir bolsa
                items.Clear();
                NetworkServer.Destroy(gameObject);

                Debug.Log($"[LootBag] {player.name} recogió el loot.");
            }
        }
        /// <summary>
        /// Reclama un item específico de la bolsa
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdTakeItem(int index, NetworkConnectionToClient sender = null)
        {
            if (sender == null || sender.identity == null) return;
            
            // Validar índice
            if (index < 0 || index >= items.Count) return;

            GameObject player = sender.identity.gameObject;

            // Validar distancia
            if (Vector3.Distance(transform.position, player.transform.position) > interactionRange)
            {
                return;
            }

            PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();
            if (playerInventory != null)
            {
                InventorySlot item = items[index];
                
                // Intentar añadir al inventario del jugador
                // Nota: AddItems acepta una lista, creamos una temporal
                List<InventorySlot> list = new List<InventorySlot> { item };
                playerInventory.AddItems(list);

                // Remover de la bolsa
                items.RemoveAt(index);

                // Si se vacía, destruir
                if (items.Count == 0)
                {
                    NetworkServer.Destroy(gameObject);
                }
            }
        }
    }
}
