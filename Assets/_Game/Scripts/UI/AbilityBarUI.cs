using UnityEngine;
using UnityEngine.UIElements;
using Game.Combat;
using Game.Core;
using System.Collections.Generic;

namespace Game.UI
{
    /// <summary>
    /// Barra de habilidades con botones y cooldowns - UI Toolkit
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class AbilityBarUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private VisualTreeAsset buttonTemplate;

        private UIDocument uiDocument;
        private VisualElement buttonsContainer;

        private PlayerCombat playerCombat;
        private List<AbilityButton> abilityButtons = new List<AbilityButton>();
        private bool isInitialized = false;

        private void Awake()
        {
            Debug.Log("[AbilityBarUI] Awake called");
        }

        private void Start()
        {
            Debug.Log("[AbilityBarUI] Start called - Initializing");

            // Obtener el documento UI y buscar los elementos
            uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError("[AbilityBarUI] UIDocument component not found!");
                return;
            }

            Debug.Log("[AbilityBarUI] UIDocument found");
            var root = uiDocument.rootVisualElement;

            if (root == null)
            {
                Debug.LogError("[AbilityBarUI] Root visual element is NULL!");
                return;
            }

            Debug.Log("[AbilityBarUI] Root element found, querying container");

            // Query contenedor de botones
            buttonsContainer = root.Q<VisualElement>("ability-buttons-container");

            if (buttonsContainer == null)
            {
                Debug.LogError("[AbilityBarUI] No se pudo encontrar 'ability-buttons-container' en el UXML!");
            }
            else
            {
                Debug.Log("[AbilityBarUI] Buttons container found");
            }

            if (buttonTemplate == null)
            {
                Debug.LogError("[AbilityBarUI] Button Template no asignado! Asigna el VisualTreeAsset en el Inspector.");
                return;
            }

            // Inicializar PlayerCombat
            if (!isInitialized)
            {
                Debug.Log("[AbilityBarUI] Starting PlayerCombat search coroutine");
                StartCoroutine(FindPlayerCombat());
            }
        }

        private System.Collections.IEnumerator FindPlayerCombat()
        {
            if (isInitialized) yield break;

            int maxAttempts = 10;
            int attempts = 0;

            while (attempts < maxAttempts && !isInitialized)
            {
                attempts++;
                yield return new WaitForSeconds(0.5f);

                // Buscar jugador local
                foreach (var player in FindObjectsOfType<PlayerCombat>())
                {
                    if (player.isLocalPlayer)
                    {
                        playerCombat = player;
                        isInitialized = true;

                        InitializeAbilityBar();

                        // Suscribirse a eventos de cooldown
                        playerCombat.OnCooldownStarted += OnCooldownStarted;
                        playerCombat.OnCooldownReady += OnCooldownReady;
                        playerCombat.OnAbilitiesUpdated += OnAbilitiesUpdated;

                        Debug.Log($"[AbilityBarUI] PlayerCombat encontrado en intento {attempts} - Inicialización completa");
                        yield break;
                    }
                }

                Debug.Log($"[AbilityBarUI] Intento {attempts}/{maxAttempts} - PlayerCombat no encontrado aún");
            }

            if (!isInitialized)
            {
                Debug.LogWarning("[AbilityBarUI] No se encontró PlayerCombat local después de 10 intentos");
            }
        }

        /// <summary>
        /// Inicializa los botones de habilidades dinámicamente
        /// </summary>
        private void InitializeAbilityBar()
        {
            Debug.Log("[AbilityBarUI] InitializeAbilityBar called");

            if (playerCombat == null)
            {
                Debug.LogError("[AbilityBarUI] playerCombat is NULL!");
                return;
            }

            if (playerCombat.abilities == null)
            {
                Debug.LogError("[AbilityBarUI] playerCombat.abilities is NULL!");
                return;
            }

            if (buttonsContainer == null)
            {
                Debug.LogError("[AbilityBarUI] buttonsContainer is NULL!");
                return;
            }

            if (buttonTemplate == null)
            {
                Debug.LogError("[AbilityBarUI] buttonTemplate is NULL!");
                return;
            }

            Debug.Log($"[AbilityBarUI] Player has {playerCombat.abilities.Count} abilities");

            // Limpiar botones existentes
            buttonsContainer.Clear();
            abilityButtons.Clear();

            // SIEMPRE crear 6 slots
            const int TOTAL_SLOTS = 6;

            for (int i = 0; i < TOTAL_SLOTS; i++)
            {
                // Obtener habilidad si existe (null si el slot está vacío)
                AbilityData ability = (i < playerCombat.abilities.Count) ? playerCombat.abilities[i] : null;

                Debug.Log($"[AbilityBarUI] Creating slot {i + 1}/6 - {(ability != null ? ability.abilityName : "Empty")}");

                // Instanciar template
                TemplateContainer buttonInstance = buttonTemplate.Instantiate();
                VisualElement buttonRoot = buttonInstance.Q<VisualElement>("ability-button");

                if (buttonRoot == null)
                {
                    Debug.LogError($"[AbilityBarUI] No se encontró 'ability-button' en el template instanciado!");
                    continue;
                }

                // Añadir al contenedor
                buttonsContainer.Add(buttonInstance);

                // Crear wrapper AbilityButton (funciona con ability null para slots vacíos)
                AbilityButton abilityButton = new AbilityButton();
                abilityButton.Initialize(buttonRoot, ability, i, this);
                abilityButtons.Add(abilityButton);
            }

            Debug.Log($"[AbilityBarUI] {TOTAL_SLOTS} slots inicializados ({playerCombat.abilities.Count} con habilidades)");
        }

        /// <summary>
        /// Llamado cuando se presiona un botón de habilidad
        /// </summary>
        public void OnAbilityButtonClicked(int abilityIndex)
        {
            if (playerCombat != null)
            {
                playerCombat.TryUseAbility(abilityIndex);
            }
        }

        /// <summary>
        /// Evento: Cooldown iniciado
        /// </summary>
        private void OnCooldownStarted(int abilityIndex, float cooldownTime)
        {
            if (abilityIndex >= 0 && abilityIndex < abilityButtons.Count)
            {
                abilityButtons[abilityIndex].StartCooldown(cooldownTime);
            }
        }

        /// <summary>
        /// Evento: Cooldown listo
        /// </summary>
        private void OnCooldownReady(int abilityIndex)
        {
            if (abilityIndex >= 0 && abilityIndex < abilityButtons.Count)
            {
                abilityButtons[abilityIndex].ResetCooldown();
            }
        }

        /// <summary>
        /// Evento: Habilidades actualizadas (reinicializar UI)
        /// </summary>
        private void OnAbilitiesUpdated()
        {
            Debug.Log("[AbilityBarUI] Habilidades actualizadas, reinicializando barra...");
            InitializeAbilityBar();
        }

        private void Update()
        {
            // Actualizar todos los botones cada frame
            foreach (var button in abilityButtons)
            {
                button.UpdateButton();
            }

            // Hotkeys (1, 2, 3, 4, 5, 6) - Solo funcionan si el slot tiene habilidad
            if (Input.GetKeyDown(KeyCode.Alpha1)) TryUseAbilityAtSlot(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TryUseAbilityAtSlot(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TryUseAbilityAtSlot(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) TryUseAbilityAtSlot(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) TryUseAbilityAtSlot(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) TryUseAbilityAtSlot(5);
        }

        /// <summary>
        /// Intenta usar habilidad en el slot especificado (solo si hay habilidad asignada)
        /// </summary>
        private void TryUseAbilityAtSlot(int slotIndex)
        {
            // Verificar que el slot tenga una habilidad asignada
            if (playerCombat != null && slotIndex < playerCombat.abilities.Count)
            {
                OnAbilityButtonClicked(slotIndex);
            }
        }

        private void OnDestroy()
        {
            // Desuscribirse de eventos
            if (playerCombat != null)
            {
                playerCombat.OnCooldownStarted -= OnCooldownStarted;
                playerCombat.OnCooldownReady -= OnCooldownReady;
                playerCombat.OnAbilitiesUpdated -= OnAbilitiesUpdated;
            }

            // Cleanup de botones
            foreach (var button in abilityButtons)
            {
                button.Cleanup();
            }
        }
    }

    /// <summary>
    /// Wrapper para un botón de habilidad individual (UI Toolkit)
    /// </summary>
    public class AbilityButton
    {
        private VisualElement buttonRoot;
        private VisualElement backgroundElement;  // NUEVO: referencia al background
        private VisualElement iconElement;
        private VisualElement cooldownOverlay;
        private Label cooldownText;
        private Label hotkeyText;

        private AbilityData ability;
        private int abilityIndex;
        private AbilityBarUI abilityBar;

        private float cooldownEndTime;
        private bool isOnCooldown;

        /// <summary>
        /// Inicializa el botón con datos de habilidad (puede ser null para slots vacíos)
        /// </summary>
        public void Initialize(VisualElement root, AbilityData abilityData, int index, AbilityBarUI bar)
        {
            buttonRoot = root;
            ability = abilityData;
            abilityIndex = index;
            abilityBar = bar;

            // Query elementos
            backgroundElement = root.Q<VisualElement>("ability-background");
            iconElement = root.Q<VisualElement>("ability-icon");
            cooldownOverlay = root.Q<VisualElement>("cooldown-overlay");
            cooldownText = root.Q<Label>("cooldown-text");
            hotkeyText = root.Q<Label>("hotkey-text");

            // IMPORTANTE: Asegurar que el background SIEMPRE esté visible
            if (backgroundElement != null)
            {
                backgroundElement.style.display = DisplayStyle.Flex;
                Debug.Log($"[AbilityButton] Background configurado para slot {index + 1}");
            }
            else
            {
                Debug.LogError($"[AbilityButton] No se encontró ability-background en slot {index + 1}!");
            }

            // Configurar icono (background-image) solo si hay habilidad
            if (iconElement != null)
            {
                if (ability != null && ability.icon != null)
                {
                    iconElement.style.backgroundImage = new StyleBackground(ability.icon);
                    iconElement.style.display = DisplayStyle.Flex;
                }
                else
                {
                    // Slot vacío: ocultar icono para que solo se vea el background
                    iconElement.style.display = DisplayStyle.None;
                }
            }

            // Configurar hotkey (1, 2, 3, 4, 5, 6)
            if (hotkeyText != null)
            {
                hotkeyText.text = (index + 1).ToString();
            }

            // Configurar click callback solo si hay habilidad
            if (buttonRoot != null && ability != null)
            {
                buttonRoot.RegisterCallback<ClickEvent>(evt => abilityBar.OnAbilityButtonClicked(abilityIndex));
            }

            // Ocultar overlay de cooldown inicialmente
            if (cooldownOverlay != null)
            {
                cooldownOverlay.style.height = Length.Percent(0);
                cooldownOverlay.style.display = DisplayStyle.None;
            }

            if (cooldownText != null)
            {
                cooldownText.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Inicia el cooldown visual
        /// </summary>
        public void StartCooldown(float cooldownTime)
        {
            isOnCooldown = true;
            cooldownEndTime = Time.time + cooldownTime;

            if (cooldownOverlay != null)
            {
                cooldownOverlay.style.display = DisplayStyle.Flex;
            }

            if (cooldownText != null)
            {
                cooldownText.style.display = DisplayStyle.Flex;
            }

            if (buttonRoot != null)
            {
                buttonRoot.AddToClassList("ability-button--on-cooldown");
            }
        }

        /// <summary>
        /// Resetea el cooldown
        /// </summary>
        public void ResetCooldown()
        {
            isOnCooldown = false;

            if (cooldownOverlay != null)
            {
                cooldownOverlay.style.height = Length.Percent(0);
                cooldownOverlay.style.display = DisplayStyle.None;
            }

            if (cooldownText != null)
            {
                cooldownText.style.display = DisplayStyle.None;
            }

            if (buttonRoot != null)
            {
                buttonRoot.RemoveFromClassList("ability-button--on-cooldown");
            }
        }

        /// <summary>
        /// Actualiza el botón cada frame
        /// </summary>
        public void UpdateButton()
        {
            // Solo actualizar si hay una habilidad asignada
            if (ability == null) return;

            if (isOnCooldown)
            {
                float remainingTime = cooldownEndTime - Time.time;

                if (remainingTime > 0)
                {
                    // Actualizar overlay (altura crece de 0% a 100%)
                    if (cooldownOverlay != null)
                    {
                        float cooldownPercent = (remainingTime / ability.cooldownTime) * 100f;
                        cooldownOverlay.style.height = Length.Percent(cooldownPercent);
                    }

                    // Actualizar texto
                    if (cooldownText != null)
                    {
                        cooldownText.text = remainingTime.ToString("F1");
                    }
                }
                else
                {
                    ResetCooldown();
                }
            }
        }

        /// <summary>
        /// Limpia eventos registrados
        /// </summary>
        public void Cleanup()
        {
            // UI Toolkit maneja el cleanup automáticamente cuando se destruye el elemento
            // pero por consistencia dejamos este método
        }
    }
}
