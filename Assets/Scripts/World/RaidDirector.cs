using System.Collections;
using Ashveil.Enemies;
using UnityEngine;
using UnityEngine.AI;

namespace Ashveil.World
{
    /// <summary>
    /// «Пепловый налёт» (GDD §10, угрозы базе):
    /// волна заражённых атакует базу с предупреждением за 60 секунд.
    /// </summary>
    public class RaidDirector : MonoBehaviour
    {
        [SerializeField] private float raidInterval = 300f;   // раз в ~5 минут
        [SerializeField] private float warningTime = 60f;     // GDD: предупреждение 60 сек
        [SerializeField] private int baseWaveSize = 3;
        [SerializeField] private float spawnRingRadius = 30f;

        private EnemyAI _enemyTemplate;
        private Transform _hearth;
        private float _nextRaidAt;
        private string _banner = string.Empty;
        private int _raidsCompleted;

        public void Configure(EnemyAI enemyTemplate, Transform hearth)
        {
            _enemyTemplate = enemyTemplate;
            _hearth = hearth;
            _nextRaidAt = Time.time + raidInterval;
        }

        private void Update()
        {
            if (_hearth == null || _enemyTemplate == null)
                return;

            float untilRaid = _nextRaidAt - Time.time;

            if (untilRaid <= warningTime && untilRaid > 0f)
                _banner = $"ПЕПЛОВЫЙ НАЛЁТ ЧЕРЕЗ {Mathf.CeilToInt(untilRaid)} СЕК";
            else if (untilRaid <= 0f)
            {
                _nextRaidAt = Time.time + raidInterval;
                StartCoroutine(SpawnWave());
            }
        }

        private IEnumerator SpawnWave()
        {
            _banner = "ПЕПЛОВЫЙ НАЛЁТ!";
            int waveSize = baseWaveSize + _raidsCompleted; // налёты постепенно растут
            _raidsCompleted++;

            for (int i = 0; i < waveSize; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                Vector3 candidate = _hearth.position +
                    new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnRingRadius;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    EnemyAI enemy = Instantiate(_enemyTemplate, hit.position, Quaternion.identity);
                    enemy.gameObject.SetActive(true);
                }

                yield return new WaitForSeconds(0.5f);
            }

            yield return new WaitForSeconds(5f);
            _banner = string.Empty;
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_banner))
                return;

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            GUI.color = new Color(1f, 0.4f, 0.2f);
            GUI.Label(new Rect(0, 70, Screen.width, 30), _banner, style);
            GUI.color = Color.white;
        }
    }
}
