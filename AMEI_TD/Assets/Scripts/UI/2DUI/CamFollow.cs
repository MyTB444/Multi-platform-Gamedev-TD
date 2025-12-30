using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFollow : MonoBehaviour
{
    [Header("Setup")]
    public Vector3 referenceObjectPosition = new Vector3(17.86f, 4.33f, 11.9f);
    public Vector3 referenceCameraPosition = new Vector3(28.62f, 19f, 11.3f);

    void Start()
    {
        // Calculate the look direction from the reference object to the camera
        Vector3 referenceDirection = referenceCameraPosition - referenceObjectPosition;

        // Apply the same direction from THIS object's position
        Quaternion rotation = Quaternion.LookRotation(referenceDirection.normalized, Vector3.up);
        transform.rotation = rotation;
    }
}

