using System.Collections;
using System.Collections.Generic;
using FallGuys.StateMachine;
using UnityEngine;

[CreateAssetMenu(fileName = "IsDiveFinishedConditionSO", menuName = "StateMachine/Conditions/IsDiveFinished")]
public class IsDiveFinishedConditionSO : ConditionSO
{
    public override bool IsMet(Blackboard bb)
    {
        return bb.IsServer && bb.Get<bool>("IsDiveFinished");
    }
}
