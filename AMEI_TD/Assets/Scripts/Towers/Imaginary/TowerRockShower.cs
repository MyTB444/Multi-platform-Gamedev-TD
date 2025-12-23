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
    
    private bool isShowering = false;
    private bool isAttacking = false;
    private Vector3 enemyVelocity;
    private Vector3 lastEnemyPosition;
    
    protected override void FixedUpdate()
    {
        UpdateEnemyVelocity();
    
        if (!isShowering)
        {
            base.FixedUpdate();
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
        
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(attackAnimationTrigger);
        }
    }
    
    // Called by animation event
    public void OnStartRockShower()
    {
        StartCoroutine(RockShowerRoutine());
    }
    
    private IEnumerator RockShowerRoutine()
    {
        isShowering = true;

        float elapsed = 0f;

        while (elapsed < showerDuration)
        {
            if (currentEnemy != null)
            {
                SpawnRock();
            }
    
            yield return new WaitForSeconds(timeBetweenRocks);
            elapsed += timeBetweenRocks;
        }

        isShowering = false;
        isAttacking = false;
    }
    
    private void SpawnRock()
    {
        float randomSpeed = Random.Range(rockSpeedMin, rockSpeedMax);
        float fallTime = spawnHeight / randomSpeed;

        Vector3 predictedPos = GetPredictedPosition(fallTime);

        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = predictedPos + new Vector3(randomOffset.x, spawnHeight, randomOffset.y);

        if (attackSpawnEffectPrefab != null)
        {
            GameObject vfx = Instantiate(attackSpawnEffectPrefab, spawnPos, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        GameObject rock = Instantiate(rockPrefab, spawnPos, Random.rotation);

        float randomSize = Random.Range(rockSizeMin, rockSizeMax);
        rock.transform.localScale = rockPrefab.transform.localScale * randomSize;

        float scaledDamage = damage * randomSize;

        RockProjectile projectile = rock.GetComponent<RockProjectile>();
        projectile.Setup(scaledDamage, whatIsEnemy, randomSpeed, randomSize);
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