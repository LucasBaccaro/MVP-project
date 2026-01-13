using UnityEngine;
using UnityEngine.UIElements; // IMPORTANTE
using Game.Network; // Tu namespace donde está NetworkManagerMMO
using System.Collections.Generic;

namespace Game.UI
{
    // Este atributo asegura que no te olvides de poner el UIDocument en el GameObject
    [RequireComponent(typeof(UIDocument))]
    public class LoginUI : MonoBehaviour
    {
        [Header("Referencias Lógicas")]
        [SerializeField] private NetworkManagerMMO networkManager;

        // YA NO USAMOS [SerializeField] para la UI. 
        // Las referencias se buscan en memoria al iniciar.
        private TextField _nameInput;
        private DropdownField _classDropdown;
        private Button _hostBtn;
        private Button _clientBtn;
        private Label _statusLabel;

        // Se llama cuando el objeto se activa en la escena
        private void OnEnable()
        {
            // 1. Conectar con el documento
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            if (root == null) {
                Debug.LogError("LoginUI: No hay UXML asignado en el UIDocument.");
                return;
            }

            // 2. Buscar los elementos por el "Name" que pusiste en el UI Builder
            _nameInput = root.Q<TextField>("input-name");
            _classDropdown = root.Q<DropdownField>("dropdown-class");
            _hostBtn = root.Q<Button>("btn-host");
            _clientBtn = root.Q<Button>("btn-client");
            _statusLabel = root.Q<Label>("lbl-status");

            // 3. Suscribirse a eventos (Click listeners)
            // Nota: En Toolkit usamos .clicked += Metodo
            if (_hostBtn != null) _hostBtn.clicked += OnHostClick;
            if (_clientBtn != null) _clientBtn.clicked += OnClientClick;

            // 4. Inicializar datos visuales
            InitializeUI();
        }

        private void InitializeUI()
        {
            if (networkManager == null) 
                networkManager = FindFirstObjectByType<NetworkManagerMMO>();

            // Llenar el Dropdown
            if (_classDropdown != null && networkManager != null)
            {
                _classDropdown.choices.Clear();
                var listaClases = new List<string>();
                
                foreach(var c in networkManager.availableClasses) 
                    if(c != null) listaClases.Add(c.className);
                
                _classDropdown.choices = listaClases;
                
                // Seleccionar valor guardado o el primero por defecto
                int savedIndex = PlayerPrefs.GetInt("SelectedClass", 0);
                if (listaClases.Count > 0) 
                    _classDropdown.index = Mathf.Clamp(savedIndex, 0, listaClases.Count - 1);
            }

            // Poner nombre guardado
            if (_nameInput != null && PlayerPrefs.HasKey("PlayerName"))
            {
                _nameInput.value = PlayerPrefs.GetString("PlayerName");
            }
        }

        // Lógica de Negocio al hacer click en Host
        private void OnHostClick()
        {
            string playerName = _nameInput.value; // En Toolkit es .value, no .text
            
            if (string.IsNullOrEmpty(playerName))
            {
                UpdateStatus("¡Necesitas un nombre!");
                return;
            }

            // Guardar datos
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.SetInt("SelectedClass", _classDropdown.index);
            PlayerPrefs.Save();

            // Configurar NetworkManager
            networkManager.playerName = playerName;
            networkManager.selectedClassIndex = _classDropdown.index;

            UpdateStatus("Iniciando Host...");
            networkManager.StartMMOServer();
        }

        // Lógica de Negocio al hacer click en Client
        private void OnClientClick()
        {
            string playerName = _nameInput.value;

            if (string.IsNullOrEmpty(playerName))
            {
                UpdateStatus("¡Necesitas un nombre!");
                return;
            }

            // Guardar datos
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.SetInt("SelectedClass", _classDropdown.index);
            PlayerPrefs.Save();

            networkManager.playerName = playerName;
            networkManager.selectedClassIndex = _classDropdown.index;

            UpdateStatus("Conectando al servidor...");
            networkManager.ConnectAsClient("localhost");
        }

        private void UpdateStatus(string msg)
        {
            if (_statusLabel != null) _statusLabel.text = msg;
            Debug.Log($"[UI]: {msg}");
        }

        // Limpieza de eventos para evitar errores de memoria
        private void OnDisable()
        {
            if (_hostBtn != null) _hostBtn.clicked -= OnHostClick;
            if (_clientBtn != null) _clientBtn.clicked -= OnClientClick;
        }
    }
}