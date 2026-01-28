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

        private Coroutine _connectionTimeoutCoroutine;
        private const float CONNECTION_TIMEOUT = 3f; // Seconds to wait for connection

        // --- Events ---
        // Event invoked when connection attempt starts (for showing loading UI)
        public event Action OnConnectionStarted;

        // Event invoked when connection is successfully established
        public event Action OnConnectionSuccess;

        // Event invoked when connection attempt fails
        public event Action<string> OnConnectionFailed;

        // Event invoked when lobby is left (disconnected, kicked, or quit)
        public event Action OnLeftLobby;


        private void Awake()
        {
            // Initialisation critique AVANT tout check de Singleton pour que Netcode soit content s'il scanne cette instance
            ConnectedPlayers = new NetworkList<PlayerData>();

            if (Singleton != null && Singleton != this)
            {
                // If the old singleton exists but we're in the Lobby scene, 
                // it means we returned after a disconnect - destroy the OLD one and use this new clean instance
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Lobby")
                {
                    // Destroy the OLD singleton (it has corrupted state)
                    Destroy(Singleton.gameObject);

                    // This new instance becomes the singleton
                    Singleton = this;
                    DontDestroyOnLoad(gameObject);
                    return;
                }

                // Normal case: we're in a game scene, destroy THIS duplicate
                ConnectedPlayers.Dispose();
                Destroy(gameObject);
                return;
            }

            Singleton = this;
            DontDestroyOnLoad(gameObject);
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
                ConnectedPlayers.OnListChanged -= OnConnectedPlayersChanged;
                ConnectedPlayers.Dispose();
            }
        }

        private void Update()
        {
            // NOTE: Client disconnect detection is now handled by ClientDisconnectWatcher (standalone MonoBehaviour)
            // This Update only handles SERVER lobby state management
            if (!IsServer) return;

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
                AddPlayer(NetworkManager.Singleton.LocalClientId, "HostPlayer");
                // Host is immediately connected - trigger success event
                OnConnectionSuccess?.Invoke();
            }
        }

        public void StartClient(string ip = null, int port = 0)
        {
            try
            {
                if (NetworkManager.Singleton == null)
                {
                    Debug.LogError("NetworkManager.Singleton is null");
                    OnConnectionFailed?.Invoke("NetworkManager not found");
                    return;
                }

                // Configure Transport if IP/Port provided
                if (!string.IsNullOrEmpty(ip))
                {
                    var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                    if (transport != null)
                    {
                        // SMART LOCALHOST: If we are trying to connect to our own public IP, switch to 127.0.0.1
                        string localIp = GetLocalIPAddress();
                        if (ip == localIp)
                        {
                            ip = "127.0.0.1";
                        }
                        transport.SetConnectionData(ip, (ushort)port);
                    }
                }

                // Subscribe to callbacks for connection result
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedForUI;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedDuringConnect;

                NetworkManager.Singleton.StartClient();

                // Start timeout - if no connection after X seconds, cancel
                _connectionTimeoutCoroutine = StartCoroutine(ConnectionTimeoutCoroutine());
            }
            catch (Exception e)
            {
                Debug.LogError("Error starting client: " + e.Message);
                OnConnectionFailed?.Invoke(e.Message);
            }
        }

        private void OnClientConnectedForUI(ulong clientId)
        {
            // Only care about our own connection
            if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
            {
                // Cancel timeout
                if (_connectionTimeoutCoroutine != null)
                {
                    StopCoroutine(_connectionTimeoutCoroutine);
                    _connectionTimeoutCoroutine = null;
                }

                // Clean up callbacks
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedForUI;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedDuringConnect;

                // Connection successful!
                OnConnectionSuccess?.Invoke();
            }
        }

        private void OnClientDisconnectedDuringConnect(ulong clientId)
        {
            if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
            {
                CancelConnectionAttempt();
            }
        }

        private void CancelConnectionAttempt()
        {
            // Cancel timeout coroutine
            if (_connectionTimeoutCoroutine != null)
            {
                StopCoroutine(_connectionTimeoutCoroutine);
                _connectionTimeoutCoroutine = null;
            }

            // Clean up callbacks
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedForUI;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedDuringConnect;
                NetworkManager.Singleton.Shutdown();
            }

            OnConnectionFailed?.Invoke("Connection failed");
        }

        private System.Collections.IEnumerator ConnectionTimeoutCoroutine()
        {
            yield return new WaitForSeconds(CONNECTION_TIMEOUT);

            // If we get here, connection timed out
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsConnectedClient)
            {
                CancelConnectionAttempt();
            }
        }

        public void Shutdown()
        {
            Debug.Log("[LobbyManager] Shutdown called.");

            if (LanDiscoveryManager.Singleton != null)
            {
                LanDiscoveryManager.Singleton.StopBroadcasting();
            }

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            OnLeftLobby?.Invoke();

            // Return to lobby scene
            ReturnToLobbyScene();
        }

        /// <summary>
        /// Loads the Lobby scene. Call this when disconnected.
        /// </summary>
        private void ReturnToLobbyScene()
        {
            Debug.Log($"[LobbyManager] ReturnToLobbyScene called. Singleton={Singleton != null}, this={this != null}");
            SceneManager.LoadScene("Lobby");
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
                Debug.Log("[LobbyManager] CLIENT: Marked as connected");
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
