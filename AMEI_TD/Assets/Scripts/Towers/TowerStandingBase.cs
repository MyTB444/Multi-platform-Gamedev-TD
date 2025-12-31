using UnityEngine;

public enum FacingDirection
{
    North,      // Z+
    East,       // X+
    South,      // Z-
    West        // X-
}

public class TowerStandingBase : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPointDefault;
    [SerializeField] private Transform spawnPointMechanic;
    [SerializeField] private Transform spawnPointImaginary;
    
    [Header("Road Facing")]
    [SerializeField] private FacingDirection roadFacing = FacingDirection.North;
    
    public Transform GetSpawnPoint(ElementType elementType)
    {
        switch (elementType)
        {
            case ElementType.Mechanic:
                return spawnPointMechanic != null ? spawnPointMechanic : spawnPointDefault;
                
            case ElementType.Imaginary:
                return spawnPointImaginary != null ? spawnPointImaginary : spawnPointDefault;
                
            default:
                return spawnPointDefault;
        }
    }
    
    public Quaternion GetSpawnRotation(ElementType elementType)
    {
        switch (elementType)
        {
            case ElementType.Imaginary:
            case ElementType.Mechanic:
                return GetFacingRotation();
            
            default: // Physical, Magic
                Transform spawnPoint = GetSpawnPoint(elementType);
                return spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        }
    }
    
    private Quaternion GetFacingRotation()
    {
        return Quaternion.Euler(0, GetFacingAngle(), 0);
    }
    
    private float GetFacingAngle()
    {
        switch (roadFacing)
        {
            case FacingDirection.North: return 0f;
            case FacingDirection.East: return 90f;
            case FacingDirection.South: return 180f;
            case FacingDirection.West: return 270f;
            default: return 0f;
        }
    }
    
    private Vector3 GetFacingDirection()
    {
        switch (roadFacing)
        {
            case FacingDirection.North: return Vector3.forward;
            case FacingDirection.East: return Vector3.right;
            case FacingDirection.South: return Vector3.back;
            case FacingDirection.West: return Vector3.left;
            default: return Vector3.forward;
        }
    }
    
    private void OnDrawGizmos()
    {
        // Spawn points
        if (spawnPointDefault != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(spawnPointDefault.position, 0.2f);
        }
    
        if (spawnPointMechanic != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(spawnPointMechanic.position, 0.2f);
        }
    
        if (spawnPointImaginary != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPointImaginary.position, 0.2f);
        }
    
        // Road facing arrow - green
        DrawArrow(transform.position + Vector3.up * 0.5f, GetFacingDirection(), 2f, Color.green);
    }

    private void DrawArrow(Vector3 start, Vector3 direction, float length, Color color)
    {
        Gizmos.color = color;
        Vector3 end = start + direction * length;
        Gizmos.DrawLine(start, end);
        
        float arrowHeadLength = 0.4f;
        float arrowHeadAngle = 25f;
        
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;
        
        Gizmos.DrawLine(end, end + right * arrowHeadLength);
        Gizmos.DrawLine(end, end + left * arrowHeadLength);
    }
}