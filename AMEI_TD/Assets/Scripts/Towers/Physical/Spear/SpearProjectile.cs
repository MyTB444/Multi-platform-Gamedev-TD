using UnityEngine;

public class SpearProjectile : TowerProjectileBase
{
    [Header("Spear Settings")]
    [SerializeField] private TrailRenderer trail;
    
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
    
    protected override void MoveProjectile()
    {
    }
    
    protected override void OnHit(Collider other)
    {
        if (hasHit) return;
        
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        
        if (enemy == null) return;
        
        hasHit = true;
        
        if (impactEffectPrefab != null)
        {
            Vector3 impactPoint = other.ClosestPoint(transform.position);
            GameObject impact = Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);
            Destroy(impact, 2f);
        }
        
        if (damageable != null)
        {
            damageable.TakeDamage(damageInfo);
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