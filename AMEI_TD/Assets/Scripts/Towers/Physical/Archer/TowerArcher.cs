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
    
    // Locked target data
    private EnemyBase lockedTarget;
    private Vector3 lockedVelocity;
    private Vector3 lockedPosition;
    
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
        // Use locked target if attacking, otherwise current enemy
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
    
        // Use path-aware prediction instead of simple velocity
        predictedPosition = GetPathAwarePrediction(targetToPredict, predictionTime);
    }
    
    private Vector3 GetPathAwarePrediction(EnemyBase target, float predictionTime)
    {
        if (target == null) return Vector3.zero;
    
        Vector3 currentPos = target.transform.position;
        float speed = enemyVelocity.magnitude;
    
        if (speed < 0.1f) return currentPos;
    
        // How far enemy travels in prediction time
        float travelDistance = speed * predictionTime;
    
        // Get remaining distance to next waypoint
        float distanceToWaypoint = target.GetDistanceToNextWaypoint();
    
        // If enemy won't reach waypoint, use simple prediction
        if (distanceToWaypoint <= 0 || travelDistance < distanceToWaypoint)
        {
            return currentPos + (enemyVelocity * predictionTime);
        }
    
        // Enemy WILL turn - predict in two parts
        // Part 1: Travel to waypoint
        float timeToWaypoint = distanceToWaypoint / speed;
        Vector3 waypointPos = target.GetNextWaypointPosition();
    
        // Part 2: Travel along new direction after turn
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
        return base.CanAttack() && !isAttacking;
    }
    
    protected override void Attack()
    {
        lastTimeAttacked = Time.time;
        isAttacking = true;
        
        // Lock target data at attack start
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
        // Use locked target
        if (lockedTarget == null || !lockedTarget.gameObject.activeSelf)
        {
            ClearLockedTarget();
            return;
        }

        // Calculate prediction using locked/updated data
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