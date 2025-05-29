using System.Threading;
using UnityEngine;
using WitNetwork.Client;
using WitNetwork.Communication;
using WitNetwork.Server;
namespace WitNetwork
{
    public class NetworkInitializer : MonoBehaviour
    {
        private static NetworkInitializer _instance;
        public static NetworkInitializer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<NetworkInitializer>();

                }
                return _instance;
            }
        }

        private Settings Settings => Settings.Instance;
        private MyTcpServer server;
        private MyTcpClient client;

        public MyTcpServer Server => server;
        public MyTcpClient Client => client;

        [Header("Settings")]
        public bool autoStart = true;
        public bool communicationInitialization = true;

        private void Start()
        {
            if (Settings == null)
            {
                WitNetwork.Log.Logger.LogError("Settings ScriptableObject not assigned!");
                return;
            }

#if UNITY_SERVER && !UNITY_EDITOR
            // If running as a dedicated server, force server mode and initialize server only
            Settings.Mode = NetworkMode.Server;
            StartServer();
            return;
#endif

            if (autoStart)
            {
                InitializeNetwork();
            }
        }

        private void InitializeNetwork()
        {
            if (Settings.Mode == NetworkMode.Server)
            {
                StartServer();
            }
            else if (Settings.Mode == NetworkMode.Client)
            {
                StartClient();
            }
            if (communicationInitialization)
            {
                CommunicationManagerSO.Instance.Initialize();
            }
        }

        private void StartServer()
        {
            server = new MyTcpServer(Settings.ServerPort, "0.0.0.0");
            server.StartServer();
        }

        private void StartClient()
        {
            WitNetwork.Log.Logger.Log("Starting WitShells Smart Network Client!");

            string discoverIp = Settings.ServerIp;
            int port = Settings.ServerPort;
            int groupId = Settings.GroupId;

            if (Settings.ServerMode == ServerMode.Local)
            {
                MyUdpBroadcaster udpBroadcaster = new MyUdpBroadcaster();
                udpBroadcaster.SendBroadcastMessage((ushort)port, "ip_request");
                while (!udpBroadcaster.HasIpAddress(out discoverIp))
                {
                    udpBroadcaster.Update();
                    if (udpBroadcaster.IsFailed)
                    {
                        WitNetwork.Log.Logger.LogError("Failed to discover server IP.");
                        return;
                    }
                    Thread.Sleep(100); // Wait a bit before trying again
                }
                WitNetwork.Log.Logger.Log($"Discovered server IP: {discoverIp}");
            }

            client = new MyTcpClient(discoverIp.Trim(), port);
            if (groupId > 0)
            {
                client.Send($"groupId {groupId}");
            }
        }

        private void Update()
        {
            if (server != null && server.IsRunning)
            {
                server.Update();
            }

            if (client != null && client.IsRunning)
            {
                client.Update();
            }
        }

        private void OnApplicationQuit()
        {
            server?.Dispose();
            client?.Dispose();
        }

#if UNITY_EDITOR
        // Adds a button in the Inspector to stop the server or client if running (Editor only)
        [UnityEditor.CustomEditor(typeof(NetworkInitializer))]
        public class NetworkInitializerEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                NetworkInitializer ni = (NetworkInitializer)target;
                if (ni.server != null && ni.server.IsRunning)
                {
                    if (GUILayout.Button("Stop Server"))
                    {
                        ni.server.Dispose();
                        ni.server = null;
                        WitNetwork.Log.Logger.Log("Server stopped.");
                    }
                }

                if (ni.client != null && ni.client.IsRunning)
                {
                    if (GUILayout.Button("Stop Client"))
                    {
                        ni.client.Dispose();
                        ni.client = null;
                        WitNetwork.Log.Logger.Log("Client stopped.");
                    }
                }
            }
        }
#endif
    }
}