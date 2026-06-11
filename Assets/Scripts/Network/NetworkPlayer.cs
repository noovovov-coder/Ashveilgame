#if FISHNET
using Ashveil.Player;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace Ashveil.Network
{
    /// <summary>
    /// Сетевая обёртка над игроком.
    /// Владелец (Owner) читает ввод и двигает персонажа локально —
    /// предсказание на клиенте без задержки.
    /// Трансформ транслируется остальным через NetworkTransform (компонент на префабе).
    /// Анимационные параметры и здоровье передаются через SyncVar.
    /// </summary>
    public class NetworkPlayer : NetworkBehaviour
    {
        [SerializeField] private GameObject localCameraRig;
        [SerializeField] private GameObject playerNameplate;

        private PlayerController _controller;
        private PlayerInputReader _inputReader;
        private PlayerCombat _combat;

        [SyncVar(Channel = FishNet.Transporting.Channel.Unreliable)]
        private Vector3 _syncedVelocity;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _controller = GetComponent<PlayerController>();
            _inputReader = GetComponent<PlayerInputReader>();
            _combat = GetComponent<PlayerCombat>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (IsOwner)
            {
                // Только свой игрок получает камеру и слушает ввод.
                if (localCameraRig != null)
                    localCameraRig.SetActive(true);

                if (playerNameplate != null)
                    playerNameplate.SetActive(false);
            }
            else
            {
                // Чужой игрок: отключить все компоненты ввода.
                if (_inputReader != null)
                    _inputReader.enabled = false;

                if (_controller != null)
                    _controller.enabled = false;

                if (_combat != null)
                    _combat.enabled = false;

                if (localCameraRig != null)
                    localCameraRig.SetActive(false);
            }
        }

        private void Update()
        {
            // Хост публикует скорость для интерполяции у клиентов (анимация).
            if (IsServerInitialized && _controller != null)
                _syncedVelocity = _controller.Controller.velocity;
        }
    }
}
#endif
