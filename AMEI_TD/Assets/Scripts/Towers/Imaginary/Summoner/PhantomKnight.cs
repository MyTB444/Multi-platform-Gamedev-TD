using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Spectral warrior that chases and attacks enemies. Spawned by TowerPhantomKnight.
/// Uses NavMeshAgent for pathfinding, attacks with sword collider, then fades out.
/// Can optionally attack twice (double slash upgrade) before despawning.
/// </summary>
public class PhantomKnight : MonoBehaviour
{
    // ==================== REFERENCES ====================
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private GameObject slashVFX;
    private NavMeshAgent agent;
    
    // ==================== ANIMATION ====================
    [Header("Animation")]
    [SerializeField] private string walkBool = "Walk";
    [SerializeField] private string[] attackTriggers = { "Attack1", "Attack2", "Attack3" };  // Random attack variety
    [SerializeField] private string spawnTrigger = "Spawn";
    [SerializeField] private float spawnAnimationDuration = 1f;
    
    // ==================== SWORD TIMING ====================
    [Header("Sword Collider")]
    [SerializeField] private float swordActiveTime = 0.5f;       // How long sword deals damage
    [SerializeField] private float swordActivationDelay = 0.2f;  // Delay before sword becomes active
    
    // ==================== VFX ====================
    [Header("VFX")]
    [SerializeField] private Vector3 slashVFXRotationOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private float slashVFXStartDelay = 0f;
    [SerializeField] private float slashVFXDuration = 0.5f;
    
    // ==================== TARGET FINDING ====================
    [Header("Target Finding")]
    [SerializeField] private float searchRadius = 15f;          // Range to search for new targets
    [SerializeField] private float maxTimeWithoutTarget = 5f;   // Fade out if no target found
    
    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 8f;            // Max time before forced fade out
    
    // ==================== UPGRADE FLAGS ====================
    private bool canDoubleSlash = false;   // Attack twice before despawning
    private int attackCount = 0;           // Track attacks for double slash
    private EnemyBase lastHitEnemy;        // Exclude from second target search
    private bool applySlow = false;        // Spectral Chains upgrade
    private float slowPercent;
    private float slowDuration;

    // ==================== AUDIO ====================
    private AudioClip slashSound;
    private float slashSoundVolume;

    // ==================== COMBAT STATE ====================
    private DamageInfo damageInfo;
    private float attackRadius;
    private float fadeOutTime;
    private LayerMask enemyLayer;
    
    private bool hasAttacked = false;      // Has initiated attack sequence
    private bool isFading = false;         // Currently fading out
    private bool isReady = false;          // Spawn animation complete
    private Transform targetEnemy;
    
    private float timeWithoutTarget = 0f;  // Timer for no-target despawn
    private float spawnTime;               // For lifetime tracking
    
    // ==================== COMPONENTS ====================
    private GhostEffect ghostEffect;
    private PhantomSwordDamage swordDamage;
    
    private void Awake()
    {
        // Find animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        agent = GetComponent<NavMeshAgent>();
    }
    
    /// <summary>
    /// Reset state when retrieved from pool.
    /// </summary>
    private void OnEnable()
    {
        hasAttacked = false;
        isFading = false;
        isReady = false;
        targetEnemy = null;
        timeWithoutTarget = 0f;
        attackCount = 0;
        lastHitEnemy = null;
        spawnTime = Time.time;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
    
    /// <summary>
    /// Configures the phantom with all necessary parameters.
    /// Called by TowerPhantomKnight when spawning.
    /// </summary>
    public void Setup(float newSpeed, DamageInfo newDamageInfo, float newAttackRadius, float newStoppingDistance, float newFadeOutTime, LayerMask newEnemyLayer, Transform enemy, bool doubleSlash = false, bool applySlow = false, float slowPercent = 0f, float slowDuration = 0f, AudioClip slashSound = null, float slashSoundVolume = 1f)
    {
        damageInfo = newDamageInfo;
        attackRadius = newAttackRadius;
        fadeOutTime = newFadeOutTime;
        enemyLayer = newEnemyLayer;
        targetEnemy = enemy;
        canDoubleSlash = doubleSlash;
        attackCount = 0;
        lastHitEnemy = null;
        this.applySlow = applySlow;
        this.slowPercent = slowPercent;
        this.slowDuration = slowDuration;
        this.slashSound = slashSound;
        this.slashSoundVolume = slashSoundVolume;

        // Configure sword damage component
        swordDamage = GetComponentInChildren<PhantomSwordDamage>();
        if (swordDamage != null)
        {
            swordDamage.Setup(damageInfo, enemyLayer, slashVFX, slashVFXRotationOffset, slashVFXStartDelay, slashVFXDuration, applySlow, slowPercent, slowDuration, slashSound, slashSoundVolume);
        }
        
        // Setup ghost visual effect
        ghostEffect = GetComponent<GhostEffect>();
        if (ghostEffect == null)
        {
            ghostEffect = gameObject.AddComponent<GhostEffect>();
        }
        ghostEffect.SetAlpha(0.6f);
        
        // Configure NavMeshAgent
        if (agent != null)
        {
            agent.speed = newSpeed;
            agent.stoppingDistance = newStoppingDistance;
            
            StartCoroutine(WaitForNavMesh());
        }
        else
        {
            isReady = true;
            StartWalking();
        }
    }
    
    /// <summary>
    /// Ensures phantom is properly placed on NavMesh before moving.
    /// Plays spawn animation while waiting.
    /// </summary>
    private IEnumerator WaitForNavMesh()
    {
        yield return null;
        
        int attempts = 0;
        int maxAttempts = 10;
        
        // Try to find valid NavMesh position
        while (!agent.isOnNavMesh && attempts < maxAttempts)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                // Warp agent to valid position
                agent.enabled = false;
                transform.position = hit.position;
                agent.enabled = true;
                agent.Warp(hit.position);
            }
            
            attempts++;
            yield return null;
        }
        
        // Failed to find NavMesh - return to pool
        if (!agent.isOnNavMesh)
        {
            ObjectPooling.instance.Return(gameObject);
            yield break;
        }
        
        // Wait for spawn animation
        agent.isStopped = true;
        
        if (animator != null)
        {
            animator.SetTrigger(spawnTrigger);
        }
        
        yield return new WaitForSeconds(spawnAnimationDuration);
        
        // Ready to move
        agent.isStopped = false;
        isReady = true;
        StartWalking();
    }
    
    private void StartWalking()
    {
        if (animator != null)
        {
            animator.SetBool(walkBool, true);
        }
    }
    
    private void StopWalking()
    {
        if (animator != null)
        {
            animator.SetBool(walkBool, false);
        }
    }
    
    /// <summary>
    /// Main update loop - handles targeting, movement, and attack initiation.
    /// </summary>
    private void Update()
    {
        if (!isReady || hasAttacked || isFading) return;
        
        // Check lifetime limit
        if (Time.time - spawnTime >= maxLifetime)
        {
            StartCoroutine(FadeOut());
            return;
        }
        
        bool hasValidTarget = targetEnemy != null && targetEnemy.gameObject.activeSelf;
        
        if (hasValidTarget)
        {
            timeWithoutTarget = 0f;
            
            // Move toward target
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(targetEnemy.position);
            }
            
            StartWalking();
            
            // Check if in attack range
            float distToTarget = Vector3.Distance(transform.position, targetEnemy.position);
            if (distToTarget <= attackRadius)
            {
                StartAttack();
            }
        }
        else
        {
            // No valid target - search for new one
            timeWithoutTarget += Time.deltaTime;
            
            FindNewTarget();
            
            // Stop moving while searching
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            
            StopWalking();
            
            // Fade out if no target found for too long
            if (timeWithoutTarget >= maxTimeWithoutTarget)
            {
                StartCoroutine(FadeOut());
            }
        }
    }
    
    /// <summary>
    /// Searches for closest enemy within search radius.
    /// </summary>
    private void FindNewTarget()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, searchRadius, enemyLayer);
        
        if (enemies.Length > 0)
        {
            float closestDist = float.MaxValue;
            Transform closest = null;
            
            foreach (Collider col in enemies)
            {
                if (!col.gameObject.activeSelf) continue;
                
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = col.transform;
                }
            }
            
            if (closest != null)
            {
                targetEnemy = closest;
                timeWithoutTarget = 0f;
            }
        }
    }
    
    /// <summary>
    /// Initiates attack sequence - stops movement, faces target, plays random attack animation.
    /// </summary>
    private void StartAttack()
    {
        hasAttacked = true;
        
        // Stop moving
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        
        // Face the target
        if (targetEnemy != null)
        {
            Vector3 lookDir = (targetEnemy.position - transform.position).normalized;
            lookDir.y = 0;
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
        
        // Play random attack animation
        if (animator != null)
        {
            animator.SetBool(walkBool, false);
            
            string randomAttack = attackTriggers[Random.Range(0, attackTriggers.Length)];
            animator.SetTrigger(randomAttack);
        }
        
        // Start sword damage window
        StartCoroutine(ActivateSwordCollider());
    }
    
    /// <summary>
    /// Manages the sword damage window timing.
    /// After attack, either finds new target (double slash) or fades out.
    /// </summary>
    private IEnumerator ActivateSwordCollider()
    {
        // Wait for animation wind-up
        yield return new WaitForSeconds(swordActivationDelay);
    
        // Enable sword damage
        if (swordDamage != null)
        {
            swordDamage.EnableDamage();
        }
    
        // Sword active duration
        yield return new WaitForSeconds(swordActiveTime);
    
        // Disable sword damage
        if (swordDamage != null)
        {
            swordDamage.DisableDamage();
        }
    
        attackCount++;
    
        // Check for double slash upgrade
        if (canDoubleSlash && attackCount < 2)
        {
            // Find new target, excluding the one we just hit
            FindNewTargetExcluding(lastHitEnemy);
        
            if (targetEnemy != null && targetEnemy.gameObject.activeSelf)
            {
                // Attack again
                hasAttacked = false;
                StartWalking();
            
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                }
            }
            else
            {
                StartCoroutine(FadeOut());
            }
        }
        else
        {
            // Done attacking - fade out
            StartCoroutine(FadeOut());
        }
    }
    
    /// <summary>
    /// Finds closest enemy, optionally excluding a specific enemy (the one just attacked).
    /// Falls back to excluded enemy if it's the only one left.
    /// </summary>
    private void FindNewTargetExcluding(EnemyBase excludeEnemy)
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, searchRadius, enemyLayer);
    
        float closestDist = float.MaxValue;
        Transform closest = null;
    
        foreach (Collider col in enemies)
        {
            if (!col.gameObject.activeSelf) continue;
        
            // Skip the excluded enemy
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null && enemy == excludeEnemy) continue;
        
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = col.transform;
            }
        }
    
        // Fallback: if no other enemies, re-target excluded enemy
        if (closest == null && excludeEnemy != null && excludeEnemy.gameObject.activeSelf)
        {
            closest = excludeEnemy.transform;
        }
    
        targetEnemy = closest;
    }
    
    /// <summary>
    /// Gradually fades phantom to invisible, then returns to pool.
    /// </summary>
    private IEnumerator FadeOut()
    {
        isFading = true;
        
        // Stop all movement
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        
        StopWalking();
        
        // Fade alpha from 0.6 to 0
        float elapsed = 0f;
        
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float alpha = 0.6f * (1f - (elapsed / fadeOutTime));
            
            if (ghostEffect != null)
            {
                ghostEffect.SetAlpha(alpha);
            }
            
            yield return null;
        }
        
        // Return to pool
        ObjectPooling.instance.Return(gameObject);
    }
    
    public bool HasAttacked()
    {
        return hasAttacked;
    }
    
    /// <summary>
    /// Editor visualization - shows attack range and search radius.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Attack range
        Gizmos.color = Color.red;
        Vector3 pos = attackPoint != null ? attackPoint.position : transform.position;
        Gizmos.DrawWireSphere(pos, attackRadius);
        
        // Search radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
}