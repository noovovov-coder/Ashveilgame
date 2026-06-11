using Ashveil.Player;
using UnityEngine;

namespace Ashveil.Items
{
    /// <summary>
    /// Предмет, лежащий в мире. Подбирается касанием.
    /// Используется и для дропа с врагов, и для системы смерти из GDD §7
    /// (снаряжение падает на месте гибели игрока).
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobSpeed = 2f;

        private Vector3 _basePosition;

        public ItemDefinition Item
        {
            get => item;
            set => item = value;
        }

        private void Awake()
        {
            GetComponent<SphereCollider>().isTrigger = true;
            _basePosition = transform.position;
        }

        private void Update()
        {
            // Лёгкое парение, чтобы лут читался на земле.
            transform.position = _basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobAmplitude);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.transform.root.TryGetComponent(out PlayerController _))
                return;

            if (other.transform.root.TryGetComponent(out Inventory inventory) && inventory.Add(item))
                Destroy(gameObject);
        }
    }
}
