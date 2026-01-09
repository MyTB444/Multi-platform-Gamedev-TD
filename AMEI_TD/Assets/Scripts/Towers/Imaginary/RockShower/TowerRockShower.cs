using System.Collections;
using UnityEngine;

/// <summary>
/// Tower that rains rocks on enemies over a duration. Predicts enemy movement
/// and spawns rocks above the predicted position. Can optionally finish with a meteor strike.
/// </summary>
public class TowerRockShower : TowerBase
{
    // ==================== ROCK SHOWER SETTINGS ====================
    [Header("Rock Shower")]
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private float spawnHeight = 10f;      // Height above target to spawn rocks
    [SerializeField] private float spawnRadius = 1.5f;     // Random spread radius for rock spawns
    
    [Header("Shower Settings")]
    [SerializeField] private float showerDuration = 3f;    // Total duration of rock rain
    [SerializeField] private float timeBetweenRocks = 0.2f; // Spawn interval
    [SerializeField] private float rockSizeMin = 0.5f;     // Minimum random rock size
    [SerializeField] private float rockSizeMax = 1.5f;     // Maximum random rock size
    [SerializeField] private float rockSpeedMin = 8f;      // Minimum fall speed
    [SerializeField] private float rockSpeedMax = 15f;     // Maximum fall speed
    
    // ==================== PREDICTION ====================
    [Header("Prediction")]
    [SerializeField] private float predictionMultiplier = 0.5f;  // How much to lead the target
    
    [Header("Pooling")]
    [SerializeField] private int rockPoolAmount = 30;
    
    // ==================== UPGRADES ====================
    [Header("Rock Shower Upgrades")]
    [SerializeField] private bool moreRocks = false;       // Spawns additional rocks
    [SerializeField] private int bonusRocks = 3;
    [Space]
    [SerializeField] private bool biggerRocks = false;     // Increases rock size
    [SerializeField] private float rockSizeBonus = 0.3f;
    [Space]
    [SerializeField] private bool longerShower = false;    // Extends shower duration
    [SerializeField] private float bonusShowerDuration = 1f;
    [Space]
    [SerializeField] private bool meteorStrike = false;    // Finishes with a big meteor
    [SerializeField] private GameObject meteorPrefab;
    [SerializeField] private float meteorSize = 3f;
    [SerializeField] private float meteorHeight = 3f;      // Extra height above normal spawn
    [SerializeField] private float meteorDamageMultiplier = 3f;
    [SerializeField] private float meteorSpeed = 4f;
    
    // ==================== RUNTIME STATE ====================
    private bool isShowering = false;        // Currently in shower coroutine
    private bool isAttacking = false;        // Attack initiated, waiting for animation
    private Vector3 enemyVelocity;           // Tracked enemy movement for prediction
    private Vector3 lastEnemyPosition;       // Previous frame position for velocity calc
    private Vector3 lockedTargetPosition;    // Position locked at attack start
    
    protected override void Start()
    {
        base.Start();

        // Register prefabs with object pool
        if (rockPrefab != null)
        {
            ObjectPooling.instance.Register(rockPrefab, rockPoolAmount);
        }
    
        if (meteorPrefab != null)
        {
            ObjectPooling.instance.Register(meteorPrefab, 3);
        }
    }
    
    protected override void FixedUpdate()
    {
        UpdateDebuffs();
        UpdateDisabledVisual();
    
        if (isDisabled) return;
    
        // Track enemy velocity for prediction
        UpdateEnemyVelocity();

        if (!isShowering)
        {
            // Normal targeting behavior when not showering
            ClearTargetOutOfRange();
            UpdateTarget();
            HandleRotation();

            if (CanAttack()) AttemptToAttack();
        }
        else
        {
            // During shower: just track rotation, handle dead targets
            if (currentEnemy != null && !currentEnemy.gameObject.activeSelf)
            {
                currentEnemy = null;
            }
    
            HandleRotation();
        }
    }
    
    /// <summary>
    /// Handles upgrade state changes from the skill tree.
    /// </summary>
    public override void SetUpgrade(TowerUpgradeType upgradeType, bool enabled)
    {
        base.SetUpgrade(upgradeType, enabled);
    
        switch (upgradeType)
        {
            case TowerUpgradeType.MoreRocks:
                moreRocks = enabled;
                break;
            case TowerUpgradeType.BiggerRocks:
                biggerRocks = enabled;
                break;
            case TowerUpgradeType.LongerShower:
                longerShower = enabled;
                break;
            case TowerUpgradeType.MeteorStrike:
                meteorStrike = enabled;
                break;
        }
    }
    
    /// <summary>
    /// Calculates enemy velocity by comparing positions between frames.
    /// Used for position prediction.
    /// </summary>
    private void UpdateEnemyVelocity()
    {
        if (currentEnemy == null)
        {
            enemyVelocity = Vector3.zero;
            lastEnemyPosition = Vector3.zero;
            return;
        }
        
        Vector3 currentPos = currentEnemy.transform.position;
        
        // Calculate velocity from position delta
        if (lastEnemyPosition != Vector3.zero)
        {
            enemyVelocity = (currentPos - lastEnemyPosition) / Time.fixedDeltaTime;
        }
        
        lastEnemyPosition = currentPos;
    }
    
    /// <summary>
    /// Predicts where the enemy will be when rocks land.
    /// </summary>
    /// <param name="fallTime">Time for rocks to fall</param>
    /// <returns>Predicted world position</returns>
    private Vector3 GetPredictedPosition(float fallTime)
    {
        if (currentEnemy == null) return Vector3.zero;
        
        Vector3 currentPos = currentEnemy.transform.position;
        // Project position forward based on velocity and prediction multiplier
        return currentPos + (enemyVelocity * fallTime * predictionMultiplier);
    }
    
    /// <summary>
    /// Initiates attack - locks target position and triggers animation.
    /// </summary>
    protected override void Attack()
    {
        if (isShowering || isAttacking) return;
        
        lastTimeAttacked = Time.time;
        isAttacking = true;
        
        // Calculate and lock predicted position at attack start
        if (currentEnemy != null)
        {
            float avgSpeed = (rockSpeedMin + rockSpeedMax) / 2f;
            float fallTime = spawnHeight / avgSpeed;
            lockedTargetPosition = GetPredictedPosition(fallTime);
            
            // Fallback to current position if prediction fails
            if (lockedTargetPosition == Vector3.zero)
            {
                lockedTargetPosition = currentEnemy.transform.position;
            }
        }
        
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(attackAnimationTrigger);
        }
    }
    
    /// <summary>
    /// Called by Animation Event to start the rock shower.
    /// </summary>
    public void OnStartRockShower()
    {
        PlayAttackSound();
        StartCoroutine(RockShowerRoutine());
    }
    
    /// <summary>
    /// Main shower coroutine - spawns rocks over duration, optionally ends with meteor.
    /// </summary>
    private IEnumerator RockShowerRoutine()
    {
        isShowering = true;

        float elapsed = 0f;
        float finalDuration = longerShower ? showerDuration + bonusShowerDuration : showerDuration;

        // Calculate spawn interval - more rocks = faster spawns to fit in duration
        float finalTimeBetweenRocks = moreRocks ? 
            finalDuration / ((finalDuration / timeBetweenRocks) + bonusRocks) : 
            timeBetweenRocks;

        // Spawn rocks at intervals
        while (elapsed < finalDuration)
        {
            if (!isDisabled)
            {
                SpawnRock(lockedTargetPosition);
            }

            yield return new WaitForSeconds(finalTimeBetweenRocks);
            elapsed += finalTimeBetweenRocks;
        }

        // Finish with meteor if upgraded
        if (meteorStrike && !isDisabled)
        {
            SpawnMeteor(lockedTargetPosition);
        }

        isShowering = false;
        isAttacking = false;
    }
    
    /// <summary>
    /// Spawns a single rock with random size/speed/position within spawn radius.
    /// </summary>
    private void SpawnRock(Vector3 targetPosition)
    {
        float randomSpeed = Random.Range(rockSpeedMin, rockSpeedMax);

        // Random offset within spawn radius
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = targetPosition + new Vector3(randomOffset.x, spawnHeight, randomOffset.y);

        // Spawn VFX at rock spawn location
        if (attackSpawnEffectPrefab != null)
        {
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, spawnPos, Quaternion.identity, 2f);
        }

        // Get rock from pool
        GameObject rock = ObjectPooling.instance.Get(rockPrefab);
        rock.transform.position = spawnPos;
        rock.transform.rotation = Random.rotation;  // Random tumble
        rock.SetActive(true);

        // Apply size with upgrade bonus
        float sizeMultiplier = biggerRocks ? 1f + rockSizeBonus : 1f;
        float randomSize = Random.Range(rockSizeMin, rockSizeMax) * sizeMultiplier;
        rock.transform.localScale = rockPrefab.transform.localScale * randomSize;

        // Bigger rocks deal more damage (linear scaling)
        float scaledDamage = damage * randomSize;
        DamageInfo rockDamageInfo = new DamageInfo(scaledDamage, elementType);

        // Configure projectile
        RockProjectile projectile = rock.GetComponent<RockProjectile>();
        projectile.Setup(rockDamageInfo, whatIsEnemy, randomSpeed, randomSize);
    }
    
    /// <summary>
    /// Spawns the finishing meteor. Re-targets closest enemy for accuracy.
    /// </summary>
    private void SpawnMeteor(Vector3 fallbackPosition)
    {
        Vector3 targetPosition = fallbackPosition;

        // Find closest enemy for meteor targeting (more accurate than locked position)
        Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange, whatIsEnemy);

        if (enemies.Length > 0)
        {
            float closestDist = float.MaxValue;
            Transform closestEnemy = null;

            foreach (Collider col in enemies)
            {
                if (!col.gameObject.activeSelf) continue;

                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestEnemy = col.transform;
                }
            }

            if (closestEnemy != null)
            {
                targetPosition = closestEnemy.position;
            }
        }

        // Meteor spawns higher than regular rocks
        Vector3 spawnPos = targetPosition + Vector3.up * (spawnHeight + meteorHeight);

        // Bigger VFX for meteor spawn
        if (attackSpawnEffectPrefab != null)
        {
            Vector3 scaledSize = attackSpawnEffectPrefab.transform.localScale * 2f;
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, spawnPos, Quaternion.identity, scaledSize, 2f);
        }
        
        // Use meteor prefab if available, otherwise scaled rock
        GameObject prefabToUse = meteorPrefab != null ? meteorPrefab : rockPrefab;
    
        GameObject meteor = ObjectPooling.instance.Get(prefabToUse);
        meteor.transform.position = spawnPos;
        meteor.transform.rotation = Random.rotation;
        meteor.SetActive(true);
        meteor.transform.localScale = prefabToUse.transform.localScale * meteorSize;

        // Meteor damage = base * size * multiplier
        float scaledDamage = damage * meteorSize * meteorDamageMultiplier;
        DamageInfo meteorDamageInfo = new DamageInfo(scaledDamage, elementType);

        RockProjectile projectile = meteor.GetComponent<RockProjectile>();
        projectile.Setup(meteorDamageInfo, whatIsEnemy, meteorSpeed, meteorSize);
        projectile.SetAsMeteor();  // Always plays impact sound
    }
    
    /// <summary>
    /// Additional attack conditions - can't attack while showering.
    /// </summary>
    protected override bool CanAttack()
    {
        return base.CanAttack() && !isShowering && !isAttacking;
    }
    
    /// <summary>
    /// Rotates tower body to face current enemy.
    /// </summary>
    protected override void HandleRotation()
    {
        if (currentEnemy == null || towerBody == null) return;
    
        Vector3 direction = currentEnemy.transform.position - towerBody.position;
        direction.y = 0;  // Keep rotation horizontal
    
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            towerBody.rotation = Quaternion.Slerp(towerBody.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Editor visualization - shows attack range, predicted position, and spawn area.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Show predicted spawn area when targeting
        if (currentEnemy != null)
        {
            float avgSpeed = (rockSpeedMin + rockSpeedMax) / 2f;
            float fallTime = spawnHeight / avgSpeed;
            Vector3 predicted = GetPredictedPosition(fallTime);
            
            // Spawn radius at predicted position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(predicted, spawnRadius);
            
            // Line showing spawn height
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(predicted, predicted + Vector3.up * spawnHeight);
            Gizmos.DrawWireSphere(predicted + Vector3.up * spawnHeight, 0.5f);
        }
        
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}