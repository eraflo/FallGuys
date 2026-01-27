using System.Collections;
using System.Collections.Generic;
using FallGuys.StateMachine;
using UnityEngine;

[CreateAssetMenu(fileName = "New DiveState", menuName = "StateMachine/States/DiveState")]
public class DiveState : StateBaseSO
{
    [Header("Dive Settings")]
    [SerializeField] private float diveBoostForce = 15f;
    [SerializeField] private float diveDuration = 0.5f;

    public override void OnServerEnter(Blackboard bb, System.Threading.CancellationToken ct)
    {
        // AUTHORITY: The initial boost is applied only on the server
        if (!bb.IsServer) return;

        bb.Set("DiveStartTime", Time.time);
        bb.Set("IsDiveFinished", false);

        GameObject playerGameObject = bb.Get<GameObject>("PlayerGameObject");
        if (playerGameObject == null) return;

        Rigidbody rb = playerGameObject.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // Boost forward based on the player's current facing direction
            Vector3 boostDir = playerGameObject.transform.forward;
            rb.AddForce(boostDir * diveBoostForce, ForceMode.VelocityChange);
        }
    }

    public override void OnServerUpdate(Blackboard bb)
    {
        // AUTHORITY: Dive gravity and duration are server-authoritative
        if (!bb.IsServer) return;

        GameObject playerGameObject = bb.Get<GameObject>("PlayerGameObject");
        if (playerGameObject == null) return;

        Rigidbody rb = playerGameObject.GetComponent<Rigidbody>();
        if (rb == null) return;

        // Apply extra gravity during dive to make it feel "snappy"
        rb.velocity += Vector3.up * Physics.gravity.y * 2f * Time.deltaTime;

        // Check if we hit the ground during the dive (early exit)
        // We only consider it a landing if we are not moving upwards significantly
        // Robust Ground Check (Server side)
        bool isGrounded = bb.Get<bool>("IsGrounded");
        bool isLanding = isGrounded && rb.velocity.y <= 0.1f;

        // Check if dive phase is over (Timer OR actual Landing)
        float startTime = bb.Get<float>("DiveStartTime");
        if (Time.time - startTime > diveDuration || isLanding)
        {
            bb.Set("IsDiveFinished", true);
        }
    }

    public override void OnExit(Blackboard bb)
    {
        bb.Set("IsDiveFinished", false);
    }

    public override void OnClientEnter(Blackboard bb, System.Threading.CancellationToken ct)
    {
        Animator anim = bb.GetOwnerObject().GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("DiveTrigger");
            anim.SetBool("Grounded", false);
        }
    }

    public override void OnClientUpdate(Blackboard bb)
    {
        // VISUALS: Leaning in air
        Animator anim = bb.GetOwnerObject().GetComponentInChildren<Animator>();
        if (anim == null) return;

        // Provide speed parameter even during dive for visual tilt/leaning
        float speed = bb.Get<float>("DirectionMagnitude");
        anim.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
    }
}
