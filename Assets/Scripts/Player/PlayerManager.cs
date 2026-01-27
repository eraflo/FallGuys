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
    }

    public GameObject playerPrefab;     // Prefab du joueur
    public Transform[] spawnPoints;      // Points de spawn

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        if (!IsServer) return;
        SpawnPlayers();
    }

    [ContextMenu("Spawn Players Now")] // Allows triggering from Inspector for testing
    public void SpawnPlayers()
    {
        if (!IsServer) return;

        Debug.Log("[PlayerManager] Spawning all connected players...");
        foreach (var client in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayer(client);
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        int index = (int)(clientId % (ulong)spawnPoints.Length);
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoints[index].position, Quaternion.identity);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(clientId);
    }

    // Spawn tous les players (historique/manuel)
    [ServerRpc]
    public void SpawnAllServerRpc()
    {
        foreach (Transform point in spawnPoints)
        {
            GameObject playerInstance = Instantiate(playerPrefab, point.position, Quaternion.identity);
            playerInstance.GetComponent<NetworkObject>().Spawn();
        }
    }
}
