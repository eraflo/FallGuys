using Eraflo.Common.ObjectSystem;
using FallGuys.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps
{
    /// <summary>
    /// Base class for all trap logic ScriptableObjects.
    /// Provides automatic subscription to TrapDetector events (Triggers and Collisions).
    /// </summary>
    public abstract class TrapBehaviourSO : SimpleBehaviourSO
    {
        public override void OnStart(BaseObject owner, Blackboard blackboard)
        {
            // Automatically link the detector events to virtual methods for subclasses
            // Trigger events (Used by zones like Blowers or Launcher detection)
            owner.onTriggerEnter += (other) => OnTrapTriggerEnter(owner, blackboard, other);
            owner.onTriggerStay += (other) => OnTrapTriggerStay(owner, blackboard, other);
            owner.onTriggerExit += (other) => OnTrapTriggerExit(owner, blackboard, other);

            // Collision events (Used by physical impacts like Bumpers)
            owner.onCollisionEnter += (collision) => OnAreaCollided(owner, blackboard, collision);
            owner.onCollisionStay += (collision) => OnAreaCollisionStay(owner, blackboard, collision);
            owner.onCollisionExit += (collision) => OnAreaCollisionExit(owner, blackboard, collision);
        }

        // Virtual hooks for subclasses to implement their specific trap logic
        protected virtual void OnTrapTriggerEnter(BaseObject owner, Blackboard blackboard, Collider other) { }
        protected virtual void OnTrapTriggerStay(BaseObject owner, Blackboard blackboard, Collider other) { }
        protected virtual void OnTrapTriggerExit(BaseObject owner, Blackboard blackboard, Collider other) { }

        protected virtual void OnAreaCollided(BaseObject owner, Blackboard blackboard, Collision collision) { }
        protected virtual void OnAreaCollisionStay(BaseObject owner, Blackboard blackboard, Collision collision) { }
        protected virtual void OnAreaCollisionExit(BaseObject owner, Blackboard blackboard, Collision collision) { }

        /// <summary>
        /// Global utility to check if an object is a valid target for a trap.
        /// It MUST have a Rigidbody (to be affected by physics) and be on the correct Layer.
        /// </summary>
        /// <param name="target">The GameObject to check</param>
        /// <param name="config">The trap configuration containing the ImpactLayer mask</param>
        /// <param name="rb">The output Rigidbody if found</param>
        /// <returns>True if the target should be affected by the trap</returns>
        protected bool IsValidTarget(GameObject target, TrapSO config, out Rigidbody rb)
        {
            // We search in parents to find the root player Rigidbody even if we hit a sub-collider
            rb = target.GetComponentInParent<Rigidbody>();
            if (rb == null) return false;

            // Bitwise check against the LayerMask defined in the asset
            return (config.ImpactLayer & (1 << target.layer)) != 0;
        }
    }
}
