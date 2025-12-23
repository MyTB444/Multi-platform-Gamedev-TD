using UnityEngine;

public class SpearProjectile : TowerProjectileBase
{
    [Header("Spear Settings")]
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private float gravityMultiplier = 2f;
    
    private Rigidbody rb;
    private bool launched = false;
    private Vector3 targetPosition;
    private float spearSpeed;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    public void SetupSpear(Vector3 targetPos, IDamageable newDamageable, float newDamage, float newSpeed)
    {
        damage = newDamage;
        damageable = newDamageable;
        spawnTime = Time.time;
        targetPosition = targetPos;
        spearSpeed = newSpeed;
    
        // Fire along UP instead of FORWARD (green axis is the spear tip)
        Vector3 fireDirection = transform.up;
    
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
    
        rb.velocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
    
        // Rotate so the UP axis (spear tip) faces velocity direction
        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.FromToRotation(Vector3.up, rb.velocity.normalized) ;
        }
    }
    
    protected override void MoveProjectile()
    {
        // Using rigidbody instead
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
            damageable.TakeDamage(damage);
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