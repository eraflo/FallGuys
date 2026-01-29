using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using Unity.Netcode;
using UnityEngine;

namespace FallGuys.AreaSystem
{
    [CreateAssetMenu(fileName = "DeadZoneAreaBehaviour", menuName = "FallGuys/Areas/Behaviours/DeadZone Area")]
    public class DeadZoneAreaBehaviourSO : AreaBehaviourSO
    {
        protected override void OnAreaEnter(BaseObject owner, Blackboard blackboard, Collider other)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            // Detect player using Player script
            var player = other.GetComponentInParent<Player>();
            if (player == null) return;

            Debug.Log($"[Race] DEAD ZONE: Player_{player.OwnerClientId} fell into dead zone!");

            // Get respawn position from Player's last checkpoint
            Vector3 respawnPos = player.LastCheckpointPosition;

            if (respawnPos == Vector3.zero)
            {
                // No checkpoint - use spawn position or default
                Debug.LogWarning($"[Race] DEAD ZONE: No checkpoint for Player_{player.OwnerClientId}, using default respawn.");
                respawnPos = new Vector3(0, 5, 0);
            }

            // Teleport player to last checkpoint (with offset to avoid ground clip)
            player.transform.position = respawnPos + Vector3.up * 1f;

            // Reset velocity
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log($"[Race] DEAD ZONE: Player_{player.OwnerClientId} respawned at {respawnPos}");
        }

        protected override void OnAreaStay(BaseObject owner, Blackboard blackboard, Collider other) { }
        protected override void OnAreaExit(BaseObject owner, Blackboard blackboard, Collider other) { }
    }
}
