using UnityEngine;
using System;
using System.Net;
using System.Text;
using CarSim.Shared;

namespace CarSim.Server
{
    public class ServerCommandRouter : MonoBehaviour
    {
        public NetConfig config;
        public TcpServerPeer tcpPeer;
        public UdpServerPeer udpPeer;
        public ServerSimulationController simController;
        public CameraFocusManager cameraFocusManager;

        private ushort _sendSeq = 0;
        private uint _sessionId = 0;
        private byte[] _sendBuffer = new byte[Protocol.MAX_PACKET_SIZE];

        private void Update()
        {
            if (tcpPeer == null)
            {
                if (Time.frameCount == 1)
                {
                    Debug.LogError("[ServerRouter] tcpPeer is NULL!");
                }
                return;
            }

            while (tcpPeer.TryDequeueMessage(out TcpMessage msg))
            {
                Debug.Log($"[ServerRouter] Received message: {msg.msgType}");
                HandleTcpMessage(msg);
            }
        }

        private void HandleTcpMessage(TcpMessage msg)
        {
            try
            {
                switch (msg.msgType)
                {
                    case MsgType.HELLO_C2S:
                        HandleHello(msg);
                        break;
                    case MsgType.SET_GEAR_C2S:
                        HandleSetGear(msg);
                        break;
                    case MsgType.TOGGLE_HEADLIGHTS_C2S:
                        HandleToggleHeadlights(msg);
                        break;
                    case MsgType.SET_INDICATOR_C2S:
                        HandleSetIndicator(msg);
                        break;
                    case MsgType.SET_CAMERA_FOCUS_C2S:
                        HandleSetCameraFocus(msg);
                        break;
                    case MsgType.RESET_CAR_C2S:
                        HandleResetCar(msg);
                        break;
                    default:
                        Debug.LogWarning($"[ServerRouter] Unknown message type: {msg.msgType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerRouter] Error handling message {msg.msgType}: {ex.Message}");
            }
        }

        private void HandleHello(TcpMessage msg)
        {
            Debug.Log($"[ServerRouter] HandleHello called, payload length={msg.payloadLength}");
            int offset = 0;
            HelloC2S hello = Protocol.DeserializeHello(msg.payload, offset);
            Debug.Log($"[ServerRouter] Deserialized HELLO");

            // Validate token
            string receivedToken = Encoding.UTF8.GetString(hello.token).TrimEnd('\0');
            if (receivedToken != config.token)
            {
                Debug.LogWarning($"[ServerRouter] Invalid token: {receivedToken}");
                SendNotice(1, "Invalid token");
                return;
            }

            Debug.Log($"[ServerRouter] HELLO from {hello.clientName}, UDP port: {hello.clientUdpPort}");

            // Extract client IP from TCP connection
            if (tcpPeer.IsConnected)
            {
                var remoteEP = (IPEndPoint)((System.Net.Sockets.TcpClient)
                    typeof(TcpServerPeer)
                    .GetField("_client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(tcpPeer)).Client.RemoteEndPoint;

                udpPeer.SetClientEndpoint(remoteEP.Address, hello.clientUdpPort);
            }

            // Send WELCOME
            _sessionId = (uint)UnityEngine.Random.Range(1000, 9999);
            WelcomeS2C welcome = new WelcomeS2C
            {
                sessionId = _sessionId,
                simTickRate = (byte)config.simTickRate,
                carId = 1
            };

            int len = Protocol.SerializeWelcome(_sendBuffer, _sendSeq++, welcome);
            tcpPeer.SendMessage(SubArray(_sendBuffer, 0, len));

            Debug.Log($"[ServerRouter] Sent WELCOME, sessionId={_sessionId}");
        }

        private void HandleSetGear(TcpMessage msg)
        {
            int offset = 0;
            SetGearC2S gear = Protocol.DeserializeSetGear(msg.payload, offset);
            simController?.SetGear(gear.gear);
            Debug.Log($"[ServerRouter] Set gear: {gear.gear}");
        }

        private void HandleToggleHeadlights(TcpMessage msg)
        {
            int offset = 0;
            ToggleHeadlightsC2S lights = Protocol.DeserializeToggleHeadlights(msg.payload, offset);
            simController?.ToggleHeadlights(lights.on == 1);
            Debug.Log($"[ServerRouter] Toggle headlights: {lights.on}");
        }

        private void HandleSetIndicator(TcpMessage msg)
        {
            int offset = 0;
            SetIndicatorC2S indicator = Protocol.DeserializeSetIndicator(msg.payload, offset);
            simController?.SetIndicator((byte)indicator.mode);
            Debug.Log($"[ServerRouter] Set indicator: {indicator.mode}");
        }

        private void HandleSetCameraFocus(TcpMessage msg)
        {
            int offset = 0;
            SetCameraFocusC2S focus = Protocol.DeserializeSetCameraFocus(msg.payload, offset);
            cameraFocusManager?.SetFocus(focus.partId);
            Debug.Log($"[ServerRouter] Set camera focus: {focus.partId}");
        }

        private void HandleResetCar(TcpMessage msg)
        {
            simController?.ResetCar();
            Debug.Log($"[ServerRouter] Reset car");
        }

        private void SendNotice(byte code, string text)
        {
            ServerNoticeS2C notice = new ServerNoticeS2C { code = code, text = text };
            int len = Protocol.SerializeServerNotice(_sendBuffer, _sendSeq++, notice);
            tcpPeer.SendMessage(SubArray(_sendBuffer, 0, len));
        }

        private byte[] SubArray(byte[] data, int index, int length)
        {
            byte[] result = new byte[length];
            Buffer.BlockCopy(data, index, result, 0, length);
            return result;
        }
    }
}
