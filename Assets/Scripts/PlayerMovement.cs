using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public Transform cameraRoot;
    public PlayerSettingsSO settings;
    private Rigidbody _rb;
    private float xRotation = 0f;
    private Vector2 moveInput;
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }
    private void FixedUpdate()
    {
        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;
        move *= settings.playerSpeed;
        _rb.AddForce(move * Time.deltaTime,ForceMode.VelocityChange);
    }
    public void MovePlayer(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }
    public void LookInput(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>();
        transform.Rotate(transform.up, input.x * settings.cameraSensitivity * Time.deltaTime);
        xRotation -= input.y * settings.cameraSensitivity * Time.deltaTime;
        xRotation = Mathf.Clamp(xRotation,-90,90);
        cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    public void Jump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            _rb.velocity = new Vector3(_rb.velocity.x, Mathf.Sqrt(-2 * Physics.gravity.y * settings.jumpHeight), _rb.velocity.z);
        }
    }
}

