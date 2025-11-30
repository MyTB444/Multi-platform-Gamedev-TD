using UnityEngine;

public class TowerArcher : TowerBase
{
    [Header("Archer Setup")]
    [SerializeField] private BowController bowController;
    [SerializeField] private GameObject arrowVisual;
    
    [Header("Animation Timing")]
    [SerializeField] private float baseAnimationLength = 1f;
    [SerializeField] private float arrowFlightTime = 0.5f;
    
    protected override void Awake()
    {
        base.Awake();
        UpdateAnimationSpeed();
    }
    
    private void UpdateAnimationSpeed()
    {
        if (characterAnimator == null) return;
        
        float animSpeed = baseAnimationLength / attackCooldown;
        characterAnimator.SetFloat("AttackSpeed", animSpeed);
    }
    
    protected override void Attack()
    {
        lastTimeAttacked = Time.time;
        
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
        
        // Use arrow visual's position and rotation
        Vector3 spawnPos = arrowVisual.transform.position;
        Quaternion spawnRot = arrowVisual.transform.rotation;
        
        GameObject newArrow = Instantiate(projectilePrefab, spawnPos, spawnRot);
        
        ArrowProjectile arrow = newArrow.GetComponent<ArrowProjectile>();
        
        Vector3 targetPos = currentEnemy.GetCenterPoint();
        
        if (Physics.Raycast(spawnPos, (targetPos - spawnPos).normalized, out RaycastHit hitInfo, Mathf.Infinity, whatIsTargetable))
        {
            IDamageable damageable = hitInfo.transform.GetComponent<IDamageable>();
            
            if (damageable != null)
            {
                arrow.SetupArcProjectile(hitInfo.point, damageable, damage, arrowFlightTime);
            }
        }
    }
}