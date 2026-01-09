using UnityEngine;

/// <summary>
/// Keeps the visual arrow model aligned with the bow's nock point and aimed at target.
/// Used for the arrow that's visible on the bow before being fired.
/// 
/// Runs in LateUpdate to ensure it follows animation updates.
/// </summary>
public class ArrowVisualAlign : MonoBehaviour
{
    [SerializeField] private Transform nockPoint;      // Where arrow attaches to bowstring
    [SerializeField] private Transform aimTarget;      // Target transform to aim toward
    [SerializeField] private Vector3 rotationOffset;   // Correct for arrow model orientation
    
    /// <summary>
    /// Follows nock point position and aims toward target.
    /// LateUpdate ensures this runs after animations.
    /// </summary>
    void LateUpdate()
    {
        // Follow nock point position (moves with bowstring draw)
        if (nockPoint != null)
        {
            transform.position = nockPoint.position;
        }
        
        // Aim toward target (predicted enemy position)
        if (aimTarget != null)
        {
            Vector3 direction = aimTarget.position - transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                // Apply rotation offset to correct arrow model orientation
                transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(rotationOffset);
            }
        }
    }
}