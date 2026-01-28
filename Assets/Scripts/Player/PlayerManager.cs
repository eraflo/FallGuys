using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Singleton { get; private set; }

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        Singleton = this;
        DontDestroyOnLoad(this);
    }

    public GameObject playerPrefab;     // Prefab of player

    /// <summary>
    /// Spawns all connected players at the specified spawn points.
    /// </summary>
    /// <param name="points">The spawn points to use.</param>
    public void SpawnPlayers(Transform[] points)
    {
        if (!IsServer) return;

        if (points == null || points.Length == 0)
        {
            Debug.LogError("[PlayerManager] Cannot spawn players: No spawn points provided!");
            return;
        }

        var clientIds = NetworkManager.Singleton.ConnectedClientsIds;
        Vector3[] positions = new Vector3[clientIds.Count];
        Quaternion[] rotations = new Quaternion[clientIds.Count];

        int i = 0;
        foreach (var clientId in clientIds)
        {
            int index = (int)(clientId % (ulong)points.Length);
            positions[i] = points[index].position;
            rotations[i] = points[index].rotation;
            i++;
        }

        SpawnPlayers(positions, rotations);
    }

    /// <summary>
    /// Spawns all connected players at the specified positions and rotations.
    /// This is the core spawning logic.
    /// </summary>
    public void SpawnPlayers(Vector3[] positions, Quaternion[] rotations)
    {
        if (!IsServer) return;

        var clientIds = NetworkManager.Singleton.ConnectedClientsIds;
        if (positions.Length < clientIds.Count)
        {
            Debug.LogError("[PlayerManager] Not enough positions provided for all connected players!");
            return;
        }

        Debug.Log($"[PlayerManager] Spawning {clientIds.Count} players...");

        int i = 0;
        foreach (var clientId in clientIds)
        {
            SpawnPlayer(clientId, positions[i], rotations[i]);
            i++;
        }
    }

    private void SpawnPlayer(ulong clientId, Vector3 position, Quaternion rotation)
    {
        GameObject playerInstance = Instantiate(playerPrefab, position, rotation);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(clientId);
        
        Debug.Log($"[PlayerManager] Spawned player {clientId} at {position}");
    }
}
