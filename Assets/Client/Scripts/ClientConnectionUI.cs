using UnityEngine;
using UnityEngine.UI;
using System.Text;
using CarSim.Shared;
using System;
using JetBrains.Annotations;

namespace CarSim.Client
{
    public class ClientConnectionUI : MonoBehaviour
    {
        [Header("Config")]
        public NetConfig config;

        [Header("Peers")]
        public TcpClientPeer tcpPeer;
        public UdpClientPeer udpPeer;

        [Header("UI - Connect Panel")]
        public GameObject panelConnect;
        public InputField inputServerIP;
        public InputField inputToken;
        public Button buttonConnect;
        public Text textStatus;

        [Header("UI - Drive Panel")]
        public GameObject panelDrive;

        private ushort _sendSeq = 0;
        private byte[] _sendBuffer = new byte[Protocol.MAX_PACKET_SIZE];

        private void Start()
        {
            Debug.Log("Starting the ClientConnectionUI...");
            if (config == null)
            {
                Debug.LogError("ClientConnectionUI: NetConfig not assigned!");
                return;
            }

            // Set defaults
            if (inputToken != null)
            {
                inputToken.text = config.token;
            }

            if (buttonConnect != null)
            {
                buttonConnect.onClick.AddListener(OnConnectClicked);
            }

            // Subscribe to events
            if (tcpPeer != null)
            {
                tcpPeer.OnWelcome += OnWelcome;
                tcpPeer.OnNotice += OnNotice;
                tcpPeer.OnDisconnected += OnDisconnected;
            }

            // Show connect panel
            if (panelConnect != null) panelConnect.SetActive(true);
            if (panelDrive != null) panelDrive.SetActive(false);
        }

        private void OnDestroy()
        {
            if (tcpPeer != null)
            {
                tcpPeer.OnWelcome -= OnWelcome;
                tcpPeer.OnNotice -= OnNotice;
                tcpPeer.OnDisconnected -= OnDisconnected;
            }
        }

        [PublicAPI]
       public void OnConnectClicked()
        {
            Debug.Log("[ClientUI] Connect clicked");
            if (inputServerIP == null || inputToken == null)
            {
                SetStatus("UI not configured!");
                return;
            }

            string serverIP = inputServerIP.text.Trim();
            string token = inputToken.text.Trim();

            if (string.IsNullOrEmpty(serverIP))
            {
                SetStatus("Enter server IP!");
                return;
            }

            SetStatus("Connecting...");
            buttonConnect.interactable = false;

            // Connect TCP
            tcpPeer.Connect(serverIP);

            // Send HELLO
            byte[] tokenBytes = new byte[HelloC2S.TOKEN_SIZE];
            byte[] tokenUtf8 = Encoding.UTF8.GetBytes(token);
            Buffer.BlockCopy(tokenUtf8, 0, tokenBytes, 0, Mathf.Min(tokenUtf8.Length, HelloC2S.TOKEN_SIZE));

            HelloC2S hello = new HelloC2S
            {
                token = tokenBytes,
                clientUdpPort = (ushort)config.udpPortClientListen,
                clientName = SystemInfo.deviceName
            };

            int len = Protocol.SerializeHello(_sendBuffer, _sendSeq++, hello);
            tcpPeer.SendMessage(SubArray(_sendBuffer, 0, len));

            // Start UDP
            udpPeer.StartClient(serverIP);
        }

        private void OnWelcome(WelcomeS2C welcome)
        {
            Debug.Log($"[ClientUI] WELCOME: sessionId={welcome.sessionId}, tickRate={welcome.simTickRate}");
            SetStatus($"Connected! Session: {welcome.sessionId}");

            // Show drive panel
            if (panelConnect != null) panelConnect.SetActive(false);
            if (panelDrive != null) panelDrive.SetActive(true);
        }

        private void OnNotice(ServerNoticeS2C notice)
        {
            Debug.LogWarning($"[ClientUI] Server notice [{notice.code}]: {notice.text}");
            SetStatus($"Server: {notice.text}");
        }

        private void OnDisconnected()
        {
            Debug.LogWarning("[ClientUI] Disconnected from server");
            SetStatus("Disconnected!");
            buttonConnect.interactable = true;

            // Show connect panel
            if (panelConnect != null) panelConnect.SetActive(true);
            if (panelDrive != null) panelDrive.SetActive(false);
        }

        private void SetStatus(string text)
        {
            if (textStatus != null)
            {
                textStatus.text = text;
            }
        }

        private byte[] SubArray(byte[] data, int index, int length)
        {
            byte[] result = new byte[length];
            Buffer.BlockCopy(data, index, result, 0, length);
            return result;
        }
    }
}
