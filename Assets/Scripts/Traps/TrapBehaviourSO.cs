using Eraflo.Common.ObjectSystem;
using FallGuys.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps
{
    /// <summary>
    /// Base class for trap logic ScriptableObjects.
    /// </summary>
    public abstract class TrapBehaviourSO : SimpleBehaviourSO
    {
        public override void OnStart(BaseObject owner, Blackboard blackboard)
        {
            if (owner is TrapDetector detector)
            {
                detector.onTriggerEnter += (other) => OnTrapTriggerEnter(owner, blackboard, other);
                detector.onTriggerStay += (other) => OnTrapTriggerStay(owner, blackboard, other);
                detector.onTriggerExit += (other) => OnTrapTriggerExit(owner, blackboard, other);

                detector.onCollisionEnter += (collision) => OnAreaCollided(owner, blackboard, collision);
                detector.onCollisionStay += (collision) => OnAreaCollisionStay(owner, blackboard, collision);
                detector.onCollisionExit += (collision) => OnAreaCollisionExit(owner, blackboard, collision);
            }
        }

        protected virtual void OnTrapTriggerEnter(BaseObject owner, Blackboard blackboard, Collider other) { }
        protected virtual void OnTrapTriggerStay(BaseObject owner, Blackboard blackboard, Collider other) { }
        protected virtual void OnTrapTriggerExit(BaseObject owner, Blackboard blackboard, Collider other) { }

        protected virtual void OnAreaCollided(BaseObject owner, Blackboard blackboard, Collision collision) { }
        protected virtual void OnAreaCollisionStay(BaseObject owner, Blackboard blackboard, Collision collision) { }
        protected virtual void OnAreaCollisionExit(BaseObject owner, Blackboard blackboard, Collision collision) { }
        
        /// <summary>
        /// Utility to check if the collided object is in the impact layer and has a Rigidbody.
        /// </summary>
        protected bool IsValidTarget(GameObject target, TrapSO config, out Rigidbody rb)
        {
            rb = target.GetComponentInParent<Rigidbody>();
            if (rb == null) return false;
            
            return (config.ImpactLayer & (1 << target.layer)) != 0;
        }
    }
}
