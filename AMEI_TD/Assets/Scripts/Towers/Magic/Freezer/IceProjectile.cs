using UnityEngine;

/// <summary>
/// Homing projectile that applies slow, DoT, and optional freeze effects in an area.
/// Used by TowerIceMage. Retargets if original target dies.
/// </summary>
public class IceProjectile : TowerProjectileBase
{
    // ==================== HOMING SETTINGS ====================
    [Header("Homing Settings")]
    [SerializeField] private float rotationSpeed = 200f;   // Degrees per second for turning
    [SerializeField] private float retargetRange = 5f;     // Search radius for new targets
    
    // ==================== TRACKING STATE ====================
    private Transform target;
    private bool isHoming = true;              // Currently tracking a target
    private LayerMask enemyLayer;
    private Quaternion rotationOffset;         // Visual rotation offset for projectile model
    private Vector3 lastKnownTargetPos;        // Continue to this pos if target dies
    private bool targetLost = false;           // Target died, flying to last known pos
    
    // ==================== ICE EFFECTS (from tower) ====================
    private float slowPercent;
    private float slowDuration;
    private float effectRadius;                // AOE radius for slow/DoT
    private DamageInfo dotDamageInfo;
    private float dotDuration;
    private float dotTickInterval;
    
    // ==================== FREEZE UPGRADE ====================
    private bool canFreeze = false;
    private float freezeChance;
    private float freezeDuration;
    
    /// <summary>
    /// Reset state when retrieved from pool.
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        target = null;
        isHoming = true;
        targetLost = false;
        canFreeze = false;
        
        // Reset physics
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    /// <summary>
    /// Configures the ice projectile with all necessary parameters.
    /// Called by TowerIceMage when spawning.
    /// </summary>
    public void SetupIceProjectile(
        Transform enemyTarget, 
        IDamageable newDamageable, 
        DamageInfo newDamageInfo, 
        float newSpeed, 
        LayerMask whatIsEnemy,
        float newSlowPercent,
        float newSlowDuration,
        float newEffectRadius,
        float newDotDamage,
        float newDotDuration,
        float newDotTickInterval,
        Vector3 visualRotationOffset)
    {
        target = enemyTarget;
        damageable = newDamageable;
        damageInfo = newDamageInfo;
        speed = newSpeed;
        spawnTime = Time.time;
        isHoming = true;
        hasHit = false;
        enemyLayer = whatIsEnemy;
        targetLost = false;
        
        // Store ice effect parameters
        slowPercent = newSlowPercent;
        slowDuration = newSlowDuration;
        effectRadius = newEffectRadius;
        dotDamageInfo = new DamageInfo(newDotDamage, newDamageInfo.elementType, true);  // isDoT = true
        dotDuration = newDotDuration;
        dotTickInterval = newDotTickInterval;
        rotationOffset = Quaternion.Euler(visualRotationOffset);
        
        // Cache initial target position for fallback
        if (target != null)
        {
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            lastKnownTargetPos = enemy != null ? enemy.GetCenterPoint() : target.position;
        }
    }
    
    /// <summary>
    /// Enables freeze effect (Freeze Solid upgrade).
    /// </summary>
    public void SetFreezeEffect(float chance, float duration)
    {
        canFreeze = true;
        freezeChance = chance;
        freezeDuration = duration;
    }
    
    /// <summary>
    /// Handles homing movement, retargeting, and destination arrival.
    /// </summary>
    protected override void MoveProjectile()
    {
        // ===== CHECK TARGET VALIDITY =====
        if (target == null || !target.gameObject.activeSelf)
        {
            // Try to find new target within range
            Transform newTarget = FindNewTarget();
            
            if (newTarget != null)
            {
                target = newTarget;
                damageable = target.GetComponent<IDamageable>();
                targetLost = false;
            }
            else
            {
                // No target available - fly to last known position
                isHoming = false;
                targetLost = true;
            }
        }
        
        // ===== UPDATE LAST KNOWN POSITION =====
        if (target != null && target.gameObject.activeSelf)
        {
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            lastKnownTargetPos = enemy != null ? enemy.GetCenterPoint() : target.position;
        }
        
        // ===== DETERMINE TARGET POSITION =====
        Vector3 targetPos;
        
        if (isHoming && target != null)
        {
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            targetPos = enemy != null ? enemy.GetCenterPoint() : target.position;
        }
        else
        {
            targetPos = lastKnownTargetPos;
        }
        
        // ===== ROTATE AND MOVE =====
        Vector3 direction = (targetPos - transform.position).normalized;
        
        // Apply visual rotation offset to model
        Quaternion flightRotation = Quaternion.LookRotation(direction);
        Quaternion targetRotation = flightRotation * rotationOffset;
        
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        transform.position += direction * (speed * Time.deltaTime);
        
        // ===== CHECK DESTINATION ARRIVAL (when target lost) =====
        if (targetLost)
        {
            float distToTarget = Vector3.Distance(transform.position, lastKnownTargetPos);
            if (distToTarget < 0.5f)
            {
                // Reached destination - apply AOE effects even without hitting enemy
                ApplyEffectsInRadius();

                DestroyProjectile();
            }
        }
    }
    
    /// <summary>
    /// Searches for closest enemy within retarget range.
    /// </summary>
    private Transform FindNewTarget()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, retargetRange, enemyLayer);
        
        Transform closest = null;
        float closestDist = float.MaxValue;
        
        foreach (Collider col in enemies)
        {
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = col.transform;
                }
            }
        }
        
        return closest;
    }
    
    /// <summary>
    /// Handles collision with enemy. Applies freeze to direct hit, AOE effects to all nearby.
    /// </summary>
    protected override void OnHit(Collider other)
    {
        if (hasHit) return;
    
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy == null) return;
    
        // Freeze only affects the directly hit enemy (not AOE)
        if (canFreeze && Random.value <= freezeChance)
        {
            enemy.ApplyStun(freezeDuration);
        }
    
        // Slow and DoT affect all enemies in radius
        ApplyEffectsInRadius();
    
        isHoming = false;
    
        base.OnHit(other);
    }

    /// <summary>
    /// Applies slow and DoT to all enemies within effect radius.
    /// </summary>
    private void ApplyEffectsInRadius()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, effectRadius, enemyLayer);

        foreach (Collider col in enemies)
        {
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.ApplySlow(slowPercent, slowDuration, true);  // true = stacking slow
                enemy.ApplyDoT(dotDamageInfo, dotDuration, dotTickInterval);
            }
        }
    }
}