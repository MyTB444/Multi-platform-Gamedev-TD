using UnityEngine;

public class TowerArcher : TowerBase
{
    [Header("Archer Setup")]
    [SerializeField] private BowController bowController;
    [SerializeField] private GameObject arrowVisual;
    
    [Header("Animation Timing")]
    [SerializeField] private float baseAnimationLength = 1f;
    [SerializeField] private float arrowRespawnDelay = 0.9f;
    
    private bool isAttacking = false;
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    private void UpdateAnimationSpeed()
    {
        if (characterAnimator == null) return;
        
        float animSpeed = baseAnimationLength / attackCooldown;
        characterAnimator.SetFloat("AttackSpeed", animSpeed);
    }
    
    protected override bool CanAttack()
    {
        return base.CanAttack() && !isAttacking;
    }
    
    protected override void Attack()
    {
        lastTimeAttacked = Time.time;
        isAttacking = true;
        
        if (characterAnimator != null)
        {
            UpdateAnimationSpeed();
            characterAnimator.SetBool("Attack", true);
        }
    }
    
    public void OnDrawBow()
    {
        if (bowController != null)
        {
            bowController.DrawBow();
        }
        
        if (arrowVisual != null)
        {
            arrowVisual.SetActive(true);
        }
    }
    
    public void OnReleaseBow()
    {
        if (bowController != null)
        {
            bowController.ReleaseBow();
        }
        
        FireArrow();
        
        if (arrowVisual != null)
        {
            arrowVisual.SetActive(false);
        }
        
        characterAnimator.SetBool("Attack", false);
        isAttacking = false;
        
        Invoke("OnArrowReady", arrowRespawnDelay);
    }
    
    public void OnArrowReady()
    {
        if (arrowVisual != null)
        {
            arrowVisual.SetActive(true);
        }
    }
    
    private void FireArrow()
    {
        if (currentEnemy == null) return;
        
        Vector3 spawnPos = arrowVisual.transform.position;
        Quaternion spawnRot = arrowVisual.transform.rotation;
        
        Vector3 targetPos = currentEnemy.GetCenterPoint();
        float distance = Vector3.Distance(spawnPos, targetPos);
        
        GameObject newArrow = Instantiate(projectilePrefab, spawnPos, spawnRot);
        
        ArrowProjectile arrow = newArrow.GetComponent<ArrowProjectile>();
        
        IDamageable damageable = currentEnemy.GetComponent<IDamageable>();
        
        if (damageable != null)
        {
            arrow.SetupArcProjectile(targetPos, damageable, damage, projectileSpeed, distance);
        }
    }
}