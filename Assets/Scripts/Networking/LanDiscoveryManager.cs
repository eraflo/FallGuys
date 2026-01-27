using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Collections.Generic;

namespace FallGuys.Networking
{
    public class LanDiscoveryManager : MonoBehaviour
    {
        public static LanDiscoveryManager Singleton { get; private set; }

        [Header("Settings")]
        [SerializeField] private int _broadcastPort = 47777; // Dedicated port for discovery
        [SerializeField] private float _broadcastInterval = 1.0f;

        public event Action<LobbyEntry> OnLobbyFound;

        private UdpClient _udpClient;
        private float _timeSinceLastBroadcast;
        private bool _isBroadcasting = false;
        private bool _isListening = false;
        private LobbyEntry _myLobbyEntry;

        private void Awake()
        {
            if (Singleton != null && Singleton != this)
            {
                Destroy(gameObject);
                return;
            }
            Singleton = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (_isBroadcasting)
            {
                _timeSinceLastBroadcast += Time.deltaTime;
                if (_timeSinceLastBroadcast >= _broadcastInterval)
                {
                    BroadcastLobby();
                    _timeSinceLastBroadcast = 0;
                }
            }

            if (_isListening)
            {
                ReceiveBroadcasts();
            }
        }

        // --- Host Side ---
        public void StartBroadcasting(string hostName, int port, int currentPlayers, int maxPlayers)
        {
            _myLobbyEntry = new LobbyEntry(GetLocalIPAddress(), port, hostName, currentPlayers, maxPlayers);
            _isBroadcasting = true;
            Debug.Log($"[LAN] Started Broadcasting as {hostName} on port {port}");
        }

        public void StopBroadcasting()
        {
            _isBroadcasting = false;
        }

        private void BroadcastLobby()
        {
            try
            {
                if (_udpClient == null) 
                {
                    _udpClient = new UdpClient();
                    _udpClient.EnableBroadcast = true;
                }

                string json = JsonUtility.ToJson(_myLobbyEntry);
                // Debug.Log($"[LAN] Broadcasting: {json}"); // Uncomment for verbose usage
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, _broadcastPort);
                
                _udpClient.Send(bytes, bytes.Length, endPoint);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LAN] Broadcast Error: {e.Message}");
            }
        }

        // --- Client Side ---
        public void StartListening()
        {
            _isListening = true;
            if (_udpClient == null)
            {
                try {
                    _udpClient = new UdpClient();
                    _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _broadcastPort));
                    _udpClient.EnableBroadcast = true;
                    Debug.Log($"[LAN] Listening on Port {_broadcastPort} (ReuseAddress: ON)");
                } catch (Exception e) {
                     Debug.LogError($"[LAN] Failed to bind listener: {e.Message}");
                }
            }
            Debug.Log("[LAN] Started Listening for lobbies...");
        }

        public void StopListening()
        {
            _isListening = false;
            if (_udpClient != null)
            {
                _udpClient.Close();
                _udpClient = null;
            }
        }

        private void ReceiveBroadcasts()
        {
            if (_udpClient == null || _udpClient.Available <= 0) return;

            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes = _udpClient.Receive(ref remoteEndPoint);
                string json = Encoding.UTF8.GetString(bytes);

                LobbyEntry entry = JsonUtility.FromJson<LobbyEntry>(json);
                
                // Filter out self if testing locally
                // if (entry.IpAddress == GetLocalIPAddress() && _isBroadcasting) return;

                OnLobbyFound?.Invoke(entry);
                Debug.Log($"[LAN] Found Lobby: {entry.HostName} at {entry.IpAddress}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LAN] Receive Error: {e.Message}");
            }
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
        
        private void OnDestroy()
        {
            if (_udpClient != null) _udpClient.Close();
        }
    }
}
