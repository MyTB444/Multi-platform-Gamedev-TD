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

    [Header("Slow Effect")]
    [SerializeField] private GameObject slowEffectPrefab;
    
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
    private GameObject activeSlowEffect;

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
    private DamageInfo dotDamageInfo;
    private float dotEndTime;
    private float dotTickInterval = 0.5f;
    private float lastDotTick;
    private bool hasDot = false;
    private bool dotCanSpread = false;
    private float dotSpreadRadius;
    private LayerMask dotSpreadLayer;
    
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

        // Handle DoT
        if (hasDot)
        {
            if (Time.time >= dotEndTime)
            {
                hasDot = false;
                dotCanSpread = false;
            }
            else if (Time.time >= lastDotTick + dotTickInterval)
            {
                lastDotTick = Time.time;
                TakeDamage(dotDamageInfo);
    
                if (dotCanSpread)
                {
                    SpreadDoT();
                }
            }
        }
    }
    
    private void SpreadDoT()
    {
        Vector3 spreadCenter = centerPoint != null ? centerPoint.position : transform.position;
        Collider[] nearbyEnemies = Physics.OverlapSphere(spreadCenter, dotSpreadRadius, dotSpreadLayer);
    
        foreach (Collider col in nearbyEnemies)
        {
            if (col.gameObject == gameObject) continue;
        
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null && !enemy.HasDoT())
            {
                enemy.ApplyDoT(dotDamageInfo, dotEndTime - Time.time, dotTickInterval, false, 0f);
            }
        }
    }

    public void ApplySlow(float slowPercent, float duration, bool showVFX = true)
    {
        isSlowed = true;
        slowEndTime = Time.time + duration;

        float clampedSlow = Mathf.Clamp(slowPercent, 0f, 0.9f);
        enemySpeed = baseSpeed * (1f - clampedSlow);

        if (showVFX && activeSlowEffect == null && slowEffectPrefab != null)
        {
            activeSlowEffect = Instantiate(slowEffectPrefab, transform);
            activeSlowEffect.transform.localPosition = Vector3.up * 0.5f;
            Debug.Log($"Spawned slow effect: {activeSlowEffect.name}");
        }
    }

    private void RemoveSlow()
    {
        isSlowed = false;
        enemySpeed = baseSpeed;

        if (activeSlowEffect != null)
        {
            Destroy(activeSlowEffect);
            activeSlowEffect = null;
        }
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

    public void ApplyDoT(DamageInfo damagePerTick, float duration, float tickInterval = 0.5f, bool canSpread = false, float spreadRadius = 0f, LayerMask spreadLayer = default)
    {
        DamageCalculator.DamageResult testResult = DamageCalculator.Calculate(damagePerTick, elementType);
    
        if (testResult.wasImmune)
        {
            return;
        }
    
        hasDot = true;
        dotDamageInfo = damagePerTick;
        dotDamageInfo.isDoT = true;
        dotEndTime = Time.time + duration;
        dotTickInterval = tickInterval;
        lastDotTick = Time.time;
    
        // Spread settings
        dotCanSpread = canSpread;
        dotSpreadRadius = spreadRadius;
        dotSpreadLayer = spreadLayer;
    }

    public void SetupEnemy(EnemySpawner myNewSpawner, Vector3[] pathWaypoints)
    {
        mySpawner = myNewSpawner;
        UpdateWaypoints(pathWaypoints);
        CollectTotalDistance();
        ResetEnemy();
        BeginMovement();
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
    
        enemyCurrentHp = enemyMaxHp;
        isDead = false;
        canMove = true;
        currentWaypointIndex = 0;

        // Reset slow
        isSlowed = false;
        enemySpeed = baseSpeed;
        if (activeSlowEffect != null)
        {
            Destroy(activeSlowEffect);
            activeSlowEffect = null;
        }

        // Reset DoT
        hasDot = false;
        dotCanSpread = false;

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
    public bool HasDoT() => hasDot;
    public bool IsSpeedBuffed() => isSpeedBuffed;
    public bool HasHoT() => hasHoT;
    public ElementType GetElementType() => elementType;

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

    public bool HasShield() => hasShield && shieldHealth > 0;
    public float GetShieldHealth() => shieldHealth;
}