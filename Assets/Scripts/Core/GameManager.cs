using Eraflo.Common.LevelSystem;
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
        [SerializeField] private string _lobbySceneName = "LobbyScene";

        [Header("References")]
        [SerializeField] private LevelLoader _levelLoader;

        /// <summary>
        /// The level selected from the lobby UI. Set before launching game.
        /// </summary>
        public Level SelectedLevel { get; set; }

        /// <summary>
        /// The leaderboard for the current game session.
        /// </summary>
        public Leaderboard CurrentLeaderboard { get; private set; } = new Leaderboard();

        /// <summary>
        /// Timer tracking elapsed race time (server-side).
        /// </summary>
        public float RaceTimer { get; private set; }

        /// <summary>
        /// Whether the race is currently active.
        /// </summary>
        public bool RaceStarted { get; private set; }

        /// <summary>
        /// Whether the race has ended.
        /// </summary>
        public bool RaceEnded { get; private set; }

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

            if (SelectedLevel == null)
            {
                Debug.LogWarning("[GameManager] No level selected! Using empty level.");
                SelectedLevel = new Level("Default");
            }

            Debug.Log($"[GameManager] Launching Game with level: {SelectedLevel.LevelName}");

            // Reset game state for new race
            CurrentLeaderboard = new Leaderboard();
            RaceTimer = 0f;
            RaceStarted = false;
            RaceEnded = false;

            // Load the scene using NGO SceneManager for synchronization
            NetworkManager.Singleton.SceneManager.LoadScene(_gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        private void LoadLevel()
        {
            Debug.Log("[GameManager] LoadLevel: Spawning level objects...");

            // 1. Spawn level objects via LevelLoader, then spawn players
            if (_levelLoader != null && SelectedLevel != null)
            {
                _levelLoader.OnLevelLoaded += OnLevelObjectsLoaded;
                _levelLoader.LoadLevel(SelectedLevel);
            }
            else
            {
                // No level loader or level, spawn players immediately
                SpawnPlayers();
            }
        }

        private void OnLevelObjectsLoaded()
        {
            if (_levelLoader != null)
            {
                _levelLoader.OnLevelLoaded -= OnLevelObjectsLoaded;
            }

            Debug.Log("[GameManager] Level objects loaded. Spawning players...");
            SpawnPlayers();
        }

        private void SpawnPlayers()
        {
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

        private void Update()
        {
            if (!IsServer) return;

            // Increment race timer while race is active
            if (RaceStarted && !RaceEnded)
            {
                RaceTimer += Time.deltaTime;
            }
        }

        /// <summary>
        /// Called by StartAreaBehaviour when countdown finishes.
        /// </summary>
        public void StartRace()
        {
            if (!IsServer) return;
            RaceStarted = true;
            RaceTimer = 0f;
            Debug.Log("[GameManager] Race started!");
        }

        public void StartGame(Game game)
        {
            if (!IsServer) return;
            Debug.Log($"[GameManager] Starting Game: {game.GameName}");
        }

        /// <summary>
        /// Records a player finishing the race.
        /// </summary>
        public void RecordFinish(ulong clientId, string playerName, float finishTime)
        {
            if (!IsServer) return;
            CurrentLeaderboard.RecordFinish(clientId, playerName, finishTime);
            Debug.Log($"[GameManager] Player {playerName} finished at {finishTime:F2}s");

            // Check if all players finished (optional: end game automatically)
        }

        /// <summary>
        /// Called when the game ends (all players finished or time ran out).
        /// </summary>
        public void EndGame()
        {
            if (!IsServer) return;
            RaceEnded = true;
            Debug.Log("[GameManager] Ending Game.");

            // Show leaderboard via ClientRpc
            ShowEndRaceUIClientRpc();
        }

        [ClientRpc]
        private void ShowEndRaceUIClientRpc()
        {
            // Find and show the EndRaceUI
            var endRaceUI = Object.FindFirstObjectByType<EndRaceUI>();
            if (endRaceUI != null)
            {
                endRaceUI.Show(CurrentLeaderboard.GetRankedEntries());
            }
        }

        /// <summary>
        /// Returns all players to the lobby scene.
        /// </summary>
        public void ReturnToLobby()
        {
            if (!IsServer) return;

            // Unload level objects
            if (_levelLoader != null)
            {
                _levelLoader.UnloadLevel();
            }

            // Load lobby scene
            NetworkManager.Singleton.SceneManager.LoadScene(_lobbySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}
