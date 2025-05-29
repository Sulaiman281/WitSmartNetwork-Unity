using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace WitNetwork.Server
{
    public abstract class ATcpServer
    {
        public abstract int Port { get; }
        public abstract string ServerAddress { get; }

        private TcpListener _listener;
        private Thread _serverThread;
        private readonly ConcurrentDictionary<uint, TcpClientHandle> _clients = new(); // Use TcpClientHandle
        private readonly ConcurrentQueue<Action> _receiveEvents = new();
        private volatile bool _isRunning = false;
        private uint _successfulConnections = 0;
        public int TotalClients => _clients.Count;
        public bool IsRunning => _isRunning;

        // grouping the clients
        private readonly ConcurrentDictionary<int, List<uint>> _clientGroups = new(); // Maps group ID to list of client IDs
        private readonly ConcurrentDictionary<uint, int> _clientGroupIds = new(); // Maps client ID to group ID

        public void StartServer()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Parse(ServerAddress), Port);
                _listener.Start();
                _isRunning = true;
                _serverThread = new Thread(HandleIncomingConnections);
                _serverThread.Start();
                OnServerStarted();
            }
            catch (Exception ex)
            {
                OnServerFailed(ex);
            }
        }

        public void StopServer()
        {
            _isRunning = false;
            foreach (var client in _clients.Values)
            {
                client.Close();
            }
            _clients.Clear();
            _listener?.Stop();
            OnServerStopped();
        }

        private void HandleIncomingConnections()
        {
            try
            {
                while (_isRunning)
                {
                    if (_listener != null && _listener.Pending())
                    {
                        var tcpClient = _listener.AcceptTcpClient();
                        AddClient(tcpClient);
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                OnServerFailed(ex);
            }
            finally
            {
                StopServer();
            }
        }

        private void AddClient(TcpClient tcpClient)
        {
            uint id = ++_successfulConnections;
            var clientHandle = new TcpClientHandle(tcpClient);
            _clients.TryAdd(id, clientHandle);

            // Listen for messages in a background task
            Task.Run(() => HandleClient(id, clientHandle));
            QueueReceiveEvent(() => OnClientConnected(id));
        }

        private async Task HandleClient(uint clientId, TcpClientHandle clientHandle)
        {
            try
            {
                while (true)
                {
                    // Process received messages
                    while (clientHandle.ReceiveMessages.TryDequeue(out var message))
                    {
                        if (message.Contains("groupId"))
                        {
                            // Handle groupId message
                            string[] parts = message.Split(' ');
                            if (parts.Length > 1 && int.TryParse(parts[1], out int groupId))
                            {
                                AssignClientToGroup(clientId, groupId);
                            }

                        }
                        else
                        {
                            QueueReceiveEvent(() => OnMessageReceived(clientId, message));
                        }
                    }

                    // Handle disconnects
                    if (clientHandle.IsDisconnected.TryDequeue(out var _))
                    {
                        break;
                    }

                    await Task.Delay(10);
                }
            }
            catch
            {
                // Ignore client disconnect exceptions
            }
            finally
            {
                clientHandle.Close();
                _clients.TryRemove(clientId, out _);
                QueueReceiveEvent(() => OnClientDisconnected(clientId));
            }
        }

        private void AssignClientToGroup(uint clientId, int groupId)
        {
            if (groupId <= 0) return;
            if (!_clientGroups.ContainsKey(groupId))
            {
                _clientGroups[groupId] = new List<uint>();
            }

            if (_clients.TryGetValue(clientId, out var clientHandle))
            {
                _clientGroups[groupId].Add(clientId);
                _clientGroupIds[clientId] = groupId;
            }
        }

        public int GetClientGroupId(uint clientId)
        {
            return _clientGroupIds.TryGetValue(clientId, out var groupId) ? groupId : -1;
        }

        private void RemoveClientFromGroup(uint clientId)
        {
            if (_clientGroupIds.TryRemove(clientId, out var groupId) && _clientGroups.TryGetValue(groupId, out var clientList))
            {
                clientList.Remove(clientId);
                if (clientList.Count == 0)
                {
                    _clientGroups.TryRemove(groupId, out _);
                }
            }
        }

        private void QueueReceiveEvent(Action action)
        {
            _receiveEvents.Enqueue(action);
        }

        public virtual void Update()
        {
            while (_receiveEvents.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        public void SendMessageToGroup(int groupId, string message)
        {
            if (_clientGroups.TryGetValue(groupId, out var clientIds))
            {
                foreach (var clientId in clientIds)
                {
                    SendMessageToClient(clientId, message);
                }
            }
        }

        public void SendMessageToAllClients(string message)
        {
            foreach (var kvp in _clients)
            {
                SendMessageToClient(kvp.Key, message);
            }
        }

        public void SendMessageToAllClientsExcept(uint excludedClientId, string message)
        {
            foreach (var kvp in _clients)
            {
                if (kvp.Value.GroupId > 0) // Only send to clients not in a group
                    continue;
                if (kvp.Key != excludedClientId)
                {
                    SendMessageToClient(kvp.Key, message);
                }
            }
        }

        public void SendMessageToClient(uint clientId, string message)
        {
            if (_clients.TryGetValue(clientId, out var clientHandle))
            {
                clientHandle.SendMessage(message);
            }
        }

        public void DisconnectClient(uint clientId)
        {
            if (_clients.TryRemove(clientId, out var clientHandle))
            {
                clientHandle.Close();
                QueueReceiveEvent(() => OnClientDisconnected(clientId));
            }

            RemoveClientFromGroup(clientId);
        }

        public void Dispose()
        {
            StopServer();
        }

        // Event hooks
        protected abstract void OnServerStarted();
        protected abstract void OnClientConnected(uint clientId);
        protected abstract void OnClientDisconnected(uint clientId);
        protected abstract void OnServerStopped();
        protected abstract void OnServerFailed(Exception ex);
        protected abstract void OnMessageReceived(uint clientId, string message);
    }
}