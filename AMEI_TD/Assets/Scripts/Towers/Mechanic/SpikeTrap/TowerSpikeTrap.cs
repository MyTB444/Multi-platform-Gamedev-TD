using UnityEngine;

public class TowerSpikeTrap : TowerBase
{
    [Header("Trap Spawning")]
    [SerializeField] private GameObject spikeTrapPrefab;
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private LayerMask roadLayer;
    [SerializeField] private float downwardAngle = 45f;
    
    [Header("Road Centering")]
    [SerializeField] private bool autoCenter = true;
    [SerializeField] private bool debugDrawRays = false;
    
    [Header("VFX")]
    [SerializeField] private Transform hammerImpactPoint;
    
    [Header("Spike Upgrades")]
    [SerializeField] [Range(0f, 0.5f)] private float spikeTrapAttackSpeedPercent = 0.20f;
    [Space]
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
            case TowerUpgradeType.SpikeTrapAttackSpeed:
                attackSpeedBoost = enabled;
                attackSpeedBoostPercent = spikeTrapAttackSpeedPercent;
                ApplyStatUpgrades();
                if (spikeTrap != null)
                {
                    spikeTrap.SetCooldown(attackCooldown);
                }
                break;
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
            Vector3 spawnPosition;
            
            if (autoCenter)
            {
                spawnPosition = GetTileCenterPosition(hit.point, hit.collider);
            }
            else
            {
                spawnPosition = hit.point;
            }
            
            // Get rotation to face along road
            Quaternion spawnRotation = GetTrapRotation();
            
            GameObject trap = Instantiate(spikeTrapPrefab, spawnPosition, spawnRotation);
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
    
    private Vector3 GetTileCenterPosition(Vector3 hitPoint, Collider roadCollider)
    {
        Vector3 tileCenter = roadCollider.transform.position;
    
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();
    
        Vector3 spawnPosition;
    
        // If tower faces mostly along Z, width is Z (keep green Z), path is X (use yellow X)
        // If tower faces mostly along X, width is X (keep green X), path is Z (use yellow Z)
        if (Mathf.Abs(forward.z) > Mathf.Abs(forward.x))
        {
            // Tower faces along Z - width is X, path is Z... wait no
            // Looking at screenshot: need yellow's X, green's Z
            spawnPosition = new Vector3(hitPoint.x, hitPoint.y, tileCenter.z);
        }
        else
        {
            // Tower faces along X
            spawnPosition = new Vector3(tileCenter.x, hitPoint.y, hitPoint.z);
        }
    
        if (debugDrawRays)
        {
            Debug.Log($"SpikeTrap - Hit: {hitPoint}, TileCenter: {tileCenter}, Final: {spawnPosition}");
        }
    
        return spawnPosition;
    }
    
    private Quaternion GetTrapRotation()
    {
        // Trap should align with road direction
        Vector3 roadDirection = transform.forward;
        roadDirection.y = 0;
        roadDirection.Normalize();
        
        return Quaternion.LookRotation(roadDirection, Vector3.up);
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
        PlayAttackSound();
        if (attackSpawnEffectPrefab != null && hammerImpactPoint != null)
        {
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, hammerImpactPoint.position, Quaternion.identity, 2f);
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
    
        if (debugDrawRays && Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, raycastDistance, roadLayer))
        {
            // Show hit point (yellow)
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(hit.point, 0.2f);
        
            // Show tile center (green)
            Gizmos.color = Color.green;
            Vector3 tileCenter = hit.collider.transform.position;
            Gizmos.DrawSphere(new Vector3(tileCenter.x, hit.point.y, tileCenter.z), 0.3f);
        
            // Show final spawn position (cyan)
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(GetTileCenterPosition(hit.point, hit.collider), 0.35f);
        }
    }
}