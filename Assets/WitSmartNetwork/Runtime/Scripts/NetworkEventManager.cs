using System;
using UnityEngine;
using UnityEngine.Events;

namespace WitSmartNetwork
{
    public class NetworkEventManager : MonoBehaviour
    {
        private static NetworkEventManager _instance;

        public static NetworkEventManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<NetworkEventManager>();
                }
                return _instance;
            }
        }

        [Header("Server Events")]
        public UnityEvent OnServerStarted;
        public UnityEvent OnServerStopped;
        public UnityEvent<Exception> OnServerFailed;
        public UnityEvent<uint> OnClientConnected;
        public UnityEvent<uint> OnClientDisconnected;
        public UnityEvent<uint, string> OnServerMessageReceived;

        [Header("Client Events")]
        public UnityEvent OnClientConnectedToServer;
        public UnityEvent OnDisconnectedFromServer;
        public UnityEvent OnFailToConnectWithServer;
        public UnityEvent<string> OnReceiveMessage;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}