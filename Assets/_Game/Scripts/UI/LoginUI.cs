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
        [SerializeField] private TMP_Dropdown classDropdown;
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

            // Configurar dropdown de clases
            if (classDropdown != null && networkManager != null)
            {
                classDropdown.ClearOptions();
                System.Collections.Generic.List<string> classNames = new System.Collections.Generic.List<string>();

                foreach (var classData in networkManager.availableClasses)
                {
                    if (classData != null)
                    {
                        classNames.Add(classData.className);
                    }
                }

                classDropdown.AddOptions(classNames);

                // Cargar clase guardada si existe
                if (PlayerPrefs.HasKey("SelectedClass"))
                {
                    classDropdown.value = PlayerPrefs.GetInt("SelectedClass");
                }
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

            UpdateStatus("Selecciona tu clase e ingresa tu nombre");
        }

        private void OnHostButtonClicked()
        {
            string playerName = nameInputField.text.Trim();

            if (string.IsNullOrEmpty(playerName))
            {
                UpdateStatus("Por favor ingresa un nombre válido");
                return;
            }

            // Guardar nombre y clase
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.SetInt("SelectedClass", classDropdown.value);
            PlayerPrefs.Save();

            // Asignar al NetworkManager
            networkManager.playerName = playerName;
            networkManager.selectedClassIndex = classDropdown.value;

            string className = networkManager.availableClasses[classDropdown.value]?.className ?? "Unknown";

            // Iniciar como Host (Server + Client)
            UpdateStatus($"Iniciando servidor como {playerName} ({className})...");
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

            // Guardar nombre y clase
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.SetInt("SelectedClass", classDropdown.value);
            PlayerPrefs.Save();

            // Asignar al NetworkManager
            networkManager.playerName = playerName;
            networkManager.selectedClassIndex = classDropdown.value;

            string className = networkManager.availableClasses[classDropdown.value]?.className ?? "Unknown";

            // Conectar como cliente
            UpdateStatus($"Conectando como {playerName} ({className})...");
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
