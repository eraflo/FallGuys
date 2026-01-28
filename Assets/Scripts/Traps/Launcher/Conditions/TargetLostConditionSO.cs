using FallGuys.StateMachine;
using UnityEngine;
using System.Collections.Generic;

namespace FallGuys.Traps.Launcher.Conditions
{
    [CreateAssetMenu(fileName = "TargetLostCondition", menuName = "StateMachine/Conditions/Launcher/TargetLost")]
    public class TargetLostConditionSO : ConditionSO
    {
        public override bool IsMet(Blackboard bb)
        {
            var targets = bb.Get<List<Transform>>("_targets");
            return targets == null || targets.Count == 0;
        }
    }
}
