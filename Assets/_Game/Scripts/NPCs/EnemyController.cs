using Mirror;
using UnityEngine;
using UnityEngine.AI;
using Game.Player;

namespace Game.NPCs
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NpcStats))]
    public class EnemyController : NetworkBehaviour
    {
        [Header("AI Settings")]
        public NpcData data;
        
        [Header("State")]
        private NavMeshAgent agent;
        private NpcStats stats;
        private Transform target;
        private Vector3 startPosition; // Punto de inicio para Leash
        private bool isReturning;      // Estado de retorno a spawn
        
        private float nextAttackTime;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            stats = GetComponent<NpcStats>();
            
            // Failsafe: Forzar ignore collision entre capas Player y Enemy por código
            int playerLayer = LayerMask.NameToLayer("Player");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            
            if (playerLayer >= 0 && enemyLayer >= 0)
            {
                Physics.IgnoreLayerCollision(playerLayer, enemyLayer, true);
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            // Desactivar NavMeshAgent en clientes puros para que no interfiera con NetworkTransform
            // y evitar que intente "empujar" o evitar obstáculos localmente.
            if (!isServer && agent != null)
            {
                agent.enabled = false;
            }
        }

        public override void OnStartServer()
        {
            if (data != null && agent != null)
            {
                agent.speed = data.moveSpeed;
                // Stop exactly at attack range to avoid pushing
                agent.stoppingDistance = data.attackRange; 
                // Asignar NetworkTransform sync interval si es necesario?
                // Mirror sincroniza Transform, NavMeshAgent mueve el Transform en Server.
            }
            
            // Guardar posición inicial para el sistema de Leash
            startPosition = transform.position;
        }

        [ServerCallback]
        void Update()
        {
            if (data == null || stats.currentHealth <= 0) return;

            // 0. Lógica de Retorno (Leash)
            if (isReturning)
            {
                if (!agent.pathPending && agent.remainingDistance < 1f)
                {
                    isReturning = false;
                    stats.currentHealth = stats.MaxHealth; // Curar al volver a casa
                    Debug.Log($"[EnemyController] {data.npcName} volvió a su puesto y se curó.");
                }
                return; // Ignorar todo lo demás mientras vuelve
            }

            // 1. Buscar objetivo si no hay
            if (target == null)
            {
                FindTarget();
            }

            // 2. Si hay objetivo, decidir qué hacer
            if (target != null)
            {
                // A. Validaciones de Leash y SafeZone
                if (Vector3.Distance(transform.position, startPosition) > data.maxChaseDistance)
                {
                    ReturnToSpawn();
                    return;
                }

                ZoneHandler targetZone = target.GetComponent<ZoneHandler>();
                if (targetZone != null && targetZone.IsInSafeZone())
                {
                    ReturnToSpawn();
                    return;
                }

                // B. Lógica de Persecución/Ataque
                float distance = Vector3.Distance(transform.position, target.position);

                // Perder aggro si se aleja mucho (ej: aggroRange * 1.5)
                if (distance > data.aggroRange * 1.5f)
                {
                    target = null;
                    agent.ResetPath();
                    return;
                }

                // Perseguir
                if (distance > data.attackRange)
                {
                    agent.stoppingDistance = data.attackRange; // Restaurar por si venía de Return
                    agent.isStopped = false;
                    agent.SetDestination(target.position);
                }
                // Atacar
                else
                {
                    // Detener completamente
                    agent.isStopped = true;

                    // Asegurar mirar al target
                    Vector3 direction = (target.position - transform.position).normalized;
                    direction.y = 0;
                    if (direction != Vector3.zero)
                        transform.rotation = Quaternion.LookRotation(direction);

                    if (Time.time >= nextAttackTime)
                    {
                        Attack(target);
                        nextAttackTime = Time.time + (1f / data.attackSpeed);
                    }
                }
            }
        }

        [Server]
        private void FindTarget()
        {
            // Buscar jugadores en rango
            Collider[] colliders = Physics.OverlapSphere(transform.position, data.aggroRange, LayerMask.GetMask("Player"));
            
            if (colliders.Length > 0)
            {
                // Tomar el primero (o el más cercano)
                target = colliders[0].transform;
            }
        }

        [Server]
        private void Attack(Transform targetTransform)
        {
            // Verificar stats
            PlayerStats playerStats = targetTransform.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                Debug.Log($"[EnemyController] {data.npcName} ataca a {targetTransform.name} por {data.damage} daño.");
                playerStats.TakeDamage(data.damage, stats);
            }
        }

        [Server]
        private void ReturnToSpawn()
        {
            if (isReturning) return;

            Debug.Log($"[EnemyController] {data.npcName} abandona la persecución y vuelve a spawn.");
            
            target = null;
            isReturning = true;
            agent.stoppingDistance = 0f; // Importante: volver EXACTAMENTE al punto
            agent.isStopped = false;
            agent.SetDestination(startPosition);
        }
    }
}
