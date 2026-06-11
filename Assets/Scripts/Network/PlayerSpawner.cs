#if FISHNET
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

namespace Ashveil.Network
{
    /// <summary>
    /// Спаун игрока при подключении.
    /// GDD §14: «хост владеет миром, клиенты приносят своих персонажей».
    /// Добавить компонент на тот же GameObject, что и NetworkManager.
    /// В поле PlayerPrefab назначить сетевой префаб игрока
    /// (должен быть зарегистрирован в NetworkManager → Spawnable Prefabs).
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;

        private NetworkManager _networkManager;
        private int _spawnIndex;

        private void Awake()
        {
            _networkManager = InstanceFinder.NetworkManager;
        }

        private void OnEnable()
        {
            _networkManager.SceneManager.OnClientLoadedStartScenes += OnClientReady;
        }

        private void OnDisable()
        {
            _networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientReady;
        }

        private void OnClientReady(NetworkConnection conn, bool asServer)
        {
            if (!asServer)
                return;

            Vector3 position = GetSpawnPoint();
            NetworkObject nob = Instantiate(playerPrefab, position, Quaternion.identity);
            _networkManager.ServerManager.Spawn(nob, conn);
        }

        private Vector3 GetSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return new Vector3(_spawnIndex * 2f, 0f, 0f);

            Transform point = spawnPoints[_spawnIndex % spawnPoints.Length];
            _spawnIndex++;
            return point.position;
        }
    }
}
#endif
