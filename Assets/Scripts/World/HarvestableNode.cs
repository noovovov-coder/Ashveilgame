using Ashveil.Combat;
using Ashveil.Items;
using UnityEngine;

namespace Ashveil.World
{
    /// <summary>
    /// Добываемый объект мира: дерево, камень, жила.
    /// Бьётся обычной атакой — каждый удар даёт ресурс атакующему.
    /// </summary>
    public class HarvestableNode : MonoBehaviour, IDamageable
    {
        [SerializeField] private ResourceType resource = ResourceType.Wood;
        [SerializeField] private int yieldPerHit = 2;
        [SerializeField] private float durability = 50f;

        public void Configure(ResourceType type, int perHit, float maxDurability)
        {
            resource = type;
            yieldPerHit = perHit;
            durability = maxDurability;
        }

        public bool TakeDamage(in DamageInfo info)
        {
            if (info.Attacker != null &&
                info.Attacker.TryGetComponent(out ResourceWallet wallet))
            {
                wallet.Add(resource, yieldPerHit);
            }

            durability -= info.Amount;
            if (durability <= 0f)
                Destroy(gameObject);

            return true;
        }
    }
}
