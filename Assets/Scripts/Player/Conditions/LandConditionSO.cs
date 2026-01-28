using System.Collections;
using System.Collections.Generic;
using FallGuys.StateMachine;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "LandConditionSO", menuName = "StateMachine/Conditions/Land")]
public class LandConditionSO : ConditionSO
{
    [SerializeField] private float minAirTime = 0.1f; // Don't land immediately after jumping

    public override bool IsMet(Blackboard bb)
    {
        if (!bb.IsServer) return false;

        // Prevent landing logic from running too soon after a jump starts
        if (bb.Has("JumpStartTime"))
        {
            float timeInAir = Time.time - bb.Get<float>("JumpStartTime");
            if (timeInAir < minAirTime) return false;
        }

        GameObject playerGameObject = bb.Get<GameObject>("PlayerGameObject");
        if (playerGameObject == null) return false;

        Rigidbody rb = playerGameObject.GetComponent<Rigidbody>();
        if (rb == null) return false;

        bool isGrounded = bb.Get<bool>("IsGrounded");

        // AUTHORITY: Only land if we are grounded AND not moving upwards
        return isGrounded && rb.velocity.y <= 0.1f;
    }
}
