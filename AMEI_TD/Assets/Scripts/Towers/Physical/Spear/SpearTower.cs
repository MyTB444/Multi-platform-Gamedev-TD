using UnityEngine;

/// <summary>
/// Spear-throwing tower with prediction and DoT/explosive upgrades.
/// Similar to TowerArcher but with straight-line projectiles instead of arcing arrows.
/// 
/// Requires valid velocity tracking (2+ frames) before attacking to ensure
/// accurate prediction calculations.
/// </summary>
public class SpearTower : TowerBase
{
    // ==================== SPEAR SETUP ====================
    [Header("Spear Setup")]
    [SerializeField] private GameObject spearVisual;  // Spear shown in hand before throwing
    
    // ==================== PREDICTION ====================
    [Header("Prediction")]
    [SerializeField] private float baseFlightTime = 0.2f;    // Base time for spear to reach target
    [SerializeField] private float speedMultiplier = 0.1f;   // How much enemy speed affects prediction
    
    // ==================== ANIMATION ====================
    [Header("Animation Timing")]
    [SerializeField] private float spearRespawnDelay = 1.5f;    // Delay before new spear appears
    [SerializeField] private float throwAnimationDelay = 0.5f;  // Not currently used
    
    [Header("Spawn Offset")]
    [SerializeField] private float forwardSpawnOffset = 0.5f;   // Spawn projectile ahead of hand
    
    // ==================== VFX ====================
    [Header("Spear Visual VFX")]
    [SerializeField] private Transform spearVisualVFXPoint;  // VFX attach point on visual spear
    private GameObject activeSpearVisualVFX;                  // Currently attached VFX (bleed)
    
    // ==================== UPGRADES ====================
    [Header("Spear Effects")]
    [SerializeField] private bool bleedSpear = false;
    [SerializeField] private float bleedDamage = 3f;
    [SerializeField] private float bleedDuration = 4f;
    [SerializeField] private GameObject bleedSpearVFX;

    [SerializeField] private bool explosiveTip = false;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float explosionDamage = 10f;
    [SerializeField] private GameObject explosionVFX;
    
    // ==================== RUNTIME STATE ====================
    private bool isAttacking = false;
    private Vector3 enemyVelocity;        // Tracked enemy velocity
    private Vector3 lastEnemyPosition;
    private int velocityFrameCount = 0;   // Frames of velocity data collected
    
    // Target locked at attack start
    private Vector3 lockedTargetPosition;
    private Vector3 lockedVelocity;
    private IDamageable lockedDamageable;
    private EnemyBase lockedTarget;
    
    
    protected override void Start()
    {
        base.Start();
        UpdateSpearVisualVFX();
    }
    
    protected override void FixedUpdate()
    {
        UpdateEnemyVelocity();
        base.FixedUpdate();
    }
    
    /// <summary>
    /// Handles upgrade state changes from skill tree.
    /// </summary>
    public override void SetUpgrade(TowerUpgradeType upgradeType, bool enabled)
    {
        base.SetUpgrade(upgradeType, enabled);

        switch (upgradeType)
        {
            case TowerUpgradeType.PhysicalAttackSpeed:
                attackSpeedBoost = enabled;
                ApplyStatUpgrades();
                break;
            case TowerUpgradeType.PhysicalRange:
                rangeBoost = enabled;
                ApplyStatUpgrades();
                break;
            case TowerUpgradeType.BarbedSpear:
                bleedSpear = enabled;
                UpdateSpearVisualVFX();
                break;
            case TowerUpgradeType.ExplosiveTip:
                explosiveTip = enabled;
                break;
        }
    }
    
    /// <summary>
    /// Tracks enemy velocity by comparing positions between frames.
    /// Uses locked target during attack, otherwise current target.
    /// Requires 2+ frames of data for valid tracking.
    /// </summary>
    private void UpdateEnemyVelocity()
    {
        EnemyBase targetToTrack = isAttacking && lockedTarget != null ? lockedTarget : currentEnemy;
        
        if (targetToTrack == null || !targetToTrack.gameObject.activeSelf)
        {
            enemyVelocity = Vector3.zero;
            lastEnemyPosition = Vector3.zero;
            velocityFrameCount = 0;
            return;
        }
        
        Vector3 currentPos = targetToTrack.transform.position;
        
        if (lastEnemyPosition != Vector3.zero)
        {
            velocityFrameCount++;
            
            // Only calculate velocity after first frame (need 2 positions)
            if (velocityFrameCount > 1)
            {
                enemyVelocity = (currentPos - lastEnemyPosition) / Time.fixedDeltaTime;
            }
        }
        
        lastEnemyPosition = currentPos;
    }
    
    /// <summary>
    /// Updates VFX on visual spear based on current upgrades.
    /// </summary>
    private void UpdateSpearVisualVFX()
    {
        // Clear existing VFX
        if (activeSpearVisualVFX != null)
        {
            ObjectPooling.instance.Return(activeSpearVisualVFX);
            activeSpearVisualVFX = null;
        }
    
        // Attach bleed VFX if upgraded
        if (bleedSpear && bleedSpearVFX != null)
        {
            Transform spawnPoint = spearVisualVFXPoint != null ? spearVisualVFXPoint : spearVisual.transform;
            activeSpearVisualVFX = ObjectPooling.instance.GetVFXWithParent(bleedSpearVFX, spawnPoint, -1f);
            activeSpearVisualVFX.transform.localPosition = Vector3.zero;
        }
    }
    
    /// <summary>
    /// Calculates predicted target position using velocity.
    /// Uses simpler prediction than archer (no path-awareness for most cases).
    /// </summary>
    private Vector3 PredictTargetPosition()
    {
        EnemyBase targetToPredict = isAttacking && lockedTarget != null ? lockedTarget : currentEnemy;

        if (targetToPredict == null || !targetToPredict.gameObject.activeSelf) return Vector3.zero;

        Vector3 enemyCenter = targetToPredict.GetCenterPoint();
        float enemySpeed = enemyVelocity.magnitude;
    
        // Simple linear prediction
        float predictionTime = baseFlightTime + (enemySpeed * speedMultiplier);
    
        Vector3 predictedPos = enemyCenter + (enemyVelocity * predictionTime);
        predictedPos.y = enemyCenter.y;  // Keep at enemy height

        return predictedPos;
    }

    /// <summary>
    /// Path-aware prediction that handles waypoint turns.
    /// Note: Not currently used by main prediction, kept for potential future use.
    /// </summary>
    private Vector3 GetPathAwarePrediction(EnemyBase target, float predictionTime)
    {
        Vector3 currentPos = target.transform.position;
        float speed = enemyVelocity.magnitude;
    
        // Not moving - return current position
        if (speed < 0.1f) return target.GetCenterPoint();
    
        float travelDistance = speed * predictionTime;
        float distanceToWaypoint = target.GetDistanceToNextWaypoint();
    
        // Simple case: won't reach next waypoint
        if (distanceToWaypoint <= 0 || travelDistance < distanceToWaypoint)
        {
            return currentPos + (enemyVelocity * predictionTime);
        }
    
        // Complex case: will turn at waypoint
        float timeToWaypoint = distanceToWaypoint / speed;
        Vector3 waypointPos = target.GetNextWaypointPosition();
        float remainingTime = predictionTime - timeToWaypoint;
        Vector3 directionAfterTurn = target.GetDirectionAfterNextWaypoint();
    
        return waypointPos + (directionAfterTurn * speed * remainingTime);
    }
    
    /// <summary>
    /// Rotates tower toward predicted position.
    /// </summary>
    protected override void HandleRotation()
    {
        EnemyBase targetToFace = isAttacking && lockedTarget != null ? lockedTarget : currentEnemy;
        
        if (targetToFace == null || towerBody == null) return;
        
        // Use predicted position for aiming
        Vector3 targetPos = PredictTargetPosition();
        if (targetPos == Vector3.zero) targetPos = targetToFace.GetCenterPoint();
        
        Vector3 direction = targetPos - towerBody.position;
        direction.y = 0;  // Keep rotation horizontal
        
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            towerBody.rotation = Quaternion.Slerp(towerBody.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Additional attack conditions:
    /// - Must have target
    /// - Must be facing enemy (within 15 degrees)
    /// - Must have valid velocity tracking (2+ frames)
    /// </summary>
    protected override bool CanAttack()
    {
        if (currentEnemy == null) return false;
    
        // Check if facing enemy
        if (!IsFacingEnemy(15f)) return false;
    
        // Require valid velocity tracking for accurate prediction
        bool hasValidTracking = velocityFrameCount > 1;
        return base.CanAttack() && !isAttacking && hasValidTracking;
    }

    /// <summary>
    /// Checks if tower is facing the enemy within angle threshold.
    /// </summary>
    private bool IsFacingEnemy(float maxAngle)
    {
        if (currentEnemy == null || towerBody == null) return false;
    
        Vector3 directionToEnemy = currentEnemy.transform.position - towerBody.position;
        directionToEnemy.y = 0;
    
        float angle = Vector3.Angle(towerBody.forward, directionToEnemy);
    
        return angle <= maxAngle;
    }
    
    /// <summary>
    /// Initiates attack - locks target and triggers throw animation.
    /// </summary>
    protected override void Attack()
    {
        lastTimeAttacked = Time.time;
        isAttacking = true;
        
        // Lock target data for throw
        if (currentEnemy != null)
        {
            lockedTarget = currentEnemy;
            lockedVelocity = enemyVelocity;
            lockedTargetPosition = PredictTargetPosition();
            lockedDamageable = currentEnemy.GetComponent<IDamageable>();
        }
        
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(attackAnimationTrigger);
        }
    }
    
    /// <summary>
    /// Called by Animation Event when spear leaves hand.
    /// </summary>
    public void OnThrowSpear()
    {
        PlayAttackSound();
        FireSpear();
        
        // Hide visual spear
        if (spearVisual != null)
        {
            spearVisual.SetActive(false);
        }
        
        // Schedule spear to reappear
        Invoke("OnSpearReady", spearRespawnDelay);
    }
    
    /// <summary>
    /// Called after delay - shows new spear in hand, resets attack state.
    /// </summary>
    public void OnSpearReady()
    {
        if (spearVisual != null)
        {
            spearVisual.SetActive(true);
        }
        
        isAttacking = false;
        ClearLockedTarget();
    }
    
    /// <summary>
    /// Spawns spear projectile aimed at predicted position.
    /// </summary>
    private void FireSpear()
    {
        if (lockedDamageable == null)
        {
            ClearLockedTarget();
            return;
        }
        
        // Recalculate prediction if target still valid
        if (lockedTarget != null && lockedTarget.gameObject.activeSelf)
        {
            lockedTargetPosition = PredictTargetPosition();
        }

        // Calculate spawn position and rotation
        Vector3 spawnPos = spearVisual.transform.position;
        Quaternion spawnRot = spearVisual.transform.rotation;

        // Apply forward offset (spear model points up, so up = forward)
        Vector3 fireDirection = spawnRot * Vector3.up;
        spawnPos += fireDirection * forwardSpawnOffset;

        // Get spear from pool
        GameObject newSpear = ObjectPooling.instance.Get(projectilePrefab);
        newSpear.transform.position = spawnPos;
        newSpear.transform.rotation = spawnRot;
        newSpear.SetActive(true);
    
        // Configure spear projectile
        SpearProjectile spear = newSpear.GetComponent<SpearProjectile>();

        spear.SetupSpear(lockedTargetPosition, lockedDamageable, CreateDamageInfo(), projectileSpeed);

        // Apply upgrades
        if (bleedSpear)
        {
            spear.SetBleedEffect(bleedDamage, bleedDuration, elementType, bleedSpearVFX);
        }

        if (explosiveTip)
        {
            spear.SetExplosiveEffect(explosionRadius, explosionDamage, elementType, whatIsEnemy, explosionVFX);
        }
    }
    
    /// <summary>
    /// Clears locked target data after throw completes.
    /// </summary>
    private void ClearLockedTarget()
    {
        lockedTarget = null;
        lockedDamageable = null;
        lockedVelocity = Vector3.zero;
        lockedTargetPosition = Vector3.zero;
    }
}