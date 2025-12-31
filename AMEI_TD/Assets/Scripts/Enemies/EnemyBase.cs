using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyType
{
    Basic,
    Fast,
    Tank,
    Invisible,
    Reinforced,
    Summoner,
    Minion,
    Hexer,
    Herald,
    Adaptive,
    Splitter,
    Ghostwalk,
    Decoy
}

public enum DebuffType
{
    None,
    Slow,
    Freeze,
    Burn,
    Poison,
    Bleed,
    Frostbite 
}

public class EnemyBase : MonoBehaviour, IDamageable
{
    protected EnemySpawner mySpawner;

    [Header("Enemy Stats")]
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private float enemySpeed;
    [SerializeField] private int damage;
    [SerializeField] private int reward;
    public float enemyMaxHp = 100;
    
    [Header("Element Type")]
    [SerializeField] private ElementType elementType;

    [Header("Ability")]
    [SerializeField] private bool isInvisible;
    [SerializeField] private bool isReinforced;

    [Header("Visuals")]
    [SerializeField] private Color enemyColor = Color.white;

    [Header("Path")]
    [SerializeField] private Transform centerPoint;
    [SerializeField] private Transform bottomPoint;
    
    [Header("Spawn Settings")]
    [SerializeField] private bool useSpawnGrace = true;
    [SerializeField] private float spawnGracePeriod = 0.5f;
    private float spawnTime;
    
    [Header("Stun Effect")]
    [SerializeField] private Color frozenColor = new Color(0.5f, 0.8f, 1f, 1f);
    private Renderer[] enemyRenderers;
    private Color[] originalColors;
    private bool hasSavedColors = false;

    private NavMeshAgent NavAgent;
    private Animator EnemyAnimator;

    [SerializeField] protected float enemyCurrentHp;
    protected bool isDead;

    protected Vector3[] myWaypoints;
    protected int currentWaypointIndex = 0;
    private float waypointReachDistance = 0.3f;

    private int originalLayerIndex;
    private bool hasStoredLayer = false;
    private float totalDistance;

    private Vector3 Destination;
    protected bool canMove = true;

    // Slow system
    private float baseSpeed;
    private float slowEndTime;
    private bool isSlowed = false;
    private bool isIceSlow = false;

    // Speed buff system
    private bool isSpeedBuffed = false;
    private float speedBuffEndTime;

    // HoT (Heal over Time) system
    private bool hasHoT = false;
    private float hotEndTime;
    private float hotTotalAmount;
    private float hotAmountPerTick;
    private float hotTickInterval;
    private float lastHotTick;

    // Stun system
    private bool isStunned = false;
    private float stunEndTime;

    // DoT system
    private bool hasBurn = false;
    private DamageInfo burnDamageInfo;
    private float burnEndTime;
    private float lastBurnTick;
    
    // Spread settings (for burn spread)
    private bool burnCanSpread = false;
    private float burnSpreadRadius;
    private LayerMask burnSpreadLayer;

    private bool hasPoison = false;
    private DamageInfo poisonDamageInfo;
    private float poisonEndTime;
    private float lastPoisonTick;

    private bool hasBleed = false;
    private DamageInfo bleedDamageInfo;
    private float bleedEndTime;
    private float lastBleedTick;
    
    private bool hasFrostbite = false;
    private DamageInfo frostbiteDamageInfo;
    private float frostbiteEndTime;
    private float lastFrostbiteTick;

    private float dotTickInterval = 0.5f;
    
    // Shield system
    private bool hasShield = false;
    private float shieldHealth = 0f;
    private GameObject activeShieldEffect;
    
    private void OnEnable()
    {
        UpdateVisuals();
        NavAgent = GetComponent<NavMeshAgent>();
        EnemyAnimator = GetComponentInChildren<Animator>();
    
        // Disable NavAgent until SetupEnemy is called
        if (NavAgent != null)
        {
            NavAgent.enabled = false;
        }
    }

    protected virtual void Awake()
    {
        baseSpeed = enemySpeed;
        originalLayerIndex = gameObject.layer;
    }

    protected virtual void Start()
    {
        UpdateVisuals();
        NavAgent = GetComponent<NavMeshAgent>();
        EnemyAnimator = GetComponentInChildren<Animator>();

        SaveOriginalColors();
        
        if (GetComponent<EnemyDebuffDisplay>() == null)
        {
            gameObject.AddComponent<EnemyDebuffDisplay>();
        }
    }

    protected virtual void Update()
    {
        UpdateStatusEffects();
        FollowPath();
        PlayAnimations();
    }

    private void UpdateStatusEffects()
    {
        // Handle stun expiry
        if (isStunned && Time.time >= stunEndTime)
        {
            RemoveStun();
        }
    
        // Handle slow expiry
        if (isSlowed && Time.time >= slowEndTime)
        {
            RemoveSlow();
        }

        // Handle speed buff expiry
        if (isSpeedBuffed && Time.time >= speedBuffEndTime)
        {
            RemoveSpeedBuff();
        }

        // Handle HoT
        if (hasHoT)
        {
            if (Time.time >= hotEndTime)
            {
                hasHoT = false;
            }
            else if (Time.time >= lastHotTick + hotTickInterval)
            {
                lastHotTick = Time.time;
                Heal(hotAmountPerTick);
            }
        }

        // Handle Burn DoT
        if (hasBurn)
        {
            if (Time.time >= burnEndTime)
            {
                hasBurn = false;
                burnCanSpread = false;
            }
            else if (Time.time >= lastBurnTick + dotTickInterval)
            {
                lastBurnTick = Time.time;
                TakeDamage(burnDamageInfo);

                if (burnCanSpread)
                {
                    SpreadBurn();
                }
            }
        }

        // Handle Poison DoT
        if (hasPoison)
        {
            if (Time.time >= poisonEndTime)
            {
                hasPoison = false;
            }
            else if (Time.time >= lastPoisonTick + dotTickInterval)
            {
                lastPoisonTick = Time.time;
                TakeDamage(poisonDamageInfo);
            }
        }

        // Handle Bleed DoT
        if (hasBleed)
        {
            if (Time.time >= bleedEndTime)
            {
                hasBleed = false;
            }
            else if (Time.time >= lastBleedTick + dotTickInterval)
            {
                lastBleedTick = Time.time;
                TakeDamage(bleedDamageInfo);
            }
        }
        
        // Handle Frostbite DoT
        if (hasFrostbite)
        {
            if (Time.time >= frostbiteEndTime)
            {
                hasFrostbite = false;
            }
            else if (Time.time >= lastFrostbiteTick + dotTickInterval)
            {
                lastFrostbiteTick = Time.time;
                TakeDamage(frostbiteDamageInfo);
            }
        }
    }
    
    private void SpreadBurn()
    {
        Vector3 spreadCenter = centerPoint != null ? centerPoint.position : transform.position;
        Collider[] nearbyEnemies = Physics.OverlapSphere(spreadCenter, burnSpreadRadius, burnSpreadLayer);

        foreach (Collider col in nearbyEnemies)
        {
            if (col.gameObject == gameObject) continue;
    
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null && !enemy.HasBurn())
            {
                enemy.ApplyDoT(burnDamageInfo, burnEndTime - Time.time, dotTickInterval, false, 0f, default, DebuffType.Burn);
            }
        }
    }

    public void ApplySlow(float slowPercent, float duration, bool fromIce = false)
    {
        isSlowed = true;
        slowEndTime = Time.time + duration;
        isIceSlow = fromIce;

        float clampedSlow = Mathf.Clamp(slowPercent, 0f, 0.9f);
        enemySpeed = baseSpeed * (1f - clampedSlow);
    }

    private void RemoveSlow()
    {
        isSlowed = false;
        isIceSlow = false;
        enemySpeed = baseSpeed;
    }

    public void ApplySpeedBuff(float speedMultiplier, float duration)
    {
        isSpeedBuffed = true;
        speedBuffEndTime = Time.time + duration;

        float clampedMultiplier = Mathf.Clamp(speedMultiplier, 1f, 3f);
        enemySpeed = baseSpeed * clampedMultiplier;
    }

    private void RemoveSpeedBuff()
    {
        isSpeedBuffed = false;
        enemySpeed = baseSpeed;
    }

    public void ApplyHoT(float percentOfMaxHp, float duration, float tickInterval = 0.5f)
    {
        hasHoT = true;
        hotEndTime = Time.time + duration;
        hotTickInterval = tickInterval;
        lastHotTick = Time.time;

        float totalHeal = enemyMaxHp * percentOfMaxHp;
        int tickCount = Mathf.CeilToInt(duration / tickInterval);
        hotAmountPerTick = totalHeal / tickCount;
    }

    private void Heal(float amount)
    {
        enemyCurrentHp += amount;
        if (enemyCurrentHp > enemyMaxHp)
        {
            enemyCurrentHp = enemyMaxHp;
        }
        enemyCurrentHp = Mathf.Floor(enemyCurrentHp);
    }

    public void ApplyStun(float duration)
    {
        isStunned = true;
        stunEndTime = Time.time + duration;
    
        if (hasSavedColors)
        {
            foreach (Renderer r in enemyRenderers)
            {
                r.material.color = frozenColor;
            }
        }
    }
    
    private void RemoveStun()
    {
        isStunned = false;
    
        if (hasSavedColors)
        {
            for (int i = 0; i < enemyRenderers.Length; i++)
            {
                enemyRenderers[i].material.color = originalColors[i];
            }
        }
    }
    
    private void SaveOriginalColors()
    {
        enemyRenderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[enemyRenderers.Length];
    
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            originalColors[i] = enemyRenderers[i].material.color;
        }
    
        hasSavedColors = true;
    }

    public void ApplyDoT(DamageInfo damagePerTick, float duration, float tickInterval = 0.5f, bool canSpread = false, float spreadRadius = 0f, LayerMask spreadLayer = default, DebuffType dotType = DebuffType.None)
    {
        DamageCalculator.DamageResult testResult = DamageCalculator.Calculate(damagePerTick, elementType);

        if (testResult.wasImmune)
        {
            return;
        }

        dotTickInterval = tickInterval;

        switch (dotType)
        {
            case DebuffType.Burn:
                hasBurn = true;
                burnDamageInfo = damagePerTick;
                burnDamageInfo.isDoT = true;
                burnEndTime = Time.time + duration;
                lastBurnTick = Time.time;
                burnCanSpread = canSpread;
                burnSpreadRadius = spreadRadius;
                burnSpreadLayer = spreadLayer;
                break;
            
            case DebuffType.Poison:
                hasPoison = true;
                poisonDamageInfo = damagePerTick;
                poisonDamageInfo.isDoT = true;
                poisonEndTime = Time.time + duration;
                lastPoisonTick = Time.time;
                break;
            
            case DebuffType.Bleed:
                hasBleed = true;
                bleedDamageInfo = damagePerTick;
                bleedDamageInfo.isDoT = true;
                bleedEndTime = Time.time + duration;
                lastBleedTick = Time.time;
                break;
            
            case DebuffType.Frostbite:
                hasFrostbite = true;
                frostbiteDamageInfo = damagePerTick;
                frostbiteDamageInfo.isDoT = true;
                frostbiteEndTime = Time.time + duration;
                lastFrostbiteTick = Time.time;
                break;
        }
    }

    public void SetupEnemy(EnemySpawner myNewSpawner, Vector3[] pathWaypoints)
    {
        mySpawner = myNewSpawner;
        spawnTime = Time.time; // Reset spawn time for grace period
        UpdateWaypoints(pathWaypoints);
        CollectTotalDistance();
        ResetEnemy();
        BeginMovement();
    }
    
    // For minions/summons that spawn without a spawner
    public void SetupEnemyNoGrace(Vector3[] pathWaypoints)
    {
        spawnTime = -spawnGracePeriod; // Skip grace period
        UpdateWaypoints(pathWaypoints);
        CollectTotalDistance();
        ResetEnemy();
        BeginMovement();
    }
    
    public bool IsTargetable()
    {
        if (!useSpawnGrace) return true;
        return Time.time > spawnTime + spawnGracePeriod;
    }

    private void UpdateWaypoints(Vector3[] newWaypoints)
    {
        myWaypoints = new Vector3[newWaypoints.Length];
        for (int i = 0; i < myWaypoints.Length; i++)
        {
            myWaypoints[i] = newWaypoints[i];
        }
    }

    private void BeginMovement()
    {
        currentWaypointIndex = 0;

        if (NavAgent != null)
        {
            NavAgent.enabled = true;
        
            // Find a valid NavMesh position near spawn point
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
            {
                NavAgent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Could not find NavMesh near {transform.position}");
                NavAgent.Warp(transform.position);
            }
        
            NavAgent.speed = enemySpeed;
            NavAgent.isStopped = false;
            NavAgent.updateRotation = false;
        
            // Set first destination immediately so it starts moving right away
            if (myWaypoints != null && myWaypoints.Length > 0)
            {
                NavAgent.SetDestination(myWaypoints[0]);
            }
        }
    }

    private void CollectTotalDistance()
    {
        totalDistance = 0;
        for (int i = 0; i < myWaypoints.Length; i++)
        {
            if (i == myWaypoints.Length - 1) break;
            float distance = Vector3.Distance(myWaypoints[i], myWaypoints[i + 1]);
            totalDistance += distance;
        }
    }

    private void FollowPath()
    {
        if (myWaypoints == null || currentWaypointIndex >= myWaypoints.Length) return;

        if (NavAgent == null || !NavAgent.isActiveAndEnabled) return;
    
        // Must be on NavMesh to do anything
        if (!NavAgent.isOnNavMesh) return;

        if (isStunned || !canMove)
        {
            NavAgent.isStopped = true;
            return;
        }
    
        NavAgent.isStopped = false;
        NavAgent.speed = enemySpeed;

        // Skip waypoints we've already passed
        while (currentWaypointIndex < myWaypoints.Length && HasPassedWaypoint(currentWaypointIndex))
        {
            currentWaypointIndex++;
        }

        if (currentWaypointIndex >= myWaypoints.Length) return;

        Vector3 targetWaypoint = myWaypoints[currentWaypointIndex];
        NavAgent.SetDestination(targetWaypoint);

        // Smooth rotation towards steering target
        Vector3 direction = NavAgent.steeringTarget - transform.position;
        direction.y = 0;
    
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        if (!NavAgent.pathPending && NavAgent.remainingDistance <= waypointReachDistance)
        {
            currentWaypointIndex++;
        }
    }

    private bool HasPassedWaypoint(int waypointIndex)
    {
        if (waypointIndex >= myWaypoints.Length) return false;
        
        Vector3 waypoint = myWaypoints[waypointIndex];
        
        // Get direction the path is going at this waypoint
        Vector3 pathDirection;
        if (waypointIndex < myWaypoints.Length - 1)
        {
            pathDirection = (myWaypoints[waypointIndex + 1] - waypoint).normalized;
        }
        else
        {
            // Last waypoint - use direction from previous
            if (waypointIndex > 0)
            {
                pathDirection = (waypoint - myWaypoints[waypointIndex - 1]).normalized;
            }
            else
            {
                return false; // Only one waypoint, can't determine
            }
        }
        
        // Check if enemy is past the waypoint (dot product positive means we've passed it)
        Vector3 toEnemy = transform.position - waypoint;
        toEnemy.y = 0;
        pathDirection.y = 0;
        
        return Vector3.Dot(toEnemy, pathDirection) > 0;
    }
    
    private void PlayAnimations()
    {
        if (EnemyAnimator == null)
        {
            Debug.Log($"{gameObject.name}: Animator is NULL");
            return;
        }

        if (isStunned)
        {
            EnemyAnimator.SetBool("Walk", false);
            return;
        }

        Debug.Log($"{gameObject.name}: Setting Walk to true");
        EnemyAnimator.SetBool("Walk", true);
    }

    public float GetRemainingDistance()
    {
        if (myWaypoints == null || currentWaypointIndex >= myWaypoints.Length) return 0;

        float remainingDistance = 0;
        remainingDistance += Vector3.Distance(transform.position, myWaypoints[currentWaypointIndex]);

        for (int i = currentWaypointIndex; i < myWaypoints.Length - 1; i++)
        {
            remainingDistance += Vector3.Distance(myWaypoints[i], myWaypoints[i + 1]);
        }
        return remainingDistance;
    }

    private void ReachedEnd()
    {
        Destroy(gameObject);
    }

    public virtual void TakeDamage(DamageInfo damageInfo)
    {
        DamageCalculator.DamageResult result = DamageCalculator.Calculate(damageInfo, elementType);

        if (DamageNumberSpawner.instance != null)
        {
            Vector3 spawnPos = centerPoint != null ? centerPoint.position : transform.position;
            DamageNumberSpawner.instance.Spawn(
                spawnPos,
                result.finalDamage,
                result.wasSuperEffective,
                result.wasNotVeryEffective,
                result.wasImmune
            );
        }

        if (result.wasImmune)
        {
            Debug.Log($"IMMUNE! {damageInfo.elementType} vs {elementType}");
            return;
        }

        if (result.wasNotVeryEffective)
        {
            Debug.Log($"Not very effective... {damageInfo.elementType} vs {elementType} = {result.finalDamage}");
        }
        else if (result.wasSuperEffective)
        {
            Debug.Log($"Super effective! {damageInfo.elementType} vs {elementType} = {result.finalDamage}");
        }

        Debug.Log($"DamageInfo: {damageInfo.elementType} vs {elementType} = {result.finalDamage}");

        float damageToApply = result.finalDamage;
        if (hasShield && shieldHealth > 0)
        {
            if (shieldHealth >= damageToApply)
            {
                shieldHealth -= damageToApply;
                damageToApply = 0;
                Debug.Log($"Shield absorbed all damage. Shield remaining: {shieldHealth}");
            }
            else
            {
                damageToApply -= shieldHealth;
                Debug.Log($"Shield broken");
                shieldHealth = 0;
                RemoveShield();
            }
        }

        enemyCurrentHp -= damageToApply;

        if (enemyCurrentHp <= 0 && !isDead)
        {
            isDead = true;
            Die();
        }
    }

    public virtual void TakeDamage(float incomingDamage)
    {
        TakeDamage(new DamageInfo(incomingDamage, ElementType.Physical));
    }

    private void Die()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.UpdateSkillPoints(reward);
        }
        RemoveEnemy();
        ObjectPooling.instance.Return(gameObject);
    }
    
    public void RemoveEnemy()
    {
        if (mySpawner != null) mySpawner.RemoveActiveEnemy(gameObject);
    }

    protected virtual void ResetEnemy()
    {
        // Restore layer first
        gameObject.layer = originalLayerIndex;
    
        float healthMultiplier = 1f;
        if (WaveManager.instance != null && WaveManager.instance.IsMegaWaveActive())
        {
            healthMultiplier = WaveManager.instance.GetMegaWaveHealthMultiplier();
        }
        enemyCurrentHp = enemyMaxHp * healthMultiplier;
        
        isDead = false;
        canMove = true;
        currentWaypointIndex = 0;

        // Reset slow
        isSlowed = false;
        isIceSlow = false;
        enemySpeed = baseSpeed;

        // Reset DoT
        hasBurn = false;
        hasPoison = false;
        hasBleed = false;
        hasFrostbite = false;
        burnCanSpread = false;
        // Reset Speed Buff
        isSpeedBuffed = false;

        // Reset HoT
        hasHoT = false;

        // Reset Stun
        isStunned = false;
        if (hasSavedColors)
        {
            for (int i = 0; i < enemyRenderers.Length; i++)
            {
                if (enemyRenderers[i] != null)
                {
                    enemyRenderers[i].material.color = originalColors[i];
                }
            }
        }

        // Reset Shield
        if (hasShield)
        {
            RemoveShield();
        }

        // Reset NavAgent
        if (NavAgent != null)
        {
            NavAgent.enabled = false;
            NavAgent.speed = baseSpeed;
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        Renderer r = GetComponent<Renderer>();
        if (r == null) return;

        Color finalColor = enemyColor;

        if (isInvisible)
        {
            finalColor.a = 0.3f;
        }
        else
        {
            finalColor.a = 1f;
        }
        r.material.color = finalColor;
    }
    
    public void ApplyShield(float shieldAmount, GameObject shieldEffectPrefab)
    {
        hasShield = true;
        shieldHealth = shieldAmount;

        if (shieldEffectPrefab != null && activeShieldEffect == null)
        {
            activeShieldEffect = Instantiate(shieldEffectPrefab, transform.position, Quaternion.identity, transform);
        }

        Debug.Log($"Shield applied: {shieldAmount} HP");
    }

    private void RemoveShield()
    {
        hasShield = false;
        shieldHealth = 0f;

        if (activeShieldEffect != null)
        {
            Destroy(activeShieldEffect);
            activeShieldEffect = null;
        }

        Debug.Log("Shield removed");
    }

    // Getters
    public Vector3 GetCenterPoint() => centerPoint.position;
    public EnemyType GetEnemyType() => enemyType;
    public float GetEnemyHp() => enemyCurrentHp;
    public Transform GetBottomPoint() => bottomPoint;
    public bool IsInvisible() => isInvisible;
    public bool IsReinforced() => isReinforced;
    public int GetDamage() => damage;
    public bool IsSlowed() => isSlowed;
    public bool IsStunned() => isStunned;
    public bool IsSpeedBuffed() => isSpeedBuffed;
    public bool HasHoT() => hasHoT;
    public ElementType GetElementType() => elementType;
    public bool IsFrozen() => isStunned;  // Freeze uses stun system
    public bool HasIceSlow() => isSlowed && isIceSlow;
    public bool HasBurn() => hasBurn;
    public bool HasPoison() => hasPoison;
    public bool HasBleed() => hasBleed;
    public bool HasFrostbite() => hasFrostbite;
    public bool HasDoT() => hasBurn || hasPoison || hasBleed || hasFrostbite;
    public bool HasShield() => hasShield && shieldHealth > 0;
    public float GetShieldHealth() => shieldHealth;
    public float GetSlowDurationNormalized(float maxDuration = 3f) => isSlowed ? Mathf.Clamp01((slowEndTime - Time.time) / maxDuration) : 0f;
    public float GetFreezeDurationNormalized(float maxDuration = 2f) => isStunned ? Mathf.Clamp01((stunEndTime - Time.time) / maxDuration) : 0f;
    public float GetBurnDurationNormalized(float maxDuration = 4f) => hasBurn ? Mathf.Clamp01((burnEndTime - Time.time) / maxDuration) : 0f;
    public float GetPoisonDurationNormalized(float maxDuration = 4f) => hasPoison ? Mathf.Clamp01((poisonEndTime - Time.time) / maxDuration) : 0f;
    public float GetBleedDurationNormalized(float maxDuration = 4f) => hasBleed ? Mathf.Clamp01((bleedEndTime - Time.time) / maxDuration) : 0f;
    public float GetFrostbiteDurationNormalized(float maxDuration = 3f) => hasFrostbite ? Mathf.Clamp01((frostbiteEndTime - Time.time) / maxDuration) : 0f;
    public float GetDistanceToNextWaypoint()
    {
        if (myWaypoints == null || currentWaypointIndex >= myWaypoints.Length) return 0f;
        return Vector3.Distance(transform.position, myWaypoints[currentWaypointIndex]);
    }

    public Vector3 GetNextWaypointPosition()
    {
        if (myWaypoints == null || currentWaypointIndex >= myWaypoints.Length) return transform.position;
        return myWaypoints[currentWaypointIndex];
    }

    public Vector3 GetDirectionAfterNextWaypoint()
    {
        if (myWaypoints == null) return transform.forward;
    
        // Need at least 2 more waypoints to know direction after turn
        if (currentWaypointIndex + 1 >= myWaypoints.Length) return transform.forward;
    
        Vector3 currentWaypoint = myWaypoints[currentWaypointIndex];
        Vector3 nextWaypoint = myWaypoints[currentWaypointIndex + 1];
    
        return (nextWaypoint - currentWaypoint).normalized;
    }
}