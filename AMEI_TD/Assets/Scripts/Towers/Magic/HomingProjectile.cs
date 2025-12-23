using UnityEngine;

public class HomingProjectile : TowerProjectileBase
{
    [Header("Homing Settings")]
    [SerializeField] private float rotationSpeed = 200f;
    
    private Transform target;
    private bool isHoming = true;
    

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

        // Target center point
        EnemyBase enemy = target.GetComponent<EnemyBase>();
        Vector3 targetPos = enemy != null ? enemy.GetCenterPoint() : target.position;

        Vector3 direction = (targetPos - transform.position).normalized;
        
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        transform.position += transform.forward * (speed * Time.deltaTime);
    }

    protected override void OnHit(Collider other)
    {
        base.OnHit(other);
        
        isHoming = false;
    }
}