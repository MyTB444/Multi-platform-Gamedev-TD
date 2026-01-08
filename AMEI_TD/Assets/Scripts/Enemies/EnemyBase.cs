using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

public class EnemyBase : MonoBehaviour, IDamageable, IPointerEnterHandler, IPointerExitHandler
{
    public EnemySpawner mySpawner;

    [Header("Enemy Stats")]
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private float enemySpeed;
    [SerializeField] private int damage;
    [SerializeField] private int reward;
    public float enemyMaxHp = 100;
    
    [Header("Element Type")]
    [SerializeField] private ElementType elementType;

    [Header("Ability")]
    public bool isInvisible;
    [SerializeField] private bool isReinforced;

    [Header("Visuals")]
    [SerializeField] private Color enemyColor = Color.white;

    [Header("Path")]
    [SerializeField] private Transform centerPoint;
    [SerializeField] private Transform bottomPoint;
    
    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float angularSpeed = 360f;
    [SerializeField] private float animationSpeedMultiplier = 1f;
    
    [Header("Spawn Settings")]
    [SerializeField] private bool useSpawnGrace = true;
    [SerializeField] private float spawnGracePeriod = 0.5f;
    private float spawnTime;
    
    [Header("Path Variation")]
    [Tooltip("Random lateral offset from path center (0 = always center, 0.5 = spread across road)")]
    [SerializeField] private float pathOffsetRange = 1f;

    [Tooltip("Random speed variation (0.1 = ±10% speed variation)")]
    [SerializeField] private float speedVariationPercent = 0.1f;
    [SerializeField] private bool isHighPriority = false;
    [Tooltip("How close enemy must get to waypoint before moving to next (lower = tighter corners)")]
    [SerializeField] private float waypointReachThreshold = 0.3f;

    [Tooltip("Variation in corner cutting (0.2 = ±20% threshold variation)")]
    [SerializeField] private float cornerCutVariation = 0.8f;

    [Tooltip("How much path offset changes per waypoint (0 = consistent, 0.3 = drifts across path)")]
    [SerializeField] private float pathDriftAmount = 0.3f;

    private float myPathOffset;
    private float mySpeedMultiplier;
    private float myCornerCutMultiplier;
    private float myPathDrift;
    
    [Header("Stun Effect")]
    [SerializeField] private Color frozenColor = new Color(0.5f, 0.8f, 1f, 1f);
    private Renderer[] enemyRenderers;
    public Color[] originalColors;
    private bool hasSavedColors = false;
    
    [Header("Stuck Detection")]
    [SerializeField] private float stuckCheckInterval = 0.5f;
    [SerializeField] private float stuckDistanceThreshold = 0.2f;
    [SerializeField] private float stuckSkipDistance = 2f;

    private Vector3 lastStuckCheckPosition;
    private float lastStuckCheckTime;
    private float stuckDuration;

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
    [HideInInspector]public Rigidbody myBody;

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
    
    private VFXDamage vfxDamageScriptRef;

    [Header("Health Bar Display")]
    [SerializeField] private Canvas enemyHealthDisplayCanvas;
    [SerializeField] private Slider healthBar;
    [SerializeField] private float healthBarDisplayDuration = 3f;
    private Coroutine hideHealthBarCoroutine;

    [SerializeField] private EnemyVFXPool enemyVFXPoolScriptRef;
    private bool spellsActivated = false;
    
    protected virtual void OnEnable()
    {
        UpdateVisuals();
        NavAgent = GetComponent<NavMeshAgent>();
        EnemyAnimator = GetComponentInChildren<Animator>();
    
        if (NavAgent != null)
        {
            NavAgent.enabled = false;
        }
        
        
        if(vfxDamageScriptRef != null)
        {
            vfxDamageScriptRef.GetAffectedEnemyList().Remove(this);
        }
        
        enemyHealthDisplayCanvas.enabled = false;
        
        vfxContainer = enemyVFXPoolScriptRef;
        vfxContainer.PoolVFXGameObjects();

        enemyHealthDisplayCanvas.worldCamera = Camera.main;
        healthBar.value = 1;
        myBody = GetComponent<Rigidbody>();
        EnemyAnimator.enabled = true;
        spellsActivated = false;
        myBody.useGravity = true;
        myBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void OnDisable()
    {
        if (hideHealthBarCoroutine != null)
        {
            StopCoroutine(hideHealthBarCoroutine);
            hideHealthBarCoroutine = null;
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
    
    public void GetRefOfVfxDamageScript(VFXDamage vfxDamageScriptRef)
    {
        this.vfxDamageScriptRef = vfxDamageScriptRef;
        this.vfxDamageScriptRef.stopFlames = false;
    }

    protected virtual void Update()
    {
        UpdateStatusEffects();
           
        FollowPath();           
       
        PlayAnimations();
    }

    private void UpdateStatusEffects()
    {
        if (isStunned && Time.time >= stunEndTime)
        {
            RemoveStun();
        }
    
        if (isSlowed && Time.time >= slowEndTime)
        {
            RemoveSlow();
        }

        if (isSpeedBuffed && Time.time >= speedBuffEndTime)
        {
            RemoveSpeedBuff();
        }

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
        healthBar.value = enemyCurrentHp / enemyMaxHp;
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
        spawnTime = Time.time;

        myPathOffset = Random.Range(-pathOffsetRange, pathOffsetRange);
        mySpeedMultiplier = Random.Range(1f - speedVariationPercent, 1f + speedVariationPercent);
        myCornerCutMultiplier = Random.Range(1f - cornerCutVariation, 1f + cornerCutVariation);
        myPathDrift = Random.Range(-pathDriftAmount, pathDriftAmount);

        UpdateWaypoints(pathWaypoints);
        CollectTotalDistance();
        ResetEnemy();
        BeginMovement();
    }
    
    public void SetupEnemyNoGrace(Vector3[] pathWaypoints)
    {
        spawnTime = -spawnGracePeriod;
    
        myPathOffset = Random.Range(-pathOffsetRange, pathOffsetRange);
        mySpeedMultiplier = Random.Range(1f - speedVariationPercent, 1f + speedVariationPercent);
        myCornerCutMultiplier = Random.Range(1f - cornerCutVariation, 1f + cornerCutVariation);
        myPathDrift = Random.Range(-pathDriftAmount, pathDriftAmount);
    
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
        
        if (myBody != null)
        {
            myBody.isKinematic = true;
        }
        
        lastStuckCheckPosition = transform.position;
        lastStuckCheckTime = Time.time;
        stuckDuration = 0f;

        if (NavAgent != null)
        {
            NavAgent.enabled = true;
            
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

            float speedFactor = Mathf.Max(1f, enemySpeed / 3f);

            NavAgent.speed = enemySpeed * mySpeedMultiplier;
            NavAgent.acceleration = acceleration * speedFactor;
            NavAgent.angularSpeed = angularSpeed * speedFactor;
            
            NavAgent.isStopped = false;
            NavAgent.updateRotation = false;

            if (isHighPriority)
            {
                NavAgent.avoidancePriority = 1;
                NavAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            }
            else if (enemySpeed > 2.5f)
            {
                NavAgent.avoidancePriority = 80;
                NavAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            }
            else
            {
                NavAgent.avoidancePriority = Random.Range(30, 70);
            }

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
        
        if (this is EnemySplitter)
        {
            Debug.Log($"[{gameObject.name}] Index: {currentWaypointIndex}, Total waypoints: {myWaypoints.Length}, Target: {myWaypoints[currentWaypointIndex]}, MyPos: {transform.position}");
        }


        if (NavAgent == null || !NavAgent.isActiveAndEnabled) return;

        if (!NavAgent.isOnNavMesh) return;
        if (NavAgent.enabled)
        {
            if (isStunned || !canMove)
            {
                NavAgent.isStopped = true;
                return;
            }

            NavAgent.isStopped = false;
            NavAgent.speed = enemySpeed * mySpeedMultiplier;

            HandleStuckDetection();

            // Skip waypoints we've already passed
            while (currentWaypointIndex < myWaypoints.Length && HasPassedWaypoint(currentWaypointIndex))
            {
                float dist = Vector3.Distance(transform.position, myWaypoints[currentWaypointIndex]);
                if (dist > 3f) break;

                currentWaypointIndex++;
            }

            if (currentWaypointIndex >= myWaypoints.Length) return;

            Vector3 targetWaypoint = myWaypoints[currentWaypointIndex];

            // Apply path offset for variation
            if (currentWaypointIndex < myWaypoints.Length - 1)
            {
                Vector3 nextWaypoint = myWaypoints[currentWaypointIndex + 1];
                Vector3 pathDirection = (nextWaypoint - targetWaypoint).normalized;
                Vector3 offsetDirection = Vector3.Cross(pathDirection, Vector3.up);

                float waypointOffset = myPathOffset + (myPathDrift * currentWaypointIndex);
                waypointOffset = Mathf.Clamp(waypointOffset, -pathOffsetRange, pathOffsetRange);

                targetWaypoint += offsetDirection * waypointOffset;
            }

            NavAgent.SetDestination(targetWaypoint);
            
            if (this is EnemySplitter)
            {
                Debug.Log($"[{gameObject.name}] SetDest: {targetWaypoint}, NavAgent.dest: {NavAgent.destination}, pathStatus: {NavAgent.pathStatus}");
            }

            // Simple rotation towards movement direction
            Vector3 currentDirection = NavAgent.steeringTarget - transform.position;
            currentDirection.y = 0;

            if (currentDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(currentDirection.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }

            // Check if reached waypoint
            float dynamicThreshold = waypointReachThreshold * myCornerCutMultiplier;

            if (!NavAgent.pathPending && NavAgent.remainingDistance <= dynamicThreshold)
            {
                currentWaypointIndex++;
                stuckDuration = 0f;
    
                // Debug
                if (currentWaypointIndex < myWaypoints.Length)
                {
                    Debug.Log($"[{gameObject.name}] Reached waypoint {currentWaypointIndex - 1}, now heading to {currentWaypointIndex}: {myWaypoints[currentWaypointIndex]}");
                }
            }
        }
    }
    
    private void HandleStuckDetection()
    {
        if (Time.time < lastStuckCheckTime + stuckCheckInterval) return;

        float movedDistance = Vector3.Distance(transform.position, lastStuckCheckPosition);
    
        if (movedDistance < stuckDistanceThreshold)
        {
            stuckDuration += stuckCheckInterval;
        
            if (stuckDuration >= 1f)
            {
                float distToWaypoint = Vector3.Distance(transform.position, myWaypoints[currentWaypointIndex]);
            
                if (distToWaypoint <= stuckSkipDistance)
                {
                    currentWaypointIndex++;
                    stuckDuration = 0f;
                }
                else if (stuckDuration >= 2f)
                {
                    NavMeshHit hit;
                    Vector3 randomOffset = Random.insideUnitSphere * 0.5f;
                    randomOffset.y = 0;
                
                    if (NavMesh.SamplePosition(transform.position + randomOffset, out hit, 1f, NavMesh.AllAreas))
                    {
                        NavAgent.Warp(hit.position);
                    }
                    stuckDuration = 0f;
                }
            }
        }
        else
        {
            stuckDuration = 0f;
        }

        lastStuckCheckPosition = transform.position;
        lastStuckCheckTime = Time.time;
    }

    private bool HasPassedWaypoint(int waypointIndex)
    {
        if (waypointIndex >= myWaypoints.Length) return false;
        
        Vector3 waypoint = myWaypoints[waypointIndex];
        
        Vector3 pathDirection;
        if (waypointIndex < myWaypoints.Length - 1)
        {
            pathDirection = (myWaypoints[waypointIndex + 1] - waypoint).normalized;
        }
        else
        {
            if (waypointIndex > 0)
            {
                pathDirection = (waypoint - myWaypoints[waypointIndex - 1]).normalized;
            }
            else
            {
                return false;
            }
        }
        
        Vector3 toEnemy = transform.position - waypoint;
        toEnemy.y = 0;
        pathDirection.y = 0;
        
        return Vector3.Dot(toEnemy, pathDirection) > 0;
    }
    
    private void PlayAnimations()
    {
        if (EnemyAnimator == null) return;

        if (isStunned || !canMove)
        {
            EnemyAnimator.SetBool("Walk", false);
            return;
        }

        if (spellsActivated)
        {
            EnemyAnimator.SetBool("Walk", false);
            EnemyAnimator.SetTrigger("FloatMidAir");
            return;
        }

        EnemyAnimator.SetBool("Walk", true);
        myBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (NavAgent != null && NavAgent.isOnNavMesh && NavAgent.enabled)
        {
            float speedRatio = NavAgent.velocity.magnitude / (enemySpeed * mySpeedMultiplier);
            EnemyAnimator.SetFloat("WalkSpeed", animationSpeedMultiplier * Mathf.Clamp(speedRatio, 0.8f, 1.2f));
        }
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

    public virtual void TakeDamage(DamageInfo damageInfo, float vfxDamage = 0, bool spellDamageEnabled = false)
    {
        DamageCalculator.DamageResult result = DamageCalculator.Calculate(damageInfo, elementType);

        if (DamageNumberSpawner.instance != null)
        {
            if (!spellDamageEnabled)
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
            else
            {
                Debug.Log("12345");
                Vector3 spawnPos = centerPoint != null ? centerPoint.position : transform.position;
                DamageNumberSpawner.instance.Spawn(
                    spawnPos,
                    result.finalDamage,
                    result.wasSuperEffective,
                    result.wasNotVeryEffective,
                    result.wasImmune,
                    true
                );
            }
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
        
        if (spellDamageEnabled)
        {
            if (gameObject.activeInHierarchy)
            {
                enemyCurrentHp -= vfxDamage * 100;
            }
        }
        else
        {
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
        }
        
        // Update health bar visual
        healthBar.value = enemyCurrentHp / enemyMaxHp;
        
        // Show health bar and start fade timer
        ShowHealthBar();

        // Check for death
        if (enemyCurrentHp <= 0 && !isDead)
        {
            isDead = true;
            HideHealthBarImmediate();
            Die();
        }
    }

    public virtual void TakeDamage(float incomingDamage, float vfxDamage = 0, bool spellDamageEnabled = false)
    {
        TakeDamage(new DamageInfo(incomingDamage, ElementType.Physical), vfxDamage, spellDamageEnabled);
    }
    
    private void ShowHealthBar()
    {
        if (!gameObject.activeInHierarchy) return;
        
        enemyHealthDisplayCanvas.enabled = true;
        
        if (hideHealthBarCoroutine != null)
        {
            StopCoroutine(hideHealthBarCoroutine);
        }
        
        hideHealthBarCoroutine = StartCoroutine(HideHealthBarAfterDelay());
    }

    private IEnumerator HideHealthBarAfterDelay()
    {
        yield return new WaitForSeconds(healthBarDisplayDuration);
        
        if (gameObject.activeInHierarchy && !isDead)
        {
            enemyHealthDisplayCanvas.enabled = false;
        }
        
        hideHealthBarCoroutine = null;
    }

    private void HideHealthBarImmediate()
    {
        if (hideHealthBarCoroutine != null)
        {
            StopCoroutine(hideHealthBarCoroutine);
            hideHealthBarCoroutine = null;
        }
        
        enemyHealthDisplayCanvas.enabled = false;
    }

    public void Die()
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

        stuckDuration = 0f;
        lastStuckCheckTime = Time.time;
        lastStuckCheckPosition = transform.position;
        
        // Reset health bar
        healthBar.value = 1f;
        enemyHealthDisplayCanvas.enabled = false;
        if (hideHealthBarCoroutine != null)
        {
            StopCoroutine(hideHealthBarCoroutine);
            hideHealthBarCoroutine = null;
        }
        
        UpdateVisuals();
    }
    
    public void LiftEffectFunction(bool status, bool isMechanicSpellDamage,EnemyBase enemyBaseRef)
    {
        if (gameObject.activeInHierarchy && gameObject != null)
        {
            enemyBaseRef.myBody.isKinematic = !status;
            enemyBaseRef.myBody.useGravity = !status;
            if (!status)
            {
                myBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                StartCoroutine(EnableNavMesh(enemyBaseRef));
            }
            else
            {
                enemyBaseRef.spellsActivated = status;
                enemyBaseRef.NavAgent.enabled = !status;
                StartCoroutine(DisableYPos(isMechanicSpellDamage));
            }
        }
    }

    private IEnumerator DisableYPos(bool isMechanicalSpellDamage)
    {
        if (gameObject.activeInHierarchy && gameObject != null && !isMechanicalSpellDamage)
        {
            yield return new WaitForSeconds(0.5f);
            myBody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        }
    }
    
    private IEnumerator EnableNavMesh(EnemyBase enemyBaseRef)
    {
        if (gameObject.activeInHierarchy && gameObject != null)
        {
            yield return new WaitForSecondsRealtime(0.4f);
            spellsActivated = false;
            enemyBaseRef.NavAgent.enabled = true;
        }
    }
  
    public IEnumerator ExplodeEnemy(Vector3 currentMousePosition, EnemyBase affectedEnemy)
    {
        NavAgent.enabled = false;
        EnemyAnimator.enabled = false;
        spellsActivated = true;
        myBody.isKinematic = false;
        myBody.constraints = RigidbodyConstraints.None;
       
        myBody.useGravity = true;

        myBody.AddExplosionForce(0.5f, currentMousePosition, 5, 2, ForceMode.Force);

        yield return new WaitForSeconds(2f);
        
        Die();
    }

    public void UpdateVisuals()
    {
        if (!isInvisible) return;
    
        Renderer r = GetComponentInChildren<Renderer>();
        if (r == null) return;

        Color currentColor = r.material.color;
        currentColor.a = 0.3f;
        r.material.color = currentColor;
    }
    
    public void ApplyShield(float shieldAmount, GameObject shieldEffectPrefab)
    {
        hasShield = true;
        shieldHealth = shieldAmount;

        if (shieldEffectPrefab != null && activeShieldEffect == null)
        {
            activeShieldEffect = ObjectPooling.instance.GetVFXWithParent(shieldEffectPrefab, transform, -1f);
        }

        Debug.Log($"Shield applied: {shieldAmount} HP");
    }

    private void RemoveShield()
    {
        hasShield = false;
        shieldHealth = 0f;

        if (activeShieldEffect != null)
        {
            ObjectPooling.instance.Return(activeShieldEffect);
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
    public bool IsFrozen() => isStunned;
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
    public EnemyVFXPool vfxContainer { get; set; }
    //public EnemyBase enemyBaseRef { get; set; }
    public bool isDeadProperty => isDead;
    
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
    
        if (currentWaypointIndex + 1 >= myWaypoints.Length) return transform.forward;
    
        Vector3 currentWaypoint = myWaypoints[currentWaypointIndex];
        Vector3 nextWaypoint = myWaypoints[currentWaypointIndex + 1];
    
        return (nextWaypoint - currentWaypoint).normalized;
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        if (SpellAbility.instance.MechanicSpellActivated)
        {
            SkinnedMeshRenderer skinnedMeshRenderer = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
            skinnedMeshRenderer.material.color = new Color(1, 0, 0, 0.7f);
        }
    }

   void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        if (SpellAbility.instance.MechanicSpellActivated)
        {
            SkinnedMeshRenderer skinnedMeshRenderer = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
            foreach (Color o in originalColors)
            {
                skinnedMeshRenderer.material.color = o;
            }
        }
    }
}