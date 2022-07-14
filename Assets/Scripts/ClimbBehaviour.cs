using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbBehaviour : MonoBehaviour
{
    public PlayerMovement movementScript;
    public float ClimbHeight = 2.0f;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ledge") && movementScript.Velocity > -1)
            movementScript.JumpCall(ClimbHeight);
    }
}
