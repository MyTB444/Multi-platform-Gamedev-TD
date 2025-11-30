using UnityEngine;

public class TowerArcher : TowerBase
{
    [Header("Archer Setup")]
    [SerializeField] private BowController bowController;
    [SerializeField] private GameObject arrowVisual;
    
    [Header("Animation Timing")]
    [SerializeField] private float baseAnimationLength = 1f;
    [SerializeField] private float arrowRespawnDelay = 0.9f;
    
    [Header("Prediction")]
    [SerializeField] private float baseFlightTime = 0.3f;
    [SerializeField] private float speedMultiplier = 0.15f;
    
    private bool isAttacking = false;
    private Vector3 enemyVelocity;
    private Vector3 predictedPosition;
    private Vector3 lastEnemyPosition;
    
    protected override void Awake()
    {
        base.Awake();
    }
    
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
    
    private void UpdateAnimationSpeed()
    {
        if (characterAnimator == null) return;
        
        float animSpeed = baseAnimationLength / attackCooldown;
        characterAnimator.SetFloat("AttackSpeed", animSpeed);
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
            UpdateAnimationSpeed();
            characterAnimator.SetBool("Attack", true);
        }
    }
    
    public void OnDrawBow()
    {
        if (bowController != null)
        {
            bowController.DrawBow();
        }
        
        if (arrowVisual != null)
        {
            arrowVisual.SetActive(true);
        }
    }
    
    public void OnReleaseBow()
    {
        if (bowController != null)
        {
            bowController.ReleaseBow();
        }
        
        FireArrow();
        
        if (arrowVisual != null)
        {
            arrowVisual.SetActive(false);
        }
        
        characterAnimator.SetBool("Attack", false);
        isAttacking = false;
        
        Invoke("OnArrowReady", arrowRespawnDelay);
    }
    
    public void OnArrowReady()
    {
        if (arrowVisual != null)
        {
            arrowVisual.SetActive(true);
        }
    }
    
    private void FireArrow()
    {
        if (currentEnemy == null) return;
        
        Vector3 spawnPos = arrowVisual.transform.position;
        Quaternion spawnRot = arrowVisual.transform.rotation;
        
        float distance = Vector3.Distance(spawnPos, predictedPosition);
        
        GameObject newArrow = Instantiate(projectilePrefab, spawnPos, spawnRot);
        
        ArrowProjectile arrow = newArrow.GetComponent<ArrowProjectile>();
        
        IDamageable damageable = currentEnemy.GetComponent<IDamageable>();
        
        if (damageable != null)
        {
            arrow.SetupArcProjectile(predictedPosition, damageable, damage, projectileSpeed, distance);
        }
    }
}