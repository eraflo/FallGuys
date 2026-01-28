using System.Collections;
using System.Collections.Generic;
using FallGuys.StateMachine;
using UnityEngine;

[CreateAssetMenu(fileName = "New RecoveryState", menuName = "StateMachine/States/RecoveryState")]
public class RecoveryState : StateBaseSO
{
    [Header("Recovery Settings")]
    [SerializeField] private float recoveryDuration = 1.0f;
    [SerializeField] private float slideFriction = 10f;
    [SerializeField] private float movementRestriction = 0.2f; // Limited control

    public override void OnServerEnter(Blackboard bb, System.Threading.CancellationToken ct)
    {
        // AUTHORITY: Server decides when recovery starts
        if (!bb.IsServer) return;

        bb.Set("RecoveryFinished", false);
        bb.Set("RecoveryStartTime", Time.time);
    }

    public override void OnServerUpdate(Blackboard bb)
    {
        // AUTHORITY: Physics of the belly-slide are server-authoritative
        if (!bb.IsServer) return;

        float startTime = bb.Get<float>("RecoveryStartTime");
        if (Time.time - startTime >= recoveryDuration)
        {
            bb.Set("RecoveryFinished", true);
        }

        GameObject playerGameObject = bb.Get<GameObject>("PlayerGameObject");
        if (playerGameObject == null) return;

        Rigidbody rb = playerGameObject.GetComponent<Rigidbody>();
        if (rb == null) return;

        // Apply sliding friction and small control
        Vector2 directionInput = bb.Get<Vector2>("Direction");
        float cameraYaw = bb.Get<float>("CameraYaw");

        // Rotate the move input by the camera yaw
        Quaternion cameraRotation = Quaternion.Euler(0f, cameraYaw, 0f);
        Vector3 moveInput = cameraRotation * new Vector3(directionInput.x, 0, directionInput.y);

        // Apply force to the rigidbody
        rb.AddForce(moveInput * movementRestriction * 10f * Time.deltaTime, ForceMode.VelocityChange);

        // Apply friction to the rigidbody
        Vector3 horizontalVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.velocity -= horizontalVel * slideFriction * Time.deltaTime;
    }

    public override void OnExit(Blackboard bb)
    {
        bb.Set("RecoveryFinished", false);
    }

    public override void OnClientEnter(Blackboard bb, System.Threading.CancellationToken ct)
    {
        Animator anim = bb.GetOwnerObject().GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetBool("IsRecovering", true);
            anim.SetBool("Grounded", true);
        }
    }

    public override void OnClientExit(Blackboard bb)
    {
        Animator anim = bb.GetOwnerObject().GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetBool("IsRecovering", false);
        }
    }

    public override void OnClientUpdate(Blackboard bb)
    {
        // VISUALS: Only handle parameters that change every frame
        Animator anim = bb.GetOwnerObject().GetComponentInChildren<Animator>();
        if (anim == null) return;

        float speed = bb.Get<float>("DirectionMagnitude");
        anim.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
    }
}
