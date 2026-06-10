using System.Collections.Generic;
using Ashveil.Items;
using Ashveil.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ashveil.Building
{
    /// <summary>
    /// Режим строительства (GDD §10):
    ///   B — вкл/выкл, 1–9 — выбор элемента, ЛКМ — поставить,
    ///   R — повернуть на 90°, X+ПКМ — сломать (возврат 50% материалов).
    /// Привязка к сетке 2 м. Призрак краснеет, если не хватает ресурсов.
    /// </summary>
    public class BuildPlacer : MonoBehaviour
    {
        public class PieceDef
        {
            public string Id;
            public string DisplayName;
            public GameObject Template;
            public (ResourceType, int)[] Cost;
            public float MaxHealth = 100f;
        }

        private const float GridStep = 2f;

        private readonly List<PieceDef> _pieces = new();
        private ResourceWallet _wallet;
        private PlayerCombat _combat;
        private Camera _camera;

        private bool _buildMode;
        private int _selected;
        private float _ghostYaw;
        private GameObject _ghost;

        public void Configure(List<PieceDef> pieces)
        {
            _pieces.Clear();
            _pieces.AddRange(pieces);
        }

        private void Awake()
        {
            _wallet = GetComponent<ResourceWallet>();
            _combat = GetComponent<PlayerCombat>();
        }

        private void Start()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (kb == null || mouse == null || _pieces.Count == 0)
                return;

            if (kb.bKey.wasPressedThisFrame)
                ToggleBuildMode();

            if (!_buildMode)
            {
                // Слом вне режима строительства: X + ПКМ по постройке.
                if (kb.xKey.isPressed && mouse.rightButton.wasPressedThisFrame)
                    TryDemolish();
                return;
            }

            for (int i = 0; i < Mathf.Min(_pieces.Count, 9); i++)
            {
                if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                    _selected = i;
            }

            if (kb.rKey.wasPressedThisFrame)
                _ghostYaw = (_ghostYaw + 90f) % 360f;

            UpdateGhost(out Vector3 position, out bool valid);

            if (valid && mouse.leftButton.wasPressedThisFrame)
                TryPlace(position);
        }

        private void ToggleBuildMode()
        {
            _buildMode = !_buildMode;

            // В режиме строительства бой выключен, чтобы ЛКМ не била.
            if (_combat != null)
                _combat.enabled = !_buildMode;

            if (!_buildMode && _ghost != null)
            {
                Destroy(_ghost);
                _ghost = null;
            }
        }

        private void UpdateGhost(out Vector3 position, out bool valid)
        {
            position = Vector3.zero;
            valid = false;

            PieceDef piece = _pieces[_selected];

            if (_ghost == null || _ghost.name != $"Ghost_{piece.Id}")
            {
                if (_ghost != null)
                    Destroy(_ghost);

                _ghost = Instantiate(piece.Template);
                _ghost.name = $"Ghost_{piece.Id}";
                _ghost.SetActive(true);
                foreach (Collider col in _ghost.GetComponentsInChildren<Collider>())
                    col.enabled = false;
            }

            Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
            if (!Physics.Raycast(ray, out RaycastHit hit, 12f))
            {
                _ghost.SetActive(false);
                return;
            }

            position = new Vector3(
                Mathf.Round(hit.point.x / GridStep) * GridStep,
                hit.point.y,
                Mathf.Round(hit.point.z / GridStep) * GridStep);

            _ghost.SetActive(true);
            _ghost.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, _ghostYaw, 0f));

            valid = _wallet == null || _wallet.CanAfford(piece.Cost);
            TintGhost(valid ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f));
        }

        private void TintGhost(Color color)
        {
            foreach (Renderer renderer in _ghost.GetComponentsInChildren<Renderer>())
                renderer.material.color = color;
        }

        private void TryPlace(Vector3 position)
        {
            PieceDef piece = _pieces[_selected];

            if (_wallet != null && !_wallet.TrySpend(piece.Cost))
                return;

            GameObject built = Instantiate(piece.Template, position, Quaternion.Euler(0f, _ghostYaw, 0f));
            built.name = piece.Id;
            built.SetActive(true);

            var health = built.AddComponent<Combat.Health>();
            health.SetMax(piece.MaxHealth, refill: true);
            built.AddComponent<BuiltPiece>().Init(piece.Id);
        }

        /// <summary>Восстановить постройку из сейва без списания ресурсов.</summary>
        public void PlaceFromSave(string pieceId, Vector3 position, float yaw)
        {
            PieceDef piece = _pieces.Find(p => p.Id == pieceId);
            if (piece == null)
                return;

            GameObject built = Instantiate(piece.Template, position, Quaternion.Euler(0f, yaw, 0f));
            built.name = piece.Id;
            built.SetActive(true);

            var health = built.AddComponent<Combat.Health>();
            health.SetMax(piece.MaxHealth, refill: true);
            built.AddComponent<BuiltPiece>().Init(pieceId);
        }

        private void TryDemolish()
        {
            Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
            if (!Physics.Raycast(ray, out RaycastHit hit, 8f))
                return;

            BuiltPiece piece = hit.transform.GetComponentInParent<BuiltPiece>();
            if (piece == null)
                return;

            // Возврат 50% материалов (GDD: 50–100%).
            PieceDef def = _pieces.Find(p => p.Id == piece.PieceId);
            if (def != null && _wallet != null)
            {
                foreach ((ResourceType type, int amount) in def.Cost)
                    _wallet.Add(type, Mathf.Max(1, amount / 2));
            }

            Destroy(piece.gameObject);
        }

        private void OnGUI()
        {
            if (!_buildMode)
                return;

            GUI.color = Color.white;
            var lines = new List<string> { "<b>СТРОИТЕЛЬСТВО</b> (B — выйти, R — поворот, ЛКМ — поставить)" };
            for (int i = 0; i < _pieces.Count; i++)
            {
                string marker = i == _selected ? "►" : "  ";
                string cost = string.Empty;
                foreach ((ResourceType type, int amount) in _pieces[i].Cost)
                    cost += $" {type}:{amount}";
                lines.Add($"{marker} [{i + 1}] {_pieces[i].DisplayName} —{cost}");
            }

            var style = new GUIStyle(GUI.skin.label) { richText = true };
            GUI.Label(new Rect(20, Screen.height - 30 - lines.Count * 20, 500, lines.Count * 20 + 10),
                string.Join("\n", lines), style);

            // Прицел.
            GUI.DrawTexture(new Rect(Screen.width / 2f - 2, Screen.height / 2f - 2, 4, 4), Texture2D.whiteTexture);
        }
    }
}
