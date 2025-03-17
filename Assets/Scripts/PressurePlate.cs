using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PressurePlate : NetworkBehaviour
{
    public GameObject ringBeamPrefab;
    public Transform spawnPoint;
    public float minCooldownTime = 3f;
    public float maxCooldownTime = 10f;
    public float initialDelay = 5f; // Time before the plate turns green initially
    
    private NetworkVariable<bool> isActive = new NetworkVariable<bool>(false);
    private Renderer plateRenderer;
    private Color activeColor = Color.green;
    private Color inactiveColor = Color.gray;

    private void Start()
    {
        plateRenderer = GetComponent<Renderer>();
        UpdateColor();
        
        // Turn the plate green after an initial delay
        Invoke(nameof(ActivatePlate), initialDelay);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isActive.Value)
        {
            ActivatePlateServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivatePlateServerRpc()
    {
        if (isActive.Value)
        {
            isActive.Value = false;
            SpawnRingBeam();
            StartCoroutine(CooldownCoroutine());
        }
    }

    private void SpawnRingBeam()
    {
        if (ringBeamPrefab != null && spawnPoint != null)
        {
            GameObject spawnedRingBeam = Instantiate(ringBeamPrefab, spawnPoint.position, Quaternion.identity);
            spawnedRingBeam.GetComponent<NetworkObject>().Spawn();
            Debug.Log("RingBeam spawned");
        }
        else
        {
            Debug.LogError("RingBeam prefab or spawn point is null");
        }
    }

    private IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(minCooldownTime);
        
        float additionalWaitTime = Random.Range(0, maxCooldownTime - minCooldownTime);
        yield return new WaitForSeconds(additionalWaitTime);

        isActive.Value = true;
    }

    private void ActivatePlate()
    {
        isActive.Value = true;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isActive.OnValueChanged += OnActiveStateChanged;
        UpdateColor();
    }

    private void OnActiveStateChanged(bool previousValue, bool newValue)
    {
        UpdateColor();
    }

    private void UpdateColor()
    {
        if (plateRenderer != null)
        {
            plateRenderer.material.color = isActive.Value ? activeColor : inactiveColor;
        }
    }
}
