using UnityEngine;

public class SpearTower : TowerBase
{
    [Header("Spear Setup")]
    [SerializeField] private GameObject spearVisual;
    
    [Header("Prediction")]
    [SerializeField] private float baseFlightTime = 0.2f;
    [SerializeField] private float speedMultiplier = 0.1f;
    
    [Header("Animation Timing")]
    [SerializeField] private float spearRespawnDelay = 1.5f;
    [SerializeField] private float throwAnimationDelay = 0.5f;
    
    [Header("Spawn Offset")]
    [SerializeField] private float forwardSpawnOffset = 0.5f;
    
    [Header("Spear Visual VFX")]
    [SerializeField] private Transform spearVisualVFXPoint;
    private GameObject activeSpearVisualVFX;
    
    [Header("Spear Effects")]
    [SerializeField] private bool bleedSpear = false;
    [SerializeField] private float bleedDamage = 3f;
    [SerializeField] private float bleedDuration = 4f;
    [SerializeField] private GameObject bleedSpearVFX;

    [SerializeField] private bool explosiveTip = false;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float explosionDamage = 10f;
    [SerializeField] private GameObject explosionVFX;
    
    private bool isAttacking = false;
    private Vector3 enemyVelocity;
    private Vector3 lastEnemyPosition;
    private int velocityFrameCount = 0;
    
    // Locked at attack time
    private Vector3 lockedTargetPosition;
    private Vector3 lockedVelocity;
    private IDamageable lockedDamageable;
    private EnemyBase lockedTarget;
    
    
    protected override void Start()
    {
        base.Start();
        UpdateSpearVisualVFX();
    }
    
    protected override void FixedUpdate()
    {
        UpdateEnemyVelocity();
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
            case TowerUpgradeType.BarbedSpear:
                bleedSpear = enabled;
                UpdateSpearVisualVFX();
                break;
            case TowerUpgradeType.ExplosiveTip:
                explosiveTip = enabled;
                break;
        }
    }
    
    private void UpdateEnemyVelocity()
    {
        // Use locked target if attacking
        EnemyBase targetToTrack = isAttacking && lockedTarget != null ? lockedTarget : currentEnemy;
        
        if (targetToTrack == null || !targetToTrack.gameObject.activeSelf)
        {
            enemyVelocity = Vector3.zero;
            lastEnemyPosition = Vector3.zero;
            velocityFrameCount = 0;
            return;
        }
        
        Vector3 currentPos = targetToTrack.transform.position;
        
        if (lastEnemyPosition != Vector3.zero)
        {
            velocityFrameCount++;
            
            if (velocityFrameCount > 1)
            {
                enemyVelocity = (currentPos - lastEnemyPosition) / Time.fixedDeltaTime;
            }
        }
        
        lastEnemyPosition = currentPos;
    }
    
    private void UpdateSpearVisualVFX()
    {
        if (activeSpearVisualVFX != null)
        {
            ObjectPooling.instance.Return(activeSpearVisualVFX);
            activeSpearVisualVFX = null;
        }
    
        if (bleedSpear && bleedSpearVFX != null)
        {
            Transform spawnPoint = spearVisualVFXPoint != null ? spearVisualVFXPoint : spearVisual.transform;
            activeSpearVisualVFX = ObjectPooling.instance.GetVFXWithParent(bleedSpearVFX, spawnPoint, -1f);
            activeSpearVisualVFX.transform.localPosition = Vector3.zero;
        }
    }
    
    private Vector3 PredictTargetPosition()
    {
        EnemyBase targetToPredict = isAttacking && lockedTarget != null ? lockedTarget : currentEnemy;

        if (targetToPredict == null || !targetToPredict.gameObject.activeSelf) return Vector3.zero;

        Vector3 enemyCenter = targetToPredict.GetCenterPoint();
        float enemySpeed = enemyVelocity.magnitude;
    
        // Simple prediction like Archer
        float predictionTime = baseFlightTime + (enemySpeed * speedMultiplier);
    
        Vector3 predictedPos = enemyCenter + (enemyVelocity * predictionTime);
        predictedPos.y = enemyCenter.y;

        return predictedPos;
    }

    private Vector3 GetPathAwarePrediction(EnemyBase target, float predictionTime)
    {
        Vector3 currentPos = target.transform.position;
        float speed = enemyVelocity.magnitude;
    
        if (speed < 0.1f) return target.GetCenterPoint();
    
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
        
        Vector3 targetPos = PredictTargetPosition();
        if (targetPos == Vector3.zero) targetPos = targetToFace.GetCenterPoint();
        
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
        if (currentEnemy == null) return false;
    
        // Check if facing enemy (within 15 degrees)
        if (!IsFacingEnemy(15f)) return false;
    
        bool hasValidTracking = velocityFrameCount > 1;
        return base.CanAttack() && !isAttacking && hasValidTracking;
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
        PlayAttackSound();
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
        ClearLockedTarget();
    }
    
    private void FireSpear()
    {
        if (lockedDamageable == null)
        {
            ClearLockedTarget();
            return;
        }
        
        // Recalculate prediction if target still valid
        if (lockedTarget != null && lockedTarget.gameObject.activeSelf)
        {
            lockedTargetPosition = PredictTargetPosition();
        }

        Vector3 spawnPos = spearVisual.transform.position;
        Quaternion spawnRot = spearVisual.transform.rotation;

        Vector3 fireDirection = spawnRot * Vector3.up;
        spawnPos += fireDirection * forwardSpawnOffset;

        GameObject newSpear = ObjectPooling.instance.Get(projectilePrefab);
        newSpear.transform.position = spawnPos;
        newSpear.transform.rotation = spawnRot;
        newSpear.SetActive(true);
    
        SpearProjectile spear = newSpear.GetComponent<SpearProjectile>();

        spear.SetupSpear(lockedTargetPosition, lockedDamageable, CreateDamageInfo(), projectileSpeed);

        if (bleedSpear)
        {
            spear.SetBleedEffect(bleedDamage, bleedDuration, elementType, bleedSpearVFX);
        }

        if (explosiveTip)
        {
            spear.SetExplosiveEffect(explosionRadius, explosionDamage, elementType, whatIsEnemy, explosionVFX);
        }
    }
    
    private void ClearLockedTarget()
    {
        lockedTarget = null;
        lockedDamageable = null;
        lockedVelocity = Vector3.zero;
        lockedTargetPosition = Vector3.zero;
    }
}