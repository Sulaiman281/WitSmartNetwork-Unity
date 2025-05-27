using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WitSmartNetwork.Client
{
    public abstract class UdpBroadcaster : IDisposable
    {
        private readonly float _broadcastWaitTimeSeconds;
        private readonly ConcurrentQueue<Action> _receiveEvents = new();
        private readonly ConcurrentDictionary<int, UdpClient> _clients = new();
        private readonly ConcurrentDictionary<int, CancellationTokenSource> _cancelTokens = new();

        protected UdpBroadcaster(float broadcastWaitTimeSeconds = 2f)
        {
            _broadcastWaitTimeSeconds = broadcastWaitTimeSeconds;
        }

        public UdpClient Client
        {
            get
            {
                if (_clients.TryGetValue(0, out UdpClient client))
                    return client;
                var newClient = new UdpClient();
                newClient.EnableBroadcast = true;
                _clients.TryAdd(0, newClient);
                return newClient;
            }
        }

        public void Update()
        {
            while (_receiveEvents.TryDequeue(out Action action))
            {
                try { action(); }
                catch (Exception e) { Logger.LogError($"[UDP] Action failed: {e}"); }
            }
        }

        public void Shutdown()
        {
            Client.Close();
            foreach (var cancelToken in _cancelTokens.Values)
                cancelToken.Cancel();
        }

        public void Dispose() => Shutdown();

        public void SendBroadcastMessage(ushort port, string message)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, port);
            SendMessage(message, remoteEndPoint);
        }

        private void SendMessage(string message, IPEndPoint remoteEndPoint)
        {
            try
            {
                var data = Encoding.UTF8.GetBytes(message);
                Client.Send(data, data.Length, remoteEndPoint);

                var cancelToken = new CancellationTokenSource();
                var thread = new Thread(() => WaitingForResponse(remoteEndPoint, cancelToken.Token));
                thread.Start();

                AddPortCancelToken(remoteEndPoint.Port, cancelToken);

                Task.Delay(TimeSpan.FromSeconds(_broadcastWaitTimeSeconds)).ContinueWith(_ =>
                {
                    cancelToken.Cancel();
                });
            }
            catch (Exception e)
            {
                Logger.LogError($"[UDP] Send error: {e}");
            }
        }

        private void AddPortCancelToken(int port, CancellationTokenSource cancelToken)
        {
            if (_cancelTokens.TryGetValue(port, out var token))
            {
                token.Cancel();
                _cancelTokens.TryRemove(port, out _);
            }
            _cancelTokens.TryAdd(port, cancelToken);
        }

        private async void WaitingForResponse(IPEndPoint remoteEndPoint, CancellationToken token = default)
        {
            try
            {
                using (token.Register(() => Client.Close()))
                {
                    var receiveTask = Client.ReceiveAsync();
                    var completedTask = await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(_broadcastWaitTimeSeconds), token));

                    if (completedTask == receiveTask && !token.IsCancellationRequested)
                    {
                        var result = receiveTask.Result;
                        var message = Encoding.UTF8.GetString(result.Buffer);
                        _receiveEvents.Enqueue(() => OnBroadcastReceivedFromPort((uint)remoteEndPoint.Port, message));
                    }
                    else
                    {
                        _receiveEvents.Enqueue(() => TimeUpForPort((uint)remoteEndPoint.Port));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"[UDP] Broadcast receive error: {e}");
                _receiveEvents.Enqueue(() => OnBroadcastFailedToReceive((uint)remoteEndPoint.Port));
            }
        }

        // Abstract methods to implement in your concrete class
        protected abstract void OnBroadcastReceivedFromPort(uint port, string message);
        protected abstract void TimeUpForPort(uint port);
        protected abstract void OnBroadcastFailedToReceive(uint port);
    }
}