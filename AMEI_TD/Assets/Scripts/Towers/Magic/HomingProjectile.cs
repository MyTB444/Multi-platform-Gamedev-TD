using UnityEngine;

public class HomingProjectile : TowerProjectileBase
{
    [Header("Homing Settings")]
    [SerializeField] private float rotationSpeed = 200f;
    [SerializeField] private float retargetRange = 5f;
    
    private Transform target;
    private bool isHoming = true;
    private Vector3 lastKnownTargetPos;
    private bool targetLost = false;
    private LayerMask enemyLayer;

    public void SetupHomingProjectile(Transform enemyTarget, IDamageable newDamageable, float newDamage, float newSpeed, LayerMask whatIsEnemy)
    {
        target = enemyTarget;
        damageable = newDamageable;
        damageInfo.amount = newDamage;
        speed = newSpeed;
        spawnTime = Time.time;
        isHoming = true;
        hasHit = false;
        targetLost = false;
        enemyLayer = whatIsEnemy;
        
        if (target != null)
        {
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            lastKnownTargetPos = enemy != null ? enemy.GetCenterPoint() : target.position;
        }
    }

    protected override void MoveProjectile()
    {
        // Check if target is dead
        if (target == null || !target.gameObject.activeSelf)
        {
            Transform newTarget = FindNewTarget();
            
            if (newTarget != null)
            {
                target = newTarget;
                damageable = target.GetComponent<IDamageable>();
                targetLost = false;
            }
            else
            {
                isHoming = false;
                targetLost = true;
            }
        }
        
        // Update last known position if we have a target
        if (target != null && target.gameObject.activeSelf)
        {
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            lastKnownTargetPos = enemy != null ? enemy.GetCenterPoint() : target.position;
        }
        
        Vector3 targetPos;
        
        if (isHoming && target != null)
        {
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            targetPos = enemy != null ? enemy.GetCenterPoint() : target.position;
        }
        else
        {
            targetPos = lastKnownTargetPos;
        }
        
        Vector3 direction = (targetPos - transform.position).normalized;
        
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        transform.position += transform.forward * (speed * Time.deltaTime);
        
        // If target lost, check if we reached the destination
        if (targetLost)
        {
            float distToTarget = Vector3.Distance(transform.position, lastKnownTargetPos);
            if (distToTarget < 0.5f)
            {
                if (impactEffectPrefab != null)
                {
                    GameObject impact = Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
                    Destroy(impact, 2f);
                }
                
                DestroyProjectile();
            }
        }
    }
    
    private Transform FindNewTarget()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, retargetRange, enemyLayer);
        
        Transform closest = null;
        float closestDist = float.MaxValue;
        
        foreach (Collider col in enemies)
        {
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = col.transform;
                }
            }
        }
        
        return closest;
    }

    protected override void OnHit(Collider other)
    {
        base.OnHit(other);
        
        isHoming = false;
    }
}