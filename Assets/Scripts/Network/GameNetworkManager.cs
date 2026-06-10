using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.UI;

namespace Ashveil.Network
{
    /// <summary>
    /// Точка входа в сессию: хост или клиент.
    /// GDD §14: P2P через хоста, кооп до 4 игроков.
    /// На прототипном этапе использует Tugboat (UDP-транспорт FishNet,
    /// работает локально). Позже заменяется на FishySteamworks + Steam Relay.
    /// </summary>
    public class GameNetworkManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Text statusText;

        [Header("Prefabs")]
        [SerializeField] private GameObject playerPrefab;

        private NetworkManager _networkManager;

        private void Awake()
        {
            _networkManager = InstanceFinder.NetworkManager;
        }

        private void OnEnable()
        {
            if (hostButton != null) hostButton.onClick.AddListener(StartHost);
            if (joinButton != null) joinButton.onClick.AddListener(StartClient);

            if (_networkManager != null)
            {
                _networkManager.ClientManager.OnClientConnectionState += OnClientState;
                _networkManager.ServerManager.OnServerConnectionState += OnServerState;
            }
        }

        private void OnDisable()
        {
            if (hostButton != null) hostButton.onClick.RemoveListener(StartHost);
            if (joinButton != null) joinButton.onClick.RemoveListener(StartClient);

            if (_networkManager != null)
            {
                _networkManager.ClientManager.OnClientConnectionState -= OnClientState;
                _networkManager.ServerManager.OnServerConnectionState -= OnServerState;
            }
        }

        public void StartHost()
        {
            _networkManager.ServerManager.StartConnection();
            _networkManager.ClientManager.StartConnection();
        }

        public void StartClient()
        {
            _networkManager.ClientManager.StartConnection();
        }

        public void Disconnect()
        {
            if (_networkManager.IsServerStarted)
                _networkManager.ServerManager.StopConnection(true);
            else
                _networkManager.ClientManager.StopConnection();
        }

        private void OnServerState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
                SetStatus("Хост запущен. Ожидание подключений…");
        }

        private void OnClientState(ClientConnectionStateArgs args)
        {
            switch (args.ConnectionState)
            {
                case LocalConnectionState.Started:
                    SetStatus("Подключено к хосту.");
                    HideLobby();
                    break;
                case LocalConnectionState.Stopped:
                    SetStatus("Отключено.");
                    ShowLobby();
                    break;
            }
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
        }

        private void HideLobby()
        {
            if (lobbyPanel != null)
                lobbyPanel.SetActive(false);
        }

        private void ShowLobby()
        {
            if (lobbyPanel != null)
                lobbyPanel.SetActive(true);
        }
    }
}
