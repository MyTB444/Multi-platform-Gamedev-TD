using UnityEngine;

/// <summary>
/// Defines cardinal directions for tower facing relative to the road.
/// Towers rotate to face the road when placed.
/// </summary>
public enum FacingDirection
{
    North,      // Z+
    East,       // X+
    South,      // Z-
    West        // X-
}

/// <summary>
/// Tower placement foundation that handles spawn positioning and rotation.
/// Supports different spawn points per element type and configurable road-facing direction.
/// </summary>
public class TowerStandingBase : MonoBehaviour
{
    // ==================== Spawn Configuration ====================
    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPointDefault;
    [SerializeField] private Transform spawnPointMechanic;
    [SerializeField] private Transform spawnPointImaginary;
    
    [Header("Road Facing")]
    [SerializeField] private FacingDirection roadFacing = FacingDirection.North;
    
    /// <summary>
    /// Returns the appropriate spawn point transform based on tower element type.
    /// Falls back to default spawn point if element-specific point is not assigned.
    /// </summary>
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
    
    /// <summary>
    /// Returns the rotation towers should use when spawned on this base.
    /// Currently all element types face the same direction (toward the road).
    /// </summary>
    public Quaternion GetSpawnRotation(ElementType elementType)
    {
        return GetFacingRotation();
    }
    
    private Quaternion GetFacingRotation()
    {
        return Quaternion.Euler(0, GetFacingAngle(), 0);
    }
    
    /// <summary>
    /// Converts FacingDirection enum to Y-axis rotation angle.
    /// </summary>
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
    
    /// <summary>
    /// Converts FacingDirection enum to world-space direction vector.
    /// Used for gizmo visualization.
    /// </summary>
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
        // Spawn point indicators - color-coded by element type
        if (spawnPointDefault != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(spawnPointDefault.position, 0.2f);
        }
    
        if (spawnPointMechanic != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange for Mechanic
            Gizmos.DrawWireSphere(spawnPointMechanic.position, 0.2f);
        }
    
        if (spawnPointImaginary != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPointImaginary.position, 0.2f);
        }
    
        // Green arrow showing which direction towers will face (toward road)
        DrawArrow(transform.position + Vector3.up * 0.5f, GetFacingDirection(), 2f, Color.green);
    }

    /// <summary>
    /// Draws a directional arrow in the scene view for editor visualization.
    /// </summary>
    private void DrawArrow(Vector3 start, Vector3 direction, float length, Color color)
    {
        Gizmos.color = color;
        Vector3 end = start + direction * length;
        Gizmos.DrawLine(start, end);
        
        // Arrowhead geometry
        float arrowHeadLength = 0.4f;
        float arrowHeadAngle = 25f;
        
        // Calculate arrowhead wing directions using rotation
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;
        
        Gizmos.DrawLine(end, end + right * arrowHeadLength);
        Gizmos.DrawLine(end, end + left * arrowHeadLength);
    }
}