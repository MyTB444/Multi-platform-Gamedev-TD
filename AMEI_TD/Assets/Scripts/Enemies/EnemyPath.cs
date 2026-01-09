using UnityEngine;

public class EnemyPath : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;

    void Awake()
    {
        // Collect all child transforms as waypoints
        waypoints = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            waypoints[i] = transform.GetChild(i);
        }
    }
    
    /// <summary>
    /// Returns the array of waypoint transforms that define this path.
    /// </summary>
    public Transform[] GetWaypoints() => waypoints;

    /// <summary>
    /// Returns the total number of waypoints in this path.
    /// </summary>
    public int GetWaypointCount() => waypoints.Length;

    /// <summary>
    /// Visualizes the path in the Unity editor with colored lines and spheres.
    /// Green sphere = start, Yellow spheres = intermediate waypoints, Red sphere = end.
    /// </summary>
    void OnDrawGizmos()
    {
        // Collect waypoints for gizmo drawing
        Transform[] points = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            points[i] = transform.GetChild(i);
        }

        if (points.Length == 0) return;

        // Draw green lines connecting waypoints
        Gizmos.color = Color.green;
        for (int i = 0; i < points.Length - 1; i++)
        {
            if (points[i] != null && points[i + 1] != null)
            {
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
            }
        }

        // Draw yellow spheres for intermediate waypoints
        Gizmos.color = Color.yellow;
        for (int i = 1; i < points.Length - 1; i++)
        {
            if (points[i] != null)
            {
                Gizmos.DrawSphere(points[i].position, 0.3f);
            }
        }

        // Draw green sphere for start point
        if (points.Length > 0 && points[0] != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(points[0].position, 0.4f);
        }

        // Draw red sphere for end point
        if (points.Length > 0 && points[points.Length - 1] != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(points[points.Length - 1].position, 0.4f);
        }
    }
}