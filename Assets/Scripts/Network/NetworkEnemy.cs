using Ashveil.Enemies;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace Ashveil.Network
{
    /// <summary>
    /// Сетевая обёртка врага.
    /// GDD §14: «ИИ только у хоста» — EnemyAI работает исключительно на сервере.
    /// Позиция и анимационное состояние реплицируются через NetworkTransform (компонент префаба).
    /// Здоровье синхронизируется через NetworkHealth.
    /// </summary>
    public class NetworkEnemy : NetworkBehaviour
    {
        private EnemyAI _ai;

        [SyncVar(OnChange = nameof(OnDeadChanged))]
        private bool _dead;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _ai = GetComponent<EnemyAI>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsServerInitialized)
            {
                // Клиент не считает ИИ — только отображает.
                if (_ai != null)
                    _ai.enabled = false;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (TryGetComponent(out Ashveil.Combat.Health health))
                health.Died += OnServerDied;
        }

        private void OnServerDied()
        {
            _dead = true;
        }

        private void OnDeadChanged(bool prev, bool next, bool asServer)
        {
            if (next && !asServer)
            {
                // Клиент: визуально убираем врага (труп, лут — позже).
                gameObject.SetActive(false);
            }
        }
    }
}
