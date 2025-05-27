using System.Net;

namespace WitSmartNetwork.Client
{
    public class MyUdpBroadcaster : UdpBroadcaster
    {
        public MyUdpBroadcaster() : base(2f) { }

        private string IpAddress;
        public bool IsFailed = false;


        protected override void OnBroadcastReceivedFromPort(uint port, string message)
        {
            // check if message is the ip format then assign it
            if (IPAddress.TryParse(message, out IPAddress ip))
            {
                IpAddress = ip.ToString();
                return;
            }

            Logger.Log($"Received UDP response from port {port}: {message}");
        }

        protected override void TimeUpForPort(uint port)
        {
            Logger.LogWarning($"UDP broadcast timed out for port {port}");
            IsFailed = true;
        }

        protected override void OnBroadcastFailedToReceive(uint port)
        {
            Logger.LogWarning($"UDP broadcast failed for port {port}");
            IsFailed = true;
        }

        public bool HasIpAddress(out string ipAddress)
        {
            if (IpAddress != null &&
                !string.IsNullOrEmpty(IpAddress) &&
                IPAddress.TryParse(IpAddress, out _))
            {
                ipAddress = IpAddress;
                return true;
            }
            ipAddress = string.Empty;
            return false;
        }
    }
}