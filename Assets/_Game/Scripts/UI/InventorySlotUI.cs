using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Representa un slot individual del inventario con drag & drop.
/// </summary>
public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("UI References")]
    public Image iconImage;                 // Icono del item
    public TextMeshProUGUI amountText;      // Texto de cantidad
    public Image backgroundImage;           // Fondo del slot

    [Header("Drag & Drop")]
    public Canvas canvas;                   // Referencia al canvas para drag
    private GameObject draggedIcon;         // Copia visual durante el drag
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    [Header("Slot Data")]
    public int slotIndex = -1;              // Índice de este slot en el inventario
    private InventorySlot currentSlot;      // Datos actuales del slot

    // Referencia al manager del inventario
    private InventoryUI inventoryUI;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(InventoryUI invUI, Canvas mainCanvas, int index)
    {
        inventoryUI = invUI;
        canvas = mainCanvas;
        slotIndex = index;
    }

    /// <summary>
    /// Actualiza la visual del slot según el InventorySlot
    /// </summary>
    public void UpdateSlot(InventorySlot slot)
    {
        currentSlot = slot;

        if (slot.itemID < 0) // Slot vacío
        {
            iconImage.enabled = false;
            amountText.text = "";
        }
        else
        {
            ItemData itemData = ItemDatabase.Instance?.GetItem(slot.itemID);
            if (itemData != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = itemData.icon;

                // Mostrar cantidad si es apilable y > 1
                if (itemData.isStackable && slot.amount > 1)
                {
                    amountText.text = slot.amount.ToString();
                }
                else
                {
                    amountText.text = "";
                }
            }
            else
            {
                // Item no existe en la base de datos
                iconImage.enabled = false;
                amountText.text = "?";
            }
        }
    }

    #region Drag & Drop

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSlot.itemID < 0) return; // No arrastrar slots vacíos

        // Crear copia visual para arrastrar
        draggedIcon = new GameObject("DraggedIcon");
        draggedIcon.transform.SetParent(canvas.transform, false);
        draggedIcon.transform.SetAsLastSibling(); // Dibujar encima de todo

        Image dragImage = draggedIcon.AddComponent<Image>();
        dragImage.sprite = iconImage.sprite;
        dragImage.raycastTarget = false; // No bloquear raycasts

        RectTransform dragRect = draggedIcon.GetComponent<RectTransform>();
        dragRect.sizeDelta = rectTransform.sizeDelta;

        // Hacer transparente el slot original
        canvasGroup.alpha = 0.5f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedIcon != null)
        {
            draggedIcon.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Destruir icono arrastrado
        if (draggedIcon != null)
        {
            Destroy(draggedIcon);
        }

        // Restaurar transparencia
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Detectar sobre qué slot se soltó
        GameObject targetObject = eventData.pointerCurrentRaycast.gameObject;
        if (targetObject != null)
        {
            InventorySlotUI targetSlot = targetObject.GetComponentInParent<InventorySlotUI>();
            if (targetSlot != null && targetSlot != this)
            {
                // Solicitar swap al servidor
                inventoryUI.SwapSlots(slotIndex, targetSlot.slotIndex);
            }
        }
    }

    #endregion

    #region Click (Usar Item)

    public void OnPointerClick(PointerEventData eventData)
    {
        // Click derecho para usar item
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (currentSlot.itemID >= 0)
            {
                inventoryUI.UseItem(slotIndex);
            }
        }
    }

    #endregion
}
