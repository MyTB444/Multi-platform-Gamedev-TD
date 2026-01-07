using System.Collections;
using UnityEngine;

public class TowerRockShower : TowerBase
{
    [Header("Rock Shower")]
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private float spawnHeight = 10f;
    [SerializeField] private float spawnRadius = 1.5f;
    
    [Header("Shower Settings")]
    [SerializeField] private float showerDuration = 3f;
    [SerializeField] private float timeBetweenRocks = 0.2f;
    [SerializeField] private float rockSizeMin = 0.5f;
    [SerializeField] private float rockSizeMax = 1.5f;
    [SerializeField] private float rockSpeedMin = 8f;
    [SerializeField] private float rockSpeedMax = 15f;
    
    [Header("Prediction")]
    [SerializeField] private float predictionMultiplier = 0.5f;
    
    [Header("Pooling")]
    [SerializeField] private int rockPoolAmount = 30;
    
    [Header("Rock Shower Upgrades")]
    [SerializeField] private bool moreRocks = false;
    [SerializeField] private int bonusRocks = 3;
    [Space]
    [SerializeField] private bool biggerRocks = false;
    [SerializeField] private float rockSizeBonus = 0.3f;
    [Space]
    [SerializeField] private bool longerShower = false;
    [SerializeField] private float bonusShowerDuration = 1f;
    [Space]
    [SerializeField] private bool meteorStrike = false;
    [SerializeField] private GameObject meteorPrefab;
    [SerializeField] private float meteorSize = 3f;
    [SerializeField] private float meteorHeight = 3f;
    [SerializeField] private float meteorDamageMultiplier = 3f;
    [SerializeField] private float meteorSpeed = 4f;
    
    private bool isShowering = false;
    private bool isAttacking = false;
    private Vector3 enemyVelocity;
    private Vector3 lastEnemyPosition;
    private Vector3 lockedTargetPosition;
    
    protected override void Start()
    {
        base.Start();

        if (rockPrefab != null)
        {
            ObjectPooling.instance.Register(rockPrefab, rockPoolAmount);
        }
    
        if (meteorPrefab != null)
        {
            ObjectPooling.instance.Register(meteorPrefab, 3);
        }
    }
    
    protected override void FixedUpdate()
    {
        UpdateDebuffs();
        UpdateDisabledVisual();
    
        if (isDisabled) return;
    
        UpdateEnemyVelocity();

        if (!isShowering)
        {
            ClearTargetOutOfRange();
            UpdateTarget();
            HandleRotation();

            if (CanAttack()) AttemptToAttack();
        }
        else
        {
            if (currentEnemy != null && !currentEnemy.gameObject.activeSelf)
            {
                currentEnemy = null;
            }
    
            HandleRotation();
        }
    }
    
    public override void SetUpgrade(TowerUpgradeType upgradeType, bool enabled)
    {
        base.SetUpgrade(upgradeType, enabled);
    
        switch (upgradeType)
        {
            case TowerUpgradeType.MoreRocks:
                moreRocks = enabled;
                break;
            case TowerUpgradeType.BiggerRocks:
                biggerRocks = enabled;
                break;
            case TowerUpgradeType.LongerShower:
                longerShower = enabled;
                break;
            case TowerUpgradeType.MeteorStrike:
                meteorStrike = enabled;
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
    
    private Vector3 GetPredictedPosition(float fallTime)
    {
        if (currentEnemy == null) return Vector3.zero;
        
        Vector3 currentPos = currentEnemy.transform.position;
        return currentPos + (enemyVelocity * fallTime * predictionMultiplier);
    }
    
    protected override void Attack()
    {
        if (isShowering || isAttacking) return;
        
        lastTimeAttacked = Time.time;
        isAttacking = true;
        
        if (currentEnemy != null)
        {
            float avgSpeed = (rockSpeedMin + rockSpeedMax) / 2f;
            float fallTime = spawnHeight / avgSpeed;
            lockedTargetPosition = GetPredictedPosition(fallTime);
            
            if (lockedTargetPosition == Vector3.zero)
            {
                lockedTargetPosition = currentEnemy.transform.position;
            }
        }
        
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(attackAnimationTrigger);
        }
    }
    
    public void OnStartRockShower()
    {
        PlayAttackSound();
        StartCoroutine(RockShowerRoutine());
    }
    
    private IEnumerator RockShowerRoutine()
    {
        isShowering = true;

        float elapsed = 0f;
        float finalDuration = longerShower ? showerDuration + bonusShowerDuration : showerDuration;

        float finalTimeBetweenRocks = moreRocks ? 
            finalDuration / ((finalDuration / timeBetweenRocks) + bonusRocks) : 
            timeBetweenRocks;

        while (elapsed < finalDuration)
        {
            if (!isDisabled)
            {
                SpawnRock(lockedTargetPosition);
            }

            yield return new WaitForSeconds(finalTimeBetweenRocks);
            elapsed += finalTimeBetweenRocks;
        }

        if (meteorStrike && !isDisabled)
        {
            SpawnMeteor(lockedTargetPosition);
        }

        isShowering = false;
        isAttacking = false;
    }
    
    private void SpawnRock(Vector3 targetPosition)
    {
        float randomSpeed = Random.Range(rockSpeedMin, rockSpeedMax);

        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = targetPosition + new Vector3(randomOffset.x, spawnHeight, randomOffset.y);

        if (attackSpawnEffectPrefab != null)
        {
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, spawnPos, Quaternion.identity, 2f);
        }

        GameObject rock = ObjectPooling.instance.Get(rockPrefab);
        rock.transform.position = spawnPos;
        rock.transform.rotation = Random.rotation;
        rock.SetActive(true);

        float sizeMultiplier = biggerRocks ? 1f + rockSizeBonus : 1f;
        float randomSize = Random.Range(rockSizeMin, rockSizeMax) * sizeMultiplier;
        rock.transform.localScale = rockPrefab.transform.localScale * randomSize;

        float scaledDamage = damage * randomSize;
        DamageInfo rockDamageInfo = new DamageInfo(scaledDamage, elementType);

        RockProjectile projectile = rock.GetComponent<RockProjectile>();
        projectile.Setup(rockDamageInfo, whatIsEnemy, randomSpeed, randomSize);
    }
    
    private void SpawnMeteor(Vector3 fallbackPosition)
    {
        Vector3 targetPosition = fallbackPosition;

        Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange, whatIsEnemy);

        if (enemies.Length > 0)
        {
            float closestDist = float.MaxValue;
            Transform closestEnemy = null;

            foreach (Collider col in enemies)
            {
                if (!col.gameObject.activeSelf) continue;

                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestEnemy = col.transform;
                }
            }

            if (closestEnemy != null)
            {
                targetPosition = closestEnemy.position;
            }
        }

        Vector3 spawnPos = targetPosition + Vector3.up * (spawnHeight + meteorHeight);

        if (attackSpawnEffectPrefab != null)
        {
            Vector3 scaledSize = attackSpawnEffectPrefab.transform.localScale * 2f;
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, spawnPos, Quaternion.identity, scaledSize, 2f);
        }
        
        GameObject prefabToUse = meteorPrefab != null ? meteorPrefab : rockPrefab;
    
        GameObject meteor = ObjectPooling.instance.Get(prefabToUse);
        meteor.transform.position = spawnPos;
        meteor.transform.rotation = Random.rotation;
        meteor.SetActive(true);
        meteor.transform.localScale = prefabToUse.transform.localScale * meteorSize;

        float scaledDamage = damage * meteorSize * meteorDamageMultiplier;
        DamageInfo meteorDamageInfo = new DamageInfo(scaledDamage, elementType);

        RockProjectile projectile = meteor.GetComponent<RockProjectile>();
        projectile.Setup(meteorDamageInfo, whatIsEnemy, meteorSpeed, meteorSize);
        projectile.SetAsMeteor();
    }
    
    protected override bool CanAttack()
    {
        return base.CanAttack() && !isShowering && !isAttacking;
    }
    
    protected override void HandleRotation()
    {
        if (currentEnemy == null || towerBody == null) return;
    
        Vector3 direction = currentEnemy.transform.position - towerBody.position;
        direction.y = 0;
    
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            towerBody.rotation = Quaternion.Slerp(towerBody.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (currentEnemy != null)
        {
            float avgSpeed = (rockSpeedMin + rockSpeedMax) / 2f;
            float fallTime = spawnHeight / avgSpeed;
            Vector3 predicted = GetPredictedPosition(fallTime);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(predicted, spawnRadius);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(predicted, predicted + Vector3.up * spawnHeight);
            Gizmos.DrawWireSphere(predicted + Vector3.up * spawnHeight, 0.5f);
        }
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}