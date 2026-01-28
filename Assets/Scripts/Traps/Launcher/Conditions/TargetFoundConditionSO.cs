using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps.Launcher.Conditions
{
    /// <summary>
    /// Condition to check if a valid target has been found.
    /// Used to transition from Searching to Tracking/Orienting.
    /// </summary>
    [CreateAssetMenu(fileName = "TargetFoundCondition", menuName = "StateMachine/Conditions/Launcher/TargetFound")]
    public class TargetFoundConditionSO : ConditionSO
    {
        public override bool IsMet(Blackboard bb)
        {
            // LauncherSearchStateSO actively scans and fills the "Target" key
            return bb.Get<Transform>("Target") != null;
        }
    }
}
