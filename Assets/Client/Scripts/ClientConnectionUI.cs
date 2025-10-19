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
            Debug.Log("[CarSimulatorClient] Starting the ClientConnectionUI...");
            Debug.Log($"[CarSimulatorClient] Device: {SystemInfo.deviceName}, OS: {SystemInfo.operatingSystem}");

            if (config == null)
            {
                Debug.LogError("[CarSimulatorClient] CRITICAL: NetConfig not assigned!");
                return;
            }

            Debug.Log($"[CarSimulatorClient] Config loaded - TCP Port: {config.tcpPort}, UDP Server Port: {config.udpPortServer}, UDP Client Port: {config.udpPortClientListen}");

            // Set defaults
            if (inputToken != null)
            {
                inputToken.text = config.token;
                Debug.Log($"[CarSimulatorClient] Default token set: {config.token}");
            }

            if (buttonConnect != null)
            {
                buttonConnect.onClick.AddListener(OnConnectClicked);
                Debug.Log("[CarSimulatorClient] Connect button listener registered");
            }

            // Subscribe to events
            if (tcpPeer != null)
            {
                tcpPeer.OnConnected += OnConnected;
                tcpPeer.OnWelcome += OnWelcome;
                tcpPeer.OnNotice += OnNotice;
                tcpPeer.OnDisconnected += OnDisconnected;
                tcpPeer.OnConnectionFailed += OnConnectionFailed;
                Debug.Log("[CarSimulatorClient] TCP peer events subscribed");
            }
            else
            {
                Debug.LogError("[CarSimulatorClient] TCP peer is NULL!");
            }

            if (udpPeer == null)
            {
                Debug.LogError("[CarSimulatorClient] UDP peer is NULL!");
            }

            // Show connect panel
            if (panelConnect != null) panelConnect.SetActive(true);
            if (panelDrive != null) panelDrive.SetActive(false);

            Debug.Log("[CarSimulatorClient] UI initialized, showing connect panel");
        }

        private void OnDestroy()
        {
            if (tcpPeer != null)
            {
                tcpPeer.OnConnected -= OnConnected;
                tcpPeer.OnWelcome -= OnWelcome;
                tcpPeer.OnNotice -= OnNotice;
                tcpPeer.OnDisconnected -= OnDisconnected;
                tcpPeer.OnConnectionFailed -= OnConnectionFailed;
            }
        }

        private string _pendingServerIP;
        private string _pendingToken;

        [PublicAPI]
       public void OnConnectClicked()
        {
            Debug.Log("[CarSimulatorClient] ========== CONNECT BUTTON CLICKED ==========");

            if (inputServerIP == null || inputToken == null)
            {
                Debug.LogError("[CarSimulatorClient] UI not configured - input fields are null!");
                SetStatus("UI not configured!");
                return;
            }

            string serverIP = inputServerIP.text.Trim();
            string token = inputToken.text.Trim();

            Debug.Log($"[CarSimulatorClient] Server IP entered: '{serverIP}'");
            Debug.Log($"[CarSimulatorClient] Token entered: '{token}'");

            if (string.IsNullOrEmpty(serverIP))
            {
                Debug.LogError("[CarSimulatorClient] Server IP is empty!");
                SetStatus("Enter server IP!");
                return;
            }

            // Store for use in OnConnected callback
            _pendingServerIP = serverIP;
            _pendingToken = token;

#if UNITY_ANDROID
            // Check for Internet permission on Android
            Debug.Log("[CarSimulatorClient] Running on Android platform");
            Debug.Log($"[CarSimulatorClient] Network reachability: {Application.internetReachability}");

            // Detailed network diagnostics
            try
            {
                System.Net.NetworkInformation.NetworkInterface[] interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                Debug.Log($"[CarSimulatorClient] Found {interfaces.Length} network interfaces:");
                foreach (var iface in interfaces)
                {
                    if (iface.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                    {
                        Debug.Log($"[CarSimulatorClient]   - {iface.Name}: {iface.NetworkInterfaceType}, Status: {iface.OperationalStatus}");
                        var ipProps = iface.GetIPProperties();
                        foreach (var addr in ipProps.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                Debug.Log($"[CarSimulatorClient]     IP: {addr.Address}");
                            }
                        }
                    }
                }

                // Try to ping the server
                Debug.Log($"[CarSimulatorClient] Testing reachability to {serverIP}...");
                System.Net.Sockets.Socket testSocket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                testSocket.Blocking = false;
                try
                {
                    testSocket.Connect(serverIP, config.tcpPort);
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    if (ex.SocketErrorCode == System.Net.Sockets.SocketError.WouldBlock ||
                        ex.SocketErrorCode == System.Net.Sockets.SocketError.InProgress)
                    {
                        Debug.Log($"[CarSimulatorClient] Non-blocking connect initiated (this is expected)");
                    }
                    else
                    {
                        Debug.LogWarning($"[CarSimulatorClient] Test connect failed: {ex.SocketErrorCode} - {ex.Message}");
                    }
                }
                finally
                {
                    testSocket.Close();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CarSimulatorClient] Network diagnostics failed: {ex.Message}");
            }
#endif

            SetStatus("Connecting...");
            buttonConnect.interactable = false;

            // Initiate TCP connection (async, will call OnConnected when ready)
            Debug.Log($"[CarSimulatorClient] Initiating async TCP connection to {serverIP}:{config.tcpPort}...");
            tcpPeer.Connect(serverIP);
            Debug.Log("[CarSimulatorClient] TCP Connect() method called, connection will complete asynchronously");
        }

        private void OnConnected()
        {
            Debug.Log("[CarSimulatorClient] ========== TCP CONNECTION ESTABLISHED ==========");
            SetStatus("Sending HELLO...");

            try
            {
                // Send HELLO
                byte[] tokenBytes = new byte[HelloC2S.TOKEN_SIZE];
                byte[] tokenUtf8 = Encoding.UTF8.GetBytes(_pendingToken);
                Buffer.BlockCopy(tokenUtf8, 0, tokenBytes, 0, Mathf.Min(tokenUtf8.Length, HelloC2S.TOKEN_SIZE));

                HelloC2S hello = new HelloC2S
                {
                    token = tokenBytes,
                    clientUdpPort = (ushort)config.udpPortClientListen,
                    clientName = SystemInfo.deviceName
                };

                Debug.Log($"[CarSimulatorClient] Preparing HELLO message - Client UDP Port: {config.udpPortClientListen}, Device Name: {SystemInfo.deviceName}");

                int len = Protocol.SerializeHello(_sendBuffer, _sendSeq++, hello);
                Debug.Log($"[CarSimulatorClient] HELLO message serialized, length: {len} bytes, sequence: {_sendSeq - 1}");

                tcpPeer.SendMessage(SubArray(_sendBuffer, 0, len));
                Debug.Log("[CarSimulatorClient] HELLO message sent via TCP");

                // Start UDP
                Debug.Log($"[CarSimulatorClient] Starting UDP client to {_pendingServerIP}:{config.udpPortServer}, listening on port {config.udpPortClientListen}");
                udpPeer.StartClient(_pendingServerIP);
                Debug.Log("[CarSimulatorClient] UDP StartClient() method called successfully");

                SetStatus("Waiting for WELCOME...");
                Debug.Log("[CarSimulatorClient] Connection initialization complete, waiting for server WELCOME response...");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CarSimulatorClient] EXCEPTION during HELLO send: {ex.GetType().Name} - {ex.Message}");
                Debug.LogError($"[CarSimulatorClient] Stack trace: {ex.StackTrace}");
                SetStatus($"Error: {ex.Message}");
                buttonConnect.interactable = true;
            }
        }

        private void OnWelcome(WelcomeS2C welcome)
        {
            Debug.Log($"[CarSimulatorClient] ========== WELCOME RECEIVED FROM SERVER ==========");
            Debug.Log($"[CarSimulatorClient] Session ID: {welcome.sessionId}");
            Debug.Log($"[CarSimulatorClient] Sim Tick Rate: {welcome.simTickRate}");
            Debug.Log($"[CarSimulatorClient] Connection SUCCESSFUL! Switching to drive panel...");

            SetStatus($"Connected! Session: {welcome.sessionId}");

            // Show drive panel
            if (panelConnect != null) panelConnect.SetActive(false);
            if (panelDrive != null) panelDrive.SetActive(true);

            Debug.Log("[CarSimulatorClient] Drive panel now active");
        }

        private void OnNotice(ServerNoticeS2C notice)
        {
            Debug.LogWarning($"[CarSimulatorClient] Server notice received - Code: [{notice.code}], Message: {notice.text}");
            SetStatus($"Server: {notice.text}");
        }

        private void OnDisconnected()
        {
            Debug.LogWarning("[CarSimulatorClient] ========== DISCONNECTED FROM SERVER ==========");
            Debug.LogWarning("[CarSimulatorClient] TCP connection lost, returning to connect screen");

            SetStatus("Disconnected!");
            buttonConnect.interactable = true;

            // Show connect panel
            if (panelConnect != null) panelConnect.SetActive(true);
            if (panelDrive != null) panelDrive.SetActive(false);

            Debug.Log("[CarSimulatorClient] Connect panel restored");
        }

        private void OnConnectionFailed(string errorMessage)
        {
            Debug.LogError($"[CarSimulatorClient] ========== CONNECTION FAILED ==========");
            Debug.LogError($"[CarSimulatorClient] Error: {errorMessage}");

            SetStatus($"Failed: {errorMessage}");
            buttonConnect.interactable = true;

            Debug.Log("[CarSimulatorClient] Re-enabled connect button after failure");
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
