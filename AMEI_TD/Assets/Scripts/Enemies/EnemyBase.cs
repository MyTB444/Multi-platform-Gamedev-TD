using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    Basic,
    Fast,
    Tank,
}

public class EnemyBase : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyType enemyType;
    [SerializeField] float enemyHp;
    [SerializeField] float enemySpeed;
    [SerializeField] private Transform centerPoint;
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private EnemyPath path;

    private int currentWaypointIndex = 0;
    private float waypointReachDistance = 0.1f;

    protected virtual void Start()
    {
        if (path != null && path.WaypointCount > 0)
        {
            Transform firstWaypoint = path.GetWaypoint(0);
            if (firstWaypoint != null && bottomPoint != null)
            {
                Vector3 offset = transform.position - bottomPoint.position;
                transform.position = firstWaypoint.position + offset;
            }
        }
        
        Renderer renderer = GetComponent<Renderer>();
        switch (enemyType)
        {
            case EnemyType.Basic:
                renderer.material.color = Color.green;
                break;
            case EnemyType.Fast:
                renderer.material.color = Color.magenta;
                break;
            case EnemyType.Tank:
                renderer.material.color = Color.red;
                break;
            default:
                break;
        }
    }

    protected virtual void Update()
    {
        if (enemyHp <= 0)
        {
            Die();
            return;
        }

        FollowPath();
    }

    private void FollowPath()
    {
        if (!path || currentWaypointIndex >= path.WaypointCount)
        {
            ReachedEnd();
            return;
        }

        Transform targetWaypoint = path.GetWaypoint(currentWaypointIndex);
        if (!targetWaypoint || !bottomPoint)
        {
            return;
        }

        Vector3 direction = (targetWaypoint.position - bottomPoint.position).normalized;
    
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    
        transform.position += direction * (enemySpeed * Time.deltaTime);

        float distanceToWaypoint = Vector3.Distance(bottomPoint.position, targetWaypoint.position);
        if (distanceToWaypoint <= waypointReachDistance)
        {
            currentWaypointIndex++;
        }
    }

    private void ReachedEnd()
    {
        Destroy(gameObject);
    }

    // Get Main Damage
    public virtual void TakeDamage(float damage)
    {
        enemyHp -= damage;
    }

    public Vector3 GetCenterPoint()
    {
        return centerPoint.position;
    }

    public EnemyType GetEnemyType()
    {
        return enemyType;
    }

    void Die()
    {
        Destroy(gameObject);
    }
}