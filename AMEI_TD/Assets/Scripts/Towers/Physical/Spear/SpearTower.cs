using UnityEngine;

public class SpearTower : TowerBase
{
    [Header("Spear Setup")]
    [SerializeField] private GameObject spearVisual;
    
    [Header("Animation Timing")]
    [SerializeField] private float spearRespawnDelay = 1.5f;
    
    [Header("Prediction")]
    [SerializeField] private float baseFlightTime = 0.4f;
    [SerializeField] private float speedMultiplier = 0.2f;
    
    private bool isAttacking = false;
    private Vector3 enemyVelocity;
    private Vector3 predictedPosition;
    private Vector3 lastEnemyPosition;
    
    protected override void FixedUpdate()
    {
        UpdateEnemyVelocity();
        UpdatePredictedPosition();
        base.FixedUpdate();
    }
    
    private void UpdateEnemyVelocity()
    {
        if (currentEnemy == null)
        {
            enemyVelocity = Vector3.zero;
            lastEnemyPosition = Vector3.zero;
            return;
        }
        
        Vector3 currentPos = currentEnemy.transform.position;
        
        if (lastEnemyPosition != Vector3.zero)
        {
            enemyVelocity = (currentPos - lastEnemyPosition) / Time.fixedDeltaTime;
        }
        
        lastEnemyPosition = currentPos;
    }
    
    private void UpdatePredictedPosition()
    {
        if (currentEnemy == null)
        {
            predictedPosition = Vector3.zero;
            return;
        }
        
        Vector3 currentTargetPos = currentEnemy.transform.position;
        float enemySpeed = enemyVelocity.magnitude;
        
        float predictionTime = baseFlightTime + (enemySpeed * speedMultiplier);
        
        predictedPosition = currentTargetPos + (enemyVelocity * predictionTime);
    }
    
    protected override void HandleRotation()
    {
        if (currentEnemy == null || towerBody == null) return;
        
        Vector3 targetPos = predictedPosition != Vector3.zero ? predictedPosition : currentEnemy.transform.position;
        
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
        return base.CanAttack() && !isAttacking;
    }
    
    protected override void Attack()
    {
        lastTimeAttacked = Time.time;
        isAttacking = true;
        
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(attackAnimationTrigger);
        }
    }
    
    // Called by animation event at the throw release frame
    public void OnThrowSpear()
    {
        FireSpear();
        
        if (spearVisual != null)
        {
            spearVisual.SetActive(false);
        }
        
        isAttacking = false;
        
        Invoke("OnSpearReady", spearRespawnDelay);
    }
    
    public void OnSpearReady()
    {
        if (spearVisual != null)
        {
            spearVisual.SetActive(true);
        }
    }
    
    private void FireSpear()
    {
        if (currentEnemy == null) return;
    
        // Spawn at exact spearVisual position and rotation
        Vector3 spawnPos = spearVisual.transform.position;
        Quaternion spawnRot = spearVisual.transform.rotation;
    
        Vector3 targetPos = predictedPosition != Vector3.zero ? predictedPosition : currentEnemy.transform.position;
    
        // Offset slightly forward in the fire direction (spear tip is transform.up)
        Vector3 fireDirection = spawnRot * Vector3.up;
        spawnPos += fireDirection * 0.5f; // Adjust this value as needed
    
        GameObject newSpear = Instantiate(projectilePrefab, spawnPos, spawnRot);
    
        SpearProjectile spear = newSpear.GetComponent<SpearProjectile>();
        IDamageable damageable = currentEnemy.GetComponent<IDamageable>();
    
        if (damageable != null)
        {
            spear.SetupSpear(targetPos, damageable, damage, projectileSpeed);
        }
    }
}