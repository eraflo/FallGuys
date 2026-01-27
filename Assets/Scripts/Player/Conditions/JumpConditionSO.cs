using System.Collections;
using System.Collections.Generic;
using FallGuys.StateMachine;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "JumpConditionSO", menuName = "StateMachine/Conditions/Jump")]
public class JumpConditionSO : ConditionSO
{
    public override bool IsMet(Blackboard bb)
    {
        if (!bb.IsServer) return false;

        bool isJumpPressed = bb.Get<bool>("IsJump");
        bool isGrounded = bb.Get<bool>("IsGrounded");

        return isJumpPressed && isGrounded;
    }
}
