using UnityEngine;
using UnityEngine.InputSystem;

public class Inputs : MonoBehaviour
{
    public InputActionReference moveActionRef;
    public InputActionReference jumpActionRef;
    
    public Vector2 MoveDirection;
    public bool IsJumping;

    private bool activated = true;


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
        moveActionRef.action.Enable();
        jumpActionRef.action.Enable();
        activated = true;
    }

    public void Desactivate()
    {
        moveActionRef.action.Disable();
        jumpActionRef.action.Disable();
        activated = false;
    }

    private void Update()
    {
        if(!activated) return;
        MoveDirection = moveActionRef.action.ReadValue<Vector2>();
        IsJumping = jumpActionRef.action.ReadValue<bool>();
    }
}
