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

        private CameraPartId _currentPartId = CameraPartId.Dashboard;
        private CameraPartId _targetPartId = CameraPartId.Dashboard;
        private float _lerpProgress = 1f;
        private float _lerpDuration = 1f;

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

            Debug.Log($"[CameraFocus] Transitioning to {_targetPartId}");
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
