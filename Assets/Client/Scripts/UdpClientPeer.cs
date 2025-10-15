using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using CarSim.Shared;

namespace CarSim.Client
{
    public class UdpClientPeer : MonoBehaviour
    {
        public NetConfig config;

        private UdpClient _socket;
        private Thread _recvThread;
        private volatile bool _running;

        private IPEndPoint _serverEndpoint;
        private bool _hasServerEndpoint;

        private StateS2C _latestState;
        private uint _latestStateTimestamp;
        private readonly object _stateLock = new object();

        private byte[] _recvBuffer = new byte[Protocol.MAX_PACKET_SIZE];

        public float LastPingMs { get; private set; }

        private void OnDestroy()
        {
            Stop();
        }

        public void StartClient(string serverIp)
        {
            if (_running) return;

            try
            {
                _running = true;
                _socket = new UdpClient(config.udpPortClientListen);
                _serverEndpoint = new IPEndPoint(IPAddress.Parse(serverIp), config.udpPortServer);
                _hasServerEndpoint = true;

                _recvThread = new Thread(RecvLoop) { IsBackground = true };
                _recvThread.Start();

                Debug.Log($"[UdpClient] Listening on port {config.udpPortClientListen}, sending to {_serverEndpoint}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UdpClient] Start error: {ex.Message}");
            }
        }

        public void Stop()
        {
            _running = false;
            _socket?.Close();
            _recvThread?.Join(500);
        }

        private void RecvLoop()
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (_running)
                {
                    byte[] data = _socket.Receive(ref remoteEP);

                    if (data.Length < ByteCodec.HEADER_SIZE) continue;

                    int offset = 0;
                    ByteCodec.ReadHeader(data, ref offset, out MsgType msgType, out ushort seq, out uint ts, out ushort payloadLength);

                    if (msgType == MsgType.STATE_S2C)
                    {
                        StateS2C state = Protocol.DeserializeState(data, offset);

                        uint now = StopwatchTime.TimestampMs();
                        float rtt = (now - ts);

                        lock (_stateLock)
                        {
                            _latestState = state;
                            _latestStateTimestamp = ts;
                            LastPingMs = rtt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_running)
                {
                    Debug.LogError($"[UdpClient] RecvLoop error: {ex.Message}");
                }
            }
        }

        public bool TryGetLatestState(out StateS2C state)
        {
            lock (_stateLock)
            {
                if (_latestStateTimestamp > 0)
                {
                    state = _latestState;
                    return true;
                }
            }

            state = default;
            return false;
        }

        public void Send(byte[] data, int length)
        {
            if (!_hasServerEndpoint) return;

            try
            {
                _socket.Send(data, length, _serverEndpoint);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UdpClient] Send error: {ex.Message}");
            }
        }
    }
}
