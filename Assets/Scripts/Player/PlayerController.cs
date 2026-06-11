using UnityEngine;

namespace Ashveil.Player
{
    /// <summary>
    /// Движение от третьего лица: направление берётся относительно камеры
    /// (Cinemachine следует за игроком, см. docs/combat-prototype-setup.md).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerStamina))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Движение")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float rotationSpeed = 14f;
        [SerializeField] private float gravity = -20f;

        [Header("Стамина")]
        [SerializeField] private float sprintStaminaPerSecond = 12f;

        /// <summary>PlayerCombat блокирует движение на время атак и уклонения.</summary>
        public bool MovementLocked { get; set; }

        public CharacterController Controller { get; private set; }

        private PlayerInputReader _input;
        private PlayerStamina _stamina;
        private Transform _cameraTransform;
        private float _verticalVelocity;

        private void Awake()
        {
            Controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputReader>();
            _stamina = GetComponent<PlayerStamina>();
        }

        private void Start()
        {
            _cameraTransform = Camera.main != null ? Camera.main.transform : null;
        }

        private void Update()
        {
            ApplyGravity();

            if (MovementLocked)
            {
                Controller.Move(Vector3.up * (_verticalVelocity * Time.deltaTime));
                return;
            }

            Vector3 moveDirection = GetCameraRelativeDirection(_input.Move);

            float speed = walkSpeed;
            bool wantsSprint = _input.SprintHeld && moveDirection.sqrMagnitude > 0.01f;
            if (wantsSprint && _stamina.Drain(sprintStaminaPerSecond))
                speed = sprintSpeed;

            Vector3 motion = moveDirection * speed;
            motion.y = _verticalVelocity;
            Controller.Move(motion * Time.deltaTime);

            RotateTowards(moveDirection);
        }

        /// <summary>Направление ввода в мировых координатах относительно камеры.</summary>
        public Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.01f)
                return Vector3.zero;

            Vector3 forward = _cameraTransform != null ? _cameraTransform.forward : Vector3.forward;
            Vector3 right = _cameraTransform != null ? _cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;

            return (forward.normalized * input.y + right.normalized * input.x).normalized;
        }

        public void RotateTowards(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.01f)
                return;

            Quaternion target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        private void ApplyGravity()
        {
            if (Controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += gravity * Time.deltaTime;
        }
    }
}
