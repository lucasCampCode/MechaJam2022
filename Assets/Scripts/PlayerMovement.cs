using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Camera")]
    public Animator playerCam;
    public Transform cameraRoot;
    [Header("Physics gravity")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.4f;
    public float _gravity = -9.81f;
    public float jumpHeight = 2f;
    [Header("Player Physics")]
    public float cameraSensitivity = 0f;
    public float playerSpeed = 0f;
    [Tooltip("how much faster should the character move while Sprinting")]
    public float speedMultiplier = 2f;
    [Tooltip("how much faster should the character move while in air")]
    public float InAirMultiplier = 0.9f;

    private CharacterController _controller;
    private float xRotation = 0f;
    private Vector2 moveInput;
    private Vector3 _velocity;
    private bool isGrounded;
    private bool isSprinting;
    public float Velocity{get { return _velocity.y; }}
    void Start()
    {
        _controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void FixedUpdate()
    {
        isGrounded = IsGrounded(Physics.OverlapSphere(groundCheck.position, groundCheckRadius));
        //reset gravity velocity
        if (isGrounded && _velocity.y < 0)
            _velocity.y = -2f;
        //move player based on player input
        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;
        //speed multiplication
        move *= playerSpeed;
        if (isSprinting)
            move *= speedMultiplier;
        if (!isGrounded)
            move *= InAirMultiplier;

        _controller.Move(move * Time.deltaTime);
        //set animation walking perameter, based on if the player is moving
        playerCam.SetBool("IsWalking", move.magnitude > 0f);
        //apply gravity
        _velocity.y += _gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
    public void MovePlayer(InputAction.CallbackContext ctx)
    {
        //grab player input
        moveInput = ctx.ReadValue<Vector2>();
    }
    public void LookInput(InputAction.CallbackContext ctx)
    {
        //grab player input
        Vector2 input = ctx.ReadValue<Vector2>();
        //rotate character on it y axis when player moves its mouse on the x axis
        transform.Rotate(transform.up, input.x * cameraSensitivity * Time.deltaTime);
        //rotate camera on it x axis when player moves its mouse on the y axis
        xRotation -= input.y * cameraSensitivity * Time.deltaTime;
        //clamp the rotation
        xRotation = Mathf.Clamp(xRotation,-90,90);
        //set the new rotation for the camera
        cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
    public void Jump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && isGrounded)
        {
            JumpCall(jumpHeight);
        }
    }
    public void JumpCall(float height)
    {
        //change gravity velocity when the player jumps
        _velocity.y = Mathf.Sqrt(-2 * _gravity * height);
    }
    public void Sprint(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            isSprinting = true;
        }
        if (ctx.canceled)
        {
            isSprinting = false;
        }
    }
    private bool IsGrounded(Collider[] colliders)
    {
        foreach (Collider col in colliders)
        {
            if (col.gameObject != gameObject)//if any thing get hit that is not the player itself then return true
                return true;
        }
        return false;
    }
}

