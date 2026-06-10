using System.Collections.Generic;
using Ashveil.Combat;
using Ashveil.Enemies;
using Ashveil.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Ashveil.Building
{
    /// <summary>
    /// Очаг Искры — главный объект базы (GDD §10).
    /// Пока горит: замедляет врагов в защитном радиусе и
    /// восстанавливает HP игроков рядом (упрощённая система уюта).
    /// </summary>
    public class Hearth : MonoBehaviour
    {
        [SerializeField] private float protectionRadius = 15f; // ур.1 по GDD: 15–60м
        [SerializeField] private float enemySlowFactor = 0.6f;
        [SerializeField] private float playerRegenPerSecond = 2f;

        private readonly Dictionary<NavMeshAgent, float> _slowedAgents = new();
        private float _nextTickAt;

        public float ProtectionRadius => protectionRadius;

        private void Update()
        {
            if (Time.time < _nextTickAt)
                return;
            _nextTickAt = Time.time + 0.5f;

            ApplyEnemySlow();
            RegenPlayers();
        }

        private void ApplyEnemySlow()
        {
            // Снимаем замедление с тех, кто вышел из радиуса.
            var toRestore = new List<NavMeshAgent>();
            foreach (var pair in _slowedAgents)
            {
                if (pair.Key == null)
                {
                    toRestore.Add(pair.Key);
                    continue;
                }

                if (Vector3.Distance(pair.Key.transform.position, transform.position) > protectionRadius)
                {
                    pair.Key.speed = pair.Value;
                    toRestore.Add(pair.Key);
                }
            }
            foreach (NavMeshAgent agent in toRestore)
                _slowedAgents.Remove(agent);

            // Замедляем вошедших.
            foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
            {
                if (Vector3.Distance(enemy.transform.position, transform.position) > protectionRadius)
                    continue;

                if (!enemy.TryGetComponent(out NavMeshAgent agent) || _slowedAgents.ContainsKey(agent))
                    continue;

                _slowedAgents[agent] = agent.speed;
                agent.speed *= enemySlowFactor;
            }
        }

        private void RegenPlayers()
        {
            foreach (PlayerController player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (Vector3.Distance(player.transform.position, transform.position) > protectionRadius)
                    continue;

                if (player.TryGetComponent(out Health health))
                    health.Heal(playerRegenPerSecond * 0.5f); // тик раз в полсекунды
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, protectionRadius);
        }
    }
}
