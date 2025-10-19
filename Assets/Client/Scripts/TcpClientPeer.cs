using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;
using CarSim.Shared;

namespace CarSim.Client
{
    public class TcpClientPeer : MonoBehaviour
    {
        public NetConfig config;

        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _recvThread;
        private Thread _sendThread;
        private volatile bool _running;

        private RingBuffer<byte[]> _outboundQueue = new RingBuffer<byte[]>(128);
        private RingBuffer<Action> _mainThreadActions = new RingBuffer<Action>(128);
        private byte[] _recvBuffer = new byte[Protocol.MAX_PACKET_SIZE];

        // Heartbeat to keep connection alive
        private float _lastPingTime;
        private const float PING_INTERVAL = 3f; // Send ping every 3 seconds

        public event Action<WelcomeS2C> OnWelcome;
        public event Action<ServerNoticeS2C> OnNotice;
        public event Action OnDisconnected;
        public event Action<string> OnConnectionFailed;
        public event Action OnConnected;

        public bool IsConnected => _client != null && _client.Connected;

        private void Update()
        {
            // Process main thread callbacks
            while (_mainThreadActions.TryDequeue(out Action action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CarSimulatorClient] TCP Main thread callback error: {ex.Message}");
                }
            }

            // Send periodic ping to keep connection alive
            if (IsConnected && Time.time - _lastPingTime >= PING_INTERVAL)
            {
                _lastPingTime = Time.time;
                SendPing();
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public void Connect(string serverIp)
        {
            Debug.Log($"[CarSimulatorClient] TCP Connect() called with serverIp: {serverIp}");

            if (_running)
            {
                Debug.LogWarning("[CarSimulatorClient] TCP connection already running, ignoring connect request");
                return;
            }

            // Start connection in background thread to avoid blocking UI
            Debug.Log("[CarSimulatorClient] Starting connection thread...");
            Thread connectThread = new Thread(() => ConnectAsync(serverIp)) { IsBackground = true };
            connectThread.Start();
        }

        private void ConnectAsync(string serverIp)
        {
            try
            {
                Debug.Log("[CarSimulatorClient] Connection thread started");
                Debug.Log("[CarSimulatorClient] Setting _running = true");
                _running = true;

                Debug.Log("[CarSimulatorClient] Creating new TcpClient...");
                _client = new TcpClient();

                Debug.Log($"[CarSimulatorClient] Attempting async connection to {serverIp}:{config.tcpPort}...");
                Debug.Log($"[CarSimulatorClient] Connection timeout: 10 seconds");

                // Use BeginConnect with timeout instead of blocking Connect
                IAsyncResult result = _client.BeginConnect(serverIp, config.tcpPort, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10));

                if (!success)
                {
                    Debug.LogError($"[CarSimulatorClient] Connection TIMED OUT after 10 seconds!");
                    _client.Close();
                    _running = false;
                    _mainThreadActions.TryEnqueue(() => OnConnectionFailed?.Invoke("Connection timed out. Check if server is running and reachable."));
                    return;
                }

                _client.EndConnect(result);
                Debug.Log($"[CarSimulatorClient] TCP socket connected successfully!");

                // Check if we're still supposed to be running
                if (!_running)
                {
                    Debug.Log("[CarSimulatorClient] Connection cancelled");
                    _client.Close();
                    return;
                }

                // Set socket options for better connection stability
                Debug.Log("[CarSimulatorClient] Configuring socket options...");
                _client.NoDelay = true; // Disable Nagle's algorithm for low latency
                _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                // Set longer timeouts to prevent premature disconnections
                _client.ReceiveTimeout = 30000; // 30 seconds
                _client.SendTimeout = 30000;
                Debug.Log("[CarSimulatorClient] Socket configured: NoDelay=true, KeepAlive=true, Timeouts=30s");

                _stream = _client.GetStream();
                Debug.Log("[CarSimulatorClient] Network stream obtained");

                Debug.Log($"[CarSimulatorClient] TCP Connected to {serverIp}:{config.tcpPort}");

                Debug.Log("[CarSimulatorClient] Starting RecvLoop thread...");
                _recvThread = new Thread(RecvLoop) { IsBackground = true };
                _recvThread.Start();
                Debug.Log("[CarSimulatorClient] RecvLoop thread started");

                Debug.Log("[CarSimulatorClient] Starting SendLoop thread...");
                _sendThread = new Thread(SendLoop) { IsBackground = true };
                _sendThread.Start();
                Debug.Log("[CarSimulatorClient] SendLoop thread started");

                Debug.Log("[CarSimulatorClient] TCP connection fully established!");

                // Notify that connection is ready
                _mainThreadActions.TryEnqueue(() => OnConnected?.Invoke());
                Debug.Log("[CarSimulatorClient] OnConnected event enqueued");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CarSimulatorClient] TCP Connection FAILED!");
                Debug.LogError($"[CarSimulatorClient] Exception Type: {ex.GetType().Name}");
                Debug.LogError($"[CarSimulatorClient] Exception Message: {ex.Message}");
                Debug.LogError($"[CarSimulatorClient] Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.LogError($"[CarSimulatorClient] Inner Exception: {ex.InnerException.Message}");
                }
                _running = false;

                // Notify UI of failure
                string errorMsg = $"Connection failed: {ex.Message}";
                _mainThreadActions.TryEnqueue(() => OnConnectionFailed?.Invoke(errorMsg));
            }
        }

        public void Disconnect()
        {
            Debug.Log("[CarSimulatorClient] TCP Disconnect() called");
            _running = false;
            try
            {
                _stream?.Close();
                _client?.Close();
                Debug.Log("[CarSimulatorClient] TCP stream and client closed");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CarSimulatorClient] Exception during disconnect: {ex.Message}");
            }

            // Increase timeout for proper thread cleanup on Android
            Debug.Log("[CarSimulatorClient] Waiting for threads to finish...");
            _recvThread?.Join(2000);
            _sendThread?.Join(2000);

            Debug.Log("[CarSimulatorClient] TCP Disconnected cleanly");
        }

        private void RecvLoop()
        {
            Debug.Log("[CarSimulatorClient] TCP RecvLoop thread started");
            try
            {
                while (_running && _client != null && _client.Connected)
                {
                    // Read header
                    int headerRead = 0;
                    while (headerRead < ByteCodec.HEADER_SIZE)
                    {
                        int n = _stream.Read(_recvBuffer, headerRead, ByteCodec.HEADER_SIZE - headerRead);
                        if (n <= 0)
                        {
                            Debug.LogWarning("[CarSimulatorClient] TCP Server disconnected while reading header");
                            _mainThreadActions.TryEnqueue(() => OnDisconnected?.Invoke());
                            return;
                        }
                        headerRead += n;
                    }

                    int offset = 0;
                    ByteCodec.ReadHeader(_recvBuffer, ref offset, out MsgType msgType, out ushort seq, out uint ts, out ushort payloadLength);
                    Debug.Log($"[CarSimulatorClient] TCP Received message - Type: {msgType}, Seq: {seq}, PayloadLength: {payloadLength}");

                    // Read payload based on length from header
                    int payloadSize = payloadLength;
                    if (payloadSize > 0)
                    {
                        int payloadRead = 0;
                        while (payloadRead < payloadSize)
                        {
                            int n = _stream.Read(_recvBuffer, ByteCodec.HEADER_SIZE + payloadRead, payloadSize - payloadRead);
                            if (n <= 0)
                            {
                                Debug.LogWarning("[CarSimulatorClient] TCP Server disconnected while reading payload");
                                _mainThreadActions.TryEnqueue(() => OnDisconnected?.Invoke());
                                return;
                            }
                            payloadRead += n;
                        }
                        Debug.Log($"[CarSimulatorClient] TCP Payload received: {payloadSize} bytes");
                    }

                    // Process message on main thread
                    ProcessMessage(msgType, seq, ts, _recvBuffer, ByteCodec.HEADER_SIZE);
                }
                Debug.Log("[CarSimulatorClient] TCP RecvLoop exited normally");
            }
            catch (Exception ex)
            {
                if (_running)
                {
                    Debug.LogError($"[CarSimulatorClient] TCP RecvLoop EXCEPTION!");
                    Debug.LogError($"[CarSimulatorClient] Exception Type: {ex.GetType().Name}");
                    Debug.LogError($"[CarSimulatorClient] Exception Message: {ex.Message}");
                    Debug.LogError($"[CarSimulatorClient] Stack Trace: {ex.StackTrace}");
                    _mainThreadActions.TryEnqueue(() => OnDisconnected?.Invoke());
                }
                else
                {
                    Debug.Log("[CarSimulatorClient] TCP RecvLoop exception during shutdown (expected)");
                }
            }
        }

        private void SendLoop()
        {
            Debug.Log("[CarSimulatorClient] TCP SendLoop thread started");
            try
            {
                while (_running && _client != null && _client.Connected)
                {
                    if (_outboundQueue.TryDequeue(out byte[] packet))
                    {
                        Debug.Log($"[CarSimulatorClient] TCP Sending packet, length: {packet.Length} bytes");
                        _stream.Write(packet, 0, packet.Length);
                        _stream.Flush();
                        Debug.Log("[CarSimulatorClient] TCP Packet sent and flushed");
                    }
                    else
                    {
                        Thread.Sleep(5);
                    }
                }
                Debug.Log("[CarSimulatorClient] TCP SendLoop exited normally");
            }
            catch (Exception ex)
            {
                if (_running)
                {
                    Debug.LogError($"[CarSimulatorClient] TCP SendLoop EXCEPTION!");
                    Debug.LogError($"[CarSimulatorClient] Exception Type: {ex.GetType().Name}");
                    Debug.LogError($"[CarSimulatorClient] Exception Message: {ex.Message}");
                    Debug.LogError($"[CarSimulatorClient] Stack Trace: {ex.StackTrace}");
                }
                else
                {
                    Debug.Log("[CarSimulatorClient] TCP SendLoop exception during shutdown (expected)");
                }
            }
        }

        private void ProcessMessage(MsgType msgType, ushort seq, uint ts, byte[] buffer, int offset)
        {
            Debug.Log($"[CarSimulatorClient] TCP Processing message type: {msgType}");
            switch (msgType)
            {
                case MsgType.WELCOME_S2C:
                    Debug.Log("[CarSimulatorClient] TCP Deserializing WELCOME message...");
                    WelcomeS2C welcome = Protocol.DeserializeWelcome(buffer, offset);
                    Debug.Log($"[CarSimulatorClient] TCP WELCOME deserialized, enqueuing callback to main thread");
                    _mainThreadActions.TryEnqueue(() => OnWelcome?.Invoke(welcome));
                    break;
                case MsgType.SERVER_NOTICE_S2C:
                    Debug.Log("[CarSimulatorClient] TCP Deserializing SERVER_NOTICE message...");
                    ServerNoticeS2C notice = Protocol.DeserializeServerNotice(buffer, offset);
                    Debug.Log($"[CarSimulatorClient] TCP SERVER_NOTICE deserialized, enqueuing callback to main thread");
                    _mainThreadActions.TryEnqueue(() => OnNotice?.Invoke(notice));
                    break;
                case MsgType.PONG_S2C:
                    // Received pong response - connection is alive
                    Debug.Log("[CarSimulatorClient] TCP Received PONG from server (heartbeat OK)");
                    break;
                default:
                    Debug.LogWarning($"[CarSimulatorClient] TCP Unknown message type received: {msgType}");
                    break;
            }
        }

        public void SendMessage(byte[] packet)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[CarSimulatorClient] TCP SendMessage called but not connected!");
                return;
            }

            Debug.Log($"[CarSimulatorClient] TCP Enqueuing message for send, length: {packet.Length} bytes");
            byte[] copy = new byte[packet.Length];
            Buffer.BlockCopy(packet, 0, copy, 0, packet.Length);
            bool enqueued = _outboundQueue.TryEnqueue(copy);
            Debug.Log($"[CarSimulatorClient] TCP Message enqueue result: {enqueued}");
        }

        private void SendPing()
        {
            if (!IsConnected) return;

            byte[] buffer = new byte[ByteCodec.HEADER_SIZE];
            int offset = 0;
            ByteCodec.WriteHeader(buffer, ref offset, MsgType.PING_C2S, 0, (uint)(Time.time * 1000), 0);
            SendMessage(buffer);
        }
    }
}
