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
        CamLock = true;
    }

    void OnEnable()
    {
        Activate();
    }

    void OnDisable()
    {
        Desactivate();
    }

    public void Activate()
    {
        if (moveActionRef != null) moveActionRef.action.Enable();
        if (jumpActionRef != null) jumpActionRef.action.Enable();
        if (camLockActionRef != null) camLockActionRef.action.Enable();
        if (diveActionRef != null) diveActionRef.action.Enable();
        activated = true;
    }

    public void Desactivate()
    {
        if (moveActionRef != null) moveActionRef.action.Disable();
        if (jumpActionRef != null) jumpActionRef.action.Disable();
        if (camLockActionRef != null) camLockActionRef.action.Disable();
        if (diveActionRef != null) diveActionRef.action.Disable();
        activated = false;
    }

    private void Update()
    {
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
