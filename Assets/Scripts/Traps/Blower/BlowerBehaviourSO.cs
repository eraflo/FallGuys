using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps.Blower
{
    /// <summary>
    /// Logic for the Blower (Wind) trap.
    /// Applies a constant force (acceleration) while a player stays within the trigger zone.
    /// </summary>
    [CreateAssetMenu(fileName = "BlowerBehaviour", menuName = "Traps/Behaviours/Blower")]
    public class BlowerBehaviourSO : TrapBehaviourSO
    {
        public override void OnStart(BaseObject owner, Blackboard blackboard)
        {
            base.OnStart(owner, blackboard);

            BlowerTrapSO config = owner.RuntimeData.Config as BlowerTrapSO;
            if (config != null && config.ParticlePrefab != null)
            {
                // Visual particles are spawned locally on ALL clients for better performance/visuals.
                // They don't affect gameplay logic, just feedback.
                Instantiate(config.ParticlePrefab, owner.transform);
            }
        }

        protected override void OnTrapTriggerStay(BaseObject owner, Blackboard blackboard, Collider other)
        {
            // IMPORTANT: Forces affecting player movement must be applied ONLY by the server.
            if (!blackboard.IsServer) return;

            BlowerTrapSO config = owner.RuntimeData.Config as BlowerTrapSO;
            if (config == null) return;

            // Apply wind if target is valid
            if (IsValidTarget(other.gameObject, config, out Rigidbody rb))
            {
                // Apply force in the forward direction of the trap.
                // Use ForceMode.Acceleration to ensure it's independent of the player's mass (cleaner feel).
                rb.AddForce(owner.transform.forward * config.WindStrength, ForceMode.Acceleration);
            }
        }
    }
}
