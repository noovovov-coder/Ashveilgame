using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashveil.Items
{
    /// <summary>
    /// Инвентарь игрока (прототип).
    /// GDD §8: персонаж хранится локально у каждого игрока — инвентарь
    /// не реплицируется по сети, только подбор предмета подтверждается хостом.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private int capacity = 24;

        private readonly List<ItemDefinition> _items = new();
        private readonly Dictionary<EquipSlot, ItemDefinition> _equipped = new();

        public IReadOnlyList<ItemDefinition> Items => _items;
        public IReadOnlyDictionary<EquipSlot, ItemDefinition> Equipped => _equipped;

        public event Action Changed;

        public bool Add(ItemDefinition item)
        {
            if (item == null || _items.Count >= capacity)
                return false;

            _items.Add(item);
            Changed?.Invoke();
            return true;
        }

        public bool Remove(ItemDefinition item)
        {
            if (!_items.Remove(item))
                return false;

            Changed?.Invoke();
            return true;
        }

        /// <summary>Надеть предмет; прежний из того же слота возвращается в сумку.</summary>
        public bool Equip(ItemDefinition item)
        {
            if (item == null || item.slot == EquipSlot.None || !_items.Contains(item))
                return false;

            if (_equipped.TryGetValue(item.slot, out ItemDefinition previous) && previous != null)
                _items.Add(previous);

            _items.Remove(item);
            _equipped[item.slot] = item;
            Changed?.Invoke();
            return true;
        }

        public void Unequip(EquipSlot slot)
        {
            if (!_equipped.TryGetValue(slot, out ItemDefinition item) || item == null)
                return;

            _equipped.Remove(slot);
            _items.Add(item);
            Changed?.Invoke();
        }

        /// <summary>Суммарный стат со всего надетого (для PlayerCombat/Health).</summary>
        public float TotalStat(Func<ItemDefinition, float> selector)
        {
            float total = 0f;
            foreach (ItemDefinition item in _equipped.Values)
                if (item != null)
                    total += selector(item);
            return total;
        }
    }
}
