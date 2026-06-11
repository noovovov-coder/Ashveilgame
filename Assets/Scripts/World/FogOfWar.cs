using System.Collections.Generic;
using Ashveil.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ashveil.World
{
    /// <summary>
    /// Туман войны (GDD §1, столп «Исследование»):
    /// мир разбит на клетки, исследованные открываются на карте (клавиша M).
    /// </summary>
    public class FogOfWar : MonoBehaviour
    {
        [SerializeField] private float cellSize = 8f;
        [SerializeField] private float revealRadius = 24f;
        [SerializeField] private float worldSize = 200f;

        private readonly HashSet<Vector2Int> _explored = new();
        private Transform _player;
        private bool _mapVisible;

        public void Configure(float size) => worldSize = size;

        private void Start()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                _player = player.transform;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
                _mapVisible = !_mapVisible;

            if (_player == null)
                return;

            // Открываем клетки вокруг игрока.
            int cells = Mathf.CeilToInt(revealRadius / cellSize);
            Vector2Int center = WorldToCell(_player.position);
            for (int dz = -cells; dz <= cells; dz++)
            {
                for (int dx = -cells; dx <= cells; dx++)
                {
                    var cell = new Vector2Int(center.x + dx, center.y + dz);
                    if (Vector2Int.Distance(center, cell) * cellSize <= revealRadius)
                        _explored.Add(cell);
                }
            }
        }

        private Vector2Int WorldToCell(Vector3 world) =>
            new(Mathf.FloorToInt(world.x / cellSize), Mathf.FloorToInt(world.z / cellSize));

        private void OnGUI()
        {
            if (!_mapVisible || _player == null)
                return;

            float mapSize = Mathf.Min(Screen.height - 120f, 420f);
            Rect panel = new((Screen.width - mapSize) / 2f, (Screen.height - mapSize) / 2f, mapSize, mapSize);

            GUI.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);

            float scale = mapSize / worldSize;
            float cellPixels = Mathf.Max(2f, cellSize * scale);

            GUI.color = new Color(0.45f, 0.42f, 0.38f);
            foreach (Vector2Int cell in _explored)
            {
                float px = panel.x + (cell.x * cellSize + worldSize / 2f) * scale;
                float py = panel.y + panel.height - ((cell.y * cellSize + worldSize / 2f) * scale) - cellPixels;
                if (px < panel.x || py < panel.y || px > panel.xMax - 2 || py > panel.yMax - 2)
                    continue;
                GUI.DrawTexture(new Rect(px, py, cellPixels, cellPixels), Texture2D.whiteTexture);
            }

            // Игрок.
            GUI.color = Color.cyan;
            float playerX = panel.x + (_player.position.x + worldSize / 2f) * scale;
            float playerY = panel.y + panel.height - ((_player.position.z + worldSize / 2f) * scale);
            GUI.DrawTexture(new Rect(playerX - 3f, playerY - 3f, 6f, 6f), Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 8f, panel.y + 4f, 300f, 24f), "Карта (M — закрыть)");
        }
    }
}
