using UnityEngine;
using UnityEngine.UIElements;
using Game.Player;
using System.Collections.Generic;

namespace Game.UI
{
    /// <summary>
    /// HUD gráfico del jugador con efecto Reveal (Masking) para las barras.
    /// Las barras se recortan de derecha a izquierda sin deformarse.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PlayerHUD : MonoBehaviour
    {
        // Class icon paths mapping (loaded from Assets/UI/Art/Classes/)
        // Keys in Spanish (game) -> Values point to English icon files
        private static readonly Dictionary<string, string> ClassIconPaths =
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                // Spanish class names (used in game)
                { "Guerrero", "Assets/UI/Art/Classes/MISC-Class_Warrior.png" },
                { "Mago", "Assets/UI/Art/Classes/MISC-Class_Mage.png" },
                { "Cazador", "Assets/UI/Art/Classes/MISC-Class_Hunter.png" },
                { "Sacerdote", "Assets/UI/Art/Classes/MISC-Class_Priest.png" },
                { "Picaro", "Assets/UI/Art/Classes/MISC-Class_Rogue.png" },
                { "Paladin", "Assets/UI/Art/Classes/MISC-Class_Paladin.png" },
                { "Druida", "Assets/UI/Art/Classes/MISC-Class_Druid.png" },
                { "Chaman", "Assets/UI/Art/Classes/MISC-Class_Shaman.png" },
            };

        private UIDocument uiDocument;
        private VisualElement root;

        // Bar Masks (controlled by code for reveal effect)
        private VisualElement hpBarMask;
        private VisualElement manaBarMask;
        private VisualElement xpBarMask;

        // Bar Labels
        private Label hpBarLabel;
        private Label manaBarLabel;
        private Label xpBarLabel;

        // Other elements
        private VisualElement portraitImage;
        private Label levelLabel;
        private VisualElement classIcon;
        private VisualElement zoneContainer;
        private Label playerNameLabel;

        // Player references
        private PlayerStats playerStats;
        private ZoneHandler zoneHandler;

        // Track current state to avoid unnecessary updates
        private string currentClassName = "";
        private bool lastSafeZoneState = false;

        private void Start()
        {
            uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError("[PlayerHUD] UIDocument component not found!");
                return;
            }

            root = uiDocument.rootVisualElement;
            CacheUIElements();
        }

        private void CacheUIElements()
        {
            // Bar Masks (these control the reveal effect)
            hpBarMask = root.Q<VisualElement>("hp-bar-mask");
            manaBarMask = root.Q<VisualElement>("mana-bar-mask");
            xpBarMask = root.Q<VisualElement>("xp-bar-mask");

            // Bar Labels
            hpBarLabel = root.Q<Label>("hp-bar-label");
            manaBarLabel = root.Q<Label>("mana-bar-label");
            xpBarLabel = root.Q<Label>("xp-bar-label");

            // Other elements
            portraitImage = root.Q<VisualElement>("portrait-image");
            levelLabel = root.Q<Label>("level-label");
            classIcon = root.Q<VisualElement>("class-icon");
            zoneContainer = root.Q<VisualElement>("zone-container");
            playerNameLabel = root.Q<Label>("player-name-label");

            // Verify critical elements
            if (hpBarMask == null || manaBarMask == null || xpBarMask == null)
            {
                Debug.LogError("[PlayerHUD] Failed to find bar mask elements!");
            }

            // Hide zone container by default
            if (zoneContainer != null)
            {
                zoneContainer.style.display = DisplayStyle.None;
            }
        }

        private void Update()
        {
            if (playerStats == null)
            {
                FindLocalPlayer();
                return;
            }

            UpdateUI();
        }

        private void FindLocalPlayer()
        {
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.isLocalPlayer)
                {
                    playerStats = player.GetComponent<PlayerStats>();
                    zoneHandler = player.GetComponent<ZoneHandler>();

                    if (playerStats != null)
                    {
                        Debug.Log("[PlayerHUD] PlayerStats found, initializing HUD");
                        UpdateUI();
                        break;
                    }
                }
            }
        }

        private void UpdateUI()
        {
            if (playerStats == null) return;

            UpdateHealthBar();
            UpdateManaBar();
            UpdateXPBar();
            UpdateLevel();
            UpdatePlayerName();
            UpdateClassIcon();
            UpdateSafeZoneIndicator();
        }

        /// <summary>
        /// Updates HP bar using mask width (reveal effect).
        /// </summary>
        private void UpdateHealthBar()
        {
            if (hpBarMask == null || hpBarLabel == null) return;

            float percentage = CalculatePercentage(playerStats.currentHealth, playerStats.maxHealth);
            hpBarMask.style.width = Length.Percent(percentage);
            hpBarLabel.text = $"{playerStats.currentHealth}/{playerStats.maxHealth}";
        }

        /// <summary>
        /// Updates Mana bar using mask width (reveal effect).
        /// </summary>
        private void UpdateManaBar()
        {
            if (manaBarMask == null || manaBarLabel == null) return;

            float percentage = CalculatePercentage(playerStats.currentMana, playerStats.maxMana);
            manaBarMask.style.width = Length.Percent(percentage);
            manaBarLabel.text = $"{playerStats.currentMana}/{playerStats.maxMana}";
        }

        /// <summary>
        /// Updates XP bar using mask width (reveal effect).
        /// </summary>
        private void UpdateXPBar()
        {
            if (xpBarMask == null || xpBarLabel == null) return;

            float percentage = CalculatePercentage(playerStats.currentXP, playerStats.xpToNextLevel);
            xpBarMask.style.width = Length.Percent(percentage);
            xpBarLabel.text = $"{playerStats.currentXP}/{playerStats.xpToNextLevel}";
        }

        /// <summary>
        /// Updates the level display.
        /// </summary>
        private void UpdateLevel()
        {
            if (levelLabel == null) return;
            levelLabel.text = playerStats.level.ToString();
        }

        /// <summary>
        /// Updates the player name display.
        /// </summary>
        private void UpdatePlayerName()
        {
            if (playerNameLabel == null) return;
            playerNameLabel.text = playerStats.EntityName;
        }

        /// <summary>
        /// Updates the class icon dynamically based on player class.
        /// </summary>
        private void UpdateClassIcon()
        {
            if (classIcon == null)
            {
                Debug.LogWarning("[PlayerHUD] classIcon element is null!");
                return;
            }

            if (string.IsNullOrEmpty(playerStats.className))
            {
                Debug.LogWarning("[PlayerHUD] playerStats.className is null or empty!");
                return;
            }

            // Only update if class changed
            if (currentClassName == playerStats.className) return;

            Debug.Log($"[PlayerHUD] Updating class icon from '{currentClassName}' to '{playerStats.className}'");
            currentClassName = playerStats.className;

            // Load and set the class icon
            if (ClassIconPaths.TryGetValue(playerStats.className, out string path))
            {
#if UNITY_EDITOR
                // In editor, load using AssetDatabase
                var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    classIcon.style.backgroundImage = new StyleBackground(texture);
                    Debug.Log($"[PlayerHUD] Class icon set successfully: {playerStats.className} -> {path}");
                }
                else
                {
                    Debug.LogError($"[PlayerHUD] Failed to load texture at path: {path}");
                }
#else
                // In build, use Resources
                string resourcePath = $"ClassIcons/MISC-Class_{playerStats.className}";
                var sprite = Resources.Load<Sprite>(resourcePath);
                if (sprite != null)
                {
                    classIcon.style.backgroundImage = new StyleBackground(sprite);
                }
                else
                {
                    Debug.LogWarning($"[PlayerHUD] Class icon not found in Resources: {resourcePath}");
                }
#endif
            }
            else
            {
                Debug.LogWarning($"[PlayerHUD] No icon path found for class: '{playerStats.className}'");
            }
        }

        /// <summary>
        /// Updates the unsafe zone indicator visibility.
        /// Shows when player is in UNSAFE zone, hides when in SAFE zone.
        /// </summary>
        private void UpdateSafeZoneIndicator()
        {
            if (zoneContainer == null || zoneHandler == null) return;

            bool isInSafeZone = zoneHandler.isSafeZone;

            // Only update if state changed
            if (lastSafeZoneState == isInSafeZone) return;
            lastSafeZoneState = isInSafeZone;

            // Show indicator when in UNSAFE zone (not safe)
            zoneContainer.style.display = isInSafeZone ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>
        /// Calculates percentage (0-100) with safety checks.
        /// </summary>
        private float CalculatePercentage(float current, float max)
        {
            if (max <= 0) return 0f;
            return Mathf.Clamp((current / max) * 100f, 0f, 100f);
        }

        /// <summary>
        /// Sets the portrait image from a Texture2D.
        /// </summary>
        public void SetPortrait(Texture2D texture)
        {
            if (portraitImage == null || texture == null) return;
            portraitImage.style.backgroundImage = new StyleBackground(texture);
        }

        /// <summary>
        /// Sets the portrait image from a Sprite.
        /// </summary>
        public void SetPortrait(Sprite sprite)
        {
            if (portraitImage == null || sprite == null) return;
            portraitImage.style.backgroundImage = new StyleBackground(sprite);
        }

        /// <summary>
        /// Forces a full HUD update.
        /// </summary>
        public void ForceUpdate()
        {
            // Reset cached states to force update
            currentClassName = "";
            lastSafeZoneState = !lastSafeZoneState;
            UpdateUI();
        }
    }
}
