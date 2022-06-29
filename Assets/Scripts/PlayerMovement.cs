using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    private ControlSceme _control;
    public float speed;
    public float jumpHeight = 2;
    private float gravityMuliplayer = 1f;

    private Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _control = new ControlSceme();
        _control.Player.Jump.performed += Jump_performed;
    }

    private void Jump_performed(InputAction.CallbackContext obj)
    {
        float jumpforce = Mathf.Sqrt(jumpHeight * 2 * Physics.gravity.y);
        _rb.AddForce(jumpforce * transform.up);

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

        _rb.AddForce(move * Time.deltaTime);
    }
    public void ToggleGravity()
    {
        if (gravityMuliplayer == 0)
            gravityMuliplayer = 1;
        else
            gravityMuliplayer = 0;
    }
}
