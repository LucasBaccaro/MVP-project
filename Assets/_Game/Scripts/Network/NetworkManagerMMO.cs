using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core;
using Game.Player;

namespace Game.Network
{
    /// <summary>
    /// Mensaje para enviar datos del personaje al servidor (nombre y clase)
    /// </summary>
    public struct CharacterMessage : NetworkMessage
    {
        public string characterName;
        public int classIndex;
    }

    /// <summary>
    /// Datos temporales del jugador antes del spawn
    /// </summary>
    public class PlayerData
    {
        public string name;
        public int classIndex;
    }

    /// <summary>
    /// NetworkManager personalizado para el MMO
    /// Maneja el login, creación de personajes y persistencia de sesión
    /// </summary>
    public class NetworkManagerMMO : NetworkManager
    {
        [Header("MMO Settings")]
        [Tooltip("Nombre del jugador (se guardará localmente)")]
        public string playerName = "";

        [Tooltip("Índice de la clase seleccionada (0-3)")]
        public int selectedClassIndex = 0;

        [Header("Class Data")]
        [Tooltip("Array de clases disponibles (Guerrero, Mago, Cazador, Sacerdote)")]
        public ClassData[] availableClasses = new ClassData[4];

        // Dictionary para almacenar datos de cada conexión en el servidor
        private System.Collections.Generic.Dictionary<int, PlayerData> pendingPlayers =
            new System.Collections.Generic.Dictionary<int, PlayerData>();

        public override void OnStartServer()
        {
            base.OnStartServer();

            // Registrar el handler de mensajes de personaje
            NetworkServer.RegisterHandler<CharacterMessage>(OnCharacterMessageReceived);

            Debug.Log("[Server] Servidor MMO iniciado");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // Spawn del jugador en la escena del mundo
            GameObject player = Instantiate(playerPrefab);

            // Obtener datos del jugador según el tipo de conexión
            string finalPlayerName;
            int finalClassIndex;

            // Si es un LocalPlayer (Host), usar variables locales
            if (conn is LocalConnectionToClient)
            {
                finalPlayerName = playerName;
                finalClassIndex = selectedClassIndex;
                Debug.Log($"[Server] Host detectado, usando datos locales: {finalPlayerName}, clase {finalClassIndex}");
            }
            // Si es un cliente remoto, usar datos del Dictionary
            else if (pendingPlayers.TryGetValue(conn.connectionId, out PlayerData data))
            {
                finalPlayerName = data.name;
                finalClassIndex = data.classIndex;
                Debug.Log($"[Server] Cliente remoto detectado, usando datos recibidos: {finalPlayerName}, clase {finalClassIndex}");

                // Limpiar datos pendientes
                pendingPlayers.Remove(conn.connectionId);
            }
            else
            {
                // Fallback: usar valores por defecto
                finalPlayerName = $"Player_{conn.connectionId}";
                finalClassIndex = 0;
                Debug.LogWarning($"[Server] No se encontraron datos para conexión {conn.connectionId}, usando valores por defecto");
            }

            // Asignar nombre
            player.name = $"Player_{finalPlayerName}";

            // Aplicar la clase seleccionada
            if (finalClassIndex >= 0 && finalClassIndex < availableClasses.Length)
            {
                ClassData selectedClass = availableClasses[finalClassIndex];
                if (selectedClass != null)
                {
                    PlayerStats stats = player.GetComponent<PlayerStats>();
                    if (stats != null)
                    {
                        stats.InitializeStats(selectedClass);
                        Debug.Log($"[Server] Clase aplicada: {selectedClass.className} para {finalPlayerName}");
                    }
                    else
                    {
                        Debug.LogError("[Server] PlayerStats component no encontrado en el prefab!");
                    }
                }
                else
                {
                    Debug.LogError($"[Server] ClassData en índice {finalClassIndex} es null!");
                }
            }

            NetworkServer.AddPlayerForConnection(conn, player);
            Debug.Log($"[Server] Jugador añadido: {player.name}");
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            Debug.Log("[Client] Conectado al servidor");

            // Enviar mensaje al servidor con nombre y clase seleccionada
            CharacterMessage msg = new CharacterMessage
            {
                characterName = playerName,
                classIndex = selectedClassIndex
            };

            NetworkClient.Send(msg);
            Debug.Log($"[Client] Mensaje enviado al servidor - Nombre: {playerName}, Clase: {selectedClassIndex}");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            Debug.Log($"[Server] Jugador desconectado: {conn.connectionId}");

            // Limpiar datos pendientes si no spawneó
            if (pendingPlayers.ContainsKey(conn.connectionId))
            {
                pendingPlayers.Remove(conn.connectionId);
                Debug.Log($"[Server] Datos pendientes limpiados para {conn.connectionId}");
            }

            // Aquí se podría guardar los datos del jugador antes de desconectar
            // (FASE 8-9: Persistencia en RAM)

            base.OnServerDisconnect(conn);
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            Debug.Log("[Client] Desconectado del servidor");
        }

        /// <summary>
        /// Handler del mensaje CharacterMessage en el servidor
        /// Guarda los datos del jugador para usarlos cuando spawne
        /// </summary>
        private void OnCharacterMessageReceived(NetworkConnectionToClient conn, CharacterMessage msg)
        {
            Debug.Log($"[Server] Mensaje recibido de {conn.connectionId}: {msg.characterName}, clase {msg.classIndex}");

            // Guardar datos del jugador en el Dictionary
            pendingPlayers[conn.connectionId] = new PlayerData
            {
                name = msg.characterName,
                classIndex = msg.classIndex
            };

            Debug.Log($"[Server] Datos guardados para conexión {conn.connectionId}");
        }

        /// <summary>
        /// Método para iniciar el servidor (Host o Dedicated Server)
        /// </summary>
        public void StartMMOServer()
        {
            StartHost(); // O StartServer() para servidor dedicado
        }

        /// <summary>
        /// Método para conectar como cliente
        /// </summary>
        public void ConnectAsClient(string serverAddress = "localhost")
        {
            networkAddress = serverAddress;
            StartClient();
        }
    }
}
