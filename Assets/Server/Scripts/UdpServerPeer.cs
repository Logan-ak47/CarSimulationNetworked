using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using CarSim.Shared;

namespace CarSim.Server
{
    public class UdpServerPeer : MonoBehaviour
    {
        public NetConfig config;

        [Header("Debug")]
        [Range(0, 100)]
        public int simulateDropPercent = 0;

        private UdpClient _socket;
        private Thread _recvThread;
        private volatile bool _running;
        private bool _initialized;
        private int _instanceId;

        private IPEndPoint _clientEndpoint;
        private bool _hasClientEndpoint;

        // Input queue to buffer all incoming inputs
        private struct QueuedInput
        {
            public InputC2S input;
            public ushort seq;
            public uint timestamp;
        }
        private RingBuffer<QueuedInput> _inputQueue = new RingBuffer<QueuedInput>(128);

        private byte[] _recvBuffer = new byte[Protocol.MAX_PACKET_SIZE];
        private byte[] _sendBuffer = new byte[Protocol.MAX_PACKET_SIZE];

        private System.Random _random = new System.Random();

        private void Start()
        {
            if (config == null)
            {
                Debug.LogError("UdpServerPeer: NetConfig not assigned!");
                return;
            }

            // Prevent multiple initializations
            if (_initialized)
            {
                Debug.LogWarning("[UdpServer] Already initialized, skipping Start()");
                return;
            }

            try
            {
                _running = true;
                _instanceId = GetInstanceID(); // Store instance ID for thread-safe access

                // Create socket with reuse address option to avoid "address already in use" errors
                _socket = new UdpClient();
                _socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
                _socket.Client.Bind(new IPEndPoint(IPAddress.Any, config.udpPortServer));

                _recvThread = new Thread(RecvLoop) { IsBackground = true };
                _recvThread.Start();

                _initialized = true;
                Debug.Log($"[UdpServer] {gameObject.name} (Instance {_instanceId}) listening on port {config.udpPortServer}");
            }
            catch (SocketException ex)
            {
                Debug.LogError($"[UdpServer] Failed to bind to port {config.udpPortServer}: {ex.Message}");
                Debug.LogError($"[UdpServer] Make sure no other instance is using this port. Try stopping play mode completely and restarting.");
                Cleanup();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UdpServer] Start error: {ex.Message}");
                Cleanup();
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            _running = false;
            _initialized = false;

            try
            {
                _socket?.Close();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UdpServer] Error closing socket: {ex.Message}");
            }

            if (_recvThread != null && _recvThread.IsAlive)
            {
                if (!_recvThread.Join(1000))
                {
                    Debug.LogWarning("[UdpServer] Receive thread did not exit gracefully");
                }
            }
        }

        private void RecvLoop()
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (_running)
                {
                    byte[] data = _socket.Receive(ref remoteEP);

                    // Simulate packet drop
                    if (simulateDropPercent > 0 && _random.Next(100) < simulateDropPercent)
                    {
                        continue;
                    }

                    if (data.Length < ByteCodec.HEADER_SIZE) continue;

                    int offset = 0;
                    ByteCodec.ReadHeader(data, ref offset, out MsgType msgType, out ushort seq, out uint ts, out ushort payloadLength);

                    if (msgType == MsgType.INPUT_C2S)
                    {
                        InputC2S input = Protocol.DeserializeInput(data, offset);

                        // Enqueue the input for processing on main thread
                        QueuedInput queuedInput = new QueuedInput
                        {
                            input = input,
                            seq = seq,
                            timestamp = ts
                        };

                        if (_inputQueue.TryEnqueue(queuedInput))
                        {
                            // Debug log
                            if (seq < 5 || seq % 100 == 0)
                            {
                                Debug.Log($"[UdpServer] Instance {_instanceId} queued input seq={seq}: throttle={input.throttle:F2}, queue size={_inputQueue.Count}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[UdpServer] Input queue full! Dropping packet seq={seq}");
                        }

                        // Learn client endpoint from first input
                        if (!_hasClientEndpoint)
                        {
                            _clientEndpoint = remoteEP;
                            _hasClientEndpoint = true;
                            Debug.Log($"[UdpServer] Client endpoint learned: {_clientEndpoint}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_running)
                {
                    Debug.LogError($"[UdpServer] RecvLoop error: {ex.Message}");
                }
            }
        }

        public bool TryGetLatestInput(out InputC2S input, out ushort seq, out uint timestamp)
        {
            // Dequeue one input from the queue
            if (_inputQueue.TryDequeue(out QueuedInput queuedInput))
            {
                input = queuedInput.input;
                seq = queuedInput.seq;
                timestamp = queuedInput.timestamp;
                return true;
            }

            input = default;
            seq = 0;
            timestamp = 0;
            return false;
        }

        public void SetClientEndpoint(IPAddress address, int port)
        {
            _clientEndpoint = new IPEndPoint(address, port);
            _hasClientEndpoint = true;
            Debug.Log($"[UdpServer] Client endpoint set: {_clientEndpoint}");
        }

        public void Send(byte[] data, int length)
        {
            if (!_hasClientEndpoint) return;

            try
            {
                _socket.Send(data, length, _clientEndpoint);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UdpServer] Send error: {ex.Message}");
            }
        }
    }
}
