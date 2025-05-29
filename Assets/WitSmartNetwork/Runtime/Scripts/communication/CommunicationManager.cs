using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WitNetwork.Log;

namespace WitNetwork.Communication
{
    /// <summary>
    /// Handles sending and receiving NetworkMessages, and dispatches them to the correct handler based on CMD.
    /// </summary>
    public class CommunicationManager
    {
        // Maps CMD to a handler and its expected type
        private readonly Dictionary<string, (Type type, Action<object> handler)> _handlers = new();

        public void Initialize()
        {
            if (Settings.Instance.Mode == NetworkMode.Client)
            {
                NetworkEventManager.Instance.OnReceiveMessage.AddListener(OnMessageReceived);
            }
        }

        // Register a handler for a specific CMD and type
        public void RegisterHandler<T>(string cmd, Action<T> handler)
        {
            _handlers[cmd] = (typeof(T), msgObj => handler((T)msgObj));
        }

        // Send a message (you can expand this to actually send over network)
        public string CreateMessage<T>(string cmd, T data)
        {
            return JsonConvert.SerializeObject(NetworkMessage.Create(cmd, data));
        }

        public void SendMessage(string message)
        {
            if (Settings.Instance.Mode == NetworkMode.Server)
            {
                NetworkInitializer.Instance.Server.SendMessageToAllClients(message);
            }
            else
            {
                NetworkInitializer.Instance.Client.Send(message);
            }
        }

        // Call this when a message is received (as JSON string)
        public void OnMessageReceived(string json)
        {
            var msg = JsonConvert.DeserializeObject<NetworkMessage>(json);
            if (msg == null || string.IsNullOrEmpty(msg.CMD))
            {
                Logger.LogError("Received invalid network message.");
                return;
            }
            Logger.Log($"Received message with Data: {json}");

            if (_handlers.TryGetValue(msg.CMD, out var handlerInfo))
            {
                var obj = JsonConvert.DeserializeObject(msg.Data, handlerInfo.type);
                handlerInfo.handler(obj);
            }
            else
            {
                Logger.LogWarning($"No handler registered for CMD '{msg.CMD}'.");
            }
        }
    }
}