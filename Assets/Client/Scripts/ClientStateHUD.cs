using UnityEngine;
using UnityEngine.UI;
using CarSim.Shared;
using TMPro;
namespace CarSim.Client
{
    public class ClientStateHUD : MonoBehaviour
    {
        public UdpClientPeer udpPeer;

        [Header("HUD UI")]
        public TextMeshProUGUI textSpeed;
        public TextMeshProUGUI textGear;
        public TextMeshProUGUI textIndicator;
        public TextMeshProUGUI textCameraFocus;
        public TextMeshProUGUI textPing;
        public TextMeshProUGUI textConnectionStatus;

        private void Update()
        {
            if (udpPeer == null) return;

            bool hasState = udpPeer.TryGetLatestState(out StateS2C state);

            if (hasState)
            {
                // Speed
                if (textSpeed != null)
                {
                    textSpeed.text = $"{state.speedKmh:F0} km/h";
                }

                // Gear
                if (textGear != null)
                {
                    textGear.text = GetGearString(state.currentGear);
                }

                // Indicator
                if (textIndicator != null)
                {
                    textIndicator.text = state.indicator.ToString();
                }

                // Camera Focus
                if (textCameraFocus != null)
                {
                    textCameraFocus.text = GetCameraPartString(state.cameraPart);
                }

                // Ping
                if (textPing != null)
                {
                    textPing.text = $"{udpPeer.LastPingMs:F0} ms";
                }

                // Connection status
                if (textConnectionStatus != null)
                {
                    textConnectionStatus.text = "Connected";
                    textConnectionStatus.color = Color.green;
                }
            }
            else
            {
                // No state
                if (textConnectionStatus != null)
                {
                    textConnectionStatus.text = "Waiting for server...";
                    textConnectionStatus.color = Color.yellow;
                }
            }
        }

        private string GetGearString(sbyte gear)
        {
            if (gear == -1) return "R";
            if (gear == 0) return "N";
            return gear.ToString();
        }

        private string GetCameraPartString(CameraPartId part)
        {
            switch (part)
            {
                case CameraPartId.FL_Wheel: return "FL Wheel";
                case CameraPartId.FR_Wheel: return "FR Wheel";
                case CameraPartId.RL_Wheel: return "RL Wheel";
                case CameraPartId.RR_Wheel: return "RR Wheel";
                case CameraPartId.Engine: return "Engine";
                case CameraPartId.Exhaust: return "Exhaust";
                case CameraPartId.SteeringLinkage: return "Steering";
                case CameraPartId.BrakeCaliperFront: return "Brake Caliper";
                case CameraPartId.SuspensionFront: return "Suspension";
                case CameraPartId.Dashboard: return "Dashboard";
                default: return part.ToString();
            }
        }
    }
}
