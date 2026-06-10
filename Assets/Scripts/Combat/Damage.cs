using UnityEngine;

namespace Ashveil.Combat
{
    /// <summary>Описание одного входящего удара.</summary>
    public struct DamageInfo
    {
        public float Amount;
        public Vector3 HitPoint;
        public GameObject Attacker;

        public DamageInfo(float amount, Vector3 hitPoint, GameObject attacker)
        {
            Amount = amount;
            HitPoint = hitPoint;
            Attacker = attacker;
        }
    }

    /// <summary>Всё, что может получать урон (игрок, враги, позже — постройки).</summary>
    public interface IDamageable
    {
        /// <returns>true, если урон прошёл (не был отменён парированием/неуязвимостью).</returns>
        bool TakeDamage(in DamageInfo info);
    }

    /// <summary>Всё, что можно ошеломить — например, успешным парированием.</summary>
    public interface IStaggerable
    {
        void Stagger(float duration);
    }
}
