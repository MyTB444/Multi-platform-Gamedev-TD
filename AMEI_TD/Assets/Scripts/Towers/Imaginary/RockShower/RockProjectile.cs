using UnityEngine;

public class RockProjectile : TowerProjectileBase
{
    [SerializeField] private float damageRadius = 1.5f;
    [SerializeField] private LayerMask groundLayer;
    
    private float rockSize = 1f;
    private LayerMask whatIsEnemy;
    
    private Vector3 originalScale;
    private float baseDamageRadius;

    private void Awake()
    {
        originalScale = transform.localScale;
        baseDamageRadius = damageRadius;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        transform.localScale = originalScale;
        damageRadius = baseDamageRadius;
        rockSize = 1f;
    }
    
    public void Setup(DamageInfo newDamageInfo, LayerMask enemyLayer, float fallSpeed, float size = 1f)
    {
        damageInfo = newDamageInfo;
        whatIsEnemy = enemyLayer;
        speed = fallSpeed;
        direction = Vector3.down;
        spawnTime = Time.time;
        rockSize = size;
        damageRadius *= size;
    }
    
    protected override void OnHit(Collider other)
    {
        if (hasHit) return;

        if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            hasHit = true;
            DealAOEDamage();
    
            if (impactEffectPrefab != null)
            {
                Vector3 scaledSize = impactEffectPrefab.transform.localScale * rockSize;
                ObjectPooling.instance.GetVFX(impactEffectPrefab, transform.position, Quaternion.identity, scaledSize, 2f);
            }

            PlayImpactSound(transform.position);

            ObjectPooling.instance.Return(gameObject);
        }
    }
    
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

            PlayImpactSound(transform.position);

            ObjectPooling.instance.Return(gameObject);
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}