using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CarSim.Shared;
using System;
using TMPro;

namespace CarSim.Client
{
    public class ClientInputController : MonoBehaviour
    {
        [Header("Config & Peers")]
        public NetConfig config;
        public TcpClientPeer tcpPeer;
        public UdpClientPeer udpPeer;

        [Header("Driving Control Buttons")]
        public Button btnThrottle;
        public Button btnBrake;
        public Button btnSteerLeft;
        public Button btnSteerRight;
        public Toggle toggleHandbrake;

        [Header("Gear UI")]
        public Button btnGearR;
        public Button btnGearN;
        public Button btnGear1;
        public Button btnGear2;
        public Button btnGear3;
        public Button btnGear4;
        public Button btnGear5;
        public Button btnGear6;

        [Header("Lights UI")]
        public Toggle toggleHeadlights;
        public Button btnIndicatorLeft;
        public Button btnIndicatorRight;
        public Button btnIndicatorHazard;
        public Button btnIndicatorOff;

        [Header("Camera UI")]
        public TMP_Dropdown dropdownCameraFocus;

        [Header("Other")]
        public Button btnResetCar;

        [Header("Input Response")]
        public float throttleRate = 2f;      // How fast throttle increases when button held
        public float brakeRate = 3f;         // How fast brake increases when button held
        public float steerRate = 3f;         // How fast steering changes when button held
        public float returnToZeroSpeed = 5f; // How fast inputs return to zero when released

        // Button states (pressed/released)
        private bool _isThrottlePressed = false;
        private bool _isBrakePressed = false;
        private bool _isSteerLeftPressed = false;
        private bool _isSteerRightPressed = false;

        // Current input values
        private float _currentSteer = 0f;
        private float _currentThrottle = 0f;
        private float _currentBrake = 0f;
        private byte _handbrake = 0;

        private float _sendInterval;
        private float _nextSendTime;
        private ushort _sendSeq = 0;
        private byte[] _sendBuffer = new byte[Protocol.MAX_PACKET_SIZE];

        private float _lastDiscreteCommandTime;
        private const float DISCRETE_COMMAND_COOLDOWN = 0.1f;

        private void Start()
        {
            if (config != null)
            {
                _sendInterval = 1f / config.inputSendRate;
            }

            SetupDrivingControls();
            SetupGearButtons();
            SetupLightsAndIndicators();
            SetupCameraAndOther();
        }

        private void SetupDrivingControls()
        {
            // Throttle button - hold to accelerate
            if (btnThrottle != null)
            {
                EventTrigger trigger = btnThrottle.gameObject.GetComponent<EventTrigger>();
                if (trigger == null) trigger = btnThrottle.gameObject.AddComponent<EventTrigger>();

                EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entryDown.callback.AddListener((data) => {
                    _isThrottlePressed = true;
                    Debug.Log("[ClientInput] Throttle button pressed!");
                });
                trigger.triggers.Add(entryDown);

                EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                entryUp.callback.AddListener((data) => {
                    _isThrottlePressed = false;
                    Debug.Log("[ClientInput] Throttle button released (PointerUp)!");
                });
                trigger.triggers.Add(entryUp);

                EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                entryExit.callback.AddListener((data) => {
                    _isThrottlePressed = false;
                    Debug.Log("[ClientInput] Throttle button released (PointerExit)!");
                });
                trigger.triggers.Add(entryExit);

                Debug.Log($"[ClientInput] Throttle button setup complete on {btnThrottle.gameObject.name}");
            }
            else
            {
                Debug.LogError("[ClientInput] btnThrottle is not assigned!");
            }

            // Brake button - hold to brake
            if (btnBrake != null)
            {
                EventTrigger trigger = btnBrake.gameObject.GetComponent<EventTrigger>();
                if (trigger == null) trigger = btnBrake.gameObject.AddComponent<EventTrigger>();

                EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entryDown.callback.AddListener((data) => { _isBrakePressed = true; });
                trigger.triggers.Add(entryDown);

                EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                entryUp.callback.AddListener((data) => { _isBrakePressed = false; });
                trigger.triggers.Add(entryUp);
            }

            // Steer Left button - hold to turn left
            if (btnSteerLeft != null)
            {
                EventTrigger trigger = btnSteerLeft.gameObject.GetComponent<EventTrigger>();
                if (trigger == null) trigger = btnSteerLeft.gameObject.AddComponent<EventTrigger>();

                EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entryDown.callback.AddListener((data) => { _isSteerLeftPressed = true; });
                trigger.triggers.Add(entryDown);

                EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                entryUp.callback.AddListener((data) => { _isSteerLeftPressed = false; });
                trigger.triggers.Add(entryUp);
            }

            // Steer Right button - hold to turn right
            if (btnSteerRight != null)
            {
                EventTrigger trigger = btnSteerRight.gameObject.GetComponent<EventTrigger>();
                if (trigger == null) trigger = btnSteerRight.gameObject.AddComponent<EventTrigger>();

                EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entryDown.callback.AddListener((data) => { _isSteerRightPressed = true; });
                trigger.triggers.Add(entryDown);

                EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                entryUp.callback.AddListener((data) => { _isSteerRightPressed = false; });
                trigger.triggers.Add(entryUp);
            }

            // Handbrake toggle
            if (toggleHandbrake != null)
            {
                toggleHandbrake.onValueChanged.AddListener((isOn) => { _handbrake = (byte)(isOn ? 1 : 0); });
            }
        }

        private void SetupGearButtons()
        {
            if (btnGearR != null) btnGearR.onClick.AddListener(() => SendGear(-1));
            if (btnGearN != null) btnGearN.onClick.AddListener(() => SendGear(0));
            if (btnGear1 != null) btnGear1.onClick.AddListener(() => SendGear(1));
            if (btnGear2 != null) btnGear2.onClick.AddListener(() => SendGear(2));
            if (btnGear3 != null) btnGear3.onClick.AddListener(() => SendGear(3));
            if (btnGear4 != null) btnGear4.onClick.AddListener(() => SendGear(4));
            if (btnGear5 != null) btnGear5.onClick.AddListener(() => SendGear(5));
            if (btnGear6 != null) btnGear6.onClick.AddListener(() => SendGear(6));
        }

        private void SetupLightsAndIndicators()
        {
            if (toggleHeadlights != null)
            {
                toggleHeadlights.onValueChanged.AddListener(SendHeadlights);
            }

            if (btnIndicatorLeft != null) btnIndicatorLeft.onClick.AddListener(() => SendIndicator(IndicatorMode.Left));
            if (btnIndicatorRight != null) btnIndicatorRight.onClick.AddListener(() => SendIndicator(IndicatorMode.Right));
            if (btnIndicatorHazard != null) btnIndicatorHazard.onClick.AddListener(() => SendIndicator(IndicatorMode.Hazard));
            if (btnIndicatorOff != null) btnIndicatorOff.onClick.AddListener(() => SendIndicator(IndicatorMode.Off));
        }

        private void SetupCameraAndOther()
        {
            if (dropdownCameraFocus != null)
            {
                dropdownCameraFocus.ClearOptions();
                dropdownCameraFocus.AddOptions(new System.Collections.Generic.List<string>
                {
                    // General camera modes
                    "Follow Camera", "Hood Camera", "Orbit Camera",
                    // Car part focus cameras
                    "FL Wheel", "FR Wheel", "RL Wheel", "RR Wheel",
                    "Engine", "Exhaust", "Steering", "Brake Caliper", "Suspension", "Dashboard"
                });
                dropdownCameraFocus.onValueChanged.AddListener(SendCameraFocus);
            }

            if (btnResetCar != null)
            {
                btnResetCar.onClick.AddListener(SendResetCar);
            }
        }

        private void Update()
        {
            UpdateInputValues();

            if (Time.time >= _nextSendTime)
            {
                _nextSendTime = Time.time + _sendInterval;
                SendInput();
            }
        }

        private void UpdateInputValues()
        {
            float deltaTime = Time.deltaTime;

            // Debug button states every 60 frames
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[ClientInput] Button states: throttle={_isThrottlePressed}, brake={_isBrakePressed}, steerL={_isSteerLeftPressed}, steerR={_isSteerRightPressed}");
            }

            // Update Steering
            if (_isSteerLeftPressed && _isSteerRightPressed)
            {
                // Both pressed - return to center
                _currentSteer = Mathf.MoveTowards(_currentSteer, 0f, returnToZeroSpeed * deltaTime);
            }
            else if (_isSteerLeftPressed)
            {
                // Steer left (-1)
                _currentSteer = Mathf.MoveTowards(_currentSteer, -1f, steerRate * deltaTime);
            }
            else if (_isSteerRightPressed)
            {
                // Steer right (+1)
                _currentSteer = Mathf.MoveTowards(_currentSteer, 1f, steerRate * deltaTime);
            }
            else
            {
                // Released - return to center
                _currentSteer = Mathf.MoveTowards(_currentSteer, 0f, returnToZeroSpeed * deltaTime);
            }

            // Update Throttle
            if (_isThrottlePressed)
            {
                // Increase throttle
                float oldThrottle = _currentThrottle;
                _currentThrottle = Mathf.MoveTowards(_currentThrottle, 1f, throttleRate * deltaTime);

                // Debug when throttle changes
                if (Mathf.Abs(_currentThrottle - oldThrottle) > 0.01f)
                {
                    Debug.Log($"[ClientInput] Throttle increasing: {_currentThrottle:F2} (pressed={_isThrottlePressed})");
                }
            }
            else
            {
                // Release throttle - return to zero
                _currentThrottle = Mathf.MoveTowards(_currentThrottle, 0f, returnToZeroSpeed * deltaTime);
            }

            // Update Brake
            if (_isBrakePressed)
            {
                // Increase brake
                _currentBrake = Mathf.MoveTowards(_currentBrake, 1f, brakeRate * deltaTime);
            }
            else
            {
                // Release brake - return to zero
                _currentBrake = Mathf.MoveTowards(_currentBrake, 0f, returnToZeroSpeed * deltaTime);
            }

            // Handbrake is handled by toggle callback
        }

        private void SendInput()
        {
            if (udpPeer == null) return;

            InputC2S input = new InputC2S
            {
                steer = Mathf.Clamp(_currentSteer, -1f, 1f),
                throttle = Mathf.Clamp01(_currentThrottle),
                brake = Mathf.Clamp01(_currentBrake),
                handbrake = _handbrake
            };

            // Debug log first few inputs or when non-zero
            if (_sendSeq < 5 || (_sendSeq % 100 == 0) || input.throttle > 0.1f || input.brake > 0.1f || Mathf.Abs(input.steer) > 0.1f)
            {
                Debug.Log($"[ClientInput] Sending seq={_sendSeq}: throttle={input.throttle:F2}, brake={input.brake:F2}, steer={input.steer:F2}, handbrake={input.handbrake}");
            }

            int len = Protocol.SerializeInput(_sendBuffer, _sendSeq++, input);
            udpPeer.Send(_sendBuffer, len);
        }

        #region Discrete Commands (TCP)

        private void SendGear(sbyte gear)
        {
            if (!CanSendDiscreteCommand()) return;

            SetGearC2S msg = new SetGearC2S { gear = gear };
            int len = Protocol.SerializeSetGear(_sendBuffer, _sendSeq++, msg);
            tcpPeer.SendMessage(SubArray(_sendBuffer, 0, len));
        }

        private void SendHeadlights(bool on)
        {
            if (!CanSendDiscreteCommand()) return;

            ToggleHeadlightsC2S msg = new ToggleHeadlightsC2S { on = (byte)(on ? 1 : 0) };
            int len = Protocol.SerializeToggleHeadlights(_sendBuffer, _sendSeq++, msg);
            tcpPeer.SendMessage(SubArray(_sendBuffer, 0, len));
        }

        private void SendIndicator(IndicatorMode mode)
        {
            if (!CanSendDiscreteCommand()) return;

            SetIndicatorC2S msg = new SetIndicatorC2S { mode = mode };
            int len = Protocol.SerializeSetIndicator(_sendBuffer, _sendSeq++, msg);
            tcpPeer.SendMessage(SubArray(_sendBuffer, 0, len));
        }

        private void SendCameraFocus(int index)
        {
            if (!CanSendDiscreteCommand()) return;

            SetCameraFocusC2S msg = new SetCameraFocusC2S { partId = (CameraPartId)index };
            int len = Protocol.SerializeSetCameraFocus(_sendBuffer, _sendSeq++, msg);
            tcpPeer.SendMessage(SubArray(_sendBuffer, 0, len));
        }

        private void SendResetCar()
        {
            if (!CanSendDiscreteCommand()) return;

            int len = Protocol.SerializeResetCar(_sendBuffer, _sendSeq++);
            tcpPeer.SendMessage(SubArray(_sendBuffer, 0, len));
        }

        private bool CanSendDiscreteCommand()
        {
            if (Time.time - _lastDiscreteCommandTime < DISCRETE_COMMAND_COOLDOWN)
            {
                return false;
            }

            _lastDiscreteCommandTime = Time.time;
            return true;
        }

        #endregion

        private byte[] SubArray(byte[] data, int index, int length)
        {
            byte[] result = new byte[length];
            Buffer.BlockCopy(data, index, result, 0, length);
            return result;
        }
    }
}
