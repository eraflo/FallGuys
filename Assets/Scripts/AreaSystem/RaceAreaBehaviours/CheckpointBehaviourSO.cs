using Eraflo.Common.AreaSystem;
using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using Unity.Netcode;
using UnityEngine;

namespace FallGuys.AreaSystem
{
    [CreateAssetMenu(fileName = "CheckpointAreaBehaviour", menuName = "FallGuys/Areas/Behaviours/Checkpoint Area")]
    public class CheckpointAreaBehaviourSO : AreaBehaviourSO
    {
        protected override void OnAreaEnter(BaseObject owner, Blackboard blackboard, Collider other)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            // Detect player using Player script
            var player = other.GetComponentInParent<Player>();
            if (player == null) return;

            // IMPORTANT: Read from Blackboard to get the calculated (and overridden) CheckpointIndex
            int checkpointIndex = blackboard.Get<int>("_checkpointIndex", 0);

            // Only update if this checkpoint is further than player's current progress
            if (checkpointIndex > player.LastCheckpointIndex)
            {
                player.LastCheckpointIndex = checkpointIndex;
                player.LastCheckpointPosition = owner.transform.position;
                Debug.Log($"[Race] CHECKPOINT: Player_{player.OwnerClientId} reached checkpoint #{checkpointIndex} at {owner.transform.position}");
            }
        }

        protected override void OnAreaStay(BaseObject owner, Blackboard blackboard, Collider other) { }
        protected override void OnAreaExit(BaseObject owner, Blackboard blackboard, Collider other) { }
    }
}
