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
            Debug.Log($"[CarSimulatorClient] UDP StartClient() called with serverIp: {serverIp}");

            if (_running)
            {
                Debug.LogWarning("[CarSimulatorClient] UDP client already running, ignoring start request");
                return;
            }

            try
            {
                Debug.Log("[CarSimulatorClient] Setting UDP _running = true");
                _running = true;

                Debug.Log($"[CarSimulatorClient] Creating UDP socket on port {config.udpPortClientListen}...");
                _socket = new UdpClient(config.udpPortClientListen);
                Debug.Log($"[CarSimulatorClient] UDP socket created and bound to port {config.udpPortClientListen}");

                Debug.Log($"[CarSimulatorClient] Setting server endpoint to {serverIp}:{config.udpPortServer}");
                _serverEndpoint = new IPEndPoint(IPAddress.Parse(serverIp), config.udpPortServer);
                _hasServerEndpoint = true;
                Debug.Log($"[CarSimulatorClient] Server endpoint configured: {_serverEndpoint}");

                Debug.Log("[CarSimulatorClient] Starting UDP RecvLoop thread...");
                _recvThread = new Thread(RecvLoop) { IsBackground = true };
                _recvThread.Start();
                Debug.Log("[CarSimulatorClient] UDP RecvLoop thread started");

                Debug.Log($"[CarSimulatorClient] UDP Client started successfully - Listening on port {config.udpPortClientListen}, sending to {_serverEndpoint}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CarSimulatorClient] UDP Start FAILED!");
                Debug.LogError($"[CarSimulatorClient] Exception Type: {ex.GetType().Name}");
                Debug.LogError($"[CarSimulatorClient] Exception Message: {ex.Message}");
                Debug.LogError($"[CarSimulatorClient] Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.LogError($"[CarSimulatorClient] Inner Exception: {ex.InnerException.Message}");
                }
            }
        }

        public void Stop()
        {
            Debug.Log("[CarSimulatorClient] UDP Stop() called");
            _running = false;
            _socket?.Close();
            Debug.Log("[CarSimulatorClient] UDP socket closed");

            // Increase timeout for proper thread cleanup on Android
            Debug.Log("[CarSimulatorClient] Waiting for UDP thread to finish...");
            _recvThread?.Join(2000);
            Debug.Log("[CarSimulatorClient] UDP stopped cleanly");
        }

        private void RecvLoop()
        {
            Debug.Log("[CarSimulatorClient] UDP RecvLoop thread started");
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                int packetCount = 0;
                while (_running)
                {
                    byte[] data = _socket.Receive(ref remoteEP);
                    packetCount++;

                    if (packetCount == 1)
                    {
                        Debug.Log($"[CarSimulatorClient] UDP First packet received from {remoteEP}, length: {data.Length} bytes");
                    }

                    if (data.Length < ByteCodec.HEADER_SIZE)
                    {
                        Debug.LogWarning($"[CarSimulatorClient] UDP Packet too small: {data.Length} bytes, expected at least {ByteCodec.HEADER_SIZE}");
                        continue;
                    }

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

                        if (packetCount == 1)
                        {
                            Debug.Log($"[CarSimulatorClient] UDP First STATE_S2C packet processed - RTT: {rtt}ms");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[CarSimulatorClient] UDP Received unexpected message type: {msgType}");
                    }
                }
                Debug.Log($"[CarSimulatorClient] UDP RecvLoop exited normally after receiving {packetCount} packets");
            }
            catch (Exception ex)
            {
                if (_running)
                {
                    Debug.LogError($"[CarSimulatorClient] UDP RecvLoop EXCEPTION!");
                    Debug.LogError($"[CarSimulatorClient] Exception Type: {ex.GetType().Name}");
                    Debug.LogError($"[CarSimulatorClient] Exception Message: {ex.Message}");
                    Debug.LogError($"[CarSimulatorClient] Stack Trace: {ex.StackTrace}");
                }
                else
                {
                    Debug.Log("[CarSimulatorClient] UDP RecvLoop exception during shutdown (expected)");
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
            if (!_hasServerEndpoint)
            {
                Debug.LogWarning("[CarSimulatorClient] UDP Send called but no server endpoint configured!");
                return;
            }

            try
            {
                _socket.Send(data, length, _serverEndpoint);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CarSimulatorClient] UDP Send error: {ex.GetType().Name} - {ex.Message}");
            }
        }
    }
}
