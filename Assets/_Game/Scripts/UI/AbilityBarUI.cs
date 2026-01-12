using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Combat;
using Game.Core;
using System.Collections.Generic;

namespace Game.UI
{
    /// <summary>
    /// Barra de habilidades con botones y cooldowns
    /// </summary>
    public class AbilityBarUI : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Prefab del botón de habilidad")]
        public GameObject abilityButtonPrefab;
        
        [Tooltip("Contenedor de los botones")]
        public Transform buttonsContainer;

        [Header("Colors")]
        public Color normalColor = Color.white;
        public Color cooldownColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        public Color noManaColor = new Color(0.5f, 0.2f, 0.2f, 1f);

        private PlayerCombat playerCombat;
        private List<AbilityButton> abilityButtons = new List<AbilityButton>();
        private bool isInitialized = false; // Flag para evitar múltiples inicializaciones
        private Coroutine findPlayerCoroutine = null; // Referencia al coroutine

        private void Awake()
        {
            // Awake se llama una sola vez cuando el GameObject se crea
            if (findPlayerCoroutine == null && !isInitialized)
            {
                Debug.Log("[AbilityBarUI] Awake - Iniciando búsqueda de PlayerCombat");
                findPlayerCoroutine = StartCoroutine(FindPlayerCombat());
            }
        }

        private System.Collections.IEnumerator FindPlayerCombat()
        {
            // Si ya se inicializó, no hacer nada
            if (isInitialized)
            {
                Debug.Log("[AbilityBarUI] Ya inicializado, saltando...");
                yield break;
            }

            int maxAttempts = 10;
            int attempts = 0;

            while (attempts < maxAttempts && !isInitialized)
            {
                attempts++;
                
                // Esperar antes de buscar
                yield return new WaitForSeconds(0.5f);

                // Buscar jugador local
                foreach (var player in FindObjectsOfType<PlayerCombat>())
                {
                    if (player.isLocalPlayer)
                    {
                        playerCombat = player;
                        isInitialized = true; // Marcar como inicializado
                        findPlayerCoroutine = null; // Limpiar referencia
                        
                        InitializeAbilityBar();
                        
                        // Suscribirse a eventos de cooldown
                        playerCombat.OnCooldownStarted += OnCooldownStarted;
                        playerCombat.OnCooldownReady += OnCooldownReady;
                        
                        Debug.Log($"[AbilityBarUI] PlayerCombat encontrado en intento {attempts} - Inicialización completa");
                        yield break; // IMPORTANTE: Salir del coroutine
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
        /// Inicializa los botones de habilidades
        /// </summary>
        private void InitializeAbilityBar()
        {
            if (playerCombat == null || playerCombat.abilities == null) return;

            // Limpiar botones existentes
            foreach (Transform child in buttonsContainer)
            {
                Destroy(child.gameObject);
            }
            abilityButtons.Clear();

            // Crear botón para cada habilidad
            for (int i = 0; i < playerCombat.abilities.Count; i++)
            {
                AbilityData ability = playerCombat.abilities[i];
                
                if (ability == null) continue;

                // Instanciar botón
                GameObject buttonObj = Instantiate(abilityButtonPrefab, buttonsContainer);
                
                // Añadir componente AbilityButton dinámicamente
                AbilityButton abilityButton = buttonObj.AddComponent<AbilityButton>();
                
                if (abilityButton != null)
                {
                    abilityButton.Initialize(ability, i, this);
                    abilityButtons.Add(abilityButton);
                }
                else
                {
                    Debug.LogError($"[AbilityBarUI] No se pudo añadir AbilityButton al botón {i}");
                }
            }

            Debug.Log($"[AbilityBarUI] {abilityButtons.Count} habilidades inicializadas");
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
        }

        private void OnDestroy()
        {
            // Desuscribirse de eventos
            if (playerCombat != null)
            {
                playerCombat.OnCooldownStarted -= OnCooldownStarted;
                playerCombat.OnCooldownReady -= OnCooldownReady;
            }
        }
    }

    /// <summary>
    /// Componente individual de un botón de habilidad
    /// </summary>
    public class AbilityButton : MonoBehaviour
    {
        [Header("UI References")]
        public Image iconImage;
        public Image cooldownOverlay;
        public TextMeshProUGUI cooldownText;
        public TextMeshProUGUI hotkeyText;
        public Button button;

        private AbilityData ability;
        private int abilityIndex;
        private AbilityBarUI abilityBar;
        
        private float cooldownEndTime;
        private bool isOnCooldown;

        /// <summary>
        /// Inicializa el botón con datos de habilidad
        /// </summary>
        public void Initialize(AbilityData abilityData, int index, AbilityBarUI bar)
        {
            ability = abilityData;
            abilityIndex = index;
            abilityBar = bar;

            // Auto-buscar referencias si no están asignadas
            if (button == null) button = GetComponent<Button>();
            if (iconImage == null) iconImage = transform.Find("Icon")?.GetComponent<Image>();
            if (cooldownOverlay == null) cooldownOverlay = transform.Find("CooldownOverlay")?.GetComponent<Image>();
            if (cooldownText == null) cooldownText = transform.Find("CooldownText")?.GetComponent<TextMeshProUGUI>();
            if (hotkeyText == null) hotkeyText = transform.Find("HotkeyText")?.GetComponent<TextMeshProUGUI>();

            // Configurar icono
            if (iconImage != null && ability.icon != null)
            {
                iconImage.sprite = ability.icon;
            }

            // Configurar hotkey (1, 2, 3, 4)
            if (hotkeyText != null)
            {
                hotkeyText.text = (index + 1).ToString();
            }

            // Configurar botón
            if (button != null)
            {
                button.onClick.AddListener(() => abilityBar.OnAbilityButtonClicked(abilityIndex));
            }

            // Ocultar overlay de cooldown
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = 0f;
            }

            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Inicia el cooldown visual
        /// </summary>
        public void StartCooldown(float cooldownTime)
        {
            isOnCooldown = true;
            cooldownEndTime = Time.time + cooldownTime;

            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(true);
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
                cooldownOverlay.fillAmount = 0f;
            }

            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(false);
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
                    // Actualizar overlay
                    if (cooldownOverlay != null)
                    {
                        float cooldownPercent = remainingTime / ability.cooldownTime;
                        cooldownOverlay.fillAmount = cooldownPercent;
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
    }
}
