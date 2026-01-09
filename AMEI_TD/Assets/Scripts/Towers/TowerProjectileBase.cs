using UnityEngine;

/// <summary>
/// Base class for tower projectiles. Handles movement, collision, damage dealing, and pooling.
/// </summary>
public class TowerProjectileBase : MonoBehaviour
{
    // ==================== Movement & Damage ====================
    protected Vector3 direction;
    protected DamageInfo damageInfo;
    protected float speed;
    protected IDamageable damageable;
    protected Rigidbody rb;

    // ==================== State Tracking ====================
    protected bool isActive = true;
    protected bool hasHit = false;
    
    // Projectiles auto-destroy after this time to prevent orphaned objects
    [SerializeField] protected float maxLifeTime = 10f;
    protected float spawnTime;

    [Header("VFX")]
    [SerializeField] protected GameObject impactEffectPrefab;

    [Header("Audio")]
    [SerializeField] protected AudioClip impactSound;
    [SerializeField] [Range(0f, 1f)] protected float impactSoundVolume = 1f;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Initializes the projectile with target direction, damage information, and speed.
    /// Called when the projectile is spawned from the object pool.
    /// </summary>
    /// <param name="targetPosition">World position to aim towards</param>
    /// <param name="newDamageable">The target that will receive damage on hit</param>
    /// <param name="newDamageInfo">Damage amount and element type</param>
    /// <param name="newSpeed">Movement speed of the projectile</param>
    public void SetupProjectile(Vector3 targetPosition, IDamageable newDamageable, DamageInfo newDamageInfo, float newSpeed)
    {
        direction = (targetPosition - transform.position).normalized;
        damageInfo = newDamageInfo;
        speed = newSpeed;
        spawnTime = Time.time;
        damageable = newDamageable;
    }

    /// <summary>
    /// Initializes the projectile with physical damage (convenience overload).
    /// </summary>
    /// <param name="targetPosition">World position to aim towards</param>
    /// <param name="newDamageable">The target that will receive damage on hit</param>
    /// <param name="newDamage">Damage amount (physical type)</param>
    /// <param name="newSpeed">Movement speed of the projectile</param>
    public void SetupProjectile(Vector3 targetPosition, IDamageable newDamageable, float newDamage, float newSpeed)
    {
        SetupProjectile(targetPosition, newDamageable, new DamageInfo(newDamage, ElementType.Physical), newSpeed);
    }

    protected virtual void Update()
    {
        if (!isActive) return;

        // Safety cleanup for projectiles that miss or get stuck
        if (Time.time - spawnTime > maxLifeTime)
        {
            DestroyProjectile();
            return;
        }

        MoveProjectile();
    }

    protected virtual void MoveProjectile()
    {
        transform.position += direction * (speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        OnHit(other);
        
        // Pass through towers without being destroyed (allows projectiles to not collide with friendly towers)
        LayerMask TowerLayer = LayerMask.NameToLayer("Tower");
        if (other.gameObject.layer != TowerLayer)
            DestroyProjectile();
    }

    protected virtual void OnHit(Collider other)
    {
        // Prevent multiple hits from same projectile
        if (hasHit) return;
        hasHit = true;

        // Get precise impact location for VFX placement
        Vector3 impactPoint = other.ClosestPoint(transform.position);

        if (impactEffectPrefab != null)
        {
            ObjectPooling.instance.GetVFX(impactEffectPrefab, impactPoint, Quaternion.identity, 2f);
        }

        PlayImpactSound();

        // Only deal damage to enemies, not other colliders like terrain
        if (other.GetComponent<EnemyBase>())
        {
            damageable?.TakeDamage(damageInfo);
        }
    }

    protected virtual void PlayImpactSound()
    {
        if (impactSound != null && SFXPlayer.instance != null)
        {
            SFXPlayer.instance.Play(impactSound, transform.position, impactSoundVolume);
        }
    }

    /// <summary>
    /// Returns projectile to the object pool. Shows impact VFX if projectile
    /// expired without hitting anything (e.g., missed target).
    /// </summary>
    protected virtual void DestroyProjectile()
    {
        // Show impact effect even on timeout/miss for visual feedback
        if (!hasHit && impactEffectPrefab != null)
        {
            ObjectPooling.instance.GetVFX(impactEffectPrefab, transform.position, Quaternion.identity, 2f);
            PlayImpactSound();
        }

        // Reset state and return to pool
        isActive = false;
        hasHit = false;
        ObjectPooling.instance.Return(gameObject);
    }
    
    /// <summary>
    /// Resets projectile state when retrieved from the object pool.
    /// </summary>
    protected virtual void OnEnable()
    {
        isActive = true;
        hasHit = false;
        spawnTime = Time.time;
        
        // Clear any residual physics from previous use
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}