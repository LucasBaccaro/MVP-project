using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base de datos centralizada de todos los items del juego.
/// Singleton para acceso global.
/// </summary>
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [Header("Item Database")]
    [Tooltip("Lista de TODOS los items del juego. Asignar en Inspector.")]
    public List<ItemData> allItems = new List<ItemData>();

    private Dictionary<int, ItemData> itemDictionary = new Dictionary<int, ItemData>();

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Inicializa el diccionario para búsquedas rápidas por ID
    /// </summary>
    void InitializeDatabase()
    {
        itemDictionary.Clear();

        foreach (ItemData item in allItems)
        {
            if (item != null)
            {
                if (!itemDictionary.ContainsKey(item.itemID))
                {
                    itemDictionary.Add(item.itemID, item);
                }
                else
                {
                    Debug.LogWarning($"ItemDatabase: Item duplicado con ID {item.itemID} ({item.itemName}). Ignorado.");
                }
            }
        }

        Debug.Log($"ItemDatabase: Inicializado con {itemDictionary.Count} items.");
    }

    /// <summary>
    /// Obtiene un item por su ID
    /// </summary>
    /// <param name="itemID">ID del item</param>
    /// <returns>ItemData o null si no existe</returns>
    public ItemData GetItem(int itemID)
    {
        if (itemID < 0) return null; // ID -1 = slot vacío

        if (itemDictionary.TryGetValue(itemID, out ItemData item))
        {
            return item;
        }
        else
        {
            Debug.LogWarning($"ItemDatabase: Item con ID {itemID} no encontrado.");
            return null;
        }
    }

    /// <summary>
    /// Verifica si existe un item con este ID
    /// </summary>
    public bool ItemExists(int itemID)
    {
        return itemDictionary.ContainsKey(itemID);
    }

    /// <summary>
    /// Obtiene todos los items de un tipo específico
    /// </summary>
    public List<ItemData> GetItemsByType(ItemType type)
    {
        List<ItemData> result = new List<ItemData>();

        foreach (ItemData item in allItems)
        {
            if (item != null && item.itemType == type)
            {
                result.Add(item);
            }
        }

        return result;
    }
}
