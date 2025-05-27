using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WitSmartNetwork.Client
{
    public abstract class TcpClientBase : IDisposable
    {
        protected struct ClientRef
        {
            public TcpClient Client;
            public CancellationTokenSource TokenSource;

            public readonly bool IsRunning => Client.Connected;
            public readonly bool IsConnected => IsRunning && !TokenSource.Token.IsCancellationRequested;

            public readonly void Connect(IPEndPoint endPoint)
            {
                Client.Connect(endPoint);
            }

            public readonly void Stop()
            {
                TokenSource.Cancel();
                Client.Close();
            }
        }

        private ConcurrentDictionary<int, ClientRef> _tcpClients = new ConcurrentDictionary<int, ClientRef>();
        private ConcurrentQueue<Action> _receivedMessages = new ConcurrentQueue<Action>();
        private ConcurrentQueue<string> _sendMessages = new ConcurrentQueue<string>();

        private Thread receiveThread;
        private Thread sendThread;

        protected ClientRef Client
        {
            get
            {
                if (_tcpClients.TryGetValue(0, out ClientRef client))
                {
                    return client;
                }
                else
                {
                    client = new ClientRef()
                    {
                        Client = new TcpClient(),
                        TokenSource = new CancellationTokenSource(),
                    };

                    _tcpClients.TryAdd(0, client);
                }

                return client;
            }
        }

        public bool IsRunning => Client.IsRunning;

        public TcpClientBase(string serverAddress, int port)
        {
            IPEndPoint endPoint = null;
            try
            {
                endPoint = new IPEndPoint(IPAddress.Parse(serverAddress), port);
            }
            catch (Exception)
            {
                OnFailedToConnectWithServer();
                return;
            }

            try
            {
                Client.Connect(endPoint);
                Initialize();
            }
            catch (Exception)
            {
                OnFailedToConnectWithServer();
            }
        }

        protected void Initialize()
        {
            receiveThread = new Thread(HandleIncoming);
            sendThread = new Thread(HandleOutgoing);

            receiveThread.Start();
            sendThread.Start();

            OnConnectedToServer();
        }

        public void Stop()
        {
            Client.Stop();
            _receivedMessages.Enqueue(OnDisconnectedFromServer);
        }

        public void Dispose()
        {
            Stop();
        }

        private void HandleIncoming()
        {
            try
            {
                using StreamReader reader = new StreamReader(Client.Client.GetStream());

                while (Client.IsConnected)
                {
                    try
                    {
                        string message = reader.ReadLine();
                        if (!string.IsNullOrEmpty(message))
                        {
                            _receivedMessages.Enqueue(() => OnReceiveMessage(message));
                        }
                    }
                    catch (IOException)
                    {
                        // ignored
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                Logger.LogError(e.ToString());
            }
            catch (Exception e)
            {
                Logger.LogError("Incoming thread exception\n" + e);
            }
            finally
            {
                Stop();
            }
        }

        private void HandleOutgoing()
        {
            try
            {
                using StreamWriter writer = new StreamWriter(Client.Client.GetStream());
                writer.AutoFlush = true;
                while (Client.IsConnected)
                {
                    if (_sendMessages.TryDequeue(out string message))
                    {
                        writer.WriteLine(message);
                        writer.Flush();
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                Logger.LogError(e.ToString());
            }
            catch (Exception e)
            {
                Logger.LogError("Outgoing thread exception\n" + e);
            }
            finally
            {
                Stop();
            }
        }

        public void Send(string message)
        {
            _sendMessages.Enqueue(message);
        }

        public void Update()
        {
            try
            {
                while (_receivedMessages.TryDequeue(out Action action))
                {
                    action?.Invoke();
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Error while processing received messages\n" + e);
            }
        }

        #region Abstraction

        protected abstract void OnConnectedToServer();
        protected abstract void OnDisconnectedFromServer();
        protected abstract void OnFailedToConnectWithServer();
        protected abstract void OnReceiveMessage(string message);

        #endregion
    }
}