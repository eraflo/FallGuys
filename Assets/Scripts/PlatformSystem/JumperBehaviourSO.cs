using Eraflo.Common.ObjectSystem;
using Eraflo.Common.PlatformSystem;
using FallGuys.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.PlatformSystem
{
    /// <summary>
    /// Logic for the Trampoline (Jumper) platform.
    /// Applies a vertical impulse when a player enters the top trigger zone.
    /// </summary>
    [CreateAssetMenu(fileName = "JumperBehaviour", menuName = "Platforms/Behaviours/Jumper")]
    public class JumperBehaviourSO : PlatformBehaviourSO
    {
        protected override void OnPlatformTriggerEnter(BaseObject owner, Blackboard blackboard, Collider other)
        {
            // Physics impulses are authoritative on the server
            if (!blackboard.IsServer) return;

            JumperSO config = owner.RuntimeData.Config as JumperSO;
            if (config == null) return;

            if (IsPlayer(other.gameObject, out Rigidbody rb))
            {
                // Reset Y velocity for a consistent jump height regardless of how the player landed
                Vector3 vel = rb.velocity;
                vel.y = 0;
                rb.velocity = vel;

                // Apply vertical impulse
                rb.AddForce(owner.transform.up * config.JumpStrength, ForceMode.Impulse);

                Debug.Log($"[Jumper] Sever applied jump impulse to {other.gameObject.name}");
            }
        }
    }
}
