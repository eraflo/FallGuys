using System.Collections;
using System.Collections.Generic;
using FallGuys.StateMachine;
using UnityEngine;

[CreateAssetMenu(fileName = "DiveConditionSO", menuName = "StateMachine/Conditions/Dive")]
public class DiveConditionSO : ConditionSO
{
    public override bool IsMet(Blackboard bb)
    {
        // Must be on server and Dive input must be triggered
        return bb.IsServer && bb.Get<bool>("IsDive");
    }
}
