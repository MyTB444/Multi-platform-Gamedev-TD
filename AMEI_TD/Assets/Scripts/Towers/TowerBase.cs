using System.Collections.Generic;
using UnityEngine;

public enum TowerUpgradeType
{
    // Common
    DamageBoost,

    // Physical (Archer & Spear)
    PhysicalAttackSpeed,
    PhysicalRange,

    // Archer
    PoisonArrows,
    FireArrows,

    // Spear
    BarbedSpear,
    ExplosiveTip,

    // Pyromancer
    PyromancerAttackSpeed,
    BurnChance,
    BiggerFireball,
    BurnSpread,

    // Ice Mage
    StrongerSlow,
    LongerSlow,
    Frostbite,
    FreezeSolid,

    // Spike Trap
    SpikeTrapAttackSpeed,
    PoisonSpikes,
    BleedingSpikes,
    CripplingSpikes,

    // Blade Tower
    BladeSpinSpeed,
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

    [Header("Pooling")]
    [SerializeField] protected int projectilePoolAmount = 20;

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
    [SerializeField] protected LayerMask decoyLayer;
    [SerializeField] protected LayerMask enemyLayerMask;

    private float targetCheckInterval = .1f;
    private float lastTimeCheckedTarget;
    private Collider[] allocatedColliders = new Collider[100];

    // Debuff system
    protected bool isSlowed = false;
    private float slowEndTime;
    protected float slowMultiplier = 1f;

    protected bool isDisabled = false;
    private float disableEndTime;
    private float disableStartTime;
    private float disableDuration;

    // Guardian buff system
    protected bool hasGuardianBuff = false;
    protected float guardianDamageBuffPercent = 0f;
    protected float guardianAttackSpeedBuffPercent = 0f;
    protected float guardianRangeBuffPercent = 0f;

    // Debuff visuals
    [Header("Debuff Visuals")]
    [SerializeField] private Color disabledColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    private Renderer[] towerRenderers;
    private Color[] originalColors;
    private bool hasSavedColors = false;

    // Guardian buff visuals
    [Header("Guardian Buff Visuals")]
    [SerializeField] private Color guardianBuffColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private float guardianGlowIntensity = 0.3f;
    [SerializeField] private float guardianPulseSpeed = 2f;

    protected virtual void Awake()
    {
    }

    protected virtual void Start()
    {
        if (projectilePrefab != null)
        {
            ObjectPooling.instance.Register(projectilePrefab, projectilePoolAmount);
        }

        baseDamage = damage;
        baseAttackCooldown = attackCooldown;
        baseAttackRange = attackRange;

        ApplyStatUpgrades();
        SaveOriginalColors();

        // Apply any unlocked upgrades from manager
        if (TowerUpgradeManager.instance != null)
        {
            TowerUpgradeManager.instance.ApplyUnlockedUpgrades(this);
        }
    }

    private void SaveOriginalColors()
    {
        towerRenderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[towerRenderers.Length];

        for (int i = 0; i < towerRenderers.Length; i++)
        {
            if (towerRenderers[i].material.HasProperty("_Color"))
            {
                originalColors[i] = towerRenderers[i].material.color;
            }
        }

        hasSavedColors = true;
    }

    protected virtual void FixedUpdate()
    {
        UpdateDebuffs();
        UpdateDisabledVisual();
        UpdateGuardianBuffVisual();

        if (isDisabled) return;

        ClearTargetOutOfRange();
        UpdateTarget();
        HandleRotation();

        if (CanAttack()) AttemptToAttack();
    }

    protected void UpdateDebuffs()
    {
        if (isSlowed && Time.time >= slowEndTime)
        {
            RemoveSlow();
        }

        if (isDisabled && Time.time >= disableEndTime)
        {
            RemoveDisable();
        }
    }

    protected void UpdateDisabledVisual()
    {
        if (!hasSavedColors) return;

        if (!isDisabled)
        {
            return;
        }

        float elapsed = Time.time - disableStartTime;
        float remaining = disableEndTime - Time.time;

        // Calculate intensity based on fade in/out
        float intensity = 1f;

        // Fade in
        if (elapsed < fadeInDuration)
        {
            intensity = elapsed / fadeInDuration;
        }
        // Fade out
        else if (remaining < fadeOutDuration)
        {
            intensity = Mathf.Max(0f, remaining / fadeOutDuration);
        }

        // Pulse effect
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

        // Combine pulse with fade intensity
        float finalLerp = pulse * intensity;

        for (int i = 0; i < towerRenderers.Length; i++)
        {
            if (towerRenderers[i] != null && towerRenderers[i].material.HasProperty("_Color"))
            {
                towerRenderers[i].material.color = Color.Lerp(originalColors[i], disabledColor, finalLerp);
            }
        }
    }

    private void RestoreOriginalColors()
    {
        if (!hasSavedColors) return;

        for (int i = 0; i < towerRenderers.Length; i++)
        {
            if (towerRenderers[i] != null && towerRenderers[i].material.HasProperty("_Color"))
            {
                towerRenderers[i].material.color = originalColors[i];
            }
        }
    }

    protected void UpdateGuardianBuffVisual()
    {
        if (!hasSavedColors || isDisabled) return;
        if (!hasGuardianBuff) return;

        // Gentle golden pulse effect for guardian buff
        float pulse = (Mathf.Sin(Time.time * guardianPulseSpeed) + 1f) / 2f;
        float lerpAmount = guardianGlowIntensity * pulse;

        for (int i = 0; i < towerRenderers.Length; i++)
        {
            if (towerRenderers[i] != null && towerRenderers[i].material.HasProperty("_Color"))
            {
                towerRenderers[i].material.color = Color.Lerp(originalColors[i], guardianBuffColor, lerpAmount);
            }
        }
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        isSlowed = true;
        slowEndTime = Time.time + duration;
        slowMultiplier = 1f - Mathf.Clamp(slowPercent, 0f, 0.9f);
        Debug.Log($"{gameObject.name} slowed! slowPercent={slowPercent}, slowMultiplier={slowMultiplier}");
    }

    private void RemoveSlow()
    {
        isSlowed = false;
        slowMultiplier = 1f;
    }

    public void ApplyDisable(float duration)
    {
        isDisabled = true;
        disableStartTime = Time.time;
        disableDuration = duration;
        disableEndTime = Time.time + duration;
    }

    private void RemoveDisable()
    {
        isDisabled = false;
        RestoreOriginalColors();
    }

    public bool IsSlowed() => isSlowed;
    public bool IsDisabled() => isDisabled;

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
    protected void UpdateTarget()
    {
        if (Time.time > lastTimeCheckedTarget + targetCheckInterval || currentEnemy == null)
        {
            lastTimeCheckedTarget = Time.time;
            currentEnemy = FindEnemyWithinRange();
        }
    }

    protected virtual EnemyBase FindEnemyWithinRange()
    {
        List<EnemyBase> decoyTargets = new List<EnemyBase>();
        List<EnemyBase> priorityTargets = new List<EnemyBase>();
        List<EnemyBase> allTargets = new List<EnemyBase>();

        int decoysAround = Physics.OverlapSphereNonAlloc(transform.position, attackRange, allocatedColliders, decoyLayer);

        if (decoysAround > 0)
        {
            for (int i = 0; i < decoysAround; i++)
            {
                EnemyBase decoy = allocatedColliders[i].GetComponent<EnemyBase>();

                if (decoy == null) continue;
                
                // Skip enemies in spawn grace period
                if (!decoy.IsTargetable()) continue;

                float distanceToDecoy = Vector3.Distance(transform.position, decoy.transform.position);

                if (distanceToDecoy > attackRange) continue;

                decoyTargets.Add(decoy);
            }

            if (decoyTargets.Count > 0)
            {
                return ChooseEnemyToTarget(decoyTargets);
            }
        }

        int enemiesAround = Physics.OverlapSphereNonAlloc(transform.position, attackRange, allocatedColliders, enemyLayerMask);

        if (enemiesAround == 0) return null;

        // Collect valid enemies within range
        for (int i = 0; i < enemiesAround; i++)
        {
            EnemyBase newEnemy = allocatedColliders[i].GetComponent<EnemyBase>();

            if (newEnemy == null) continue;
            
            // Skip enemies in spawn grace period
            if (!newEnemy.IsTargetable()) continue;

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

            GameObject newProjectile = ObjectPooling.instance.Get(projectilePrefab);
            newProjectile.transform.position = spawnPosition;
            newProjectile.transform.rotation = gunPoint.rotation;
            newProjectile.SetActive(true);

            newProjectile.GetComponent<TowerProjectileBase>().SetupProjectile(hitInfo.point, damageable, CreateDamageInfo(), projectileSpeed);
        }
    }

    protected virtual bool CanAttack()
    {
        // slowMultiplier < 1 means slowed, so divide to increase cooldown
        // e.g., slowMultiplier = 0.5 means 50% speed, so cooldown takes 2x longer
        float effectiveCooldown = attackCooldown / slowMultiplier;
        return Time.time > lastTimeAttacked + effectiveCooldown && currentEnemy != null;
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
        // Calculate total buff percentages (tower upgrades + guardian buff)
        float totalDamageBoost = 0f;
        float totalAttackSpeedBoost = 0f;
        float totalRangeBoost = 0f;

        if (damageBoost)
        {
            totalDamageBoost += damageBoostPercent;
        }

        if (attackSpeedBoost)
        {
            totalAttackSpeedBoost += attackSpeedBoostPercent;
        }

        if (rangeBoost)
        {
            totalRangeBoost += rangeBoostPercent;
        }

        // Add Guardian buffs
        if (hasGuardianBuff)
        {
            totalDamageBoost += guardianDamageBuffPercent;
            totalAttackSpeedBoost += guardianAttackSpeedBuffPercent;
            totalRangeBoost += guardianRangeBuffPercent;
        }

        // Apply calculated stats
        damage = Mathf.RoundToInt(baseDamage * (1f + totalDamageBoost));
        attackCooldown = baseAttackCooldown * (1f - totalAttackSpeedBoost);
        attackRange = baseAttackRange * (1f + totalRangeBoost);
    }

    public virtual void SetUpgrade(TowerUpgradeType upgradeType, bool enabled)
    {
        switch (upgradeType)
        {
            case TowerUpgradeType.DamageBoost:
                damageBoost = enabled;
                ApplyStatUpgrades();
                break;
        }
    }

    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    // Guardian Buff System
    public void ApplyGuardianBuff(float damagePercent, float attackSpeedPercent, float rangePercent)
    {
        hasGuardianBuff = true;
        guardianDamageBuffPercent = damagePercent;
        guardianAttackSpeedBuffPercent = attackSpeedPercent;
        guardianRangeBuffPercent = rangePercent;
        ApplyStatUpgrades();
    }

    public void RemoveGuardianBuff()
    {
        hasGuardianBuff = false;
        guardianDamageBuffPercent = 0f;
        guardianAttackSpeedBuffPercent = 0f;
        guardianRangeBuffPercent = 0f;
        ApplyStatUpgrades();
    }

    public bool HasGuardianBuff() => hasGuardianBuff;

    public int GetBuyPrice() { return buyPrice; }
    public int GetSellPrice() { return sellPrice; }
    public float GetSlowMultiplier() => slowMultiplier;
    public ElementType GetElementType() => elementType;
}