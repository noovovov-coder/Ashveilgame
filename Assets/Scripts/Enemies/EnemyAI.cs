using System.Collections;
using Ashveil.Combat;
using Ashveil.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Ashveil.Enemies
{
    /// <summary>
    /// Первый рядовой враг — «заражённый» (Unity NavMesh, как задано в GDD п.14).
    /// Конечный автомат: ожидание → преследование → замах (телеграф) → удар → восстановление.
    /// Замах оставляет игроку время на уклонение или парирование — «каждый враг — угроза»,
    /// но угроза честная и читаемая.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class EnemyAI : MonoBehaviour, IStaggerable
    {
        private enum State { Idle, Chase, Windup, Recover, Staggered, Dead }

        [Header("Восприятие")]
        [SerializeField] private float aggroRadius = 12f;
        [SerializeField] private float loseAggroRadius = 20f;

        [Header("Атака")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackDamage = 20f;
        [SerializeField] private float windupTime = 0.6f;
        [SerializeField] private float recoverTime = 0.9f;
        [SerializeField] private float strikeRadius = 1.2f;

        private State _state = State.Idle;
        private NavMeshAgent _agent;
        private Health _health;
        private Transform _target;
        private Coroutine _currentAction;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _health.Died += OnDied;
        }

        private void Start()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                _target = player.transform;
        }

        private void Update()
        {
            if (_target == null || _state == State.Dead)
                return;

            float distance = Vector3.Distance(transform.position, _target.position);

            switch (_state)
            {
                case State.Idle:
                    if (distance <= aggroRadius)
                        _state = State.Chase;
                    break;

                case State.Chase:
                    if (distance > loseAggroRadius)
                    {
                        _state = State.Idle;
                        _agent.ResetPath();
                        break;
                    }

                    _agent.SetDestination(_target.position);

                    if (distance <= attackRange)
                        _currentAction = StartCoroutine(AttackRoutine());
                    break;
            }
        }

        private IEnumerator AttackRoutine()
        {
            _state = State.Windup;
            _agent.ResetPath();

            // Телеграф: враг разворачивается к цели и замахивается.
            float windupEnd = Time.time + windupTime;
            while (Time.time < windupEnd)
            {
                FaceTarget();
                yield return null;
            }

            Strike();

            _state = State.Recover;
            yield return new WaitForSeconds(recoverTime);

            if (_state == State.Recover)
                _state = State.Chase;
            _currentAction = null;
        }

        private void Strike()
        {
            Vector3 center = transform.position + transform.forward * (attackRange * 0.75f) + Vector3.up;
            Collider[] hits = Physics.OverlapSphere(center, strikeRadius);

            foreach (Collider hit in hits)
            {
                if (hit.transform.root == transform.root)
                    continue;

                if (hit.transform.root.TryGetComponent(out PlayerController _) &&
                    hit.transform.root.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(new DamageInfo(attackDamage, hit.ClosestPoint(center), gameObject));
                }
            }
        }

        private void FaceTarget()
        {
            Vector3 direction = _target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                return;

            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(direction), 8f * Time.deltaTime);
        }

        /// <summary>Успешное парирование игрока прерывает атаку и ошеломляет.</summary>
        public void Stagger(float duration)
        {
            if (_state == State.Dead)
                return;

            if (_currentAction != null)
            {
                StopCoroutine(_currentAction);
                _currentAction = null;
            }

            StartCoroutine(StaggerRoutine(duration));
        }

        private IEnumerator StaggerRoutine(float duration)
        {
            _state = State.Staggered;
            _agent.ResetPath();

            yield return new WaitForSeconds(duration);

            if (_state == State.Staggered)
                _state = State.Chase;
        }

        private void OnDied()
        {
            _state = State.Dead;
            StopAllCoroutines();
            _agent.enabled = false;

            foreach (Collider col in GetComponentsInChildren<Collider>())
                col.enabled = false;

            // Прототип: труп исчезает; позже здесь появятся лут и подъём Вестником.
            Destroy(gameObject, 3f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, aggroRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
