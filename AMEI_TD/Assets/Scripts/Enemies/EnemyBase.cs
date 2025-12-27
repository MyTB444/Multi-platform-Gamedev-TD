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

    private NavMeshAgent NavAgent;
    private Animator EnemyAnimator;

    [SerializeField] protected float enemyCurrentHp;
    protected bool isDead;

    protected Vector3[] myWaypoints;
    protected int currentWaypointIndex = 0;
    private float waypointReachDistance = 0.3f;

    private int originalLayerIndex;
    private float totalDistance;

    private Vector3 Destination;

    // Slow system
    private float baseSpeed;
    private float slowEndTime;
    private bool isSlowed = false;
    private GameObject activeSlowEffect;

    // DoT system
    private DamageInfo dotDamageInfo;
    private float dotEndTime;
    private float dotTickInterval = 0.5f;
    private float lastDotTick;
    private bool hasDot = false;

    private void Awake()
    {
        originalLayerIndex = gameObject.layer;
        baseSpeed = enemySpeed;
    }

    protected virtual void Start()
    {
        UpdateVisuals();
        NavAgent = GetComponent<NavMeshAgent>();
        EnemyAnimator = GetComponent<Animator>();
    }

    protected virtual void Update()
    {
        UpdateStatusEffects();
        FollowPath();
        PlayAnimations();
    }

    private void UpdateStatusEffects()
    {
        // Handle slow expiry
        if (isSlowed && Time.time >= slowEndTime)
        {
            RemoveSlow();
        }

        // Handle DoT
        if (hasDot)
        {
            if (Time.time >= dotEndTime)
            {
                hasDot = false;
            }
            else if (Time.time >= lastDotTick + dotTickInterval)
            {
                lastDotTick = Time.time;
                TakeDamage(dotDamageInfo);
            }
        }
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        Debug.Log($"ApplySlow called - slowEffectPrefab: {slowEffectPrefab}");
    
        isSlowed = true;
        slowEndTime = Time.time + duration;

        enemySpeed = baseSpeed * (1f - slowPercent);

        if (activeSlowEffect == null && slowEffectPrefab != null)
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

    public void ApplyDoT(DamageInfo damagePerTick, float duration, float tickInterval = 0.5f)
    {
        // Check if immune to this element - skip DoT entirely
        DamageCalculator.DamageResult testResult = DamageCalculator.Calculate(damagePerTick, elementType);
    
        if (testResult.wasImmune)
        {
            // Initial hit already showed IMMUNE, just don't apply DoT
            return;
        }
    
        hasDot = true;
        dotDamageInfo = damagePerTick;
        dotDamageInfo.isDoT = true;
        dotEndTime = Time.time + duration;
        dotTickInterval = tickInterval;
        lastDotTick = Time.time;
    }

    public void SetupEnemy(EnemySpawner myNewSpawner)
    {
        mySpawner = myNewSpawner;
        UpdateWaypoints(myNewSpawner.currentWaypoints);
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
        if (myWaypoints == null || currentWaypointIndex >= myWaypoints.Length)
        {
            return;
        }

        Vector3 targetWaypoint = myWaypoints[currentWaypointIndex];
        if (!bottomPoint) return;

        Vector3 direction = (targetWaypoint - bottomPoint.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
        Destination = direction * (enemySpeed * Time.deltaTime);
        transform.position += Destination;

        float distanceToWaypoint = Vector3.Distance(bottomPoint.position, targetWaypoint);
        if (distanceToWaypoint <= waypointReachDistance)
        {
            currentWaypointIndex++;
        }
        
        if (NavAgent != null)
        {
            NavAgent.SetDestination(Destination);
        }
    }

    private void PlayAnimations()
    {
        if (EnemyAnimator == null) return;

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
        enemyCurrentHp -= result.finalDamage;

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
        Destroy(gameObject);
        if (GameManager.instance != null)
        {
            GameManager.instance.UpdateSkillPoints(reward);
        }
        RemoveEnemy();
    }

    public void RemoveEnemy()
    {
        if (mySpawner != null) mySpawner.RemoveActiveEnemy(gameObject);
    }

    private void ResetEnemy()
    {
        gameObject.layer = originalLayerIndex;
        enemyCurrentHp = enemyMaxHp;
        isDead = false;

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
    public bool HasDoT() => hasDot;
    public ElementType GetElementType() => elementType;
}