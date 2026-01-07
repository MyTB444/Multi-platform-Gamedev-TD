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
    
    private bool launched = false;
    private float spearSpeed;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        launched = false;
        applyBleed = false;
        isExplosive = false;
        explosionVFX = null;
    
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
            GameObject vfx = ObjectPooling.instance.GetVFXWithParent(spearVFX, spawnPoint, -1f);
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
        Vector3 impactPoint = other.ClosestPoint(transform.position);
        
        // If we hit an enemy, deal direct damage and apply effects
        if (enemy != null)
        {
            hasHit = true;

            if (impactEffectPrefab != null)
            {
                ObjectPooling.instance.GetVFX(impactEffectPrefab, impactPoint, Quaternion.identity, 2f);
            }

            PlayImpactSound();

            if (damageable != null)
            {
                damageable.TakeDamage(damageInfo);
            }

            if (applyBleed)
            {
                enemy.ApplyDoT(bleedDamageInfo, bleedDuration, 0.5f, false, 0f, default, DebuffType.Bleed);
            }

            if (isExplosive)
            {
                TriggerExplosion(enemy.GetCenterPoint(), impactPoint);
            }
            else
            {
                DestroyProjectile();
            }
        }
        // If we hit ground/environment and have explosive tip, still explode
        else if (isExplosive)
        {
            hasHit = true;
            
            PlayImpactSound();
            TriggerExplosion(impactPoint, impactPoint);
        }
        // Non-explosive spear hits ground - just destroy it
        else
        {
            hasHit = true;
            DestroyProjectile();
        }
    }
    
    private void TriggerExplosion(Vector3 explosionCenter, Vector3 vfxPosition)
    {
        if (explosionVFX != null)
        {
            ObjectPooling.instance.GetVFX(explosionVFX, vfxPosition, Quaternion.identity, 2f);
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
        
        DestroyProjectile();
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