using UnityEngine;
using CarSim.Shared;

namespace CarSim.Server
{
    public class ServerSimulationController : MonoBehaviour
    {
        [Header("Network")]
        public UdpServerPeer udpPeer;

        [Header("Car Physics")]
        public Rigidbody carBody;
        public Transform centerOfMass;

        [Header("Wheels")]
        public WheelCollider wheelFL;
        public WheelCollider wheelFR;
        public WheelCollider wheelRL;
        public WheelCollider wheelRR;

        [Header("Wheel Meshes")]
        public Transform wheelMeshFL;
        public Transform wheelMeshFR;
        public Transform wheelMeshRL;
        public Transform wheelMeshRR;

        [Header("Engine & Transmission")]
        public AnimationCurve torqueCurve = AnimationCurve.Linear(0, 200, 6000, 400);
        public float[] gearRatios = new float[] { -3.5f, 0f, 3.5f, 2.5f, 1.8f, 1.3f, 1.0f, 0.8f }; // R,N,1..6
        public float finalDriveRatio = 3.5f;
        public float maxRpm = 6000f;

        [Header("Steering & Brakes")]
        public float maxSteerAngle = 30f;
        public float steerSpeed = 5f;
        public float brakeTorque = 3000f;
        public float handbrakeTorque = 5000f;
        public float brakeBiasFront = 0.6f; // 60% front, 40% rear

        [Header("State")]
        public sbyte currentGear = 1;
        public bool headlightsOn = false;
        public IndicatorMode indicatorMode = IndicatorMode.Off;

        // Input state
        private InputC2S _currentInput;
        private ushort _lastProcessedInputSeq;
        private uint _lastInputTimestamp;
        private float _currentSteerAngle;

        // Physics state
        private float _currentRpm;
        private float _speedKmh;
        private float _wheelSlipFL, _wheelSlipFR, _wheelSlipRL, _wheelSlipRR;

        private Vector3 _resetPosition;
        private Quaternion _resetRotation;

        private int _fixedUpdateCounter = 0;

        private void Start()
        {
            if (carBody != null && centerOfMass != null)
            {
                carBody.centerOfMass = centerOfMass.localPosition;
            }

            _resetPosition = transform.position;
            _resetRotation = transform.rotation;

            // Debug: Check if udpPeer is assigned
            if (udpPeer == null)
            {
                Debug.LogError("[SimController] udpPeer is NOT assigned in Inspector!");
            }
            else
            {
                Debug.Log($"[SimController] udpPeer is assigned: {udpPeer.gameObject.name} (Instance {udpPeer.GetInstanceID()})");
            }
        }

        private void Update()
        {
            // Process ALL queued inputs this frame
            int inputsProcessed = 0;
            while (udpPeer != null && udpPeer.TryGetLatestInput(out InputC2S input, out ushort seq, out uint timestamp))
            {
                ApplyInput(input, seq, timestamp);
                inputsProcessed++;

                // Safety limit
                if (inputsProcessed > 200) break;
            }

            // Debug logging
            if (inputsProcessed > 0 && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[Update] Processed {inputsProcessed} inputs, final throttle={_currentInput.throttle:F2}, seq={_lastProcessedInputSeq}");
            }
        }

        private void FixedUpdate()
        {
            _fixedUpdateCounter++;

            // Physics runs at 50Hz using whatever the latest input is
            ApplyPhysics();
            UpdateWheelMeshes();
            ComputeState();
        }

        public void ApplyInput(InputC2S input, ushort seq, uint timestamp)
        {
            // Accept newer input (wrap-around safe)
            short seqDelta = (short)(seq - _lastProcessedInputSeq);
            if (seqDelta > 0 || _lastProcessedInputSeq == 0)
            {
                _currentInput = input;
                _lastProcessedInputSeq = seq;
                _lastInputTimestamp = timestamp;

                // Debug when significant throttle is applied
                if (input.throttle > 0.1f && seq % 100 == 0)
                {
                    Debug.Log($"[SimController] ApplyInput: seq={seq}, throttle={input.throttle:F2}, stored _currentInput.throttle={_currentInput.throttle:F2}");
                }
            }
        }

        private void ApplyPhysics()
        {
            // Debug every 50 frames
            if (_fixedUpdateCounter % 50 == 0)
            {
                Debug.Log($"[ApplyPhysics] _currentInput.throttle={_currentInput.throttle:F2}, gear={currentGear}, lastSeq={_lastProcessedInputSeq}");
            }

            // Steering with rate limit
            float targetSteer = Mathf.Clamp(_currentInput.steer, -1f, 1f) * maxSteerAngle;
            _currentSteerAngle = Mathf.Lerp(_currentSteerAngle, targetSteer, Time.fixedDeltaTime * steerSpeed);

            wheelFL.steerAngle = _currentSteerAngle;
            wheelFR.steerAngle = _currentSteerAngle;

            // Speed & RPM
            _speedKmh = carBody.linearVelocity.magnitude * 3.6f;

            // Gear ratio
            int gearIndex = Mathf.Clamp(currentGear + 1, 0, gearRatios.Length - 1); // -1→0, 0→1, 1→2...
            float ratio = gearRatios[gearIndex] * finalDriveRatio;

            // Motor torque
            float motorTorque = 0f;
            if (currentGear != 0) // Not neutral
            {
                // Estimate RPM from wheel speed
                float avgWheelRpm = (wheelRL.rpm + wheelRR.rpm) * 0.5f;
                _currentRpm = Mathf.Abs(avgWheelRpm * ratio);
                _currentRpm = Mathf.Clamp(_currentRpm, 800f, maxRpm);

                float torqueFromCurve = torqueCurve.Evaluate(_currentRpm);
                motorTorque = _currentInput.throttle * torqueFromCurve * ratio;

                // Debug motor torque calculation
                if (_currentInput.throttle > 0.01f)
                {
                    Debug.Log($"[Physics] gear={currentGear}, throttle={_currentInput.throttle:F2}, avgWheelRpm={avgWheelRpm:F1}, rpm={_currentRpm:F0}, torqueFromCurve={torqueFromCurve:F1}, ratio={ratio:F2}, motorTorque={motorTorque:F1}");
                    Debug.Log($"[Physics] Applying to wheels: RL.motorTorque={motorTorque * 0.5f:F1}, RR.motorTorque={motorTorque * 0.5f:F1}");

                    // Check if wheels are grounded
                    Debug.Log($"[Physics] Wheels grounded: FL={wheelFL.isGrounded}, FR={wheelFR.isGrounded}, RL={wheelRL.isGrounded}, RR={wheelRR.isGrounded}");
                }

                if (currentGear == -1) // Reverse
                {
                    motorTorque = -Mathf.Abs(motorTorque);
                }
            }
            else
            {
                _currentRpm = 800f; // Idle
            }

            // Apply motor to rear wheels
            wheelRL.motorTorque = motorTorque * 0.5f;
            wheelRR.motorTorque = motorTorque * 0.5f;

            // Brakes
            float frontBrake = _currentInput.brake * brakeTorque * brakeBiasFront;
            float rearBrake = _currentInput.brake * brakeTorque * (1f - brakeBiasFront);

            wheelFL.brakeTorque = frontBrake;
            wheelFR.brakeTorque = frontBrake;
            wheelRL.brakeTorque = rearBrake;
            wheelRR.brakeTorque = rearBrake;

            // Handbrake
            if (_currentInput.handbrake == 1)
            {
                wheelRL.brakeTorque += handbrakeTorque;
                wheelRR.brakeTorque += handbrakeTorque;
            }
        }

        private void UpdateWheelMeshes()
        {
            UpdateWheelMesh(wheelFL, wheelMeshFL);
            UpdateWheelMesh(wheelFR, wheelMeshFR);
            UpdateWheelMesh(wheelRL, wheelMeshRL);
            UpdateWheelMesh(wheelRR, wheelMeshRR);
        }

        private void UpdateWheelMesh(WheelCollider col, Transform mesh)
        {
            if (col == null || mesh == null) return;
            col.GetWorldPose(out Vector3 pos, out Quaternion rot);
            mesh.position = pos;
            // Add 90-degree rotation to align cylinder mesh with wheel collider
            mesh.rotation = rot * Quaternion.Euler(0, 0, 90);
        }

        private void ComputeState()
        {
            // Wheel slip
            wheelFL.GetGroundHit(out WheelHit hitFL);
            wheelFR.GetGroundHit(out WheelHit hitFR);
            wheelRL.GetGroundHit(out WheelHit hitRL);
            wheelRR.GetGroundHit(out WheelHit hitRR);

            _wheelSlipFL = Mathf.Abs(hitFL.forwardSlip) + Mathf.Abs(hitFL.sidewaysSlip);
            _wheelSlipFR = Mathf.Abs(hitFR.forwardSlip) + Mathf.Abs(hitFR.sidewaysSlip);
            _wheelSlipRL = Mathf.Abs(hitRL.forwardSlip) + Mathf.Abs(hitRL.sidewaysSlip);
            _wheelSlipRR = Mathf.Abs(hitRR.forwardSlip) + Mathf.Abs(hitRR.sidewaysSlip);
        }

        public StateS2C GetCurrentState(CameraPartId cameraPart)
        {
            return new StateS2C
            {
                position = carBody.position,
                rotation = carBody.rotation,
                speedKmh = _speedKmh,
                rpm = _currentRpm,
                currentGear = currentGear,
                steerAngle = _currentSteerAngle,
                wheelSlipFL = _wheelSlipFL,
                wheelSlipFR = _wheelSlipFR,
                wheelSlipRL = _wheelSlipRL,
                wheelSlipRR = _wheelSlipRR,
                lights = headlightsOn ? LightFlags.Headlight : LightFlags.None,
                indicator = indicatorMode,
                cameraPart = cameraPart,
                lastProcessedInputSeq = _lastProcessedInputSeq
            };
        }

        public void SetGear(sbyte gear)
        {
            currentGear = (sbyte)Mathf.Clamp(gear, -1, 6);
        }

        public void ToggleHeadlights(bool on)
        {
            headlightsOn = on;
        }

        public void SetIndicator(byte mode)
        {
            indicatorMode = (IndicatorMode)mode;
        }

        public void ResetCar()
        {
            carBody.position = _resetPosition;
            carBody.rotation = _resetRotation;
            carBody.linearVelocity = Vector3.zero;
            carBody.angularVelocity = Vector3.zero;
            currentGear = 1;
            _currentInput = default;
            _lastProcessedInputSeq = 0;
            Debug.Log("[SimController] Car reset");
        }
    }
}
