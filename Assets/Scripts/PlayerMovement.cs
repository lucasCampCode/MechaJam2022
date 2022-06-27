using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private ControlSceme _control;
    public float speed;

    private float gravityMuliplayer;

    private CharacterController _charControl;
    private float _gravity = -9.81f;
    private Vector3 velocity;

    void Start()
    {
        _charControl = GetComponent<CharacterController>();
        _control = new ControlSceme();
        _control.Player.Jump.performed += Jump_performed;
    }

    private void Jump_performed(InputAction.CallbackContext obj)
    {
    }

    private void OnEnable()
    {
        _control.Player.Enable();
    }
    private void OnDisable()
    {
        _control.Player.Disable();
    }

    private void FixedUpdate()
    {
        Vector2 moveVal = _control.Player.Move.ReadValue<Vector2>();
        Vector3 move = transform.forward * moveVal.y + transform.right * moveVal.x;
        move *= speed;
        move += transform.up * _gravity * gravityMuliplayer;

        _charControl.Move(move * Time.deltaTime);
    }
    public void ToggleGravity()
    {
        if (gravityMuliplayer == 0)
            gravityMuliplayer = 1;
        else
            gravityMuliplayer = 0;
    }
}
