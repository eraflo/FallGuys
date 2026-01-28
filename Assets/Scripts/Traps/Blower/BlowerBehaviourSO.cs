using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps.Blower
{
    [CreateAssetMenu(fileName = "BlowerBehaviour", menuName = "Traps/Behaviours/Blower")]
    public class BlowerBehaviourSO : TrapBehaviourSO
    {
        public override void OnStart(BaseObject owner, Blackboard blackboard)
        {
            base.OnStart(owner, blackboard);

            BlowerTrapSO config = owner.RuntimeData.Config as BlowerTrapSO;
            if (config != null && config.ParticlePrefab != null)
            {
                // Particles are visual only, so they can run on all clients
                Instantiate(config.ParticlePrefab, owner.transform);
            }
        }

        protected override void OnTrapTriggerStay(BaseObject owner, Blackboard blackboard, Collider other)
        {
            // Only server applies forces for authority
            if (!blackboard.IsServer) return;

            BlowerTrapSO config = owner.RuntimeData.Config as BlowerTrapSO;
            if (config == null) return;

            if (IsValidTarget(other.gameObject, config, out Rigidbody rb))
            {
                // Apply force in the forward direction of the trap
                rb.AddForce(owner.transform.forward * config.WindStrength, ForceMode.Acceleration);
            }
        }
    }
}
