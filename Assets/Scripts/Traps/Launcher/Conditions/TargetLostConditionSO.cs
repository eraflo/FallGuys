using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps.Launcher.Conditions
{
    /// <summary>
    /// Condition to check if the current target is no longer valid.
    /// Used to exit Tracking/Firing states and return to Searching.
    /// </summary>
    [CreateAssetMenu(fileName = "TargetLostCondition", menuName = "StateMachine/Conditions/Launcher/TargetLost")]
    public class TargetLostConditionSO : ConditionSO
    {
        public override bool IsMet(Blackboard bb)
        {
            // The Launcher states (Orient, Fire, Search) are responsible for 
            // verifying target validity (Range + Angle) every frame on the server.
            // If the target is no longer valid, they set the blackboard "Target" to null.

            // This condition simply reacts to that state change.
            return bb.Get<Transform>("Target") == null;
        }
    }
}
