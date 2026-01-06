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
    
    private EnemyBase lockedTarget;
    private Vector3 lockedVelocity;
    private Vector3 lockedPosition;
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    private void Update()
    {
        if (isAttacking && Time.time > lastTimeAttacked + attackCooldown + 1f)
        {
            isAttacking = false;
        
            if (characterAnimator != null)
            {
                characterAnimator.SetBool("Attack", false);
            }
        
            if (arrowVisual != null)
            {
                arrowVisual.SetActive(true);
            }
            
            ClearLockedTarget();
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
            case TowerUpgradeType.PhysicalAttackSpeed:
                attackSpeedBoost = enabled;
                ApplyStatUpgrades();
                break;
            case TowerUpgradeType.PhysicalRange:
                rangeBoost = enabled;
                ApplyStatUpgrades();
                break;
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
        EnemyBase targetToTrack = isAttacking && lockedTarget != null ? lockedTarget : currentEnemy;
        
        if (targetToTrack == null || !targetToTrack.gameObject.activeSelf)
        {
            enemyVelocity = Vector3.zero;
            lastEnemyPosition = Vector3.zero;
            return;
        }
        
        Vector3 currentPos = targetToTrack.transform.position;
        
        if (lastEnemyPosition != Vector3.zero)
        {
            enemyVelocity = (currentPos - lastEnemyPosition) / Time.fixedDeltaTime;
        }
        
        lastEnemyPosition = currentPos;
    }
    
    private void UpdatePredictedPosition()
    {
        EnemyBase targetToPredict = isAttacking && lockedTarget != null ? lockedTarget : currentEnemy;
    
        if (targetToPredict == null || !targetToPredict.gameObject.activeSelf)
        {
            predictedPosition = Vector3.zero;
            return;
        }
    
        float enemySpeed = enemyVelocity.magnitude;
        float predictionTime = baseFlightTime + (enemySpeed * speedMultiplier);
    
        float distance = Vector3.Distance(transform.position, targetToPredict.transform.position);
        float predictionScale = Mathf.Clamp01(distance / attackRange);
        predictionTime *= predictionScale;
    
        predictedPosition = GetPathAwarePrediction(targetToPredict, predictionTime);
    }
    
    private Vector3 GetPathAwarePrediction(EnemyBase target, float predictionTime)
    {
        if (target == null) return Vector3.zero;
    
        Vector3 currentPos = target.transform.position;
        float speed = enemyVelocity.magnitude;
    
        if (speed < 0.1f) return currentPos;
    
        float travelDistance = speed * predictionTime;
        float distanceToWaypoint = target.GetDistanceToNextWaypoint();
    
        if (distanceToWaypoint <= 0 || travelDistance < distanceToWaypoint)
        {
            return currentPos + (enemyVelocity * predictionTime);
        }
    
        float timeToWaypoint = distanceToWaypoint / speed;
        Vector3 waypointPos = target.GetNextWaypointPosition();
        float remainingTime = predictionTime - timeToWaypoint;
        Vector3 directionAfterTurn = target.GetDirectionAfterNextWaypoint();
    
        return waypointPos + (directionAfterTurn * speed * remainingTime);
    }
    
    protected override void HandleRotation()
    {
        EnemyBase targetToFace = isAttacking && lockedTarget != null ? lockedTarget : currentEnemy;
        
        if (targetToFace == null || towerBody == null) return;
        
        Vector3 targetPos = predictedPosition != Vector3.zero ? predictedPosition : targetToFace.transform.position;
        
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
        if (currentEnemy == null) return false;
        if (!IsFacingEnemy(15f)) return false;
        return base.CanAttack() && !isAttacking;
    }

    private bool IsFacingEnemy(float maxAngle)
    {
        if (currentEnemy == null || towerBody == null) return false;
    
        Vector3 directionToEnemy = currentEnemy.transform.position - towerBody.position;
        directionToEnemy.y = 0;
    
        float angle = Vector3.Angle(towerBody.forward, directionToEnemy);
        return angle <= maxAngle;
    }
    
    protected override void Attack()
    {
        lastTimeAttacked = Time.time;
        isAttacking = true;
        
        if (currentEnemy != null)
        {
            lockedTarget = currentEnemy;
            lockedVelocity = enemyVelocity;
            lockedPosition = currentEnemy.transform.position;
        }
    
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
        
        PlayAttackSound();
        
        FireArrow();
        
        if (arrowVisual != null)
        {
            arrowVisual.SetActive(false);
        }
        
        isAttacking = false;
        ClearLockedTarget();
        
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
        if (activeArrowVisualVFX != null)
        {
            ObjectPooling.instance.Return(activeArrowVisualVFX);
            activeArrowVisualVFX = null;
        }
    
        Transform spawnPoint = arrowVisualVFXPoint != null ? arrowVisualVFXPoint : arrowVisual.transform;
    
        if (fireArrows && fireArrowVFX != null)
        {
            activeArrowVisualVFX = ObjectPooling.instance.GetVFXWithParent(fireArrowVFX, spawnPoint, -1f);
            activeArrowVisualVFX.transform.localPosition = Vector3.zero;
        }
        else if (poisonArrows && poisonArrowVFX != null)
        {
            activeArrowVisualVFX = ObjectPooling.instance.GetVFXWithParent(poisonArrowVFX, spawnPoint, -1f);
            activeArrowVisualVFX.transform.localPosition = Vector3.zero;
        }
    }
    
    private void FireArrow()
    {
        if (lockedTarget == null || !lockedTarget.gameObject.activeSelf)
        {
            ClearLockedTarget();
            return;
        }

        Vector3 targetPos = lockedTarget.transform.position;
        float enemySpeed = enemyVelocity.magnitude;
        float predictionTime = baseFlightTime + (enemySpeed * speedMultiplier);
        Vector3 finalPredictedPosition = targetPos + (enemyVelocity * predictionTime);

        Vector3 spawnPos = arrowVisual.transform.position;
        Quaternion spawnRot = arrowVisual.transform.rotation;
        float distance = Vector3.Distance(spawnPos, finalPredictedPosition);

        GameObject newArrow = ObjectPooling.instance.Get(projectilePrefab);
        newArrow.transform.position = spawnPos;
        newArrow.transform.rotation = spawnRot;
        newArrow.SetActive(true);

        ArrowProjectile arrow = newArrow.GetComponent<ArrowProjectile>();
        IDamageable damageable = lockedTarget.GetComponent<IDamageable>();

        if (damageable != null)
        {
            arrow.SetupArcProjectile(finalPredictedPosition, damageable, CreateDamageInfo(), projectileSpeed, distance);

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
    
    private void ClearLockedTarget()
    {
        lockedTarget = null;
        lockedVelocity = Vector3.zero;
        lockedPosition = Vector3.zero;
    }
}