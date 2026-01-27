using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public GameObject playerPrefab;     // Prefab du joueur
    public Transform[] spawnPoints;      // Points de spawn

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // The NetworkManager usually handles player spawning via the PlayerPrefab property,
            // but if we want custom spawn points or manual spawning, we do it here.
            // For NGO, if a PlayerPrefab is set in NetworkManager, it spawns automatically.
            // If the user wants to use THIS PlayerManager for manual spawning:
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        SpawnPlayer(clientId);
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
