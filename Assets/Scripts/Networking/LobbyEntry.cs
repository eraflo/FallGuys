using System;

namespace FallGuys.Networking
{
    [Serializable]
    public class LobbyEntry
    {
        public string IpAddress;
        public int Port;
        public string HostName;
        public int PlayerCount;
        public int MaxPlayers;

        public LobbyEntry(string ip, int port, string name, int currentPlayers, int maxPlayers)
        {
            IpAddress = ip;
            Port = port;
            HostName = name;
            PlayerCount = currentPlayers;
            MaxPlayers = maxPlayers;
        }
    }
}
