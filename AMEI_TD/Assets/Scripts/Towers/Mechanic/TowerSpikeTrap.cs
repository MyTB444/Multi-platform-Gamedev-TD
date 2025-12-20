using UnityEngine;

public class TowerSpikeTrap : TowerBase
{
    [Header("Trap Spawning")]
    [SerializeField] private GameObject spikeTrapPrefab;
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private LayerMask roadLayer;
    [SerializeField] private float downwardAngle = 45f;
    
    [Header("VFX")]
    [SerializeField] private Transform hammerImpactPoint;
    
    private SpikeTrapDamage spikeTrap;
    
    protected override void Awake()
    {
        base.Awake();
        SpawnTrapOnRoad();
    }
    
    private void SpawnTrapOnRoad()
    {
        Vector3 rayDirection = Quaternion.AngleAxis(downwardAngle, transform.right) * transform.forward;
        
        if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, raycastDistance, roadLayer))
        {
            GameObject trap = Instantiate(spikeTrapPrefab, hit.point, Quaternion.identity);
            spikeTrap = trap.GetComponent<SpikeTrapDamage>();
            spikeTrap.Setup(damage, whatIsEnemy, attackCooldown, this);
            trap.transform.SetParent(transform);
        }
        else
        {
            Debug.LogWarning("SpikeTrap: No road found!");
        }
    }
    
    public void PlayHammerAnimation()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(attackAnimationTrigger);
        }
    }
    
    public void SpawnHammerImpactVFX()
    {
        if (attackSpawnEffectPrefab != null && hammerImpactPoint != null)
        {
            GameObject vfx = Instantiate(attackSpawnEffectPrefab, hammerImpactPoint.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }
    }
    
    protected override void Attack() { }
    protected override bool CanAttack() { return false; }
    protected override void HandleRotation() { }
    
    private void OnDrawGizmosSelected()
    {
        Vector3 rayDirection = Quaternion.AngleAxis(downwardAngle, transform.right) * transform.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, rayDirection * raycastDistance);
    }
}