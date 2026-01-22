using System.Collections.Generic;
using UnityEngine;

namespace FallGuys.StateMachine
{
    /// <summary>
    /// A registry asset that maps State ScriptableObjects to unique network IDs (list indices).
    /// Used for synchronizing state changes across the network efficiently.
    /// </summary>
    [CreateAssetMenu(fileName = "StateConfig", menuName = "StateMachine/StateConfig")]
    public class StateConfigSO : ScriptableObject
    {
        [Tooltip("The ordered list of all possible states for a machine.")]
        public List<StateBaseSO> states;

        /// <summary>
        /// Retrieves a state asset based on its network ID.
        /// </summary>
        public StateBaseSO GetStateByID(int id)
        {
            if (id >= 0 && id < states.Count)
            {
                return states[id];
            }
            return null;
        }

        /// <summary>
        /// Retrieves the network ID (index) for a specific state asset.
        /// </summary>
        public int GetIDByState(StateBaseSO state)
        {
            return states.IndexOf(state);
        }
    }
}
