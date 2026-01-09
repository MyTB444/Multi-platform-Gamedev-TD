using UnityEngine;

/// <summary>
/// Physical tower that spawns a BladeApparatus on the road below.
/// The dwarf character operates a hammer that drives the rotating blade mechanism.
/// 
/// Unique behavior: Does NOT attack directly - the spawned BladeApparatus
/// handles all enemy detection and damage. Tower provides animation and upgrades.
/// 
/// Uses raycast to find road surface, then auto-centers the apparatus on the tile.
/// </summary>
public class BladeTower : TowerBase
{
    // ==================== BLADE SPAWNING ====================
    [Header("Blade Spawning")]
    [SerializeField] private GameObject bladeApparatusPrefab;
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private LayerMask roadLayer;
    [SerializeField] private float downwardAngle = 45f;       // Angle to raycast toward road
    [SerializeField] private float baseSpinSpeed = 200f;
    [SerializeField] private float apparatusRotationOffset = 90f;
    [SerializeField] private float heightOffset = 1.42f;      // Height above road surface
    [SerializeField] private float forwardOffset = 0.5f;      // Forward offset for centering
    [SerializeField] private float apparatusScale = 0.6666667f;
    
    [Header("VFX")]
    [SerializeField] private Transform hammerImpactPoint;
    
    // ==================== ROAD CENTERING ====================
    [Header("Road Centering")]
    [SerializeField] private bool autoCenter = true;          // Auto-center on tile
    [SerializeField] private bool debugDrawRays = false;
    
    // ==================== UPGRADES ====================
    [Header("Blade Upgrades")]
    [SerializeField] [Range(0f, 0.5f)] private float spinSpeedBoostPercent = 0.2f;
    [Space]
    [SerializeField] private bool bleedChance = false;
    [SerializeField] [Range(0f, 1f)] private float bleedChancePercent = 0.45f;
    [SerializeField] private float bleedDamage = 15f;
    [SerializeField] private float bleedDuration = 4f;
    [SerializeField] private GameObject bleedVFX;
    [Space]
    [SerializeField] private bool moreBlades = false;         // Enable extra blades
    [Space]
    [SerializeField] private bool extendedReach = false;      // Scale up blade length
    [SerializeField] private float extendedBladeScale = 1.5f;
    
    private BladeApparatus bladeApparatus;
    private bool spinSpeedBoosted = false;
    
    protected override void Awake()
    {
        base.Awake();
        SpawnBladeOnRoad();
    }
    
    /// <summary>
    /// Raycasts to find road surface and spawns BladeApparatus.
    /// Auto-centers on tile if enabled.
    /// </summary>
    private void SpawnBladeOnRoad()
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
            
            // Apply forward offset to center apparatus on tile
            Vector3 forwardDir = transform.forward;
            forwardDir.y = 0;
            forwardDir.Normalize();
            spawnPosition += forwardDir * forwardOffset;
            
            Quaternion spawnRotation = GetBladeRotation();
            
            // Instantiate and configure apparatus
            GameObject apparatus = Instantiate(bladeApparatusPrefab, spawnPosition, spawnRotation);
            Vector3 originalScale = apparatus.transform.localScale;

            bladeApparatus = apparatus.GetComponent<BladeApparatus>();
            bladeApparatus.Setup(CreateDamageInfo(), attackRange, whatIsEnemy, this, characterAnimator, attackAnimationTrigger);
            
            // ===== APPLY CURRENT UPGRADES =====
            if (spinSpeedBoosted)
            {
                bladeApparatus.SetSpinSpeed(baseSpinSpeed * (1f + spinSpeedBoostPercent));
            }
            
            if (bleedChance)
            {
                bladeApparatus.SetBleedEffect(bleedChancePercent, bleedDamage, bleedDuration, elementType, bleedVFX);
            }
            
            if (moreBlades)
            {
                bladeApparatus.SetMoreBlades(true);
            }
            
            if (extendedReach)
            {
                bladeApparatus.SetExtendedReach(true, extendedBladeScale);
            }
            
            // Parent to tower and preserve scale
            apparatus.transform.SetParent(transform);
            apparatus.transform.localScale = originalScale;
        }
        else
        {
            Debug.LogWarning("BladeTower: No road found!");
        }
    }
    
    /// <summary>
    /// Plays hammer impact VFX at impact point.
    /// Called by animation event.
    /// </summary>
    public void PlayHammerEffect()
    {
        PlayAttackSound();
    
        if (attackSpawnEffectPrefab != null && hammerImpactPoint != null)
        {
            ObjectPooling.instance.GetVFX(attackSpawnEffectPrefab, hammerImpactPoint.position, Quaternion.identity, 2f);
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
        if (Mathf.Abs(forward.z) > Mathf.Abs(forward.x))
        {
            // Tower faces along Z - use hit X, center Z
            spawnPosition = new Vector3(hitPoint.x, hitPoint.y + heightOffset, tileCenter.z);
        }
        else
        {
            // Tower faces along X - center X, use hit Z
            spawnPosition = new Vector3(tileCenter.x, hitPoint.y + heightOffset, hitPoint.z);
        }
    
        return spawnPosition;
    }
    
    /// <summary>
    /// Calculates apparatus rotation to align with road direction.
    /// </summary>
    private Quaternion GetBladeRotation()
    {
        Vector3 roadDirection = transform.forward;
        roadDirection.y = 0;
        roadDirection.Normalize();
    
        Quaternion baseRotation = Quaternion.LookRotation(roadDirection, Vector3.up);
        return baseRotation * Quaternion.Euler(0f, apparatusRotationOffset, 0f);
    }
    
    /// <summary>
    /// Handles upgrade state changes from skill tree.
    /// Forwards relevant upgrades to BladeApparatus.
    /// </summary>
    public override void SetUpgrade(TowerUpgradeType upgradeType, bool enabled)
    {
        base.SetUpgrade(upgradeType, enabled);
        
        switch (upgradeType)
        {
            case TowerUpgradeType.DamageBoost:
                // Update apparatus damage when base tower damage changes
                if (bladeApparatus != null)
                {
                    bladeApparatus.UpdateDamageInfo(CreateDamageInfo());
                }
                break;
                
            case TowerUpgradeType.BladeSpinSpeed:
                spinSpeedBoosted = enabled;
                if (bladeApparatus != null)
                {
                    float newSpeed = enabled ? baseSpinSpeed * (1f + spinSpeedBoostPercent) : baseSpinSpeed;
                    bladeApparatus.SetSpinSpeed(newSpeed);
                }
                break;
                
            case TowerUpgradeType.BleedChance:
                bleedChance = enabled;
                if (bladeApparatus != null)
                {
                    if (enabled)
                    {
                        bladeApparatus.SetBleedEffect(bleedChancePercent, bleedDamage, bleedDuration, elementType, bleedVFX);
                    }
                    else
                    {
                        bladeApparatus.ClearBleedEffect();
                    }
                }
                break;
                
            case TowerUpgradeType.MoreBlades:
                moreBlades = enabled;
                if (bladeApparatus != null)
                {
                    bladeApparatus.SetMoreBlades(enabled);
                }
                break;
                
            case TowerUpgradeType.ExtendedReach:
                extendedReach = enabled;
                if (bladeApparatus != null)
                {
                    bladeApparatus.SetExtendedReach(enabled, extendedBladeScale);
                }
                break;
        }
    }
    
    /// <summary>
    /// Called by BladeApparatus to play attack sound via tower.
    /// </summary>
    public void PlayAttackSoundFromApparatus()
    {
        PlayAttackSound();
    }
    
    // ===== DISABLED BASE METHODS =====
    // This tower doesn't attack directly - apparatus handles everything
    protected override void HandleRotation() { }
    protected override void Attack() { }
    protected override bool CanAttack() { return false; }
    
    /// <summary>
    /// Editor visualization - shows range from apparatus position and raycast.
    /// </summary>
    protected override void OnDrawGizmos()
    {
        // Draw range from apparatus position if it exists, otherwise from tower
        Vector3 rangeCenter = (bladeApparatus != null) ? bladeApparatus.transform.position : transform.position;
        bool isActive = bladeApparatus != null && bladeApparatus.IsActive();
    
        Gizmos.color = isActive ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(rangeCenter, attackRange);
    
        // Draw raycast direction
        Vector3 rayDirection = Quaternion.AngleAxis(downwardAngle, transform.right) * transform.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, rayDirection * raycastDistance);
    
        // Debug: show hit point and centering
        if (debugDrawRays && Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, raycastDistance, roadLayer))
        {
            // Hit point (yellow)
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(hit.point, 0.2f);
        
            // Final spawn position (cyan)
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(GetTileCenterPosition(hit.point, hit.collider), 0.35f);
        }
    }
}