using AreaSystem;
using Eraflo.Common.AreaSystem;
using FallGuys.StateMachine;
using ObjectSystem;
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
            Debug.Log("[Race] CHECKPOINT: Reached (Server)");
        }

        protected override void OnAreaStay(BaseObject owner, Blackboard blackboard, Collider other) { }
        protected override void OnAreaExit(BaseObject owner, Blackboard blackboard, Collider other) { }
    }
}
