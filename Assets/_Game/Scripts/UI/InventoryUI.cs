using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Game.Player;
using Game.Items;

namespace Game.UI
{
    /// <summary>
    /// Manager del UI del inventario - UI Toolkit
    /// Sistema de swap por dos clicks (no drag & drop completo para MVP)
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class InventoryUI : MonoBehaviour
    {
        [Header("Controls")]
        public KeyCode toggleKey = KeyCode.I;

        private UIDocument uiDocument;
        private VisualElement inventoryPanel;
        private VisualElement slotsContainer;
        private Label selectedItemInfo;
        private Button closeButton;
        private VisualTreeAsset slotTemplate;

        private List<VisualElement> slotElements = new List<VisualElement>();
        private PlayerInventory playerInventory;
        private bool isInitialized = false;

        // Sistema de swap por dos clicks
        private int selectedSlotIndex = -1;

        private void Start()
        {
            // Obtener el documento UI y buscar los elementos
            uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError("[InventoryUI] UIDocument component not found!");
                return;
            }

            var root = uiDocument.rootVisualElement;

            // Query elementos
            inventoryPanel = root.Q<VisualElement>("inventory-panel");
            slotsContainer = root.Q<VisualElement>("inventory-slots-container");
            selectedItemInfo = root.Q<Label>("selected-item-info");
            closeButton = root.Q<Button>("inventory-close-btn");

            // Verificar que todos los elementos se encontraron
            if (inventoryPanel == null || slotsContainer == null)
            {
                Debug.LogError("[InventoryUI] No se pudieron encontrar todos los elementos UI en el UXML!");
            }

            // Suscribirse al botón de cerrar
            if (closeButton != null)
            {
                closeButton.clicked += CloseInventory;
            }

            // Cargar template del slot desde Resources
            slotTemplate = Resources.Load<VisualTreeAsset>("UI/GameWorld/UXML/InventorySlot");
            if (slotTemplate == null)
            {
                Debug.LogError("[InventoryUI] No se pudo cargar el template 'InventorySlot' desde Resources! Asegúrate de que esté en Assets/Resources/UI/GameWorld/UXML/");
            }

            // Ocultar panel al inicio
            if (inventoryPanel != null)
            {
                inventoryPanel.style.display = DisplayStyle.None;
            }
        }

        private void OnDisable()
        {
            // Cleanup
            if (closeButton != null)
            {
                closeButton.clicked -= CloseInventory;
            }

            // Desuscribirse del evento
            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged -= RefreshUI;
            }
        }

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
            // Buscar jugador local
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
            if (slotsContainer == null || slotTemplate == null) return;

            // Limpiar slots anteriores si existen
            slotsContainer.Clear();
            slotElements.Clear();

            // Crear slots según el tamaño del inventario
            int inventorySize = playerInventory.inventory.Count;
            for (int i = 0; i < inventorySize; i++)
            {
                // Instanciar template
                TemplateContainer slotInstance = slotTemplate.Instantiate();
                VisualElement slotRoot = slotInstance.Q<VisualElement>("inventory-slot");

                if (slotRoot == null)
                {
                    Debug.LogError($"[InventoryUI] No se encontró 'inventory-slot' en el template instanciado!");
                    continue;
                }

                // Guardar índice en userData
                slotRoot.userData = i;

                // Añadir al contenedor
                slotsContainer.Add(slotInstance);
                slotElements.Add(slotRoot);

                // Registrar eventos de click
                RegisterSlotEvents(slotRoot, i);
            }

            // Suscribirse a cambios en el inventario
            playerInventory.OnInventoryChanged += RefreshUI;

            // Refresh inicial
            RefreshUI();

            Debug.Log($"[InventoryUI] Inicializado con {inventorySize} slots.");
        }

        /// <summary>
        /// Registra eventos de click en un slot
        /// </summary>
        void RegisterSlotEvents(VisualElement slotRoot, int index)
        {
            slotRoot.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button == 0) // Left click
                {
                    OnSlotLeftClick(index);
                }
                else if (evt.button == 1) // Right click
                {
                    OnSlotRightClick(index);
                }
            });
        }

        /// <summary>
        /// Left click: Sistema de swap por dos clicks
        /// </summary>
        void OnSlotLeftClick(int clickedIndex)
        {
            if (selectedSlotIndex == -1)
            {
                // Primer click: Seleccionar slot
                selectedSlotIndex = clickedIndex;

                // Añadir clase visual de seleccionado
                if (clickedIndex >= 0 && clickedIndex < slotElements.Count)
                {
                    slotElements[clickedIndex].AddToClassList("inv-slot--selected");
                }

                // Actualizar info
                if (selectedItemInfo != null)
                {
                    selectedItemInfo.text = $"Slot {clickedIndex + 1} seleccionado. Click en otro slot para intercambiar.";
                }
            }
            else
            {
                // Segundo click: Swap
                if (clickedIndex != selectedSlotIndex)
                {
                    SwapSlots(selectedSlotIndex, clickedIndex);
                }

                // Deseleccionar
                if (selectedSlotIndex >= 0 && selectedSlotIndex < slotElements.Count)
                {
                    slotElements[selectedSlotIndex].RemoveFromClassList("inv-slot--selected");
                }

                selectedSlotIndex = -1;

                // Actualizar info
                if (selectedItemInfo != null)
                {
                    selectedItemInfo.text = "Click en un slot para seleccionar";
                }
            }
        }

        /// <summary>
        /// Right click: Usar item
        /// </summary>
        void OnSlotRightClick(int clickedIndex)
        {
            if (playerInventory == null) return;

            // Verificar que el slot tiene un item
            if (clickedIndex >= 0 && clickedIndex < playerInventory.inventory.Count)
            {
                InventorySlot slot = playerInventory.inventory[clickedIndex];
                if (slot.itemID >= 0) // Item válido
                {
                    UseItem(clickedIndex);
                }
            }
        }

        /// <summary>
        /// Actualiza toda la UI del inventario
        /// </summary>
        void RefreshUI()
        {
            if (playerInventory == null || slotsContainer == null) return;

            for (int i = 0; i < slotElements.Count && i < playerInventory.inventory.Count; i++)
            {
                UpdateSlot(slotElements[i], playerInventory.inventory[i]);
            }
        }

        /// <summary>
        /// Actualiza un slot individual
        /// </summary>
        void UpdateSlot(VisualElement slotRoot, InventorySlot slotData)
        {
            VisualElement icon = slotRoot.Q<VisualElement>("slot-icon");
            Label amountLabel = slotRoot.Q<Label>("slot-amount");

            // Obtener ItemData desde el ID
            ItemData itemData = slotData.itemID >= 0 ? ItemDatabase.Instance?.GetItem(slotData.itemID) : null;

            if (itemData == null) // Slot vacío
            {
                slotRoot.AddToClassList("inv-slot--empty");

                if (icon != null)
                {
                    icon.style.display = DisplayStyle.None;
                }

                if (amountLabel != null)
                {
                    amountLabel.style.display = DisplayStyle.None;
                }
            }
            else // Slot con item
            {
                slotRoot.RemoveFromClassList("inv-slot--empty");

                // Icono
                if (icon != null && itemData.icon != null)
                {
                    icon.style.backgroundImage = new StyleBackground(itemData.icon);
                    icon.style.display = DisplayStyle.Flex;
                }

                // Cantidad
                if (amountLabel != null)
                {
                    if (itemData.isStackable && slotData.amount > 1)
                    {
                        amountLabel.text = slotData.amount.ToString();
                        amountLabel.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        amountLabel.style.display = DisplayStyle.None;
                    }
                }
            }
        }

        /// <summary>
        /// Toggle del inventario
        /// </summary>
        public void ToggleInventory()
        {
            if (inventoryPanel != null)
            {
                if (inventoryPanel.style.display == DisplayStyle.None)
                {
                    inventoryPanel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    inventoryPanel.style.display = DisplayStyle.None;
                }
            }
        }

        public void OpenInventory()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.style.display = DisplayStyle.Flex;
            }
        }

        public void CloseInventory()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.style.display = DisplayStyle.None;
            }

            // Deseleccionar si hay algo seleccionado
            if (selectedSlotIndex >= 0 && selectedSlotIndex < slotElements.Count)
            {
                slotElements[selectedSlotIndex].RemoveFromClassList("inv-slot--selected");
                selectedSlotIndex = -1;
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
                Debug.Log($"[InventoryUI] Swap solicitado: {indexA} <-> {indexB}");
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
                Debug.Log($"[InventoryUI] Usar item en slot {slotIndex}");
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
    }
}
