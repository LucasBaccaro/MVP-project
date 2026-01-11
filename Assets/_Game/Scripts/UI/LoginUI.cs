using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Network;

namespace Game.UI
{
    /// <summary>
    /// Controlador del menú de login
    /// Permite al jugador ingresar su nombre y conectarse al servidor
    /// </summary>
    public class LoginUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button clientButton;
        [SerializeField] private TMP_Text statusText;

        [Header("Network")]
        [SerializeField] private NetworkManagerMMO networkManager;

        private void Start()
        {
            // Buscar el NetworkManager si no está asignado
            if (networkManager == null)
            {
                networkManager = FindFirstObjectByType<NetworkManagerMMO>();
            }

            // Configurar botones
            if (hostButton != null)
            {
                hostButton.onClick.AddListener(OnHostButtonClicked);
            }

            if (clientButton != null)
            {
                clientButton.onClick.AddListener(OnClientButtonClicked);
            }

            // Cargar nombre guardado si existe
            if (PlayerPrefs.HasKey("PlayerName"))
            {
                nameInputField.text = PlayerPrefs.GetString("PlayerName");
            }

            UpdateStatus("Ingresa tu nombre para comenzar");
        }

        private void OnHostButtonClicked()
        {
            string playerName = nameInputField.text.Trim();

            if (string.IsNullOrEmpty(playerName))
            {
                UpdateStatus("Por favor ingresa un nombre válido");
                return;
            }

            // Guardar nombre
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();

            // Asignar nombre al NetworkManager
            networkManager.playerName = playerName;

            // Iniciar como Host (Server + Client)
            UpdateStatus($"Iniciando servidor como {playerName}...");
            networkManager.StartMMOServer();
        }

        private void OnClientButtonClicked()
        {
            string playerName = nameInputField.text.Trim();

            if (string.IsNullOrEmpty(playerName))
            {
                UpdateStatus("Por favor ingresa un nombre válido");
                return;
            }

            // Guardar nombre
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();

            // Asignar nombre al NetworkManager
            networkManager.playerName = playerName;

            // Conectar como cliente
            UpdateStatus($"Conectando como {playerName}...");
            networkManager.ConnectAsClient("localhost");
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[LoginUI] {message}");
        }

        private void OnDestroy()
        {
            // Limpiar listeners
            if (hostButton != null)
            {
                hostButton.onClick.RemoveListener(OnHostButtonClicked);
            }

            if (clientButton != null)
            {
                clientButton.onClick.RemoveListener(OnClientButtonClicked);
            }
        }
    }
}
