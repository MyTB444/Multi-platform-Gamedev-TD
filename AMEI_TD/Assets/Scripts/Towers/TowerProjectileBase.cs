using UnityEngine;

public class TowerProjectileBase : MonoBehaviour
{
    private Vector3 direction;
    private float damage;
    private float speed;
    private float threshold = .01f;
    private bool isActive = true;

    [SerializeField] protected float maxLifeTime = 10f;
    protected float spawnTime;

    public void SetupProjectile(Vector3 targetPosition, float newDamage, float newSpeed)
    {
        direction = (targetPosition - transform.position).normalized;
        damage = newDamage;
        speed = newSpeed;
        spawnTime = Time.time;
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
        DestroyProjectile();
    }

    protected virtual void OnHit(Collider other)
    {
        if (other.GetComponent<EnemyBase>())
        {
            Debug.Log("Enemy Hit");
            // Check has damageable component.
            // Deal Damage using TakeDamage(damage).
        }
    }

    protected virtual void DestroyProjectile()
    {
        Destroy(gameObject);
        isActive = false;
    }
}
