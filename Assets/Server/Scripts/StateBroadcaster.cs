using UnityEngine;
using CarSim.Shared;

namespace CarSim.Server
{
    public class StateBroadcaster : MonoBehaviour
    {
        public NetConfig config;
        public UdpServerPeer udpPeer;
        public ServerSimulationController simController;
        public CameraFocusManager cameraFocusManager;

        private float _sendInterval;
        private float _nextSendTime;
        private ushort _sendSeq = 0;
        private byte[] _sendBuffer = new byte[Protocol.MAX_PACKET_SIZE];

        private void Start()
        {
            if (config != null)
            {
                _sendInterval = 1f / config.stateSendRate;
            }
        }

        private void Update()
        {
            if (Time.time >= _nextSendTime)
            {
                _nextSendTime = Time.time + _sendInterval;
                BroadcastState();
            }
        }

        private void BroadcastState()
        {
            if (udpPeer == null || simController == null || cameraFocusManager == null)
                return;

            CameraPartId currentPart = cameraFocusManager.CurrentPartId;
            StateS2C state = simController.GetCurrentState(currentPart);

            int len = Protocol.SerializeState(_sendBuffer, _sendSeq++, state);
            udpPeer.Send(_sendBuffer, len);
        }
    }
}
