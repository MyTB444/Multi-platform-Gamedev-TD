using UnityEngine;

/// <summary>
/// Homing projectile used by TowerPyromancer. Tracks enemies and supports burn DoT and AOE damage.
/// Retargets if original target dies. Continues to last known position if no targets available.
/// </summary>
public class HomingProjectile : TowerProjectileBase
{
    // ==================== HOMING SETTINGS ====================
    [Header("Homing Settings")]
    [SerializeField] private float rotationSpeed = 200f;   // Degrees per second for turning
    [SerializeField] private float retargetRange = 5f;     // Search radius for new targets
    
    // ==================== BURN EFFECTS (set by tower) ====================
    [Header("Burn Effects")]
    private bool canBurn = false;
    private float burnChancePercent;
    private float burnDamage;
    private float burnDuration;
    private DamageInfo burnDamageInfo;
    private bool canSpreadBurn = false;        // Burn Spread upgrade
    private float spreadRadius;
    private LayerMask spreadEnemyLayer;
    
    // ==================== AOE EFFECTS (set by tower) ====================
    [Header("AoE Effects")]
    private bool hasAoE = false;               // Bigger Fireball upgrade
    private float aoERadius;
    private float aoEDamagePercent;            // Percentage of main damage for AOE
    private LayerMask aoEEnemyLayer;
    
    // ==================== TRACKING STATE ====================
    private Transform target;
    private bool isHoming = true;
    private Vector3 lastKnownTargetPos;        // Fallback position if target dies
    private bool targetLost = false;
    private LayerMask enemyLayer;
    
    private Vector3 originalScale;             // For pool reset (bigger fireball changes scale)

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    /// <summary>
    /// Reset state when retrieved from pool.
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        transform.localScale = originalScale;  // Reset scale (bigger fireball modifies it)
        target = null;
        isHoming = true;
        targetLost = false;
        canBurn = false;
        hasAoE = false;
        canSpreadBurn = false;
        
        // Reset physics
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Configures basic homing projectile parameters.
    /// Called by TowerPyromancer when spawning.
    /// </summary>
    public void SetupHomingProjectile(Transform enemyTarget, IDamageable newDamageable, DamageInfo newDamageInfo, float newSpeed, LayerMask whatIsEnemy)
    {
        target = enemyTarget;
        damageable = newDamageable;
        damageInfo = newDamageInfo;
        speed = newSpeed;
        spawnTime = Time.time;
        isHoming = true;
        hasHit = false;
        targetLost = false;
        enemyLayer = whatIsEnemy;
    
        // Cache initial target position for fallback
        if (target != null)
        {
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            lastKnownTargetPos = enemy != null ? enemy.GetCenterPoint() : target.position;
        }
    }
    
    /// <summary>
    /// Enables burn DoT effect (Burn Chance upgrade).
    /// Optionally enables burn spread to nearby enemies.
    /// </summary>
    public void SetBurnEffect(float chance, float damage, float duration, ElementType elementType, bool spread = false, float spreadRadius = 0f, LayerMask enemyLayer = default)
    {
        canBurn = true;
        burnChancePercent = chance;
        burnDamage = damage;
        burnDuration = duration;
        burnDamageInfo = new DamageInfo(damage, elementType, true);  // isDoT = true
        canSpreadBurn = spread;
        this.spreadRadius = spreadRadius;
        spreadEnemyLayer = enemyLayer;
    }
    
    /// <summary>
    /// Enables AOE splash damage (Bigger Fireball upgrade).
    /// </summary>
    public void SetAoEEffect(float radius, float damagePercent, LayerMask enemyLayer)
    {
        hasAoE = true;
        aoERadius = radius;
        aoEDamagePercent = damagePercent;
        aoEEnemyLayer = enemyLayer;
    }

    /// <summary>
    /// Handles homing movement, retargeting, and destination arrival.
    /// </summary>
    protected override void MoveProjectile()
    {
        // ===== CHECK TARGET VALIDITY =====
        if (target == null || !target.gameObject.activeSelf)
        {
            Transform newTarget = FindNewTarget();
            
            if (newTarget != null)
            {
                target = newTarget;
                damageable = target.GetComponent<IDamageable>();
                targetLost = false;
            }
            else
            {
                // No target - fly to last known position
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
        
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        // Move along forward direction (smoother curves than moving toward target directly)
        transform.position += transform.forward * (speed * Time.deltaTime);
        
        // ===== CHECK DESTINATION ARRIVAL (when target lost) =====
        if (targetLost)
        {
            float distToTarget = Vector3.Distance(transform.position, lastKnownTargetPos);
            if (distToTarget < 0.5f)
            {
                // Reached destination without hitting anything
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
    /// Handles collision with enemy. Applies burn and AOE effects.
    /// </summary>
    protected override void OnHit(Collider other)
    {
        if (hasHit) return;
    
        hasHit = true;
        
        // Get impact point for VFX positioning
        Vector3 impactPoint = other.ClosestPoint(transform.position);
    
        // Spawn impact VFX
        if (impactEffectPrefab != null)
        {
            ObjectPooling.instance.GetVFX(impactEffectPrefab, impactPoint, Quaternion.identity, 2f);
        }
        
        PlayImpactSound();
    
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            // ===== DEAL IMPACT DAMAGE =====
            damageable?.TakeDamage(damageInfo);
        
            // ===== APPLY BURN (if upgraded and roll succeeds) =====
            if (canBurn && Random.value <= burnChancePercent)
            {
                // Burn can optionally spread to nearby enemies
                enemy.ApplyDoT(burnDamageInfo, burnDuration, 0.5f, canSpreadBurn, spreadRadius, spreadEnemyLayer, DebuffType.Burn);
            }
        
            // ===== APPLY AOE DAMAGE (if upgraded) =====
            if (hasAoE)
            {
                // AOE deals percentage of main damage
                DamageInfo aoEDamage = new DamageInfo(damageInfo.amount * aoEDamagePercent, damageInfo.elementType);
                Collider[] enemies = Physics.OverlapSphere(transform.position, aoERadius, aoEEnemyLayer);
            
                foreach (Collider col in enemies)
                {
                    // Skip the directly hit enemy (already took full damage)
                    if (col.gameObject == other.gameObject) continue;
                
                    IDamageable target = col.GetComponent<IDamageable>();
                    if (target != null)
                    {
                        target.TakeDamage(aoEDamage);
                    }
                }
            }
        }
    
        isHoming = false;
        DestroyProjectile();
    }
}