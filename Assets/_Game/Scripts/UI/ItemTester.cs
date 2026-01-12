using UnityEngine;
using UnityEngine.UI;
using Game.Player;

/// <summary>
/// Script de testing para añadir items al inventario
/// </summary>
public class ItemTester : MonoBehaviour
{
    [Header("Testing Settings")]
    [Tooltip("ID del item a añadir (1=HP Potion, 2=Mana Potion, 3=Sword, 4=Shield, 5=Gold)")]
    public int testItemID = 1;

    [Tooltip("Cantidad a añadir")]
    public int testAmount = 1;

    [Header("Hotkeys (Solo para testing)")]
    public KeyCode addItemKey = KeyCode.T;

    private PlayerInventory localPlayerInventory;
    private bool isInitialized = false;

    void Update()
    {
        // Buscar jugador local si aún no se inicializó
        if (!isInitialized)
        {
            TryInitialize();
        }

        // Añadir item con tecla T
        if (Input.GetKeyDown(addItemKey))
        {
            AddTestItem();
        }
    }

    void TryInitialize()
    {
        PlayerController localPlayer = FindLocalPlayer();
        if (localPlayer != null)
        {
            localPlayerInventory = localPlayer.GetComponent<PlayerInventory>();
            if (localPlayerInventory != null)
            {
                isInitialized = true;
                Debug.Log("[ItemTester] Inicializado con jugador local");
            }
        }
    }

    PlayerController FindLocalPlayer()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in players)
        {
            if (player.isLocalPlayer)
            {
                return player;
            }
        }
        return null;
    }

    /// <summary>
    /// Añade un item de prueba al inventario
    /// </summary>
    public void AddTestItem()
    {
        if (localPlayerInventory != null)
        {
            localPlayerInventory.CmdAddItem(testItemID, testAmount);
            Debug.Log($"[ItemTester] Añadiendo {testAmount}x Item ID {testItemID}");
        }
        else
        {
            Debug.LogWarning("[ItemTester] No se encontró PlayerInventory");
        }
    }

    /// <summary>
    /// Añade un item específico (para botones UI)
    /// </summary>
    public void AddItemByID(int itemID)
    {
        if (localPlayerInventory != null)
        {
            localPlayerInventory.CmdAddItem(itemID, 1);
            Debug.Log($"[ItemTester] Añadiendo Item ID {itemID}");
        }
    }

    /// <summary>
    /// Métodos para botones UI (items específicos)
    /// </summary>
    public void AddHealthPotion() => AddItemByID(1);
    public void AddManaPotion() => AddItemByID(2);
    public void AddIronSword() => AddItemByID(3);
    public void AddWoodenShield() => AddItemByID(4);
    public void AddGoldCoin() => AddItemByID(5);

    /// <summary>
    /// Añade múltiples items aleatorios (para testing rápido)
    /// </summary>
    public void AddRandomItems()
    {
        if (localPlayerInventory != null)
        {
            for (int i = 0; i < 5; i++)
            {
                int randomID = Random.Range(1, 6); // IDs 1-5
                int randomAmount = Random.Range(1, 5);
                localPlayerInventory.CmdAddItem(randomID, randomAmount);
            }
            Debug.Log("[ItemTester] Añadidos 5 items aleatorios");
        }
    }
}
