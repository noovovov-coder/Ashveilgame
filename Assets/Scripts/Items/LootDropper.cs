using Ashveil.Combat;
using UnityEngine;

namespace Ashveil.Items
{
    /// <summary>
    /// Выпадение лута при смерти владельца (враг, босс, сундук).
    /// GDD §6: легендарки только с боссов — у рядовых врагов в таблице их просто нет.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class LootDropper : MonoBehaviour
    {
        [System.Serializable]
        public struct LootEntry
        {
            public ItemDefinition item;
            [Range(0f, 1f)] public float chance;
            public bool guaranteed; // боссовый дроп — всегда
        }

        [SerializeField] private LootEntry[] lootTable;
        [SerializeField] private ItemPickup pickupPrefab;
        [SerializeField] private float scatterRadius = 1.5f;

        /// <summary>Настройка из кода (GameBootstrap) вместо инспектора.</summary>
        public void Configure(ItemPickup pickup, params LootEntry[] table)
        {
            pickupPrefab = pickup;
            lootTable = table;
        }

        private void Awake()
        {
            GetComponent<Health>().Died += DropLoot;
        }

        private void DropLoot()
        {
            if (pickupPrefab == null)
                return;

            foreach (LootEntry entry in lootTable)
            {
                if (entry.item == null)
                    continue;

                if (!entry.guaranteed && Random.value > entry.chance)
                    continue;

                Vector2 scatter = Random.insideUnitCircle * scatterRadius;
                Vector3 position = transform.position + new Vector3(scatter.x, 0.5f, scatter.y);

                ItemPickup pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
                pickup.gameObject.SetActive(true); // шаблон может быть неактивным
                pickup.Item = entry.item;
            }
        }
    }
}
