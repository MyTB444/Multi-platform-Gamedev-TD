using System.Collections.Generic;
using UnityEngine;

public enum TowerUpgradeType
{
    // Common
    DamageBoost,
    AttackSpeedBoost,
    RangeBoost,
    
    // Archer
    PoisonArrows,
    FireArrows,
    
    // Spear
    BarbedSpear,
    ExplosiveTip,
    
    // Pyromancer
    BurnChance,
    BiggerFireball,
    BurnSpread,
    
    // Ice Mage
    StrongerSlow,
    LongerSlow,
    Frostbite,
    FreezeSolid,
    
    // Spike Trap
    PoisonSpikes,
    BleedingSpikes,
    CripplingSpikes,
    
    // Blade Tower
    BleedChance,
    MoreBlades,
    ExtendedReach,
    
    // Rock Shower
    MoreRocks,
    BiggerRocks,
    LongerShower,
    MeteorStrike,
    
    // Phantom Knight
    MorePhantoms,
    CloserSpawn,
    SpectralChains,
    DoubleSlash
}

public class TowerBase : MonoBehaviour
{
    protected EnemyBase currentEnemy;
    [SerializeField] protected int damage;
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
    
    [SerializeField] protected ElementType elementType = ElementType.Physical;

    [Header(("Tower Pricing"))] 
    [SerializeField] protected int buyPrice = 1;
    [SerializeField] protected int sellPrice = 1;
    
    [Header("Stat Upgrades")]
    [SerializeField] protected bool damageBoost = false;
    [SerializeField] protected float damageBoostPercent = 0.25f;
    [Space]
    [SerializeField] protected bool attackSpeedBoost = false;
    [SerializeField] protected float attackSpeedBoostPercent = 0.20f;
    [Space]
    [SerializeField] protected bool rangeBoost = false;
    [SerializeField] protected float rangeBoostPercent = 0.20f;

    protected int baseDamage;
    protected float baseAttackCooldown;
    protected float baseAttackRange;
    
    [Header("VFX")]
    [SerializeField] protected GameObject attackSpawnEffectPrefab;
    
    [Header("Animation")]
    [SerializeField] protected Animator characterAnimator;
    [SerializeField] protected string attackAnimationTrigger = "Attack";
    [SerializeField] protected float projectileSpawnDelay = 0f;

    [Header("Targeting Setup")]
    [SerializeField] protected bool targetMostAdvancedEnemy = true;
    [SerializeField] protected bool targetPriorityEnemy = true;
    [SerializeField] protected EnemyType enemyPriorityType;
    [SerializeField] protected bool useHpTargeting = true;
    [SerializeField] protected bool targetHighestHpEnemy = true;
    [SerializeField] protected bool useRandomTargeting = false;

    private float targetCheckInterval = .1f;
    private float lastTimeCheckedTarget;
    private Collider[] allocatedColliders = new Collider[100];

    protected virtual void Awake()
    {
    }

    protected virtual void Start()
    {
        baseDamage = damage;
        baseAttackCooldown = attackCooldown;
        baseAttackRange = attackRange;
    
        ApplyStatUpgrades();
    }

    protected virtual void FixedUpdate()
    {
        ClearTargetOutOfRange();
        UpdateTarget();
        HandleRotation();

        if (CanAttack()) AttemptToAttack();
    }
    
    protected DamageInfo CreateDamageInfo()
    {
        return new DamageInfo(damage, elementType);
    }

    protected virtual void ClearTargetOutOfRange()
    {
        if (currentEnemy == null) return;

        if (Vector3.Distance(currentEnemy.transform.position, transform.position) > attackRange) currentEnemy = null;
    }

    // Periodically searches for new targets instead of every frame for performance
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

    // Selects best enemy from candidates based on HP and/or distance criteria
    private EnemyBase ChooseEnemyToTarget(List<EnemyBase> targets)
    {
        EnemyBase enemyToTarget = null;
        
        if (useRandomTargeting)
        {
            return targets[Random.Range(0, targets.Count)];
        }

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
    
        // Trigger attack animation
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(attackAnimationTrigger);
        }
    
        // Fire projectile with delay (0 = immediate)
        if (projectileSpawnDelay > 0)
        {
            Invoke("FireProjectile", projectileSpawnDelay);
        }
        else
        {
            FireProjectile();
        }
    }

    // Raycasts to enemy and spawns projectile with hit information
    protected virtual void FireProjectile()
    {
        if (attackSpawnEffectPrefab != null && gunPoint != null)
        {
            GameObject spawnVFX = Instantiate(attackSpawnEffectPrefab, gunPoint.position, Quaternion.identity);
            Destroy(spawnVFX, 2f);
        }
    
        Vector3 directionToEnemy = DirectionToEnemyFrom(gunPoint);

        if (Physics.Raycast(gunPoint.position, directionToEnemy, out RaycastHit hitInfo, Mathf.Infinity, whatIsTargetable))
        {
            IDamageable damageable = hitInfo.transform.GetComponent<IDamageable>();

            if (damageable == null) return;

            Vector3 spawnPosition = gunPoint.position + directionToEnemy * 0.1f;
            GameObject newProjectile = Instantiate(projectilePrefab, spawnPosition, gunPoint.rotation);
            newProjectile.GetComponent<TowerProjectileBase>().SetupProjectile(hitInfo.point, damageable, CreateDamageInfo(), projectileSpeed);
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
        return (currentEnemy.GetCenterPoint() - startPosition.position).normalized;
    }
    
    protected virtual void ApplyStatUpgrades()
    {
        if (damageBoost)
        {
            damage = Mathf.RoundToInt(baseDamage * (1f + damageBoostPercent));
        }
        else
        {
            damage = baseDamage;
        }
    
        if (attackSpeedBoost)
        {
            attackCooldown = baseAttackCooldown * (1f - attackSpeedBoostPercent);
        }
        else
        {
            attackCooldown = baseAttackCooldown;
        }
    
        if (rangeBoost)
        {
            attackRange = baseAttackRange * (1f + rangeBoostPercent);
        }
        else
        {
            attackRange = baseAttackRange;
        }
    }
    
    public virtual void SetUpgrade(TowerUpgradeType upgradeType, bool enabled)
    {
        switch (upgradeType)
        {
            case TowerUpgradeType.DamageBoost:
                damageBoost = enabled;
                ApplyStatUpgrades();
                break;
            case TowerUpgradeType.AttackSpeedBoost:
                attackSpeedBoost = enabled;
                ApplyStatUpgrades();
                break;
            case TowerUpgradeType.RangeBoost:
                rangeBoost = enabled;
                ApplyStatUpgrades();
                break;
        }
    }

    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    
    public int GetBuyPrice() { return buyPrice; }
    public int GetSellPrice() { return sellPrice; }
}