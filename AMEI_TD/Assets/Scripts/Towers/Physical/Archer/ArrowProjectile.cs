using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Physics-based arrow projectile with arc trajectory and DoT effects.
/// Uses rigidbody velocity with initial curve toward target then gravity falloff.
/// </summary>
public class ArrowProjectile : TowerProjectileBase
{
    // ==================== Arc Trajectory Settings ====================
    [Header("Arrow Settings")]
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private float curveDelay = 0.05f;           // Time before homing kicks in
    [SerializeField] private float curveStrength = 8f;           // How aggressively arrow curves toward target
    [SerializeField] private float gravityMultiplier = 3f;       // Gravity strength after curve phase
    [SerializeField] private float maxCurveDuration = 0.5f;      // How long the homing phase lasts
    [SerializeField] private float closeRangeGravityBoost = 3f;  // Extra gravity for close targets (steeper arc)
    [SerializeField] private float closeRangeThreshold = 6f;     // Distance below which close range gravity applies
    
    [Header("VFX")]
    [SerializeField] private Transform vfxPoint;  // Attach point for DoT effect visuals on the arrow
    
    // ==================== Damage Over Time Effects ====================
    [Header("DoT Effects")]
    private bool applyPoison = false;
    private float poisonDamage;
    private float poisonDuration;
    private DamageInfo poisonDamageInfo;

    private bool applyFire = false;
    private float fireDamage;
    private float fireDuration;
    private DamageInfo fireDamageInfo;
    
    // ==================== Flight State ====================
    private bool launched = false;
    private bool curving = true;          // True while arrow is homing toward target
    private float launchTime;
    private Vector3 targetPosition;
    private Vector3 initialForward;
    private float arrowSpeed;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        
        // Reset flight state
        launched = false;
        curving = true;
        applyPoison = false;
        applyFire = false;

        // Clean up any attached VFX from previous use
        if (vfxPoint != null)
        {
            foreach (Transform child in vfxPoint)
            {
                ObjectPooling.instance.Return(child.gameObject);
            }
        }

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (trail != null)
        {
            trail.Clear();
        }
    }
    
    /// <summary>
    /// Initializes arrow with arc trajectory parameters.
    /// Speed and curve duration scale with distance for consistent flight feel.
    /// </summary>
    /// <param name="targetPos">Predicted impact position</param>
    /// <param name="newDamageable">Target to damage on hit</param>
    /// <param name="newDamageInfo">Damage amount and element type</param>
    /// <param name="newSpeed">Base arrow speed</param>
    /// <param name="distance">Distance to target (affects arc parameters)</param>
    public void SetupArcProjectile(Vector3 targetPos, IDamageable newDamageable, DamageInfo newDamageInfo, float newSpeed, float distance)
    {
        damageInfo = newDamageInfo;
        damageable = newDamageable;
        spawnTime = Time.time;
        launchTime = Time.time;
        targetPosition = targetPos;
        initialForward = transform.forward;

        // Scale speed and curve duration based on distance
        arrowSpeed = newSpeed + (distance * 0.5f);
        maxCurveDuration = 0.3f + (distance * 0.05f);

        // Close range shots need steeper arcs to not overshoot
        if (distance < closeRangeThreshold)
        {
            gravityMultiplier = 3f + closeRangeGravityBoost;
        }
        else
        {
            gravityMultiplier = 1.5f;
        }

        // Launch using physics velocity
        rb.useGravity = false;  // We apply custom gravity
        rb.velocity = initialForward * arrowSpeed;
        launched = true;
        curving = true;

        if (trail != null)
        {
            trail.Clear();
        }
    }
    
    protected override void Update()
    {
        if (!launched || !isActive) return;
        
        // Safety cleanup for arrows that miss
        if (Time.time - spawnTime > maxLifeTime)
        {
            DestroyProjectile();
            return;
        }
        
        float timeSinceLaunch = Time.time - launchTime;
        
        // Homing phase: arrow curves toward predicted target position
        if (timeSinceLaunch > curveDelay && curving)
        {
            if (timeSinceLaunch > curveDelay + maxCurveDuration)
            {
                // Stop homing, let gravity take over
                curving = false;
            }
            else
            {
                // Lerp velocity toward target direction
                Vector3 toTarget = (targetPosition - transform.position).normalized;
                Vector3 desiredVelocity = toTarget * arrowSpeed;
                rb.velocity = Vector3.Lerp(rb.velocity, desiredVelocity, curveStrength * Time.deltaTime);
            }
        }
        
        // Apply gravity after initial curve delay
        if (timeSinceLaunch > curveDelay)
        {
            rb.velocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
        }
        
        // Rotate arrow to face movement direction
        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.velocity);
        }
    }
    
    /// <summary>
    /// Configures arrow to apply poison DoT on hit.
    /// Optionally attaches a visual effect to the arrow.
    /// </summary>
    public void SetPoisonEffect(float damage, float duration, ElementType elementType, GameObject arrowVFX = null)
    {
        applyPoison = true;
        poisonDamage = damage;
        poisonDuration = duration;
        poisonDamageInfo = new DamageInfo(damage, elementType, true);
    
        if (arrowVFX != null)
        {
            Transform spawnPoint = vfxPoint != null ? vfxPoint : transform;
            GameObject vfx = ObjectPooling.instance.GetVFXWithParent(arrowVFX, spawnPoint, -1f);
            vfx.transform.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// Configures arrow to apply fire DoT on hit.
    /// Optionally attaches a visual effect to the arrow.
    /// </summary>
    public void SetFireEffect(float damage, float duration, ElementType elementType, GameObject arrowVFX = null)
    {
        applyFire = true;
        fireDamage = damage;
        fireDuration = duration;
        fireDamageInfo = new DamageInfo(damage, elementType, true);

        if (arrowVFX != null)
        {
            Transform spawnPoint = vfxPoint != null ? vfxPoint : transform;
            GameObject vfx = ObjectPooling.instance.GetVFXWithParent(arrowVFX, spawnPoint, -1f);
            vfx.transform.localPosition = Vector3.zero;
        }
    }
    
    protected override void OnHit(Collider other)
    {
        if (hasHit) return;
        hasHit = true;

        Vector3 impactPoint = other.ClosestPoint(transform.position);

        if (impactEffectPrefab != null)
        {
            ObjectPooling.instance.GetVFX(impactEffectPrefab, impactPoint, Quaternion.identity, 2f);
        }

        PlayImpactSound();

        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            damageable?.TakeDamage(damageInfo);
        
            // Fire takes priority over poison (can't have both)
            if (applyFire)
            {
                enemy.ApplyDoT(fireDamageInfo, fireDuration, 0.5f, false, 0f, default, DebuffType.Burn);
            }
            else if (applyPoison)
            {
                enemy.ApplyDoT(poisonDamageInfo, poisonDuration, 0.5f, false, 0f, default, DebuffType.Poison);
            }
        }
    }
    
    /// <summary>
    /// Disabled - arrow uses rigidbody physics instead of transform movement.
    /// </summary>
    protected override void MoveProjectile()
    {
        // Movement handled by rigidbody velocity in Update()
    }
    
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