using System.Collections.Generic;

using System.Collections;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

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
    private float totalDistance;

    private Vector3 Destination;
    [HideInInspector]public Rigidbody myBody;
    private bool spellsActivated;

    // Slow system
    private float baseSpeed;
    private float slowEndTime;
    private bool isSlowed = false;
    private GameObject activeSlowEffect;
    
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
    

   

    private VFXDamage vfxDamageScriptRef;

    [SerializeField] private Canvas enemyHealthDisplayCanvas;
    [SerializeField] private Slider healthBar;

    [SerializeField] private EnemyVFXPool enemyVFXPoolScriptRef;

    
    private void OnEnable()
    {
        enemyHealthDisplayCanvas.enabled  = false;
        if (enemyBaseRef == null)
        {
            enemyBaseRef = this;
        }
        vfxContainer = enemyVFXPoolScriptRef;
        vfxContainer.PoolVFXGameObjects();

        enemyHealthDisplayCanvas.worldCamera = Camera.main; 
        healthBar.value = 1;
       
        UpdateVisuals();
        //Renderer renderer = GetComponent<Renderer>();
        NavAgent = GetComponent<NavMeshAgent>();
        EnemyAnimator = GetComponent<Animator>();
        myBody = GetComponent<Rigidbody>();
        NavAgent.enabled = false;
    }

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
    
        SaveOriginalColors();
    }
    
    public void GetRefOfVfxDamageScript(VFXDamage vfxDamageScriptRef)
    {
        this.vfxDamageScriptRef = vfxDamageScriptRef;
        if (this.vfxDamageScriptRef != null)
        {
            this.vfxDamageScriptRef.stopFlames = false;
        }
    }

    protected virtual void Update()
    {
        UpdateStatusEffects();
        if (!enemyBaseRef.spellsActivated)
        {
            FollowPath();
          
        }
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
        if (isStunned) return;
    
        if (myWaypoints == null || currentWaypointIndex >= myWaypoints.Length)
        {
            ReachedEnd();
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

        // Manual movement
        Destination = direction * (enemySpeed * Time.deltaTime);
        transform.position += Destination;

        float distanceToWaypoint = Vector3.Distance(bottomPoint.position, targetWaypoint);
        if (distanceToWaypoint <= waypointReachDistance)
        {
            currentWaypointIndex++;
        }
        
        if (NavAgent != null && NavAgent.isActiveAndEnabled && NavAgent.isOnNavMesh)
        {
            NavAgent.SetDestination(targetWaypoint);
        }
    }

    private void PlayAnimations()
    {
        if (EnemyAnimator == null) return;

        // Don't walk if stunned
        if (isStunned)
        {
            EnemyAnimator.SetBool("Walk", false);
            return;
        }

        if (!spellsActivated)
        {
            EnemyAnimator.SetBool("Walk", true);
            myBody.constraints = RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
        }
        else
        {
            
            EnemyAnimator.SetBool("Walk", false);
            EnemyAnimator.SetTrigger("FloatMidAir");
           
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

    public virtual void TakeDamage(DamageInfo damageInfo, bool spellDamageEnabled = false)
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

        if (spellDamageEnabled)
        {

            healthBar.value -= result.finalDamage * Time.fixedDeltaTime * 10;
            StartCoroutine(DisableHealthBar(false));
        }
        else
        {

            healthBar.value -= result.finalDamage;
            StartCoroutine(DisableHealthBar(false));
        }
        
        if ((enemyCurrentHp <= 0 || healthBar.value <= 0) && !isDead)
        {
            isDead = true;
            Die();
        }
        if (gameObject.activeInHierarchy && gameObject != null)
        {

            StartCoroutine(DisableHealthBar(true));

        }
    }
    
    private IEnumerator DisableHealthBar(bool status)
    {
        if (gameObject.activeInHierarchy && gameObject != null)
        {
            if (status)
            {
                yield return new WaitForSeconds(8f);
            }
            enemyHealthDisplayCanvas.enabled = !status;
        }
    }

    public virtual void TakeDamage(float incomingDamage, bool spellDamageEnabled = false)
    {
        TakeDamage(new DamageInfo(incomingDamage, ElementType.Physical), spellDamageEnabled);
    }

    private void Die()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.UpdateSkillPoints(reward);
        }
        RemoveEnemy();
        ObjectPooling.instance.ReturnGameObejctToPool(GetEnemyTypeForPooling(), gameObject);
    }
 
    
    private PoolGameObjectType GetEnemyTypeForPooling()
    {
        switch (enemyType)
        {
            case EnemyType.Basic:
                return PoolGameObjectType.EnemyBasic;

            case EnemyType.Fast:
                return PoolGameObjectType.EnemyFast;

            case EnemyType.Tank:
                return PoolGameObjectType.EnemyTank;

            case EnemyType.Invisible:
                return PoolGameObjectType.EnemyInvisible;

            case EnemyType.Reinforced:
                return PoolGameObjectType.EnemyReinforced;
        }
        return 0;
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
        dotCanSpread = false;
    
        // Reset Stun
        isStunned = false;
        if (hasSavedColors)
        {
            for (int i = 0; i < enemyRenderers.Length; i++)
            {
                enemyRenderers[i].material.color = originalColors[i];
            }
        }

        UpdateVisuals();
    }
    
    public void LiftEffectFunction(bool status)
    {
        if (gameObject.activeInHierarchy && gameObject != null)
        {
            enemyBaseRef.myBody.useGravity = !status;
            if (!status)
            {
                myBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                StartCoroutine(EnableNavMesh());
            }
            else
            {
                enemyBaseRef.spellsActivated = status;
                enemyBaseRef.NavAgent.enabled = !status;
                StartCoroutine(DisableYPos());
            }
        }
    }

    private void OnMouseDown()
    {
        if (SpellAbility.instance.MechanicSpellActivated)
        {
            vfxDamageScriptRef.SelectedEnemy(this);
        }
    }

    private IEnumerator DisableYPos()
    {
        if (gameObject.activeInHierarchy && gameObject != null)
        {
            yield return new WaitForSeconds(0.5f);
            myBody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        }
    }
    
    private IEnumerator EnableNavMesh()
    {
        if (gameObject.activeInHierarchy && gameObject != null)
        {
            yield return new WaitForSecondsRealtime(0.4f);
            spellsActivated = false;
            enemyBaseRef.NavAgent.enabled = true;
        }
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
    public ElementType GetElementType() => elementType;

    public EnemyVFXPool vfxContainer { get; set; }
    public EnemyBase enemyBaseRef { get; set; }
}