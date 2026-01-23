using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public GameObject playerPrefab;     // Prefab du joueur
    public Transform[] spawnPoints;      // Points de spawn

    // Spawn un seul player
    public GameObject Spawn(GameObject playerPrefab)
    {
        int index = Random.Range(0, spawnPoints.Length);
        return Instantiate(
            playerPrefab,
            spawnPoints[index].position,
            Quaternion.identity
        );
    }

    // Spawn tous les players
    public void SpawnAll()
    {
        foreach (Transform point in spawnPoints)
        {
            Instantiate(playerPrefab, point.position, Quaternion.identity);
        }
    }
}
