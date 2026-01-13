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
        private UIDocument uiDocument;
        private VisualElement buttonsContainer;
        private VisualTreeAsset buttonTemplate;

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

            // Cargar template del botón desde Resources
            Debug.Log("[AbilityBarUI] Loading template from Resources/UI/GameWorld/UXML/AbilityButton");
            buttonTemplate = Resources.Load<VisualTreeAsset>("UI/GameWorld/UXML/AbilityButton");

            if (buttonTemplate == null)
            {
                Debug.LogError("[AbilityBarUI] No se pudo cargar el template 'AbilityButton' desde Resources! Asegúrate de que esté en Assets/Resources/UI/GameWorld/UXML/");
            }
            else
            {
                Debug.Log("[AbilityBarUI] Template loaded successfully");
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

            // Crear botón para cada habilidad
            for (int i = 0; i < playerCombat.abilities.Count; i++)
            {
                AbilityData ability = playerCombat.abilities[i];
                if (ability == null)
                {
                    Debug.LogWarning($"[AbilityBarUI] Ability at index {i} is NULL, skipping");
                    continue;
                }

                Debug.Log($"[AbilityBarUI] Creating button for ability: {ability.abilityName}");

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
                Debug.Log($"[AbilityBarUI] Button instance added to container for {ability.abilityName}");

                // Verificar el tamaño del botón
                Debug.Log($"[AbilityBarUI] Button {i} - Width: {buttonRoot.resolvedStyle.width}, Height: {buttonRoot.resolvedStyle.height}");
                Debug.Log($"[AbilityBarUI] Button {i} - Position: ({buttonRoot.resolvedStyle.left}, {buttonRoot.resolvedStyle.top})");
                Debug.Log($"[AbilityBarUI] Button {i} - Display: {buttonRoot.resolvedStyle.display}");

                // Crear wrapper AbilityButton
                AbilityButton abilityButton = new AbilityButton();
                abilityButton.Initialize(buttonRoot, ability, i, this);
                abilityButtons.Add(abilityButton);
            }

            Debug.Log($"[AbilityBarUI] {abilityButtons.Count} habilidades inicializadas y visibles");
            Debug.Log($"[AbilityBarUI] Container position: ({buttonsContainer.resolvedStyle.left}, {buttonsContainer.resolvedStyle.top}, {buttonsContainer.resolvedStyle.bottom})");
            Debug.Log($"[AbilityBarUI] Container size: {buttonsContainer.resolvedStyle.width}x{buttonsContainer.resolvedStyle.height}");
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

        private void Update()
        {
            // Actualizar todos los botones cada frame
            foreach (var button in abilityButtons)
            {
                button.UpdateButton();
            }

            // Hotkeys (1, 2, 3, 4)
            if (Input.GetKeyDown(KeyCode.Alpha1)) OnAbilityButtonClicked(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) OnAbilityButtonClicked(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) OnAbilityButtonClicked(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) OnAbilityButtonClicked(3);
        }

        private void OnDestroy()
        {
            // Desuscribirse de eventos
            if (playerCombat != null)
            {
                playerCombat.OnCooldownStarted -= OnCooldownStarted;
                playerCombat.OnCooldownReady -= OnCooldownReady;
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
        /// Inicializa el botón con datos de habilidad
        /// </summary>
        public void Initialize(VisualElement root, AbilityData abilityData, int index, AbilityBarUI bar)
        {
            buttonRoot = root;
            ability = abilityData;
            abilityIndex = index;
            abilityBar = bar;

            // Query elementos
            iconElement = root.Q<VisualElement>("ability-icon");
            cooldownOverlay = root.Q<VisualElement>("cooldown-overlay");
            cooldownText = root.Q<Label>("cooldown-text");
            hotkeyText = root.Q<Label>("hotkey-text");

            // Configurar icono (background-image)
            if (iconElement != null && ability.icon != null)
            {
                iconElement.style.backgroundImage = new StyleBackground(ability.icon);
            }

            // Configurar hotkey (1, 2, 3, 4)
            if (hotkeyText != null)
            {
                hotkeyText.text = (index + 1).ToString();
            }

            // Configurar click callback
            if (buttonRoot != null)
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
