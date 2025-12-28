using UnityEngine;

public class SpearProjectile : TowerProjectileBase
{
    [Header("Spear Settings")]
    [SerializeField] private TrailRenderer trail;
    
    [Header("Spear Effects")]
    private bool applyBleed = false;
    private float bleedDamage;
    private float bleedDuration;
    private DamageInfo bleedDamageInfo;

    private bool isExplosive = false;
    private float explosionRadius;
    private float explosionDamage;
    private DamageInfo explosionDamageInfo;
    private LayerMask enemyLayer;
    private GameObject explosionVFX;

    [Header("VFX")]
    [SerializeField] private Transform vfxPoint;
    
    private Rigidbody rb;
    private bool launched = false;
    private float spearSpeed;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    public void SetupSpear(Vector3 targetPos, IDamageable newDamageable, DamageInfo newDamageInfo, float newSpeed)
    {
        damageInfo = newDamageInfo;
        damageable = newDamageable;
        spawnTime = Time.time;
        spearSpeed = newSpeed;
        
        Vector3 fireDirection = (targetPos - transform.position).normalized;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, fireDirection);
        
        rb.useGravity = false;
        rb.velocity = fireDirection * spearSpeed;
        launched = true;
    }
    
    protected override void Update()
    {
        if (!launched || !isActive) return;
        
        if (Time.time - spawnTime > maxLifeTime)
        {
            DestroyProjectile();
            return;
        }
        
        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.FromToRotation(Vector3.up, rb.velocity.normalized);
        }
    }
    
    public void SetBleedEffect(float damage, float duration, ElementType elementType, GameObject spearVFX = null)
    {
        applyBleed = true;
        bleedDamage = damage;
        bleedDuration = duration;
        bleedDamageInfo = new DamageInfo(damage, elementType, true);

        if (spearVFX != null)
        {
            Transform spawnPoint = vfxPoint != null ? vfxPoint : transform;
            GameObject vfx = Instantiate(spearVFX, spawnPoint);
            vfx.transform.localPosition = Vector3.zero;
        }
    }

    public void SetExplosiveEffect(float radius, float damage, ElementType elementType, LayerMask whatIsEnemy, GameObject vfx = null)
    {
        isExplosive = true;
        explosionRadius = radius;
        explosionDamage = damage;
        explosionDamageInfo = new DamageInfo(damage, elementType);
        enemyLayer = whatIsEnemy;
        explosionVFX = vfx;
    }
    
    protected override void MoveProjectile()
    {
    }
    
    protected override void OnHit(Collider other)
    {
        if (hasHit) return;

        EnemyBase enemy = other.GetComponent<EnemyBase>();

        if (enemy == null) return;

        hasHit = true;

        Vector3 impactPoint = other.ClosestPoint(transform.position);

        if (impactEffectPrefab != null)
        {
            GameObject impact = Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);
            Destroy(impact, 2f);
        }

        // Deal impact damage
        if (damageable != null)
        {
            damageable.TakeDamage(damageInfo);
        }

        // Apply bleed
        if (applyBleed)
        {
            enemy.ApplyDoT(bleedDamageInfo, bleedDuration);
        }

        // Explosion
        if (isExplosive)
        {
            Vector3 explosionCenter = enemy.GetCenterPoint();
        
            if (explosionVFX != null)
            {
                GameObject vfx = Instantiate(explosionVFX, impactPoint, Quaternion.identity);
                Destroy(vfx, 2f);
            }

            Collider[] enemies = Physics.OverlapSphere(explosionCenter, explosionRadius, enemyLayer);
            foreach (Collider col in enemies)
            {
                IDamageable explosionTarget = col.GetComponent<IDamageable>();
                if (explosionTarget != null)
                {
                    explosionTarget.TakeDamage(explosionDamageInfo);
                }
            }
        }

        DestroyProjectile();
    }
    
    protected override void DestroyProjectile()
    {
        if (trail != null)
        {
            trail.transform.SetParent(null);
            Destroy(trail.gameObject, trail.time);
        }
        
        base.DestroyProjectile();
    }
}