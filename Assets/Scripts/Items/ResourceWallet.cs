using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashveil.Items
{
    public enum ResourceType
    {
        Wood,
        Stone,
        Iron,
        BossEssence // материал босса для апгрейдов (GDD §6)
    }

    /// <summary>Кошелёк ресурсов для строительства и крафта.</summary>
    public class ResourceWallet : MonoBehaviour
    {
        private readonly Dictionary<ResourceType, int> _amounts = new();

        public event Action Changed;

        public int Get(ResourceType type) =>
            _amounts.TryGetValue(type, out int amount) ? amount : 0;

        public void Add(ResourceType type, int amount)
        {
            if (amount <= 0)
                return;

            _amounts[type] = Get(type) + amount;
            Changed?.Invoke();
        }

        public bool CanAfford(params (ResourceType type, int amount)[] costs)
        {
            foreach ((ResourceType type, int amount) in costs)
                if (Get(type) < amount)
                    return false;
            return true;
        }

        public bool TrySpend(params (ResourceType type, int amount)[] costs)
        {
            if (!CanAfford(costs))
                return false;

            foreach ((ResourceType type, int amount) in costs)
            {
                _amounts[type] = Get(type) - amount;
            }

            Changed?.Invoke();
            return true;
        }
    }
}
