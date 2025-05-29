using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using WitNetwork.Log;

namespace WitNetwork.Server
{
    public class UdpIpListener : IDisposable
    {
        private readonly int _port;
        private UdpClient _udpClient;
        private Thread _receiveThread;
        private volatile bool _isRunning;
        public UdpIpListener(int port)
        {
            _port = port;
        }

        public void Start()
        {
            _udpClient = new UdpClient(_port);
            _isRunning = true;
            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();
            Logger.Log($"[UDP] Listening for IP requests on port {_port}");
        }

        private void ReceiveLoop()
        {
            try
            {
                while (_isRunning)
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _udpClient!.Receive(ref remoteEP);
                    string message = Encoding.UTF8.GetString(data);
                    Logger.Log($"[UDP] Received message '{message}' from {remoteEP}");
                    if (message == "ip_request")
                    {
                        // Respond to IP request
                        string serverIp = GetServerIp();
                        byte[] response = Encoding.UTF8.GetBytes(serverIp);
                        _udpClient.Send(response, response.Length, remoteEP);
                        Logger.Log($"[UDP] Sent IP '{serverIp}' to {remoteEP}");
                    }
                }
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
        }

        public string GetServerIp()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4
                {
                    string[] parts = ip.ToString().Split('.');
                    if (parts.Length == 4 && parts[3] != "1")
                    {
                        return ip.ToString();
                    }
                }
            }
            return "";
        }

        public void Dispose()
        {
            _isRunning = false;
            _udpClient?.Close();
        }
    }
}