using UnityEngine;
using CarSim.Shared;

namespace CarSim.Server
{
    [System.Serializable]
    public class CameraFocusPoint
    {
        public CameraPartId partId;
        public Transform anchor;
        public Vector3 offset = new Vector3(0, 1, -3);
        public float fov = 60f;
        public float lerpTime = 1f;
    }

    public class CameraFocusManager : MonoBehaviour
    {
        public Camera mainCamera;
        public CameraFocusPoint[] focusPoints;
        public ServerCameraController cameraController; // For general camera modes

        private CameraPartId _currentPartId = CameraPartId.FollowCamera;
        private CameraPartId _targetPartId = CameraPartId.FollowCamera;
        private float _lerpProgress = 1f;
        private float _lerpDuration = 1f;
        private bool _isGeneralCameraMode = true;

        private Vector3 _startPos, _targetPos;
        private Quaternion _startRot, _targetRot;
        private float _startFov, _targetFov;

        public CameraPartId CurrentPartId => _currentPartId;

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // Initialize to first focus point
            if (focusPoints != null && focusPoints.Length > 0)
            {
                SetFocus(_currentPartId);
                _lerpProgress = 1f; // Instant
            }
        }

        private void Update()
        {
            // Only update camera if NOT in general camera mode (ServerCameraController handles those)
            if (_isGeneralCameraMode) return;

            if (_lerpProgress < 1f)
            {
                _lerpProgress += Time.deltaTime / _lerpDuration;
                _lerpProgress = Mathf.Clamp01(_lerpProgress);

                float t = Mathf.SmoothStep(0f, 1f, _lerpProgress);

                mainCamera.transform.position = Vector3.Lerp(_startPos, _targetPos, t);
                mainCamera.transform.rotation = Quaternion.Slerp(_startRot, _targetRot, t);
                mainCamera.fieldOfView = Mathf.Lerp(_startFov, _targetFov, t);

                if (_lerpProgress >= 1f)
                {
                    _currentPartId = _targetPartId;
                }
            }
            else
            {
                // Follow target
                CameraFocusPoint focus = GetFocusPoint(_currentPartId);
                if (focus != null && focus.anchor != null)
                {
                    mainCamera.transform.position = focus.anchor.position + focus.anchor.TransformDirection(focus.offset);
                    mainCamera.transform.LookAt(focus.anchor.position);
                }
            }
        }

        public void SetFocus(CameraPartId partId)
        {
            if (_targetPartId == partId && _lerpProgress >= 1f)
            {
                return; // Already at target
            }

            _targetPartId = partId;

            // Check if it's a general camera mode (0-2)
            if ((int)partId <= 2)
            {
                // General camera mode - use ServerCameraController
                _isGeneralCameraMode = true;
                _lerpProgress = 1f; // Instant switch
                _currentPartId = partId;

                if (cameraController != null)
                {
                    cameraController.enabled = true;
                    cameraController.SetCameraMode((int)partId);
                    Debug.Log($"[CameraFocus] Switched to general camera mode: {_targetPartId}");
                }
                else
                {
                    Debug.LogWarning($"[CameraFocus] ServerCameraController not assigned!");
                }
            }
            else
            {
                // Part-specific focus - use focus points
                _isGeneralCameraMode = false;

                // Disable ServerCameraController
                if (cameraController != null)
                {
                    cameraController.enabled = false;
                }

                CameraFocusPoint targetFocus = GetFocusPoint(_targetPartId);
                if (targetFocus == null || targetFocus.anchor == null)
                {
                    Debug.LogWarning($"[CameraFocus] No focus point for {_targetPartId}");
                    return;
                }

                _startPos = mainCamera.transform.position;
                _startRot = mainCamera.transform.rotation;
                _startFov = mainCamera.fieldOfView;

                _targetPos = targetFocus.anchor.position + targetFocus.anchor.TransformDirection(targetFocus.offset);
                _targetRot = Quaternion.LookRotation(targetFocus.anchor.position - _targetPos);
                _targetFov = targetFocus.fov;

                _lerpDuration = targetFocus.lerpTime;
                _lerpProgress = 0f;

                Debug.Log($"[CameraFocus] Transitioning to part focus: {_targetPartId}");
            }
        }

        private CameraFocusPoint GetFocusPoint(CameraPartId partId)
        {
            if (focusPoints == null) return null;

            foreach (var point in focusPoints)
            {
                if (point.partId == partId)
                {
                    return point;
                }
            }

            return null;
        }
    }
}
