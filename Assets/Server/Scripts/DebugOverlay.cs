using UnityEngine;
using UnityEngine.UI;
using CarSim.Shared;
using TMPro;

namespace CarSim.Server
{
    public class DebugOverlay : MonoBehaviour
    {
        public UdpServerPeer udpPeer;
        public ServerSimulationController simController;
        public CameraFocusManager cameraFocusManager;

        [Header("UI")]
        public TextMeshProUGUI statusText;

        private void Update()
        {
            if (statusText == null) return;

            bool hasInput = udpPeer.TryGetLatestInput(out InputC2S input, out ushort seq, out uint timestamp);

            uint now = StopwatchTime.TimestampMs();
            float inputAge = hasInput ? (now - timestamp) : 0f;

            StateS2C state = simController.GetCurrentState(cameraFocusManager.CurrentPartId);

            statusText.text = $"SERVER DEBUG\n" +
                $"Input Seq: {seq} Age: {inputAge:F0}ms\n" +
                $"Speed: {state.speedKmh:F1} km/h\n" +
                $"RPM: {state.rpm:F0}\n" +
                $"Gear: {GetGearString(state.currentGear)}\n" +
                $"Steer: {state.steerAngle:F1}°\n" +
                $"Focus: {state.cameraPart}\n" +
                $"Lights: {state.lights}\n" +
                $"Indicator: {state.indicator}\n" +
                $"Steer In: {input.steer:F2} Throttle: {input.throttle:F2} Brake: {input.brake:F2} HB: {input.handbrake}";
        }

        private string GetGearString(sbyte gear)
        {
            if (gear == -1) return "R";
            if (gear == 0) return "N";
            return gear.ToString();
        }
    }
}
