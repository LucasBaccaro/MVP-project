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
    [SerializeField] private int inventorySize = 20;

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

    void OnDestroy()
    {
        // Desuscribirse del callback
        inventory.Callback -= OnInventoryUpdated;
    }
}
