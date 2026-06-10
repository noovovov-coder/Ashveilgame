using System.Collections;
using Ashveil.Combat;
using Ashveil.Network;
using FishNet.Object;
using UnityEngine;

namespace Ashveil.Player
{
    /// <summary>
    /// Боевая система игрока по столпу «Бой» из GDD:
    /// трёхударное комбо с буферизацией ввода, уклонение с i-фреймами,
    /// парирование с коротким окном (успех ошеломляет атакующего).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerStamina))]
    [RequireComponent(typeof(Health))]
    public class PlayerCombat : MonoBehaviour
    {
        private enum State { Ready, Attacking, Dodging, Parrying }

        [Header("Комбо")]
        [SerializeField] private float[] comboDamage = { 15f, 15f, 28f };
        [SerializeField] private float attackDuration = 0.45f;
        [SerializeField] private float comboInputWindow = 0.5f;
        [SerializeField] private float attackStaminaCost = 12f;
        [SerializeField] private float attackRange = 1.3f;
        [SerializeField] private float attackRadius = 1f;
        [SerializeField] private LayerMask hittableLayers = ~0;

        [Header("Уклонение")]
        [SerializeField] private float dodgeDuration = 0.45f;
        [SerializeField] private float dodgeInvulnerableTime = 0.3f;
        [SerializeField] private float dodgeDistance = 4f;
        [SerializeField] private float dodgeStaminaCost = 20f;

        [Header("Парирование")]
        [SerializeField] private float parryWindow = 0.18f;
        [SerializeField] private float parryRecovery = 0.4f;
        [SerializeField] private float parryStaminaCost = 8f;
        [SerializeField] private float parryStaggerDuration = 1.5f;

        private State _state = State.Ready;
        private PlayerController _controller;
        private PlayerInputReader _input;
        private PlayerStamina _stamina;
        private Health _health;

        private int _comboIndex;
        private float _comboWindowEndsAt;
        private bool _attackBuffered;
        private float _invulnerableUntil;
        private float _parryWindowEndsAt;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _input = GetComponent<PlayerInputReader>();
            _stamina = GetComponent<PlayerStamina>();
            _health = GetComponent<Health>();

            _health.DamageGate = GateIncomingDamage;
        }

        private void OnEnable()
        {
            _input.AttackPressed += OnAttackPressed;
            _input.DodgePressed += OnDodgePressed;
            _input.ParryPressed += OnParryPressed;
        }

        private void OnDisable()
        {
            _input.AttackPressed -= OnAttackPressed;
            _input.DodgePressed -= OnDodgePressed;
            _input.ParryPressed -= OnParryPressed;
        }

        /// <summary>i-фреймы и парирование решают судьбу входящего удара.</summary>
        private bool GateIncomingDamage(DamageInfo info)
        {
            if (Time.time < _invulnerableUntil)
                return false;

            if (_state == State.Parrying && Time.time < _parryWindowEndsAt)
            {
                if (info.Attacker != null && info.Attacker.TryGetComponent(out IStaggerable staggerable))
                    staggerable.Stagger(parryStaggerDuration);
                return false;
            }

            return true;
        }

        private void OnAttackPressed()
        {
            if (_state == State.Attacking)
            {
                _attackBuffered = true;
                return;
            }

            if (_state != State.Ready)
                return;

            // Комбо продолжается, только пока открыто окно после прошлого удара.
            if (Time.time > _comboWindowEndsAt)
                _comboIndex = 0;

            TryStartAttack();
        }

        private void TryStartAttack()
        {
            if (!_stamina.TrySpend(attackStaminaCost))
                return;

            StartCoroutine(AttackRoutine());
        }

        private IEnumerator AttackRoutine()
        {
            _state = State.Attacking;
            _controller.MovementLocked = true;

            // Доворот в сторону ввода, чтобы комбо можно было перенацелить.
            Vector3 aim = _controller.GetCameraRelativeDirection(_input.Move);
            if (aim.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(aim);

            // Удар срабатывает в середине замаха.
            yield return new WaitForSeconds(attackDuration * 0.5f);
            DealDamage(comboDamage[Mathf.Min(_comboIndex, comboDamage.Length - 1)]);
            yield return new WaitForSeconds(attackDuration * 0.5f);

            _comboIndex = (_comboIndex + 1) % comboDamage.Length;
            _comboWindowEndsAt = Time.time + comboInputWindow;
            _state = State.Ready;
            _controller.MovementLocked = false;

            if (_attackBuffered)
            {
                _attackBuffered = false;
                TryStartAttack();
            }
        }

        private void DealDamage(float amount)
        {
            Vector3 center = transform.position + transform.forward * attackRange + Vector3.up;
            Collider[] hits = Physics.OverlapSphere(center, attackRadius, hittableLayers);

            foreach (Collider hit in hits)
            {
                if (hit.transform.root == transform.root)
                    continue;

                GameObject root = hit.transform.root.gameObject;

                // Сетевой враг: урон через ServerRpc, чтобы хост был авторитетным.
                if (root.TryGetComponent(out NetworkHealth networkHealth))
                {
                    NetworkObject attackerNob = GetComponent<NetworkObject>();
                    networkHealth.ServerRequestDamage(amount, hit.ClosestPoint(center), attackerNob);
                    continue;
                }

                // Локальный режим (без сети): прямой вызов.
                if (hit.TryGetComponent(out IDamageable damageable) ||
                    root.TryGetComponent(out damageable))
                {
                    damageable.TakeDamage(new DamageInfo(amount, hit.ClosestPoint(center), gameObject));
                }
            }
        }

        private void OnDodgePressed()
        {
            if (_state != State.Ready || !_stamina.TrySpend(dodgeStaminaCost))
                return;

            StartCoroutine(DodgeRoutine());
        }

        private IEnumerator DodgeRoutine()
        {
            _state = State.Dodging;
            _controller.MovementLocked = true;
            _invulnerableUntil = Time.time + dodgeInvulnerableTime;
            _attackBuffered = false;

            Vector3 direction = _controller.GetCameraRelativeDirection(_input.Move);
            if (direction.sqrMagnitude < 0.01f)
                direction = -transform.forward; // без ввода — отскок назад

            transform.rotation = Quaternion.LookRotation(
                direction == -transform.forward ? transform.forward : direction);

            float speed = dodgeDistance / dodgeDuration;
            float endTime = Time.time + dodgeDuration;
            while (Time.time < endTime)
            {
                _controller.Controller.Move(direction * (speed * Time.deltaTime));
                yield return null;
            }

            _state = State.Ready;
            _controller.MovementLocked = false;
        }

        private void OnParryPressed()
        {
            if (_state != State.Ready || !_stamina.TrySpend(parryStaminaCost))
                return;

            StartCoroutine(ParryRoutine());
        }

        private IEnumerator ParryRoutine()
        {
            _state = State.Parrying;
            _controller.MovementLocked = true;
            _parryWindowEndsAt = Time.time + parryWindow;

            yield return new WaitForSeconds(parryWindow + parryRecovery);

            _state = State.Ready;
            _controller.MovementLocked = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + transform.forward * attackRange + Vector3.up, attackRadius);
        }
    }
}
