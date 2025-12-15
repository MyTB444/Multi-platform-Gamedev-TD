using UnityEngine;

public class TowerProjectileBase : MonoBehaviour
{
    protected Vector3 direction;
    protected float damage;
    protected float speed;
    protected bool isActive = true;
    protected bool hasHit = false;
    protected IDamageable damageable;

    [SerializeField] protected float maxLifeTime = 10f;
    protected float spawnTime;

    [Header("VFX")]
    [SerializeField] protected GameObject impactEffectPrefab;
    
    public void SetupProjectile(Vector3 targetPosition, IDamageable newDamageable, float newDamage, float newSpeed)
    {
        direction = (targetPosition - transform.position).normalized;
        damage = newDamage;
        speed = newSpeed;
        spawnTime = Time.time;
        damageable = newDamageable;
    }

    protected virtual void Update()
    {
        if (isActive == false) return;

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
        LayerMask TowerLayer = LayerMask.NameToLayer("Tower");
        if (other.gameObject.layer != TowerLayer)
            DestroyProjectile();

    }

    protected virtual void OnHit(Collider other)
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
    }

    protected virtual void DestroyProjectile()
    {
        Destroy(gameObject);
        isActive = false;
    }
}