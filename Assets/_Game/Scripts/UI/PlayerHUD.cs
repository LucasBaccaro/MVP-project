using UnityEngine;
using TMPro;
using Game.Player;

namespace Game.UI
{
    /// <summary>
    /// HUD que muestra los stats del jugador local
    /// Se actualiza automáticamente cuando los SyncVars cambian
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Texto que muestra la clase del jugador")]
        public TMP_Text classText;

        [Tooltip("Texto que muestra HP actual/máximo")]
        public TMP_Text hpText;

        [Tooltip("Texto que muestra Mana actual/máximo")]
        public TMP_Text manaText;

        [Tooltip("Texto que muestra el oro")]
        public TMP_Text goldText;

        [Tooltip("Texto que muestra el nivel")]
        public TMP_Text levelText;

        [Tooltip("Texto que muestra XP")]
        public TMP_Text xpText;

        [Header("Runtime")]
        private PlayerStats playerStats;

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
            if (classText != null)
            {
                // Usar el SyncVar className en lugar de classData (que solo existe en servidor)
                classText.text = $"Clase: {playerStats.className}";
            }

            // HP
            if (hpText != null)
            {
                hpText.text = $"HP: {playerStats.currentHealth}/{playerStats.maxHealth}";
            }

            // Mana
            if (manaText != null)
            {
                manaText.text = $"Mana: {playerStats.currentMana}/{playerStats.maxMana}";
            }

            // Oro
            if (goldText != null)
            {
                goldText.text = $"Oro: {playerStats.gold}";
            }

            // Level
            if (levelText != null)
            {
                levelText.text = $"Nivel: {playerStats.level}";
            }

            // XP
            if (xpText != null)
            {
                xpText.text = $"XP: {playerStats.currentXP}/{playerStats.xpToNextLevel}";
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
