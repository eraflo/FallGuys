using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FallGuys.StateMachine;
using UnityEngine.Networking;
using Unity.Netcode;

public class Player : NetworkBehaviour
{   
    public Camera PlayerCamera;
   
    private float lookSpeed = 0.1f;
    private bool _inputCamLock;
    private float _yaw;
    private float _pitch;
    private Vector2 _inputLook;
    private Renderer playerRenderer;
    private Inputs _inputs;
    private NetworkStateMachine networkStateMachine;
    
    public override void OnNetworkSpawn()
    {
        _inputs = GetComponent<Inputs>();
        networkStateMachine = GetComponent<NetworkStateMachine>();
        networkStateMachine.Blackboard.Set("PlayerGameObject", gameObject);

        SetupPlayer();
    }

    public void DisableLocalInput()
    {
        _inputs.Desactivate();
    }



    private void SetupPlayer()
    {
        playerRenderer = GetComponent<Renderer>();

        if (IsOwner)
        {
            if (playerRenderer != null) playerRenderer.enabled = false;
            ToggleCursor(true);
        }
        else
        {
            if (playerRenderer != null) playerRenderer.enabled = true;
            DisableLocalInput();
            if (PlayerCamera != null) PlayerCamera.enabled = false;

        }
    }

    private void SendInputToServer()
    {
        
    }

    void ToggleCursor(bool _inputCamLock)
    {
       
    }
    // Update is called once per frame
    void Update()
    {
        if(!NetworkManager.Singleton.IsServer) return;

        networkStateMachine.Blackboard.Set("Direction",_inputs.MoveDirection);
        networkStateMachine.Blackboard.Set("IsJump",_inputs.IsJumping);
        networkStateMachine.Blackboard.Set("DeltaTime", Time.deltaTime);

        if (IsOwner)
        {
            SendInputToServer();
            ToggleCursor(_inputCamLock);
        }
    }


private void ServerLook()
{
    if (!_inputCamLock)
    {
        _inputLook = Vector2.zero;
        return;
    }
    float lookX = _inputLook.x * lookSpeed;
    float lookY = _inputLook.y * lookSpeed;
    _inputLook = Vector2.zero;

    //corps rotation
    _yaw += lookX;
    transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
    
    //rotatio camera
    _pitch -= lookX;
    _pitch = Mathf.Clamp(_pitch, -90f, 90f);

    if (PlayerCamera != null)
        PlayerCamera.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);

}
}

