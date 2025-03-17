using UnityEngine;
using Unity.Netcode;

public class PressurePlate : NetworkBehaviour
{
    public GameObject ringBeamPrefab;
    public Transform spawnPoint;
    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger entered by: {other.gameObject.name}");

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player detected on pressure plate");

            if (!isTriggered)
            {
                Debug.Log("Attempting to spawn RingBeam");
                isTriggered = true;

                if (ringBeamPrefab != null && spawnPoint != null)
                {
                    SpawnRingBeamServerRpc();
                }
                else
                {
                    Debug.LogError("RingBeam prefab or spawn point is null");
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnRingBeamServerRpc()
    {
        GameObject spawnedRingBeam = Instantiate(ringBeamPrefab, spawnPoint.position, Quaternion.identity);
        spawnedRingBeam.GetComponent<NetworkObject>().Spawn();
        Debug.Log("RingBeam spawned");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isTriggered = false;
            Debug.Log("Player exited pressure plate");
        }
    }
}
