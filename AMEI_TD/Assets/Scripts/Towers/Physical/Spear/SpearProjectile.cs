using UnityEngine;

/// <summary>
/// Straight-flying spear projectile with bleed DoT and optional explosion.
/// Uses rigidbody velocity for physics-based movement.
/// 
/// Unlike ArrowProjectile, spears fly in a straight line without arc/gravity.
/// Model uses Vector3.up as forward direction (rotates from that axis).
/// </summary>
public class SpearProjectile : TowerProjectileBase
{
    [Header("Spear Settings")]
    [SerializeField] private TrailRenderer trail;
    
    // ==================== BLEED UPGRADE ====================
    [Header("Spear Effects")]
    private bool applyBleed = false;
    private float bleedDamage;
    private float bleedDuration;
    private DamageInfo bleedDamageInfo;

    // ==================== EXPLOSIVE TIP UPGRADE ====================
    private bool isExplosive = false;
    private float explosionRadius;
    private float explosionDamage;
    private DamageInfo explosionDamageInfo;
    private LayerMask enemyLayer;
    private GameObject explosionVFX;

    [Header("VFX")]
    [SerializeField] private Transform vfxPoint;  // Attach point for bleed VFX on spear
    
    // ==================== FLIGHT STATE ====================
    private bool launched = false;
    private float spearSpeed;
    
    /// <summary>
    /// Reset state when retrieved from pool.
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        launched = false;
        applyBleed = false;
        isExplosive = false;
        explosionVFX = null;
    
        // Clean up any attached VFX from previous use
        if (vfxPoint != null)
        {
            foreach (Transform child in vfxPoint)
            {
                ObjectPooling.instance.Return(child.gameObject);
            }
        }
    
        // Reset physics
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    
        // Clear trail artifacts
        if (trail != null)
        {
            trail.Clear();
        }
    }
    
    /// <summary>
    /// Configures spear for straight-line flight toward target.
    /// Note: Spear model uses Vector3.up as forward, so FromToRotation aligns up-axis.
    /// </summary>
    public void SetupSpear(Vector3 targetPos, IDamageable newDamageable, DamageInfo newDamageInfo, float newSpeed)
    {
        damageInfo = newDamageInfo;
        damageable = newDamageable;
        spawnTime = Time.time;
        spearSpeed = newSpeed;
        
        // Calculate fire direction
        Vector3 fireDirection = (targetPos - transform.position).normalized;
        
        // Rotate spear model to face fire direction (spear points along Y axis)
        transform.rotation = Quaternion.FromToRotation(Vector3.up, fireDirection);
        
        // Launch using physics velocity
        rb.useGravity = false;  // Spears fly straight, no gravity
        rb.velocity = fireDirection * spearSpeed;
        launched = true;
    }
    
    /// <summary>
    /// Handles spear flight and rotation to match velocity.
    /// </summary>
    protected override void Update()
    {
        if (!launched || !isActive) return;
        
        // Safety cleanup for spears that miss
        if (Time.time - spawnTime > maxLifeTime)
        {
            DestroyProjectile();
            return;
        }
        
        // Keep spear pointed in direction of travel
        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.FromToRotation(Vector3.up, rb.velocity.normalized);
        }
    }
    
    /// <summary>
    /// Enables bleed DoT effect on hit.
    /// Optionally attaches VFX to spear.
    /// </summary>
    public void SetBleedEffect(float damage, float duration, ElementType elementType, GameObject spearVFX = null)
    {
        applyBleed = true;
        bleedDamage = damage;
        bleedDuration = duration;
        bleedDamageInfo = new DamageInfo(damage, elementType, true);

        // Attach bleed VFX to spear
        if (spearVFX != null)
        {
            Transform spawnPoint = vfxPoint != null ? vfxPoint : transform;
            GameObject vfx = ObjectPooling.instance.GetVFXWithParent(spearVFX, spawnPoint, -1f);
            vfx.transform.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// Enables explosion on impact.
    /// Explodes on both enemy hits and environment hits.
    /// </summary>
    public void SetExplosiveEffect(float radius, float damage, ElementType elementType, LayerMask whatIsEnemy, GameObject vfx = null)
    {
        isExplosive = true;
        explosionRadius = radius;
        explosionDamage = damage;
        explosionDamageInfo = new DamageInfo(damage, elementType);
        enemyLayer = whatIsEnemy;
        explosionVFX = vfx;
    }
    
    /// <summary>
    /// Disabled - spear uses rigidbody physics.
    /// </summary>
    protected override void MoveProjectile()
    {
        // Movement handled by rigidbody velocity
    }
    
    /// <summary>
    /// Handles collision with enemies and environment.
    /// Three cases:
    /// 1. Hit enemy: damage + bleed + optional explosion
    /// 2. Hit ground (explosive): trigger explosion
    /// 3. Hit ground (non-explosive): just destroy
    /// </summary>
    protected override void OnHit(Collider other)
    {
        if (hasHit) return;

        EnemyBase enemy = other.GetComponent<EnemyBase>();
        Vector3 impactPoint = other.ClosestPoint(transform.position);
        
        // ===== CASE 1: HIT ENEMY =====
        if (enemy != null)
        {
            hasHit = true;

            // Spawn impact VFX
            if (impactEffectPrefab != null)
            {
                ObjectPooling.instance.GetVFX(impactEffectPrefab, impactPoint, Quaternion.identity, 2f);
            }

            PlayImpactSound();

            // Deal direct damage
            if (damageable != null)
            {
                damageable.TakeDamage(damageInfo);
            }

            // Apply bleed DoT
            if (applyBleed)
            {
                enemy.ApplyDoT(bleedDamageInfo, bleedDuration, 0.5f, false, 0f, default, DebuffType.Bleed);
            }

            // Trigger explosion (centered on enemy, not impact point)
            if (isExplosive)
            {
                TriggerExplosion(enemy.GetCenterPoint(), impactPoint);
            }
            else
            {
                DestroyProjectile();
            }
        }
        // ===== CASE 2: HIT GROUND (EXPLOSIVE) =====
        else if (isExplosive)
        {
            hasHit = true;
            
            PlayImpactSound();
            TriggerExplosion(impactPoint, impactPoint);
        }
        // ===== CASE 3: HIT GROUND (NON-EXPLOSIVE) =====
        else
        {
            hasHit = true;
            DestroyProjectile();
        }
    }
    
    /// <summary>
    /// Triggers AOE explosion, dealing damage to all enemies in radius.
    /// </summary>
    /// <param name="explosionCenter">Center point for damage calculation</param>
    /// <param name="vfxPosition">Where to spawn explosion VFX</param>
    private void TriggerExplosion(Vector3 explosionCenter, Vector3 vfxPosition)
    {
        // Spawn explosion VFX
        if (explosionVFX != null)
        {
            ObjectPooling.instance.GetVFX(explosionVFX, vfxPosition, Quaternion.identity, 2f);
        }

        // Deal AOE damage
        Collider[] enemies = Physics.OverlapSphere(explosionCenter, explosionRadius, enemyLayer);
        foreach (Collider col in enemies)
        {
            IDamageable explosionTarget = col.GetComponent<IDamageable>();
            if (explosionTarget != null)
            {
                explosionTarget.TakeDamage(explosionDamageInfo);
            }
        }
        
        DestroyProjectile();
    }
    
    /// <summary>
    /// Cleanup before returning to pool.
    /// </summary>
    protected override void DestroyProjectile()
    {
        if (trail != null)
        {
            trail.Clear();
        }
    
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    
        base.DestroyProjectile();
    }
}