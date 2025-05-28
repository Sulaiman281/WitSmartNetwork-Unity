using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace WitSmartNetwork.Server
{
    public class MyTcpServer : ATcpServer
    {
        private int _port;
        private string _address;

        // Track last pong time for each client
        private readonly ConcurrentDictionary<uint, DateTime> _lastPongTimes = new();
        private DateTime _lastPingSent = DateTime.MinValue;
        private UdpIpListener _udpListener;

        public MyTcpServer(int port, string address)
        {
            _port = port;
            _address = address;
        }

        public override int Port => _port;
        public override string ServerAddress => _address;

        private Settings Settings => Settings.Instance;

        // Optionally, allow changing configuration before starting
        public void SetPort(int port) => _port = port;
        public void SetAddress(string address) => _address = address;

        // Example event hooks
        protected override void OnServerStarted()
        {
            NetworkEventManager.Instance.OnServerStarted.Invoke();

            // open the udp listener if server mode is local
            if (Settings.ServerMode == ServerMode.Local)
            {
                _udpListener = new UdpIpListener(Port);
                _udpListener.Start();
            }
        }

        protected override void OnClientConnected(uint clientId)
        {
            NetworkEventManager.Instance.OnClientConnected.Invoke(clientId);
            _lastPongTimes[clientId] = DateTime.UtcNow;
        }

        protected override void OnClientDisconnected(uint clientId)
        {
            NetworkEventManager.Instance.OnClientDisconnected.Invoke(clientId);
            _lastPongTimes.TryRemove(clientId, out _);
        }

        protected override void OnServerStopped()
        {
            NetworkEventManager.Instance.OnServerStopped.Invoke();
        }

        protected override void OnServerFailed(Exception ex)
        {
            NetworkEventManager.Instance.OnServerFailed.Invoke(ex);
        }

        protected override void OnMessageReceived(uint clientId, string message)
        {
            _lastPongTimes[clientId] = DateTime.UtcNow; // Update last pong time
            if (string.Equals(message, "pong", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            Logger.Log($"Message received from client {clientId}: {message}");
            NetworkEventManager.Instance.OnServerMessageReceived.Invoke(clientId, message);
            var groupId = GetClientGroupId(clientId);
            if (groupId <= 0)
            {
                SendMessageToAllClientsExcept(clientId, message);
            }
            else
            {
                SendMessageToGroup(groupId, message);
            }
        }

        public override void Update()
        {
            base.Update();

            var now = DateTime.UtcNow;

            // Send ping at interval
            if ((now - _lastPingSent).TotalSeconds >= Settings.Instance.PingIntervalSeconds)
            {
                foreach (var clientId in GetClientIds())
                {
                    SendMessageToClient(clientId, "ping");
                }
                _lastPingSent = now;
            }

            // Check for pong timeouts
            foreach (var clientId in GetClientIds())
            {
                if (_lastPongTimes.TryGetValue(clientId, out var lastPong))
                {
                    if ((now - lastPong).TotalSeconds > Settings.Instance.PingIntervalSeconds * Settings.Instance.PingTimeoutIntervals)
                    {
                        Console.WriteLine($"Client {clientId} did not respond to ping. Disconnecting.");
                        DisconnectClient(clientId);
                        _lastPongTimes.TryRemove(clientId, out _);
                    }
                }
            }
        }

        // Helper to get all connected client IDs
        private IEnumerable<uint> GetClientIds()
        {
            // _clients is private in ATcpServer, so add a protected getter or expose as needed
            return Enumerable.Range(1, TotalClients).Select(i => (uint)i);
        }
    }
}