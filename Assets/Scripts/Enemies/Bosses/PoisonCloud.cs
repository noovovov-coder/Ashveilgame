using Ashveil.Combat;
using Ashveil.Player;
using UnityEngine;

namespace Ashveil.Enemies.Bosses
{
    /// <summary>
    /// Ядовитое облако Гнилого Пастыря (GDD §5, Акт I):
    /// живёт 6 секунд, медленно расширяется, наносит периодический урон игрокам внутри.
    /// Телеграф: первые секунды облако видно, но урон ещё не идёт.
    /// </summary>
    public class PoisonCloud : MonoBehaviour
    {
        [SerializeField] private float lifetime = 6f;
        [SerializeField] private float startRadius = 1.5f;
        [SerializeField] private float endRadius = 4f;
        [SerializeField] private float telegraphTime = 1f;
        [SerializeField] private float damagePerTick = 6f;
        [SerializeField] private float tickInterval = 0.75f;

        private float _spawnedAt;
        private float _nextTickAt;
        private GameObject _owner;

        public void Init(GameObject owner)
        {
            _owner = owner;
        }

        private void Awake()
        {
            _spawnedAt = Time.time;
            _nextTickAt = _spawnedAt + telegraphTime;
        }

        private void Update()
        {
            float age = Time.time - _spawnedAt;
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float radius = Mathf.Lerp(startRadius, endRadius, age / lifetime);
            transform.localScale = Vector3.one * (radius * 2f);

            if (Time.time < _nextTickAt)
                return;

            _nextTickAt = Time.time + tickInterval;
            DamagePlayersInside(radius);
        }

        private void DamagePlayersInside(float radius)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider hit in hits)
            {
                Transform root = hit.transform.root;
                if (!root.TryGetComponent(out PlayerController _))
                    continue;

                if (root.TryGetComponent(out IDamageable damageable))
                    damageable.TakeDamage(new DamageInfo(damagePerTick, hit.transform.position, _owner));
            }
        }
    }
}
