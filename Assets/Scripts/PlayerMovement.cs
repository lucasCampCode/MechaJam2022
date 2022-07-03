using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float speed;
    public float jumpHeight = 2;
    public Transform cameraRoot;
    private Rigidbody _rb;
    private Vector3 moveForce;
    private float xRotation = 0f;
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }
    private void FixedUpdate()
    {
        _rb.AddForce(moveForce,ForceMode.Force);
    }
    public void MovePlayer(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>();
        Vector3 move = transform.forward * input.y + transform.right * input.x;
        moveForce = move * speed;
    }
    public void LookInput(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>();
        transform.Rotate(transform.up, input.x * Time.deltaTime);
        xRotation -= input.y * Time.deltaTime;
        xRotation = Mathf.Clamp(xRotation,-90,90);
        cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    public void Jump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            _rb.velocity = new Vector3(_rb.velocity.x, Mathf.Sqrt(-2 * Physics.gravity.y * jumpHeight), _rb.velocity.z);
        }
    }
}

