using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Mirror;
using Game.Player;

/// <summary>
/// Manager del UI del inventario. Se sincroniza con PlayerInventory (SyncList).
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;           // Panel principal del inventario
    public GameObject slotPrefab;               // Prefab de un slot individual
    public Transform slotsContainer;            // Contenedor de los slots (GridLayoutGroup)
    public Canvas mainCanvas;                   // Canvas principal

    [Header("Controls")]
    public KeyCode toggleKey = KeyCode.I;       // Tecla para abrir/cerrar inventario

    private List<InventorySlotUI> slotUIList = new List<InventorySlotUI>();
    private PlayerInventory playerInventory;
    private bool isInitialized = false;

    void Update()
    {
        // Buscar jugador local si aún no se inicializó
        if (!isInitialized)
        {
            TryInitialize();
        }

        // Toggle del inventario con tecla I
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
    }

    /// <summary>
    /// Intenta encontrar el PlayerInventory del jugador local
    /// </summary>
    void TryInitialize()
    {
        // Buscar jugador local (NetworkBehaviour con isLocalPlayer)
        PlayerController localPlayer = FindLocalPlayer();
        if (localPlayer != null)
        {
            playerInventory = localPlayer.GetComponent<PlayerInventory>();
            if (playerInventory != null)
            {
                InitializeSlots();
                isInitialized = true;
            }
        }
    }

    /// <summary>
    /// Encuentra el jugador local en la escena
    /// </summary>
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
    /// Crea los slots de UI y se suscribe a cambios del inventario
    /// </summary>
    void InitializeSlots()
    {
        // Limpiar slots anteriores si existen
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIList.Clear();

        // Crear slots según el tamaño del inventario
        int inventorySize = playerInventory.inventory.Count;
        for (int i = 0; i < inventorySize; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();

            if (slotUI != null)
            {
                slotUI.Initialize(this, mainCanvas, i);
                slotUIList.Add(slotUI);
            }
            else
            {
                Debug.LogError("InventoryUI: El prefab de slot no tiene InventorySlotUI component!");
            }
        }

        // Suscribirse a cambios en el inventario
        playerInventory.OnInventoryChanged += RefreshUI;

        // Refresh inicial
        RefreshUI();

        Debug.Log($"InventoryUI: Inicializado con {inventorySize} slots.");
    }

    /// <summary>
    /// Actualiza toda la UI del inventario
    /// </summary>
    void RefreshUI()
    {
        if (playerInventory == null) return;

        for (int i = 0; i < slotUIList.Count && i < playerInventory.inventory.Count; i++)
        {
            slotUIList[i].UpdateSlot(playerInventory.inventory[i]);
        }
    }

    /// <summary>
    /// Abre/cierra el inventario
    /// </summary>
    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        }
    }

    public void OpenInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
        }
    }

    public void CloseInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    #region Comunicación con PlayerInventory (Commands)

    /// <summary>
    /// Solicita al servidor intercambiar dos slots
    /// </summary>
    public void SwapSlots(int indexA, int indexB)
    {
        if (playerInventory != null)
        {
            playerInventory.CmdSwapItems(indexA, indexB);
        }
    }

    /// <summary>
    /// Solicita al servidor usar un item
    /// </summary>
    public void UseItem(int slotIndex)
    {
        if (playerInventory != null)
        {
            playerInventory.CmdUseItem(slotIndex);
        }
    }

    /// <summary>
    /// Solicita al servidor añadir un item (para testing)
    /// </summary>
    public void AddItem(int itemID, int amount = 1)
    {
        if (playerInventory != null)
        {
            playerInventory.CmdAddItem(itemID, amount);
        }
    }

    #endregion

    void OnDestroy()
    {
        // Desuscribirse del evento
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= RefreshUI;
        }
    }
}
