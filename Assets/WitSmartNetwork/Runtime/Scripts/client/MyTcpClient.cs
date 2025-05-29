using System;
using WitNetwork.Log;

namespace WitNetwork.Client
{
    public class MyTcpClient : TcpClientBase
    {
        public MyTcpClient(string serverAddress, int port) : base(serverAddress, port) { }

        protected override void OnConnectedToServer()
        {
            Logger.Log("Connected to server successfully.");
            NetworkEventManager.Instance.OnClientConnectedToServer.Invoke();
        }

        protected override void OnDisconnectedFromServer()
        {
            NetworkEventManager.Instance.OnDisconnectedFromServer.Invoke();
        }

        protected override void OnFailedToConnectWithServer()
        {
            NetworkEventManager.Instance.OnFailToConnectWithServer.Invoke();
        }

        protected override void OnReceiveMessage(string message)
        {
            if (string.Equals(message, "ping", StringComparison.OrdinalIgnoreCase))
            {
                Send("pong");
                return;
            }
            Logger.Log($"Received message from server: {message}");
            NetworkEventManager.Instance.OnReceiveMessage.Invoke(message);
        }
    }
}