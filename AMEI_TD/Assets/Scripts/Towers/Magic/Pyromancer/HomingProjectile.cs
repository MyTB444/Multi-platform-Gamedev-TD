using UnityEngine;

public class HomingProjectile : TowerProjectileBase
{
    [Header("Homing Settings")]
    [SerializeField] private float rotationSpeed = 200f;
    [SerializeField] private float retargetRange = 5f;
    
    [Header("Burn Effects")]
    private bool canBurn = false;
    private float burnChancePercent;
    private float burnDamage;
    private float burnDuration;
    private DamageInfo burnDamageInfo;
    private bool canSpreadBurn = false;
    private float spreadRadius;
    private LayerMask spreadEnemyLayer;
    
    [Header("AoE Effects")]
    private bool hasAoE = false;
    private float aoERadius;
    private float aoEDamagePercent;
    private LayerMask aoEEnemyLayer;
    
    private Transform target;
    private bool isHoming = true;
    private Vector3 lastKnownTargetPos;
    private bool targetLost = false;
    private LayerMask enemyLayer;
    
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        transform.localScale = originalScale;
        target = null;
        isHoming = true;
        targetLost = false;
        canBurn = false;
        hasAoE = false;
        canSpreadBurn = false;
    }

    public void SetupHomingProjectile(Transform enemyTarget, IDamageable newDamageable, DamageInfo newDamageInfo, float newSpeed, LayerMask whatIsEnemy)
    {
        target = enemyTarget;
        damageable = newDamageable;
        damageInfo = newDamageInfo;
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
    
    public void SetBurnEffect(float chance, float damage, float duration, ElementType elementType, bool spread = false, float spreadRadius = 0f, LayerMask enemyLayer = default)
    {
        canBurn = true;
        burnChancePercent = chance;
        burnDamage = damage;
        burnDuration = duration;
        burnDamageInfo = new DamageInfo(damage, elementType, true);
        canSpreadBurn = spread;
        this.spreadRadius = spreadRadius;
        spreadEnemyLayer = enemyLayer;
    }
    
    public void SetAoEEffect(float radius, float damagePercent, LayerMask enemyLayer)
    {
        hasAoE = true;
        aoERadius = radius;
        aoEDamagePercent = damagePercent;
        aoEEnemyLayer = enemyLayer;
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
        if (hasHit) return;
    
        hasHit = true;
    
        if (impactEffectPrefab != null)
        {
            Vector3 impactPoint = other.ClosestPoint(transform.position);
            GameObject impact = Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);
            Destroy(impact, 2f);
        }
    
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            // Deal impact damage
            damageable?.TakeDamage(damageInfo);
        
            // Burn chance
            if (canBurn && Random.value <= burnChancePercent)
            {
                enemy.ApplyDoT(burnDamageInfo, burnDuration, 0.5f, canSpreadBurn, spreadRadius, spreadEnemyLayer);
            }
        
            // AoE damage
            if (hasAoE)
            {
                DamageInfo aoEDamage = new DamageInfo(damageInfo.amount * aoEDamagePercent, damageInfo.elementType);
                Collider[] enemies = Physics.OverlapSphere(transform.position, aoERadius, aoEEnemyLayer);
            
                foreach (Collider col in enemies)
                {
                    if (col.gameObject == other.gameObject) continue;
                
                    IDamageable target = col.GetComponent<IDamageable>();
                    if (target != null)
                    {
                        target.TakeDamage(aoEDamage);
                    }
                }
            }
        }
    
        isHoming = false;
        DestroyProjectile();
    }
}