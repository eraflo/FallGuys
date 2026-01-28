using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace FallGuys.StateMachine
{
    /// <summary>
    /// Represents a transition rule between two states.
    /// Evaluated only on the server.
    /// </summary>
    [Serializable]
    public class Transition
    {
        [Tooltip("All conditions must be true (AND logic).")]
        public List<ConditionSO> conditions;

        [Tooltip("The state to switch to if all conditions are met.")]
        public StateBaseSO targetState;

        /// <summary>
        /// Evaluates if the transition should be triggered.
        /// </summary>
        /// <param name="bb">The blackboard containing instance data.</param>
        /// <returns>True if all conditions are met.</returns>
        public bool Evaluate(Blackboard bb)
        {
            if (conditions == null || conditions.Count == 0) return true;

            foreach (var condition in conditions)
            {
                if (condition == null) continue;
                if (!condition.IsMet(bb)) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Base class for all ScriptableObject-based states.
    /// </summary>
    public abstract class StateBaseSO : ScriptableObject
    {
        [Header("Transitions")]
        [Tooltip("List of possible exits from this state. Evaluated in order.")]
        public List<Transition> transitions;

        /// <summary>
        /// Called when the state is entered on both Server and Clients.
        /// </summary>
        public virtual void OnEnter(Blackboard bb, CancellationToken ct) { }

        public virtual void OnServerEnter(Blackboard bb, CancellationToken ct) { }
        public virtual void OnClientEnter(Blackboard bb, CancellationToken ct) { }

        /// <summary>
        /// Called every frame on both Server and Clients.
        /// </summary>
        public virtual void OnUpdate(Blackboard bb) { }

        /// <summary>
        /// Called every frame ONLY on the Server.
        /// </summary>
        public virtual void OnServerUpdate(Blackboard bb) { }

        /// <summary>
        /// Called every frame ONLY on Clients.
        /// </summary>
        public virtual void OnClientUpdate(Blackboard bb) { }

        /// <summary>
        /// Called when the state is exited on both Server and Clients.
        /// </summary>
        public virtual void OnExit(Blackboard bb) { }

        public virtual void OnServerExit(Blackboard bb) { }
        public virtual void OnClientExit(Blackboard bb) { }

        /// <summary>
        /// Called when a generic action/intent is received via RPC.
        /// </summary>
        public virtual void OnActionReceived(Blackboard bb, string actionName) { }
    }
}
