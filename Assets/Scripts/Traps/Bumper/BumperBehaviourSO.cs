using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps.Bumper
{
    /// <summary>
    /// Logic for the Bumper trap. 
    /// Applies a sudden physical impulse to any valid target upon collision.
    /// </summary>
    [CreateAssetMenu(fileName = "BumperBehaviour", menuName = "Traps/Behaviours/Bumper")]
    public class BumperBehaviourSO : TrapBehaviourSO
    {
        protected override void OnAreaCollided(BaseObject owner, Blackboard blackboard, Collision collision)
        {
            // IMPORTANT: Only the server calculates and applies physics impulses.
            // This ensures authoritative behavior and prevents "double-bumping" or desyncs.
            if (!blackboard.IsServer) return;

            BumperTrapSO config = owner.RuntimeData.Config as BumperTrapSO;
            if (config == null) return;

            // Check if what we hit is actually a player (or valid target)
            if (IsValidTarget(collision.gameObject, config, out Rigidbody rb))
            {
                // Calculate push direction: From the center of the trap towards the point of impact.
                Vector3 pushDir = collision.contacts[0].point - owner.transform.position;

                // Keep it horizontal (Y=0) to prevent players from flying upwards unexpectedly
                // like a pinball bumper.
                pushDir.y = 0;
                pushDir.Normalize();

                // Apply the force as an Impulse (instantaneous velocity change)
                rb.AddForce(pushDir * config.Strength, ForceMode.Impulse);

                Debug.Log($"[Bumper] Server applied bump to {collision.gameObject.name} (Strength: {config.Strength})");
            }
        }
    }
}
