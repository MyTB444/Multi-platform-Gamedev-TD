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
    
    [Header("Spike Upgrades")]
    [SerializeField] private bool poisonSpikes = false;
    [SerializeField] private float poisonDamage = 2f;
    [SerializeField] private float poisonDuration = 3f;
    [SerializeField] private GameObject poisonSpikeVFX;
    [Space]
    [SerializeField] private bool bleedingSpikes = false;
    [SerializeField] private float bleedDamage = 3f;
    [SerializeField] private float bleedDuration = 4f;
    [SerializeField] private GameObject bleedSpikeVFX;
    [SerializeField] [Range(0f, 1f)] private float critChance = 0.2f;
    [SerializeField] private float critMultiplier = 2f;
    [Space]
    [SerializeField] private bool cripplingSpikes = false;
    [SerializeField] private float slowPercent = 0.3f;
    [SerializeField] private float slowDuration = 2f;
    
    private SpikeTrapDamage spikeTrap;
    
    protected override void Awake()
    {
        base.Awake();
        SpawnTrapOnRoad();
    }
    
    public override void SetUpgrade(TowerUpgradeType upgradeType, bool enabled)
    {
        base.SetUpgrade(upgradeType, enabled);
    
        switch (upgradeType)
        {
            case TowerUpgradeType.PoisonSpikes:
                poisonSpikes = enabled;
                UpdateTrapEffects();
                break;
            case TowerUpgradeType.BleedingSpikes:
                bleedingSpikes = enabled;
                UpdateTrapEffects();
                break;
            case TowerUpgradeType.CripplingSpikes:
                cripplingSpikes = enabled;
                if (spikeTrap != null)
                {
                    if (enabled)
                    {
                        spikeTrap.SetCrippleEffect(slowPercent, slowDuration);
                    }
                    else
                    {
                        spikeTrap.ClearCrippleEffect();
                    }
                }
                break;
        }
    }

    private void UpdateTrapEffects()
    {
        if (spikeTrap == null) return;
    
        spikeTrap.ClearDoTEffects();
    
        if (bleedingSpikes)
        {
            spikeTrap.SetBleedEffect(bleedDamage, bleedDuration, elementType, bleedSpikeVFX, critChance, critMultiplier);
        }
        else if (poisonSpikes)
        {
            spikeTrap.SetPoisonEffect(poisonDamage, poisonDuration, elementType, poisonSpikeVFX);
        }
    }
    
    private void SpawnTrapOnRoad()
    {
        Vector3 rayDirection = Quaternion.AngleAxis(downwardAngle, transform.right) * transform.forward;
    
        if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, raycastDistance, roadLayer))
        {
            GameObject trap = Instantiate(spikeTrapPrefab, hit.point, Quaternion.identity);
            spikeTrap = trap.GetComponent<SpikeTrapDamage>();
        
            spikeTrap.Setup(CreateDamageInfo(), whatIsEnemy, attackCooldown, this);
        
            // Set upgrades
            if (bleedingSpikes)
            {
                spikeTrap.SetBleedEffect(bleedDamage, bleedDuration, elementType, bleedSpikeVFX, critChance, critMultiplier);
            }
            else if (poisonSpikes)
            {
                spikeTrap.SetPoisonEffect(poisonDamage, poisonDuration, elementType, poisonSpikeVFX);
            }

            if (cripplingSpikes)
            {
                spikeTrap.SetCrippleEffect(slowPercent, slowDuration);
            }
        
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