using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FallGuys.StateMachine;
using Unity.Netcode;

[CreateAssetMenu(fileName = "IdletoMoveConditionSO", menuName = "StateMachine/Conditions/IdletoMove")]
public class IdletoMoveConditionSO : ConditionSO
{
    public override bool IsMet(Blackboard bb)
    {
        Vector2 Direction = bb.Get<Vector2>("Direction");
        return Direction != Vector2.zero && NetworkManager.Singleton.IsServer;
    }
}