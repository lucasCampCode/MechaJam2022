using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSpringArm : MonoBehaviour
{
    public Camera camera;
    public bool attachCamera = true;
    public float springArmLength = 1f;

    void Update()
    {
        if (attachCamera)
        {
            Vector3 newPosition = transform.position + transform.forward * -springArmLength;
            camera.transform.position = newPosition;
            camera.transform.rotation = Quaternion.LookRotation(transform.forward, transform.up);
        }
    }
}
