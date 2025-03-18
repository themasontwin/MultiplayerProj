using HelloWorld;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class ExpandingRing : NetworkBehaviour
{
    public float expandSpeed = 1.0f;  
    public float maxSize = 10.0f;     
    public float thickness = 0.5f;    
    private float currentSize = 1.0f; // Starting size for the ring

    private Transform topWall, bottomWall, leftWall, rightWall;

    // Network variable to check which player activated the ring
    public NetworkVariable<ulong> ActivatorClientId = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Keeps track of players scored on this ring
    private HashSet<ulong> scoredPlayers = new HashSet<ulong>();

    void Start()
    {
        // Walls based on object names
        topWall = transform.Find("Top");
        bottomWall = transform.Find("Bottom");
        leftWall = transform.Find("Left");
        rightWall = transform.Find("Right");

        foreach(Transform child in transform)
        {
            var collider = child.gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
        }
    }

    void Update()
    {
        // Keep expanding until it reaches max size
        if (currentSize < maxSize)
        {
            currentSize += expandSpeed * Time.deltaTime;
            UpdateRing();
        }
    }

    // When ring collides with something
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"ExpandingRing hit something: {other.gameObject.name}");

        if (!IsServer) 
        {
            Debug.Log("Not server, returning");
            return;
        }

        // Checks if collision is with a player
        if (other.CompareTag("Player"))
        {
            var playerNetworkObject = other.GetComponent<NetworkObject>();
            if (playerNetworkObject == null) 
            {
                Debug.LogError($"ExpandingRing: No NetworkObject found on {other.gameObject.name}");
                return;
            }

            ulong hitClientId = playerNetworkObject.OwnerClientId;
            Debug.Log($"Player {hitClientId} hit by ring. Activator was {ActivatorClientId.Value}");

            // Allow scoring even if hitClientId is the same as ActivatorClientId
            if (!scoredPlayers.Contains(hitClientId))
            {
                Debug.Log($"Calling UpdateScoreServerRpc with hitClientId: {hitClientId}");
                scoredPlayers.Add(hitClientId);
                UpdateScoreServerRpc(hitClientId);
            }
            else
            {
                Debug.Log($"Not updating score. hitClientId: {hitClientId}, Already scored: {scoredPlayers.Contains(hitClientId)}");
            }
        }
        else
        {
            Debug.Log($"Hit object is not a player: {other.gameObject.name}");
        }
    }

    [ServerRpc]
    private void UpdateScoreServerRpc(ulong hitClientId)
    {
        // The player who doesn't get hit scores
        ulong scoringClientId = (hitClientId == ActivatorClientId.Value) ? GetOtherPlayerId(hitClientId) : ActivatorClientId.Value;

        Debug.Log($"UpdateScoreServerRpc called. Hit player: {hitClientId}, Scoring player: {scoringClientId}");

        // Tries to find the scoring player
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(scoringClientId, out var scoringClient))
        {
            var scoringPlayer = scoringClient.PlayerObject.GetComponent<HelloWorldPlayer>();
            if (scoringPlayer != null)
            {
                scoringPlayer.Score.Value += 1;
                Debug.Log($"Increased score for player {scoringClientId}. New score: {scoringPlayer.Score.Value}");

                // Inform all clients about the updated score
                UpdateScoreClientRpc(scoringClientId, scoringPlayer.Score.Value);
            }
            else
            {
                Debug.LogError($"HelloWorldPlayer component not found for client {scoringClientId}");
            }
        }
        else
        {
            Debug.LogError($"Client {scoringClientId} not found in ConnectedClients");
        }
    }

    [ClientRpc]
    private void UpdateScoreClientRpc(ulong clientId, int newScore)
    {
        Debug.Log($"UpdateScoreClientRpc: Updating score for client {clientId} to {newScore}");

        // Find the HelloWorldManager and update the UI
        var manager = Object.FindFirstObjectByType<HelloWorldManager>();
        if (manager != null)
        {
            manager.UpdateScoreDisplay(clientId, 0, newScore);
        }
        else
        {
            Debug.LogError("HelloWorldManager not found in the scene");
        }
    }

    private ulong GetOtherPlayerId(ulong currentPlayerId)
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Key != currentPlayerId)
                return client.Key;
        }

        Debug.LogError("GetOtherPlayerId: Could not find another player!");
        return currentPlayerId; // Fallback to avoid errors
    }

    void UpdateRing()
    {
        float halfSize = currentSize / 2;
        float halfThickness = thickness / 2;

        // Updates Top and Bottom
        if (topWall && bottomWall)
        {
            topWall.localScale = new Vector3(currentSize + thickness, topWall.localScale.y, thickness);
            bottomWall.localScale = new Vector3(currentSize + thickness, bottomWall.localScale.y, thickness);

            topWall.localPosition = new Vector3(0, 0, halfSize);
            bottomWall.localPosition = new Vector3(0, 0, -halfSize);
        }

        // Updates Left and Right
        if (leftWall && rightWall)
        {
            leftWall.localScale = new Vector3(thickness, leftWall.localScale.y, currentSize + thickness);
            rightWall.localScale = new Vector3(thickness, rightWall.localScale.y, currentSize + thickness);

            leftWall.localPosition = new Vector3(-halfSize, 0, 0);
            rightWall.localPosition = new Vector3(halfSize, 0, 0);
        }
    }
}
