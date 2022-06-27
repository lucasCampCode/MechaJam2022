using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSpringArm : MonoBehaviour
{
    public Camera camera;
    public bool attachCamera = true;
    public float springArmLength = 1f;
    private float trueLength = 0f;

    void Update()
    {
        if (attachCamera)
        {
            Vector3 newPosition = transform.position + transform.forward * -trueLength;
            camera.transform.position = newPosition;
            camera.transform.rotation = Quaternion.LookRotation(transform.forward, transform.up);
        }
    }
    private void FixedUpdate()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward * -1, out hit, springArmLength))
        {
            trueLength = hit.distance;
        }
        else
            trueLength = springArmLength;
    }
}
