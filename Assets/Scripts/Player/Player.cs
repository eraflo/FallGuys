using System.Collections;
using System.Collections.Generic;
using FallGuys.StateMachine;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public Camera PlayerCamera;
    public Transform CameraPivot; // The pivot that orbits the player

    [SerializeField] private float lookSpeed = 2f;
    private float _yaw; // Horizontal camera rotation (Orbit)
    private float _pitch; // Vertical camera rotation (Orbit)
    private Inputs _inputs;
    private NetworkStateMachine networkStateMachine;

    // Server-side input state
    private Vector2 _serverMoveDirection;
    private bool _serverIsJumping;
    private bool _serverIsDiving;
    private Vector2 _serverLookInput;
    private float _serverCameraYaw;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckOffset = 0.1f;
    [SerializeField] private float groundCheckDistance = 0.2f; // Distance below pivot
    [SerializeField] private LayerMask groundLayer = ~1; // Exclude 'TransparentFX' by default

    // Synchronize pitch to other clients
    private NetworkVariable<float> networkPitch = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Synchronize camera yaw (useful for knowing where others are looking, and for server move logic)
    private NetworkVariable<float> networkCameraYaw = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Sync move speed for animations
    private NetworkVariable<float> networkMoveSpeed = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> networkVerticalVelocity = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkIsGrounded = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// Last checkpoint position for respawn (server-authoritative).
    /// </summary>
    public Vector3 LastCheckpointPosition { get; set; } = Vector3.zero;

    /// <summary>
    /// Index of the last checkpoint reached (higher = further in race).
    /// </summary>
    public int LastCheckpointIndex { get; set; } = -1;

    public override void OnNetworkSpawn()
    {
        _inputs = GetComponent<Inputs>();
        networkStateMachine = GetComponent<NetworkStateMachine>();
        networkStateMachine.Blackboard.Set("PlayerGameObject", gameObject);

        SetupPlayer();

        if (IsServer)
        {
            LastCheckpointPosition = transform.position;
            LastCheckpointIndex = -1; // -1 means starting point/no checkpoint hit yet
        }

        if (IsOwner)
        {
            // Initialize camera angles based on pivot if it exists
            if (CameraPivot != null)
            {
                _yaw = CameraPivot.eulerAngles.y;
                _pitch = CameraPivot.eulerAngles.x;
            }
            UpdateCursorState();
        }

        // Sync visual pitch for other clients
        if (!IsOwner)
        {
            networkPitch.OnValueChanged += (oldVal, newVal) =>
            {
                if (CameraPivot != null) CameraPivot.localEulerAngles = new Vector3(newVal, networkCameraYaw.Value, 0f);
            };
            networkCameraYaw.OnValueChanged += (oldVal, newVal) =>
            {
                if (CameraPivot != null) CameraPivot.localEulerAngles = new Vector3(networkPitch.Value, newVal, 0f);
            };
        }
    }

    private void SetupPlayer()
    {
        Renderer playerRenderer = GetComponent<Renderer>();

        if (IsOwner)
        {
            // In Fall Guys, you definitely see your body.
            if (playerRenderer != null) playerRenderer.enabled = true;
            _inputs.Activate();
            _inputs.CamLock = true;
            if (PlayerCamera != null) PlayerCamera.enabled = true;
        }
        else
        {
            if (playerRenderer != null) playerRenderer.enabled = true;
            _inputs.Desactivate();
            if (PlayerCamera != null) PlayerCamera.enabled = false;
        }
    }

    private void UpdateCursorState()
    {
        if (_inputs.CamLock)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            UpdateCursorState();

            // Set IsOwner on blackboard for state logic/animations
            networkStateMachine.Blackboard.Set("IsOwner", true);

            // 1. Calculate and Apply WORLD-SPACE rotation for the camera
            if (_inputs.CamLock)
            {
                float lookX = Input.GetAxis("Mouse X") * lookSpeed;
                float lookY = Input.GetAxis("Mouse Y") * lookSpeed;

                _yaw += lookX;
                _pitch -= lookY;
                _pitch = Mathf.Clamp(_pitch, -80f, 80f); // Fall Guys style clamp

                if (CameraPivot != null)
                {
                    // By setting .rotation (World) instead of .localRotation,
                    // we decouple the camera from the character's body rotation.
                    CameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
                }
            }

            // 2. Send inputs to server
            UpdatePlayerInputsServerRpc(_inputs.MoveDirection, _inputs.IsJumping, _inputs.IsDiving,
                new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")), _yaw);
        }

        if (IsServer)
        {
            // Physics / Authority
            networkStateMachine.Blackboard.Set("Direction", _serverMoveDirection);
            networkStateMachine.Blackboard.Set("IsJump", _serverIsJumping);
            networkStateMachine.Blackboard.Set("IsDive", _serverIsDiving);
            networkStateMachine.Blackboard.Set("CameraYaw", _serverCameraYaw);
            networkStateMachine.Blackboard.Set("DeltaTime", Time.deltaTime);

            // Update sync variables for clients
            networkMoveSpeed.Value = _serverMoveDirection.magnitude;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) networkVerticalVelocity.Value = rb.velocity.y;

            // ROBUST CENTRALIZED GROUND CHECK
            Vector3 rayStart = transform.position + Vector3.up * groundCheckOffset;
            bool isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance + groundCheckOffset, groundLayer, QueryTriggerInteraction.Ignore);

            // Visual feedback in Editor
            Debug.DrawRay(rayStart, Vector3.down * (groundCheckDistance + groundCheckOffset), isGrounded ? Color.green : Color.red);
            networkIsGrounded.Value = isGrounded;
            networkStateMachine.Blackboard.Set("IsGrounded", isGrounded);
            networkStateMachine.Blackboard.Set("VerticalVelocity", networkVerticalVelocity.Value);

            if (!IsOwner) ServerLook(_serverLookInput, _serverCameraYaw);
            else { networkPitch.Value = _pitch; networkCameraYaw.Value = _yaw; }
        }

        // --- BLACKBOARD SYNC ---
        // We sync these on all clients so the current STATE can use them 
        // in its OnClientUpdate method for fine-grained animation control.
        if (IsClient)
        {
            // Owner uses local input for zero-latency animations, others use synced speed
            float localSpeed = IsOwner ? _inputs.MoveDirection.magnitude : networkMoveSpeed.Value;

            // Velocity handling: Owner can use local rb velocity, others use synced velocity
            float verticalVel = networkVerticalVelocity.Value;
            if (IsOwner)
            {
                Rigidbody rbLocal = GetComponent<Rigidbody>();
                if (rbLocal != null) verticalVel = rbLocal.velocity.y;
            }

            networkStateMachine.Blackboard.Set("LocalMoveSpeed", localSpeed);
            networkStateMachine.Blackboard.Set("DirectionMagnitude", networkMoveSpeed.Value);
            networkStateMachine.Blackboard.Set("IsGrounded", networkIsGrounded.Value);
            networkStateMachine.Blackboard.Set("VerticalVelocity", verticalVel);
        }
    }

    [ServerRpc]
    private void UpdatePlayerInputsServerRpc(Vector2 moveDir, bool jumping, bool diving, Vector2 lookInput, float currentYaw)
    {
        _serverMoveDirection = moveDir;
        _serverIsJumping = jumping;
        _serverIsDiving = diving;
        _serverLookInput = lookInput;
        _serverCameraYaw = currentYaw;
    }

    private void ServerLook(Vector2 lookInput, float cameraYaw)
    {
        float lookY = lookInput.y * lookSpeed;

        // Sync Yaw from client (World-Space)
        _yaw = cameraYaw;

        // Update pitch manually (needs to be clamped vertically)
        _pitch -= lookY;
        _pitch = Mathf.Clamp(_pitch, -80f, 80f);

        networkPitch.Value = _pitch;
        networkCameraYaw.Value = _yaw;

        if (CameraPivot != null)
        {
            CameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }
}
