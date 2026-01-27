using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FallGuys.StateMachine;
using ObjectSystem;

[CreateAssetMenu(fileName = "New JumpState", menuName = "StateMachine/States/JumpState")]
public class JumpState : StateBaseSO
{
    public override void OnServerUpdate(Blackboard bb)
    {
        // Authoritative logic (Server only)
        // var owner = bb.GetOwner<BaseObject>();
        
        bool Jump = bb.Get<bool>("IsJump");
       
      
    }

    public override void OnClientUpdate(Blackboard bb)
    {
        // Visual logic (Clients only)
    }
}