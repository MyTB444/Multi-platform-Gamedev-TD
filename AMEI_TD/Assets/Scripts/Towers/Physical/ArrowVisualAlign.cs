using UnityEngine;

public class ArrowVisualAlign : MonoBehaviour
{
    [SerializeField] private Transform nockPoint;
    [SerializeField] private Transform aimTarget;
    [SerializeField] private Vector3 rotationOffset;
    
    void LateUpdate()
    {
        // Follow nock point position
        if (nockPoint != null)
        {
            transform.position = nockPoint.position;
        }
        
        // Aim toward target
        if (aimTarget != null)
        {
            Vector3 direction = aimTarget.position - transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(rotationOffset);
            }
        }
    }
}