using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using CarSim.Shared;

namespace CarSim.Server
{
    public struct TcpMessage
    {
        public MsgType msgType;
        public ushort seq;
        public uint timestampMs;
        public byte[] payload;
        public int payloadLength;
    }

    public class TcpServerPeer : MonoBehaviour
    {
        public NetConfig config;

        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _acceptThread;
        private Thread _recvThread;
        private Thread _sendThread;
        private volatile bool _running;
        private bool _initialized;

        private RingBuffer<TcpMessage> _inboundQueue = new RingBuffer<TcpMessage>(128);
        private RingBuffer<byte[]> _outboundQueue = new RingBuffer<byte[]>(128);

        private byte[] _recvBuffer = new byte[Protocol.MAX_PACKET_SIZE];
        private byte[] _sendBuffer = new byte[Protocol.MAX_PACKET_SIZE];

        public bool IsConnected => _client != null && _client.Connected;

        public IPEndPoint GetClientEndpoint()
        {
            if (_client != null && _client.Connected)
            {
                return (IPEndPoint)_client.Client.RemoteEndPoint;
            }
            return null;
        }

        private void Start()
        {
            if (config == null)
            {
                Debug.LogError("TcpServerPeer: NetConfig not assigned!");
                return;
            }

            // Prevent multiple initializations
            if (_initialized)
            {
                Debug.LogWarning("[TcpServer] Already initialized, skipping Start()");
                return;
            }

            _running = true;
            _initialized = true;
            _acceptThread = new Thread(AcceptLoop) { IsBackground = true };
            _acceptThread.Start();
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
                _listener?.Stop();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TcpServer] Error stopping listener: {ex.Message}");
            }

            try
            {
                _stream?.Close();
            }
            catch { }

            try
            {
                _client?.Close();
            }
            catch { }

            if (_acceptThread != null && _acceptThread.IsAlive)
            {
                if (!_acceptThread.Join(1000))
                {
                    Debug.LogWarning("[TcpServer] Accept thread did not exit gracefully");
                }
            }

            if (_recvThread != null && _recvThread.IsAlive)
            {
                if (!_recvThread.Join(1000))
                {
                    Debug.LogWarning("[TcpServer] Receive thread did not exit gracefully");
                }
            }

            if (_sendThread != null && _sendThread.IsAlive)
            {
                if (!_sendThread.Join(1000))
                {
                    Debug.LogWarning("[TcpServer] Send thread did not exit gracefully");
                }
            }
        }

        private void AcceptLoop()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, config.tcpPort);
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Start();
                Debug.Log($"[TcpServer] Listening on port {config.tcpPort}");

                while (_running)
                {
                    if (_listener.Pending())
                    {
                        _client = _listener.AcceptTcpClient();
                        _stream = _client.GetStream();
                        Debug.Log($"[TcpServer] Client connected: {_client.Client.RemoteEndPoint}");

                        _recvThread = new Thread(RecvLoop) { IsBackground = true };
                        _recvThread.Start();

                        _sendThread = new Thread(SendLoop) { IsBackground = true };
                        _sendThread.Start();
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            catch (SocketException ex)
            {
                if (_running)
                {
                    Debug.LogError($"[TcpServer] Failed to bind to port {config.tcpPort}: {ex.Message}");
                    Debug.LogError($"[TcpServer] Make sure no other instance is using this port. Try stopping play mode completely and restarting.");
                }
            }
            catch (Exception ex)
            {
                if (_running)
                {
                    Debug.LogError($"[TcpServer] AcceptLoop error: {ex.Message}");
                }
            }
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
                            Debug.LogWarning("[TcpServer] Client disconnected (header)");
                            return;
                        }
                        headerRead += n;
                    }

                    int offset = 0;
                    ByteCodec.ReadHeader(_recvBuffer, ref offset, out MsgType msgType, out ushort seq, out uint ts, out ushort payloadLength);

                    // Use payload length from header
                    int payloadSize = payloadLength;
                    if (payloadSize > 0)
                    {
                        int payloadRead = 0;
                        while (payloadRead < payloadSize)
                        {
                            int n = _stream.Read(_recvBuffer, ByteCodec.HEADER_SIZE + payloadRead, payloadSize - payloadRead);
                            if (n <= 0)
                            {
                                Debug.LogWarning("[TcpServer] Client disconnected (payload)");
                                return;
                            }
                            payloadRead += n;
                        }
                    }

                    TcpMessage msg = new TcpMessage
                    {
                        msgType = msgType,
                        seq = seq,
                        timestampMs = ts,
                        payload = new byte[payloadSize],
                        payloadLength = payloadSize
                    };

                    if (payloadSize > 0)
                    {
                        Buffer.BlockCopy(_recvBuffer, ByteCodec.HEADER_SIZE, msg.payload, 0, payloadSize);
                    }

                    _inboundQueue.TryEnqueue(msg);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TcpServer] RecvLoop error: {ex.Message}");
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
                Debug.LogError($"[TcpServer] SendLoop error: {ex.Message}");
            }
        }

        public bool TryDequeueMessage(out TcpMessage msg)
        {
            return _inboundQueue.TryDequeue(out msg);
        }

        public void SendMessage(byte[] packet)
        {
            if (!IsConnected) return;

            byte[] copy = new byte[packet.Length];
            Buffer.BlockCopy(packet, 0, copy, 0, packet.Length);
            _outboundQueue.TryEnqueue(copy);
        }

        private int GetPayloadSize(MsgType msgType)
        {
            switch (msgType)
            {
                case MsgType.HELLO_C2S:
                    return 200; // Fixed token + port + name (max estimate)
                case MsgType.SET_GEAR_C2S:
                    return 1;
                case MsgType.TOGGLE_HEADLIGHTS_C2S:
                    return 1;
                case MsgType.SET_INDICATOR_C2S:
                    return 1;
                case MsgType.SET_CAMERA_FOCUS_C2S:
                    return 1;
                case MsgType.RESET_CAR_C2S:
                    return 0;
                default:
                    return 0;
            }
        }
    }
}
