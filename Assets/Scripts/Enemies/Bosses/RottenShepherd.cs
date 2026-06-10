using System.Collections;
using System.Collections.Generic;
using Ashveil.Combat;
using Ashveil.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Ashveil.Enemies.Bosses
{
    /// <summary>
    /// Гнилой Пастырь — первый босс (GDD §5, Акт I). Прототип с двумя фазами
    /// по roadmap §15 («Пастырь с двумя фазами»):
    ///
    ///   Фаза 1 «Заражение» (100–50% HP): медленный, ближняя атака,
    ///     ядовитые споры под случайным игроком — учит уклонению.
    ///   Фаза 2 «Ритуал» (50–0% HP): призывает заражённых; если аддов
    ///     не убить за 20 секунд — они сливаются с боссом и лечат его.
    ///
    /// Скейлинг по числу игроков (GDD §5): HP +40% за второго игрока и далее.
    /// Третья фаза «Агония» (яд на 50% арены) — после этапа «Первый биом».
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class RottenShepherd : MonoBehaviour, IStaggerable
    {
        private enum Phase { Inactive, Infection, Ritual, Dead }

        [Header("Скейлинг (GDD §5)")]
        [SerializeField] private float baseHealth = 600f;
        [SerializeField] private float[] healthMultiplierByPlayers = { 1f, 1.4f, 1.8f, 2.3f };
        [SerializeField] private float[] damageMultiplierByPlayers = { 1f, 1.15f, 1.3f, 1.5f };

        [Header("Ближняя атака")]
        [SerializeField] private float attackRange = 2.5f;
        [SerializeField] private float baseAttackDamage = 25f;
        [SerializeField] private float windupTime = 0.9f;
        [SerializeField] private float recoverTime = 1.2f;
        [SerializeField] private float strikeRadius = 1.8f;

        [Header("Ядовитые споры (фаза 1+)")]
        [SerializeField] private PoisonCloud poisonCloudPrefab;
        [SerializeField] private float poisonCooldown = 7f;

        [Header("Ритуал (фаза 2)")]
        [SerializeField] private EnemyAI addPrefab;
        [SerializeField] private int addsPerRitual = 2;
        [SerializeField] private float ritualCooldown = 25f;
        [SerializeField] private float addMergeTimeout = 20f;
        [SerializeField] private float healPerMergedAdd = 0.08f; // доля от макс. HP

        [Header("Активация")]
        [SerializeField] private float aggroRadius = 15f;

        private Phase _phase = Phase.Inactive;
        private NavMeshAgent _agent;
        private Health _health;
        private float _damageMultiplier = 1f;
        private float _nextPoisonAt;
        private float _nextRitualAt;
        private bool _attacking;
        private readonly List<EnemyAI> _liveAdds = new();

        public event System.Action<int> PhaseChanged; // для UI/музыки (FMOD позже)

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _health.Died += OnDied;
            _health.Damaged += OnDamaged;
        }

        private void Update()
        {
            if (_phase == Phase.Dead)
                return;

            PlayerController target = FindClosestPlayer(out float distance);
            if (target == null)
                return;

            if (_phase == Phase.Inactive)
            {
                if (distance <= aggroRadius)
                    ActivateFight();
                return;
            }

            if (_attacking)
                return;

            // Споры — в обеих фазах.
            if (Time.time >= _nextPoisonAt)
            {
                _nextPoisonAt = Time.time + poisonCooldown;
                StartCoroutine(CastPoisonAtRandomPlayer());
                return;
            }

            // Ритуал — только во второй фазе.
            if (_phase == Phase.Ritual && Time.time >= _nextRitualAt)
            {
                _nextRitualAt = Time.time + ritualCooldown;
                StartCoroutine(RitualRoutine());
                return;
            }

            // Базовое поведение: догнать и ударить.
            _agent.SetDestination(target.transform.position);
            if (distance <= attackRange)
                StartCoroutine(MeleeRoutine(target.transform));
        }

        /// <summary>Скейлинг по числу игроков на момент начала боя (GDD §5).</summary>
        private void ActivateFight()
        {
            int playerCount = Mathf.Clamp(
                FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length, 1, 4);

            _damageMultiplier = damageMultiplierByPlayers[playerCount - 1];
            _health.SetMax(baseHealth * healthMultiplierByPlayers[playerCount - 1], refill: true);

            _phase = Phase.Infection;
            _nextPoisonAt = Time.time + 3f;
            PhaseChanged?.Invoke(1);
        }

        private void OnDamaged(DamageInfo info)
        {
            // Переход фаз по порогу HP.
            if (_phase == Phase.Infection && _health.Current / _health.Max <= 0.5f)
            {
                _phase = Phase.Ritual;
                _nextRitualAt = Time.time; // первый ритуал сразу
                PhaseChanged?.Invoke(2);
            }
        }

        private IEnumerator MeleeRoutine(Transform target)
        {
            _attacking = true;
            _agent.ResetPath();

            float windupEnd = Time.time + windupTime;
            while (Time.time < windupEnd)
            {
                FaceTowards(target.position);
                yield return null;
            }

            Vector3 center = transform.position + transform.forward * (attackRange * 0.7f) + Vector3.up;
            foreach (Collider hit in Physics.OverlapSphere(center, strikeRadius))
            {
                Transform root = hit.transform.root;
                if (root == transform.root || !root.TryGetComponent(out PlayerController _))
                    continue;

                if (root.TryGetComponent(out IDamageable damageable))
                    damageable.TakeDamage(new DamageInfo(
                        baseAttackDamage * _damageMultiplier, hit.ClosestPoint(center), gameObject));
            }

            yield return new WaitForSeconds(recoverTime);
            _attacking = false;
        }

        private IEnumerator CastPoisonAtRandomPlayer()
        {
            _attacking = true;
            _agent.ResetPath();

            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            if (players.Length > 0 && poisonCloudPrefab != null)
            {
                Vector3 targetPos = players[Random.Range(0, players.Length)].transform.position;

                // Телеграф каста — босс замирает.
                yield return new WaitForSeconds(1f);

                PoisonCloud cloud = Instantiate(poisonCloudPrefab, targetPos, Quaternion.identity);
                cloud.Init(gameObject);
            }

            _attacking = false;
        }

        private IEnumerator RitualRoutine()
        {
            _attacking = true;
            _agent.ResetPath();
            PhaseChanged?.Invoke(2); // повторный сигнал — для звука ритуала

            // Каст ритуала — окно для бурста по боссу.
            yield return new WaitForSeconds(2f);

            _liveAdds.Clear();
            for (int i = 0; i < addsPerRitual; i++)
            {
                if (addPrefab == null)
                    break;

                Vector2 offset = Random.insideUnitCircle.normalized * 4f;
                Vector3 position = transform.position + new Vector3(offset.x, 0f, offset.y);
                EnemyAI add = Instantiate(addPrefab, position, Quaternion.identity);
                _liveAdds.Add(add);
            }

            _attacking = false;

            // GDD: не убили аддов за 20 секунд — сливаются с боссом и лечат его.
            yield return new WaitForSeconds(addMergeTimeout);
            MergeSurvivingAdds();
        }

        private void MergeSurvivingAdds()
        {
            if (_phase == Phase.Dead)
                return;

            int merged = 0;
            foreach (EnemyAI add in _liveAdds)
            {
                if (add == null)
                    continue;

                Destroy(add.gameObject);
                merged++;
            }
            _liveAdds.Clear();

            if (merged > 0)
                _health.Heal(_health.Max * healPerMergedAdd * merged);
        }

        private PlayerController FindClosestPlayer(out float distance)
        {
            PlayerController closest = null;
            distance = float.MaxValue;

            foreach (PlayerController player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(transform.position, player.transform.position);
                if (d < distance)
                {
                    distance = d;
                    closest = player;
                }
            }

            return closest;
        }

        private void FaceTowards(Vector3 position)
        {
            Vector3 direction = position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                return;

            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(direction), 5f * Time.deltaTime);
        }

        /// <summary>Боссы не ошеломляются парированием — только прерывание урона.</summary>
        public void Stagger(float duration) { }

        private void OnDied()
        {
            _phase = Phase.Dead;
            StopAllCoroutines();
            _agent.enabled = false;
            PhaseChanged?.Invoke(0);

            foreach (Collider col in GetComponentsInChildren<Collider>())
                col.enabled = false;

            // Лут выпадает через LootDropper на этом же объекте.
            Destroy(gameObject, 5f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, aggroRadius);
        }
    }
}
