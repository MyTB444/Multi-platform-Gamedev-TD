using System;
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
    protected EnemySpawner mySpawner;

    [SerializeField] private EnemyType enemyType;
    [SerializeField] private float enemySpeed;
    [SerializeField] private Transform centerPoint;
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private int damage;
    [SerializeField] private int reward;

    private float enemyCurrentHp;
    public float enemyMaxHp = 100;
    protected bool isDead;

    protected Vector3[] myWaypoints;
    private int currentWaypointIndex = 0;
    private float waypointReachDistance = 0.1f;

    private int originalLayerIndex;
   
    private float totalDistance;

    private void Awake()
    {
        originalLayerIndex = gameObject.layer;
    }

    protected virtual void Start()
    {
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
        FollowPath();
    }

    public void SetupEnemy(EnemySpawner myNewSpawner)
    {
        mySpawner = myNewSpawner;

        UpdateWaypoints(myNewSpawner.currentWaypoints);
        CollectTotalDistance();
        ResetEnemy();
        BeginMovement();
    }

    private void UpdateWaypoints(Vector3[] newWaypoints)
    {
        myWaypoints = new Vector3[newWaypoints.Length];

        for (int i = 0; i < myWaypoints.Length; i++)
        {
            myWaypoints[i] = newWaypoints[i];
        }
    }

    private void BeginMovement()
    {
        currentWaypointIndex = 0;
    }

    private void CollectTotalDistance()
    {
        for (int i = 0; i < myWaypoints.Length; i++)
        {
            if (i == myWaypoints.Length - 1) break;
            float distance = Vector3.Distance(myWaypoints[i], myWaypoints[i + 1]);
            totalDistance += distance;
        }
    }

    public float GetRemainingDistance()
    {
        if (myWaypoints == null || currentWaypointIndex >= myWaypoints.Length) return 0;

        float remainingDistance = 0;

        // Distance from current position to the next waypoint
        remainingDistance += Vector3.Distance(transform.position, myWaypoints[currentWaypointIndex]);

        // Distance for all remaining waypoint segments
        for (int i = currentWaypointIndex; i < myWaypoints.Length - 1; i++)
        {
            remainingDistance += Vector3.Distance(myWaypoints[i], myWaypoints[i + 1]);
        }

        return remainingDistance;
    }

    private void FollowPath()
    {
        if (myWaypoints == null || currentWaypointIndex >= myWaypoints.Length)
        {
            ReachedEnd();
            return;
        }

        Vector3 targetWaypoint = myWaypoints[currentWaypointIndex];
        if (!bottomPoint)
        {
            return;
        }

        Vector3 direction = (targetWaypoint - bottomPoint.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        transform.position += direction * (enemySpeed * Time.deltaTime);

        float distanceToWaypoint = Vector3.Distance(bottomPoint.position, targetWaypoint);
        if (distanceToWaypoint <= waypointReachDistance)
        {
            currentWaypointIndex++;
        }
    }

    private void ReachedEnd()
    {
        Destroy(gameObject);
    }
    public virtual void TakeDamage(float damage)
    {
        enemyCurrentHp -= damage;

        if (enemyCurrentHp <= 0 && isDead == false)
        {
            isDead = true;
            Die();
        }
    }

    public Vector3 GetCenterPoint() => centerPoint.position;
    public EnemyType GetEnemyType() => enemyType;
    public float GetEnemyHp() => enemyCurrentHp;
    public Transform GetBottomPoint() => bottomPoint;
    private void ResetEnemy()
    {
        gameObject.layer = originalLayerIndex;

        enemyCurrentHp = enemyMaxHp;
        isDead = false;

    }

    private void Die()
    {
        Destroy(gameObject);
        GameManager.instance.UpdateSkillPoints(reward);
        RemoveEnemy();
    }

    public void RemoveEnemy()
    {
        if (mySpawner != null) mySpawner.RemoveActiveEnemy(gameObject);
    }
}