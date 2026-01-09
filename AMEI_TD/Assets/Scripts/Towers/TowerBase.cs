using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All available tower upgrades, grouped by tower type.
/// Used by skill tree system to unlock permanent bonuses.
/// </summary>
public enum TowerUpgradeType
{
    Null,
    
    // ==================== Common ====================
    DamageBoost,

    // ==================== Physical (Archer & Spear) ====================
    PhysicalAttackSpeed,
    PhysicalRange,

    // Archer-specific
    PoisonArrows,
    FireArrows,

    // Spear-specific
    BarbedSpear,
    ExplosiveTip,

    // ==================== Pyromancer ====================
    PyromancerAttackSpeed,
    BurnChance,
    BiggerFireball,
    BurnSpread,

    // ==================== Ice Mage ====================
    StrongerSlow,
    LongerSlow,
    Frostbite,
    FreezeSolid,

    // ==================== Spike Trap ====================
    SpikeTrapAttackSpeed,
    PoisonSpikes,
    BleedingSpikes,
    CripplingSpikes,

    // ==================== Blade Tower ====================
    BladeSpinSpeed,
    BleedChance,
    MoreBlades,
    ExtendedReach,

    // ==================== Rock Shower ====================
    MoreRocks,
    BiggerRocks,
    LongerShower,
    MeteorStrike,

    // ==================== Phantom Knight ====================
    MorePhantoms,
    CloserSpawn,
    SpectralChains,
    DoubleSlash
}

/// <summary>
/// Base class for all tower types. Handles targeting, rotation, attacks, debuffs, and upgrades.
/// Derived classes override specific behaviors like projectile firing and special abilities.
/// </summary>
public class TowerBase : MonoBehaviour
{
    // ==================== Combat Stats ====================
    protected EnemyBase currentEnemy;
    [SerializeField] protected int damage;
    [SerializeField] protected float attackCooldown = 1f;
    protected float lastTimeAttacked;

    // ==================== Tower Components ====================
    [Header("Tower Setup")]
    [SerializeField] protected Transform towerBody;      // Rotates horizontally to face enemy
    [SerializeField] protected Transform towerHead;      // Rotates to aim at enemy (includes vertical)
    [SerializeField] protected Transform gunPoint;       // Where projectiles spawn from
    [SerializeField] protected float rotationSpeed = 10f;

    [SerializeField] protected float attackRange = 2.5f;
    [SerializeField] protected LayerMask whatIsEnemy;
    [SerializeField] protected LayerMask whatIsTargetable;

    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected float projectileSpeed;

    [SerializeField] protected ElementType elementType = ElementType.Physical;

    [Header("Pooling")]
    [SerializeField] protected int projectilePoolAmount = 20;

    // ==================== Economy ====================
    [Header(("Tower Pricing"))]
    [SerializeField] protected int buyPrice = 1;
    [SerializeField] protected int sellPrice = 1;

    // ==================== Stat Upgrades ====================
    [Header("Stat Upgrades")]
    [SerializeField] protected bool damageBoost = false;
    [SerializeField] protected float damageBoostPercent = 0.25f;
    [Space]
    [SerializeField] protected bool attackSpeedBoost = false;
    [SerializeField] protected float attackSpeedBoostPercent = 0.20f;
    [Space]
    [SerializeField] protected bool rangeBoost = false;
    [SerializeField] protected float rangeBoostPercent = 0.20f;

    // Base values stored for recalculating after upgrades/buffs
    protected int baseDamage;
    protected float baseAttackCooldown;
    protected float baseAttackRange;

    // ==================== VFX & Animation ====================
    [Header("VFX")]
    [SerializeField] protected GameObject attackSpawnEffectPrefab;

    [Header("Animation")]
    [SerializeField] protected Animator characterAnimator;
    [SerializeField] protected string attackAnimationTrigger = "Attack";
    [SerializeField] protected float projectileSpawnDelay = 0f;  // Sync projectile with animation

    // ==================== Audio ====================
    [Header("Audio")]
    protected AudioSource audioSource;
    [SerializeField] protected AudioClip attackSound;
    [SerializeField][Range(0f, 1f)] protected float attackSoundVolume = 1f;

    // ==================== Targeting Configuration ====================
    [Header("Targeting Setup")]
    [SerializeField] protected bool targetMostAdvancedEnemy = true;   // Closest to castle
    [SerializeField] protected bool targetPriorityElement = true;     // Prefer specific element type
    [SerializeField] protected ElementType priorityElementType;
    [SerializeField] protected bool useHpTargeting = true;            // Factor HP into targeting
    [SerializeField] protected bool targetHighestHpEnemy = true;      // High HP vs low HP preference
    [SerializeField] protected bool useRandomTargeting = false;       // Chaos mode
    [SerializeField] protected LayerMask decoyLayer;                  // Decoys take targeting priority
    [SerializeField] protected LayerMask enemyLayerMask;

    // Target acquisition runs on interval to reduce Physics.OverlapSphere calls
    private float targetCheckInterval = .1f;
    private float lastTimeCheckedTarget;
    private Collider[] allocatedColliders = new Collider[100];  // Pre-allocated to avoid GC

    // ==================== Debuff System ====================
    // Slow: reduces attack speed (applied by Hexer enemies)
    protected bool isSlowed = false;
    private float slowEndTime;
    protected float slowMultiplier = 1f;  // 1.0 = normal, lower = slower attacks

    // Disable: completely prevents attacking (applied by certain enemies)
    protected bool isDisabled = false;
    private float disableEndTime;
    private float disableStartTime;
    private float disableDuration;

    // ==================== Guardian Buff System ====================
    // Guardian tower provides aura buffs to nearby towers
    protected bool hasGuardianBuff = false;
    protected float guardianDamageBuffPercent = 0f;
    protected float guardianAttackSpeedBuffPercent = 0f;
    protected float guardianRangeBuffPercent = 0f;

    // ==================== Debuff Visuals ====================
    [Header("Debuff Visuals")]
    [SerializeField] private Color disabledColor = new Color(1f, 0.2f, 0.2f, 1f);  // Red tint when disabled
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    private Renderer[] towerRenderers;
    private Color[] originalColors;
    private bool hasSavedColors = false;

    // ==================== Guardian Buff Visuals ====================
    [Header("Guardian Buff Visuals")]
    [SerializeField] private Color guardianBuffColor = new Color(1f, 0.85f, 0.3f, 1f);  // Golden glow
    [SerializeField] private float guardianGlowIntensity = 0.3f;
    [SerializeField] private float guardianPulseSpeed = 2f;

    // ==================== Public Properties ====================
    public int BuyPrice => buyPrice;
    public int SellPrice => sellPrice;

    protected virtual void Awake()
    {
        // Cache base values before any upgrades modify them
        baseDamage = damage;
        baseAttackCooldown = attackCooldown;
        baseAttackRange = attackRange;
        
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;  // Full 3D spatialization
    }

    protected virtual void Start()
    {
        // Register projectiles with object pool for efficient spawning
        if (projectilePrefab != null)
        {
            ObjectPooling.instance.Register(projectilePrefab, projectilePoolAmount);
        }

        ApplyStatUpgrades();
        SaveOriginalColors();

        // Apply any upgrades unlocked from skill tree
        if (TowerUpgradeManager.instance != null)
        {
            TowerUpgradeManager.instance.ApplyUnlockedUpgrades(this);
        }
    }

    /// <summary>
    /// Caches original material colors for debuff/buff visual effects.
    /// Must be called before any color modifications.
    /// </summary>
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

        // Skip all combat logic while disabled
        if (isDisabled) return;

        ClearTargetOutOfRange();
        UpdateTarget();
        HandleRotation();

        // DEBUG: Periodic status logging
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"{gameObject.name} - Enemy: {currentEnemy != null}, CanAttack: {CanAttack()}, Range: {attackRange}, LayerMask: {whatIsEnemy.value}");
        }

        if (CanAttack()) AttemptToAttack();
    }

    // ==================== Debuff Management ====================
    
    /// <summary>
    /// Checks and removes expired debuffs each frame.
    /// </summary>
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

    /// <summary>
    /// Renders pulsing red tint while tower is disabled.
    /// Includes fade-in and fade-out transitions.
    /// </summary>
    protected void UpdateDisabledVisual()
    {
        if (!hasSavedColors) return;
        if (!isDisabled) return;

        float elapsed = Time.time - disableStartTime;
        float remaining = disableEndTime - Time.time;

        // Calculate intensity with fade-in/fade-out at start/end of disable
        float intensity = 1f;
        if (elapsed < fadeInDuration)
        {
            intensity = elapsed / fadeInDuration;
        }
        else if (remaining < fadeOutDuration)
        {
            intensity = Mathf.Max(0f, remaining / fadeOutDuration);
        }

        // Sine wave creates smooth pulsing effect
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
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

    /// <summary>
    /// Renders golden pulsing glow while Guardian buff is active.
    /// Uses both color tint and emission for visible effect.
    /// </summary>
    protected void UpdateGuardianBuffVisual()
    {
        if (!hasSavedColors || isDisabled) return;
        if (!hasGuardianBuff) return;

        // Pulse between 40% and 100% intensity
        float pulse = (Mathf.Sin(Time.time * guardianPulseSpeed) + 1f) / 2f;
        float lerpAmount = Mathf.Lerp(0.4f, 1f, pulse);

        for (int i = 0; i < towerRenderers.Length; i++)
        {
            if (towerRenderers[i] == null) continue;
        
            Material mat = towerRenderers[i].material;
        
            // Apply color tint
            if (mat.HasProperty("_Color"))
            {
                Color tintedColor = Color.Lerp(originalColors[i], guardianBuffColor, lerpAmount);
                mat.color = tintedColor;
            }
        
            // Apply emission for actual glow (makes it visibly bright)
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                Color emissionColor = guardianBuffColor * guardianGlowIntensity * lerpAmount;
                mat.SetColor("_EmissionColor", emissionColor);
            }
        }
    }

    /// <summary>
    /// Applies a slow effect to this tower, reducing its attack speed.
    /// Used by Hexer enemies to debuff towers.
    /// </summary>
    /// <param name="slowPercent">Percentage to reduce attack speed (0.0 to 0.9)</param>
    /// <param name="duration">How long the slow lasts in seconds</param>
    public void ApplySlow(float slowPercent, float duration)
    {
        isSlowed = true;
        slowEndTime = Time.time + duration;
        // Clamp to 90% max slow to prevent complete stop
        slowMultiplier = 1f - Mathf.Clamp(slowPercent, 0f, 0.9f);
    }

    private void RemoveSlow()
    {
        isSlowed = false;
        slowMultiplier = 1f;
    }

    /// <summary>
    /// Disables this tower completely for the specified duration.
    /// Prevents the tower from attacking and shows red pulsing visual.
    /// </summary>
    /// <param name="duration">How long the tower remains disabled in seconds</param>
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

    /// <summary>Returns whether this tower is currently slowed by a debuff.</summary>
    public bool IsSlowed() => isSlowed;

    /// <summary>Returns whether this tower is currently disabled by a debuff.</summary>
    public bool IsDisabled() => isDisabled;

    // ==================== Combat ====================
    
    protected DamageInfo CreateDamageInfo()
    {
        return new DamageInfo(damage, elementType);
    }

    /// <summary>
    /// Clears current target if it has moved out of attack range.
    /// </summary>
    protected virtual void ClearTargetOutOfRange()
    {
        if (currentEnemy == null) return;
        if (Vector3.Distance(currentEnemy.transform.position, transform.position) > attackRange) 
            currentEnemy = null;
    }

    /// <summary>
    /// Periodically searches for new target to reduce OverlapSphere calls.
    /// </summary>
    protected void UpdateTarget()
    {
        if (Time.time > lastTimeCheckedTarget + targetCheckInterval || currentEnemy == null)
        {
            lastTimeCheckedTarget = Time.time;
            currentEnemy = FindEnemyWithinRange();
        }
    }

    /// <summary>
    /// Finds best target using priority system:
    /// 1. Decoys (always targeted first if present)
    /// 2. Priority element type (if enabled)
    /// 3. HP-based or distance-based targeting
    /// </summary>
    protected virtual EnemyBase FindEnemyWithinRange()
    {
        List<EnemyBase> decoyTargets = new List<EnemyBase>();
        List<EnemyBase> priorityTargets = new List<EnemyBase>();
        List<EnemyBase> allTargets = new List<EnemyBase>();

        // First priority: check for decoys
        int decoysAround = Physics.OverlapSphereNonAlloc(transform.position, attackRange, allocatedColliders, decoyLayer);

        if (decoysAround > 0)
        {
            for (int i = 0; i < decoysAround; i++)
            {
                EnemyBase decoy = allocatedColliders[i].GetComponent<EnemyBase>();
                if (decoy == null) continue;
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

        // Second priority: regular enemies
        int enemiesAround = Physics.OverlapSphereNonAlloc(transform.position, attackRange, allocatedColliders, enemyLayerMask);
        if (enemiesAround == 0) return null;

        for (int i = 0; i < enemiesAround; i++)
        {
            EnemyBase newEnemy = allocatedColliders[i].GetComponent<EnemyBase>();
            if (newEnemy == null) continue;
            if (!newEnemy.IsTargetable()) continue;
            if (newEnemy.IsInvisible()) continue;

            float distanceToEnemy = Vector3.Distance(transform.position, newEnemy.transform.position);
            if (distanceToEnemy > attackRange) continue;

            ElementType enemyElement = newEnemy.GetElementType();
            allTargets.Add(newEnemy);

            // Collect priority element targets separately
            if (enemyElement == priorityElementType)
            {
                priorityTargets.Add(newEnemy);
            }
        }

        // Prefer priority element targets if enabled
        if (targetPriorityElement && priorityTargets.Count > 0)
        {
            return ChooseEnemyToTarget(priorityTargets);
        }

        if (allTargets.Count > 0)
        {
            return ChooseEnemyToTarget(allTargets);
        }

        return null;
    }

    /// <summary>
    /// Selects best enemy from list based on targeting configuration.
    /// Uses HP and/or distance as sorting criteria.
    /// </summary>
    private EnemyBase ChooseEnemyToTarget(List<EnemyBase> targets)
    {
        EnemyBase enemyToTarget = null;

        // Random targeting ignores all other criteria
        if (useRandomTargeting)
        {
            return targets[Random.Range(0, targets.Count)];
        }

        if (useHpTargeting)
        {
            // Primary sort by HP, secondary sort by distance (for ties)
            float bestHp = targetHighestHpEnemy ? float.MinValue : float.MaxValue;
            float bestDistance = targetMostAdvancedEnemy ? float.MaxValue : float.MinValue;

            foreach (EnemyBase enemy in targets)
            {
                float enemyHp = enemy.GetEnemyHp();
                float remainingDistance = enemy.GetRemainingDistance();

                bool isBetterHp = targetHighestHpEnemy ? enemyHp > bestHp : enemyHp < bestHp;
                bool shouldTarget = false;

                if (isBetterHp)
                {
                    shouldTarget = true;
                }
                else if (Mathf.Approximately(enemyHp, bestHp))
                {
                    // Tie-breaker: use distance
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
            // Distance-only targeting (most advanced = closest to castle)
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

    /// <summary>
    /// Validates target is still active before attacking.
    /// </summary>
    protected void AttemptToAttack()
    {
        // Enemy might have died or been pooled since last check
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

        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(attackAnimationTrigger);
        }

        PlayAttackSound();

        // Delay projectile to sync with animation (e.g., arrow release frame)
        if (projectileSpawnDelay > 0)
        {
            Invoke("FireProjectile", projectileSpawnDelay);
        }
        else
        {
            FireProjectile();
        }
    }

    protected virtual void PlayAttackSound()
    {
        if (attackSound != null && audioSource != null)
        {
            audioSource.clip = attackSound;
            audioSource.volume = attackSoundVolume;
            audioSource.Play();
        }
    }

    /// <summary>
    /// Spawns projectile from gun point aimed at current enemy.
    /// Uses raycast to determine hit point for projectile targeting.
    /// </summary>
    protected virtual void FireProjectile()
    {
        // Muzzle flash VFX
        if (attackSpawnEffectPrefab != null && gunPoint != null)
        {
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, gunPoint.position, Quaternion.identity, 2f);
        }

        Vector3 directionToEnemy = DirectionToEnemyFrom(gunPoint);

        // Raycast to find exact hit point on enemy
        if (Physics.Raycast(gunPoint.position, directionToEnemy, out RaycastHit hitInfo, Mathf.Infinity, whatIsTargetable))
        {
            IDamageable damageable = hitInfo.transform.GetComponent<IDamageable>();
            if (damageable == null) return;

            // Slight offset to prevent spawning inside gun model
            Vector3 spawnPosition = gunPoint.position + directionToEnemy * 0.1f;

            GameObject newProjectile = ObjectPooling.instance.Get(projectilePrefab);
            newProjectile.transform.position = spawnPosition;
            newProjectile.transform.rotation = gunPoint.rotation;
            newProjectile.SetActive(true);

            newProjectile.GetComponent<TowerProjectileBase>().SetupProjectile(hitInfo.point, damageable, CreateDamageInfo(), projectileSpeed);
        }
    }

    /// <summary>
    /// Checks if tower can attack based on cooldown and target availability.
    /// Slow debuff increases effective cooldown.
    /// </summary>
    protected virtual bool CanAttack()
    {
        // slowMultiplier < 1 means slower attacks (divide increases cooldown)
        float effectiveCooldown = attackCooldown / slowMultiplier;
        return Time.time > lastTimeAttacked + effectiveCooldown && currentEnemy != null;
    }

    // ==================== Rotation ====================
    
    protected virtual void HandleRotation()
    {
        RotateTowardsEnemy();
        RotateBodyTowardsEnemy();
    }

    /// <summary>
    /// Rotates tower head (turret) to aim at enemy including vertical angle.
    /// </summary>
    protected virtual void RotateTowardsEnemy()
    {
        if (currentEnemy == null || towerHead == null) return;

        Vector3 directionToEnemy = DirectionToEnemyFrom(towerHead);
        Quaternion lookRotation = Quaternion.LookRotation(directionToEnemy);
        Vector3 rotation = Quaternion.Lerp(towerHead.rotation, lookRotation, rotationSpeed * Time.deltaTime).eulerAngles;
        towerHead.rotation = Quaternion.Euler(rotation);
    }

    /// <summary>
    /// Rotates tower body horizontally to face enemy (Y-axis only).
    /// </summary>
    protected void RotateBodyTowardsEnemy()
    {
        if (towerBody == null || currentEnemy == null) return;

        Vector3 directionToEnemy = DirectionToEnemyFrom(towerBody);
        directionToEnemy.y = 0;  // Flatten to horizontal plane

        Quaternion lookRotation = Quaternion.LookRotation(directionToEnemy);
        towerBody.rotation = Quaternion.Slerp(towerBody.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Calculates normalized direction from a transform to enemy's center mass.
    /// </summary>
    protected Vector3 DirectionToEnemyFrom(Transform startPosition)
    {
        return (currentEnemy.GetCenterPoint() - startPosition.position).normalized;
    }

    // ==================== Upgrades & Buffs ====================
    
    /// <summary>
    /// Recalculates all stats from base values plus upgrade/buff percentages.
    /// Call this after any upgrade or buff change.
    /// </summary>
    protected virtual void ApplyStatUpgrades()
    {
        float totalDamageBoost = 0f;
        float totalAttackSpeedBoost = 0f;
        float totalRangeBoost = 0f;

        // Skill tree upgrades
        if (damageBoost) totalDamageBoost += damageBoostPercent;
        if (attackSpeedBoost) totalAttackSpeedBoost += attackSpeedBoostPercent;
        if (rangeBoost) totalRangeBoost += rangeBoostPercent;

        // Guardian tower buff (stacks with upgrades)
        if (hasGuardianBuff)
        {
            totalDamageBoost += guardianDamageBuffPercent;
            totalAttackSpeedBoost += guardianAttackSpeedBuffPercent;
            totalRangeBoost += guardianRangeBuffPercent;
        }

        // Apply all bonuses from base values
        damage = Mathf.RoundToInt(baseDamage * (1f + totalDamageBoost));
        attackCooldown = baseAttackCooldown * (1f - totalAttackSpeedBoost);
        attackRange = baseAttackRange * (1f + totalRangeBoost);
    }

    /// <summary>
    /// Applies or removes a tower upgrade. Override in derived classes to handle specific upgrades.
    /// Automatically recalculates stats after upgrade changes.
    /// </summary>
    /// <param name="upgradeType">The type of upgrade to apply</param>
    /// <param name="enabled">True to enable the upgrade, false to disable it</param>
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

    // ==================== Guardian Buff ====================
    
    /// <summary>
    /// Applies a Guardian tower buff, boosting damage, attack speed, and range.
    /// Adds a glowing emission effect to visualize the buff.
    /// </summary>
    /// <param name="damagePercent">Percentage damage boost</param>
    /// <param name="attackSpeedPercent">Percentage attack speed boost</param>
    /// <param name="rangePercent">Percentage range boost</param>
    /// <param name="glowIntensity">Intensity of the buff glow effect</param>
    public void ApplyGuardianBuff(float damagePercent, float attackSpeedPercent, float rangePercent, float glowIntensity = 0.7f)
    {
        hasGuardianBuff = true;
        guardianDamageBuffPercent = damagePercent;
        guardianAttackSpeedBuffPercent = attackSpeedPercent;
        guardianRangeBuffPercent = rangePercent;
        guardianGlowIntensity = glowIntensity;
        ApplyStatUpgrades();
    }

    /// <summary>
    /// Removes the Guardian tower buff and restores original stats and visual appearance.
    /// </summary>
    public void RemoveGuardianBuff()
    {
        hasGuardianBuff = false;
        guardianDamageBuffPercent = 0f;
        guardianAttackSpeedBuffPercent = 0f;
        guardianRangeBuffPercent = 0f;
        ApplyStatUpgrades();
        RestoreOriginalColors();
    
        // Disable emission glow
        if (towerRenderers != null)
        {
            for (int i = 0; i < towerRenderers.Length; i++)
            {
                if (towerRenderers[i] != null && towerRenderers[i].material.HasProperty("_EmissionColor"))
                {
                    towerRenderers[i].material.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }

    // ==================== Editor Visualization ====================
    
    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    // ==================== Public Getters ====================
    
    /// <summary>Returns whether this tower currently has a Guardian buff active.</summary>
    public bool HasGuardianBuff() => hasGuardianBuff;

    /// <summary>Returns the cost to buy this tower.</summary>
    public int GetBuyPrice() { return buyPrice; }

    /// <summary>Returns the amount refunded when selling this tower.</summary>
    public int GetSellPrice() { return sellPrice; }

    /// <summary>Returns the current slow multiplier (1.0 = normal speed, lower = slowed).</summary>
    public float GetSlowMultiplier() => slowMultiplier;

    /// <summary>Returns the element type of this tower's damage.</summary>
    public ElementType GetElementType() => elementType;
}