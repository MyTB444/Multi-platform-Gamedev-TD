using UnityEngine;

/// <summary>
/// Archer tower that fires arc-trajectory arrows with prediction.
/// Features bow draw/release animation sync and DoT upgrades (poison/fire).
/// 
/// Prediction system accounts for enemy velocity and waypoint turns
/// to accurately lead moving targets.
/// </summary>
public class TowerArcher : TowerBase
{
    // ==================== ARCHER SETUP ====================
    [Header("Archer Setup")]
    [SerializeField] private BowController bowController;  // Controls bow visual state
    [SerializeField] private GameObject arrowVisual;       // Arrow shown on bow before firing
    
    // ==================== ANIMATION ====================
    [Header("Animation Timing")]
    [SerializeField] private float baseAnimationLength = 1f;  // Base attack animation duration
    
    // ==================== PREDICTION ====================
    [Header("Prediction")]
    [SerializeField] private float baseFlightTime = 0.3f;     // Base time for arrow to reach target
    [SerializeField] private float speedMultiplier = 0.15f;   // How much enemy speed affects prediction
    
    // ==================== VFX ====================
    [Header("Arrow Visual VFX")]
    [SerializeField] private Transform arrowVisualVFXPoint;  // VFX attach point on visual arrow
    private GameObject activeArrowVisualVFX;                  // Currently attached VFX (fire/poison)
    
    // ==================== UPGRADES ====================
    [Header("Arrow Effects")]
    [SerializeField] private bool poisonArrows = false;
    [SerializeField] private float poisonDamage = 2f;
    [SerializeField] private float poisonDuration = 3f;
    [SerializeField] private GameObject poisonArrowVFX;

    [SerializeField] private bool fireArrows = false;
    [SerializeField] private float fireDamage = 4f;
    [SerializeField] private float fireDuration = 3f;
    [SerializeField] private GameObject fireArrowVFX;
    
    // ==================== RUNTIME STATE ====================
    private bool isAttacking = false;
    private Vector3 enemyVelocity;          // Tracked enemy velocity for prediction
    private Vector3 predictedPosition;       // Where we expect enemy to be when arrow lands
    private Vector3 lastEnemyPosition;       // Previous frame position for velocity calc
    
    // Target locked at attack start (prevents mid-animation target switching)
    private EnemyBase lockedTarget;
    private Vector3 lockedVelocity;
    private Vector3 lockedPosition;
    private float baseAttackCooldownForArrow;  // Cached for animation speed scaling
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    /// <summary>
    /// Safety check - reset attack state if stuck.
    /// </summary>
    private void Update()
    {
        // Failsafe: if attacking for too long, reset state
        if (isAttacking && Time.time > lastTimeAttacked + attackCooldown + 1f)
        {
            isAttacking = false;
        
            if (characterAnimator != null)
            {
                characterAnimator.SetBool("Attack", false);
            }
        
            if (arrowVisual != null)
            {
                arrowVisual.SetActive(true);
            }
            
            ClearLockedTarget();
        }
    }
    
    protected override void Start()
    {
        base.Start();
        baseAttackCooldownForArrow = attackCooldown;
        UpdateArrowVisualVFX();
        UpdateAnimationSpeed();
    }
    
    protected override void FixedUpdate()
    {
        UpdateEnemyVelocity();
        UpdatePredictedPosition();
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
            case TowerUpgradeType.PoisonArrows:
                poisonArrows = enabled;
                UpdateArrowVisualVFX();
                break;
            case TowerUpgradeType.FireArrows:
                fireArrows = enabled;
                UpdateArrowVisualVFX();
                break;
        }
    }
    
    /// <summary>
    /// Tracks enemy velocity by comparing positions between frames.
    /// Uses locked target during attack, otherwise current target.
    /// </summary>
    private void UpdateEnemyVelocity()
    {
        EnemyBase targetToTrack = isAttacking && lockedTarget != null ? lockedTarget : currentEnemy;
        
        if (targetToTrack == null || !targetToTrack.gameObject.activeSelf)
        {
            enemyVelocity = Vector3.zero;
            lastEnemyPosition = Vector3.zero;
            return;
        }
        
        Vector3 currentPos = targetToTrack.transform.position;
        
        // Calculate velocity from position delta
        if (lastEnemyPosition != Vector3.zero)
        {
            enemyVelocity = (currentPos - lastEnemyPosition) / Time.fixedDeltaTime;
        }
        
        lastEnemyPosition = currentPos;
    }
    
    /// <summary>
    /// Calculates where enemy will be when arrow arrives.
    /// Prediction time scales with enemy speed and distance.
    /// </summary>
    private void UpdatePredictedPosition()
    {
        EnemyBase targetToPredict = isAttacking && lockedTarget != null ? lockedTarget : currentEnemy;
    
        if (targetToPredict == null || !targetToPredict.gameObject.activeSelf)
        {
            predictedPosition = Vector3.zero;
            return;
        }
    
        // Calculate prediction time based on enemy speed
        float enemySpeed = enemyVelocity.magnitude;
        float predictionTime = baseFlightTime + (enemySpeed * speedMultiplier);
    
        // Scale prediction by distance (closer = less prediction needed)
        float distance = Vector3.Distance(transform.position, targetToPredict.transform.position);
        float predictionScale = Mathf.Clamp01(distance / attackRange);
        predictionTime *= predictionScale;
    
        // Use path-aware prediction that handles waypoint turns
        predictedPosition = GetPathAwarePrediction(targetToPredict, predictionTime);
    }
    
    /// <summary>
    /// Calculates where the enemy will be after a given time, accounting for waypoint paths.
    /// Handles prediction across waypoint turns for accurate arrow targeting.
    /// </summary>
    /// <param name="target">Enemy to predict position for</param>
    /// <param name="predictionTime">How far into the future to predict in seconds</param>
    private Vector3 GetPathAwarePrediction(EnemyBase target, float predictionTime)
    {
        if (target == null) return Vector3.zero;

        Vector3 currentPos = target.transform.position;
        float speed = enemyVelocity.magnitude;

        // Not moving - return current position
        if (speed < 0.1f) return currentPos;

        float travelDistance = speed * predictionTime;
        float distanceToWaypoint = target.GetDistanceToNextWaypoint();

        // Simple case: won't reach next waypoint during prediction time
        if (distanceToWaypoint <= 0 || travelDistance < distanceToWaypoint)
        {
            return currentPos + (enemyVelocity * predictionTime);
        }

        // ===== COMPLEX CASE: Will turn at waypoint =====
        // Calculate position after the turn
        float timeToWaypoint = distanceToWaypoint / speed;
        Vector3 waypointPos = target.GetNextWaypointPosition();
        float remainingTime = predictionTime - timeToWaypoint;
        Vector3 directionAfterTurn = target.GetDirectionAfterNextWaypoint();

        return waypointPos + (directionAfterTurn * speed * remainingTime);
    }
    
    /// <summary>
    /// Rotates tower toward predicted position (or current enemy position).
    /// </summary>
    protected override void HandleRotation()
    {
        EnemyBase targetToFace = isAttacking && lockedTarget != null ? lockedTarget : currentEnemy;
        
        if (targetToFace == null || towerBody == null) return;
        
        // Use predicted position if available
        Vector3 targetPos = predictedPosition != Vector3.zero ? predictedPosition : targetToFace.transform.position;
        
        Vector3 direction = targetPos - towerBody.position;
        direction.y = 0;  // Keep rotation horizontal
        
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            towerBody.rotation = Quaternion.Slerp(towerBody.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Adjusts animation speed based on attack cooldown.
    /// Faster attacks = faster animation.
    /// </summary>
    private void UpdateAnimationSpeed()
    {
        if (characterAnimator == null) return;
        
        float animSpeed = baseAnimationLength / attackCooldown;
        characterAnimator.SetFloat("AttackSpeed", animSpeed);
    }
    
    /// <summary>
    /// Additional attack conditions: must have target and be facing it.
    /// </summary>
    protected override bool CanAttack()
    {
        if (currentEnemy == null) return false;
        if (!IsFacingEnemy(15f)) return false;  // Must be within 15 degrees
        return base.CanAttack() && !isAttacking;
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
    /// Initiates attack - locks target and triggers animation.
    /// </summary>
    protected override void Attack()
    {
        lastTimeAttacked = Time.time;
        isAttacking = true;
        
        // Lock target for duration of attack animation
        if (currentEnemy != null)
        {
            lockedTarget = currentEnemy;
            lockedVelocity = enemyVelocity;
            lockedPosition = currentEnemy.transform.position;
        }
    
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("Attack");
        }
    }
    
    /// <summary>
    /// Called by animation event when the archer begins drawing the bow.
    /// Activates the arrow visual on the bow.
    /// </summary>
    public void OnDrawBow()
    {
        if (bowController != null)
        {
            bowController.DrawBow();
        }

        if (arrowVisual != null)
        {
            arrowVisual.SetActive(true);
        }
    }

    /// <summary>
    /// Called by animation event when the archer releases the arrow.
    /// Fires the arrow projectile and hides the visual arrow from the bow.
    /// </summary>
    public void OnReleaseBow()
    {
        float cooldownRatio = attackCooldown / baseAttackCooldown;

        if (bowController != null)
        {
            bowController.ReleaseBow(cooldownRatio);
        }

        // Reacquire target if locked target died during draw
        if (lockedTarget == null || !lockedTarget.gameObject.activeSelf)
        {
            if (currentEnemy != null && currentEnemy.gameObject.activeSelf)
            {
                lockedTarget = currentEnemy;
            }
        }

        // Fire if we have a valid target
        if (lockedTarget != null && lockedTarget.gameObject.activeSelf)
        {
            PlayAttackSound();
            FireArrow();
        }

        // Hide arrow on bow
        if (arrowVisual != null)
        {
            arrowVisual.SetActive(false);
        }

        isAttacking = false;
        ClearLockedTarget();

        // Schedule arrow visual to reappear
        float dynamicRespawnDelay = 0.9f * cooldownRatio;
        Invoke("OnArrowReady", dynamicRespawnDelay);
    }

    /// <summary>
    /// Called after a delay to show the arrow is ready on the bow again.
    /// Reactivates the arrow visual.
    /// </summary>
    public void OnArrowReady()
    {
        if (arrowVisual != null)
        {
            arrowVisual.SetActive(true);
        }
    }
    
    /// <summary>
    /// Updates VFX on visual arrow based on current upgrades.
    /// Fire takes priority over poison.
    /// </summary>
    private void UpdateArrowVisualVFX()
    {
        // Clear existing VFX
        if (activeArrowVisualVFX != null)
        {
            ObjectPooling.instance.Return(activeArrowVisualVFX);
            activeArrowVisualVFX = null;
        }
    
        Transform spawnPoint = arrowVisualVFXPoint != null ? arrowVisualVFXPoint : arrowVisual.transform;
    
        // Fire takes priority over poison
        if (fireArrows && fireArrowVFX != null)
        {
            activeArrowVisualVFX = ObjectPooling.instance.GetVFXWithParent(fireArrowVFX, spawnPoint, -1f);
            activeArrowVisualVFX.transform.localPosition = Vector3.zero;
        }
        else if (poisonArrows && poisonArrowVFX != null)
        {
            activeArrowVisualVFX = ObjectPooling.instance.GetVFXWithParent(poisonArrowVFX, spawnPoint, -1f);
            activeArrowVisualVFX.transform.localPosition = Vector3.zero;
        }
    }
    
    /// <summary>
    /// Spawns arrow projectile aimed at predicted position.
    /// </summary>
    private void FireArrow()
    {
        if (lockedTarget == null || !lockedTarget.gameObject.activeSelf)
        {
            ClearLockedTarget();
            return;
        }

        // Calculate final predicted position
        Vector3 targetPos = lockedTarget.transform.position;
        float enemySpeed = enemyVelocity.magnitude;
        float predictionTime = baseFlightTime + (enemySpeed * speedMultiplier);
        Vector3 finalPredictedPosition = targetPos + (enemyVelocity * predictionTime);

        // Spawn arrow from visual arrow position
        Vector3 spawnPos = arrowVisual.transform.position;
        Quaternion spawnRot = arrowVisual.transform.rotation;
        float distance = Vector3.Distance(spawnPos, finalPredictedPosition);

        // Get arrow from pool
        GameObject newArrow = ObjectPooling.instance.Get(projectilePrefab);
        newArrow.transform.position = spawnPos;
        newArrow.transform.rotation = spawnRot;
        newArrow.SetActive(true);

        // Configure arrow projectile
        ArrowProjectile arrow = newArrow.GetComponent<ArrowProjectile>();
        IDamageable damageable = lockedTarget.GetComponent<IDamageable>();

        if (damageable != null)
        {
            arrow.SetupArcProjectile(finalPredictedPosition, damageable, CreateDamageInfo(), projectileSpeed, distance);

            // Apply DoT effect if upgraded (fire takes priority)
            if (fireArrows)
            {
                arrow.SetFireEffect(fireDamage, fireDuration, elementType, fireArrowVFX);
            }
            else if (poisonArrows)
            {
                arrow.SetPoisonEffect(poisonDamage, poisonDuration, elementType, poisonArrowVFX);
            }
        }
    }
    
    /// <summary>
    /// Clears locked target data after attack completes.
    /// </summary>
    private void ClearLockedTarget()
    {
        lockedTarget = null;
        lockedVelocity = Vector3.zero;
        lockedPosition = Vector3.zero;
    }
}