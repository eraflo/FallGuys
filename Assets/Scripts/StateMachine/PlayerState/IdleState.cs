using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FallGuys.StateMachine;
using ObjectSystem;

[CreateAssetMenu(fileName = "New IdleState", menuName = "StateMachine/States/IdleState")]
public class IdleStateState : StateBaseSO
{
    public override void OnServerUpdate(Blackboard bb)
    {
      
       
      
    }

    public override void OnClientUpdate(Blackboard bb)
    {
        // Visual logic (Clients only)
    }
}