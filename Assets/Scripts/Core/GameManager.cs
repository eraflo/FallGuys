using FallGuys.Networking;
using Unity.Netcode;
using UnityEngine;

namespace FallGuys.Core
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        [Header("Settings")]
        [SerializeField] private string _gameSceneName = "GameScene";
        //public StockLevel CurrentLevel;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            Debug.Log($"[GameManager] Scene Event: {sceneEvent.SceneEventType} for scene {sceneEvent.SceneName}");

            // Trigger level load once the scene is fully loaded on the server
            if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted && sceneEvent.SceneName == _gameSceneName)
            {
                if (IsServer)
                {
                    Debug.Log("[GameManager] Scene load completed. Triggering LoadLevel...");
                    LoadLevel();
                }
            }
        }

        /// <summary>
        /// Triggered when the UI/Lobby initiates the game launch.
        /// </summary>
        public void OnLaunchGame()
        {
            if (!IsServer) return;

            Debug.Log("[GameManager] Launching Game...");

            // TODO: Select which StockLevel to load

            // Load the scene using NGO SceneManager for synchronization
            NetworkManager.Singleton.SceneManager.LoadScene(_gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        private void LoadLevel()
        {
            Debug.Log("[GameManager] LoadLevel: Searching for PlayerSpawnZone in scene...");

            //Debug.Log($"[GameManager] Loading Level: {(CurrentLevel != null ? CurrentLevel.name : "Default")}");
            // TODO: Implement the actual level instantiation using StockLevel data.

            // 1. Try to find the PlayerSpawnZone in the scene
            Eraflo.Common.Player.PlayerSpawnZone spawnZone = Object.FindFirstObjectByType<Eraflo.Common.Player.PlayerSpawnZone>();

            if (spawnZone != null)
            {
                Debug.Log("[GameManager] Found PlayerSpawnZone. Triggering player spawn.");
                var clientIds = NetworkManager.Singleton.ConnectedClientsIds;
                Vector3[] positions = new Vector3[clientIds.Count];
                Quaternion[] rotations = new Quaternion[clientIds.Count];

                for (int i = 0; i < clientIds.Count; i++)
                {
                    positions[i] = spawnZone.GetRandomPoint();
                    rotations[i] = spawnZone.transform.rotation;
                }

                if (PlayerManager.Singleton != null)
                {
                    PlayerManager.Singleton.SpawnPlayers(positions, rotations);
                }
                else
                {
                    Debug.LogError("[GameManager] PlayerManager.Singleton not found!");
                }
            }
            else
            {
                Debug.LogWarning("[GameManager] No PlayerSpawnZone found in the loaded scene! Players cannot spawn.");
            }
        }

        public void StartGame(Game game)
        {
            if (!IsServer) return;
            Debug.Log($"[GameManager] Starting Game: {game.GameName}");
            // Additional game start logic (e.g., unlocking movement)
        }

        public void EndGame()
        {
            if (!IsServer) return;
            Debug.Log("[GameManager] Ending Game.");
            // Handle results, display leaderboard, etc.
        }
    }
}
