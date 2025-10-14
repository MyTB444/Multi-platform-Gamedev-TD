using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPath : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;

    void Awake()
    {
        waypoints = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            waypoints[i] = transform.GetChild(i);
        }
    }
    
    public Transform[] GetWaypoints() => waypoints;
    public int GetWaypointCount() => waypoints.Length;

    void OnDrawGizmos()
    {
        Transform[] points = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            points[i] = transform.GetChild(i);
        }

        if (points.Length == 0) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < points.Length - 1; i++)
        {
            if (points[i] != null && points[i + 1] != null)
            {
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
            }
        }

        Gizmos.color = Color.yellow;
        for (int i = 1; i < points.Length - 1; i++)
        {
            if (points[i] != null)
            {
                Gizmos.DrawSphere(points[i].position, 0.3f);
            }
        }

        if (points.Length > 0 && points[0] != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(points[0].position, 0.4f);
        }

        if (points.Length > 0 && points[points.Length - 1] != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(points[points.Length - 1].position, 0.4f);
        }
    }
}