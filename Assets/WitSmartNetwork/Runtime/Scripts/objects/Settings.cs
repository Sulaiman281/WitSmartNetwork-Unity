using UnityEngine;
using WitNetwork.Server;
namespace WitNetwork
{
    public enum NetworkMode { Server, Client }

    [CreateAssetMenu(menuName = "WitNetwork/Settings", fileName = "NetworkSettings", order = 1)]
    public class Settings : ScriptableObject
    {
        private static readonly Settings _instance = null;
        public static Settings Instance => _instance ?? Load();

        public NetworkMode Mode;
        public ServerMode ServerMode;
        public int ServerPort = 9092;
        public string ServerIp = "127.0.0.1";
        public int GroupId = 0;
        public int PingIntervalSeconds = 5;
        public int PingTimeoutIntervals = 3;

        public static Settings Load()
        {
            return Resources.Load<Settings>("NetworkSettings");
        }
    }
}