using UnityEngine;
using UnityEngine.UIElements;
using Game.Player;

namespace Game.UI
{
    /// <summary>
    /// HUD que muestra los stats del jugador local
    /// Se actualiza automáticamente cuando los SyncVars cambian
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PlayerHUD : MonoBehaviour
    {
        // UI Toolkit - No necesitamos referencias serializadas
        private UIDocument uiDocument;
        private Label classLabel;
        private Label hpLabel;
        private Label manaLabel;
        private Label goldLabel;
        private Label levelLabel;
        private Label xpLabel;

        private PlayerStats playerStats;

        private void Start()
        {
            // Obtener el documento UI y buscar los elementos
            uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError("[PlayerHUD] UIDocument component not found!");
                return;
            }

            var root = uiDocument.rootVisualElement;

            // Query los elementos por nombre
            classLabel = root.Q<Label>("hud-class-text");
            hpLabel = root.Q<Label>("hud-hp-text");
            manaLabel = root.Q<Label>("hud-mana-text");
            goldLabel = root.Q<Label>("hud-gold-text");
            levelLabel = root.Q<Label>("hud-level-text");
            xpLabel = root.Q<Label>("hud-xp-text");

            // Verificar que todos los elementos se encontraron
            if (classLabel == null || hpLabel == null || manaLabel == null ||
                goldLabel == null || levelLabel == null || xpLabel == null)
            {
                Debug.LogError("[PlayerHUD] No se pudieron encontrar todos los elementos UI en el UXML!");
            }
        }

        private void Update()
        {
            // Buscar el PlayerStats del jugador local si no lo tenemos
            if (playerStats == null)
            {
                // Buscar el jugador local
                Game.Player.PlayerController[] players = FindObjectsByType<Game.Player.PlayerController>(FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    if (player.isLocalPlayer)
                    {
                        playerStats = player.GetComponent<PlayerStats>();
                        if (playerStats != null)
                        {
                            Debug.Log("[PlayerHUD] PlayerStats encontrado");
                            break;
                        }
                    }
                }
                return;
            }

            // Actualizar UI con los stats actuales
            UpdateHUD();
        }

        private void UpdateHUD()
        {
            if (playerStats == null) return;

            // Clase
            if (classLabel != null)
            {
                // Usar el SyncVar className en lugar de classData (que solo existe en servidor)
                classLabel.text = $"Clase: {playerStats.className}";
            }

            // HP
            if (hpLabel != null)
            {
                hpLabel.text = $"HP: {playerStats.currentHealth}/{playerStats.maxHealth}";
            }

            // Mana
            if (manaLabel != null)
            {
                manaLabel.text = $"Mana: {playerStats.currentMana}/{playerStats.maxMana}";
            }

            // Oro
            if (goldLabel != null)
            {
                goldLabel.text = $"Oro: {playerStats.gold}";
            }

            // Level
            if (levelLabel != null)
            {
                levelLabel.text = $"Nivel: {playerStats.level}";
            }

            // XP
            if (xpLabel != null)
            {
                xpLabel.text = $"XP: {playerStats.currentXP}/{playerStats.xpToNextLevel}";
            }
        }

        /// <summary>
        /// Fuerza la actualización del HUD (llamar cuando cambien stats importantes)
        /// </summary>
        public void ForceUpdate()
        {
            UpdateHUD();
        }
    }
}
