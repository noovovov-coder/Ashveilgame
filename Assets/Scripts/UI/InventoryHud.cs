using Ashveil.Items;
using Ashveil.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ashveil.UI
{
    /// <summary>
    /// Временный инвентарь на OnGUI (клавиша I).
    /// Цвет имени = редкость предмета (GDD §6).
    /// </summary>
    public class InventoryHud : MonoBehaviour
    {
        private Inventory _inventory;
        private bool _visible;

        private void Start()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                _inventory = player.GetComponent<Inventory>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible || _inventory == null)
                return;

            const float width = 280f;
            float height = 60f + (_inventory.Items.Count + _inventory.Equipped.Count) * 22f;
            Rect panel = new(Screen.width - width - 20f, 20f, width, Mathf.Max(height, 100f));

            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, panel.height - 16f));
            GUILayout.Label("<b>Снаряжение</b>", RichLabel());

            foreach (var pair in _inventory.Equipped)
                DrawItem(pair.Value, $"[{pair.Key}] ");

            GUILayout.Space(6f);
            GUILayout.Label($"<b>Сумка ({_inventory.Items.Count})</b>", RichLabel());

            foreach (ItemDefinition item in _inventory.Items)
                DrawItem(item, string.Empty);

            GUILayout.EndArea();
        }

        private static void DrawItem(ItemDefinition item, string prefix)
        {
            if (item == null)
                return;

            Color c = item.RarityColor;
            string hex = ColorUtility.ToHtmlStringRGB(c);
            GUILayout.Label($"{prefix}<color=#{hex}>{item.displayName}</color>", RichLabel());
        }

        private static GUIStyle RichLabel()
        {
            return new GUIStyle(GUI.skin.label) { richText = true };
        }
    }
}
