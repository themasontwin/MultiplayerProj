using Unity.Netcode;
using UnityEngine;

namespace HelloWorld
{
    public class HelloWorldPlayer : NetworkBehaviour
    {
        // Synced network variables
        public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();
        public NetworkVariable<bool> IsJumping = new NetworkVariable<bool>(false); // Synchronize jump state
        public NetworkVariable<int> Score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


        public float movementSpeed = 0.6f;
        public float jumpHeight = 0.3f; // Jump height above GROUND_Y
        public float jumpDuration = 0.5f; // Time to reach peak of jump
        
        private CharacterController controller;
        private float jumpTimer;
        private const float GROUND_Y = 0.4f; // Player prefab is fully above the ground

        public override void OnNetworkSpawn()
        {
            Position.OnValueChanged += OnStateChanged;
            IsJumping.OnValueChanged += OnJumpStateChanged; // Looks for jump state changes
            controller = GetComponent<CharacterController>();

            if (IsServer) // Server handles spawn positions
            {
                Vector3 spawnPos = OwnerClientId == 0 ?
                    SpawnManager.Instance.hostSpawnPoint.position :
                    SpawnManager.Instance.clientSpawnPoint.position;

                spawnPos.y = GROUND_Y;
                

                // Disables controller to put players in correct position
                controller.enabled = false;
                transform.position = spawnPos;
                controller.enabled = true;
                
                Position.Value = transform.position;
            }
        }

        void Update()
        {
            // Checks if you control your own player
            if (!IsOwner) return;

            var moveDirection = new Vector3(
                Input.GetAxis("Horizontal"),
                0,
                Input.GetAxis("Vertical")
            );

            if (Input.GetButtonDown("Jump") && !IsJumping.Value)
            {
                RequestJumpServerRpc();
            }

            MoveServerRpc(moveDirection);
        }

        [ServerRpc]
        private void RequestJumpServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsJumping.Value) // Only allow jumping if not already jumping
            {
                IsJumping.Value = true; // Set jump state on server
                jumpTimer = 0f; // Reset jump timer
            }
        }

        [ServerRpc]
        void MoveServerRpc(Vector3 direction)
        {
            if (controller == null) return;

            Vector3 movement = direction.normalized * movementSpeed * Time.deltaTime;

            if (IsJumping.Value) // Jump physics
            {
                jumpTimer += Time.deltaTime;
                float jumpProgress = jumpTimer / jumpDuration;

                if (jumpProgress < 1f)
                {
                    // Parabolic jump
                    float jumpY = GROUND_Y + jumpHeight * 4f * (jumpProgress - jumpProgress * jumpProgress);
                    movement.y = jumpY - transform.position.y;
                }
                else // End of jump
                {
                    IsJumping.Value = false; // Reset jumping state when done
                    movement.y = GROUND_Y - transform.position.y;
                }
            }
            else // Normal movement
            {
                movement.y = GROUND_Y - transform.position.y;
            }

            controller.Move(movement);
            
            Position.Value = transform.position;
        }

        private void OnStateChanged(Vector3 previous, Vector3 current)
        {
            transform.position = current;
        }


        // Makes sure Player lands back on the 0.4 ground level when jump ends
        private void OnJumpStateChanged(bool previous, bool current)
        {
            if (!current) 
            {
                Vector3 pos = transform.position;
                pos.y = GROUND_Y;
                transform.position = pos;
            }
        }
    }
}
