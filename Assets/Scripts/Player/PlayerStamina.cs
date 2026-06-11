using UnityEngine;

namespace Ashveil.Player
{
    /// <summary>
    /// Стамина — основа «весомого» боя из GDD: атаки, уклонения,
    /// парирование и спринт стоят ресурс, бездумный спам наказывается.
    /// </summary>
    public class PlayerStamina : MonoBehaviour
    {
        [SerializeField] private float max = 100f;
        [SerializeField] private float regenPerSecond = 25f;
        [SerializeField] private float regenDelay = 0.8f;

        public float Current { get; private set; }
        public float Max => max;

        private float _lastSpendTime = float.NegativeInfinity;

        private void Awake()
        {
            Current = max;
        }

        public bool TrySpend(float cost)
        {
            if (Current < cost)
                return false;

            Current -= cost;
            _lastSpendTime = Time.time;
            return true;
        }

        /// <summary>Постепенный расход (спринт). Вернёт false, когда стамина кончилась.</summary>
        public bool Drain(float perSecond)
        {
            if (Current <= 0f)
                return false;

            Current = Mathf.Max(0f, Current - perSecond * Time.deltaTime);
            _lastSpendTime = Time.time;
            return Current > 0f;
        }

        private void Update()
        {
            if (Time.time - _lastSpendTime >= regenDelay)
                Current = Mathf.Min(max, Current + regenPerSecond * Time.deltaTime);
        }
    }
}
