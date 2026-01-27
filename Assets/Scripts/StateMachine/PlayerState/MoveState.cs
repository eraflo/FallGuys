using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FallGuys.StateMachine;
using ObjectSystem;

[CreateAssetMenu(fileName = "New MoveState", menuName = "StateMachine/States/MoveState")]
public class MoveState : StateBaseSO
{
    public override void OnServerUpdate(Blackboard bb)
    {
        // Authoritative logic (Server only)
        // var owner = bb.GetOwner<BaseObject>();
        if(!bb.IsServer) return; 

        Vector2 Direction = bb.Get<Vector2>("Direction");
        GameObject PlayerGameObject = bb.Get<GameObject>("PlayerGameObject");
        float dt = bb.Get<float>("DeltaTime");
        PlayerGameObject.transform.position += new Vector3(Direction.x, 0, Direction.y) * dt;
      
    }

    public override void OnClientUpdate(Blackboard bb)
    {
        // Visual logic (Clients only)
    }
}