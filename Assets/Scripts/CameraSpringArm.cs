using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSpringArm : MonoBehaviour
{
    public Transform AttachPoint;
    public bool attachCamera = true;
    public float springArmLength = 1f;
    private float trueLength = 0f;

    void FixedUpdate()
    {
        if (attachCamera)
        {

            Vector3 newPosition = AttachPoint.position + AttachPoint.forward * -trueLength;
            transform.position = newPosition;
            transform.rotation = Quaternion.LookRotation(AttachPoint.forward, AttachPoint.up);
        }
    }
}
