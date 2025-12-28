using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class PhantomKnight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private GameObject slashVFX;
    private NavMeshAgent agent;
    
    [Header("Animation")]
    [SerializeField] private string walkBool = "Walk";
    [SerializeField] private string[] attackTriggers = { "Attack1", "Attack2", "Attack3" };
    [SerializeField] private string spawnTrigger = "Spawn";
    [SerializeField] private float spawnAnimationDuration = 1f;
    
    [Header("Sword Collider")]
    [SerializeField] private float swordActiveTime = 0.5f;
    [SerializeField] private float swordActivationDelay = 0.2f;
    
    [Header("VFX")]
    [SerializeField] private Vector3 slashVFXRotationOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private float slashVFXStartDelay = 0f;
    [SerializeField] private float slashVFXDuration = 0.5f;
    
    [Header("Target Finding")]
    [SerializeField] private float searchRadius = 15f;
    [SerializeField] private float maxTimeWithoutTarget = 5f;
    
    private bool canDoubleSlash = false;
    private int attackCount = 0;
    private EnemyBase lastHitEnemy;
    private bool applySlow = false;
    private float slowPercent;
    private float slowDuration;
    
    private DamageInfo damageInfo;
    private float attackRadius;
    private float fadeOutTime;
    private LayerMask enemyLayer;
    
    private bool hasAttacked = false;
    private bool isFading = false;
    private bool isReady = false;
    private Transform targetEnemy;
    
    private float timeWithoutTarget = 0f;
    
    private GhostEffect ghostEffect;
    private PhantomSwordDamage swordDamage;
    
    private void Awake()
    {
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
    
    private void OnEnable()
    {
        hasAttacked = false;
        isFading = false;
        isReady = false;
        targetEnemy = null;
        timeWithoutTarget = 0f;
        attackCount = 0;
        lastHitEnemy = null;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
    
    public void Setup(float newSpeed, DamageInfo newDamageInfo, float newAttackRadius, float newStoppingDistance, float newFadeOutTime, LayerMask newEnemyLayer, Transform enemy, bool doubleSlash = false, bool applySlow = false, float slowPercent = 0f, float slowDuration = 0f)
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
    
        swordDamage = GetComponentInChildren<PhantomSwordDamage>();
        if (swordDamage != null)
        {
            swordDamage.Setup(damageInfo, enemyLayer, slashVFX, slashVFXRotationOffset, slashVFXStartDelay, slashVFXDuration, applySlow, slowPercent, slowDuration);
        }
        
        ghostEffect = GetComponent<GhostEffect>();
        if (ghostEffect == null)
        {
            ghostEffect = gameObject.AddComponent<GhostEffect>();
        }
        ghostEffect.SetAlpha(0.6f);
        
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
    
    private IEnumerator WaitForNavMesh()
    {
        yield return null;
    
        int attempts = 0;
        int maxAttempts = 10;
    
        while (!agent.isOnNavMesh && attempts < maxAttempts)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                agent.enabled = false;
                transform.position = hit.position;
                agent.enabled = true;
            }
        
            attempts++;
            yield return null;
        }
    
        if (!agent.isOnNavMesh)
        {
            ObjectPooling.instance.Return(gameObject);
            yield break;
        }
        
        agent.isStopped = true;
        
        if (animator != null)
        {
            animator.SetTrigger(spawnTrigger);
        }
        
        yield return new WaitForSeconds(spawnAnimationDuration);
        
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
    
    private void Update()
    {
        if (!isReady || hasAttacked || isFading) return;
        
        // Check if target is valid
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
            
            // Check distance to target
            float distToTarget = Vector3.Distance(transform.position, targetEnemy.position);
            if (distToTarget <= attackRadius)
            {
                StartAttack();
            }
        }
        else
        {
            // No valid target - find new one
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
    
    private void FindNewTarget()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, searchRadius, enemyLayer);
        
        if (enemies.Length > 0)
        {
            float closestDist = float.MaxValue;
            Transform closest = null;
            
            foreach (Collider col in enemies)
            {
                // Make sure enemy is active
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
    
    private void StartAttack()
    {
        hasAttacked = true;
        
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        
        if (targetEnemy != null)
        {
            Vector3 lookDir = (targetEnemy.position - transform.position).normalized;
            lookDir.y = 0;
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
        
        if (animator != null)
        {
            animator.SetBool(walkBool, false);
            
            string randomAttack = attackTriggers[Random.Range(0, attackTriggers.Length)];
            animator.SetTrigger(randomAttack);
        }
        
        StartCoroutine(ActivateSwordCollider());
    }
    
    private IEnumerator ActivateSwordCollider()
    {
        yield return new WaitForSeconds(swordActivationDelay);
    
        if (swordDamage != null)
        {
            swordDamage.EnableDamage();
        }
    
        yield return new WaitForSeconds(swordActiveTime);
    
        if (swordDamage != null)
        {
            swordDamage.DisableDamage();
        }
    
        attackCount++;
    
        // Double slash - find new target after first attack
        if (canDoubleSlash && attackCount < 2)
        {
            FindNewTargetExcluding(lastHitEnemy);
        
            if (targetEnemy != null && targetEnemy.gameObject.activeSelf)
            {
                // Reset to chase new target
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
            StartCoroutine(FadeOut());
        }
    }
    
    private void FindNewTargetExcluding(EnemyBase excludeEnemy)
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, searchRadius, enemyLayer);
    
        float closestDist = float.MaxValue;
        Transform closest = null;
    
        foreach (Collider col in enemies)
        {
            if (!col.gameObject.activeSelf) continue;
        
            // Skip the enemy we just hit
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null && enemy == excludeEnemy) continue;
        
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = col.transform;
            }
        }
    
        // If no new target found, attack the same enemy again
        if (closest == null && excludeEnemy != null && excludeEnemy.gameObject.activeSelf)
        {
            closest = excludeEnemy.transform;
        }
    
        targetEnemy = closest;
    }
    
    private IEnumerator FadeOut()
    {
        isFading = true;
        
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        
        StopWalking();
        
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
        
        ObjectPooling.instance.Return(gameObject);
    }
    
    public bool HasAttacked()
    {
        return hasAttacked;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 pos = attackPoint != null ? attackPoint.position : transform.position;
        Gizmos.DrawWireSphere(pos, attackRadius);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
}