using Ashveil.Combat;
using Ashveil.Enemies.Bosses;
using UnityEngine;

namespace Ashveil.UI
{
    /// <summary>
    /// Полоса HP босса вверху экрана. Появляется при активации боя,
    /// показывает фазу (для прототипа — текстом).
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] private string bossTitle = "Гнилой Пастырь";

        private Health _bossHealth;
        private int _phase;
        private bool _active;

        private void Start()
        {
            RottenShepherd boss = FindFirstObjectByType<RottenShepherd>();
            if (boss == null)
                return;

            _bossHealth = boss.GetComponent<Health>();
            boss.PhaseChanged += phase =>
            {
                _phase = phase;
                _active = phase > 0;
            };
        }

        private void OnGUI()
        {
            if (!_active || _bossHealth == null)
                return;

            float width = Mathf.Min(600f, Screen.width - 80f);
            Rect bar = new((Screen.width - width) / 2f, 30f, width, 24f);

            GUI.color = Color.black;
            GUI.DrawTexture(bar, Texture2D.whiteTexture);

            GUI.color = new Color(0.6f, 0.15f, 0.5f);
            Rect fill = bar;
            fill.width *= Mathf.Clamp01(_bossHealth.Current / _bossHealth.Max);
            GUI.DrawTexture(fill, Texture2D.whiteTexture);

            GUI.color = Color.white;
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            string phaseName = _phase == 2 ? "Ритуал" : "Заражение";
            GUI.Label(bar, $"{bossTitle} — {phaseName}", style);
        }
    }
}
