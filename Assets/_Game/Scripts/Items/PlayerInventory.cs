using Mirror;
using UnityEngine;
using System;
using Game.Player;

/// <summary>
/// Struct que representa un slot del inventario.
/// DEBE ser serializable para funcionar con SyncList de Mirror.
/// </summary>
[Serializable]
public struct InventorySlot : IEquatable<InventorySlot>
{
    public int itemID;      // ID del item (-1 = vacío)
    public int amount;      // Cantidad en el stack

    public InventorySlot(int id, int amt)
    {
        itemID = id;
        amount = amt;
    }

    // Implementación de IEquatable para que SyncList funcione correctamente
    public bool Equals(InventorySlot other)
    {
        return itemID == other.itemID && amount == other.amount;
    }

    public override bool Equals(object obj)
    {
        return obj is InventorySlot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(itemID, amount);
    }
}

/// <summary>
/// Gestiona el inventario del jugador con sincronización de red.
/// </summary>
public class PlayerInventory : NetworkBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int inventorySize = 64;

    // SyncList sincroniza automáticamente del servidor a todos los clientes
    public readonly SyncList<InventorySlot> inventory = new SyncList<InventorySlot>();

    // Evento para notificar cuando el inventario cambia (para actualizar UI)
    public event Action OnInventoryChanged;

    void Awake()
    {
        // Suscribirse a cambios en la SyncList para actualizar UI
        inventory.Callback += OnInventoryUpdated;
    }

    public override void OnStartServer()
    {
        // Inicializar inventario con slots vacíos (solo en servidor)
        inventory.Clear();
        for (int i = 0; i < inventorySize; i++)
        {
            inventory.Add(new InventorySlot(-1, 0)); // -1 = slot vacío
        }

        Debug.Log($"PlayerInventory: Inicializado con {inventorySize} slots.");
    }

    /// <summary>
    /// Callback cuando la SyncList cambia (se ejecuta en todos los clientes)
    /// </summary>
    void OnInventoryUpdated(SyncList<InventorySlot>.Operation op, int index, InventorySlot oldItem, InventorySlot newItem)
    {
        // Notificar al UI que el inventario cambió
        OnInventoryChanged?.Invoke();
    }

    #region Commands (Cliente -> Servidor)

    /// <summary>
    /// Intercambia dos items del inventario
    /// </summary>
    [Command]
    public void CmdSwapItems(int indexA, int indexB)
    {
        // Validación de índices
        if (!IsValidIndex(indexA) || !IsValidIndex(indexB))
        {
            Debug.LogWarning($"PlayerInventory: Índices inválidos ({indexA}, {indexB})");
            return;
        }

        // Swap
        InventorySlot temp = inventory[indexA];
        inventory[indexA] = inventory[indexB];
        inventory[indexB] = temp;

        Debug.Log($"PlayerInventory: Swapped slots {indexA} <-> {indexB}");
    }

    /// <summary>
    /// Añade un item al inventario (busca slot vacío o apila)
    /// Si el item es de tipo Currency, se suma directamente al oro del jugador
    /// </summary>
    [Command]
    public void CmdAddItem(int itemID, int amount)
    {
        if (itemID < 0 || amount <= 0)
        {
            Debug.LogWarning($"PlayerInventory: Item ID o cantidad inválida ({itemID}, {amount})");
            return;
        }

        ItemData itemData = ItemDatabase.Instance?.GetItem(itemID);
        if (itemData == null)
        {
            Debug.LogWarning($"PlayerInventory: Item {itemID} no existe en la base de datos");
            return;
        }

        // CASO ESPECIAL: Currency (oro, monedas) se suma directo a PlayerStats
        if (itemData.itemType == ItemType.Currency)
        {
            PlayerStats stats = GetComponent<PlayerStats>();
            if (stats != null)
            {
                int goldToAdd = itemData.goldValue * amount;
                stats.gold += goldToAdd;
                Debug.Log($"PlayerInventory: Añadido {goldToAdd} oro ({amount}x {itemData.itemName})");
                TargetShowGoldPickup(connectionToClient, goldToAdd);
            }
            return; // No añadir al inventario
        }

        int remainingAmount = amount;

        // Si es apilable, buscar stack existente con espacio
        if (itemData.isStackable)
        {
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i].itemID == itemID && inventory[i].amount < itemData.maxStackSize)
                {
                    int spaceInStack = itemData.maxStackSize - inventory[i].amount;
                    int amountToAdd = Mathf.Min(spaceInStack, remainingAmount);

                    InventorySlot slot = inventory[i];
                    slot.amount += amountToAdd;
                    inventory[i] = slot;

                    remainingAmount -= amountToAdd;

                    if (remainingAmount <= 0) break;
                }
            }
        }

        // Añadir a slots vacíos
        while (remainingAmount > 0)
        {
            int emptySlot = FindEmptySlot();
            if (emptySlot == -1)
            {
                Debug.LogWarning("PlayerInventory: Inventario lleno!");
                TargetInventoryFull();
                break;
            }

            int amountToAdd = itemData.isStackable ? Mathf.Min(remainingAmount, itemData.maxStackSize) : 1;

            inventory[emptySlot] = new InventorySlot(itemID, amountToAdd);
            remainingAmount -= amountToAdd;
        }

        if (remainingAmount == 0)
        {
            Debug.Log($"PlayerInventory: Añadido {amount}x {itemData.itemName}");
        }
    }

    /// <summary>
    /// Remueve un item del inventario
    /// </summary>
    [Command]
    public void CmdRemoveItem(int slotIndex, int amount)
    {
        if (!IsValidIndex(slotIndex))
        {
            Debug.LogWarning($"PlayerInventory: Índice inválido ({slotIndex})");
            return;
        }

        InventorySlot slot = inventory[slotIndex];

        if (slot.itemID < 0)
        {
            Debug.LogWarning("PlayerInventory: Intentando remover de slot vacío");
            return;
        }

        slot.amount -= amount;

        if (slot.amount <= 0)
        {
            // Vaciar slot
            inventory[slotIndex] = new InventorySlot(-1, 0);
        }
        else
        {
            inventory[slotIndex] = slot;
        }

        Debug.Log($"PlayerInventory: Removido {amount} items del slot {slotIndex}");
    }

    /// <summary>
    /// Usa un item consumible del inventario
    /// </summary>
    [Command]
    public void CmdUseItem(int slotIndex)
    {
        if (!IsValidIndex(slotIndex))
        {
            Debug.LogWarning($"PlayerInventory: Índice inválido ({slotIndex})");
            return;
        }

        InventorySlot slot = inventory[slotIndex];

        if (slot.itemID < 0)
        {
            Debug.LogWarning("PlayerInventory: Intentando usar slot vacío");
            return;
        }

        ItemData itemData = ItemDatabase.Instance?.GetItem(slot.itemID);
        if (itemData == null) return;

        // Solo consumibles pueden usarse
        if (itemData.itemType != ItemType.Consumable)
        {
            Debug.LogWarning($"PlayerInventory: {itemData.itemName} no es consumible");
            return;
        }

        // Aplicar efectos del item
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            if (itemData.healthRestore > 0)
            {
                stats.currentHealth = Mathf.Min(stats.maxHealth, stats.currentHealth + itemData.healthRestore);
            }

            if (itemData.manaRestore > 0)
            {
                stats.currentMana = Mathf.Min(stats.maxMana, stats.currentMana + itemData.manaRestore);
            }
        }

        // Consumir item (reducir cantidad)
        CmdRemoveItem(slotIndex, 1);

        Debug.Log($"PlayerInventory: Usado {itemData.itemName}");
        RpcPlayUseItemEffect(itemData.itemName);
    }

    #endregion

    #region ClientRpc (Servidor -> Clientes)

    [TargetRpc]
    void TargetInventoryFull()
    {
        Debug.Log("¡Inventario lleno!");
        // Aquí puedes mostrar un mensaje en UI
    }

    [ClientRpc]
    void RpcPlayUseItemEffect(string itemName)
    {
        Debug.Log($"Efecto: {itemName} usado");
        // Aquí puedes reproducir sonido/partículas
    }

    [TargetRpc]
    void TargetShowGoldPickup(NetworkConnection target, int goldAmount)
    {
        Debug.Log($"+{goldAmount} oro recogido");
        // Aquí puedes mostrar un texto flotante, sonido, partículas doradas, etc.
    }

    #endregion

    #region Helper Methods

    bool IsValidIndex(int index)
    {
        return index >= 0 && index < inventory.Count;
    }

    int FindEmptySlot()
    {
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i].itemID < 0)
            {
                return i;
            }
        }
        return -1; // No hay slots vacíos
    }

    /// <summary>
    /// Cuenta cuántos items de un tipo hay en el inventario
    /// </summary>
    public int CountItem(int itemID)
    {
        int count = 0;
        foreach (var slot in inventory)
        {
            if (slot.itemID == itemID)
            {
                count += slot.amount;
            }
        }
        return count;
    }

    #endregion

    #region Server Methods (Loot & Death)

    /// <summary>
    /// Vacía el inventario y devuelve los items que tenía
    /// </summary>
    [Server]
    public System.Collections.Generic.List<InventorySlot> ClearInventory()
    {
        System.Collections.Generic.List<InventorySlot> droppedItems = new System.Collections.Generic.List<InventorySlot>();

        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i].itemID >= 0 && inventory[i].amount > 0)
            {
                droppedItems.Add(inventory[i]);
                inventory[i] = new InventorySlot(-1, 0); // Vaciar slot
            }
        }

        return droppedItems;
    }

    /// <summary>
    /// Añade una lista de items al inventario (Server Side)
    /// Usado cuando se recoge una LootBag
    /// </summary>
    [Server]
    public void AddItems(System.Collections.Generic.List<InventorySlot> newItems)
    {
        foreach (var newItem in newItems)
        {
            // Reutilizamos la lógica interna de añadir
            // No podemos llamar CmdAddItem desde el servidor, así que duplicamos lógica 
            // o mejor, extraemos la lógica común. Por simplicidad aquí, replicamos la lógica básica de añadir.
            
            // Nota: Podríamos refactorizar CmdAddItem para llamar a un método "AddSingleItem" común.
            // Para mantenerlo simple ahora:
            AddItemServer(newItem.itemID, newItem.amount);
        }
    }

    [Server]
    private void AddItemServer(int itemID, int amount)
    {
        // Misma lógica que CmdAddItem pero sin validación de cliente
        // Buscar stack
        ItemData itemData = ItemDatabase.Instance?.GetItem(itemID);
        if (itemData == null) return;

        // CASO ESPECIAL: Currency (Server Side - LootBag Pickup)
        if (itemData.itemType == ItemType.Currency)
        {
            PlayerStats stats = GetComponent<PlayerStats>();
            if (stats != null)
            {
                int goldToAdd = itemData.goldValue * amount;
                stats.gold += goldToAdd;
                Debug.Log($"[Server] PlayerInventory: Añadido {goldToAdd} oro ({amount}x {itemData.itemName}) vía Loot.");
                
                // Notificar cliente dueño para efecto visual
                TargetShowGoldPickup(connectionToClient, goldToAdd);
            }
            return; // No añadir al inventario físico
        }

        int remaining = amount;

        if (itemData.isStackable)
        {
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i].itemID == itemID && inventory[i].amount < itemData.maxStackSize)
                {
                    int space = itemData.maxStackSize - inventory[i].amount;
                    int add = Mathf.Min(space, remaining);
                    var slot = inventory[i];
                    slot.amount += add;
                    inventory[i] = slot;
                    remaining -= add;
                    if (remaining <= 0) break;
                }
            }
        }

        // Slots vacíos
        while (remaining > 0)
        {
            int empty = FindEmptySlot();
            if (empty == -1) break; // Lleno
            
            int add = itemData.isStackable ? Mathf.Min(remaining, itemData.maxStackSize) : 1;
            inventory[empty] = new InventorySlot(itemID, add);
            remaining -= add;
        }

        if (remaining < amount)
        {
           TargetInventoryFull(); // Notificar si no entró todo
        }
    }

    #endregion

    void OnDestroy()
    {
        // Desuscribirse del callback
        inventory.Callback -= OnInventoryUpdated;
    }
}
