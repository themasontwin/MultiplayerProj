using Unity.Netcode;
using UnityEngine;

namespace HelloWorld
{
    public class HelloWorldPlayer : NetworkBehaviour
    {
        public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();
        public float movementSpeed = 5f;
        private CharacterController controller;

        public override void OnNetworkSpawn()
        {
            Position.OnValueChanged += OnStateChanged;
            controller = GetComponent<CharacterController>();

            if (IsServer)
            {
                // Get spawn position based on player type
                Vector3 spawnPos = OwnerClientId == 0 ?
                    SpawnManager.Instance.hostSpawnPoint.position :
                    SpawnManager.Instance.clientSpawnPoint.position;

                spawnPos.y += 0.4f;

                Position.Value = spawnPos;
                transform.position = spawnPos;
            }
        }

        void Update()
        {
            if (!IsOwner) return;

            var moveDirection = new Vector3(
                Input.GetAxis("Horizontal"),
                0,
                Input.GetAxis("Vertical")
            );

            if (moveDirection != Vector3.zero)
            {
                MoveServerRpc(moveDirection);
            }
        }

        [Rpc(SendTo.Server)]
        void MoveServerRpc(Vector3 direction)
        {
            if (controller == null) return;
            direction = direction.normalized;
            Vector3 movement = new Vector3(direction.x, 0, direction.z) * movementSpeed * Time.deltaTime;
            controller.Move(movement);
            Position.Value = transform.position;

        }

        void OnStateChanged(Vector3 previous, Vector3 current)
        {
            transform.position = current;
        }
    }
}
