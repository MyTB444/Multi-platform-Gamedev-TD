using UnityEngine;

public class SpearVisualAlign : MonoBehaviour
{
    [SerializeField] private Transform handPoint;
    [SerializeField] private Transform aimTarget;
    [SerializeField] private Vector3 rotationOffset;
    
    private SpearTower spearTower;
    
    void Start()
    {
        spearTower = GetComponentInParent<SpearTower>();
    }
    
    void LateUpdate()
    {
        // Follow hand position
        if (handPoint != null)
        {
            transform.position = handPoint.position;
        }
        
        // Aim toward current enemy
        if (aimTarget != null)
        {
            Vector3 direction = aimTarget.position - transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(rotationOffset);
            }
        }
    }
    
    public void SetAimTarget(Transform target)
    {
        aimTarget = target;
    }
}