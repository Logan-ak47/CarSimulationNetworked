using UnityEngine;
using UnityEngine.UI;
using CarSim.Shared;
using TMPro;

namespace CarSim.Server
{
    public class LocalCarTestUI : MonoBehaviour
    {
        [Header("References")]
        public ServerSimulationController simController;

        [Header("UI Elements")]
        public Button btnThrottle;
        public Button btnBrake;
        public Button btnGear1;
        public TextMeshProUGUI txtStatus;

        private bool _isThrottlePressed = false;
        private float _currentThrottle = 0f;
        private ushort _seq = 0;

        private void Start()
        {
            if (btnThrottle != null)
            {
                btnThrottle.onClick.AddListener(() => {
                    _isThrottlePressed = !_isThrottlePressed;
                    Debug.Log($"[LocalTest] Throttle toggled: {_isThrottlePressed}");
                    UpdateButtonText();
                });
            }

            if (btnBrake != null)
            {
                btnBrake.onClick.AddListener(() => {
                    if (simController != null)
                    {
                        simController.ResetCar();
                        Debug.Log($"[LocalTest] Car reset");
                    }
                });
            }

            if (btnGear1 != null)
            {
                btnGear1.onClick.AddListener(() => {
                    if (simController != null)
                    {
                        simController.SetGear(1);
                        Debug.Log($"[LocalTest] Gear set to 1");
                    }
                });
            }

            UpdateButtonText();
        }

        private void Update()
        {
            if (simController == null)
            {
                Debug.LogError("[LocalTest] simController is NULL!");
                return;
            }

            // Update throttle value smoothly
            if (_isThrottlePressed)
            {
                _currentThrottle = Mathf.MoveTowards(_currentThrottle, 1f, Time.deltaTime * 2f);
            }
            else
            {
                _currentThrottle = Mathf.MoveTowards(_currentThrottle, 0f, Time.deltaTime * 5f);
            }

            // Log every 30 frames
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[LocalTest] Update running: throttlePressed={_isThrottlePressed}, currentThrottle={_currentThrottle:F2}");
            }

            // Send input directly to sim controller
            InputC2S input = new InputC2S
            {
                throttle = _currentThrottle,
                brake = 0f,
                steer = 0f,
                handbrake = 0
            };

            simController.ApplyInput(input, _seq++, StopwatchTime.TimestampMs());

            // Update status text
            if (txtStatus != null)
            {
                txtStatus.text = $"Throttle: {_currentThrottle:F2}\nGear: {simController.currentGear}\nSpeed: {simController.GetCurrentState(CameraPartId.Dashboard).speedKmh:F1} km/h";
            }
        }

        private void UpdateButtonText()
        {
            if (btnThrottle != null)
            {
                Text btnText = btnThrottle.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = _isThrottlePressed ? "THROTTLE: ON" : "THROTTLE: OFF";
                }
            }
        }
    }
}
