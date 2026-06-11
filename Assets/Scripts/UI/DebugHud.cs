using Ashveil.Combat;
using Ashveil.Items;
using Ashveil.Player;
using UnityEngine;

namespace Ashveil.UI
{
    /// <summary>
    /// Временный HUD на OnGUI для прототипа боя: HP и стамина игрока.
    /// Будет заменён на полноценный UI в этапе «Первый босс + лут».
    /// </summary>
    public class DebugHud : MonoBehaviour
    {
        private Health _playerHealth;
        private PlayerStamina _playerStamina;
        private ResourceWallet _wallet;

        private void Start()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                _playerHealth = player.GetComponent<Health>();
                _playerStamina = player.GetComponent<PlayerStamina>();
                _wallet = player.GetComponent<ResourceWallet>();
            }
        }

        private void OnGUI()
        {
            if (_playerHealth == null)
                return;

            DrawBar(new Rect(20, 20, 240, 22), _playerHealth.Current / _playerHealth.Max,
                Color.red, $"HP {_playerHealth.Current:0}/{_playerHealth.Max:0}");

            if (_playerStamina != null)
            {
                DrawBar(new Rect(20, 48, 240, 16), _playerStamina.Current / _playerStamina.Max,
                    Color.green, string.Empty);
            }

            if (_wallet != null)
            {
                GUI.Label(new Rect(20, 70, 400, 22),
                    $"Дерево: {_wallet.Get(ResourceType.Wood)}   " +
                    $"Камень: {_wallet.Get(ResourceType.Stone)}   " +
                    $"Железо: {_wallet.Get(ResourceType.Iron)}");
            }
        }

        private static void DrawBar(Rect rect, float fill, Color color, string label)
        {
            GUI.color = Color.black;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            GUI.color = color;
            Rect fillRect = rect;
            fillRect.width *= Mathf.Clamp01(fill);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

            GUI.color = Color.white;
            if (!string.IsNullOrEmpty(label))
                GUI.Label(rect, label);
        }
    }
}
