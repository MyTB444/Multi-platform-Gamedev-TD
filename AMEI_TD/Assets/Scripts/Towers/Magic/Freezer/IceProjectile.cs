using UnityEngine;

public class IceProjectile : TowerProjectileBase
{
    [Header("Homing Settings")]
    [SerializeField] private float rotationSpeed = 200f;
    [SerializeField] private float retargetRange = 5f;
    
    private Transform target;
    private bool isHoming = true;
    private LayerMask enemyLayer;
    private Quaternion rotationOffset;
    private Vector3 lastKnownTargetPos;
    private bool targetLost = false;
    
    // Ice settings passed from tower
    private float slowPercent;
    private float slowDuration;
    private float effectRadius;
    private DamageInfo dotDamageInfo;
    private float dotDuration;
    private float dotTickInterval;
    
    public void SetupIceProjectile(
        Transform enemyTarget, 
        IDamageable newDamageable, 
        DamageInfo newDamageInfo, 
        float newSpeed, 
        LayerMask whatIsEnemy,
        float newSlowPercent,
        float newSlowDuration,
        float newEffectRadius,
        float newDotDamage,
        float newDotDuration,
        float newDotTickInterval,
        Vector3 visualRotationOffset)
    {
        target = enemyTarget;
        damageable = newDamageable;
        damageInfo = newDamageInfo;
        speed = newSpeed;
        spawnTime = Time.time;
        isHoming = true;
        hasHit = false;
        enemyLayer = whatIsEnemy;
        targetLost = false;
        
        slowPercent = newSlowPercent;
        slowDuration = newSlowDuration;
        effectRadius = newEffectRadius;
        dotDamageInfo = new DamageInfo(newDotDamage, newDamageInfo.elementType);
        dotDuration = newDotDuration;
        dotTickInterval = newDotTickInterval;
        rotationOffset = Quaternion.Euler(visualRotationOffset);
        
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
            // Try to find new target
            Transform newTarget = FindNewTarget();
            
            if (newTarget != null)
            {
                target = newTarget;
                damageable = target.GetComponent<IDamageable>();
                targetLost = false;
            }
            else
            {
                // No target - fly to last known position
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
        
        Quaternion flightRotation = Quaternion.LookRotation(direction);
        Quaternion targetRotation = flightRotation * rotationOffset;
        
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        transform.position += direction * (speed * Time.deltaTime);
        
        // If target lost, check if we reached the destination
        if (targetLost)
        {
            float distToTarget = Vector3.Distance(transform.position, lastKnownTargetPos);
            if (distToTarget < 0.5f)
            {
                // Reached destination - apply AoE and destroy
                ApplyEffectsInRadius();
                
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
        if (hasHit) return;
        
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy == null) return;
        
        ApplyEffectsInRadius();
        
        isHoming = false;
        
        base.OnHit(other);
    }
    
    private void ApplyEffectsInRadius()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, effectRadius, enemyLayer);
        
        foreach (Collider col in enemies)
        {
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.ApplySlow(slowPercent, slowDuration);
                enemy.ApplyDoT(dotDamageInfo, dotDuration, dotTickInterval);
            }
        }
    }
}