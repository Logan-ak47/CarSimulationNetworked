using UnityEngine;

namespace CarSim.Server
{
    public class ServerCameraController : MonoBehaviour
    {
        [Header("References")]
        public Transform carTransform;
        public Camera mainCamera;

        [Header("Follow Camera Settings")]
        public Vector3 followOffset = new Vector3(0, 2, -5);
        public float followSmoothSpeed = 5f;

        [Header("Hood Camera Settings")]
        public Vector3 hoodOffset = new Vector3(0, 1, 1);

        [Header("Orbit Camera Settings")]
        public float orbitDistance = 8f;
        public float orbitHeight = 3f;
        public float orbitSpeed = 20f;

        public enum CameraMode
        {
            Follow,
            Hood,
            Orbit
        }

        private CameraMode _currentMode = CameraMode.Follow;
        private float _orbitAngle = 0f;

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            if (carTransform == null || mainCamera == null) return;

            switch (_currentMode)
            {
                case CameraMode.Follow:
                    UpdateFollowCamera();
                    break;
                case CameraMode.Hood:
                    UpdateHoodCamera();
                    break;
                case CameraMode.Orbit:
                    UpdateOrbitCamera();
                    break;
            }
        }

        private void UpdateFollowCamera()
        {
            // Position camera behind and above the car
            Vector3 targetPosition = carTransform.position + carTransform.TransformDirection(followOffset);
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, followSmoothSpeed * Time.deltaTime);

            // Look at car
            mainCamera.transform.LookAt(carTransform.position + Vector3.up);
        }

        private void UpdateHoodCamera()
        {
            // Position camera at hood/driver position
            mainCamera.transform.position = carTransform.position + carTransform.TransformDirection(hoodOffset);
            mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, carTransform.rotation, 10f * Time.deltaTime);
        }

        private void UpdateOrbitCamera()
        {
            // Orbit around the car
            _orbitAngle += orbitSpeed * Time.deltaTime;
            if (_orbitAngle > 360f) _orbitAngle -= 360f;

            float radians = _orbitAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(radians) * orbitDistance, orbitHeight, Mathf.Cos(radians) * orbitDistance);

            mainCamera.transform.position = carTransform.position + offset;
            mainCamera.transform.LookAt(carTransform.position + Vector3.up);
        }

        public void SetCameraMode(int modeIndex)
        {
            _currentMode = (CameraMode)modeIndex;
            Debug.Log($"[ServerCamera] Switched to {_currentMode} camera");
        }

        public void SetCameraMode(CameraMode mode)
        {
            _currentMode = mode;
        }
    }
}
