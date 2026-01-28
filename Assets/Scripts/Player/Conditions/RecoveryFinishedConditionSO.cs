using System.Collections;
using System.Collections.Generic;
using FallGuys.StateMachine;
using UnityEngine;

[CreateAssetMenu(fileName = "RecoveryFinishedConditionSO", menuName = "StateMachine/Conditions/RecoveryFinished")]
public class RecoveryFinishedConditionSO : ConditionSO
{
    public override bool IsMet(Blackboard bb)
    {
        return bb.IsServer && bb.Get<bool>("RecoveryFinished");
    }
}
