using UnityEngine;
using UnityEngine.UI;
using Game.Items;
using System.Collections.Generic;

namespace Game.UI
{
    public class LootUI : MonoBehaviour
    {
        public static LootUI Instance;

        [Header("References")]
        public GameObject lootPanel; // El panel visual
        public Transform slotsContainer;
        public GameObject slotPrefab;
        public Button closeButton;
        
        [Header("UI State")]
        private LootBag currentLootBag;
        private List<InventorySlotUI> uiSlots = new List<InventorySlotUI>();

        private void Awake()
        {
            Instance = this;
            if (lootPanel != null) lootPanel.SetActive(false);
            
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        public void Open(LootBag lootBag)
        {
            currentLootBag = lootBag;
            
            // Suscribirse a cambios en la SyncList para refrescar en tiempo real
            currentLootBag.items.Callback += OnLootUpdated;
            
            lootPanel.SetActive(true);
            RefreshUI();
        }

        public void Close()
        {
            if (currentLootBag != null)
            {
                currentLootBag.items.Callback -= OnLootUpdated;
                currentLootBag = null;
            }
            lootPanel.SetActive(false);
        }

        void OnLootUpdated(Mirror.SyncList<InventorySlot>.Operation op, int index, InventorySlot oldItem, InventorySlot newItem)
        {
            RefreshUI();
        }

        void Update()
        {
            // Cerrar si la bolsa desaparece (ej. otro jugador recogió todo)
            if (lootPanel.activeSelf && currentLootBag == null)
            {
                Close();
            }
            
            // Cerrar si me alejo demasiado
            if (lootPanel.activeSelf && currentLootBag != null)
            {
                 // Usamos PlayerController local para la posición? O simple distancia si static instance
                 // Simplificación: si LootBag se destruye (es null), se cierra en el check de arriba.
            }
        }

        void RefreshUI()
        {
            // Limpiar slots viejos
            foreach (Transform child in slotsContainer)
            {
                Destroy(child.gameObject);
            }
            uiSlots.Clear();

            if (currentLootBag == null) return;

            // Crear nuevos slots
            for (int i = 0; i < currentLootBag.items.Count; i++)
            {
                InventorySlot slotData = currentLootBag.items[i];
                GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
                InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();

                if (slotUI != null)
                {
                    // Asignar datos visuales
                    // InventorySlotUI espera solo InventorySlot (struct)
                    slotUI.UpdateSlot(slotData);
                    
                    // Configurar evento de click derecho para recoger
                    // Usamos el Action que añadimos a InventorySlotUI
                    int index = i; 
                    
                    // Sobreescribir el comportamiento de click derecho de este slot
                    slotUI.slotIndex = index; // Importante asignar el índice correcto
                    slotUI.OnRightClickAction = (idx) => TakeItem(idx);
                }
            }
        }

        public void TakeItem(int index)
        {
            if (currentLootBag != null)
            {
                currentLootBag.CmdTakeItem(index);
            }
        }
    }
}
