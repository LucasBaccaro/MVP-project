using Mirror;
using UnityEngine;
using System.Collections;
using Game.NPCs;

namespace Game.Systems
{
    /// <summary>
    /// Spawner simple que instancia un NPC y lo revive cuando muere tras un tiempo.
    /// </summary>
    public class NpcSpawner : NetworkBehaviour
    {
        [Header("Settings")]
        public GameObject npcPrefab;
        public float respawnTime = 10f;
        public float spawnRadius = 2f;
        public bool spawnOnStart = true;

        [Header("State")]
        private GameObject currentNpc;
        private bool isRespawning;

        public override void OnStartServer()
        {
            if (spawnOnStart)
            {
                SpawnNpc();
            }
        }

        [ServerCallback]
        private void Update()
        {
            // Chequear si el NPC murió (o fue destruido)
            if (currentNpc == null && !isRespawning)
            {
                StartCoroutine(RespawnRoutine());
            }
        }

        [Server]
        private void SpawnNpc()
        {
            if (npcPrefab == null)
            {
                Debug.LogWarning($"[NpcSpawner] No hay prefab asignado en {gameObject.name}");
                return;
            }

            // Calcular posición aleatoria en radio
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Ajustar altura al suelo
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            // Instanciar
            currentNpc = Instantiate(npcPrefab, spawnPos, Quaternion.LookRotation(transform.forward));
            
            // Asignar NetworkIdentity si es necesario (Spawn lo hace)
            NetworkServer.Spawn(currentNpc);
            
            Debug.Log($"[NpcSpawner] NPC Spawneado en {spawnPos}");
        }

        [Server]
        private IEnumerator RespawnRoutine()
        {
            isRespawning = true;
            Debug.Log($"[NpcSpawner] NPC muerto. Respawneando en {respawnTime}s...");
            
            yield return new WaitForSeconds(respawnTime);
            
            SpawnNpc();
            isRespawning = false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            Gizmos.DrawIcon(transform.position, "d_Linked", true);
        }
    }
}
