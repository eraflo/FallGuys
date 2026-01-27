using System.Collections;
using System.Collections.Generic;
using System.Threading;
using FallGuys.StateMachine;
using UnityEngine;

[CreateAssetMenu(fileName = "New JumpState", menuName = "StateMachine/States/JumpState")]
public class JumpState : StateBaseSO
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2.0f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2.0f;
    [SerializeField] private LayerMask groundLayer = ~0; // Default to all

    [Header("Air Control")]
    [SerializeField] private float airControlAcceleration = 20f;
    [SerializeField] private float maxAirSpeed = 6f;

    public override void OnServerEnter(Blackboard bb, CancellationToken ct)
    {
        if (!bb.IsServer) return;

        GameObject playerGameObject = bb.Get<GameObject>("PlayerGameObject");
        if (playerGameObject == null) return;

        Rigidbody rb = playerGameObject.GetComponent<Rigidbody>();
        if (rb == null) return;

        // Record jump start time for LandConditionSO stability
        bb.Set("JumpStartTime", Time.time);

        // Perform the jump impulse immediately on enter
        float jumpVelocity = Mathf.Sqrt(2 * jumpHeight * Mathf.Abs(Physics.gravity.y));
        rb.velocity = new Vector3(rb.velocity.x, jumpVelocity, rb.velocity.z);
    }

    public override void OnServerUpdate(Blackboard bb)
    {
        // AUTHORITY: Air-control physics are calculated only on the server
        if (!bb.IsServer) return;

        GameObject playerGameObject = bb.Get<GameObject>("PlayerGameObject");
        if (playerGameObject == null) return;

        Rigidbody rb = playerGameObject.GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector2 directionInput = bb.Get<Vector2>("Direction");
        float cameraYaw = bb.Get<float>("CameraYaw");
        bool isJumping = bb.Get<bool>("IsJump");

        // 1. Horizontal Air Control (Relative to Camera)
        if (directionInput.magnitude > 0.1f)
        {
            Quaternion cameraRotation = Quaternion.Euler(0f, cameraYaw, 0f);
            Vector3 airMoveDir = cameraRotation * new Vector3(directionInput.x, 0, directionInput.y);

            rb.AddForce(airMoveDir.normalized * airControlAcceleration * Time.deltaTime, ForceMode.VelocityChange);

            // Limit air speed
            Vector3 horizontalVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            if (horizontalVel.magnitude > maxAirSpeed)
            {
                rb.velocity = horizontalVel.normalized * maxAirSpeed + Vector3.up * rb.velocity.y;
            }
        }

        // 2. Responsive Jump Physics (Gamey feel)
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.velocity.y > 0 && !isJumping)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
    }

    public override void OnClientUpdate(Blackboard bb)
    {
        Animator anim = bb.GetOwnerObject().GetComponentInChildren<Animator>();
        if (anim == null) return;

        Rigidbody rb = bb.GetOwner<Rigidbody>();
        float yVel = rb != null ? rb.velocity.y : 0f;

        float speed = bb.Get<float>("LocalMoveSpeed");

        bool isGrounded = bb.Get<bool>("IsGroundedSync");

        anim.SetBool("Grounded", isGrounded);
        anim.SetFloat("VerticalVelocity", yVel);
        anim.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
    }
}