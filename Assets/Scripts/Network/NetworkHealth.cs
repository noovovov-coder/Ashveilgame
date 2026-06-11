#if FISHNET
using Ashveil.Combat;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace Ashveil.Network
{
    /// <summary>
    /// Синхронизирует HP по сети.
    /// Урон наносится только на сервере (ServerRpc → хост),
    /// значение реплицируется всем клиентам через SyncVar.
    /// GDD §14: «ИИ только у хоста» — тот же принцип: авторитетный сервер.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class NetworkHealth : NetworkBehaviour, IDamageRouter
    {
        private Health _health;

        [SyncVar(OnChange = nameof(OnHealthChanged))]
        private float _syncedHealth;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _health = GetComponent<Health>();
            _syncedHealth = _health.Current;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _health.Damaged += OnServerDamaged;
            _health.Died += OnServerDied;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            _health.Damaged -= OnServerDamaged;
            _health.Died -= OnServerDied;
        }

        private void OnServerDamaged(DamageInfo info)
        {
            _syncedHealth = _health.Current;
        }

        private void OnServerDied()
        {
            _syncedHealth = 0f;
        }

        // Вызывается на клиентах при получении нового значения SyncVar.
        private void OnHealthChanged(float prev, float next, bool asServer)
        {
            // На клиенте форсируем значение напрямую, минуя DamageGate
            // (авторитетно пришло с сервера).
            if (!IsServerInitialized && _health != null)
                _health.ForceSet(next);
        }

        /// <summary>Точка входа из боевой системы (IDamageRouter).</summary>
        public void RouteDamage(float amount, Vector3 hitPoint, GameObject attacker)
        {
            NetworkObject attackerNob = attacker != null
                ? attacker.GetComponent<NetworkObject>()
                : null;
            ServerRequestDamage(amount, hitPoint, attackerNob);
        }

        /// <summary>
        /// Клиент запрашивает нанесение урона. Хост проверяет и применяет.
        /// Так исключается читерство и рассинхрон HP.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ServerRequestDamage(float amount, Vector3 hitPoint, NetworkObject attackerNob)
        {
            GameObject attacker = attackerNob != null ? attackerNob.gameObject : null;
            _health.TakeDamage(new DamageInfo(amount, hitPoint, attacker));
        }
    }
}
#endif
