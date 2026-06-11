using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ashveil.Player
{
    /// <summary>
    /// Единственная точка чтения ввода (Unity Input System).
    /// Биндинги заданы в коде, чтобы прототип работал без .inputactions-ассета.
    /// Клавиатура+мышь и геймпад — по требованию GDD (п.14).
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public bool SprintHeld { get; private set; }

        public event Action AttackPressed;
        public event Action DodgePressed;
        public event Action ParryPressed;

        private InputAction _move;
        private InputAction _attack;
        private InputAction _dodge;
        private InputAction _parry;
        private InputAction _sprint;

        private void Awake()
        {
            _move = new InputAction("Move");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _move.AddBinding("<Gamepad>/leftStick");

            _attack = new InputAction("Attack", InputActionType.Button);
            _attack.AddBinding("<Mouse>/leftButton");
            _attack.AddBinding("<Gamepad>/buttonWest");

            _parry = new InputAction("Parry", InputActionType.Button);
            _parry.AddBinding("<Mouse>/rightButton");
            _parry.AddBinding("<Gamepad>/leftShoulder");

            _dodge = new InputAction("Dodge", InputActionType.Button);
            _dodge.AddBinding("<Keyboard>/space");
            _dodge.AddBinding("<Gamepad>/buttonEast");

            _sprint = new InputAction("Sprint", InputActionType.Button);
            _sprint.AddBinding("<Keyboard>/leftShift");
            _sprint.AddBinding("<Gamepad>/leftStickPress");

            _attack.performed += _ => AttackPressed?.Invoke();
            _dodge.performed += _ => DodgePressed?.Invoke();
            _parry.performed += _ => ParryPressed?.Invoke();
        }

        private void OnEnable()
        {
            _move.Enable();
            _attack.Enable();
            _parry.Enable();
            _dodge.Enable();
            _sprint.Enable();
        }

        private void OnDisable()
        {
            _move.Disable();
            _attack.Disable();
            _parry.Disable();
            _dodge.Disable();
            _sprint.Disable();
        }

        private void Update()
        {
            Move = _move.ReadValue<Vector2>();
            SprintHeld = _sprint.IsPressed();
        }
    }
}
