using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FallGuys.Networking
{
    // Define the states for the lobby
    public enum LobbyState
    {
        Offline,
        WaitingForPlayers,
        Countdown,
        GameLoading
    }

    // Player data to be synchronized across the network
    public struct PlayerData : INetworkSerializable, System.IEquatable<PlayerData>
    {
        public ulong ClientId;
        public Unity.Collections.FixedString32Bytes PlayerName; // Example for player name
        public bool IsReady;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref IsReady);
        }

        public bool Equals(PlayerData other)
        {
            return ClientId == other.ClientId && PlayerName.Equals(other.PlayerName) && IsReady == other.IsReady;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ClientId, PlayerName, IsReady);
        }
    }

    public class LobbyManager : NetworkBehaviour
    {
        public static LobbyManager Singleton { get; private set; }

        // --- Networked Variables ---
        public NetworkVariable<LobbyState> CurrentLobbyState = new NetworkVariable<LobbyState>(LobbyState.Offline);
        public NetworkList<PlayerData> ConnectedPlayers;
        public NetworkVariable<float> CountdownTimer = new NetworkVariable<float>(0);

        // --- Configuration ---
        [SerializeField] private int _minPlayersToStart = 2;
        [SerializeField] private float _countdownDuration = 5f;
        [SerializeField] private string _gameSceneName = "GameScene"; // Name of your actual game scene
        [SerializeField] private int _maxPlayers = 4; // Maximum number of players allowed in the lobby
        public int MaxPlayers => _maxPlayers;

        private void Awake()
        {
            // Initialisation critique AVANT tout check de Singleton pour que Netcode soit content s'il scanne cette instance
            ConnectedPlayers = new NetworkList<PlayerData>();

            if (Singleton != null && Singleton != this)
            {
                Debug.Log($"[LobbyManager] Destroying duplicate instance {gameObject.GetInstanceID()}");
                // On doit clean la liste qu'on vient de créer pour ne pas fuir sur le doublon !
                ConnectedPlayers.Dispose();
                Destroy(gameObject);
                return;
            }

            Singleton = this;
            Debug.Log($"[LobbyManager] Initialized Singleton {gameObject.GetInstanceID()}");
        }





        public override void OnDestroy()
        {
            if (LanDiscoveryManager.Singleton != null)
            {
                LanDiscoveryManager.Singleton.StopBroadcasting();
            }

            CleanupNetworkList();
            if (Singleton == this)
            {
                Singleton = null;
            }
            base.OnDestroy();
        }

        private void CleanupNetworkList()
        {
            // Only dispose if it was created and we are destroying the object
            if (ConnectedPlayers != null)
            {
                Debug.Log($"[LobbyManager] Disposing ConnectedPlayers NetworkList in instance {gameObject.GetInstanceID()}");
                ConnectedPlayers.OnListChanged -= OnConnectedPlayersChanged;
                ConnectedPlayers.Dispose();
            }
        }

        private void Update()
        {
            if (!IsServer) return; // Only server manages lobby state

            switch (CurrentLobbyState.Value)
            {
                case LobbyState.WaitingForPlayers:
                    // Check if enough players are connected and ready
                    if (ConnectedPlayers.Count >= _minPlayersToStart && AllPlayersReady())
                    {
                        CurrentLobbyState.Value = LobbyState.Countdown;
                        CountdownTimer.Value = _countdownDuration;
                    }
                    break;

                case LobbyState.Countdown:
                    CountdownTimer.Value -= Time.deltaTime;
                    if (CountdownTimer.Value <= 0)
                    {
                        CurrentLobbyState.Value = LobbyState.GameLoading;
                        LoadGameScene();
                    }
                    // If players leave during countdown, revert to waiting
                    if (ConnectedPlayers.Count < _minPlayersToStart || !AllPlayersReady())
                    {
                        CurrentLobbyState.Value = LobbyState.WaitingForPlayers;
                        CountdownTimer.Value = 0;
                    }
                    break;

                case LobbyState.GameLoading:
                    // Server has already initiated scene load.
                    break;
            }
        }

        // Event invoked when any connection attempt (Host or Client) is started locally
        public event Action OnConnectionStarted;

        // Event invoked when lobby is left (disconnected, kicked, or quit)
        public event Action OnLeftLobby;

        // --- Host/Client Connection Methods ---
        // --- Host/Client Connection Methods ---
        public void StartHost(string ip = null, int port = 0)
        {
            if (NetworkManager.Singleton == null)
            {
                var nmInScene = FindFirstObjectByType<NetworkManager>();
                if (nmInScene != null)
                {
                    Debug.LogError($"NetworkManager.Singleton is null, BUT a NetworkManager component '{nmInScene.name}' exists in the scene. Is it enabled? Is it initializing correcty (check for other errors like 'Multiple NetworkManagers')?");
                }
                else
                {
                    Debug.LogError("NetworkManager.Singleton is null AND no NetworkManager component was found in the scene using FindFirstObjectByType. You MUST add a NetworkManager to your scene.");
                }
                return;
            }

            // Notify listeners (UI) that we are starting
            OnConnectionStarted?.Invoke();

            // Explicit Binding Configuration
            if (!string.IsNullOrEmpty(ip))
            {
                var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    // FIX: Use SetConnectionData
                    if (port == 0) port = 7777;
                    transport.SetConnectionData(ip, (ushort)port);

                    Debug.Log($"[LobbyManager] Starting HOST bound to {ip}:{port}");
                }
            }
            else
            {
                Debug.Log("[LobbyManager] Starting HOST (Default Binding 0.0.0.0)");
                if (port == 0) port = 7777;
            }

            NetworkManager.Singleton.StartHost();

            // Start LAN Discovery Broadcast
            if (LanDiscoveryManager.Singleton != null)
            {
                // We broadcast the IP we bound to, OR the local IP if we bound to all
                string broadcastIp = !string.IsNullOrEmpty(ip) ? ip : GetLocalIPAddress();

                Debug.Log($"[LobbyManager] Starting Discovery Broadcast on Port: {port}...");
                LanDiscoveryManager.Singleton.StartBroadcasting($"Host {Environment.UserName}", port, 1, _maxPlayers);
            }
            else
            {
                // Ensure the manager exists if possible, or warn
                Debug.LogWarning("LanDiscoveryManager not found. Server will not be discoverable on LAN.");
            }

            // Server-side: add host player data
            if (IsServer)
            {
                AddPlayer(NetworkManager.Singleton.LocalClientId, "HostPlayer"); // Placeholder name
            }
        }

        public void StartClient(string ip = null, int port = 0)
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("NetworkManager.Singleton is null");
                return;
            }

            // Notify listeners (UI) that we are starting
            OnConnectionStarted?.Invoke();

            // Configure Transport if IP/Port provided
            if (!string.IsNullOrEmpty(ip))
            {
                var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    // SMART LOCALHOST: If we are trying to connect to our own public IP, switch to 127.0.0.1
                    // This fixes "NAT Loopback" issues on many routers/firewalls when testing locally
                    string localIp = GetLocalIPAddress();
                    if (ip == localIp)
                    {
                        Debug.LogWarning($"[LobbyManager] Detected connection to Local IP ({ip}). Switching to 127.0.0.1 for stability.");
                        ip = "127.0.0.1";
                    }

                    // FIX: Use SetConnectionData because ConnectionData is a struct property!
                    // Modifying transport.ConnectionData.Port directly modifies a copy and does nothing.
                    transport.SetConnectionData(ip, (ushort)port);
                    Debug.Log($"Configured Client to connect to {ip}:{port}");
                }
            }

            NetworkManager.Singleton.StartClient();
            // Client-side: player data will be added via HandleClientConnected on server
        }

        public void Shutdown()
        {
            if (LanDiscoveryManager.Singleton != null)
            {
                LanDiscoveryManager.Singleton.StopBroadcasting();
            }

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            // Cleanup list manually since we are leaving
            // If we are Host, the NetworkList will be destroyed when the object is destroyed/despawned
            // But good to clear local state
            if (ConnectedPlayers != null && ConnectedPlayers.Count > 0)
            {
                // We can't clear a NetworkList if we are not server or if network is down, 
                // so we just notify UI via OnLeftLobby
            }

            OnLeftLobby?.Invoke();
        }

        // Helper for finding local IP
        private string GetLocalIPAddress()
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }

        // --- Server Callbacks ---
        private void HandleClientConnected(ulong clientId)
        {
            Debug.Log($"[LobbyManager] SERVER: Client Connected! ID={clientId}");
            if (IsServer)
            {
                if (ConnectedPlayers.Count >= _maxPlayers)
                {
                    Debug.LogWarning($"Lobby is full. Client {clientId} cannot join. Disconnecting them.");
                    // Enforce Max Players: Disconnect the client immediately
                    // Note: In newer Netcode versions we can provide a reason string, but DisconnectClient is standard.
                    NetworkManager.Singleton.DisconnectClient(clientId);
                    return;
                }
                // Add new player to the list
                AddPlayer(clientId, $"Player {clientId}");
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            Debug.Log($"[LobbyManager] SERVER: Client Disconnected! ID={clientId}");
            if (IsServer)
            {
                RemovePlayer(clientId);
                // If in countdown and too few players, reset
                if (CurrentLobbyState.Value == LobbyState.Countdown && ConnectedPlayers.Count < _minPlayersToStart)
                {
                    CurrentLobbyState.Value = LobbyState.WaitingForPlayers;
                    CountdownTimer.Value = 0;
                }
            }

            // Client Logic: If the Host (Server) disconnects, we need to handle it
            // usually clientId 0 is the server, but relying on IsServer check above covers host logic.
            // For CLIENTS: We need to subscribe to OnClientDisconnectCallback in OnNetworkSpawn locally!
        }

        // --- Network Spawn Update ---
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Initialize server-side lobby state
                CurrentLobbyState.Value = LobbyState.WaitingForPlayers;
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            }
            else
            {
                // Client-side, subscribe to state changes
                CurrentLobbyState.OnValueChanged += OnLobbyStateChanged;
                // Listen for own disconnection (e.g. kicked by server or server shutdown)
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected_Local;
            }

            ConnectedPlayers.OnListChanged += OnConnectedPlayersChanged;
            // Initial update for clients joining after host is already running
            if (!IsServer)
            {
                UpdateLobbyUI(); // Or whatever initial client UI update is needed
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
            else
            {
                CurrentLobbyState.OnValueChanged -= OnLobbyStateChanged;
                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected_Local;
                }
            }

            // Do NOT Dispose the list here if the Manager persists!
            // CleanupNetworkList(); 
        }

        // Client-side callback when WE get disconnected
        private void OnClientDisconnected_Local(ulong clientId)
        {
            // If the disconnected ID is the Server's ID (usually 0), or if it's US (LocalClientId)
            if (clientId == NetworkManager.ServerClientId || clientId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.LogWarning("[LobbyManager] Disconnected from Server (or Server stopped). returning to Menu.");
                // Trigger UI cleanup
                Shutdown();
            }
        }

        // --- Player Management (Server-side) ---
        private void AddPlayer(ulong clientId, string playerName)
        {
            if (!IsServer) return;

            // Check if player already exists to avoid duplicates
            foreach (var player in ConnectedPlayers)
            {
                if (player.ClientId == clientId)
                {
                    return;
                }
            }

            PlayerData newPlayer = new PlayerData
            {
                ClientId = clientId,
                PlayerName = playerName,
                IsReady = false
            };

            ConnectedPlayers.Add(newPlayer);
        }

        private void RemovePlayer(ulong clientId)
        {
            if (!IsServer) return;

            for (int i = 0; i < ConnectedPlayers.Count; i++)
            {
                if (ConnectedPlayers[i].ClientId == clientId)
                {
                    ConnectedPlayers.RemoveAt(i);
                    break;
                }
            }
        }

        private bool AllPlayersReady()
        {
            if (ConnectedPlayers.Count == 0) return false;
            foreach (var player in ConnectedPlayers)
            {
                if (!player.IsReady) return false;
            }
            return true;
        }

        // --- Client-to-Server RPCs ---
        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerReadyServerRpc(ulong clientId, bool isReady)
        {
            for (int i = 0; i < ConnectedPlayers.Count; i++)
            {
                if (ConnectedPlayers[i].ClientId == clientId)
                {
                    PlayerData playerData = ConnectedPlayers[i];
                    playerData.IsReady = isReady;
                    ConnectedPlayers[i] = playerData; // NetworkList requires re-assignment for update
                    break;
                }
            }
        }

        // --- Scene Loading (Server-side) ---
        private void LoadGameScene()
        {
            if (IsServer)
            {
                CurrentLobbyState.Value = LobbyState.GameLoading;

                // DELEGATE TO GAMEMANAGER: Centralized transition logic
                if (FallGuys.Core.GameManager.Instance != null)
                {
                    FallGuys.Core.GameManager.Instance.OnLaunchGame();
                }
                else
                {
                    Debug.LogWarning("[LobbyManager] GameManager.Instance not found! Falling back to direct scene load.");
                    NetworkManager.Singleton.SceneManager.LoadScene(_gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
                }
            }
        }

        // --- Client Callbacks (UI Updates) ---
        private void OnLobbyStateChanged(LobbyState oldState, LobbyState newState)
        {
            Debug.Log($"Lobby State Changed: {oldState} -> {newState}");
            // Trigger UI updates based on newState
            UpdateLobbyUI();
        }

        private void OnConnectedPlayersChanged(NetworkListEvent<PlayerData> change)
        {
            Debug.Log($"Connected Players List Changed: {change.Type}");
            // Trigger UI updates based on player list changes
            UpdateLobbyUI();
        }

        private void UpdateLobbyUI()
        {
            // This method should be implemented by a separate UI manager or directly here
            // to update the lobby UI elements (player list, countdown, ready status)
            Debug.Log($"Current State: {CurrentLobbyState.Value}, Players: {ConnectedPlayers.Count}");
            foreach (var player in ConnectedPlayers)
            {
                Debug.Log($" - {player.PlayerName} (ID: {player.ClientId}) Ready: {player.IsReady}");
            }
            if (CurrentLobbyState.Value == LobbyState.Countdown)
            {
                Debug.Log($"Countdown: {Mathf.CeilToInt(CountdownTimer.Value)}");
            }
        }
    }
}
