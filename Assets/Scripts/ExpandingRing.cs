using HelloWorld;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class ExpandingRing : NetworkBehaviour
{
    public float expandSpeed = 1.0f;  // Speed at which the ring expands
    public float maxSize = 10.0f;     // Maximum expansion size
    public float thickness = 0.5f;    // Thickness of the ring
    private float currentSize = 1.0f; // Initial ring size

    private Transform topWall, bottomWall, leftWall, rightWall;

    public NetworkVariable<ulong> ActivatorClientId = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private HashSet<ulong> scoredPlayers = new HashSet<ulong>();

    void Start()
    {
        // Find the four walls based on object names
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
        if (currentSize < maxSize)
        {
            currentSize += expandSpeed * Time.deltaTime;
            UpdateRing();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"ExpandingRing hit something: {other.gameObject.name}");

        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            var playerNetworkObject = other.GetComponent<NetworkObject>();
            if (playerNetworkObject == null) 
            {
                Debug.LogError($"ExpandingRing: No NetworkObject found on {other.gameObject.name}");
                return;
            }

            var player = playerNetworkObject.GetComponent<HelloWorldPlayer>();
            if (player == null) 
            {
                Debug.LogError($"ExpandingRing: No HelloWorldPlayer found on {other.gameObject.name}");
                return;
            }

            ulong hitClientId = playerNetworkObject.OwnerClientId;
            Debug.Log($"Player {hitClientId} hit by ring. Activator was {ActivatorClientId.Value}");

            if (hitClientId != ActivatorClientId.Value && !scoredPlayers.Contains(hitClientId))
            {
                scoredPlayers.Add(hitClientId);
                UpdateScoreServerRpc(hitClientId); // Send hit player ID instead of activator
            }
        }
    }



    [ServerRpc]
    private void UpdateScoreServerRpc(ulong hitClientId)
    {
        ulong targetClientId = (hitClientId == 0) ? 1UL : 0UL;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var targetClient))
        {
            var player = targetClient.PlayerObject.GetComponent<HelloWorldPlayer>();
            if (player != null)
            {
                player.Score.Value += 1;
                UpdateScoreClientRpc(targetClientId, player.Score.Value);
            }
        }
    }

    [ClientRpc]
    private void UpdateScoreClientRpc(ulong clientId, int newScore)
    {
        Debug.Log($"Updating score for client {clientId} to {newScore}");
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<HelloWorldPlayer>();
            if (localPlayer != null)
            {
                localPlayer.Score.Value = newScore;
            }
        }
    }

    void UpdateRing()
    {
        float halfSize = currentSize / 2;
        float halfThickness = thickness / 2;

        // Update horizontal walls (Top & Bottom)
        if (topWall && bottomWall)
        {
            topWall.localScale = new Vector3(currentSize + thickness, topWall.localScale.y, thickness);
            bottomWall.localScale = new Vector3(currentSize + thickness, bottomWall.localScale.y, thickness);

            topWall.localPosition = new Vector3(0, 0, halfSize);
            bottomWall.localPosition = new Vector3(0, 0, -halfSize);
        }

        // Update vertical walls (Left & Right)
        if (leftWall && rightWall)
        {
            leftWall.localScale = new Vector3(thickness, leftWall.localScale.y, currentSize + thickness);
            rightWall.localScale = new Vector3(thickness, rightWall.localScale.y, currentSize + thickness);

            leftWall.localPosition = new Vector3(-halfSize, 0, 0);
            rightWall.localPosition = new Vector3(halfSize, 0, 0);
        }
    }
}
