using System;
using UnityEngine;

namespace Ashveil.Combat
{
    /// <summary>
    /// Общий компонент здоровья для игрока и врагов.
    /// Владелец может перехватывать входящий урон через DamageGate
    /// (i-фреймы уклонения, окно парирования, броня босса и т.п.).
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;

        public float Current { get; private set; }
        public float Max => maxHealth;
        public bool IsDead => Current <= 0f;

        /// <summary>Вернуть false, чтобы полностью отменить удар.</summary>
        public Func<DamageInfo, bool> DamageGate;

        public event Action<DamageInfo> Damaged;
        public event Action Died;

        private void Awake()
        {
            Current = maxHealth;
        }

        public bool TakeDamage(in DamageInfo info)
        {
            if (IsDead)
                return false;

            if (DamageGate != null && !DamageGate(info))
                return false;

            Current = Mathf.Max(0f, Current - info.Amount);
            Damaged?.Invoke(info);

            if (Current <= 0f)
                Died?.Invoke();

            return true;
        }
    }
}
