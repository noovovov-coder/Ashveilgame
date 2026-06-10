using UnityEngine;

namespace Ashveil.Items
{
    /// <summary>Тиры редкости из GDD §6.</summary>
    public enum ItemRarity
    {
        Common,     // Рядовые враги, крафт
        Rare,       // Элиты, мини-данжи
        Epic,       // Боссы, редкие сундуки (сетовые бонусы)
        Legendary   // Только с боссов (уникальная механика)
    }

    /// <summary>Слоты снаряжения из GDD §6.</summary>
    public enum EquipSlot
    {
        None,       // Расходники, материалы
        Head,
        Chest,
        Hands,
        Legs,
        Weapon,
        Amulet,
        Ring,
        Cloak
    }

    /// <summary>
    /// Описание предмета (ScriptableObject-ассет).
    /// GDD §6: предметы универсальны — любой класс надевает любой предмет,
    /// роль определяется статами.
    /// </summary>
    [CreateAssetMenu(menuName = "Ashveil/Item", fileName = "NewItem")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Описание")]
        public string displayName;
        [TextArea] public string description;
        public ItemRarity rarity = ItemRarity.Common;
        public EquipSlot slot = EquipSlot.None;

        [Header("Статы")]
        public float physicalPower;   // физ.сила → танк/ближний бой
        public float spellPower;      // сила заклинаний → маг
        public float armor;
        public float maxHealthBonus;

        [Header("Уникальная механика (для легендарок)")]
        [TextArea] public string uniqueEffect;

        public Color RarityColor => rarity switch
        {
            ItemRarity.Rare => new Color(0.3f, 0.55f, 1f),
            ItemRarity.Epic => new Color(0.65f, 0.3f, 0.9f),
            ItemRarity.Legendary => new Color(1f, 0.55f, 0.1f),
            _ => Color.white
        };
    }
}
