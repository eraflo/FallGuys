using UnityEngine;

namespace FallGuys.StateMachine
{
    /// <summary>
    /// Base class for all transition conditions defined as ScriptableObjects.
    /// Conditions are evaluated on the Server to determine state changes.
    /// </summary>
    public abstract class ConditionSO : ScriptableObject
    {
        /// <summary>
        /// Evaluates the condition against the current state of the entity.
        /// </summary>
        /// <param name="bb">The blackboard containing instance data.</param>
        /// <returns>True if the condition is satisfied.</returns>
        public abstract bool IsMet(Blackboard bb);
    }
}
