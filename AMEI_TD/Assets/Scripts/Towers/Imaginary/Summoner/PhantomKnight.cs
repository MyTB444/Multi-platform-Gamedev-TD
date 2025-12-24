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
    
    private float damage;
    private float attackRadius;
    private float fadeOutTime;
    private LayerMask enemyLayer;
    
    private bool hasAttacked = false;
    private bool isFading = false;
    private bool isReady = false;
    private Transform targetEnemy;
    
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
    
    public void Setup(float newSpeed, float newDamage, float newAttackRadius, float newStoppingDistance, float newFadeOutTime, LayerMask newEnemyLayer, Transform enemy)
    {
        damage = newDamage;
        attackRadius = newAttackRadius;
        fadeOutTime = newFadeOutTime;
        enemyLayer = newEnemyLayer;
        targetEnemy = enemy;
    
        // Setup sword damage component
        swordDamage = GetComponentInChildren<PhantomSwordDamage>();
        if (swordDamage != null)
        {
            swordDamage.Setup(damage, enemyLayer, slashVFX, slashVFXRotationOffset, slashVFXStartDelay, slashVFXDuration);
        }
    
        ghostEffect = GetComponent<GhostEffect>();
        if (ghostEffect == null)
        {
            ghostEffect = gameObject.AddComponent<GhostEffect>();
        }
    
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
            Destroy(gameObject);
            yield break;
        }
        
        agent.isStopped = true;
        
        // Play spawn animation
        if (animator != null)
        {
            animator.SetTrigger(spawnTrigger);
        }
        
        // Wait for spawn animation to finish
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
    
    private void Update()
    {
        if (!isReady || hasAttacked || isFading) return;
        
        if (targetEnemy != null && agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(targetEnemy.position);
        }
        
        // Check distance to target
        if (targetEnemy != null)
        {
            float distToTarget = Vector3.Distance(transform.position, targetEnemy.position);
            if (distToTarget <= attackRadius)
            {
                StartAttack();
            }
        }
        
        if (targetEnemy == null || !targetEnemy.gameObject.activeSelf)
        {
            FindNewTarget();
        }
    }
    
    private void FindNewTarget()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, 10f, enemyLayer);
        
        if (enemies.Length > 0)
        {
            float closestDist = float.MaxValue;
            Transform closest = null;
            
            foreach (Collider col in enemies)
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = col.transform;
                }
            }
            
            targetEnemy = closest;
        }
        else
        {
            StartCoroutine(FadeOut());
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
            Debug.Log($"Triggering attack: {randomAttack}");
            animator.SetTrigger(randomAttack);
        }
        
        StartCoroutine(ActivateSwordCollider());
    }
    
    private IEnumerator ActivateSwordCollider()
    {
        yield return new WaitForSeconds(swordActivationDelay);
        
        // Enable sword damage
        if (swordDamage != null)
        {
            swordDamage.EnableDamage();
        }
        
        yield return new WaitForSeconds(swordActiveTime);
        
        // Disable sword damage
        if (swordDamage != null)
        {
            swordDamage.DisableDamage();
        }
        
        // Fade out after attack
        StartCoroutine(FadeOut());
    }
    
    private IEnumerator FadeOut()
    {
        isFading = true;
        
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        
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
        
        Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 pos = attackPoint != null ? attackPoint.position : transform.position;
        Gizmos.DrawWireSphere(pos, attackRadius);
    }
}