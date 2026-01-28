using UnityEngine;
using UnityEngine.InputSystem;

public class Inputs : MonoBehaviour
{
    public InputActionReference moveActionRef;
    public InputActionReference jumpActionRef;
    public InputActionReference camLockActionRef;
    public InputActionReference diveActionRef;

    public Vector2 MoveDirection;
    public bool IsJumping;
    public bool IsDiving;
    public bool CamLock = true;

    private bool activated = true;

    private void Start()
    {
        // On startup, we make sure the global actions are enabled.
        // We do it once here.
        Activate();
        CamLock = true;
    }

    void OnEnable()
    {
        // Caution: If multiple players exist, they share the same moveActionRef.action object.
        // We shouldn't Disable() it when one player is destroyed if others still need it.
        Activate();
    }

    public void Activate()
    {
        // Enabling the action is a global state for the asset. 
        // We only do it to ensure the system is "listening".
        if (moveActionRef != null && !moveActionRef.action.enabled) moveActionRef.action.Enable();
        if (jumpActionRef != null && !jumpActionRef.action.enabled) jumpActionRef.action.Enable();
        if (camLockActionRef != null && !camLockActionRef.action.enabled) camLockActionRef.action.Enable();
        if (diveActionRef != null && !diveActionRef.action.enabled) diveActionRef.action.Enable();

        activated = true;
    }

    public void Desactivate()
    {
        activated = false;

        // Reset values to prevent "stuck" inputs
        MoveDirection = Vector2.zero;
        IsJumping = false;
        IsDiving = false;
    }

    private void Update()
    {
        // Only the "Activated" instance (the Owner) reads values from the global system.
        if (!activated) return;

        if (moveActionRef != null)
            MoveDirection = moveActionRef.action.ReadValue<Vector2>();

        if (jumpActionRef != null)
            IsJumping = jumpActionRef.action.IsPressed();

        if (diveActionRef != null)
            IsDiving = diveActionRef.action.triggered;

        if (camLockActionRef != null && camLockActionRef.action.triggered)
        {
            CamLock = !CamLock;
        }
    }
}
