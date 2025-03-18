using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PressurePlate : NetworkBehaviour
{
    public GameObject ringBeamPrefab;
    public Transform spawnPoint; // Where ring beam will spawn
    public float minCooldownTime = 3f;
    public float maxCooldownTime = 10f;
    public float initialDelay = 5f; // Time before the plate turns green once both players have joined

    private NetworkVariable<bool> isActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private Renderer plateRenderer;
    private Color activeColor = Color.green; // Ready to fire Ring Beam
    private Color inactiveColor = Color.gray;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Initialize the plate after the NetworkObject is spawned
        plateRenderer = GetComponent<Renderer>();

        // Subscribe to changes in isActive
        isActive.OnValueChanged += OnActiveStateChanged;

        // Turn the plate green after an initial delay
        if (IsServer)
            Invoke(nameof(ActivatePlate), initialDelay);
        UpdateColor();
    }

    private void Start()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        //Check to make sure its the server
        if (!IsServer) return;

        // Check if the pressure plate is active and it's the player

        if (other.CompareTag("Player") && isActive.Value)
        {
            ActivatePlateServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivatePlateServerRpc(ServerRpcParams rpcParams = default)
    {
        if (isActive.Value)
        {
            //Set isActive to false since plate is being pressed
            isActive.Value = false;

            //Spawns the ringbeam from this
            SpawnRingBeam(rpcParams.Receive.SenderClientId);

            //restart the randomized cooldown between 3 and 10 seconds
            StartCoroutine(CooldownCoroutine());
        }
    }

    private void SpawnRingBeam(ulong activatorClientId)
    {
        if (ringBeamPrefab != null && spawnPoint != null) // Checks if ring beam prefab and spawn point object are assigned
        {
            GameObject spawnedRingBeam = Instantiate(ringBeamPrefab, spawnPoint.position, Quaternion.identity);
            var ringBeam = spawnedRingBeam.GetComponent<ExpandingRing>();
            ringBeam.ActivatorClientId.Value = activatorClientId;
            spawnedRingBeam.GetComponent<NetworkObject>().Spawn();
        }
    }

    private IEnumerator CooldownCoroutine()
    {
        //Wait the minimum cooldown Time
        yield return new WaitForSeconds(minCooldownTime);
        
        // Waits a random amount of time between max and min cooldown
        float additionalWaitTime = Random.Range(0, maxCooldownTime - minCooldownTime);
        yield return new WaitForSeconds(additionalWaitTime);

        //Activate the place after cooldown
        ActivatePlate();
        isActive.Value = true;
    }

    
    private void ActivatePlate()
    {
        isActive.Value = true;
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
