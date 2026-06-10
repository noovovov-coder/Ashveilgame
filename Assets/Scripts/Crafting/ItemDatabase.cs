using System.Collections.Generic;
using Ashveil.Items;

namespace Ashveil.Crafting
{
    /// <summary>
    /// Реестр всех предметов по имени — нужен сохранениям
    /// (в JSON пишутся имена, при загрузке восстанавливаются ссылки).
    /// </summary>
    public static class ItemDatabase
    {
        private static readonly Dictionary<string, ItemDefinition> _items = new();

        public static void Register(ItemDefinition item)
        {
            if (item != null && !string.IsNullOrEmpty(item.displayName))
                _items[item.displayName] = item;
        }

        public static ItemDefinition Find(string displayName) =>
            _items.TryGetValue(displayName, out ItemDefinition item) ? item : null;
    }
}
