using UnityEngine;

public class TowerProjectileBase : MonoBehaviour
{
    private Vector3 direction;
    private float damage;
    private float speed;
    private float threshold = .01f;
    private bool isActive = true;
    private IDamageable damageable;

    [SerializeField] protected float maxLifeTime = 10f;
    protected float spawnTime;

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
        if (other.GetComponent<EnemyBase>())
        {
            if (damageable == null) return;

            damageable.TakeDamage(damage);
        }
    }

    protected virtual void DestroyProjectile()
    {
        Destroy(gameObject);
        isActive = false;
    }
}
