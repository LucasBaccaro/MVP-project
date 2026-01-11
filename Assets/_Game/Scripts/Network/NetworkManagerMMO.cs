using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Network
{
    /// <summary>
    /// Mensaje para enviar el nombre del personaje al servidor
    /// </summary>
    public struct CharacterMessage : NetworkMessage
    {
        public string characterName;
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

        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("[Server] Servidor MMO iniciado");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // Spawn del jugador en la escena del mundo
            GameObject player = Instantiate(playerPrefab);

            // Asignar nombre si existe
            if (!string.IsNullOrEmpty(playerName))
            {
                player.name = $"Player_{playerName}";
            }

            NetworkServer.AddPlayerForConnection(conn, player);
            Debug.Log($"[Server] Jugador añadido: {player.name}");
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            Debug.Log("[Client] Conectado al servidor");

            // Enviar mensaje al servidor con el nombre del personaje
            if (!string.IsNullOrEmpty(playerName))
            {
                CharacterMessage msg = new CharacterMessage { characterName = playerName };
                NetworkClient.Send(msg);
            }
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            Debug.Log($"[Server] Jugador desconectado");

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
