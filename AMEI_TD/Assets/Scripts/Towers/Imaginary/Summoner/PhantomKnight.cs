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
    
    [Header("Sword Collider")]
    [SerializeField] private Collider swordCollider;
    [SerializeField] private float swordActiveTime = 0.5f;
    [SerializeField] private float swordActivationDelay = 0.2f;
    
    private float damage;
    private PhantomSwordDamage swordDamage;
    private float attackRadius;
    private float fadeOutTime;
    private LayerMask enemyLayer;
    
    private bool hasAttacked = false;
    private bool isFading = false;
    private bool isReady = false;
    private Transform targetEnemy;
    
    private GhostEffect ghostEffect;
    
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
    
    public void Setup(float newSpeed, float newDamage, float newAttackRadius, float newFadeOutTime, LayerMask newEnemyLayer, Transform enemy)
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
            swordDamage.Setup(damage, enemyLayer);
            Debug.Log("Sword damage component found and setup!");
        }
        else
        {
            Debug.LogError("No PhantomSwordDamage found in children!");
        }
    
        ghostEffect = GetComponent<GhostEffect>();
        if (ghostEffect == null)
        {
            ghostEffect = gameObject.AddComponent<GhostEffect>();
        }
    
        if (agent != null)
        {
            agent.speed = newSpeed;
            agent.stoppingDistance = attackRadius * 0.5f;
        
            StartCoroutine(WaitForNavMesh());
        }
        else
        {
            isReady = true;
            StartWalking();
        }
    }

    private IEnumerator ActivateSwordCollider()
    {
        yield return new WaitForSeconds(swordActivationDelay);
    
        // Enable sword damage
        if (swordDamage != null)
        {
            swordDamage.EnableDamage();
        }
    
        // Spawn VFX
        if (slashVFX != null && attackPoint != null)
        {
            GameObject vfx = Instantiate(slashVFX, attackPoint.position, transform.rotation);
            Destroy(vfx, 1f);
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
        else
        {
            Debug.LogError("No animator!");
        }
    
        StartCoroutine(ActivateSwordCollider());
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