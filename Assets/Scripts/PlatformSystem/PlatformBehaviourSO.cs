using Eraflo.Common.ObjectSystem;
using FallGuys.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.PlatformSystem
{
    /// <summary>
    /// Base class for all platform logic ScriptableObjects.
    /// Handles automatic event subscription to the BaseObject's colliders.
    /// </summary>
    public abstract class PlatformBehaviourSO : SimpleBehaviourSO
    {
        public override void OnStart(BaseObject owner, Blackboard blackboard)
        {
            // Automatically subscribe to trigger and collision events
            owner.onTriggerEnter += (other) => OnPlatformTriggerEnter(owner, blackboard, other);
            owner.onTriggerStay += (other) => OnPlatformTriggerStay(owner, blackboard, other);
            owner.onTriggerExit += (other) => OnPlatformTriggerExit(owner, blackboard, other);

            owner.onCollisionEnter += (collision) => OnPlatformCollided(owner, blackboard, collision);
            owner.onCollisionStay += (collision) => OnPlatformCollisionStay(owner, blackboard, collision);
            owner.onCollisionExit += (collision) => OnPlatformCollisionExit(owner, blackboard, collision);
        }

        // Virtual hooks for platform-specific logic
        protected virtual void OnPlatformTriggerEnter(BaseObject owner, Blackboard blackboard, Collider other) { }
        protected virtual void OnPlatformTriggerStay(BaseObject owner, Blackboard blackboard, Collider other) { }
        protected virtual void OnPlatformTriggerExit(BaseObject owner, Blackboard blackboard, Collider other) { }

        protected virtual void OnPlatformCollided(BaseObject owner, Blackboard blackboard, Collision collision) { }
        protected virtual void OnPlatformCollisionStay(BaseObject owner, Blackboard blackboard, Collision collision) { }
        protected virtual void OnPlatformCollisionExit(BaseObject owner, Blackboard blackboard, Collision collision) { }

        /// <summary>
        /// Utility to check if an object is a player and retrieve its Rigidbody.
        /// </summary>
        protected bool IsPlayer(GameObject target, out Rigidbody rb)
        {
            rb = target.GetComponentInParent<Rigidbody>();
            // In our project, players are typically on the "Player" layer
            // For now, if it has a Rigidbody and it's not the platform itself, we consider it a target.
            return rb != null && !target.transform.IsChildOf(target.transform.root);
        }
    }
}
