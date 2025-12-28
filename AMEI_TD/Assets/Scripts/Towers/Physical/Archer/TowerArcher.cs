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
    
    [Header("Arrow Visual VFX")]
    [SerializeField] private Transform arrowVisualVFXPoint;
    private GameObject activeArrowVisualVFX;
    
    [Header("Arrow Effects")]
    [SerializeField] private bool poisonArrows = false;
    [SerializeField] private float poisonDamage = 2f;
    [SerializeField] private float poisonDuration = 3f;
    [SerializeField] private GameObject poisonArrowVFX;

    [SerializeField] private bool fireArrows = false;
    [SerializeField] private float fireDamage = 4f;
    [SerializeField] private float fireDuration = 3f;
    [SerializeField] private GameObject fireArrowVFX;
    
    private bool isAttacking = false;
    private Vector3 enemyVelocity;
    private Vector3 predictedPosition;
    private Vector3 lastEnemyPosition;
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    private void Update()
    {
        // Safety reset if stuck attacking
        if (isAttacking && Time.time > lastTimeAttacked + attackCooldown + 1f)
        {
            Debug.Log("Force reset isAttacking");
            isAttacking = false;
        
            if (characterAnimator != null)
            {
                characterAnimator.SetBool("Attack", false);
            }
        
            if (arrowVisual != null)
            {
                arrowVisual.SetActive(true);
            }
        }
    
        // Debug what's blocking attack
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log($"currentEnemy: {currentEnemy}");
            Debug.Log($"isAttacking: {isAttacking}");
            Debug.Log($"Time since attack: {Time.time - lastTimeAttacked}");
            Debug.Log($"attackCooldown: {attackCooldown}");
            Debug.Log($"base.CanAttack(): {base.CanAttack()}");
        }
    }
    
    protected override void Start()
    {
        base.Start();
        UpdateArrowVisualVFX();
    }
    
    protected override void FixedUpdate()
    {
        UpdateEnemyVelocity();
        UpdatePredictedPosition();
        base.FixedUpdate();
    }
    
    public override void SetUpgrade(TowerUpgradeType upgradeType, bool enabled)
    {
        base.SetUpgrade(upgradeType, enabled);
    
        switch (upgradeType)
        {
            case TowerUpgradeType.PoisonArrows:
                poisonArrows = enabled;
                UpdateArrowVisualVFX();
                break;
            case TowerUpgradeType.FireArrows:
                fireArrows = enabled;
                UpdateArrowVisualVFX();
                break;
        }
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
            characterAnimator.SetTrigger("Attack");
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
    
    private void UpdateArrowVisualVFX()
    {
        // Clear old VFX
        if (activeArrowVisualVFX != null)
        {
            Destroy(activeArrowVisualVFX);
            activeArrowVisualVFX = null;
        }
    
        // Spawn new VFX based on current upgrade
        Transform spawnPoint = arrowVisualVFXPoint != null ? arrowVisualVFXPoint : arrowVisual.transform;
    
        if (fireArrows && fireArrowVFX != null)
        {
            activeArrowVisualVFX = Instantiate(fireArrowVFX, spawnPoint);
            activeArrowVisualVFX.transform.localPosition = Vector3.zero;
        }
        else if (poisonArrows && poisonArrowVFX != null)
        {
            activeArrowVisualVFX = Instantiate(poisonArrowVFX, spawnPoint);
            activeArrowVisualVFX.transform.localPosition = Vector3.zero;
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
            arrow.SetupArcProjectile(predictedPosition, damageable, CreateDamageInfo(), projectileSpeed, distance);
        
            if (fireArrows)
            {
                arrow.SetFireEffect(fireDamage, fireDuration, elementType, fireArrowVFX);
            }
            else if (poisonArrows)
            {
                arrow.SetPoisonEffect(poisonDamage, poisonDuration, elementType, poisonArrowVFX);
            }
        }
    }
}