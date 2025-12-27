using UnityEngine;

public class RockProjectile : TowerProjectileBase
{
    [SerializeField] private float damageRadius = 1.5f;
    [SerializeField] private LayerMask groundLayer;
    
    private LayerMask whatIsEnemy;
    
    public void Setup(float damageAmount, LayerMask enemyLayer, float fallSpeed, float size = 1f)
    {
        damageInfo.amount = damageAmount;
        whatIsEnemy = enemyLayer;
        speed = fallSpeed;
        direction = Vector3.down;
        spawnTime = Time.time;
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
                GameObject vfx = Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 2f);
            }
            
            Destroy(gameObject);
        }
    }
    
    private void DealAOEDamage()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, damageRadius, whatIsEnemy);
    
        Debug.Log($"Rock impact at {transform.position}! Enemies in radius: {enemies.Length}");
    
        foreach (Collider enemy in enemies)
        {
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Debug.Log($"Dealing {damageInfo.amount} damage to {enemy.name}");
                damageable.TakeDamage(damageInfo.amount);
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
    
        Debug.Log($"Rock hit: {other.name} on layer {other.gameObject.layer}");
    
        bool hitEnemy = ((1 << other.gameObject.layer) & whatIsEnemy) != 0;
        bool hitGround = ((1 << other.gameObject.layer) & groundLayer) != 0;
    
        if (hitEnemy || hitGround)
        {
            hasHit = true;
            DealAOEDamage();
        
            if (impactEffectPrefab != null)
            {
                GameObject vfx = Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 2f);
            }
        
            Destroy(gameObject);
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}