using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;

namespace WitNetwork.Communication
{
    [CreateAssetMenu(menuName = "WitSmartNetwork/CommunicationManager")]
    public class CommunicationManagerSO : ScriptableObject
    {
        private static CommunicationManagerSO _instance;
        public static CommunicationManagerSO Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<CommunicationManagerSO>("CommunicationManagerSO");
                    if (_instance == null)
                    {
                        Debug.LogError("CommunicationManagerSO not found in Resources folder.");
                    }
                }
                return _instance;
            }
        }

        [Serializable]
        public class CommandEvent
        {
            public string CMD;
            public UnityEvent<string> OnMessage; // Receives the raw JSON data for the command
        }

        [Header("Register your commands and UnityEvents here")]
        public List<CommandEvent> commandEvents = new();

        private Dictionary<string, UnityEvent<string>> _eventLookup;

        private void OnEnable()
        {
            _eventLookup = new Dictionary<string, UnityEvent<string>>();
            foreach (var ce in commandEvents)
            {
                if (!string.IsNullOrEmpty(ce.CMD) && ce.OnMessage != null)
                    _eventLookup[ce.CMD] = ce.OnMessage;
            }
        }

        public void Initialize()
        {
            if (Settings.Instance.Mode == NetworkMode.Client)
            {
                NetworkEventManager.Instance.OnReceiveMessage.AddListener(OnMessageReceived);
            }
        }

        public void SendMessage<T>(string cmd, T data)
        {
            var msg = NetworkMessage.Create(cmd, data);
            var json = JsonConvert.SerializeObject(msg);

            if (Settings.Instance.Mode == NetworkMode.Server)
                NetworkInitializer.Instance.Server.SendMessageToAllClients(json);
            else
                NetworkInitializer.Instance.Client.Send(json);
        }

        public void OnMessageReceived(string json)
        {
            NetworkMessage msg = null;
            try
            {
                msg = JsonConvert.DeserializeObject<NetworkMessage>(json);
            }
            catch (JsonException ex)
            {
                WitNetwork.Log.Logger.LogWarning($"Failed to deserialize network message: {ex.Message}");
                return;
            }

            if (msg == null || string.IsNullOrEmpty(msg.CMD))
            {
                WitNetwork.Log.Logger.LogWarning("Received invalid network message.");
                return;
            }

            if (_eventLookup != null && _eventLookup.TryGetValue(msg.CMD, out var unityEvent))
            {
                unityEvent.Invoke(msg.Data);
            }
            else
            {
                WitNetwork.Log.Logger.LogWarning($"No UnityEvent registered for CMD '{msg.CMD}'.");
            }
        }

        public void RegisterMessageHandler(string cmd, UnityAction<string> handler)
        {
            if (string.IsNullOrEmpty(cmd) || handler == null)
            {
                WitNetwork.Log.Logger.LogWarning("Invalid command or handler provided for registration.");
                return;
            }

            if (_eventLookup.TryGetValue(cmd, out var unityEvent))
            {
                unityEvent.AddListener(handler);
            }
            else
            {
                var newEvent = new UnityEvent<string>();
                newEvent.AddListener(handler);
                _eventLookup[cmd] = newEvent;
            }
        }
    }
}