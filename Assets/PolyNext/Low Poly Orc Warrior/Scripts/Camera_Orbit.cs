using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace PolyNext.LowPolyOrcs
{
    public class CameraOrbit : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private float targetHeight = 1.5f;

        [Header("Orbit Settings")]
        [SerializeField] private float distance = 10f;
        [SerializeField] private float rotationSpeed = 0.2f;

        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float minDistance = 3f;
        [SerializeField] private float maxDistance = 20f;

        [Header("Vertical Limits")]
        [SerializeField] private float minPitch = 10f;
        [SerializeField] private float maxPitch = 80f;

        private float yaw;
        private float pitch;

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;

            yaw = angles.y;
            pitch = angles.x;

            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            UpdateCameraPosition();
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;

            if (mouse == null || keyboard == null)
                return;

            // ==========================================
            // CAMERA ROTATION - LEFT MOUSE BUTTON
            // ==========================================

            // Не вращаем камеру, если курсор находится над UI
            bool pointerOverUI = EventSystem.current != null &&
                                 EventSystem.current.IsPointerOverGameObject();

            if (mouse.leftButton.isPressed && !pointerOverUI)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();

                yaw += mouseDelta.x * rotationSpeed;
                pitch -= mouseDelta.y * rotationSpeed;

                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            // ==========================================
            // CAMERA ZOOM - W / S
            // ==========================================

            float zoomInput = 0f;

            if (keyboard.wKey.isPressed)
                zoomInput += 1f;

            if (keyboard.sKey.isPressed)
                zoomInput -= 1f;

            distance -= zoomInput * zoomSpeed * Time.deltaTime;

            distance = Mathf.Clamp(
                distance,
                minDistance,
                maxDistance
            );

            // ==========================================
            // UPDATE CAMERA
            // ==========================================

            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            Vector3 targetPosition =
                target.position + Vector3.up * targetHeight;

            Quaternion rotation =
                Quaternion.Euler(pitch, yaw, 0f);

            Vector3 offset =
                rotation * Vector3.back * distance;

            transform.position =
                targetPosition + offset;

            transform.LookAt(targetPosition);
        }
    }
}
