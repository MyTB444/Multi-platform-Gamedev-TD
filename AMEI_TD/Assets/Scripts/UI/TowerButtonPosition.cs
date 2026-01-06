using UnityEngine;

public class RotationSelectorGizmo : MonoBehaviour
{
    public enum RotationOption
    {
        ForwardTiltPositive, // (10, 0, 0)
        ForwardTiltNegative, // (-10, 0, 0)
        YawRight,            // (0, 0, 10)
        YawLeft              // (0, 0, -10)
    }

    public RotationOption selectedRotation = RotationOption.ForwardTiltPositive;
    public Color gizmoColor = Color.cyan;
    public float gizmoLength = 1.5f;
    public Transform targetObject;
    public float tiltValue;

    private void OnValidate()
    {
        targetObject.rotation = Quaternion.Euler(GetEulerRotation(selectedRotation));
    }

    private Vector3 GetEulerRotation(RotationOption option)
    {
        switch (option)
        {
            case RotationOption.ForwardTiltPositive: return new Vector3(tiltValue, 0, 0);
            case RotationOption.ForwardTiltNegative: return new Vector3(-tiltValue, 0, 0);
            case RotationOption.YawRight: return new Vector3(0, 0, tiltValue);
            case RotationOption.YawLeft: return new Vector3(0, 0, -tiltValue);
            default: return Vector3.zero;
        }
    }

    private void OnDrawGizmos()
    {
        if (targetObject == null) return;


        Gizmos.color = gizmoColor;


        Vector3 origin = targetObject.position;


        foreach (RotationOption option in System.Enum.GetValues(typeof(RotationOption)))
        {
            Vector3 dir = Quaternion.Euler(GetEulerRotation(option)) * Vector3.forward * gizmoLength;
            Gizmos.DrawRay(origin, dir);
        }
    }
}