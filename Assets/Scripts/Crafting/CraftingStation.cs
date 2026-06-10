using System.Collections.Generic;
using Ashveil.Items;
using Ashveil.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ashveil.Crafting
{
    /// <summary>
    /// Крафт-станция (первая из десяти — Кузница, GDD §9).
    /// Подойти на 3 м и нажать E — меню рецептов.
    /// GDD: крафт усиливает лут, не заменяет — крафтовые вещи чуть слабее боссовых.
    /// </summary>
    public class CraftingStation : MonoBehaviour
    {
        public class Recipe
        {
            public ItemDefinition Result;
            public (ResourceType, int)[] Cost;
        }

        [SerializeField] private string stationName = "Кузница";
        [SerializeField] private float interactRange = 3f;

        private readonly List<Recipe> _recipes = new();
        private PlayerController _player;
        private bool _menuOpen;

        public void Configure(string name, List<Recipe> recipes)
        {
            stationName = name;
            _recipes.Clear();
            _recipes.AddRange(recipes);
        }

        private void Start()
        {
            _player = FindFirstObjectByType<PlayerController>();
        }

        private void Update()
        {
            if (_player == null || Keyboard.current == null)
                return;

            bool inRange = Vector3.Distance(_player.transform.position, transform.position) <= interactRange;

            if (!inRange)
            {
                _menuOpen = false;
                return;
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
                _menuOpen = !_menuOpen;
        }

        private void OnGUI()
        {
            if (_player == null)
                return;

            bool inRange = Vector3.Distance(_player.transform.position, transform.position) <= interactRange;
            if (!inRange)
                return;

            if (!_menuOpen)
            {
                var hint = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0, Screen.height / 2f + 40, Screen.width, 24),
                    $"[E] {stationName}", hint);
                return;
            }

            float width = 360f;
            float height = 60f + _recipes.Count * 34f;
            Rect panel = new((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(panel.x + 12, panel.y + 8, panel.width - 24, panel.height - 16));
            GUILayout.Label($"<b>{stationName}</b>", new GUIStyle(GUI.skin.label) { richText = true });

            ResourceWallet wallet = _player.GetComponent<ResourceWallet>();
            Inventory inventory = _player.GetComponent<Inventory>();

            foreach (Recipe recipe in _recipes)
            {
                string cost = string.Empty;
                foreach ((ResourceType type, int amount) in recipe.Cost)
                    cost += $" {type}:{amount}";

                bool affordable = wallet != null && wallet.CanAfford(recipe.Cost);
                GUI.enabled = affordable;

                if (GUILayout.Button($"{recipe.Result.displayName} —{cost}", GUILayout.Height(28)))
                {
                    if (wallet.TrySpend(recipe.Cost) && inventory != null)
                        inventory.Add(recipe.Result);
                }

                GUI.enabled = true;
            }

            GUILayout.EndArea();
        }
    }
}
