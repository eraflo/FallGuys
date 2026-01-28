using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.ObjectSystem
{
    /// <summary>
    /// Base class for simple, non-state-machine logic.
    /// Used for objects that have only one behavior (e.g., Simple Bumpers, Wind zones).
    /// </summary>
    public abstract class SimpleBehaviourSO : LogicIdentitySO
    {
        /// <summary>
        /// Called when the object is initialized.
        /// Use this to register for events or setup initial data in the blackboard.
        /// </summary>
        public virtual void OnStart(BaseObject owner, Blackboard blackboard) { }

        /// <summary>
        /// Called every frame while the object is active.
        /// </summary>
        public virtual void OnUpdate(BaseObject owner, Blackboard blackboard) { }
    }
}