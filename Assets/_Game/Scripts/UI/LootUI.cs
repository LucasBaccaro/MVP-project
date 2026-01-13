using UnityEngine;
using UnityEngine.UIElements;
using Game.Items;
using System.Collections.Generic;

namespace Game.UI
{
    /// <summary>
    /// Panel de loot - UI Toolkit
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class LootUI : MonoBehaviour
    {
        public static LootUI Instance;

        private UIDocument uiDocument;
        private VisualElement lootPanel;
        private VisualElement slotsContainer;
        private Button closeButton;

        private LootBag currentLootBag;
        private List<VisualElement> uiSlots = new List<VisualElement>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Obtener el documento UI y buscar los elementos
            uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError("[LootUI] UIDocument component not found!");
                return;
            }

            var root = uiDocument.rootVisualElement;

            // Query elementos
            lootPanel = root.Q<VisualElement>("loot-panel");
            slotsContainer = root.Q<VisualElement>("loot-slots-container");
            closeButton = root.Q<Button>("loot-close-btn");

            // Verificar que todos los elementos se encontraron
            if (lootPanel == null || slotsContainer == null)
            {
                Debug.LogError("[LootUI] No se pudieron encontrar todos los elementos UI en el UXML!");
            }

            // Suscribirse al botón de cerrar
            if (closeButton != null)
            {
                closeButton.clicked += Close;
            }

            // Ocultar panel al inicio
            if (lootPanel != null)
            {
                lootPanel.style.display = DisplayStyle.None;
            }
        }

        private void OnDisable()
        {
            // Cleanup
            if (closeButton != null)
            {
                closeButton.clicked -= Close;
            }
        }

        /// <summary>
        /// Abre el panel de loot
        /// </summary>
        public void Open(LootBag lootBag)
        {
            currentLootBag = lootBag;

            // Suscribirse a cambios en la SyncList para refrescar en tiempo real
            currentLootBag.items.Callback += OnLootUpdated;

            if (lootPanel != null)
            {
                lootPanel.style.display = DisplayStyle.Flex;
            }

            RefreshUI();
        }

        /// <summary>
        /// Cierra el panel de loot
        /// </summary>
        public void Close()
        {
            if (currentLootBag != null)
            {
                currentLootBag.items.Callback -= OnLootUpdated;
                currentLootBag = null;
            }

            if (lootPanel != null)
            {
                lootPanel.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Callback cuando la SyncList de loot cambia
        /// </summary>
        void OnLootUpdated(Mirror.SyncList<InventorySlot>.Operation op, int index, InventorySlot oldItem, InventorySlot newItem)
        {
            RefreshUI();
        }

        void Update()
        {
            // Cerrar si la bolsa desaparece (ej. otro jugador recogió todo)
            if (lootPanel != null && lootPanel.style.display == DisplayStyle.Flex && currentLootBag == null)
            {
                Close();
            }
        }

        /// <summary>
        /// Refresca la UI del loot
        /// </summary>
        void RefreshUI()
        {
            if (slotsContainer == null) return;

            // Limpiar slots viejos
            slotsContainer.Clear();
            uiSlots.Clear();

            if (currentLootBag == null) return;

            // Crear nuevos slots
            for (int i = 0; i < currentLootBag.items.Count; i++)
            {
                InventorySlot slotData = currentLootBag.items[i];

                // Crear slot como fila (icon + info)
                VisualElement slotElement = CreateLootSlot(slotData, i);
                slotsContainer.Add(slotElement);
                uiSlots.Add(slotElement);
            }

            // Mostrar mensaje si no hay items
            if (currentLootBag.items.Count == 0)
            {
                Label emptyLabel = new Label("Bolsa vacía");
                emptyLabel.AddToClassList("loot__empty-message");
                slotsContainer.Add(emptyLabel);
            }
        }

        /// <summary>
        /// Crea un slot visual de loot
        /// </summary>
        private VisualElement CreateLootSlot(InventorySlot slotData, int index)
        {
            // Obtener ItemData desde el ID
            ItemData itemData = slotData.itemID >= 0 ? ItemDatabase.Instance?.GetItem(slotData.itemID) : null;

            // Contenedor principal (fila)
            VisualElement slot = new VisualElement();
            slot.AddToClassList("loot-slot");

            // Icono
            VisualElement icon = new VisualElement();
            icon.AddToClassList("loot-slot__icon");

            if (itemData != null && itemData.icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(itemData.icon);
            }

            // Contenedor de info (nombre + cantidad)
            VisualElement infoContainer = new VisualElement();
            infoContainer.AddToClassList("loot-slot__info");

            // Nombre del item
            Label nameLabel = new Label(itemData != null ? itemData.itemName : "Vacío");
            nameLabel.AddToClassList("loot-slot__name");

            // Cantidad
            Label quantityLabel = new Label($"Cantidad: {slotData.amount}");
            quantityLabel.AddToClassList("loot-slot__quantity");

            // Ensamblar
            infoContainer.Add(nameLabel);
            infoContainer.Add(quantityLabel);

            slot.Add(icon);
            slot.Add(infoContainer);

            // Registrar click derecho para tomar item
            slot.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button == 1) // Right click
                {
                    TakeItem(index);
                }
            });

            return slot;
        }

        /// <summary>
        /// Toma un item del loot
        /// </summary>
        public void TakeItem(int index)
        {
            if (currentLootBag != null)
            {
                currentLootBag.CmdTakeItem(index);
            }
        }
    }
}
