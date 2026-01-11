using Mirror;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Maneja las zonas seguras e inseguras del mundo
    /// Detecta cuando el jugador entra/sale de zonas y sincroniza el estado
    /// </summary>
    public class ZoneHandler : NetworkBehaviour
    {
        [Header("Zone Status")]
        [SyncVar(hook = nameof(OnSafeZoneChanged))]
        public bool isSafeZone = false;

        [Header("Debug")]
        [Tooltip("Mostrar logs de debug en consola")]
        public bool showDebugLogs = true;

        private void OnTriggerEnter(Collider other)
        {
            if (!isLocalPlayer) return;

            // Detectar zona segura
            if (other.CompareTag("SafeZone"))
            {
                if (showDebugLogs)
                    Debug.Log($"[ZoneHandler] Entrando a ZONA SEGURA");

                CmdSetSafeZone(true);
            }
            // Detectar zona insegura
            else if (other.CompareTag("UnsafeZone"))
            {
                if (showDebugLogs)
                    Debug.Log($"[ZoneHandler] Entrando a ZONA INSEGURA");

                CmdSetSafeZone(false);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isLocalPlayer) return;

            // Saliendo de zona segura
            if (other.CompareTag("SafeZone"))
            {
                if (showDebugLogs)
                    Debug.Log($"[ZoneHandler] Saliendo de ZONA SEGURA");

                CmdSetSafeZone(false);
            }
            // Saliendo de zona insegura
            else if (other.CompareTag("UnsafeZone"))
            {
                if (showDebugLogs)
                    Debug.Log($"[ZoneHandler] Saliendo de ZONA INSEGURA");

                CmdSetSafeZone(true);
            }
        }

        [Command]
        private void CmdSetSafeZone(bool value)
        {
            // El servidor establece el valor
            isSafeZone = value;

            if (showDebugLogs)
                Debug.Log($"[ZoneHandler][Server] Zona cambiada a: {(value ? "SEGURA" : "INSEGURA")}");
        }

        // Hook que se llama cuando isSafeZone cambia (cliente y servidor)
        private void OnSafeZoneChanged(bool oldValue, bool newValue)
        {
            if (!isLocalPlayer) return;

            if (showDebugLogs)
                Debug.Log($"[ZoneHandler][Client] Estado de zona sincronizado: {(newValue ? "SEGURA" : "INSEGURA")}");

            // Notificar al UI que la zona cambió
            ZoneUIManager uiManager = FindFirstObjectByType<ZoneUIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateZoneStatus(newValue);
            }
        }

        // Método público para consultar si está en zona segura
        public bool IsInSafeZone()
        {
            return isSafeZone;
        }
    }
}
