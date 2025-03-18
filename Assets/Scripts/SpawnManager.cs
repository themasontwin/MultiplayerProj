using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    public Transform hostSpawnPoint; // Where host spawns

    public Transform clientSpawnPoint; // Where client spawns


    void Awake()
    {

        // Makes sure only one spawn manager exists
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
