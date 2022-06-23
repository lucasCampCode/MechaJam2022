using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSpringArm : MonoBehaviour
{
    public Camera camera;
    public bool attachCamera = true;
    public bool cameraLag = false;
    public float springArmLength = 1f;
    void Update()
    {
        if (attachCamera)
        {
            camera.transform.position = transform.position + transform.forward * -springArmLength;
            camera.transform.rotation = Quaternion.LookRotation(transform.forward, transform.up);
        }
    }
    Vector3 lerpLocation(Vector3 A, Vector3 B, float T)
    {
        float newX = Mathf.Lerp(A.x, B.x, T);
        float newY = Mathf.Lerp(A.y, B.y, T);
        float newZ = Mathf.Lerp(A.z, B.z, T);

        return new Vector3(newX, newY, newZ);
    }
}
