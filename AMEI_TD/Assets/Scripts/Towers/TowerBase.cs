using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TowerBase : MonoBehaviour
{
    private EnemyBase currentEnemy;
    [SerializeField] private int damage;
    [SerializeField] protected float attackCooldown = 1f;
    protected float lastTimeAttacked;

    [Header("Tower Setup")]
    [SerializeField] protected Transform towerBody;
    [SerializeField] protected Transform towerHead;
    [SerializeField] protected Transform gunPoint;
    [SerializeField] protected float rotationSpeed = 10f;

    [SerializeField] protected float attackRange = 2.5f;
    [SerializeField] protected LayerMask whatIsEnemy;
    [SerializeField] protected LayerMask whatIsTargetable;

    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected float projectileSpeed;

    [Header("Targeting Setup")]
    [SerializeField] protected bool targetMostAdvancedEnemy = true;
    [SerializeField] protected bool targetPriorityEnemy = true;
    [SerializeField] protected EnemyType enemyPriorityType;
    [SerializeField] protected bool useHpTargeting = true;
    [SerializeField] protected bool targetHighestHpEnemy = true;

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

    // Checks for a new target every interval.
    // If no current target is found, then it checks for a new target.
    private void UpdateTarget()
    {
        if (Time.time > lastTimeCheckedTarget + targetCheckInterval || currentEnemy == null)
        {
            lastTimeCheckedTarget = Time.time;
            currentEnemy = FindEnemyWithinRange();
        }
    }

    protected virtual EnemyBase FindEnemyWithinRange()
    {
        List<EnemyBase> priorityTargets = new List<EnemyBase>();
        List<EnemyBase> allTargets = new List<EnemyBase>();

        int enemiesAround =
            Physics.OverlapSphereNonAlloc(transform.position, attackRange, allocatedColliders, whatIsEnemy);

        if (enemiesAround == 0) return null;

        // Collect valid enemies within range
        for (int i = 0; i < enemiesAround; i++)
        {
            EnemyBase newEnemy = allocatedColliders[i].GetComponent<EnemyBase>();

            if (newEnemy == null) continue;

            float distanceToEnemy = Vector3.Distance(transform.position, newEnemy.transform.position);

            if (distanceToEnemy > attackRange) continue;

            EnemyType newEnemyType = newEnemy.GetEnemyType();

            allTargets.Add(newEnemy);

            // Track priority enemies separately
            if (newEnemyType == enemyPriorityType)
            {
                priorityTargets.Add(newEnemy);
            }
        }

        // Priority targeting: only shoot priority enemies if they exist
        if (targetPriorityEnemy && priorityTargets.Count > 0)
        {
            return ChooseEnemyToTarget(priorityTargets);
        }

        // No priority targeting or no priority enemies: target anyone
        if (allTargets.Count > 0)
        {
            return ChooseEnemyToTarget(allTargets);
        }

        return null;
    }

    private EnemyBase ChooseEnemyToTarget(List<EnemyBase> targets)
    {
        EnemyBase enemyToTarget = null;

        // HP-based targeting takes priority over distance
        if (useHpTargeting)
        {
            float bestHp = targetHighestHpEnemy ? float.MinValue : float.MaxValue;
            float bestDistance = targetMostAdvancedEnemy ? float.MaxValue : float.MinValue;

            foreach (EnemyBase enemy in targets)
            {
                float enemyHp = enemy.GetEnemyHp();
                float remainingDistance = enemy.GetRemainingDistance();

                bool isBetterHp = targetHighestHpEnemy
                    ? enemyHp > bestHp
                    : enemyHp < bestHp;

                bool shouldTarget = false;

                // Primary criteria: HP
                if (isBetterHp)
                {
                    shouldTarget = true;
                }
                else if (Mathf.Approximately(enemyHp, bestHp))
                {
                    // HP tied: use distance as tiebreaker
                    bool isBetterDistance = targetMostAdvancedEnemy
                        ? remainingDistance < bestDistance
                        : remainingDistance > bestDistance;

                    shouldTarget = isBetterDistance;
                }

                if (shouldTarget)
                {
                    bestHp = enemyHp;
                    bestDistance = remainingDistance;
                    enemyToTarget = enemy;
                }
            }
        }
        else
        {
            // Distance-only targeting (HP ignored)
            float bestDistance = targetMostAdvancedEnemy ? float.MaxValue : float.MinValue;

            foreach (EnemyBase enemy in targets)
            {
                float remainingDistance = enemy.GetRemainingDistance();


                bool isBetterDistance = targetMostAdvancedEnemy
                    ? remainingDistance < bestDistance
                    : remainingDistance > bestDistance;

                if (isBetterDistance)
                {
                    bestDistance = remainingDistance;
                    enemyToTarget = enemy;
                }
            }
        }
        return enemyToTarget;
    }

    protected void AttemptToAttack()
    {
        if (currentEnemy == null)
        {
            Debug.LogWarning("[AttemptToAttack] currentEnemy is NULL");
            return;
        }

        if (!currentEnemy.gameObject.activeSelf)
        {
            Debug.Log($"[AttemptToAttack] Enemy '{currentEnemy.name}' inactive → clearing target.");
            currentEnemy = null;
            return;
        }

        // Debug: show cooldown state on every attempt
        float nextAllowed = lastTimeAttacked + attackCooldown;
        float remaining = Mathf.Max(0f, nextAllowed - Time.time);
        bool can = CanAttack();
        Debug.Log($"[AttemptToAttack] now={Time.time:F3}  last={lastTimeAttacked:F3}  cd={attackCooldown:F3}  next={nextAllowed:F3}  remaining={remaining:F3}  CanAttack()={can}  enemy={currentEnemy.name}");

        // NOTE: You currently ignore cooldown here and always Attack().
        // If you intended to respect cooldown, uncomment the next 3 lines:
        // if (!can) return;
        // else Debug.Log("[AttemptToAttack] Cooldown passed → attacking.");

        Attack();
    }

    protected virtual void Attack()
    {
        lastTimeAttacked = Time.time;
        Debug.Log($"[Attack] Fire! t={lastTimeAttacked:F3}  nextAllowed={lastTimeAttacked + attackCooldown:F3}");
        FireProjectile();
    }

    protected virtual void FireProjectile()
    {
        Vector3 directionToEnemy = DirectionToEnemyFrom(gunPoint);
        Debug.DrawRay(gunPoint.position, directionToEnemy.normalized * 50f, Color.cyan, 0.1f);

        Debug.Log($"[FireProjectile] From '{gunPoint.name}' → '{currentEnemy?.name ?? "null"}'  dir={directionToEnemy}  pos={gunPoint.position}");

        if (Physics.Raycast(gunPoint.position, directionToEnemy, out RaycastHit hitInfo, Mathf.Infinity, whatIsTargetable))
        {
            Debug.Log($"[FireProjectile] Raycast HIT '{hitInfo.transform.name}' at {hitInfo.point}");

            IDamageable damageable = hitInfo.transform.GetComponent<IDamageable>();
            if (damageable == null)
            {
                Debug.LogWarning("[FireProjectile] Hit object has NO IDamageable.");
                return;
            }

            Vector3 spawnPosition = gunPoint.position + directionToEnemy * 0.1f;
            GameObject newProjectile = Instantiate(projectilePrefab, spawnPosition, gunPoint.rotation);
            Debug.Log($"[FireProjectile] Spawned '{newProjectile.name}' at {spawnPosition}  damage={damage}  speed={projectileSpeed}");

            newProjectile.GetComponent<TowerProjectileBase>()
                .SetupProjectile(hitInfo.point, damageable, damage, projectileSpeed);
        }
        else
        {
            Debug.LogWarning("[FireProjectile] Raycast MISS (check LayerMask/direction/obstacles).");
        }
    }

    protected virtual bool CanAttack()
    {
        bool can = Time.time > lastTimeAttacked + attackCooldown && currentEnemy != null;
        float nextAllowed = lastTimeAttacked + attackCooldown;
        Debug.Log($"[CanAttack] now={Time.time:F3}  last={lastTimeAttacked:F3}  cd={attackCooldown:F3}  next={nextAllowed:F3}  remaining={Mathf.Max(0f, nextAllowed - Time.time):F3}  enemy={(currentEnemy ? currentEnemy.name : "null")}  -> {can}");
        return can;
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
        return (currentEnemy.GetCenterPoint() - startPosition.position).normalized;
    }

    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

}
