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
                    Debug.LogError($"[TcpClient] Main thread callback error: {ex.Message}");
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
            if (_running) return;

            try
            {
                _running = true;
                _client = new TcpClient();
                _client.Connect(serverIp, config.tcpPort);

                // Set socket options for better connection stability
                _client.NoDelay = true; // Disable Nagle's algorithm for low latency
                _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                // Set longer timeouts to prevent premature disconnections
                _client.ReceiveTimeout = 30000; // 30 seconds
                _client.SendTimeout = 30000;

                _stream = _client.GetStream();

                Debug.Log($"[TcpClient] Connected to {serverIp}:{config.tcpPort}");

                _recvThread = new Thread(RecvLoop) { IsBackground = true };
                _recvThread.Start();

                _sendThread = new Thread(SendLoop) { IsBackground = true };
                _sendThread.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TcpClient] Connection failed: {ex.Message}");
                _running = false;
            }
        }

        public void Disconnect()
        {
            _running = false;
            try
            {
                _stream?.Close();
                _client?.Close();
            }
            catch { }

            // Increase timeout for proper thread cleanup on Android
            _recvThread?.Join(2000);
            _sendThread?.Join(2000);

            Debug.Log("[TcpClient] Disconnected");
        }

        private void RecvLoop()
        {
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
                            Debug.LogWarning("[TcpClient] Server disconnected (header)");
                            _mainThreadActions.TryEnqueue(() => OnDisconnected?.Invoke());
                            return;
                        }
                        headerRead += n;
                    }

                    int offset = 0;
                    ByteCodec.ReadHeader(_recvBuffer, ref offset, out MsgType msgType, out ushort seq, out uint ts, out ushort payloadLength);

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
                                Debug.LogWarning("[TcpClient] Server disconnected (payload)");
                                _mainThreadActions.TryEnqueue(() => OnDisconnected?.Invoke());
                                return;
                            }
                            payloadRead += n;
                        }
                    }

                    // Process message on main thread
                    ProcessMessage(msgType, seq, ts, _recvBuffer, ByteCodec.HEADER_SIZE);
                }
            }
            catch (Exception ex)
            {
                if (_running)
                {
                    Debug.LogError($"[TcpClient] RecvLoop error: {ex.Message}");
                    _mainThreadActions.TryEnqueue(() => OnDisconnected?.Invoke());
                }
            }
        }

        private void SendLoop()
        {
            try
            {
                while (_running && _client != null && _client.Connected)
                {
                    if (_outboundQueue.TryDequeue(out byte[] packet))
                    {
                        _stream.Write(packet, 0, packet.Length);
                        _stream.Flush();
                    }
                    else
                    {
                        Thread.Sleep(5);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_running)
                {
                    Debug.LogError($"[TcpClient] SendLoop error: {ex.Message}");
                }
            }
        }

        private void ProcessMessage(MsgType msgType, ushort seq, uint ts, byte[] buffer, int offset)
        {
            switch (msgType)
            {
                case MsgType.WELCOME_S2C:
                    WelcomeS2C welcome = Protocol.DeserializeWelcome(buffer, offset);
                    _mainThreadActions.TryEnqueue(() => OnWelcome?.Invoke(welcome));
                    break;
                case MsgType.SERVER_NOTICE_S2C:
                    ServerNoticeS2C notice = Protocol.DeserializeServerNotice(buffer, offset);
                    _mainThreadActions.TryEnqueue(() => OnNotice?.Invoke(notice));
                    break;
                case MsgType.PONG_S2C:
                    // Received pong response - connection is alive
                    Debug.Log("[TcpClient] Received PONG from server");
                    break;
            }
        }

        public void SendMessage(byte[] packet)
        {
            if (!IsConnected) return;

            byte[] copy = new byte[packet.Length];
            Buffer.BlockCopy(packet, 0, copy, 0, packet.Length);
            _outboundQueue.TryEnqueue(copy);
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
