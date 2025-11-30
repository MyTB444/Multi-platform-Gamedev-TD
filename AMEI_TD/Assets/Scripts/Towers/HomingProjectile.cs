using UnityEngine;

public class HomingProjectile : TowerProjectileBase
{
    [Header("Homing Settings")]
    [SerializeField] private float rotationSpeed = 200f;
    
    [Header("VFX")]
    [SerializeField] private GameObject impactEffectPrefab;
    
    private Transform target;
    private bool isHoming = true;
    private bool hasHit = false;

    public void SetupHomingProjectile(Transform enemyTarget, IDamageable newDamageable, float newDamage, float newSpeed)
    {
        target = enemyTarget;
        damageable = newDamageable;
        damage = newDamage;
        speed = newSpeed;
        spawnTime = Time.time;
        isHoming = true;
        hasHit = false;
    }

    protected override void MoveProjectile()
    {
        if (!isHoming || target == null)
        {
            transform.position += transform.forward * (speed * Time.deltaTime);
            return;
        }

        Vector3 direction = (target.position - transform.position).normalized;
        
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        transform.position += transform.forward * (speed * Time.deltaTime);
    }

    protected override void OnHit(Collider other)
    {
        if (hasHit) return; // Prevent multiple hits
        
        hasHit = true;
        
        // Spawn impact effect
        if (impactEffectPrefab != null)
        {
            GameObject impact = Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
            Destroy(impact, 2f);
        }
        
        // Deal damage if hit an enemy
        if (other.GetComponent<EnemyBase>())
        {
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
        }
        
        isHoming = false;
    }
    
    protected override void DestroyProjectile()
    {
        // Always spawn impact effect when destroyed
        if (!hasHit && impactEffectPrefab != null)
        {
            GameObject impact = Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
            Destroy(impact, 2f);
        }
        
        Destroy(gameObject);
        isActive = false;
    }
}