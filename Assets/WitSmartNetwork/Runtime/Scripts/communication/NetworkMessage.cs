using Newtonsoft.Json;
namespace WitSmartNetwork.Communication
{
    public class NetworkMessage
    {
        public string CMD { get; set; }
        public string Data { get; set; }

        // Deserialize the Data property to a specific type using Newtonsoft.Json
        public T GetData<T>()
        {
            if (string.IsNullOrEmpty(Data)) return default;
            return JsonConvert.DeserializeObject<T>(Data);
        }

        // Create a NetworkMessage from a command and a data object using Newtonsoft.Json
        public static NetworkMessage Create<T>(string cmd, T data)
        {
            return new NetworkMessage
            {
                CMD = cmd,
                Data = JsonConvert.SerializeObject(data)
            };
        }
    }
}