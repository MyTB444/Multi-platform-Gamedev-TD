using UnityEngine;

public class SpearTower : TowerBase
{
    [Header("Spear Setup")]
    [SerializeField] private GameObject spearVisual;
    
    [Header("Animation Timing")]
    [SerializeField] private float spearRespawnDelay = 1.5f;
    [SerializeField] private float throwAnimationDelay = 0.5f;
    
    [Header("Spawn Offset")]
    [SerializeField] private float forwardSpawnOffset = 0.5f;
    
    private bool isAttacking = false;
    private Vector3 enemyVelocity;
    private Vector3 lastEnemyPosition;
    private int velocityFrameCount = 0;
    
    // Locked at attack time
    private Vector3 lockedTargetPosition;
    private IDamageable lockedDamageable;
    
    protected override void FixedUpdate()
    {
        UpdateEnemyVelocity();
        base.FixedUpdate();
    }
    
    private void UpdateEnemyVelocity()
    {
        if (currentEnemy == null)
        {
            enemyVelocity = Vector3.zero;
            lastEnemyPosition = Vector3.zero;
            velocityFrameCount = 0;
            return;
        }
        
        Vector3 currentPos = currentEnemy.transform.position;
        
        if (lastEnemyPosition != Vector3.zero)
        {
            velocityFrameCount++;
            
            if (velocityFrameCount > 1)
            {
                enemyVelocity = (currentPos - lastEnemyPosition) / Time.fixedDeltaTime;
                enemyVelocity.x = 0f;
            }
        }
        
        lastEnemyPosition = currentPos;
    }
    
    private Vector3 PredictTargetPosition()
    {
        if (currentEnemy == null) return Vector3.zero;
        
        Vector3 spawnPos = spearVisual.transform.position;
        Vector3 enemyCenter = currentEnemy.GetCenterPoint();
        
        Vector3 predictedPos = enemyCenter;
        
        for (int i = 0; i < 3; i++)
        {
            float distance = Vector3.Distance(spawnPos, predictedPos);
            float flightTime = distance / projectileSpeed;
            float totalTime = throwAnimationDelay + flightTime;
            
            Vector3 movement = enemyVelocity * totalTime;
            predictedPos = new Vector3(
                enemyCenter.x + movement.x,
                enemyCenter.y,
                enemyCenter.z + movement.z
            );
        }
        
        return predictedPos;
    }
    
    protected override void HandleRotation()
    {
        if (currentEnemy == null || towerBody == null) return;
        
        Vector3 targetPos = PredictTargetPosition();
        if (targetPos == Vector3.zero) targetPos = currentEnemy.GetCenterPoint();
        
        Vector3 direction = targetPos - towerBody.position;
        direction.y = 0;
        
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            towerBody.rotation = Quaternion.Slerp(towerBody.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    protected override bool CanAttack()
    {
        bool hasValidTracking = velocityFrameCount > 1;
        return base.CanAttack() && !isAttacking && hasValidTracking;
    }
    
    protected override void Attack()
    {
        lastTimeAttacked = Time.time;
        isAttacking = true;
        
        if (currentEnemy != null)
        {
            lockedTargetPosition = PredictTargetPosition();
            lockedDamageable = currentEnemy.GetComponent<IDamageable>();
        }
        
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(attackAnimationTrigger);
        }
    }
    
    public void OnThrowSpear()
    {
        FireSpear();
        
        if (spearVisual != null)
        {
            spearVisual.SetActive(false);
        }
        
        Invoke("OnSpearReady", spearRespawnDelay);
    }
    
    public void OnSpearReady()
    {
        if (spearVisual != null)
        {
            spearVisual.SetActive(true);
        }
        
        isAttacking = false;
    }
    
    private void FireSpear()
    {
        if (lockedDamageable == null) return;
        
        Vector3 spawnPos = spearVisual.transform.position;
        Quaternion spawnRot = spearVisual.transform.rotation;
        
        Vector3 fireDirection = spawnRot * Vector3.up;
        spawnPos += fireDirection * forwardSpawnOffset;
        
        GameObject newSpear = Instantiate(projectilePrefab, spawnPos, spawnRot);
        SpearProjectile spear = newSpear.GetComponent<SpearProjectile>();
        
        spear.SetupSpear(lockedTargetPosition, lockedDamageable, CreateDamageInfo(), projectileSpeed);
        
        lockedDamageable = null;
    }
}