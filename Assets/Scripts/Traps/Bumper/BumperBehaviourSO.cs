using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps.Bumper
{
    [CreateAssetMenu(fileName = "BumperBehaviour", menuName = "Traps/Behaviours/Bumper")]
    public class BumperBehaviourSO : TrapBehaviourSO
    {
        protected override void OnAreaCollided(BaseObject owner, Blackboard blackboard, Collision collision)
        {
            // Only server handles physics impulses for authority
            if (!blackboard.IsServer) return;

            BumperTrapSO config = owner.RuntimeData.Config as BumperTrapSO;
            if (config == null) return;

            if (IsValidTarget(collision.gameObject, config, out Rigidbody rb))
            {
                // Calculate push direction: from trap center to contact point
                // Or simply use collision normal (inverted)
                Vector3 pushDir = collision.contacts[0].point - owner.transform.position;
                pushDir.y = 0; // Keep it horizontal for standard bumper feel
                pushDir.Normalize();

                // Apply impulse
                rb.AddForce(pushDir * config.Strength, ForceMode.Impulse);
                
                Debug.Log($"[Bumper] Bumped {collision.gameObject.name} with force {config.Strength}");
            }
        }
    }
}
