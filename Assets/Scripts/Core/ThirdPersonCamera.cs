using UnityEngine;
using UnityEngine.InputSystem;

namespace Ashveil.Core
{
    /// <summary>
    /// Простая орбитальная камера от третьего лица для прототипа.
    /// Позже заменяется Cinemachine-ригом (GDD §14) — интерфейс движения
    /// игрока не изменится, он читает Camera.main.
    /// Esc — освободить курсор, клик — захватить обратно.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 5.5f;
        [SerializeField] private float heightOffset = 1.6f;
        [SerializeField] private float sensitivity = 0.12f;
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 65f;

        private float _yaw;
        private float _pitch = 18f;

        public void SetTarget(Transform newTarget) => target = newTarget;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.None;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
                Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (Cursor.lockState != CursorLockMode.Locked || Mouse.current == null)
                return;

            Vector2 delta = Mouse.current.delta.ReadValue();
            _yaw += delta.x * sensitivity;
            _pitch = Mathf.Clamp(_pitch - delta.y * sensitivity, minPitch, maxPitch);
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 focus = target.position + Vector3.up * heightOffset;
            Vector3 desired = focus - rotation * Vector3.forward * distance;

            // Не проваливаться под рельеф (свой коллайдер игрока игнорируем).
            if (Physics.Linecast(focus, desired, out RaycastHit hit) &&
                hit.transform.root != target.root)
            {
                desired = hit.point + hit.normal * 0.3f;
            }

            transform.SetPositionAndRotation(desired, rotation);
        }
    }
}
