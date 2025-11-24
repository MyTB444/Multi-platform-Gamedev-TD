using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyFast : EnemyBase
{
    private NavMeshAgent navAgent;
    protected int currentWaypointIndex;
    private void OnEnable()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    protected override void Update()
    {
        FollowPath();
    }
    public void FollowPath()
    {
        float distanceToWayPoint = Vector3.Distance(myWaypoints[currentWaypointIndex], transform.position);

        if (distanceToWayPoint <= 2)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % myWaypoints.Length;
        }
        navAgent.SetDestination(myWaypoints[currentWaypointIndex]);
    }
}
