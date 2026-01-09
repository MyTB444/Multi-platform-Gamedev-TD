using UnityEngine;

/// <summary>
/// Physical tower that spawns a SpikeTrapDamage mechanism on the road below.
/// The dwarf character operates a hammer that triggers the spike trap.
/// 
/// Unique behavior: Does NOT attack directly - the spawned SpikeTrapDamage
/// handles all enemy detection and damage. Tower provides animation and upgrades.
/// 
/// Uses raycast to find road surface, then auto-centers the trap on the tile.
/// </summary>
public class TowerSpikeTrap : TowerBase
{
    // ==================== TRAP SPAWNING ====================
    [Header("Trap Spawning")]
    [SerializeField] private GameObject spikeTrapPrefab;
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private LayerMask roadLayer;
    [SerializeField] private float downwardAngle = 45f;  // Angle to raycast toward road
    
    // ==================== ROAD CENTERING ====================
    [Header("Road Centering")]
    [SerializeField] private bool autoCenter = true;     // Auto-center on tile
    [SerializeField] private bool debugDrawRays = false;
    
    [Header("VFX")]
    [SerializeField] private Transform hammerImpactPoint;
    
    // ==================== UPGRADES ====================
    [Header("Spike Upgrades")]
    [SerializeField] [Range(0f, 0.5f)] private float spikeTrapAttackSpeedPercent = 0.20f;
    [Space]
    [SerializeField] private bool poisonSpikes = false;    // Poison DoT upgrade
    [SerializeField] private float poisonDamage = 2f;
    [SerializeField] private float poisonDuration = 3f;
    [SerializeField] private GameObject poisonSpikeVFX;
    [Space]
    [SerializeField] private bool bleedingSpikes = false;  // Bleed DoT + crit upgrade
    [SerializeField] private float bleedDamage = 3f;
    [SerializeField] private float bleedDuration = 4f;
    [SerializeField] private GameObject bleedSpikeVFX;
    [SerializeField] [Range(0f, 1f)] private float critChance = 0.2f;
    [SerializeField] private float critMultiplier = 2f;
    [Space]
    [SerializeField] private bool cripplingSpikes = false; // Slow effect upgrade
    [SerializeField] private float slowPercent = 0.3f;
    [SerializeField] private float slowDuration = 2f;
    
    private SpikeTrapDamage spikeTrap;
    
    protected override void Awake()
    {
        base.Awake();
        SpawnTrapOnRoad();
    }
    
    /// <summary>
    /// Handles upgrade state changes from skill tree.
    /// Forwards relevant upgrades to SpikeTrapDamage.
    /// </summary>
    public override void SetUpgrade(TowerUpgradeType upgradeType, bool enabled)
    {
        base.SetUpgrade(upgradeType, enabled);
    
        switch (upgradeType)
        {
            case TowerUpgradeType.DamageBoost:
                // Update trap damage when base tower damage changes
                if (spikeTrap != null)
                {
                    spikeTrap.UpdateDamageInfo(CreateDamageInfo());
                }
                break;
                
            case TowerUpgradeType.SpikeTrapAttackSpeed:
                // Apply attack speed boost via base class
                attackSpeedBoost = enabled;
                attackSpeedBoostPercent = spikeTrapAttackSpeedPercent;
                ApplyStatUpgrades();
                // Update trap cooldown
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

    /// <summary>
    /// Updates DoT effects on trap. Bleed replaces poison (mutually exclusive).
    /// </summary>
    private void UpdateTrapEffects()
    {
        if (spikeTrap == null) return;
    
        // Clear existing DoT effects first
        spikeTrap.ClearDoTEffects();
    
        // Apply new effect (bleed takes priority over poison)
        if (bleedingSpikes)
        {
            spikeTrap.SetBleedEffect(bleedDamage, bleedDuration, elementType, bleedSpikeVFX, critChance, critMultiplier);
        }
        else if (poisonSpikes)
        {
            spikeTrap.SetPoisonEffect(poisonDamage, poisonDuration, elementType, poisonSpikeVFX);
        }
    }
    
    /// <summary>
    /// Raycasts to find road surface and spawns SpikeTrapDamage.
    /// Auto-centers on tile if enabled.
    /// </summary>
    private void SpawnTrapOnRoad()
    {
        // Calculate raycast direction at downward angle
        Vector3 rayDirection = Quaternion.AngleAxis(downwardAngle, transform.right) * transform.forward;
    
        if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, raycastDistance, roadLayer))
        {
            Vector3 spawnPosition;
            
            // Get spawn position (auto-centered or raw hit point)
            if (autoCenter)
            {
                spawnPosition = GetTileCenterPosition(hit.point, hit.collider);
            }
            else
            {
                spawnPosition = hit.point;
            }
            
            // Align trap with road direction
            Quaternion spawnRotation = GetTrapRotation();
            
            // Instantiate and configure trap
            GameObject trap = Instantiate(spikeTrapPrefab, spawnPosition, spawnRotation);
            spikeTrap = trap.GetComponent<SpikeTrapDamage>();
        
            spikeTrap.Setup(CreateDamageInfo(), whatIsEnemy, attackCooldown, this);
        
            // ===== APPLY CURRENT UPGRADES =====
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
        
            // Parent to tower
            trap.transform.SetParent(transform);
        }
        else
        {
            Debug.LogWarning("SpikeTrap: No road found!");
        }
    }
    
    /// <summary>
    /// Calculates spawn position centered on road tile.
    /// Uses tower's forward direction to determine which axis to center on.
    /// </summary>
    private Vector3 GetTileCenterPosition(Vector3 hitPoint, Collider roadCollider)
    {
        Vector3 tileCenter = roadCollider.transform.position;
    
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();
    
        Vector3 spawnPosition;
    
        // Determine centering axis based on tower facing direction
        // If facing Z: width is perpendicular (X), path is along (Z) - use hit X, center Z
        // If facing X: width is perpendicular (Z), path is along (X) - center X, use hit Z
        if (Mathf.Abs(forward.z) > Mathf.Abs(forward.x))
        {
            spawnPosition = new Vector3(hitPoint.x, hitPoint.y, tileCenter.z);
        }
        else
        {
            spawnPosition = new Vector3(tileCenter.x, hitPoint.y, hitPoint.z);
        }
    
        if (debugDrawRays)
        {
            Debug.Log($"SpikeTrap - Hit: {hitPoint}, TileCenter: {tileCenter}, Final: {spawnPosition}");
        }
    
        return spawnPosition;
    }
    
    /// <summary>
    /// Calculates trap rotation to align with road direction.
    /// </summary>
    private Quaternion GetTrapRotation()
    {
        Vector3 roadDirection = transform.forward;
        roadDirection.y = 0;
        roadDirection.Normalize();
        
        return Quaternion.LookRotation(roadDirection, Vector3.up);
    }
    
    /// <summary>
    /// Triggers hammer strike animation on tower character.
    /// Called by SpikeTrapDamage during trap cycle.
    /// </summary>
    public void PlayHammerAnimation()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(attackAnimationTrigger);
        }
    }
    
    /// <summary>
    /// Spawns hammer impact VFX and plays sound.
    /// Called by SpikeTrapDamage after spike delay.
    /// </summary>
    public void SpawnHammerImpactVFX()
    {
        PlayAttackSound();
        if (attackSpawnEffectPrefab != null && hammerImpactPoint != null)
        {
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, hammerImpactPoint.position, Quaternion.identity, 2f);
        }
    }
    
    // ===== DISABLED BASE METHODS =====
    // This tower doesn't attack directly - trap handles everything
    protected override void Attack() { }
    protected override bool CanAttack() { return false; }
    protected override void HandleRotation() { }
    
    /// <summary>
    /// Editor visualization - shows raycast and centering debug info.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw raycast direction
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