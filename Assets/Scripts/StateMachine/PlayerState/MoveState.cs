using System.Collections;
using System.Collections.Generic;
using FallGuys.StateMachine;
using UnityEngine;

[CreateAssetMenu(fileName = "New MoveState", menuName = "StateMachine/States/MoveState")]
public class MoveState : StateBaseSO
{
    [Header("Movement Settings")]
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float friction = 10f;
    [SerializeField] private float rotationSpeed = 10f;

    public override void OnServerUpdate(Blackboard bb)
    {
        if (!bb.IsServer) return;

        Vector2 directionInput = bb.Get<Vector2>("Direction");
        float cameraYaw = bb.Get<float>("CameraYaw");
        GameObject playerGameObject = bb.Get<GameObject>("PlayerGameObject");
        if (playerGameObject == null) return;

        Rigidbody rb = playerGameObject.GetComponent<Rigidbody>();

        if (rb == null) return;

        // 1. Calculate World Direction relative to Camera orientation
        Quaternion cameraRotation = Quaternion.Euler(0f, cameraYaw, 0f);
        Vector3 moveInput = new Vector3(directionInput.x, 0, directionInput.y);
        Vector3 moveDir = cameraRotation * moveInput;

        // 2. Physics-based Movement (Acceleration)
        if (moveDir.magnitude > 0.1f)
        {
            // Apply acceleration
            rb.AddForce(moveDir.normalized * acceleration * Time.deltaTime, ForceMode.VelocityChange);

            // Limit speed
            Vector3 horizontalVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            if (horizontalVel.magnitude > maxSpeed)
            {
                rb.velocity = horizontalVel.normalized * maxSpeed + Vector3.up * rb.velocity.y;
            }

            // 3. Smooth Rotation to face direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            playerGameObject.transform.rotation = Quaternion.Slerp(playerGameObject.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Clear any physics-based rotation to avoid interference
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            // Apply friction/drag when no input
            Vector3 horizontalVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.velocity -= horizontalVel * friction * Time.deltaTime;
        }
    }

    public override void OnClientEnter(Blackboard bb, System.Threading.CancellationToken ct)
    {
        Animator anim = bb.GetOwnerObject().GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetBool("Grounded", true);
            anim.SetBool("IsRecovering", false);
            anim.SetFloat("VerticalVelocity", 0f);

            // Safety: ensure no triggers are pending
            anim.ResetTrigger("DiveTrigger");
        }
    }

    public override void OnClientUpdate(Blackboard bb)
    {
        // VISUALS: Continuously enforce grounded state while in MoveState
        Animator anim = bb.GetOwnerObject().GetComponentInChildren<Animator>();
        if (anim == null) return;

        // Use normalized blackboard data (Decoupled from Player/Inputs components)
        float speed = bb.Get<float>("LocalMoveSpeed");

        anim.SetBool("Grounded", true);
        anim.SetBool("IsRecovering", false);
        anim.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        anim.SetFloat("VerticalVelocity", 0f, 0.1f, Time.deltaTime);
    }
}