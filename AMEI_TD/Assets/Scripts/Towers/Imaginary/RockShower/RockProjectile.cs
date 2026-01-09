using UnityEngine;

/// <summary>
/// Projectile that falls from above and deals AOE damage on impact.
/// Used by TowerRockShower for both regular rocks and meteors.
/// Inherits movement from TowerProjectileBase but uses downward gravity-like motion.
/// </summary>
public class RockProjectile : TowerProjectileBase
{
    [SerializeField] private float damageRadius = 1.5f;   // AOE radius for damage on impact
    [SerializeField] private LayerMask groundLayer;        // Layer to detect ground for impact
    
    private float rockSize = 1f;           // Size multiplier (affects damage radius and visuals)
    private LayerMask whatIsEnemy;         // Enemy layer for AOE damage
    
    private Vector3 originalScale;         // Cached prefab scale for pool reset
    private float baseDamageRadius;        // Cached base radius before size scaling
    
    [Header("Audio")]
    [SerializeField] [Range(0f, 1f)] private float impactSoundChance = 0.3f;  // Chance to play sound (reduces audio spam)
    
    private bool isMeteor = false;         // Meteors always play impact sound

    private void Awake()
    {
        originalScale = transform.localScale;
        baseDamageRadius = damageRadius;
    }

    /// <summary>
    /// Reset state when retrieved from pool.
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        transform.localScale = originalScale;
        damageRadius = baseDamageRadius;
        rockSize = 1f;
        isMeteor = false;
        speed = 0f;  // Reset speed on enable
        
        // Reset physics state
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    /// <summary>
    /// Configures the rock projectile with damage, speed, and size.
    /// Called by TowerRockShower when spawning rocks.
    /// </summary>
    /// <param name="newDamageInfo">Damage to deal on impact</param>
    /// <param name="enemyLayer">Layer mask for enemy detection</param>
    /// <param name="fallSpeed">Downward velocity</param>
    /// <param name="size">Size multiplier (affects damage radius)</param>
    public void Setup(DamageInfo newDamageInfo, LayerMask enemyLayer, float fallSpeed, float size = 1f)
    {
        damageInfo = newDamageInfo;
        whatIsEnemy = enemyLayer;
        speed = fallSpeed;
        direction = Vector3.down;
        spawnTime = Time.time;
        rockSize = size;
        damageRadius = baseDamageRadius * size;  // Bigger rocks = bigger AOE
        hasHit = false;
        isActive = true;
        isMeteor = false;
    }
    
    /// <summary>
    /// Marks this rock as a meteor (always plays impact sound).
    /// </summary>
    public void SetAsMeteor()
    {
        isMeteor = true;
    }
    
    /// <summary>
    /// Handles collision with ground layer specifically.
    /// </summary>
    protected override void OnHit(Collider other)
    {
        if (hasHit) return;

        // Check if we hit ground
        if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            hasHit = true;
            DealAOEDamage();
    
            // Spawn impact VFX scaled to rock size
            if (impactEffectPrefab != null)
            {
                Vector3 scaledSize = impactEffectPrefab.transform.localScale * rockSize;
                ObjectPooling.instance.GetVFX(impactEffectPrefab, transform.position, Quaternion.identity, scaledSize, 2f);
            }

            // Meteors always play sound, regular rocks have random chance
            if (isMeteor || Random.value <= impactSoundChance)
            {
                PlayImpactSound();
            }

            ObjectPooling.instance.Return(gameObject);
        }
    }
    
    /// <summary>
    /// Deals damage to all enemies within damageRadius using OverlapSphere.
    /// </summary>
    private void DealAOEDamage()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, damageRadius, whatIsEnemy);
    
        foreach (Collider enemy in enemies)
        {
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damageInfo);
            }
        }
    }
    
    /// <summary>
    /// Trigger-based collision detection for enemies and ground.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        bool hitEnemy = ((1 << other.gameObject.layer) & whatIsEnemy) != 0;
        bool hitGround = ((1 << other.gameObject.layer) & groundLayer) != 0;

        if (hitEnemy || hitGround)
        {
            hasHit = true;
            DealAOEDamage();

            if (impactEffectPrefab != null)
            {
                Vector3 scaledSize = impactEffectPrefab.transform.localScale * rockSize;
                ObjectPooling.instance.GetVFX(impactEffectPrefab, transform.position, Quaternion.identity, scaledSize, 2f);
            }

            if (isMeteor || Random.value <= impactSoundChance)
            {
                PlayImpactSound();
            }

            ObjectPooling.instance.Return(gameObject);
        }
    }
    
    /// <summary>
    /// Moves rock downward and uses raycast to detect ground impact.
    /// Raycast provides more reliable ground detection than triggers for fast-moving objects.
    /// </summary>
    protected override void MoveProjectile()
    {
        float moveDistance = speed * Time.deltaTime;
    
        // Debug visualization
        Debug.DrawRay(transform.position, Vector3.down * (moveDistance + 0.5f), Color.red, 0.1f);
    
        // Raycast ahead to catch ground collision (prevents tunneling at high speeds)
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, moveDistance + 0.5f, groundLayer))
        {
            Debug.Log($"[ROCK] Hit ground: {hit.collider.name}");
            transform.position = hit.point;
        
            if (!hasHit)
            {
                hasHit = true;
                DealAOEDamage();

                if (impactEffectPrefab != null)
                {
                    Vector3 scaledSize = impactEffectPrefab.transform.localScale * rockSize;
                    ObjectPooling.instance.GetVFX(impactEffectPrefab, transform.position, Quaternion.identity, scaledSize, 2f);
                }

                if (isMeteor || Random.value <= impactSoundChance)
                {
                    PlayImpactSound();
                }

                ObjectPooling.instance.Return(gameObject);
            }
            return;
        }
    
        // No ground hit - continue falling
        transform.position += direction * moveDistance;
    }
    
    /// <summary>
    /// Editor visualization - shows AOE damage radius.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}