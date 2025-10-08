using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TowerBase : MonoBehaviour
{
    public EnemyBase currentEnemy;
    
    // Placeholder to check enemies distance to castle.
    // Needs to be refactored once waypoints are implemented.
    [SerializeField] GameObject playerCastle;

    [SerializeField] private int damage;
    [SerializeField] protected float attackCooldown = 1f;
    protected float lastTimeAttacked;

    [Header("Tower Setup")] 
    [SerializeField] protected Transform towerHead;
    [SerializeField] protected Transform towerBody;
    [SerializeField] protected Transform gunPoint;
    [SerializeField] protected float rotationSpeed = 10f;
    
    [SerializeField] protected float attackRange = 2.5f;
    [SerializeField] protected LayerMask whatIsEnemy;
    [SerializeField] protected LayerMask whatIsTargetable;
    
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected float projectileSpeed;

    [Header("Targeting Setup")] 
    [SerializeField] protected bool targetMostAdvancedEnemy = true;
    
    private float targetCheckInterval = .1f;
    private float lastTimeCheckedTarget;
    private Collider[] allocatedColliders = new Collider[100];

    protected virtual void Awake()
    {
        
    }
    
    protected virtual void Start()
    {
        
    }
    
    protected virtual void FixedUpdate()
    {
        ClearTargetOutOfRange();
        UpdateTarget();
        HandleRotation();

        if (CanAttack()) AttemptToAttack();
    }

    protected virtual void ClearTargetOutOfRange()
    {
        if (currentEnemy == null) return;
        
        if (Vector3.Distance(currentEnemy.transform.position, transform.position) > attackRange) currentEnemy = null;
    }

    private void UpdateTarget()
    {
        if (currentEnemy == null)
        {
            currentEnemy = FindEnemyWithinRange();
        }
        
        if (Time.time > lastTimeCheckedTarget + targetCheckInterval)
        {
            lastTimeCheckedTarget = Time.time;
            currentEnemy = FindEnemyWithinRange();
        } 
    }
    
    protected virtual EnemyBase FindEnemyWithinRange()
    {
        List<EnemyBase> possibleTargets = new List<EnemyBase>();
    
        int enemiesAround =
            Physics.OverlapSphereNonAlloc(transform.position, attackRange, allocatedColliders, whatIsEnemy);

        if (enemiesAround == 0) return null;

        for (int i = 0; i < enemiesAround; i++)
        {
            EnemyBase newEnemy = allocatedColliders[i].GetComponent<EnemyBase>();

            if (newEnemy == null) continue;
        
            float distanceToEnemy = Vector3.Distance(transform.position, newEnemy.transform.position);

            if (distanceToEnemy > attackRange) continue;
        
            possibleTargets.Add(newEnemy);
        }
    
        if (possibleTargets.Count > 0) return ChooseEnemyToTarget(possibleTargets);

        return null;
    }

    private EnemyBase ChooseEnemyToTarget(List<EnemyBase> targets)
    {
        EnemyBase enemyToTarget = null;
        float bestDistance = targetMostAdvancedEnemy ? float.MaxValue : float.MinValue;

        foreach (EnemyBase enemy in targets)
        {
            float remainingDistance = DistanceToFinishLine(enemy);
            
            // Chooses which enemy to target based on towers setting, either most or least advanced.
            bool shouldTarget = targetMostAdvancedEnemy 
                ? remainingDistance < bestDistance 
                : remainingDistance > bestDistance;

            if (shouldTarget)
            {
                bestDistance = remainingDistance;
                enemyToTarget = enemy;
            }
        }
        
        return enemyToTarget;
    }

    private float DistanceToFinishLine(EnemyBase enemy)
    {
        return Vector3.Distance(playerCastle.transform.position, enemy.transform.position);
    }

    protected void AttemptToAttack()
    {
        if (!currentEnemy.gameObject.activeSelf)
        {
            currentEnemy = null;
            return;
        }

        Attack();
    }

    protected virtual void Attack()
    {
        lastTimeAttacked = Time.time;
        FireProjectile();
    }
    
    protected virtual void FireProjectile()
    {
        Vector3 directionToEnemy = DirectionToEnemyFrom(gunPoint);

        if (Physics.Raycast(gunPoint.position, directionToEnemy, out RaycastHit hitInfo, Mathf.Infinity,
                whatIsTargetable))
        {
           // Check if hit info has a damageable component.
           // if null then return.
            
            Vector3 spawnPosition = gunPoint.position + directionToEnemy * 0.1f;
            GameObject newProjectile = Instantiate(projectilePrefab, spawnPosition, gunPoint.rotation);
            newProjectile.GetComponent<TowerProjectileBase>().SetupProjectile(hitInfo.point, damage, projectileSpeed);
        }
    }

    protected virtual bool CanAttack()
    {
        return Time.time > lastTimeAttacked + attackCooldown && currentEnemy != null;
    }

    protected virtual void HandleRotation()
    {
        RotateTowardsEnemy();
        RotateBodyTowardsEnemy();
    }

    protected virtual void RotateTowardsEnemy()
    {
        if (currentEnemy == null || towerHead == null) return;
        
        Vector3 directionToEnemy = DirectionToEnemyFrom(towerHead);

        Quaternion lookRotation = Quaternion.LookRotation(directionToEnemy);
        
        Vector3 rotation = Quaternion.Lerp(towerHead.rotation, lookRotation, rotationSpeed * Time.deltaTime).eulerAngles;

        towerHead.rotation = Quaternion.Euler(rotation);
    }

    protected void RotateBodyTowardsEnemy()
    {
        if (towerBody == null || currentEnemy == null) return;

        Vector3 directionToEnemy = DirectionToEnemyFrom(towerBody);
        directionToEnemy.y = 0;

        Quaternion lookRotation = Quaternion.LookRotation(directionToEnemy);
        towerBody.rotation = Quaternion.Slerp(towerBody.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }

    protected Vector3 DirectionToEnemyFrom(Transform startPosition)
    {
        return (currentEnemy.transform.position - startPosition.position).normalized;
    }
    
    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    
}
